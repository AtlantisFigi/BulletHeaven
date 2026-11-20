using System;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class KnockbackSystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<KnockbackComponent>? _knockbacks;

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _knockbacks = world.GetStore<KnockbackComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _knockbacks == null) return;

        float decay = MathF.Exp(-6f * deltaTime);

        foreach (Entity e in _world.Query().With<PositionComponent>().With<KnockbackComponent>())
        {
            ref KnockbackComponent kb = ref _knockbacks.Get(e);
            if (kb.RemainingTime <= 0f)
            {
                kb.VelX = 0f;
                kb.VelY = 0f;
                continue;
            }

            ref PositionComponent pos = ref _positions.Get(e);
            pos.X += kb.VelX * deltaTime;
            pos.Y += kb.VelY * deltaTime;

            kb.VelX *= decay;
            kb.VelY *= decay;
            kb.RemainingTime -= deltaTime;

            if (kb.RemainingTime <= 0f || (kb.VelX * kb.VelX + kb.VelY * kb.VelY) < 4f)
            {
                kb.VelX = 0f;
                kb.VelY = 0f;
                kb.RemainingTime = 0f;
            }
        }
    }
}
