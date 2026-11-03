using BulletHeaven.ECS.Core;

namespace BulletHeaven.Game.Components;

public struct ProjectileComponent
{
    public float Damage;
    public float Lifetime;
    public float Radius;
    public float Knockback;
    public int HitsLeft;
    public bool IsCrit;
    public float Lifesteal;
    public Entity SourcePlayer;

    public ProjectileComponent(float damage, float lifetime, float radius = 4f, float knockback = 40f, int hitsLeft = 1, bool isCrit = false, float lifesteal = 0f, Entity sourcePlayer = default)
    {
        Damage = damage;
        Lifetime = lifetime;
        Radius = radius;
        Knockback = knockback;
        HitsLeft = hitsLeft;
        IsCrit = isCrit;
        Lifesteal = lifesteal;
        SourcePlayer = sourcePlayer;
    }
}
