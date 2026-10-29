using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Systems.AI;

public sealed class SwarmAISystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<VelocityComponent>? _velocities;
    private ComponentStore<SwarmAIComponent>? _swarms;

    public float TargetX { get; set; } = 640f;
    public float TargetY { get; set; } = 360f;

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _swarms = world.GetStore<SwarmAIComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null || _swarms == null) return;

        foreach (Entity e in _world.Query()
            .With<PositionComponent>()
            .With<VelocityComponent>()
            .With<SwarmAIComponent>())
        {
            ref readonly PositionComponent pos = ref _positions.Get(e);
            ref VelocityComponent vel = ref _velocities.Get(e);
            ref readonly SwarmAIComponent swarm = ref _swarms.Get(e);

            float dx = TargetX - pos.X;
            float dy = TargetY - pos.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq > 1f)
            {
                float invDist = 1f / MathF.Sqrt(distSq);
                vel.X = dx * invDist * swarm.Speed;
                vel.Y = dy * invDist * swarm.Speed;
            }
            else
            {
                vel.X = 0f;
                vel.Y = 0f;
            }
        }
    }
}

