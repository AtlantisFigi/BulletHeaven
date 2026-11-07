namespace BulletHeaven.Game.Components;

public struct PlayerStatsComponent
{
    public float BonusMaxHp;
    public float BonusArmor;
    public float BonusDamageMultiplier;
    public float BonusAttackSpeedMultiplier;
    public float BonusCritChance;
    public float BonusHealthRegen;
    public float BonusMoveSpeedMultiplier;
    public float BonusDodgeChance;
    public float BonusRangeMultiplier;

    public float Armor;
    public float CritChance;
    public float CritMultiplier;
    public float RangeMultiplier;
    public float DamageMultiplier;
    public float AttackSpeedMultiplier;
    public float HealthRegen;
    public float MoveSpeedMultiplier;
    public float DodgeChance;

    public float StationaryTimer;
    public bool IsStationary;
    public float BurstTimer;
    public float BurstActiveTimer;
    public bool IsBursting;

    public PlayerStatsComponent()
    {
        BonusMaxHp = 0f;
        BonusArmor = 0f;
        BonusDamageMultiplier = 0f;
        BonusAttackSpeedMultiplier = 0f;
        BonusCritChance = 0f;
        BonusHealthRegen = 0f;
        BonusMoveSpeedMultiplier = 0f;
        BonusDodgeChance = 0f;
        BonusRangeMultiplier = 0f;

        Armor = 0f;
        CritChance = 0.05f;
        CritMultiplier = 1.5f;
        RangeMultiplier = 1f;
        DamageMultiplier = 1f;
        AttackSpeedMultiplier = 1f;
        HealthRegen = 0f;
        MoveSpeedMultiplier = 1f;
        DodgeChance = 0f;

        StationaryTimer = 0f;
        IsStationary = false;
        BurstTimer = 0f;
        BurstActiveTimer = 0f;
        IsBursting = false;
    }
}
