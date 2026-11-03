using System;

namespace BulletHeaven.Game.Weapons;

public enum WeaponType
{
    Ranged,
    Melee,
    Passive
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
    Heavy = 1 << 4,
    Berserk = 1 << 5,
    Pierce = 1 << 6,
    Chain = 1 << 7,
    Stationary = 1 << 8,
    Burst = 1 << 9,
    Crit = 1 << 10
}
