using System;
using System.IO;
using System.Linq;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Benchmarking;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Rendering;
using BulletHeaven.Game.Systems;
using BulletHeaven.Game.Systems.AI;
using Raylib_cs;

namespace BulletHeaven.Game;

public static class Program
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;

    public static void Main(string[] args)
    {
        if (args.Contains("--benchmark"))
        {
            string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Docs", "benchmarks", "benchmark_results.csv");
            csvPath = Path.GetFullPath(csvPath);
            BenchmarkRunner.RunSuite(csvPath, durationPerRun: 4f);
            return;
        }

        bool autoExit = args.Contains("--test");
        int frameCounter = 0;

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Somnium Rupture - Biopunk Arena");
        Raylib.SetTargetFPS(60);

        var world = new World();
        var cmd = new CommandBuffer();
        var scheduler = new SystemScheduler();

        var inputSystem = new PlayerInputSystem();
        var waveSystem = new WaveSystem(cmd);
        var swarmSystem = new SwarmAISystem();
        var chargerSystem = new ChargerSystem();
        var spitterSystem = new SpitterSystem(cmd);
        var movementSystem = new MovementSystem();
        var boundsSystem = new ArenaBoundsSystem();

        scheduler.Register(inputSystem)
                 .Register(waveSystem)
                 .Register(swarmSystem)
                 .Register(chargerSystem)
                 .Register(spitterSystem)
                 .Register(movementSystem)
                 .Register(boundsSystem);

        scheduler.Initialize(world);

        var roomRenderer = new RoomRenderSystem(world, waveSystem);
        var cameraService = new CameraService(ScreenWidth, ScreenHeight, new Vector2(1100f, 700f));

        SpawnPlayer(world);

        var positions = world.GetStore<PositionComponent>();

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

            scheduler.UpdateAll(deltaTime);
            cmd.Flush(world);

            Vector2 playerPos = cameraService.Camera.Target;
            if (positions != null)
            {
                foreach (Entity e in world.Query().With<PositionComponent>().With<PlayerComponent>())
                {
                    ref readonly PositionComponent pos = ref positions.Get(e);
                    playerPos = new Vector2(pos.X, pos.Y);
                    break;
                }
            }

            cameraService.Update(playerPos, deltaTime);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(10, 11, 16, 255));

            roomRenderer.Draw(cameraService.Camera);

            Raylib.DrawFPS(ScreenWidth - 90, 20);

            Raylib.EndDrawing();

            if (autoExit && ++frameCounter >= 60)
            {
                break;
            }
        }

        Raylib.CloseWindow();
    }

    private static void SpawnPlayer(World world)
    {
        Entity player = world.CreateEntity();
        world.AddComponent(player, new PositionComponent(1100f, 700f));
        world.AddComponent(player, new VelocityComponent(0f, 0f));
        world.AddComponent(player, new PlayerComponent(280f));
        world.AddComponent(player, new RenderColorComponent(60, 200, 190, 255, 16f));
    }
}
