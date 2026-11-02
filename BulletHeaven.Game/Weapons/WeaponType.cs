using System;

namespace BulletHeaven.Game.Weapons;

public enum WeaponType
{
    Ranged,
    Melee
}

public enum WeaponRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

[Flags]
public enum WeaponKeywords
{
    None = 0,
    Bio = 1 << 0,
    Projectile = 1 << 1,
    Blade = 1 << 2,
    Rapid = 1 << 3,
    Heavy = 1 << 4
}

