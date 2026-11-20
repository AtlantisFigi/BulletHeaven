using System;
using System.Collections.Generic;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.Game.Components;

namespace BulletHeaven.Game.Systems.Combat;

public static class EnemyDeathHelper
{
    public static bool ApplyDamage(
        World world,
        CommandBuffer? cmd,
        ComponentStore<HealthComponent>? healths,
        ComponentStore<PositionComponent>? positions,
        Entity enemy,
        float damage,
        int xpValue = 1,
        List<Entity>? deferredDestroy = null,
        List<(float X, float Y, int Value)>? deferredSpawns = null)
    {
        if (damage <= 0f || healths == null || !world.IsAlive(enemy) || !healths.Has(enemy))
            return false;

        ref HealthComponent hp = ref healths.Get(enemy);
        if (hp.IsDead || hp.DeathHandled)
            return false;

        hp.Current -= damage;
        if (!hp.IsDead)
            return false;

        return HandleDeadEnemy(world, cmd, ref hp, positions, enemy, xpValue, deferredDestroy, deferredSpawns);
    }

    public static bool HandleDeadEnemy(
        World world,
        CommandBuffer? cmd,
        ref HealthComponent hp,
        ComponentStore<PositionComponent>? positions,
        Entity enemy,
        int xpValue = 1,
        List<Entity>? deferredDestroy = null,
        List<(float X, float Y, int Value)>? deferredSpawns = null)
    {
        if (!hp.IsDead || hp.DeathHandled || !world.IsAlive(enemy))
            return false;

        hp.DeathHandled = true;

        int value = Math.Max(1, xpValue);
        if (cmd != null)
        {
            if (positions != null && positions.Has(enemy))
            {
                ref readonly PositionComponent pos = ref positions.Get(enemy);
                float dropX = pos.X;
                float dropY = pos.Y;
                cmd.CreateEntity((w, orb) =>
                {
                    w.AddComponent(orb, new PositionComponent(dropX, dropY));
                    w.AddComponent(orb, new XpOrbComponent(value, false));
                });
            }
            cmd.DestroyEntity(enemy);
        }
        else
        {
            if (positions != null && positions.Has(enemy))
            {
                ref readonly PositionComponent pos = ref positions.Get(enemy);
                deferredSpawns?.Add((pos.X, pos.Y, value));
            }
            deferredDestroy?.Add(enemy);
        }

        return true;
    }
}
