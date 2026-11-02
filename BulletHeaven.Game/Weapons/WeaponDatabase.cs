using System.Collections.Generic;
using Raylib_cs;

namespace BulletHeaven.Game.Weapons;

public static class WeaponDatabase
{
    private static readonly Dictionary<string, WeaponDefinition> Definitions = new()
    {
        ["bio_dart"] = new WeaponDefinition
        {
            Id = "bio_dart",
            Name = "Bio-Dart Spitter",
            Type = WeaponType.Ranged,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Projectile | WeaponKeywords.Rapid,
            BaseDamage = 12f,
            AttackInterval = 0.65f,
            Range = 380f,
            ProjectileSpeed = 520f,
            ProjectileRadius = 4f,
            Knockback = 40f,
            Color = new Color(80, 240, 180, 255)
        },
        ["bone_blade"] = new WeaponDefinition
        {
            Id = "bone_blade",
            Name = "Bone Scythe",
            Type = WeaponType.Melee,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Blade,
            BaseDamage = 28f,
            AttackInterval = 1.1f,
            Range = 140f,
            MeleeArcDegrees = 120f,
            Knockback = 180f,
            Color = new Color(245, 230, 210, 255)
        }
    };

    public static WeaponDefinition? Get(string id)
    {
        return Definitions.TryGetValue(id, out var def) ? def : null;
    }

    public static WeaponItem? CreateItem(string id, int tier = 1)
    {
        var def = Get(id);
        return def != null ? new WeaponItem(def, tier) : null;
    }

    public static IEnumerable<WeaponDefinition> GetAll() => Definitions.Values;
}

