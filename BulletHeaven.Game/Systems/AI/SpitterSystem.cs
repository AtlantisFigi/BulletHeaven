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
    private readonly CommandBuffer _cmd;

    public float TargetX { get; set; } = 640f;
    public float TargetY { get; set; } = 360f;
    public int MaxExtraSpawns { get; set; } = 500;
    private int _extraSpawns = 0;

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
        _extraSpawns = 0;
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null || _spitters == null) return;

        foreach (Entity e in _world.Query()
            .With<PositionComponent>()
            .With<VelocityComponent>()
            .With<SpitterComponent>())
        {
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

                if (_extraSpawns < MaxExtraSpawns)
                {
                    _extraSpawns++;
                    float bulletVx = dx * invDist * spitter.BulletSpeed;
                    float bulletVy = dy * invDist * spitter.BulletSpeed;
                    float spawnX = pos.X;
                    float spawnY = pos.Y;

                    _cmd.CreateEntity((w, b) =>
                    {
                        w.AddComponent(b, new PositionComponent(spawnX, spawnY));
                        w.AddComponent(b, new VelocityComponent(bulletVx, bulletVy));
                        w.AddComponent(b, new RenderColorComponent(220, 80, 255, 255, 3f));
                    });
                }
            }
        }
    }
}

