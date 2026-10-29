namespace BulletHeaven.Game.Components.AI;

public struct DanmakuPatternComponent
{
    public float Angle;
    public float AngularVelocity;
    public float Speed;
    public float Acceleration;

    public DanmakuPatternComponent(float angle, float angularVelocity, float speed, float acceleration = 0f)
    {
        Angle = angle;
        AngularVelocity = angularVelocity;
        Speed = speed;
        Acceleration = acceleration;
    }
}

