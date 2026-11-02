using System;
using Raylib_cs;

namespace BulletHeaven.Game.Weapons;

public sealed class WeaponItem
{
    public WeaponDefinition Definition { get; }
    public int Tier { get; set; }

    public WeaponRarity Rarity => (WeaponRarity)Math.Clamp(Tier - 1, 0, 3);
    public float Damage => Definition.BaseDamage * (1f + (Tier - 1) * 0.45f);
    public float AttackInterval => Definition.AttackInterval * MathF.Pow(0.88f, Tier - 1);
    public float Range => Definition.Range * (1f + (Tier - 1) * 0.08f);

    public Color RarityColor => Tier switch
    {
        1 => new Color(200, 210, 220, 255),
        2 => new Color(75, 160, 255, 255),
        3 => new Color(185, 80, 255, 255),
        _ => new Color(255, 195, 45, 255)
    };

    public WeaponItem(WeaponDefinition definition, int tier = 1)
    {
        Definition = definition;
        Tier = Math.Clamp(tier, 1, 4);
    }
}

