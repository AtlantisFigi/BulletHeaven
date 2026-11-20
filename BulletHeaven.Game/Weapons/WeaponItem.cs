using System;

namespace BulletHeaven.Game.Weapons;

public sealed class WeaponItem
{
    public const int MaxLevel = 5;

    public WeaponDefinition Definition { get; }
    public int Level { get; private set; }

    public float Damage => Definition.BaseDamage * (1f + (Level - 1) * 0.25f);
    public float AttackInterval => Definition.AttackInterval * MathF.Pow(0.85f, Level - 1);

    public float Range => Definition.Type == WeaponType.OuterRing
        ? Definition.Range
        : Definition.Range * (1f + (Level - 1) * 0.08f);

    public int OrbitCount => Definition.OrbitCount > 0 ? Definition.OrbitCount + (Level - 1) : 0;

    public float HazardRadius => Definition.HazardRadius > 0f
        ? Definition.HazardRadius * (1f + (Level - 1) * 0.10f)
        : 0f;

    public int BonusProjectiles => (Level - 1) / 2;

    public bool CanUpgrade => Level < MaxLevel;

    public WeaponItem(WeaponDefinition definition, int level = 1)
    {
        Definition = definition;
        Level = Math.Clamp(level, 1, MaxLevel);
    }

    public bool ApplyUpgrade()
    {
        if (Level >= MaxLevel) return false;
        Level++;
        return true;
    }
}
