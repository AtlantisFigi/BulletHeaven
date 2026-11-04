using BulletHeaven.Game.Waves;

namespace BulletHeaven.Game.Components;

public struct EnemyComponent
{
    public EnemyType Type;
    public float ContactDamage;

    public EnemyComponent(EnemyType type, float contactDamage = 5f)
    {
        Type = type;
        ContactDamage = contactDamage;
    }
}
