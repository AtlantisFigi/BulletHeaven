using System;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.Game.Components;

namespace BulletHeaven.Game.Systems.Combat;

public static class CombatKnockback
{
    public const float MaxKnockbackSpeed = 1200f;

    public static void Apply(
        World world,
        ComponentStore<KnockbackComponent>? store,
        Entity enemy,
        float dirX,
        float dirY,
        float strength,
        float duration = 0.22f)
    {
        if (store == null || strength <= 0f || duration <= 0f || !world.IsAlive(enemy)) return;

        float lenSq = dirX * dirX + dirY * dirY;
        if (lenSq <= 0.000001f) return;

        float invLen = 1f / MathF.Sqrt(lenSq);
        float nx = dirX * invLen;
        float ny = dirY * invLen;

        ref KnockbackComponent kb = ref store.TryGet(enemy, out bool found);
        if (found)
        {
            kb.VelX += nx * strength;
            kb.VelY += ny * strength;
            float speedSq = kb.VelX * kb.VelX + kb.VelY * kb.VelY;
            if (speedSq > MaxKnockbackSpeed * MaxKnockbackSpeed)
            {
                float scale = MaxKnockbackSpeed / MathF.Sqrt(speedSq);
                kb.VelX *= scale;
                kb.VelY *= scale;
            }
            kb.RemainingTime = MathF.Max(kb.RemainingTime, duration);
        }
        else
        {
            float vx = nx * strength;
            float vy = ny * strength;
            float speedSq = vx * vx + vy * vy;
            if (speedSq > MaxKnockbackSpeed * MaxKnockbackSpeed)
            {
                float scale = MaxKnockbackSpeed / MathF.Sqrt(speedSq);
                vx *= scale;
                vy *= scale;
            }
            store.Add(enemy, new KnockbackComponent(vx, vy, duration));
        }
    }
}
