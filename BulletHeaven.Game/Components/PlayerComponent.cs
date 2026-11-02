namespace BulletHeaven.Game.Components;

public struct PlayerComponent
{
    public float Speed;
    public float FacingAngle;

    public PlayerComponent(float speed)
    {
        Speed = speed;
        FacingAngle = 0f;
    }
}

