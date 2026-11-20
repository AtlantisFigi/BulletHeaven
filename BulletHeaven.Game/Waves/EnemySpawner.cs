using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Waves;

public static class EnemySpawner
{
    public static Entity Spawn(World world, EnemyType type, float x, float y)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown enemy type.");
        }

        Entity e = world.CreateEntity();
        world.AddComponent(e, new PositionComponent(x, y));
        world.AddComponent(e, new VelocityComponent(0f, 0f));

        switch (type)
        {
            case EnemyType.Swarmer:
                world.AddComponent(e, new EnemyComponent(EnemyType.Swarmer, 8f));
                world.AddComponent(e, new HealthComponent(15f));
                world.AddComponent(e, new SwarmAIComponent(125f));
                world.AddComponent(e, new RenderColorComponent(235, 80, 50, 255, 10f));
                break;

            case EnemyType.Charger:
                world.AddComponent(e, new EnemyComponent(EnemyType.Charger, 14f));
                world.AddComponent(e, new HealthComponent(35f));
                world.AddComponent(e, new ChargerComponent(normalSpeed: 75f, chargeSpeed: 420f, triggerRange: 280f));
                world.AddComponent(e, new RenderColorComponent(245, 175, 35, 255, 13f));
                break;

            case EnemyType.Spitter:
                world.AddComponent(e, new EnemyComponent(EnemyType.Spitter, 6f));
                world.AddComponent(e, new HealthComponent(22f));
                world.AddComponent(e, new SpitterComponent(normalSpeed: 70f, bulletSpeed: 220f, actionInterval: 2.2f, preferredDistance: 320f));
                world.AddComponent(e, new RenderColorComponent(175, 65, 235, 255, 12f));
                break;
        }

        return e;
    }
}
