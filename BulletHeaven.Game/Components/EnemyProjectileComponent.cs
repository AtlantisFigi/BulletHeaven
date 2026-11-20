namespace BulletHeaven.Game.Components;

public struct EnemyProjectileComponent
{
    public float Damage;
    public float Radius;
    public float Lifetime;
    public bool HasBeenGrazed;

    public EnemyProjectileComponent(float damage = 8f, float radius = 6f, float lifetime = 4.5f)
    {
        Damage = damage;
        Radius = radius;
        Lifetime = lifetime;
        HasBeenGrazed = false;
    }
}
