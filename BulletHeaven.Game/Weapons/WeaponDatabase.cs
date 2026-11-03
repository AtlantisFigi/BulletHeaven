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
            Description = "Rapidly fires compressed calcified needles at nearby foes with high kinetic penetration.",
            Slot = EquipSlot.Hand,
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
            Description = "Cleaves in a devastating 120-degree arc, knocking back swarms and destroying small enemy projectiles.",
            Slot = EquipSlot.Hand,
            Type = WeaponType.Melee,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Blade,
            BaseDamage = 28f,
            AttackInterval = 1.1f,
            Range = 140f,
            MeleeArcDegrees = 120f,
            Knockback = 180f,
            Color = new Color(245, 230, 210, 255)
        },
        ["neuro_sensor"] = new WeaponDefinition
        {
            Id = "neuro_sensor",
            Name = "Neuro Sensor",
            Description = "An awakened ocular pod that emits long-range psionic probe beams with heightened critical precision.",
            Slot = EquipSlot.Head,
            Type = WeaponType.Ranged,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Rapid,
            BaseDamage = 8f,
            AttackInterval = 0.45f,
            Range = 420f,
            ProjectileSpeed = 580f,
            ProjectileRadius = 3f,
            Knockback = 20f,
            Color = new Color(200, 100, 255, 255)
        },
        ["acid_carapace"] = new WeaponDefinition
        {
            Id = "acid_carapace",
            Name = "Acid Gland",
            Description = "Pulsating visceral gland that periodically vents a 360-degree caustic burst, melting adjacent attackers.",
            Slot = EquipSlot.Chest,
            Type = WeaponType.Melee,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Heavy,
            BaseDamage = 35f,
            AttackInterval = 1.5f,
            Range = 160f,
            MeleeArcDegrees = 360f,
            Knockback = 120f,
            Color = new Color(130, 255, 80, 255)
        },
        ["tendon_whip"] = new WeaponDefinition
        {
            Id = "tendon_whip",
            Name = "Tendon Whip",
            Description = "Braided bio-sinew that lashes backward in a fluid sweep, staggering pursuers behind your stride.",
            Slot = EquipSlot.Legs,
            Type = WeaponType.Melee,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Blade,
            BaseDamage = 22f,
            AttackInterval = 0.9f,
            Range = 170f,
            MeleeArcDegrees = 140f,
            Knockback = 160f,
            Color = new Color(255, 120, 140, 255)
        },
        ["spore_spurs"] = new WeaponDefinition
        {
            Id = "spore_spurs",
            Name = "Spore Spurs",
            Description = "Calcified heel spurs that release clouds of toxic spore clusters behind you as you navigate the arena.",
            Slot = EquipSlot.Legs,
            Type = WeaponType.Ranged,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Projectile,
            BaseDamage = 15f,
            AttackInterval = 1.2f,
            Range = 240f,
            ProjectileSpeed = 380f,
            ProjectileRadius = 5f,
            Knockback = 70f,
            Color = new Color(255, 200, 60, 255)
        },
        ["trepanned_eye"] = new WeaponDefinition
        {
            Id = "trepanned_eye",
            Name = "Trepanned Third Eye",
            Description = "Surgically exposed pineal gland. Grants +20% Crit Chance and +30% Attack Range across all mutations.",
            Slot = EquipSlot.Head,
            Type = WeaponType.Passive,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Crit,
            BaseDamage = 0f,
            AttackInterval = 0f,
            Range = 0f,
            ProjectileSpeed = 0f,
            ProjectileRadius = 0f,
            Knockback = 0f,
            Color = new Color(160, 220, 255, 255)
        },
        ["echo_antennae"] = new WeaponDefinition
        {
            Id = "echo_antennae",
            Name = "Echo-Locating Antennae",
            Description = "Vibrating sensory stalks. Grants +40% Range and +50% bonus damage against long-range foes.",
            Slot = EquipSlot.Head,
            Type = WeaponType.Passive,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Rapid,
            BaseDamage = 0f,
            AttackInterval = 0f,
            Range = 0f,
            ProjectileSpeed = 0f,
            ProjectileRadius = 0f,
            Knockback = 0f,
            Color = new Color(220, 140, 255, 255)
        },
        ["adrenaline_overcharger"] = new WeaponDefinition
        {
            Id = "adrenaline_overcharger",
            Name = "Adrenaline Overcharger",
            Description = "Hypertrophic auxiliary heart pump. [Berserk]: Attack speed scales aggressively as remaining HP drops.",
            Slot = EquipSlot.Chest,
            Type = WeaponType.Passive,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Berserk,
            BaseDamage = 0f,
            AttackInterval = 0f,
            Range = 0f,
            Knockback = 0f,
            Color = new Color(255, 60, 80, 255)
        },
        ["syringe_harpoon"] = new WeaponDefinition
        {
            Id = "syringe_harpoon",
            Name = "Syringe Harpoon",
            Description = "Hollow needle tethered by vascular sinew. Pierces up to 3 enemies and drains HP on Critical Strikes.",
            Slot = EquipSlot.Hand,
            Type = WeaponType.Ranged,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Projectile | WeaponKeywords.Pierce,
            BaseDamage = 22f,
            AttackInterval = 1.0f,
            Range = 420f,
            ProjectileSpeed = 700f,
            ProjectileRadius = 5f,
            Knockback = 80f,
            Color = new Color(255, 120, 80, 255)
        },
        ["synaptic_arc_claw"] = new WeaponDefinition
        {
            Id = "synaptic_arc_claw",
            Name = "Synaptic Arc Claw",
            Description = "Neuro-electric raptor talons. Slashing targets discharges synaptic lightning arcing to 2 nearby enemies.",
            Slot = EquipSlot.Hand,
            Type = WeaponType.Melee,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Blade | WeaponKeywords.Chain,
            BaseDamage = 24f,
            AttackInterval = 0.75f,
            Range = 135f,
            MeleeArcDegrees = 120f,
            Knockback = 140f,
            Color = new Color(80, 220, 255, 255)
        },
        ["rooted_osteo_spikes"] = new WeaponDefinition
        {
            Id = "rooted_osteo_spikes",
            Name = "Rooted Osteo-Spikes",
            Description = "Anchoring calcified quills. Standing still grants +40% Damage and +4 HP/s tissue regeneration.",
            Slot = EquipSlot.Legs,
            Type = WeaponType.Passive,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Stationary,
            BaseDamage = 0f,
            AttackInterval = 0f,
            Range = 0f,
            Knockback = 0f,
            Color = new Color(240, 220, 180, 255)
        },
        ["piston_burst_boots"] = new WeaponDefinition
        {
            Id = "piston_burst_boots",
            Name = "Piston Burst Boots",
            Description = "Pressurized bio-pneumatic calf struts. Triggers a high-speed propulsion dash every 4 seconds.",
            Slot = EquipSlot.Legs,
            Type = WeaponType.Passive,
            Keywords = WeaponKeywords.Bio | WeaponKeywords.Burst,
            BaseDamage = 0f,
            AttackInterval = 0f,
            Range = 0f,
            ProjectileSpeed = 0f,
            ProjectileRadius = 0f,
            Knockback = 0f,
            Color = new Color(120, 255, 220, 255)
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
