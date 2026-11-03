using System;
using System.Collections.Generic;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Spatial;
using BulletHeaven.Game.Weapons;
using Raylib_cs;

namespace BulletHeaven.Game.Systems.Combat;

public struct LightningArc
{
    public Vector2 From;
    public Vector2 To;
    public float Timer;
    public float MaxTimer;
}

public sealed class WeaponSystem : ISystem
{
    public static readonly List<LightningArc> ActiveArcs = new();

    private World? _world;
    private readonly CommandBuffer _cmd;
    private readonly SpatialGrid? _grid;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<PlayerStatsComponent>? _stats;
    private ComponentStore<EquipmentComponent>? _equipments;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<VelocityComponent>? _velocities;

    public WeaponSystem(CommandBuffer cmd, SpatialGrid? grid = null)
    {
        _cmd = cmd;
        _grid = grid;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _players = world.GetStore<PlayerComponent>();
        _stats = world.GetStore<PlayerStatsComponent>();
        _equipments = world.GetStore<EquipmentComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _healths = world.GetStore<HealthComponent>();
        _velocities = world.GetStore<VelocityComponent>();
    }

    public void Update(float deltaTime)
    {
        for (int a = ActiveArcs.Count - 1; a >= 0; a--)
        {
            var arc = ActiveArcs[a];
            arc.Timer -= deltaTime;
            if (arc.Timer <= 0f)
            {
                ActiveArcs.RemoveAt(a);
            }
            else
            {
                ActiveArcs[a] = arc;
            }
        }

        if (_world == null || _positions == null || _equipments == null) return;

        foreach (Entity playerEntity in _world.Query().With<PositionComponent>().With<EquipmentComponent>().With<PlayerComponent>())
        {
            ref readonly PositionComponent playerPos = ref _positions.Get(playerEntity);
            EquipmentComponent equip = _equipments.Get(playerEntity);

            float rangeMult = 1f;
            float dmgMult = 1f;
            float atkSpdMult = 1f;
            float critChance = 0.05f;
            float critMult = 1.5f;

            if (_stats != null && _stats.Has(playerEntity))
            {
                ref readonly PlayerStatsComponent s = ref _stats.Get(playerEntity);
                rangeMult = s.RangeMultiplier;
                dmgMult = s.DamageMultiplier;
                atkSpdMult = s.AttackSpeedMultiplier;
                critChance = s.CritChance;
                critMult = s.CritMultiplier;
            }

            for (int i = 0; i < equip.Slots.Length; i++)
            {
                var slot = equip.Slots[i];
                if (slot.VisualTimer > 0f)
                {
                    slot.VisualTimer -= deltaTime;
                }

                if (slot.Weapon == null || slot.Weapon.Definition.Type == WeaponType.Passive) continue;

                slot.CooldownTimer -= deltaTime;
                if (slot.CooldownTimer <= 0f)
                {
                    float effectiveRange = slot.Weapon.Range * rangeMult;

                    if (TryFindClosestEnemy(playerPos.X, playerPos.Y, effectiveRange, out Entity targetEnemy, out float enemyX, out float enemyY, out float enemyDist))
                    {
                        float cooldown = MathF.Max(0.08f, slot.Weapon.AttackInterval / MathF.Max(0.1f, atkSpdMult));
                        slot.CooldownTimer = cooldown;

                        float dx = enemyX - playerPos.X;
                        float dy = enemyY - playerPos.Y;
                        float angle = MathF.Atan2(dy, dx);

                        slot.AttackAngle = angle;
                        slot.AttackRange = effectiveRange;
                        slot.VisualTimer = slot.VisualDuration;

                        bool isCrit = Random.Shared.NextSingle() < critChance;
                        float finalDamage = slot.Weapon.Damage * dmgMult * (isCrit ? critMult : 1f);

                        if (slot.Weapon.Definition.LongRangeThreshold > 0f && enemyDist > slot.Weapon.Definition.LongRangeThreshold)
                        {
                            finalDamage *= slot.Weapon.Definition.LongRangeDamageMultiplier;
                        }

                        if (slot.Weapon.Definition.Type == WeaponType.Ranged)
                        {
                            ExecuteRangedAttack(playerPos.X, playerPos.Y, angle, slot.Weapon, finalDamage, isCrit, playerEntity);
                        }
                        else if (slot.Weapon.Definition.Type == WeaponType.Melee)
                        {
                            ExecuteMeleeAttack(playerPos.X, playerPos.Y, angle, slot.Weapon, finalDamage, isCrit, playerEntity, effectiveRange);
                        }
                    }
                }
            }
        }
    }

    private bool TryFindClosestEnemy(float px, float py, float maxRange, out Entity closest, out float targetX, out float targetY, out float distance)
    {
        closest = default;
        targetX = 0f;
        targetY = 0f;
        distance = 0f;

        if (_world == null || _positions == null || _enemies == null) return false;

        float maxRangeSq = maxRange * maxRange;
        float bestDistSq = maxRangeSq;
        bool found = false;

        if (_grid != null)
        {
            CellRange range = _grid.GetRange(px, py, maxRange);
            for (int r = range.MinRow; r <= range.MaxRow; r++)
            {
                for (int c = range.MinCol; c <= range.MaxCol; c++)
                {
                    List<Entity> cell = _grid.GetCell(c, r);
                    for (int i = 0; i < cell.Count; i++)
                    {
                        Entity e = cell[i];
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
                }
            }
            if (found) distance = MathF.Sqrt(bestDistSq);
            return found;
        }

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
                distance = MathF.Sqrt(distSq);
                found = true;
            }
        }

        return found;
    }

    private void ExecuteRangedAttack(float px, float py, float angle, WeaponItem weapon, float damage, bool isCrit, Entity playerEntity)
    {
        float speed = weapon.Definition.ProjectileSpeed;
        float vx = MathF.Cos(angle) * speed;
        float vy = MathF.Sin(angle) * speed;
        float radius = weapon.Definition.ProjectileRadius * (isCrit ? 1.3f : 1f);
        float knockback = weapon.Definition.Knockback;
        var color = isCrit ? Color.White : weapon.Definition.Color;

        int hitsLeft = Math.Max(1, weapon.Definition.PiercingHits);
        float lifesteal = isCrit ? weapon.Definition.LifestealOnCrit : 0f;

        _cmd.CreateEntity((world, entity) =>
        {
            world.AddComponent(entity, new PositionComponent(px, py));
            world.AddComponent(entity, new VelocityComponent(vx, vy));
            world.AddComponent(entity, new ProjectileComponent(damage, 2.5f, radius, knockback, hitsLeft, isCrit, lifesteal, playerEntity));
            world.AddComponent(entity, new RenderColorComponent(color.R, color.G, color.B, color.A, radius));
        });
    }

    private void ExecuteMeleeAttack(float px, float py, float centerAngle, WeaponItem weapon, float damage, bool isCrit, Entity playerEntity, float effectiveRange)
    {
        if (_world == null || _positions == null || _enemies == null) return;

        float rangeSq = effectiveRange * effectiveRange;
        float halfArc = (weapon.Definition.MeleeArcDegrees * 0.5f) * (MathF.PI / 180f);
        bool hasChain = weapon.Definition.ChainTargets > 0;

        Entity hitTarget = default;
        Vector2 hitPos = Vector2.Zero;
        bool hasHitTarget = false;

        void ProcessEnemy(Entity e)
        {
            if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) return;

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
                        hp.Current -= damage;

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

                    if (!hasHitTarget)
                    {
                        hitTarget = e;
                        hitPos = new Vector2(pos.X, pos.Y);
                        hasHitTarget = true;
                    }
                }
            }
        }

        if (_grid != null)
        {
            CellRange range = _grid.GetRange(px, py, effectiveRange);
            for (int r = range.MinRow; r <= range.MaxRow; r++)
            {
                for (int c = range.MinCol; c <= range.MaxCol; c++)
                {
                    List<Entity> cell = _grid.GetCell(c, r);
                    for (int i = 0; i < cell.Count; i++)
                    {
                        ProcessEnemy(cell[i]);
                    }
                }
            }
        }
        else
        {
            foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyComponent>())
            {
                ProcessEnemy(e);
            }
        }

        if (hasChain && hasHitTarget)
        {
            ExecuteChainLightning(hitTarget, hitPos, damage * weapon.Definition.ChainDamageRatio, weapon.Definition.ChainRange, weapon.Definition.ChainTargets);
        }
    }

    private void ExecuteChainLightning(Entity sourceEntity, Vector2 origin, float chainDamage, float chainRange, int maxTargets)
    {
        if (_world == null || _positions == null || _enemies == null) return;

        int chainedCount = 0;
        float chainRangeSq = chainRange * chainRange;

        void ProcessChainCandidate(Entity other)
        {
            if (chainedCount >= maxTargets) return;
            if (other == sourceEntity) return;
            if (_healths != null && _healths.Has(other) && _healths.Get(other).IsDead) return;

            ref readonly PositionComponent otherPos = ref _positions.Get(other);
            float cdx = otherPos.X - origin.X;
            float cdy = otherPos.Y - origin.Y;
            float cdistSq = cdx * cdx + cdy * cdy;

            if (cdistSq <= chainRangeSq)
            {
                if (_healths != null && _healths.Has(other))
                {
                    ref HealthComponent otherHp = ref _healths.Get(other);
                    otherHp.Current -= chainDamage;
                    if (otherHp.IsDead)
                    {
                        _cmd.DestroyEntity(other);
                    }
                }

                ActiveArcs.Add(new LightningArc
                {
                    From = origin,
                    To = new Vector2(otherPos.X, otherPos.Y),
                    Timer = 0.16f,
                    MaxTimer = 0.16f
                });

                chainedCount++;
            }
        }

        if (_grid != null)
        {
            CellRange range = _grid.GetRange(origin.X, origin.Y, chainRange);
            for (int r = range.MinRow; r <= range.MaxRow && chainedCount < maxTargets; r++)
            {
                for (int c = range.MinCol; c <= range.MaxCol && chainedCount < maxTargets; c++)
                {
                    List<Entity> cell = _grid.GetCell(c, r);
                    for (int i = 0; i < cell.Count && chainedCount < maxTargets; i++)
                    {
                        ProcessChainCandidate(cell[i]);
                    }
                }
            }
        }
        else
        {
            foreach (Entity other in _world.Query().With<PositionComponent>().With<EnemyComponent>())
            {
                ProcessChainCandidate(other);
                if (chainedCount >= maxTargets) break;
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
