namespace BulletHeaven.Game.Components;

public struct PlayerComponent
{
    public float Speed;
    public float FacingAngle;
    public float InvulnerabilityTimer;
    public float FlashTimer;

    public bool IsInvulnerable => InvulnerabilityTimer > 0f;

    public PlayerComponent(float speed)
    {
        Speed = speed;
        FacingAngle = 0f;
        InvulnerabilityTimer = 0f;
        FlashTimer = 0f;
    }
}
