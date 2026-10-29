namespace BulletHeaven.Game.Components.AI;

public struct ChargerComponent
{
    public EnemyState State;
    public float StateTimer;
    public float NormalSpeed;
    public float ChargeSpeed;
    public float DirX;
    public float DirY;

    public ChargerComponent(float normalSpeed = 55f, float chargeSpeed = 250f)
    {
        State = EnemyState.Idle;
        StateTimer = 1.0f;
        NormalSpeed = normalSpeed;
        ChargeSpeed = chargeSpeed;
        DirX = 0f;
        DirY = 0f;
    }
}

