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
using BulletHeaven.Game.Settings;
using BulletHeaven.Game.Spatial;
using BulletHeaven.Game.Systems;
using BulletHeaven.Game.Systems.AI;
using BulletHeaven.Game.Systems.Combat;
using BulletHeaven.Game.UI;
using BulletHeaven.Game.Weapons;
using Raylib_cs;

namespace BulletHeaven.Game;

public enum GameState
{
    Intermission,
    Playing,
    Settings,
    GameOver
}

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

        var settings = GameSettings.Load();
        var viewport = new VirtualViewport();
        viewport.Initialize();

        if (settings.IsFullscreen && !autoExit)
        {
            viewport.SetFullscreen(true);
        }

        var world = new World();
        var cmd = new CommandBuffer();
        var scheduler = new SystemScheduler();
        var damageTexts = new DamageTextService();
        damageTexts.Enabled = settings.DamageNumbers;

        var inputSystem = new PlayerInputSystem();
        var playerStatsSystem = new PlayerStatsSystem();
        var waveSystem = new WaveSystem(cmd);
        var movementSystem = new MovementSystem();
        var boundsSystem = new ArenaBoundsSystem();
        var spatialSystem = new SpatialGridSystem();
        var weaponSystem = new WeaponSystem(cmd, spatialSystem.Grid);
        var projectileSystem = new ProjectileSystem(cmd, spatialSystem.Grid);
        var swarmSystem = new SwarmAISystem();
        var playerDamageSystem = new PlayerDamageSystem(spatialSystem.Grid, damageTexts);

        scheduler.Register(inputSystem)
                 .Register(playerStatsSystem)
                 .Register(waveSystem)
                 .Register(movementSystem)
                 .Register(boundsSystem)
                 .Register(spatialSystem)
                 .Register(weaponSystem)
                 .Register(projectileSystem)
                 .Register(swarmSystem)
                 .Register(playerDamageSystem);

        scheduler.Initialize(world);

        var roomRenderer = new RoomRenderSystem(world, waveSystem, damageTexts);
        var cameraService = new CameraService(ScreenWidth, ScreenHeight, new Vector2(1100f, 700f));
        var loadoutScreen = new LoadoutScreen();
        var settingsMenu = new SettingsMenu();

        SpawnPlayer(world);
        playerStatsSystem.Update(0f);

        var positions = world.GetStore<PositionComponent>();
        var healths = world.GetStore<HealthComponent>();
        var state = GameState.Intermission;
        var previousState = GameState.Intermission;

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();
            damageTexts.Enabled = settings.DamageNumbers;

            if (Raylib.IsKeyPressed(KeyboardKey.F11))
            {
                bool nextFs = !settings.IsFullscreen;
                viewport.SetFullscreen(nextFs);
                settings.IsFullscreen = nextFs;
                settings.Save();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                if (state == GameState.Settings)
                {
                    state = previousState;
                }
                else if (state != GameState.GameOver)
                {
                    previousState = state;
                    state = GameState.Settings;
                }
            }

            Vector2 virtualMouse = viewport.GetVirtualMousePosition();

            if (state == GameState.Playing)
            {
                scheduler.UpdateAll(deltaTime);
                cmd.Flush(world);
                damageTexts.Update(deltaTime);

                Entity playerEntity = default;
                bool foundPlayer = false;
                if (positions != null)
                {
                    foreach (Entity p in world.Query().With<PlayerComponent>().With<PositionComponent>())
                    {
                        playerEntity = p;
                        foundPlayer = true;
                        break;
                    }
                }

                if (foundPlayer && positions != null)
                {
                    ref readonly PositionComponent pPos = ref positions.Get(playerEntity);
                    cameraService.Update(new Vector2(pPos.X, pPos.Y), deltaTime);

                    if (healths != null && healths.Has(playerEntity))
                    {
                        ref readonly HealthComponent hp = ref healths.Get(playerEntity);
                        if (hp.IsDead)
                        {
                            state = GameState.GameOver;
                        }
                    }
                }

                if (waveSystem.WaveCompleted)
                {
                    state = GameState.Intermission;
                }
            }

            viewport.Begin();
            Raylib.ClearBackground(new Color(10, 10, 15, 255));

            if (state == GameState.Playing)
            {
                roomRenderer.Draw(cameraService.Camera);

                if (settings.ShowFPS)
                {
                    Raylib.DrawFPS(ScreenWidth - 90, 20);
                }
            }
            else if (state == GameState.Intermission)
            {
                playerStatsSystem.Update(0f);
                roomRenderer.Draw(cameraService.Camera);

                loadoutScreen.Draw(world, waveSystem, virtualMouse, () =>
                {
                    waveSystem.StartNextWave();
                    state = GameState.Playing;
                },
                () =>
                {
                    previousState = GameState.Intermission;
                    state = GameState.Settings;
                });

                if (settings.ShowFPS)
                {
                    Raylib.DrawFPS(ScreenWidth - 90, 20);
                }
            }
            else if (state == GameState.Settings)
            {
                roomRenderer.Draw(cameraService.Camera);
                settingsMenu.Draw(settings, viewport, virtualMouse, () =>
                {
                    state = previousState;
                });

                if (settings.ShowFPS)
                {
                    Raylib.DrawFPS(ScreenWidth - 90, 20);
                }
            }
            else if (state == GameState.GameOver)
            {
                roomRenderer.Draw(cameraService.Camera);

                Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(8, 6, 12, 215));

                int boxW = 500;
                int boxH = 280;
                int boxX = (ScreenWidth - boxW) / 2;
                int boxY = (ScreenHeight - boxH) / 2;

                Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(14, 16, 24, 250));
                Raylib.DrawRectangleLinesEx(new Rectangle(boxX, boxY, boxW, boxH), 2f, new Color(220, 60, 60, 200));

                string goTitle = "BIO-OPERATOR TERMINATED";
                int goTitleW = Raylib.MeasureText(goTitle, 26);
                Raylib.DrawText(goTitle, boxX + (boxW - goTitleW) / 2, boxY + 35, 26, new Color(250, 75, 75, 255));

                string goSub = "Subject succumbed to severe bio-mechanical trauma.";
                int goSubW = Raylib.MeasureText(goSub, 13);
                Raylib.DrawText(goSub, boxX + (boxW - goSubW) / 2, boxY + 72, 13, new Color(150, 165, 185, 255));

                string waveStat = $"Wave Reached: {waveSystem.CurrentWaveNumber}";
                int waveStatW = Raylib.MeasureText(waveStat, 16);
                Raylib.DrawText(waveStat, boxX + (boxW - waveStatW) / 2, boxY + 120, 16, new Color(240, 245, 255, 255));

                Rectangle restartBtn = new(boxX + 90, boxY + 180, boxW - 180, 52);
                bool btnHover = Raylib.CheckCollisionPointRec(virtualMouse, restartBtn);
                Raylib.DrawRectangleRec(restartBtn, btnHover ? new Color(200, 45, 55, 255) : new Color(140, 30, 40, 255));
                Raylib.DrawRectangleLinesEx(restartBtn, 1.5f, btnHover ? new Color(255, 120, 120, 255) : new Color(200, 60, 70, 255));

                string btnText = "RESTART RUN  [SPACE]";
                int btnTextW = Raylib.MeasureText(btnText, 15);
                Raylib.DrawText(btnText, (int)restartBtn.X + ((int)restartBtn.Width - btnTextW) / 2, (int)restartBtn.Y + 18, 15, Color.White);

                if ((btnHover && Raylib.IsMouseButtonPressed(MouseButton.Left)) || Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    RestartGame(world, waveSystem, damageTexts);
                    state = GameState.Intermission;
                }

                if (settings.ShowFPS)
                {
                    Raylib.DrawFPS(ScreenWidth - 90, 20);
                }
            }

            viewport.End();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            viewport.RenderToScreen();
            Raylib.EndDrawing();

            if (autoExit && ++frameCounter >= 60)
            {
                break;
            }
        }

        viewport.Dispose();
        Raylib.CloseWindow();
    }

    private static void RestartGame(World world, WaveSystem waveSystem, DamageTextService damageTexts)
    {
        damageTexts.Clear();

        var enemyStore = world.GetStore<EnemyComponent>();
        if (enemyStore != null)
        {
            foreach (Entity e in world.Query().With<EnemyComponent>())
            {
                world.DestroyEntity(e);
            }
        }

        var projStore = world.GetStore<ProjectileComponent>();
        if (projStore != null)
        {
            foreach (Entity p in world.Query().With<ProjectileComponent>())
            {
                world.DestroyEntity(p);
            }
        }

        var playerStore = world.GetStore<PlayerComponent>();
        var healthStore = world.GetStore<HealthComponent>();
        var posStore = world.GetStore<PositionComponent>();

        if (playerStore != null && healthStore != null && posStore != null)
        {
            foreach (Entity p in world.Query().With<PlayerComponent>().With<HealthComponent>())
            {
                ref HealthComponent hp = ref healthStore.Get(p);
                hp.Current = hp.Max;

                ref PositionComponent pos = ref posStore.Get(p);
                pos.X = 1100f;
                pos.Y = 700f;

                ref PlayerComponent pc = ref playerStore.Get(p);
                pc.InvulnerabilityTimer = 0f;
                pc.FlashTimer = 0f;
                pc.Biomass = 50;
            }
        }

        waveSystem.Reset();
    }

    private static void SpawnPlayer(World world)
    {
        Entity player = world.CreateEntity();
        world.AddComponent(player, new PositionComponent(1100f, 700f));
        world.AddComponent(player, new VelocityComponent(0f, 0f));
        world.AddComponent(player, new PlayerComponent(280f));
        world.AddComponent(player, new HealthComponent(100f));
        world.AddComponent(player, new PlayerStatsComponent());
        world.AddComponent(player, new RenderColorComponent(60, 200, 190, 255, 16f));

        var equipment = new EquipmentComponent(6);
        var bioDart = WeaponDatabase.CreateItem("bio_dart", tier: 1);
        if (bioDart != null) equipment.Equip(0, bioDart);

        world.AddComponent(player, equipment);

        var inventory = new BioInventoryComponent();
        world.AddComponent(player, inventory);
    }
}
