using System.Collections.Generic;
using Raylib_cs;

namespace BulletHeaven.Game.Weapons;

public static class WeaponDatabase
{
    private static readonly Dictionary<string, WeaponDefinition> Definitions = new()
    {
        ["bone_blade"] = new WeaponDefinition
        {
            Id = "bone_blade",
            Name = "Bone Scythe",
            Description = "Cleaves in a devastating 120-degree arc, knocking back swarms and slicing through tissue.",
            Type = WeaponType.Melee,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Blade,
            BaseDamage = 28f,
            AttackInterval = 1.1f,
            Range = 140f,
            MeleeArcDegrees = 120f,
            Knockback = 180f,
            Color = new Color(245, 230, 210, 255)
        },
        ["bio_dart"] = new WeaponDefinition
        {
            Id = "bio_dart",
            Name = "Bio-Dart Spitter",
            Description = "Rapidly fires compressed calcified needles at nearby foes with high kinetic penetration.",
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
        ["hardened_chitin"] = new WeaponDefinition
        {
            Id = "hardened_chitin",
            Name = "Hardened Chitin",
            Description = "Calcified subcutaneous plating that reinforces vitality and deflects incoming kinetic impacts.",
            Type = WeaponType.Passive,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Heavy,
            MaxHpBonus = 10f,
            ArmorBonus = 1f,
            Color = new Color(180, 195, 215, 255)
        },
        ["adrenal_gland"] = new WeaponDefinition
        {
            Id = "adrenal_gland",
            Name = "Adrenal Gland",
            Description = "Pulsating neuro-endocrine organ that floods reflexes with predatory stimulants.",
            Type = WeaponType.Passive,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Rapid,
            AttackSpeedMultiplierBonus = 0.15f,
            MoveSpeedMultiplierBonus = 0.08f,
            Color = new Color(255, 90, 110, 255)
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
