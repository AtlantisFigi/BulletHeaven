namespace BulletHeaven.Game.Components.AI;

public struct ChargerComponent
{
    public EnemyState State;
    public float StateTimer;
    public float NormalSpeed;
    public float ChargeSpeed;
    public float TriggerRange;
    public float DirX;
    public float DirY;

    public ChargerComponent(float normalSpeed = 65f, float chargeSpeed = 420f, float triggerRange = 280f)
    {
        State = EnemyState.Idle;
        StateTimer = 0f;
        NormalSpeed = normalSpeed;
        ChargeSpeed = chargeSpeed;
        TriggerRange = triggerRange;
        DirX = 0f;
        DirY = 0f;
    }
}

