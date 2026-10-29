using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Systems;
using Raylib_cs;

namespace BulletHeaven.Game;

public static class Program
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;

    public static void Main(string[] args)
    {
        bool autoExit = args.Contains("--test");
        int frameCounter = 0;

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Bullet Heaven");
        Raylib.SetTargetFPS(60);

        var world = new World();
        var cmd = new CommandBuffer();
        var scheduler = new SystemScheduler();

        var movementSystem = new MovementSystem();
        scheduler.Register(movementSystem);
        scheduler.Initialize(world);

        var renderSystem = new RenderSystem(world);

        SpawnTestEntities(world, 100);

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

            scheduler.UpdateAll(deltaTime);
            cmd.Flush(world);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(20, 20, 25, 255));

            renderSystem.Draw();

            Raylib.DrawFPS(10, 10);
            Raylib.DrawText($"Entities: {world.EntityCount}", 10, 35, 20, Color.RayWhite);
            Raylib.DrawText("Fase 1: ECS & Raylib integratie succesvol!", 10, 60, 18, Color.Green);

            Raylib.EndDrawing();

            if (autoExit && ++frameCounter >= 60)
            {
                Console.WriteLine("[TEST OK] Ran 60 frames successfully.");
                break;
            }
        }

        Raylib.CloseWindow();
    }

    private static void SpawnTestEntities(World world, int count)
    {
        var rnd = new Random(42);
        for (int i = 0; i < count; i++)
        {
            Entity e = world.CreateEntity();
            world.AddComponent(e, new PositionComponent(
                rnd.Next(100, ScreenWidth - 100),
                rnd.Next(100, ScreenHeight - 100)));

            world.AddComponent(e, new VelocityComponent(
                (float)(rnd.NextDouble() * 100 - 50),
                (float)(rnd.NextDouble() * 100 - 50)));

            world.AddComponent(e, new RenderColorComponent(
                (byte)rnd.Next(100, 255),
                (byte)rnd.Next(100, 255),
                (byte)rnd.Next(100, 255),
                255,
                4f));
        }
    }
}

