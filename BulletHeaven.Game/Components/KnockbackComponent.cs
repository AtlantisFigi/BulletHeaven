namespace BulletHeaven.Game.Components;

public struct KnockbackComponent
{
    public float VelX;
    public float VelY;
    public float RemainingTime;

    public KnockbackComponent(float velX, float velY, float remainingTime = 0.22f)
    {
        VelX = velX;
        VelY = velY;
        RemainingTime = remainingTime;
    }
}
