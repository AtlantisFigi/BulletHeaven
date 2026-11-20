namespace BulletHeaven.Game.Components.AI;

public struct SpitterComponent
{
    public float ActionTimer;
    public float ActionInterval;
    public float NormalSpeed;
    public float BulletSpeed;
    public float PreferredDistance;
    public int ProjectileCount;

    public SpitterComponent(
        float normalSpeed = 70f,
        float bulletSpeed = 220f,
        float actionInterval = 2.2f,
        float preferredDistance = 320f,
        int projectileCount = 0)
    {
        ActionInterval = actionInterval;
        ActionTimer = actionInterval;
        NormalSpeed = normalSpeed;
        BulletSpeed = bulletSpeed;
        PreferredDistance = preferredDistance;
        ProjectileCount = projectileCount;
    }
}

