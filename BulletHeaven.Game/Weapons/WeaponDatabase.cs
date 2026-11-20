using System.Collections.Generic;
using Raylib_cs;

namespace BulletHeaven.Game.Weapons;

public static class WeaponDatabase
{
    private static readonly WeaponDefinition[] OrderedDefinitions =
    {
        new WeaponDefinition
        {
            Id = "forward_cone",
            Name = "Cone",
            Description = "Slashes in a wide 110-degree cone ahead of your movement direction with heavy knockback.",
            Type = WeaponType.ForwardCone,
            BaseDamage = 32f,
            AttackInterval = 0.85f,
            Range = 145f,
            MeleeArcDegrees = 110f,
            Knockback = 950f,
            KnockbackTime = 0.32f,
            Color = new Color(255, 225, 130, 255)
        },
        new WeaponDefinition
        {
            Id = "piercing_line",
            Name = "Sniper",
            Description = "Fires a high-velocity cyan beam toward the furthest enemy within 520px, piercing all targets.",
            Type = WeaponType.PiercingLine,
            BaseDamage = 24f,
            AttackInterval = 1.10f,
            Range = 520f,
            ProjectileSpeed = 880f,
            ProjectileRadius = 6f,
            ProjectileLifetime = 1.1f,
            PiercingHits = 99,
            Knockback = 40f,
            KnockbackTime = 0.12f,
            Color = new Color(90, 235, 255, 255)
        },
        new WeaponDefinition
        {
            Id = "outer_ring",
            Name = "Orbits",
            Description = "Orbs circle at a fixed 155px perimeter, shredding distant enemies while leaving a <125px inner deadzone.",
            Type = WeaponType.OuterRing,
            BaseDamage = 18f,
            AttackInterval = 0.35f,
            Range = 155f,
            ProjectileRadius = 10f,
            OrbitCount = 3,
            OrbitRadius = 155f,
            DeadzoneRadius = 125f,
            OrbitSpeed = 2.8f,
            Knockback = 60f,
            KnockbackTime = 0.12f,
            Color = new Color(110, 235, 255, 255)
        },
        new WeaponDefinition
        {
            Id = "movement_trail",
            Name = "Trail",
            Description = "Leaves toxic hazard pools behind you while moving, damaging enemies that cross your trail.",
            Type = WeaponType.MovementTrail,
            BaseDamage = 12f,
            AttackInterval = 0.22f,
            Range = 28f,
            HazardRadius = 28f,
            HazardDuration = 2.5f,
            HazardTickInterval = 0.30f,
            Knockback = 0f,
            KnockbackTime = 0f,
            Color = new Color(140, 240, 85, 255)
        },
        new WeaponDefinition
        {
            Id = "returning_boomerang",
            Name = "Boomerang",
            Description = "Hurls a spinning boomerang at the closest enemy up to 270px, then returns to your current position.",
            Type = WeaponType.ReturningBoomerang,
            BaseDamage = 20f,
            AttackInterval = 1.20f,
            Range = 270f,
            ProjectileSpeed = 440f,
            ProjectileRadius = 10f,
            ProjectileLifetime = 5.0f,
            PiercingHits = 999,
            Knockback = 50f,
            KnockbackTime = 0.12f,
            Color = new Color(255, 205, 70, 255)
        },
        new WeaponDefinition
        {
            Id = "sideways_broadside",
            Name = "Twin Shots",
            Description = "Fires twin flank bolts simultaneously 90 degrees left and right of your facing direction.",
            Type = WeaponType.SidewaysBroadside,
            BaseDamage = 16f,
            AttackInterval = 0.70f,
            Range = 380f,
            ProjectileCount = 2,
            ProjectileSpeed = 540f,
            ProjectileRadius = 5f,
            ProjectileLifetime = 0.70f,
            PiercingHits = 1,
            Knockback = 180f,
            KnockbackTime = 0.15f,
            Color = new Color(255, 165, 55, 255)
        }
    };

    private static readonly Dictionary<string, WeaponDefinition> Definitions = BuildDictionary();

    private static Dictionary<string, WeaponDefinition> BuildDictionary()
    {
        var dict = new Dictionary<string, WeaponDefinition>(OrderedDefinitions.Length);
        for (int i = 0; i < OrderedDefinitions.Length; i++)
        {
            var def = OrderedDefinitions[i];
            dict[def.Id] = def;
        }
        return dict;
    }

    public static WeaponDefinition? GetDefinition(string id)
    {
        return Definitions.TryGetValue(id, out var def) ? def : null;
    }

    public static WeaponDefinition? Get(string id) => GetDefinition(id);

    public static WeaponDefinition? GetByIndex(int index)
    {
        if (index < 0 || index >= OrderedDefinitions.Length) return null;
        return OrderedDefinitions[index];
    }

    public static WeaponItem? CreateItem(string id, int level = 1)
    {
        var def = GetDefinition(id);
        return def != null ? new WeaponItem(def, level) : null;
    }

    public static IReadOnlyList<WeaponDefinition> GetAll() => OrderedDefinitions;
}
