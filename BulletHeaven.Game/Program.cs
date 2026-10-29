using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Scenarios;
using BulletHeaven.Game.Systems;
using BulletHeaven.Game.Systems.AI;
using Raylib_cs;

namespace BulletHeaven.Game;

public static class Program
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;

    private static readonly int[] EntityCounts = { 1000, 5000, 10000, 25000 };
    private static int _currentCountIndex = 0; // Standaard 1.000
    private static ScenarioType _currentScenario = ScenarioType.Swarm;

    public static void Main(string[] args)
    {
        bool autoExit = args.Contains("--test");
        int frameCounter = 0;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--scenario" && i + 1 < args.Length && int.TryParse(args[i + 1], out int sc))
            {
                _currentScenario = (ScenarioType)Math.Clamp(sc, 1, 3);
            }
            if (args[i] == "--count" && i + 1 < args.Length && int.TryParse(args[i + 1], out int cnt))
            {
                int idx = Array.IndexOf(EntityCounts, cnt);
                if (idx >= 0) _currentCountIndex = idx;
            }
        }

        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Bullet Heaven - ECS Scenarios (Deelvraag 2)");
        Raylib.SetTargetFPS(0); // Ongecapte framerate om pure performance te kunnen zien!

        var world = new World();
        var cmd = new CommandBuffer();
        var scheduler = new SystemScheduler();

        var swarmSystem = new SwarmAISystem();
        var statefulSystem = new StatefulAISystem();
        var danmakuSystem = new DanmakuSystem();
        var movementSystem = new MovementSystem();

        scheduler.Register(swarmSystem)
                 .Register(statefulSystem)
                 .Register(danmakuSystem)
                 .Register(movementSystem);

        scheduler.Initialize(world);

        var renderSystem = new RenderSystem(world);

        LoadCurrentScenario(world);

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

            HandleInput(world, renderSystem);

            scheduler.UpdateAll(deltaTime);
            cmd.Flush(world);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(15, 15, 20, 255));

            renderSystem.Draw();

            DrawHUD(world, renderSystem, deltaTime);

            Raylib.EndDrawing();

            if (autoExit && ++frameCounter >= 60)
            {
                Console.WriteLine($"[TEST OK] Scenario {_currentScenario} with {EntityCounts[_currentCountIndex]} entities ran 60 frames successfully.");
                break;
            }
        }

        Raylib.CloseWindow();
    }

    private static void HandleInput(World world, RenderSystem renderSystem)
    {
        bool reload = false;

        if (Raylib.IsKeyPressed(KeyboardKey.One))
        {
            _currentScenario = ScenarioType.Swarm;
            reload = true;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Two))
        {
            _currentScenario = ScenarioType.Stateful;
            reload = true;
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Three))
        {
            _currentScenario = ScenarioType.Danmaku;
            reload = true;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            if (_currentCountIndex < EntityCounts.Length - 1)
            {
                _currentCountIndex++;
                reload = true;
            }
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            if (_currentCountIndex > 0)
            {
                _currentCountIndex--;
                reload = true;
            }
        }

        if (Raylib.IsKeyPressed(KeyboardKey.H))
        {
            renderSystem.Headless = !renderSystem.Headless;
        }

        if (reload)
        {
            LoadCurrentScenario(world);
        }
    }

    private static void LoadCurrentScenario(World world)
    {
        int count = EntityCounts[_currentCountIndex];
        switch (_currentScenario)
        {
            case ScenarioType.Swarm:
                ScenarioSpawner.SpawnSwarm(world, count, ScreenWidth, ScreenHeight);
                break;
            case ScenarioType.Stateful:
                ScenarioSpawner.SpawnStateful(world, count, ScreenWidth, ScreenHeight);
                break;
            case ScenarioType.Danmaku:
                ScenarioSpawner.SpawnDanmaku(world, count, ScreenWidth, ScreenHeight);
                break;
        }
    }

    private static void DrawHUD(World world, RenderSystem renderSystem, float deltaTime)
    {
        int fps = Raylib.GetFPS();
        float ms = deltaTime * 1000f;

        Raylib.DrawRectangle(5, 5, 450, 130, new Color(0, 0, 0, 180));
        Raylib.DrawRectangleLines(5, 5, 450, 130, new Color(70, 70, 90, 255));

        string scenarioName = _currentScenario switch
        {
            ScenarioType.Swarm => "Scenario A: Swarm AI (Vampire Survivors)",
            ScenarioType.Stateful => "Scenario B: Stateful AI (Brotato)",
            ScenarioType.Danmaku => "Scenario C: Danmaku Patterns (Touhou)",
            _ => "Onbekend"
        };

        Color fpsColor = fps >= 60 ? Color.Green : (fps >= 30 ? Color.Gold : Color.Red);

        Raylib.DrawText(scenarioName, 15, 12, 17, Color.SkyBlue);
        Raylib.DrawText($"Entiteiten: {world.EntityCount:N0}", 15, 36, 17, Color.RayWhite);
        Raylib.DrawText($"FPS: {fps} ({ms:F1} ms/frame)", 15, 58, 17, fpsColor);
        Raylib.DrawText($"Rendering: {(renderSystem.Headless ? "UIT (Headless)" : "AAN (GPU Active)")}", 15, 80, 15, renderSystem.Headless ? Color.Orange : Color.Lime);

        Raylib.DrawText("[1] Swarm  [2] Stateful  [3] Danmaku  |  [UP/DOWN] Schaal  [H] Headless", 15, 105, 12, Color.LightGray);
    }
}
