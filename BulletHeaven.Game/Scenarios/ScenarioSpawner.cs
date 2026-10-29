using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;

namespace BulletHeaven.Game.Scenarios;

public enum ScenarioType
{
    Swarm = 1,
    Stateful = 2,
    Danmaku = 3
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

    public static void SpawnStateful(World world, int count, float screenWidth = 1280f, float screenHeight = 720f)
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

            var ai = new StatefulAIComponent(
                normalSpeed: 50f,
                chargeSpeed: 240f,
                telegraphDuration: 0.4f,
                chargeDuration: 0.6f,
                cooldownDuration: 0.5f);

            ai.StateTimer = (float)(rnd.NextDouble() * 2.5);

            world.AddComponent(e, ai);
            world.AddComponent(e, new RenderColorComponent(220, 120, 40, 240, radius: 4.5f));
        }
    }

    public static void SpawnDanmaku(World world, int count, float screenWidth = 1280f, float screenHeight = 720f)
    {
        ResetWorld(world);
        var rnd = new Random(1337);

        float centerX = screenWidth * 0.5f;
        float centerY = screenHeight * 0.5f;

        const int arms = 16; // 16-armige spiraal
        float angleStep = (MathF.PI * 2f) / arms;

        for (int i = 0; i < count; i++)
        {
            int arm = i % arms;
            float baseAngle = arm * angleStep;
            float offset = (i / (float)arms) * 0.15f;
            float finalAngle = baseAngle + offset;

            float dist = (i % 200) * 2.5f;
            float posX = centerX + MathF.Cos(finalAngle) * dist;
            float posY = centerY + MathF.Sin(finalAngle) * dist;

            float speed = 100f + (i % 4) * 25f;
            float angularVel = 0.8f + (i % 3) * 0.2f;

            Entity e = world.CreateEntity();
            world.AddComponent(e, new PositionComponent(posX, posY));
            world.AddComponent(e, new VelocityComponent(0f, 0f));
            world.AddComponent(e, new DanmakuPatternComponent(finalAngle, angularVel, speed));

            byte r = (byte)(80 + (arm * 10) % 175);
            byte g = (byte)(160 + (arm * 12) % 95);
            byte b = 255;
            world.AddComponent(e, new RenderColorComponent(r, g, b, 200, radius: 2.5f));
        }
    }
}

