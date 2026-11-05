using System;
using Raylib_cs;

namespace BulletHeaven.Game.Weapons;

public sealed class EvolutionBranch
{
    public string BranchId { get; init; } = "";
    public string BranchTitle { get; init; } = "";
    public string SummaryText { get; init; } = "";
    public string FlavorText { get; init; } = "";
    public WeaponDefinition Definition { get; init; } = null!;
}

public static class EvolutionDatabase
{
    public static (EvolutionBranch BranchA, EvolutionBranch BranchB) GetEvolutionBranches(WeaponItem item)
    {
        string baseId = item.Definition.Id;
        int nextTier = item.Tier + 1;

        if (baseId.StartsWith("bio_dart"))
        {
            var defA = item.Definition.Clone($"bio_dart_flak_t{nextTier}");
            defA = new WeaponDefinition
            {
                Id = defA.Id,
                Name = $"Calcified Flak Spitter {ToRoman(nextTier)}",
                Description = "Twin piercing bone darts with heavy stopping power.",
                Slot = EquipSlot.Hand,
                Type = WeaponType.Ranged,
                Keywords = WeaponKeywords.Bio | WeaponKeywords.Heavy | WeaponKeywords.Pierce,
                BaseDamage = item.Definition.BaseDamage * 1.8f,
                AttackInterval = item.Definition.AttackInterval,
                Range = item.Definition.Range,
                ProjectileSpeed = item.Definition.ProjectileSpeed,
                ProjectileRadius = 4.5f,
                ProjectileCount = Math.Max(2, item.Definition.ProjectileCount + 1),
                SpreadDegrees = 16f,
                PiercingHits = item.Definition.PiercingHits + 1,
                Knockback = item.Definition.Knockback + 90f,
                Color = new Color(240, 225, 195, 255)
            };

            var branchA = new EvolutionBranch
            {
                BranchId = defA.Id,
                BranchTitle = "CALCIFIED FLAK EVOLUTION",
                SummaryText = "+Twin Projectiles | +80% Damage | +Heavy Knockback",
                FlavorText = "Ossifies firing chambers to launch twin penetrating bone shards.",
                Definition = defA
            };

            var defB = new WeaponDefinition
            {
                Id = $"bio_dart_venom_t{nextTier}",
                Name = $"Neuro-Venom Needler {ToRoman(nextTier)}",
                Description = "Floods needles with neurotoxins for rapid fire and life-draining punctures.",
                Slot = EquipSlot.Hand,
                Type = WeaponType.Ranged,
                Keywords = WeaponKeywords.Bio | WeaponKeywords.Rapid,
                BaseDamage = item.Definition.BaseDamage * 1.4f,
                AttackInterval = Math.Max(0.18f, item.Definition.AttackInterval * 0.65f),
                Range = item.Definition.Range + 60f,
                ProjectileSpeed = item.Definition.ProjectileSpeed + 80f,
                ProjectileRadius = 3.5f,
                ProjectileCount = 1,
                Lifesteal = 0.06f,
                Knockback = item.Definition.Knockback,
                Color = new Color(80, 240, 130, 255)
            };

            var branchB = new EvolutionBranch
            {
                BranchId = defB.Id,
                BranchTitle = "NEURO-VENOM EVOLUTION",
                SummaryText = "+35% Faster Attack Rate | +6% Lifesteal | +Range",
                FlavorText = "Floods pneumatic needles with predatory neurotoxins that siphon vitality.",
                Definition = defB
            };

            return (branchA, branchB);
        }

        if (baseId.StartsWith("bone_blade"))
        {
            var defA = new WeaponDefinition
            {
                Id = $"bone_blade_cleaver_t{nextTier}",
                Name = $"Colossal Osteo-Cleaver {ToRoman(nextTier)}",
                Description = "Elongates into a massive ulna blade that decimates horde fronts.",
                Slot = EquipSlot.Hand,
                Type = WeaponType.Melee,
                Keywords = WeaponKeywords.Bio | WeaponKeywords.Blade | WeaponKeywords.Heavy,
                BaseDamage = item.Definition.BaseDamage * 1.9f,
                AttackInterval = item.Definition.AttackInterval,
                Range = item.Definition.Range + 45f,
                MeleeArcDegrees = 150f,
                Knockback = item.Definition.Knockback + 100f,
                Color = new Color(245, 230, 210, 255)
            };

            var branchA = new EvolutionBranch
            {
                BranchId = defA.Id,
                BranchTitle = "OSTEO-CLEAVER EVOLUTION",
                SummaryText = "+90% Damage | +150deg Wide Cleave | +Crushing Knockback",
                FlavorText = "Elongates into a massive ulna blade that decimates horde fronts.",
                Definition = defA
            };

            var defB = new WeaponDefinition
            {
                Id = $"bone_blade_whip_t{nextTier}",
                Name = $"Serrated Spine-Whip {ToRoman(nextTier)}",
                Description = "Articulates into a flexible vertebral chain delivering surgical critical strikes.",
                Slot = EquipSlot.Hand,
                Type = WeaponType.Melee,
                Keywords = WeaponKeywords.Bio | WeaponKeywords.Blade | WeaponKeywords.Crit,
                BaseDamage = item.Definition.BaseDamage * 1.35f,
                AttackInterval = Math.Max(0.25f, item.Definition.AttackInterval * 0.65f),
                Range = item.Definition.Range + 15f,
                MeleeArcDegrees = 110f,
                Knockback = item.Definition.Knockback,
                CritChanceBonus = 0.15f,
                CritMultiplierBonus = 0.40f,
                Color = new Color(255, 130, 160, 255)
            };

            var branchB = new EvolutionBranch
            {
                BranchId = defB.Id,
                BranchTitle = "SPINE-WHIP EVOLUTION",
                SummaryText = "+35% Flurry Speed | +15% Crit Chance | +0.4x Crit Mult",
                FlavorText = "Articulates into a flexible vertebral chain that delivers surgical critical strikes.",
                Definition = defB
            };

            return (branchA, branchB);
        }

        if (baseId.StartsWith("syringe_harpoon"))
        {
            var defA = new WeaponDefinition
            {
                Id = $"syringe_plague_t{nextTier}",
                Name = $"Plague Cannula {ToRoman(nextTier)}",
                Description = "Infects cannula with necrotic microbes siphoning vitality on crits.",
                Slot = EquipSlot.Hand,
                Type = WeaponType.Ranged,
                Keywords = WeaponKeywords.Bio | WeaponKeywords.Pierce,
                BaseDamage = item.Definition.BaseDamage * 1.7f,
                AttackInterval = item.Definition.AttackInterval,
                Range = item.Definition.Range + 60f,
                ProjectileSpeed = item.Definition.ProjectileSpeed,
                ProjectileRadius = 5f,
                PiercingHits = item.Definition.PiercingHits + 2,
                LifestealOnCrit = item.Definition.LifestealOnCrit + 3f,
                CritChanceBonus = 0.10f,
                Knockback = item.Definition.Knockback,
                Color = new Color(255, 80, 100, 255)
            };

            var branchA = new EvolutionBranch
            {
                BranchId = defA.Id,
                BranchTitle = "PLAGUE HARPOON EVOLUTION",
                SummaryText = "+Pierces 5 Targets | +6 HP Healed on Crit | +70% Damage",
                FlavorText = "Infects cannula with necrotic microbes that siphons vitality from every target.",
                Definition = defA
            };

            var defB = new WeaponDefinition
            {
                Id = $"syringe_taser_t{nextTier}",
                Name = $"Neuro-Taser Cannula {ToRoman(nextTier)}",
                Description = "Charged clinical cannula that discharges chain lightning across pierced victims.",
                Slot = EquipSlot.Hand,
                Type = WeaponType.Ranged,
                Keywords = WeaponKeywords.Bio | WeaponKeywords.Chain,
                BaseDamage = item.Definition.BaseDamage * 1.4f,
                AttackInterval = item.Definition.AttackInterval,
                Range = item.Definition.Range,
                ProjectileSpeed = item.Definition.ProjectileSpeed + 100f,
                ProjectileRadius = 5f,
                PiercingHits = item.Definition.PiercingHits,
                ChainTargets = 4,
                ChainRange = 180f,
                ChainDamageRatio = 0.70f,
                Knockback = item.Definition.Knockback,
                Color = new Color(100, 240, 255, 255)
            };

            var branchB = new EvolutionBranch
            {
                BranchId = defB.Id,
                BranchTitle = "SYNAPTIC TASER EVOLUTION",
                SummaryText = "+Chains Lightning to 4 Nearby Targets | +Arc Range",
                FlavorText = "Equips harpoon head with neuro-capacitors that electrocute clusters.",
                Definition = defB
            };

            return (branchA, branchB);
        }

        var genericA = new WeaponDefinition
        {
            Id = $"{baseId}_apex_t{nextTier}",
            Name = $"{item.Definition.Name} [Apex] {ToRoman(nextTier)}",
            Description = "Reinforces muscular actuators for maximized kinetic impact.",
            Slot = item.Definition.Slot,
            Type = item.Definition.Type,
            Keywords = item.Definition.Keywords | WeaponKeywords.Heavy,
            BaseDamage = item.Definition.BaseDamage * 1.6f,
            AttackInterval = item.Definition.AttackInterval,
            Range = item.Definition.Range,
            ProjectileSpeed = item.Definition.ProjectileSpeed,
            Knockback = item.Definition.Knockback + 60f,
            PiercingHits = item.Definition.PiercingHits,
            Color = item.Definition.Color
        };

        var genBranchA = new EvolutionBranch
        {
            BranchId = genericA.Id,
            BranchTitle = "HEAVY APEX EVOLUTION",
            SummaryText = "+60% Base Damage | +Knockback Force",
            FlavorText = "Reinforces muscular actuators for maximized kinetic impact.",
            Definition = genericA
        };

        var genericB = new WeaponDefinition
        {
            Id = $"{baseId}_synaptic_t{nextTier}",
            Name = $"{item.Definition.Name} [Synaptic] {ToRoman(nextTier)}",
            Description = "Optimizes neural pathways for rapid reflexive discharges.",
            Slot = item.Definition.Slot,
            Type = item.Definition.Type,
            Keywords = item.Definition.Keywords | WeaponKeywords.Rapid,
            BaseDamage = item.Definition.BaseDamage * 1.25f,
            AttackInterval = Math.Max(0.15f, item.Definition.AttackInterval * 0.70f),
            Range = item.Definition.Range + 30f,
            ProjectileSpeed = item.Definition.ProjectileSpeed + 50f,
            CritChanceBonus = item.Definition.CritChanceBonus + 0.10f,
            Color = item.Definition.Color
        };

        var genBranchB = new EvolutionBranch
        {
            BranchId = genericB.Id,
            BranchTitle = "SYNAPTIC RAPID EVOLUTION",
            SummaryText = "+30% Attack Rate | +10% Critical Strike Chance",
            FlavorText = "Optimizes neural pathways for rapid reflexive discharges.",
            Definition = genericB
        };

        return (genBranchA, genBranchB);
    }

    private static string ToRoman(int number)
    {
        return number switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => number.ToString()
        };
    }
}

