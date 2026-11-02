using System;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class ProjectileSystem : ISystem
{
    private World? _world;
    private readonly CommandBuffer _cmd;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<ProjectileComponent>? _projectiles;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<VelocityComponent>? _velocities;

    public ProjectileSystem(CommandBuffer cmd)
    {
        _cmd = cmd;
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

            foreach (Entity enemyEntity in _world.Query().With<PositionComponent>().With<EnemyComponent>())
            {
                if (_healths != null && _healths.Has(enemyEntity) && _healths.Get(enemyEntity).IsDead) continue;

                ref readonly PositionComponent enemyPos = ref _positions.Get(enemyEntity);
                float dx = enemyPos.X - projPos.X;
                float dy = enemyPos.Y - projPos.Y;
                float distSq = dx * dx + dy * dy;

                float hitRadius = proj.Radius + 12f;
                if (distSq <= hitRadius * hitRadius)
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

