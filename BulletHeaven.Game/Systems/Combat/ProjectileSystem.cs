using System;
using System.Collections.Generic;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Spatial;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class ProjectileSystem : ISystem
{
    private World? _world;
    private readonly CommandBuffer _cmd;
    private readonly SpatialGrid? _grid;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<ProjectileComponent>? _projectiles;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<VelocityComponent>? _velocities;

    public ProjectileSystem(CommandBuffer cmd, SpatialGrid? grid = null)
    {
        _cmd = cmd;
        _grid = grid;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _projectiles = world.GetStore<ProjectileComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _healths = world.GetStore<HealthComponent>();
        _velocities = world.GetStore<VelocityComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _projectiles == null || _enemies == null) return;

        foreach (Entity projEntity in _world.Query().With<PositionComponent>().With<ProjectileComponent>())
        {
            ref ProjectileComponent proj = ref _projectiles.Get(projEntity);
            proj.Lifetime -= deltaTime;

            if (proj.Lifetime <= 0f)
            {
                _cmd.DestroyEntity(projEntity);
                continue;
            }

            ref readonly PositionComponent projPos = ref _positions.Get(projEntity);
            float hitRadius = proj.Radius + 12f;
            float hitRadiusSq = hitRadius * hitRadius;
            bool hitConsumed = false;

            if (_grid != null)
            {
                CellRange range = _grid.GetRange(projPos.X, projPos.Y, hitRadius);
                for (int r = range.MinRow; r <= range.MaxRow && !hitConsumed; r++)
                {
                    for (int c = range.MinCol; c <= range.MaxCol && !hitConsumed; c++)
                    {
                        List<Entity> cell = _grid.GetCell(c, r);
                        for (int i = 0; i < cell.Count; i++)
                        {
                            Entity enemyEntity = cell[i];
                            if (_healths != null && _healths.Has(enemyEntity) && _healths.Get(enemyEntity).IsDead) continue;

                            ref readonly PositionComponent enemyPos = ref _positions.Get(enemyEntity);
                            float dx = enemyPos.X - projPos.X;
                            float dy = enemyPos.Y - projPos.Y;
                            float distSq = dx * dx + dy * dy;

                            if (distSq <= hitRadiusSq)
                            {
                                if (_healths != null && _healths.Has(enemyEntity))
                                {
                                    ref HealthComponent hp = ref _healths.Get(enemyEntity);
                                    hp.Current -= proj.Damage;

                                    if (hp.IsDead)
                                    {
                                        _cmd.DestroyEntity(enemyEntity);
                                    }
                                }

                                if (_velocities != null && _velocities.Has(enemyEntity))
                                {
                                    float dist = MathF.Sqrt(MathF.Max(0.001f, distSq));
                                    ref VelocityComponent vel = ref _velocities.Get(enemyEntity);
                                    vel.X += (dx / dist) * proj.Knockback;
                                    vel.Y += (dy / dist) * proj.Knockback;
                                }

                                if (proj.Lifesteal > 0f && proj.SourcePlayer != default && _healths != null && _healths.Has(proj.SourcePlayer))
                                {
                                    ref HealthComponent playerHp = ref _healths.Get(proj.SourcePlayer);
                                    if (!playerHp.IsDead)
                                    {
                                        playerHp.Current = MathF.Min(playerHp.Max, playerHp.Current + proj.Lifesteal);
                                    }
                                }

                                proj.HitsLeft--;
                                if (proj.HitsLeft <= 0)
                                {
                                    _cmd.DestroyEntity(projEntity);
                                    hitConsumed = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (Entity enemyEntity in _world.Query().With<PositionComponent>().With<EnemyComponent>())
                {
                    if (_healths != null && _healths.Has(enemyEntity) && _healths.Get(enemyEntity).IsDead) continue;

                    ref readonly PositionComponent enemyPos = ref _positions.Get(enemyEntity);
                    float dx = enemyPos.X - projPos.X;
                    float dy = enemyPos.Y - projPos.Y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= hitRadiusSq)
                    {
                        if (_healths != null && _healths.Has(enemyEntity))
                        {
                            ref HealthComponent hp = ref _healths.Get(enemyEntity);
                            hp.Current -= proj.Damage;

                            if (hp.IsDead)
                            {
                                _cmd.DestroyEntity(enemyEntity);
                            }
                        }

                        if (_velocities != null && _velocities.Has(enemyEntity))
                        {
                            float dist = MathF.Sqrt(MathF.Max(0.001f, distSq));
                            ref VelocityComponent vel = ref _velocities.Get(enemyEntity);
                            vel.X += (dx / dist) * proj.Knockback;
                            vel.Y += (dy / dist) * proj.Knockback;
                        }

                        if (proj.Lifesteal > 0f && proj.SourcePlayer != default && _healths != null && _healths.Has(proj.SourcePlayer))
                        {
                            ref HealthComponent playerHp = ref _healths.Get(proj.SourcePlayer);
                            if (!playerHp.IsDead)
                            {
                                playerHp.Current = MathF.Min(playerHp.Max, playerHp.Current + proj.Lifesteal);
                            }
                        }

                        proj.HitsLeft--;
                        if (proj.HitsLeft <= 0)
                        {
                            _cmd.DestroyEntity(projEntity);
                            break;
                        }
                    }
                }
            }
        }
    }
}
