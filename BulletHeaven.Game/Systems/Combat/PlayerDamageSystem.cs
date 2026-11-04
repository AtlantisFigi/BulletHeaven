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
    private ComponentStore<PlayerStatsComponent>? _stats;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<RenderColorComponent>? _colors;

    private readonly SpatialGrid _grid;
    private readonly DamageTextService _damageTexts;
    private readonly Random _rng = new(42);

    public PlayerDamageSystem(SpatialGrid grid, DamageTextService damageTexts)
    {
        _grid = grid;
        _damageTexts = damageTexts;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _players = world.GetStore<PlayerComponent>();
        _stats = world.GetStore<PlayerStatsComponent>();
        _healths = world.GetStore<HealthComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _colors = world.GetStore<RenderColorComponent>();
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

            if (player.InvulnerabilityTimer > 0f) continue;

            float playerRadius = 12f;
            if (_colors != null && _colors.Has(playerEntity))
            {
                playerRadius = _colors.Get(playerEntity).Radius;
            }

            float searchRadius = playerRadius + 14f;
            CellRange range = _grid.GetRange(pos.X, pos.Y, searchRadius);
            bool tookHit = false;

            for (int r = range.MinRow; r <= range.MaxRow && !tookHit; r++)
            {
                for (int c = range.MinCol; c <= range.MaxCol && !tookHit; c++)
                {
                    var cell = _grid.GetCell(c, r);
                    for (int i = 0; i < cell.Count; i++)
                    {
                        Entity enemyEntity = cell[i];
                        if (!_enemies.Has(enemyEntity) || !_positions.Has(enemyEntity)) continue;

                        ref readonly PositionComponent ePos = ref _positions.Get(enemyEntity);
                        ref readonly EnemyComponent enemy = ref _enemies.Get(enemyEntity);

                        float enemyRadius = 10f;
                        if (_colors != null && _colors.Has(enemyEntity))
                        {
                            enemyRadius = _colors.Get(enemyEntity).Radius;
                        }

                        float hitDist = playerRadius + enemyRadius;
                        float dx = pos.X - ePos.X;
                        float dy = pos.Y - ePos.Y;
                        float distSq = dx * dx + dy * dy;

                        if (distSq <= hitDist * hitDist)
                        {
                            float dodgeChance = 0f;
                            if (_stats != null && _stats.Has(playerEntity))
                            {
                                dodgeChance = _stats.Get(playerEntity).DodgeChance;
                            }

                            bool dodged = dodgeChance > 0f && (_rng.NextDouble() < dodgeChance);
                            if (dodged)
                            {
                                player.InvulnerabilityTimer = 0.20f;
                                _damageTexts.Spawn(pos.X, pos.Y - playerRadius - 8f, "DODGE", new Color(80, 230, 255, 255));
                                tookHit = true;
                                break;
                            }

                            float damage = enemy.ContactDamage;
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

                            _damageTexts.Spawn(pos.X, pos.Y - playerRadius - 8f, $"-{(int)damage}", new Color(255, 70, 70, 255));
                            tookHit = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
