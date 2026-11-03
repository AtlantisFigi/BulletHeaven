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
    Settings
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

        var inputSystem = new PlayerInputSystem();
        var playerStatsSystem = new PlayerStatsSystem();
        var waveSystem = new WaveSystem(cmd);
        var weaponSystem = new WeaponSystem(cmd);
        var projectileSystem = new ProjectileSystem(cmd);
        var swarmSystem = new SwarmAISystem();
        var chargerSystem = new ChargerSystem();
        var spitterSystem = new SpitterSystem(cmd);
        var movementSystem = new MovementSystem();
        var boundsSystem = new ArenaBoundsSystem();

        scheduler.Register(inputSystem)
                 .Register(playerStatsSystem)
                 .Register(waveSystem)
                 .Register(weaponSystem)
                 .Register(projectileSystem)
                 .Register(swarmSystem)
                 .Register(chargerSystem)
                 .Register(spitterSystem)
                 .Register(movementSystem)
                 .Register(boundsSystem);

        scheduler.Initialize(world);

        var roomRenderer = new RoomRenderSystem(world, waveSystem);
        var cameraService = new CameraService(ScreenWidth, ScreenHeight, new Vector2(1100f, 700f));
        var loadoutScreen = new LoadoutScreen();
        var settingsMenu = new SettingsMenu();

        SpawnPlayer(world);

        var positions = world.GetStore<PositionComponent>();
        var state = GameState.Intermission;
        var previousState = GameState.Intermission;

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

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
                else
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

    private static void SpawnPlayer(World world)
    {
        Entity player = world.CreateEntity();
        world.AddComponent(player, new PositionComponent(1100f, 700f));
        world.AddComponent(player, new VelocityComponent(0f, 0f));
        world.AddComponent(player, new PlayerComponent(280f));
        world.AddComponent(player, new HealthComponent(100f));
        world.AddComponent(player, new PlayerStatsComponent());
        world.AddComponent(player, new RenderColorComponent(60, 200, 190, 255, 16f));

        var equipment = new EquipmentComponent(5);
        var bioDart = WeaponDatabase.CreateItem("bio_dart", tier: 1);
        if (bioDart != null) equipment.Equip(1, bioDart);

        var boneBlade = WeaponDatabase.CreateItem("bone_blade", tier: 1);
        if (boneBlade != null) equipment.Equip(3, boneBlade);

        world.AddComponent(player, equipment);

        var stash = new BioStashComponent(6);

        var eye = WeaponDatabase.CreateItem("trepanned_eye", tier: 1);
        if (eye != null) stash.Add(eye);

        var chest = WeaponDatabase.CreateItem("adrenaline_overcharger", tier: 1);
        if (chest != null) stash.Add(chest);

        var claw = WeaponDatabase.CreateItem("synaptic_arc_claw", tier: 1);
        if (claw != null) stash.Add(claw);

        var spikes = WeaponDatabase.CreateItem("rooted_osteo_spikes", tier: 1);
        if (spikes != null) stash.Add(spikes);

        var boots = WeaponDatabase.CreateItem("piston_burst_boots", tier: 1);
        if (boots != null) stash.Add(boots);

        var harpoon = WeaponDatabase.CreateItem("syringe_harpoon", tier: 1);
        if (harpoon != null) stash.Add(harpoon);

        world.AddComponent(player, stash);
    }
}
