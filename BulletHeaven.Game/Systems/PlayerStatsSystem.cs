using System;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Weapons;

namespace BulletHeaven.Game.Systems;

public sealed class PlayerStatsSystem : ISystem
{
    private World? _world;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<PlayerStatsComponent>? _stats;
    private ComponentStore<EquipmentComponent>? _equipments;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<VelocityComponent>? _velocities;

    public void Initialize(World world)
    {
        _world = world;
        _players = world.GetStore<PlayerComponent>();
        _stats = world.GetStore<PlayerStatsComponent>();
        _equipments = world.GetStore<EquipmentComponent>();
        _healths = world.GetStore<HealthComponent>();
        _velocities = world.GetStore<VelocityComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _players == null || _stats == null || _equipments == null) return;

        foreach (Entity entity in _world.Query().With<PlayerComponent>().With<PlayerStatsComponent>().With<EquipmentComponent>())
        {
            ref PlayerComponent player = ref _players.Get(entity);
            ref PlayerStatsComponent stats = ref _stats.Get(entity);
            ref readonly EquipmentComponent equip = ref _equipments.Get(entity);

            stats.CritChance = 0.05f;
            stats.CritMultiplier = 1.5f;
            stats.RangeMultiplier = 1f;
            stats.DamageMultiplier = 1f;
            stats.AttackSpeedMultiplier = 1f;
            stats.HealthRegen = 0f;
            stats.MoveSpeedMultiplier = 1f;
            stats.DodgeChance = 0f;

            if (_velocities != null && _velocities.Has(entity))
            {
                ref readonly VelocityComponent vel = ref _velocities.Get(entity);
                float speedSq = vel.X * vel.X + vel.Y * vel.Y;
                if (speedSq < 1f)
                {
                    stats.StationaryTimer += deltaTime;
                    if (stats.StationaryTimer >= 0.5f)
                    {
                        stats.IsStationary = true;
                    }
                }
                else
                {
                    stats.StationaryTimer = 0f;
                    stats.IsStationary = false;
                }
            }
            else
            {
                stats.StationaryTimer = 0f;
                stats.IsStationary = false;
            }

            bool hasBerserk = false;
            bool hasRootedSpikes = false;
            bool hasBurstBoots = false;

            if (equip.Slots != null)
            {
                for (int i = 0; i < equip.Slots.Length; i++)
                {
                    var weapon = equip.Slots[i].Weapon;
                    if (weapon == null) continue;

                    var def = weapon.Definition;

                    stats.CritChance += def.CritChanceBonus;
                    stats.CritMultiplier += def.CritMultiplierBonus;
                    stats.RangeMultiplier += def.RangeMultiplierBonus;
                    stats.DamageMultiplier += def.DamageMultiplierBonus;
                    stats.AttackSpeedMultiplier += def.AttackSpeedMultiplierBonus;
                    stats.HealthRegen += def.HealthRegenBonus;
                    stats.MoveSpeedMultiplier += def.MoveSpeedMultiplierBonus;
                    stats.DodgeChance += def.DodgeChanceBonus;

                    if ((def.Keywords & WeaponKeywords.Berserk) != 0)
                    {
                        hasBerserk = true;
                    }

                    if ((def.Keywords & WeaponKeywords.Stationary) != 0)
                    {
                        hasRootedSpikes = true;
                    }

                    if ((def.Keywords & WeaponKeywords.Burst) != 0)
                    {
                        hasBurstBoots = true;
                    }
                }
            }

            stats.DodgeChance = Math.Clamp(stats.DodgeChance, 0f, 0.60f);

            if (hasBerserk && _healths != null && _healths.Has(entity))
            {
                ref readonly HealthComponent hp = ref _healths.Get(entity);
                float missingFraction = MathF.Max(0f, 1f - (hp.Current / MathF.Max(1f, hp.Max)));
                stats.AttackSpeedMultiplier += missingFraction * 0.85f;
            }

            if (hasRootedSpikes && stats.IsStationary)
            {
                stats.DamageMultiplier += 0.40f;
                stats.HealthRegen += 4.0f;
            }

            if (hasBurstBoots)
            {
                if (stats.IsBursting)
                {
                    stats.BurstActiveTimer -= deltaTime;
                    stats.MoveSpeedMultiplier = 2.0f;
                    if (stats.BurstActiveTimer <= 0f)
                    {
                        stats.IsBursting = false;
                        stats.BurstTimer = 4.0f;
                    }
                }
                else
                {
                    stats.BurstTimer -= deltaTime;
                    if (stats.BurstTimer <= 0f)
                    {
                        stats.IsBursting = true;
                        stats.BurstActiveTimer = 0.4f;
                        stats.MoveSpeedMultiplier = 2.0f;
                    }
                }
            }
            else
            {
                stats.IsBursting = false;
                stats.BurstTimer = 0f;
                stats.BurstActiveTimer = 0f;
            }

            if (stats.HealthRegen > 0f && _healths != null && _healths.Has(entity))
            {
                ref HealthComponent hp = ref _healths.Get(entity);
                if (!hp.IsDead)
                {
                    hp.Current = MathF.Min(hp.Max, hp.Current + stats.HealthRegen * deltaTime);
                }
            }

            player.Speed = 280f * stats.MoveSpeedMultiplier;
        }
    }
}
