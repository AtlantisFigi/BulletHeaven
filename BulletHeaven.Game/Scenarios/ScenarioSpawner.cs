using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Scenarios;

public enum ScenarioType
{
    Swarm = 1,
    StatefulMonolith = 2,
    StatefulSegregated = 3
}

public static class ScenarioSpawner
{
    public static void ResetWorld(World world)
    {
        var entities = world.Query().With<PositionComponent>().ToList();
        foreach (Entity e in entities)
        {
            world.DestroyEntity(e);
        }
    }

    public static void SpawnSwarm(World world, int count, float screenWidth = 1280f, float screenHeight = 720f)
    {
        ResetWorld(world);
        var rnd = new Random(1337);

        float centerX = screenWidth * 0.5f;
        float centerY = screenHeight * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = (float)(rnd.NextDouble() * Math.PI * 2.0);
            float dist = 400f + (float)(rnd.NextDouble() * 300f);

            float posX = centerX + MathF.Cos(angle) * dist;
            float posY = centerY + MathF.Sin(angle) * dist;
            float speed = 60f + (float)(rnd.NextDouble() * 40f);

            Entity e = world.CreateEntity();
            world.AddComponent(e, new PositionComponent(posX, posY));
            world.AddComponent(e, new VelocityComponent(0f, 0f));
            world.AddComponent(e, new SwarmAIComponent(speed));
            world.AddComponent(e, new RenderColorComponent(230, 45, 45, 220, radius: 3.5f));
        }
    }

    public static void SpawnStatefulMonolith(World world, int count, float screenWidth = 1280f, float screenHeight = 720f)
    {
        ResetWorld(world);
        var rnd = new Random(1337);

        for (int i = 0; i < count; i++)
        {
            float posX = (float)(rnd.NextDouble() * (screenWidth - 100f) + 50f);
            float posY = (float)(rnd.NextDouble() * (screenHeight - 100f) + 50f);

            Entity e = world.CreateEntity();
            world.AddComponent(e, new PositionComponent(posX, posY));
            world.AddComponent(e, new VelocityComponent(0f, 0f));

            int roll = i % 10;
            if (roll < 4)
            {
                var ai = new StatefulAIComponent(
                    StatefulArchetype.Charger,
                    normalSpeed: 55f,
                    specialSpeed: 250f);
                ai.StateTimer = (float)(rnd.NextDouble() * 2.0);
                world.AddComponent(e, ai);
                world.AddComponent(e, new RenderColorComponent(230, 110, 30, 240, radius: 4.5f));
            }
            else if (roll < 8)
            {
                var ai = new StatefulAIComponent(
                    StatefulArchetype.Spitter,
                    normalSpeed: 65f,
                    specialSpeed: 210f,
                    actionInterval: 1.8f,
                    preferredDistance: 290f);
                ai.ActionTimer = (float)(rnd.NextDouble() * 1.8);
                world.AddComponent(e, ai);
                world.AddComponent(e, new RenderColorComponent(180, 70, 240, 240, radius: 4.0f));
            }
            else
            {
                var ai = new StatefulAIComponent(
                    StatefulArchetype.Summoner,
                    normalSpeed: 35f,
                    specialSpeed: 0f,
                    actionInterval: 2.2f);
                ai.ActionTimer = (float)(rnd.NextDouble() * 2.2);
                world.AddComponent(e, ai);
                world.AddComponent(e, new RenderColorComponent(50, 220, 90, 255, radius: 5.5f));
            }
        }
    }

    public static void SpawnStatefulSegregated(World world, int count, float screenWidth = 1280f, float screenHeight = 720f)
    {
        ResetWorld(world);
        var rnd = new Random(1337);

        for (int i = 0; i < count; i++)
        {
            float posX = (float)(rnd.NextDouble() * (screenWidth - 100f) + 50f);
            float posY = (float)(rnd.NextDouble() * (screenHeight - 100f) + 50f);

            Entity e = world.CreateEntity();
            world.AddComponent(e, new PositionComponent(posX, posY));
            world.AddComponent(e, new VelocityComponent(0f, 0f));

            int roll = i % 10;
            if (roll < 4)
            {
                var charger = new ChargerComponent(normalSpeed: 55f, chargeSpeed: 250f);
                charger.StateTimer = (float)(rnd.NextDouble() * 2.0);
                world.AddComponent(e, charger);
                world.AddComponent(e, new RenderColorComponent(230, 110, 30, 240, radius: 4.5f));
            }
            else if (roll < 8)
            {
                var spitter = new SpitterComponent(
                    normalSpeed: 65f,
                    bulletSpeed: 210f,
                    actionInterval: 1.8f,
                    preferredDistance: 290f);
                spitter.ActionTimer = (float)(rnd.NextDouble() * 1.8);
                world.AddComponent(e, spitter);
                world.AddComponent(e, new RenderColorComponent(180, 70, 240, 240, radius: 4.0f));
            }
            else
            {
                var summoner = new SummonerComponent(
                    normalSpeed: 35f,
                    actionInterval: 2.2f);
                summoner.ActionTimer = (float)(rnd.NextDouble() * 2.2);
                world.AddComponent(e, summoner);
                world.AddComponent(e, new RenderColorComponent(50, 220, 90, 255, radius: 5.5f));
            }
        }
    }
}
