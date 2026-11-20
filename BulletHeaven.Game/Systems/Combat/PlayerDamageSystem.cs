using System;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Rendering;
using BulletHeaven.Game.Spatial;
using Raylib_cs;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class PlayerDamageSystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<RenderColorComponent>? _colors;
    private ComponentStore<EnemyProjectileComponent>? _enemyProjectiles;

    private readonly SpatialGrid _grid;
    private readonly DamageTextService _damageTexts;
    private readonly CommandBuffer? _cmd;

    public PlayerDamageSystem(SpatialGrid grid, DamageTextService damageTexts, CommandBuffer? cmd = null)
    {
        _grid = grid;
        _damageTexts = damageTexts;
        _cmd = cmd;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _players = world.GetStore<PlayerComponent>();
        _healths = world.GetStore<HealthComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        _enemyProjectiles = world.GetStore<EnemyProjectileComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _players == null || _healths == null || _enemies == null) return;

        foreach (Entity playerEntity in _world.Query().With<PlayerComponent>().With<PositionComponent>().With<HealthComponent>())
        {
            ref PlayerComponent player = ref _players.Get(playerEntity);
            ref HealthComponent health = ref _healths.Get(playerEntity);
            ref readonly PositionComponent pos = ref _positions.Get(playerEntity);

            if (health.IsDead) continue;

            if (player.InvulnerabilityTimer > 0f)
            {
                player.InvulnerabilityTimer -= deltaTime;
            }

            if (player.FlashTimer > 0f)
            {
                player.FlashTimer -= deltaTime;
            }

            float hitboxRadius = player.HitboxRadius > 0f ? player.HitboxRadius : PlayerComponent.CoreHitboxRadius;
            float visualRadius = (_colors != null && _colors.Has(playerEntity)) ? _colors.Get(playerEntity).Radius : 16f;
            bool tookHit = false;

            if (!player.IsInvulnerable)
            {
                float searchRadius = hitboxRadius + 48f;
                CellRange range = _grid.GetRange(pos.X, pos.Y, searchRadius);

                for (int r = range.MinRow; r <= range.MaxRow && !tookHit; r++)
                {
                    for (int c = range.MinCol; c <= range.MaxCol && !tookHit; c++)
                    {
                        var cell = _grid.GetCell(c, r);
                        for (int i = 0; i < cell.Count; i++)
                        {
                            Entity enemyEntity = cell[i];
                            if (!_world.IsAlive(enemyEntity) || !_enemies.Has(enemyEntity) || !_positions.Has(enemyEntity)) continue;
                            if (_healths.Has(enemyEntity) && _healths.Get(enemyEntity).IsDead) continue;

                            ref readonly PositionComponent ePos = ref _positions.Get(enemyEntity);
                            ref readonly EnemyComponent enemy = ref _enemies.Get(enemyEntity);

                            float enemyRadius = 10f;
                            if (_colors != null && _colors.Has(enemyEntity))
                            {
                                enemyRadius = _colors.Get(enemyEntity).Radius;
                            }

                            float hitDist = hitboxRadius + enemyRadius;
                            float dx = pos.X - ePos.X;
                            float dy = pos.Y - ePos.Y;
                            float distSq = dx * dx + dy * dy;

                            if (distSq <= hitDist * hitDist)
                            {
                                float damage = MathF.Max(1f, enemy.ContactDamage);
                                ApplyPlayerHit(ref player, ref health, in pos, visualRadius, damage);
                                tookHit = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (_enemyProjectiles != null)
            {
                Entity? hitProjectile = null;

                foreach (Entity projEntity in _world.Query().With<PositionComponent>().With<EnemyProjectileComponent>())
                {
                    ref EnemyProjectileComponent proj = ref _enemyProjectiles.Get(projEntity);
                    if (proj.Lifetime <= 0f) continue;

                    ref readonly PositionComponent projPos = ref _positions.Get(projEntity);
                    float projRadius = MathF.Max(1f, proj.Radius);
                    float hitDist = hitboxRadius + projRadius;
                    float dx = pos.X - projPos.X;
                    float dy = pos.Y - projPos.Y;
                    float distSq = dx * dx + dy * dy;

                    if (!player.IsInvulnerable && !tookHit && distSq <= hitDist * hitDist)
                    {
                        float damage = MathF.Max(1f, proj.Damage);
                        ApplyPlayerHit(ref player, ref health, in pos, visualRadius, damage);
                        proj.Lifetime = 0f;
                        hitProjectile = projEntity;
                        tookHit = true;
                        continue;
                    }

                    if (!proj.HasBeenGrazed && distSq > hitDist * hitDist)
                    {
                        float grazeDist = PlayerComponent.DefaultGrazeRadius + projRadius;
                        if (distSq <= grazeDist * grazeDist)
                        {
                            proj.HasBeenGrazed = true;
                            player.GrazeFlashTimer = 0.18f;

                            if (player.GrazeStacks < PlayerComponent.MaxGrazeStacks)
                            {
                                player.GrazeProgress++;
                                if (player.GrazeProgress >= PlayerComponent.GrazesPerStack)
                                {
                                    player.GrazeProgress -= PlayerComponent.GrazesPerStack;
                                    player.GrazeStacks = Math.Min(PlayerComponent.MaxGrazeStacks, player.GrazeStacks + 1);
                                    if (player.GrazeStacks >= PlayerComponent.MaxGrazeStacks)
                                    {
                                        player.GrazeProgress = 0;
                                    }
                                    _damageTexts.Spawn(pos.X, pos.Y - visualRadius - 10f, "STACK READY!", new Color(255, 230, 80, 255));
                                }
                                else
                                {
                                    _damageTexts.Spawn(pos.X, pos.Y - visualRadius - 6f, "GRAZE", new Color(90, 245, 255, 255));
                                }
                            }
                        }
                    }
                }

                if (hitProjectile.HasValue)
                {
                    if (_cmd != null)
                    {
                        _cmd.DestroyEntity(hitProjectile.Value);
                    }
                    else if (_world.IsAlive(hitProjectile.Value))
                    {
                        _world.DestroyEntity(hitProjectile.Value);
                    }
                }
            }
        }
    }

    private void ApplyPlayerHit(
        ref PlayerComponent player,
        ref HealthComponent health,
        in PositionComponent pos,
        float visualRadius,
        float damage)
    {
        health.Current = MathF.Max(0f, health.Current - damage);

        float maxHp = MathF.Max(1f, health.Max);
        float damageRatio = damage / maxHp;
        float iframeDuration;
        if (damageRatio <= 0.075f)
        {
            iframeDuration = 0.20f;
        }
        else if (damageRatio >= 0.15f)
        {
            iframeDuration = 0.40f;
        }
        else
        {
            float t = (damageRatio - 0.075f) / (0.15f - 0.075f);
            iframeDuration = 0.20f + t * (0.40f - 0.20f);
        }

        player.InvulnerabilityTimer = iframeDuration;
        player.FlashTimer = 0.12f;

        _damageTexts.Spawn(pos.X, pos.Y - visualRadius - 8f, $"-{(int)MathF.Round(damage)}", new Color(255, 70, 70, 255));
    }
}
