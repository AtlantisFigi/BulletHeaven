using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Systems.AI;

public sealed class SpitterSystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<VelocityComponent>? _velocities;
    private ComponentStore<SpitterComponent>? _spitters;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<EnemyProjectileComponent>? _enemyProjectiles;
    private readonly CommandBuffer _cmd;

    public float TargetX { get; set; } = 640f;
    public float TargetY { get; set; } = 360f;
    public int? MaxActiveProjectiles { get; set; } = null;

    public SpitterSystem(CommandBuffer cmd)
    {
        _cmd = cmd;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _spitters = world.GetStore<SpitterComponent>();
        _healths = world.GetStore<HealthComponent>();
        _enemyProjectiles = world.GetStore<EnemyProjectileComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null || _spitters == null) return;

        foreach (Entity p in _world.Query().With<PositionComponent>().With<PlayerComponent>())
        {
            if (_healths != null && _healths.Has(p) && _healths.Get(p).IsDead) continue;

            ref readonly PositionComponent pPos = ref _positions.Get(p);
            TargetX = pPos.X;
            TargetY = pPos.Y;
            break;
        }

        int pendingSpawnCount = 0;

        foreach (Entity e in _world.Query()
            .With<PositionComponent>()
            .With<VelocityComponent>()
            .With<SpitterComponent>())
        {
            if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) continue;

            ref readonly PositionComponent pos = ref _positions.Get(e);
            ref VelocityComponent vel = ref _velocities.Get(e);
            ref SpitterComponent spitter = ref _spitters.Get(e);

            spitter.ActionTimer -= deltaTime;

            float dx = TargetX - pos.X;
            float dy = TargetY - pos.Y;
            float distSq = dx * dx + dy * dy;
            float dist = MathF.Sqrt(distSq);
            float invDist = dist > 0.001f ? 1f / dist : 0f;

            if (dist < spitter.PreferredDistance - 40f)
            {
                vel.X = -dx * invDist * spitter.NormalSpeed;
                vel.Y = -dy * invDist * spitter.NormalSpeed;
            }
            else if (dist > spitter.PreferredDistance + 40f)
            {
                vel.X = dx * invDist * spitter.NormalSpeed;
                vel.Y = dy * invDist * spitter.NormalSpeed;
            }
            else
            {
                vel.X = -dy * invDist * spitter.NormalSpeed * 0.7f;
                vel.Y = dx * invDist * spitter.NormalSpeed * 0.7f;
            }

            if (spitter.ActionTimer <= 0f)
            {
                spitter.ActionTimer = spitter.ActionInterval;

                if (MaxActiveProjectiles.HasValue)
                {
                    int activeCount = (_enemyProjectiles?.Count ?? 0) + pendingSpawnCount;
                    if (activeCount >= MaxActiveProjectiles.Value)
                    {
                        continue;
                    }
                }

                float baseAngle = dist > 0.001f ? MathF.Atan2(dy, dx) : 0f;
                int count = spitter.ProjectileCount >= 3
                    ? Math.Min(spitter.ProjectileCount, 5)
                    : Random.Shared.Next(3, 6);
                const float stepRad = 20f * (MathF.PI / 180f);
                float bulletSpeed = spitter.BulletSpeed > 0f ? spitter.BulletSpeed : 220f;
                float spawnX = pos.X;
                float spawnY = pos.Y;

                for (int i = 0; i < count; i++)
                {
                    float a = baseAngle + (i - (count - 1) * 0.5f) * stepRad;
                    float bulletVx = MathF.Cos(a) * bulletSpeed;
                    float bulletVy = MathF.Sin(a) * bulletSpeed;

                    _cmd.CreateEntity((w, b) =>
                    {
                        w.AddComponent(b, new PositionComponent(spawnX, spawnY));
                        w.AddComponent(b, new VelocityComponent(bulletVx, bulletVy));
                        w.AddComponent(b, new EnemyProjectileComponent(damage: 8f, radius: 6f, lifetime: 4.5f));
                        w.AddComponent(b, new RenderColorComponent(210, 75, 255, 255, 6f));
                    });
                    pendingSpawnCount++;
                }
            }
        }
    }
}

