namespace BulletHeaven.Game.Components;

public struct HealthComponent
{
    public float Current;
    public float Max;

    public bool IsDead => Current <= 0f;

    public HealthComponent(float max)
    {
        Max = max;
        Current = max;
    }

    public HealthComponent(float current, float max)
    {
        Current = current;
        Max = max;
    }
}

