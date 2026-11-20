using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Systems.AI;

public sealed class ChargerSystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<VelocityComponent>? _velocities;
    private ComponentStore<ChargerComponent>? _chargers;
    private ComponentStore<RenderColorComponent>? _colors;
    private ComponentStore<HealthComponent>? _healths;

    public float TargetX { get; set; } = 640f;
    public float TargetY { get; set; } = 360f;

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _chargers = world.GetStore<ChargerComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        _healths = world.GetStore<HealthComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null || _chargers == null) return;

        foreach (Entity p in _world.Query().With<PositionComponent>().With<PlayerComponent>())
        {
            if (_healths != null && _healths.Has(p) && _healths.Get(p).IsDead) continue;

            ref readonly PositionComponent pPos = ref _positions.Get(p);
            TargetX = pPos.X;
            TargetY = pPos.Y;
            break;
        }

        bool hasColors = _colors != null;

        foreach (Entity e in _world.Query()
            .With<PositionComponent>()
            .With<VelocityComponent>()
            .With<ChargerComponent>())
        {
            if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) continue;

            ref readonly PositionComponent pos = ref _positions.Get(e);
            ref VelocityComponent vel = ref _velocities.Get(e);
            ref ChargerComponent charger = ref _chargers.Get(e);

            charger.StateTimer -= deltaTime;

            float dx = TargetX - pos.X;
            float dy = TargetY - pos.Y;
            float distSq = dx * dx + dy * dy;

            switch (charger.State)
            {
                case EnemyState.Idle:
                    if (distSq > 100f)
                    {
                        float invDist = 1f / MathF.Sqrt(distSq);
                        vel.X = dx * invDist * charger.NormalSpeed;
                        vel.Y = dy * invDist * charger.NormalSpeed;
                    }
                    else
                    {
                        vel.X = 0f;
                        vel.Y = 0f;
                    }

                    float triggerRange = charger.TriggerRange > 0f ? charger.TriggerRange : 280f;
                    if (charger.StateTimer <= 0f && distSq <= triggerRange * triggerRange)
                    {
                        charger.State = EnemyState.Telegraph;
                        charger.StateTimer = 0.55f;
                        vel.X = 0f;
                        vel.Y = 0f;

                        if (distSq > 1f)
                        {
                            float invDist = 1f / MathF.Sqrt(distSq);
                            charger.DirX = dx * invDist;
                            charger.DirY = dy * invDist;
                        }
                        else
                        {
                            charger.DirX = 1f;
                            charger.DirY = 0f;
                        }

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 255; col.G = 135; col.B = 30;
                        }
                    }
                    break;

                case EnemyState.Telegraph:
                    vel.X = 0f;
                    vel.Y = 0f;

                    if (charger.StateTimer <= 0f)
                    {
                        charger.State = EnemyState.Charge;
                        charger.StateTimer = 0.65f;
                        vel.X = charger.DirX * charger.ChargeSpeed;
                        vel.Y = charger.DirY * charger.ChargeSpeed;

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 255; col.G = 220; col.B = 45;
                        }
                    }
                    break;

                case EnemyState.Charge:
                    vel.X = charger.DirX * charger.ChargeSpeed;
                    vel.Y = charger.DirY * charger.ChargeSpeed;

                    if (charger.StateTimer <= 0f)
                    {
                        charger.State = EnemyState.Cooldown;
                        charger.StateTimer = 0.45f;
                        vel.X = 0f;
                        vel.Y = 0f;

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 215; col.G = 140; col.B = 35;
                        }
                    }
                    break;

                case EnemyState.Cooldown:
                    vel.X = 0f;
                    vel.Y = 0f;

                    if (charger.StateTimer <= 0f)
                    {
                        charger.State = EnemyState.Idle;
                        charger.StateTimer = 1.0f;

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 245; col.G = 175; col.B = 35;
                        }
                    }
                    break;
            }
        }
    }
}

