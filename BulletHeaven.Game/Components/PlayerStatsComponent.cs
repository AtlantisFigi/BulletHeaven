namespace BulletHeaven.Game.Components;

public struct PlayerStatsComponent
{
    public float CritChance;
    public float CritMultiplier;
    public float RangeMultiplier;
    public float DamageMultiplier;
    public float AttackSpeedMultiplier;
    public float HealthRegen;
    public float MoveSpeedMultiplier;
    public float StationaryTimer;
    public bool IsStationary;
    public float BurstTimer;
    public float BurstActiveTimer;
    public bool IsBursting;

    public PlayerStatsComponent()
    {
        CritChance = 0.05f;
        CritMultiplier = 1.5f;
        RangeMultiplier = 1f;
        DamageMultiplier = 1f;
        AttackSpeedMultiplier = 1f;
        HealthRegen = 0f;
        MoveSpeedMultiplier = 1f;
        StationaryTimer = 0f;
        IsStationary = false;
        BurstTimer = 0f;
        BurstActiveTimer = 0f;
        IsBursting = false;
    }
}

