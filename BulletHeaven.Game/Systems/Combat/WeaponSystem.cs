using System;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Weapons;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class WeaponSystem : ISystem
{
    private World? _world;
    private readonly CommandBuffer _cmd;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<EquipmentComponent>? _equipments;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<VelocityComponent>? _velocities;

    public WeaponSystem(CommandBuffer cmd)
    {
        _cmd = cmd;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _players = world.GetStore<PlayerComponent>();
        _equipments = world.GetStore<EquipmentComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _healths = world.GetStore<HealthComponent>();
        _velocities = world.GetStore<VelocityComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _equipments == null) return;

        foreach (Entity playerEntity in _world.Query().With<PositionComponent>().With<EquipmentComponent>().With<PlayerComponent>())
        {
            ref readonly PositionComponent playerPos = ref _positions.Get(playerEntity);
            EquipmentComponent equip = _equipments.Get(playerEntity);

            for (int i = 0; i < equip.Slots.Length; i++)
            {
                var slot = equip.Slots[i];
                if (slot.VisualTimer > 0f)
                {
                    slot.VisualTimer -= deltaTime;
                }

                if (slot.Weapon == null) continue;

                slot.CooldownTimer -= deltaTime;
                if (slot.CooldownTimer <= 0f)
                {
                    if (TryFindClosestEnemy(playerPos.X, playerPos.Y, slot.Weapon.Range, out Entity targetEnemy, out float enemyX, out float enemyY))
                    {
                        slot.CooldownTimer = slot.Weapon.AttackInterval;

                        float dx = enemyX - playerPos.X;
                        float dy = enemyY - playerPos.Y;
                        float angle = MathF.Atan2(dy, dx);

                        slot.AttackAngle = angle;
                        slot.AttackRange = slot.Weapon.Range;
                        slot.VisualTimer = slot.VisualDuration;

                        if (slot.Weapon.Definition.Type == WeaponType.Ranged)
                        {
                            ExecuteRangedAttack(playerPos.X, playerPos.Y, angle, slot.Weapon);
                        }
                        else if (slot.Weapon.Definition.Type == WeaponType.Melee)
                        {
                            ExecuteMeleeAttack(playerPos.X, playerPos.Y, angle, slot.Weapon);
                        }
                    }
                }
            }
        }
    }

    private bool TryFindClosestEnemy(float px, float py, float maxRange, out Entity closest, out float targetX, out float targetY)
    {
        closest = default;
        targetX = 0f;
        targetY = 0f;

        if (_world == null || _positions == null || _enemies == null) return false;

        float maxRangeSq = maxRange * maxRange;
        float bestDistSq = maxRangeSq;
        bool found = false;

        foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyComponent>())
        {
            if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) continue;

            ref readonly PositionComponent pos = ref _positions.Get(e);
            float dx = pos.X - px;
            float dy = pos.Y - py;
            float distSq = dx * dx + dy * dy;

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                closest = e;
                targetX = pos.X;
                targetY = pos.Y;
                found = true;
            }
        }

        return found;
    }

    private void ExecuteRangedAttack(float px, float py, float angle, WeaponItem weapon)
    {
        float speed = weapon.Definition.ProjectileSpeed;
        float vx = MathF.Cos(angle) * speed;
        float vy = MathF.Sin(angle) * speed;
        float damage = weapon.Damage;
        float radius = weapon.Definition.ProjectileRadius;
        float knockback = weapon.Definition.Knockback;
        var color = weapon.Definition.Color;

        _cmd.CreateEntity((world, entity) =>
        {
            world.AddComponent(entity, new PositionComponent(px, py));
            world.AddComponent(entity, new VelocityComponent(vx, vy));
            world.AddComponent(entity, new ProjectileComponent(damage, 2.5f, radius, knockback, 1));
            world.AddComponent(entity, new RenderColorComponent(color.R, color.G, color.B, color.A, radius));
        });
    }

    private void ExecuteMeleeAttack(float px, float py, float centerAngle, WeaponItem weapon)
    {
        if (_world == null || _positions == null || _enemies == null) return;

        float rangeSq = weapon.Range * weapon.Range;
        float halfArc = (weapon.Definition.MeleeArcDegrees * 0.5f) * (MathF.PI / 180f);

        foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyComponent>())
        {
            if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) continue;

            ref readonly PositionComponent pos = ref _positions.Get(e);
            float dx = pos.X - px;
            float dy = pos.Y - py;
            float distSq = dx * dx + dy * dy;

            if (distSq <= rangeSq)
            {
                float angleToEnemy = MathF.Atan2(dy, dx);
                float angleDiff = MathF.Abs(NormalizeAngle(angleToEnemy - centerAngle));

                if (angleDiff <= halfArc)
                {
                    if (_healths != null && _healths.Has(e))
                    {
                        ref HealthComponent hp = ref _healths.Get(e);
                        hp.Current -= weapon.Damage;

                        if (hp.IsDead)
                        {
                            _cmd.DestroyEntity(e);
                        }
                    }

                    if (_velocities != null && _velocities.Has(e))
                    {
                        ref VelocityComponent vel = ref _velocities.Get(e);
                        vel.X += MathF.Cos(angleToEnemy) * weapon.Definition.Knockback;
                        vel.Y += MathF.Sin(angleToEnemy) * weapon.Definition.Knockback;
                    }
                }
            }
        }
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.PI * 2f;
        while (angle < -MathF.PI) angle += MathF.PI * 2f;
        return angle;
    }
}

