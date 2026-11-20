namespace BulletHeaven.Game.Components;

public struct PlayerComponent
{
    public const float CoreHitboxRadius = 6f;
    public const float DefaultGrazeRadius = 34f;
    public const int GrazesPerStack = 8;
    public const int MaxGrazeStacks = 3;

    public float Speed;
    public float HitboxRadius;
    public float FacingAngle;
    public float InvulnerabilityTimer;
    public float FlashTimer;
    public int Level;
    public int Xp;
    public int XpToNextLevel;
    public int PendingLevelUps;
    public int GrazeProgress;
    public int GrazeStacks;
    public float GrazeFlashTimer;
    public float TrailSprintTimer;

    public bool IsInvulnerable => InvulnerabilityTimer > 0f;

    public PlayerComponent() : this(280f, CoreHitboxRadius)
    {
    }

    public PlayerComponent(float speed, float hitboxRadius = CoreHitboxRadius)
    {
        Speed = speed;
        HitboxRadius = hitboxRadius;
        FacingAngle = 0f;
        InvulnerabilityTimer = 0f;
        FlashTimer = 0f;
        Level = 1;
        Xp = 0;
        XpToNextLevel = 10;
        PendingLevelUps = 0;
        GrazeProgress = 0;
        GrazeStacks = 0;
        GrazeFlashTimer = 0f;
        TrailSprintTimer = 0f;
    }
}
