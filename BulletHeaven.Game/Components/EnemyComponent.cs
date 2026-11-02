using BulletHeaven.Game.Waves;

namespace BulletHeaven.Game.Components;

public struct EnemyComponent
{
    public EnemyType Type;

    public EnemyComponent(EnemyType type)
    {
        Type = type;
    }
}

