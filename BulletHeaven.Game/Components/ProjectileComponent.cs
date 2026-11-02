namespace BulletHeaven.Game.Components;

public struct ProjectileComponent
{
    public float Damage;
    public float Lifetime;
    public float Radius;
    public float Knockback;
    public int HitsLeft;

    public ProjectileComponent(float damage, float lifetime, float radius = 4f, float knockback = 40f, int hitsLeft = 1)
    {
        Damage = damage;
        Lifetime = lifetime;
        Radius = radius;
        Knockback = knockback;
        HitsLeft = hitsLeft;
    }
}

