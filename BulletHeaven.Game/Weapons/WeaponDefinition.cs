using Raylib_cs;

namespace BulletHeaven.Game.Weapons;

public sealed class WeaponDefinition
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public EquipSlot Slot { get; init; } = EquipSlot.Hand;
    public WeaponType Type { get; init; }
    public WeaponKeywords Keywords { get; init; }
    public float BaseDamage { get; init; }
    public float AttackInterval { get; init; }
    public float Range { get; init; }
    public float ProjectileSpeed { get; init; }
    public float ProjectileRadius { get; init; } = 4f;
    public float Knockback { get; init; } = 80f;
    public float MeleeArcDegrees { get; init; } = 110f;
    public Color Color { get; init; } = Color.White;

    public int ProjectileCount { get; init; } = 1;
    public float SpreadDegrees { get; init; } = 0f;
    public float Lifesteal { get; init; } = 0f;

    public float CritChanceBonus { get; init; }
    public float CritMultiplierBonus { get; init; }
    public float RangeMultiplierBonus { get; init; }
    public float DamageMultiplierBonus { get; init; }
    public float AttackSpeedMultiplierBonus { get; init; }
    public float HealthRegenBonus { get; init; }
    public float MoveSpeedMultiplierBonus { get; init; }
    public float DodgeChanceBonus { get; init; }

    public int PiercingHits { get; init; } = 1;
    public float LifestealOnCrit { get; init; }
    public int ChainTargets { get; init; }
    public float ChainRange { get; init; }
    public float ChainDamageRatio { get; init; }
    public float LongRangeThreshold { get; init; }
    public float LongRangeDamageMultiplier { get; init; } = 1f;

    public WeaponDefinition Clone(string? newId = null)
    {
        return new WeaponDefinition
        {
            Id = newId ?? Id,
            Name = Name,
            Description = Description,
            Slot = Slot,
            Type = Type,
            Keywords = Keywords,
            BaseDamage = BaseDamage,
            AttackInterval = AttackInterval,
            Range = Range,
            ProjectileSpeed = ProjectileSpeed,
            ProjectileRadius = ProjectileRadius,
            Knockback = Knockback,
            MeleeArcDegrees = MeleeArcDegrees,
            Color = Color,
            ProjectileCount = ProjectileCount,
            SpreadDegrees = SpreadDegrees,
            Lifesteal = Lifesteal,
            CritChanceBonus = CritChanceBonus,
            CritMultiplierBonus = CritMultiplierBonus,
            RangeMultiplierBonus = RangeMultiplierBonus,
            DamageMultiplierBonus = DamageMultiplierBonus,
            AttackSpeedMultiplierBonus = AttackSpeedMultiplierBonus,
            HealthRegenBonus = HealthRegenBonus,
            MoveSpeedMultiplierBonus = MoveSpeedMultiplierBonus,
            DodgeChanceBonus = DodgeChanceBonus,
            PiercingHits = PiercingHits,
            LifestealOnCrit = LifestealOnCrit,
            ChainTargets = ChainTargets,
            ChainRange = ChainRange,
            ChainDamageRatio = ChainDamageRatio,
            LongRangeThreshold = LongRangeThreshold,
            LongRangeDamageMultiplier = LongRangeDamageMultiplier
        };
    }
}
