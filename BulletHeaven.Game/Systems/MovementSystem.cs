using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;

namespace BulletHeaven.Game.Systems;

public sealed class MovementSystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<VelocityComponent>? _velocities;

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null) return;

        // Itereer door alle entiteiten die zowel Position als Velocity bezitten
        foreach (Entity e in _world.Query().With<PositionComponent>().With<VelocityComponent>())
        {
            ref PositionComponent pos = ref _positions.Get(e);
            ref readonly VelocityComponent vel = ref _velocities.Get(e);

            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;

            // Screen wrap binnen schermgrenzen (1280 x 720)
            if (pos.X < 0) pos.X += 1280;
            else if (pos.X > 1280) pos.X -= 1280;
            if (pos.Y < 0) pos.Y += 720;
            else if (pos.Y > 720) pos.Y -= 720;
        }
    }
}

