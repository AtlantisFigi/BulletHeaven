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
        world.AddComponent(e, new HealthComponent(15f));
        world.AddComponent(e, new SwarmAIComponent(115f));
        world.AddComponent(e, new RenderColorComponent(235, 80, 50, 255, 10f));
        return e;
    }
}
