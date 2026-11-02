using Raylib_cs;

namespace BulletHeaven.Game.Weapons;

public sealed class WeaponDefinition
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
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
}

