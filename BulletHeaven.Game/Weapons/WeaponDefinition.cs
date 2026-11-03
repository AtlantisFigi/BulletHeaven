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

    public float CritChanceBonus { get; init; }
    public float CritMultiplierBonus { get; init; }
    public float RangeMultiplierBonus { get; init; }
    public float DamageMultiplierBonus { get; init; }
    public float AttackSpeedMultiplierBonus { get; init; }
    public float HealthRegenBonus { get; init; }
    public float MoveSpeedMultiplierBonus { get; init; }

    public int PiercingHits { get; init; } = 1;
    public float LifestealOnCrit { get; init; }
    public int ChainTargets { get; init; }
    public float ChainRange { get; init; }
    public float ChainDamageRatio { get; init; }
    public float LongRangeThreshold { get; init; }
    public float LongRangeDamageMultiplier { get; init; } = 1f;
}
