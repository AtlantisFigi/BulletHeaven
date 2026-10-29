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

    public float TargetX { get; set; } = 640f;
    public float TargetY { get; set; } = 360f;

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _statefuls = world.GetStore<StatefulAIComponent>();
        _colors = world.GetStore<RenderColorComponent>();
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

            ai.StateTimer -= deltaTime;

            switch (ai.State)
            {
                case EnemyState.Idle:
                    float dx = TargetX - pos.X;
                    float dy = TargetY - pos.Y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq > 100f)
                    {
                        float invDist = 1f / MathF.Sqrt(distSq);
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
                        ai.StateTimer = ai.TelegraphDuration;
                        vel.X = 0;
                        vel.Y = 0;

                        if (distSq > 1f)
                        {
                            float invDist = 1f / MathF.Sqrt(distSq);
                            ai.ChargeDirX = dx * invDist;
                            ai.ChargeDirY = dy * invDist;
                        }
                        else
                        {
                            ai.ChargeDirX = 1f;
                            ai.ChargeDirY = 0f;
                        }

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 255; col.G = 50; col.B = 50; // Rood: waarschuwing!
                        }
                    }
                    break;

                case EnemyState.Telegraph:
                    vel.X = 0;
                    vel.Y = 0;

                    if (ai.StateTimer <= 0f)
                    {
                        ai.State = EnemyState.Charge;
                        ai.StateTimer = ai.ChargeDuration;
                        vel.X = ai.ChargeDirX * ai.ChargeSpeed;
                        vel.Y = ai.ChargeDirY * ai.ChargeSpeed;

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 255; col.G = 220; col.B = 0; // Geel: actieve aanval
                        }
                    }
                    break;

                case EnemyState.Charge:
                    // Blijf in de vergrendelde richting bewegen
                    vel.X = ai.ChargeDirX * ai.ChargeSpeed;
                    vel.Y = ai.ChargeDirY * ai.ChargeSpeed;

                    if (ai.StateTimer <= 0f)
                    {
                        // Overgang naar Cooldown
                        ai.State = EnemyState.Cooldown;
                        ai.StateTimer = ai.CooldownDuration;
                        vel.X = 0;
                        vel.Y = 0;

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 80; col.G = 120; col.B = 180; // Blauwgrijs: uitgeput
                        }
                    }
                    break;

                case EnemyState.Cooldown:
                    vel.X = 0;
                    vel.Y = 0;

                    if (ai.StateTimer <= 0f)
                    {
                        ai.State = EnemyState.Idle;
                        ai.StateTimer = 1.5f;

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 220; col.G = 120; col.B = 40; // Oranje: normale vijand
                        }
                    }
                    break;
            }
        }
    }
}

