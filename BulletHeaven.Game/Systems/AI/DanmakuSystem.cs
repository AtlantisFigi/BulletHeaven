using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Systems.AI;

public sealed class DanmakuSystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<VelocityComponent>? _velocities;
    private ComponentStore<DanmakuPatternComponent>? _patterns;

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _patterns = world.GetStore<DanmakuPatternComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null || _patterns == null) return;

        foreach (Entity e in _world.Query()
            .With<PositionComponent>()
            .With<VelocityComponent>()
            .With<DanmakuPatternComponent>())
        {
            ref VelocityComponent vel = ref _velocities.Get(e);
            ref DanmakuPatternComponent pattern = ref _patterns.Get(e);

            pattern.Angle += pattern.AngularVelocity * deltaTime;

            if (pattern.Acceleration != 0f)
            {
                pattern.Speed += pattern.Acceleration * deltaTime;
            }

            vel.X = MathF.Cos(pattern.Angle) * pattern.Speed;
            vel.Y = MathF.Sin(pattern.Angle) * pattern.Speed;
        }
    }
}

