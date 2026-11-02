using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Waves;

public static class EnemySpawner
{
    public static Entity Spawn(World world, EnemyType type, float x, float y)
    {
        Entity e = world.CreateEntity();
        world.AddComponent(e, new PositionComponent(x, y));
        world.AddComponent(e, new VelocityComponent(0f, 0f));
        world.AddComponent(e, new EnemyComponent(type));

        switch (type)
        {
            case EnemyType.Swarmer:
                world.AddComponent(e, new HealthComponent(15f));
                world.AddComponent(e, new SwarmAIComponent(115f));
                world.AddComponent(e, new RenderColorComponent(235, 80, 50, 255, 10f));
                break;

            case EnemyType.Charger:
                world.AddComponent(e, new HealthComponent(50f));
                world.AddComponent(e, new ChargerComponent(normalSpeed: 50f, chargeSpeed: 230f));
                world.AddComponent(e, new RenderColorComponent(215, 45, 60, 255, 14f));
                break;

            case EnemyType.Spitter:
                world.AddComponent(e, new HealthComponent(30f));
                world.AddComponent(e, new SpitterComponent(normalSpeed: 70f, bulletSpeed: 220f, actionInterval: 2.2f, preferredDistance: 270f));
                world.AddComponent(e, new RenderColorComponent(185, 75, 235, 255, 12f));
                break;
        }

        return e;
    }
}
