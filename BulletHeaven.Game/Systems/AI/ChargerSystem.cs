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

    public float TargetX { get; set; } = 640f;
    public float TargetY { get; set; } = 360f;

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _chargers = world.GetStore<ChargerComponent>();
        _colors = world.GetStore<RenderColorComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null || _chargers == null) return;

        bool hasColors = _colors != null;

        foreach (Entity e in _world.Query()
            .With<PositionComponent>()
            .With<VelocityComponent>()
            .With<ChargerComponent>())
        {
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
                        vel.X = 0;
                        vel.Y = 0;
                    }

                    if (charger.StateTimer <= 0f)
                    {
                        charger.State = EnemyState.Telegraph;
                        charger.StateTimer = 0.4f;
                        vel.X = 0;
                        vel.Y = 0;

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
                            col.R = 255; col.G = 40; col.B = 40;
                        }
                    }
                    break;

                case EnemyState.Telegraph:
                    vel.X = 0;
                    vel.Y = 0;

                    if (charger.StateTimer <= 0f)
                    {
                        charger.State = EnemyState.Charge;
                        charger.StateTimer = 0.6f;
                        vel.X = charger.DirX * charger.ChargeSpeed;
                        vel.Y = charger.DirY * charger.ChargeSpeed;

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 255; col.G = 220; col.B = 0;
                        }
                    }
                    break;

                case EnemyState.Charge:
                    vel.X = charger.DirX * charger.ChargeSpeed;
                    vel.Y = charger.DirY * charger.ChargeSpeed;

                    if (charger.StateTimer <= 0f)
                    {
                        charger.State = EnemyState.Cooldown;
                        charger.StateTimer = 0.5f;
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

                    if (charger.StateTimer <= 0f)
                    {
                        charger.State = EnemyState.Idle;
                        charger.StateTimer = 1.8f;

                        if (hasColors && _colors!.Has(e))
                        {
                            ref RenderColorComponent col = ref _colors.Get(e);
                            col.R = 230; col.G = 110; col.B = 30;
                        }
                    }
                    break;
            }
        }
    }
}

