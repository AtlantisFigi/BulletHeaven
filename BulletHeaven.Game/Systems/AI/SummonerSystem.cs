using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Systems.AI;

public sealed class SummonerSystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<VelocityComponent>? _velocities;
    private ComponentStore<SummonerComponent>? _summoners;
    private readonly CommandBuffer _cmd;

    public float TargetX { get; set; } = 640f;
    public float TargetY { get; set; } = 360f;
    public int MaxExtraSpawns { get; set; } = 500;
    private int _extraSpawns = 0;

    public SummonerSystem(CommandBuffer cmd)
    {
        _cmd = cmd;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _summoners = world.GetStore<SummonerComponent>();
        _extraSpawns = 0;
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null || _summoners == null) return;

        foreach (Entity e in _world.Query()
            .With<PositionComponent>()
            .With<VelocityComponent>()
            .With<SummonerComponent>())
        {
            ref readonly PositionComponent pos = ref _positions.Get(e);
            ref VelocityComponent vel = ref _velocities.Get(e);
            ref SummonerComponent summoner = ref _summoners.Get(e);

            summoner.ActionTimer -= deltaTime;

            float dx = TargetX - pos.X;
            float dy = TargetY - pos.Y;
            float distSq = dx * dx + dy * dy;
            float dist = MathF.Sqrt(distSq);
            float invDist = dist > 0.001f ? 1f / dist : 0f;

            vel.X = -dy * invDist * summoner.NormalSpeed * 0.4f;
            vel.Y = dx * invDist * summoner.NormalSpeed * 0.4f;

            if (summoner.ActionTimer <= 0f)
            {
                summoner.ActionTimer = summoner.ActionInterval;

                if (_extraSpawns < MaxExtraSpawns)
                {
                    _extraSpawns++;
                    float spawnX = pos.X + (float)Random.Shared.NextDouble() * 20f - 10f;
                    float spawnY = pos.Y + (float)Random.Shared.NextDouble() * 20f - 10f;

                    _cmd.CreateEntity((w, m) =>
                    {
                        w.AddComponent(m, new PositionComponent(spawnX, spawnY));
                        w.AddComponent(m, new VelocityComponent(0f, 0f));
                        w.AddComponent(m, new SwarmAIComponent(80f));
                        w.AddComponent(m, new RenderColorComponent(80, 255, 120, 240, 2.5f));
                    });
                }
            }
        }
    }
}

