namespace BulletHeaven.Game.Components.AI;

public enum EnemyState : byte
{
    Idle = 0,
    Telegraph = 1,
    Charge = 2,
    Cooldown = 3
}

public struct StatefulAIComponent
{
    public EnemyState State;
    public float StateTimer;
    public float TelegraphDuration;
    public float ChargeDuration;
    public float CooldownDuration;
    public float NormalSpeed;
    public float ChargeSpeed;
    public float ChargeDirX;
    public float ChargeDirY;

    public StatefulAIComponent(
        float normalSpeed = 60f,
        float chargeSpeed = 220f,
        float telegraphDuration = 0.5f,
        float chargeDuration = 0.8f,
        float cooldownDuration = 0.6f)
    {
        State = EnemyState.Idle;
        StateTimer = 1.0f; // start na 1 sec met telegraph
        NormalSpeed = normalSpeed;
        ChargeSpeed = chargeSpeed;
        TelegraphDuration = telegraphDuration;
        ChargeDuration = chargeDuration;
        CooldownDuration = cooldownDuration;
        ChargeDirX = 0f;
        ChargeDirY = 0f;
    }
}

