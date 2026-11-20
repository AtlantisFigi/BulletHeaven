using System;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class BoomerangSystem : ISystem
{
    private World? _world;
    private readonly CommandBuffer _cmd;

    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<VelocityComponent>? _velocities;
    private ComponentStore<BoomerangComponent>? _boomerangs;
    private ComponentStore<ProjectileComponent>? _projectiles;
    private ComponentStore<ProjectileVisualComponent>? _projVisuals;

    public BoomerangSystem(CommandBuffer cmd)
    {
        _cmd = cmd;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _boomerangs = world.GetStore<BoomerangComponent>();
        _projectiles = world.GetStore<ProjectileComponent>();
        _projVisuals = world.GetStore<ProjectileVisualComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _velocities == null || _boomerangs == null || _projectiles == null)
            return;

        foreach (Entity e in _world.Query()
            .With<PositionComponent>()
            .With<VelocityComponent>()
            .With<BoomerangComponent>()
            .With<ProjectileComponent>())
        {
            ref readonly PositionComponent pos = ref _positions.Get(e);
            ref VelocityComponent vel = ref _velocities.Get(e);
            ref BoomerangComponent boom = ref _boomerangs.Get(e);
            ref ProjectileComponent proj = ref _projectiles.Get(e);

            float spinSpeed = 14f;
            if (_projVisuals != null && _projVisuals.Has(e))
            {
                ref ProjectileVisualComponent vis = ref _projVisuals.Get(e);
                if (vis.SpinSpeed > 0f)
                {
                    spinSpeed = vis.SpinSpeed;
                }
                boom.RotationAngle += spinSpeed * deltaTime;
                vis.SpinAngle = boom.RotationAngle;
            }
            else
            {
                boom.RotationAngle += spinSpeed * deltaTime;
            }

            float stepDist = boom.Speed * deltaTime;

            if (!boom.IsReturning)
            {
                boom.OutboundDistanceRemaining -= stepDist;
                if (boom.OutboundDistanceRemaining <= 0f)
                {
                    if (!_world.IsAlive(boom.Owner) || !_positions.Has(boom.Owner))
                    {
                        _cmd.DestroyEntity(e);
                        continue;
                    }

                    ref readonly PositionComponent ownerPos = ref _positions.Get(boom.Owner);
                    float dx = ownerPos.X - pos.X;
                    float dy = ownerPos.Y - pos.Y;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    if (dist < 0.001f)
                    {
                        _cmd.DestroyEntity(e);
                        continue;
                    }

                    float dirX = dx / dist;
                    float dirY = dy / dist;

                    vel.X = dirX * boom.Speed;
                    vel.Y = dirY * boom.Speed;

                    boom.IsReturning = true;
                    boom.ReturnTargetX = ownerPos.X;
                    boom.ReturnTargetY = ownerPos.Y;
                    boom.MaxReturnDistance = dist + 160f;
                    boom.PassedReturnTarget = false;
                    proj.HitEpoch++;

                    if (_projVisuals != null && _projVisuals.Has(e))
                    {
                        ref ProjectileVisualComponent vis = ref _projVisuals.Get(e);
                        vis.Angle = MathF.Atan2(dy, dx);
                    }
                }
            }
            else
            {
                boom.MaxReturnDistance -= stepDist;
                if (!_world.IsAlive(boom.Owner) || boom.MaxReturnDistance <= 0f)
                {
                    _cmd.DestroyEntity(e);
                    continue;
                }

                if (_positions.Has(boom.Owner))
                {
                    ref readonly PositionComponent ownerPos = ref _positions.Get(boom.Owner);
                    float cdx = ownerPos.X - pos.X;
                    float cdy = ownerPos.Y - pos.Y;
                    if (cdx * cdx + cdy * cdy <= 22f * 22f)
                    {
                        _cmd.DestroyEntity(e);
                        continue;
                    }
                }

                float tdx = boom.ReturnTargetX - pos.X;
                float tdy = boom.ReturnTargetY - pos.Y;
                float dot = tdx * vel.X + tdy * vel.Y;
                if (dot <= 0f)
                {
                    boom.PassedReturnTarget = true;
                    _cmd.DestroyEntity(e);
                }
            }
        }
    }
}
