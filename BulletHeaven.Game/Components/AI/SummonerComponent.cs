namespace BulletHeaven.Game.Components.AI;

public struct SummonerComponent
{
    public float ActionTimer;
    public float ActionInterval;
    public float NormalSpeed;

    public SummonerComponent(float normalSpeed = 35f, float actionInterval = 2.2f)
    {
        ActionInterval = actionInterval;
        ActionTimer = actionInterval;
        NormalSpeed = normalSpeed;
    }
}

