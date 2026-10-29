namespace BulletHeaven.Game.Components.AI;

public enum EnemyState : byte
{
    Idle = 0,
    Telegraph = 1,
    Charge = 2,
    Cooldown = 3
}

public enum StatefulArchetype : byte
{
    Charger = 0,
    Spitter = 1,
    Summoner = 2
}

public struct StatefulAIComponent
{
    public StatefulArchetype Archetype;
    public EnemyState State;
    public float StateTimer;
    public float ActionTimer;
    public float ActionInterval;
    public float NormalSpeed;
    public float SpecialSpeed;
    public float DirX;
    public float DirY;
    public float PreferredDistance;

    public StatefulAIComponent(
        StatefulArchetype archetype,
        float normalSpeed = 60f,
        float specialSpeed = 220f,
        float actionInterval = 2.0f,
        float preferredDistance = 300f)
    {
        Archetype = archetype;
        State = EnemyState.Idle;
        StateTimer = 1.0f;
        ActionInterval = actionInterval;
        ActionTimer = actionInterval;
        NormalSpeed = normalSpeed;
        SpecialSpeed = specialSpeed;
        PreferredDistance = preferredDistance;
        DirX = 0f;
        DirY = 0f;
    }
}
