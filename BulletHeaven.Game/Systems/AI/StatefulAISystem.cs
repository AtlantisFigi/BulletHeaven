using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Systems.AI;

public sealed class StatefulAISystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<VelocityComponent>? _velocities;
    private ComponentStore<StatefulAIComponent>? _statefuls;
    private ComponentStore<RenderColorComponent>? _colors;
    private readonly CommandBuffer _cmd;

    public float TargetX { get; set; } = 640f;
    public float TargetY { get; set; } = 360f;
    public int MaxExtraSpawns { get; set; } = 500;
    private int _extraSpawns = 0;

    public StatefulAISystem(CommandBuffer cmd)
    {
        _cmd = cmd;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _statefuls = world.GetStore<StatefulAIComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        _extraSpawns = 0;
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null || _statefuls == null) return;

        bool hasColors = _colors != null;

        foreach (Entity e in _world.Query()
            .With<PositionComponent>()
            .With<VelocityComponent>()
            .With<StatefulAIComponent>())
        {
            ref readonly PositionComponent pos = ref _positions.Get(e);
            ref VelocityComponent vel = ref _velocities.Get(e);
            ref StatefulAIComponent ai = ref _statefuls.Get(e);

            float dx = TargetX - pos.X;
            float dy = TargetY - pos.Y;
            float distSq = dx * dx + dy * dy;
            float dist = MathF.Sqrt(distSq);
            float invDist = dist > 0.001f ? 1f / dist : 0f;

            switch (ai.Archetype)
            {
                case StatefulArchetype.Charger:
                    ai.StateTimer -= deltaTime;

                    switch (ai.State)
                    {
                        case EnemyState.Idle:
                            if (dist > 100f)
                            {
                                vel.X = dx * invDist * ai.NormalSpeed;
                                vel.Y = dy * invDist * ai.NormalSpeed;
                            }
                            else
                            {
                                vel.X = 0;
                                vel.Y = 0;
                            }

                            if (ai.StateTimer <= 0f)
                            {
                                ai.State = EnemyState.Telegraph;
                                ai.StateTimer = 0.4f;
                                vel.X = 0;
                                vel.Y = 0;
                                ai.DirX = dx * invDist;
                                ai.DirY = dy * invDist;

                                if (hasColors && _colors!.Has(e))
                                {
                                    ref RenderColorComponent col = ref _colors.Get(e);
                                    col.R = 255; col.G = 40; col.B = 40;
                                }
                            }
                            break;

                        case EnemyState.Telegraph:
                            vel.X = 0;
                            vel.Y = 0;

                            if (ai.StateTimer <= 0f)
                            {
                                ai.State = EnemyState.Charge;
                                ai.StateTimer = 0.6f;
                                vel.X = ai.DirX * ai.SpecialSpeed;
                                vel.Y = ai.DirY * ai.SpecialSpeed;

                                if (hasColors && _colors!.Has(e))
                                {
                                    ref RenderColorComponent col = ref _colors.Get(e);
                                    col.R = 255; col.G = 220; col.B = 0;
                                }
                            }
                            break;

                        case EnemyState.Charge:
                            vel.X = ai.DirX * ai.SpecialSpeed;
                            vel.Y = ai.DirY * ai.SpecialSpeed;

                            if (ai.StateTimer <= 0f)
                            {
                                ai.State = EnemyState.Cooldown;
                                ai.StateTimer = 0.5f;
                                vel.X = 0;
                                vel.Y = 0;

                                if (hasColors && _colors!.Has(e))
                                {
                                    ref RenderColorComponent col = ref _colors.Get(e);
                                    col.R = 90; col.G = 130; col.B = 180;
                                }
                            }
                            break;

                        case EnemyState.Cooldown:
                            vel.X = 0;
                            vel.Y = 0;

                            if (ai.StateTimer <= 0f)
                            {
                                ai.State = EnemyState.Idle;
                                ai.StateTimer = 1.8f;

                                if (hasColors && _colors!.Has(e))
                                {
                                    ref RenderColorComponent col = ref _colors.Get(e);
                                    col.R = 230; col.G = 110; col.B = 30;
                                }
                            }
                            break;
                    }
                    break;

                case StatefulArchetype.Spitter:
                    ai.ActionTimer -= deltaTime;

                    if (dist < ai.PreferredDistance - 40f)
                    {
                        vel.X = -dx * invDist * ai.NormalSpeed;
                        vel.Y = -dy * invDist * ai.NormalSpeed;
                    }
                    else if (dist > ai.PreferredDistance + 40f)
                    {
                        vel.X = dx * invDist * ai.NormalSpeed;
                        vel.Y = dy * invDist * ai.NormalSpeed;
                    }
                    else
                    {
                        vel.X = -dy * invDist * ai.NormalSpeed * 0.7f;
                        vel.Y = dx * invDist * ai.NormalSpeed * 0.7f;
                    }

                    if (ai.ActionTimer <= 0f)
                    {
                        ai.ActionTimer = ai.ActionInterval;

                        if (_extraSpawns < MaxExtraSpawns)
                        {
                            _extraSpawns++;
                            float bulletVx = dx * invDist * ai.SpecialSpeed;
                            float bulletVy = dy * invDist * ai.SpecialSpeed;
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
                    break;

                case StatefulArchetype.Summoner:
                    ai.ActionTimer -= deltaTime;

                    vel.X = -dy * invDist * ai.NormalSpeed * 0.4f;
                    vel.Y = dx * invDist * ai.NormalSpeed * 0.4f;

                    if (ai.ActionTimer <= 0f)
                    {
                        ai.ActionTimer = ai.ActionInterval;

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
                    break;
            }
        }
    }
}
