using Raylib_cs;

namespace BulletHeaven.Game.Weapons;

public sealed class WeaponDefinition
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public WeaponType Type { get; init; }
    public float BaseDamage { get; init; }
    public float AttackInterval { get; init; }
    public float Range { get; init; }
    public float ProjectileSpeed { get; init; }
    public float ProjectileRadius { get; init; } = 4f;
    public float ProjectileLifetime { get; init; } = 2.5f;
    public float Knockback { get; init; } = 80f;
    public float KnockbackTime { get; init; } = 0.15f;
    public float MeleeArcDegrees { get; init; } = 110f;
    public Color Color { get; init; } = Color.White;

    public int ProjectileCount { get; init; } = 1;
    public float SpreadDegrees { get; init; } = 0f;
    public int PiercingHits { get; init; } = 1;

    public int OrbitCount { get; init; } = 0;
    public float OrbitRadius { get; init; } = 0f;
    public float DeadzoneRadius { get; init; } = 0f;
    public float OrbitSpeed { get; init; } = 0f;

    public float HazardRadius { get; init; } = 0f;
    public float HazardDuration { get; init; } = 0f;
    public float HazardTickInterval { get; init; } = 0.30f;

    public WeaponDefinition Clone(string? newId = null)
    {
        return new WeaponDefinition
        {
            Id = newId ?? Id,
            Name = Name,
            Description = Description,
            Type = Type,
            BaseDamage = BaseDamage,
            AttackInterval = AttackInterval,
            Range = Range,
            ProjectileSpeed = ProjectileSpeed,
            ProjectileRadius = ProjectileRadius,
            ProjectileLifetime = ProjectileLifetime,
            Knockback = Knockback,
            KnockbackTime = KnockbackTime,
            MeleeArcDegrees = MeleeArcDegrees,
            Color = Color,
            ProjectileCount = ProjectileCount,
            SpreadDegrees = SpreadDegrees,
            PiercingHits = PiercingHits,
            OrbitCount = OrbitCount,
            OrbitRadius = OrbitRadius,
            DeadzoneRadius = DeadzoneRadius,
            OrbitSpeed = OrbitSpeed,
            HazardRadius = HazardRadius,
            HazardDuration = HazardDuration,
            HazardTickInterval = HazardTickInterval
        };
    }
}
