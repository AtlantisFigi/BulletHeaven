namespace BulletHeaven.Game.Components.AI;

public struct SpitterComponent
{
    public float ActionTimer;
    public float ActionInterval;
    public float NormalSpeed;
    public float BulletSpeed;
    public float PreferredDistance;

    public SpitterComponent(float normalSpeed = 65f, float bulletSpeed = 210f, float actionInterval = 1.8f, float preferredDistance = 290f)
    {
        ActionInterval = actionInterval;
        ActionTimer = actionInterval;
        NormalSpeed = normalSpeed;
        BulletSpeed = bulletSpeed;
        PreferredDistance = preferredDistance;
    }
}

