using System;
using System.IO;
using System.Linq;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.Game.Benchmarking;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;
using BulletHeaven.Game.Rendering;
using BulletHeaven.Game.Settings;
using BulletHeaven.Game.Systems;
using BulletHeaven.Game.Systems.Combat;
using BulletHeaven.Game.UI;
using BulletHeaven.Game.Waves;
using BulletHeaven.Game.Weapons;
using Raylib_cs;

namespace BulletHeaven.Game;

public enum GameState
{
    StartChoice,
    Playing,
    LevelUp,
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
        string? captureDir = ParseCaptureDir(args);
        bool captureMode = !string.IsNullOrWhiteSpace(captureDir);

        ConfigFlags flags = ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow;
        if (captureMode)
        {
            flags = ConfigFlags.Msaa4xHint | ConfigFlags.HiddenWindow | ConfigFlags.AlwaysRunWindow;
        }

        Raylib.SetConfigFlags(flags);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Somnium Rupture - Biopunk Arena");
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(60);

        var settings = GameSettings.Load();
        var viewport = new VirtualViewport();
        viewport.Initialize();

        if (settings.IsFullscreen && !autoExit && !captureMode)
        {
            viewport.SetFullscreen(true);
        }

        if (captureMode)
        {
            RunVisualCapture(captureDir!, viewport, settings);
            viewport.Dispose();
            Raylib.CloseWindow();
            return;
        }

        var sim = GameSimulation.CreateInteractive(settings.DamageNumbers);
        var roomRenderer = new RoomRenderSystem(sim.World, sim.WaveSystem, sim.DamageTexts, sim.WeaponSystem);
        var cameraService = new CameraService(ScreenWidth, ScreenHeight, new Vector2(GameSimulation.DefaultSpawnX, GameSimulation.DefaultSpawnY));
        var settingsMenu = new SettingsMenu();
        var positions = sim.World.GetStore<PositionComponent>();

        int frameCounter = 0;

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();
            sim.DamageTexts.Enabled = settings.DamageNumbers;

            if (Raylib.IsKeyPressed(KeyboardKey.F11))
            {
                bool nextFs = !settings.IsFullscreen;
                viewport.SetFullscreen(nextFs);
                settings.IsFullscreen = nextFs;
                settings.Save();
            }

            bool openedSettingsThisFrame = false;
            if (sim.State != GameState.Settings && Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                sim.HandleEscape();
                openedSettingsThisFrame = sim.State == GameState.Settings;
            }

            Vector2 virtualMouse = viewport.GetVirtualMousePosition();

            if (sim.State == GameState.Playing)
            {
                sim.Step(deltaTime);

                if (positions != null && sim.World.IsAlive(sim.Player) && positions.Has(sim.Player))
                {
                    ref readonly PositionComponent pPos = ref positions.Get(sim.Player);
                    cameraService.Update(new Vector2(pPos.X, pPos.Y), deltaTime);
                }
            }

            viewport.Begin();
            Raylib.ClearBackground(new Color(10, 10, 15, 255));

            roomRenderer.Draw(cameraService.Camera);

            if (sim.State == GameState.StartChoice)
            {
                int pickedStarter = sim.LevelUpScreen.DrawStartChoice(virtualMouse, inputEnabled: true);
                if (pickedStarter >= 0)
                {
                    sim.SelectStarterWeapon(pickedStarter);
                }
            }
            else if (sim.State == GameState.LevelUp)
            {
                int pickedCard = sim.LevelUpScreen.DrawLevelUp(sim.World, sim.Player, virtualMouse, inputEnabled: true);
                if (pickedCard >= 0)
                {
                    sim.SelectLevelUpCard(pickedCard);
                }
            }
            else if (sim.State == GameState.Settings)
            {
                settingsMenu.Draw(settings, viewport, virtualMouse, () => sim.HandleEscape(), allowEscapeClose: !openedSettingsThisFrame);
            }
            else if (sim.State == GameState.GameOver)
            {
                DrawGameOverOverlay(sim, virtualMouse, () =>
                {
                    sim.RestartGame();
                    cameraService.Camera.Target = new Vector2(GameSimulation.DefaultSpawnX, GameSimulation.DefaultSpawnY);
                });
            }

            if (settings.ShowFPS)
            {
                Raylib.DrawFPS(ScreenWidth - 90, 20);
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

    private static string? ParseCaptureDir(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--capture-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1];
            }

            const string prefix = "--capture-dir=";
            if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return args[i].Substring(prefix.Length);
            }
        }
        return null;
    }

    private static void DrawGameOverOverlay(GameSimulation sim, Vector2 virtualMouse, Action onRestart)
    {
        Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(8, 6, 12, 215));

        const int boxW = 500;
        const int boxH = 280;
        const int boxX = (ScreenWidth - boxW) / 2;
        const int boxY = (ScreenHeight - boxH) / 2;

        Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(14, 16, 24, 250));
        Raylib.DrawRectangleLinesEx(new Rectangle(boxX, boxY, boxW, boxH), 2f, new Color(220, 60, 60, 200));

        const string goTitle = "BIO-OPERATOR TERMINATED";
        int goTitleW = Raylib.MeasureText(goTitle, 26);
        Raylib.DrawText(goTitle, boxX + (boxW - goTitleW) / 2, boxY + 35, 26, new Color(250, 75, 75, 255));

        const string goSub = "Subject succumbed to severe bio-mechanical trauma.";
        int goSubW = Raylib.MeasureText(goSub, 13);
        Raylib.DrawText(goSub, boxX + (boxW - goSubW) / 2, boxY + 72, 13, new Color(150, 165, 185, 255));

        string waveStat = $"Wave Reached: {sim.WaveSystem.CurrentWaveNumber}";
        int waveStatW = Raylib.MeasureText(waveStat, 16);
        Raylib.DrawText(waveStat, boxX + (boxW - waveStatW) / 2, boxY + 120, 16, new Color(240, 245, 255, 255));

        Rectangle restartBtn = new(boxX + 90, boxY + 180, boxW - 180, 52);
        bool btnHover = Raylib.CheckCollisionPointRec(virtualMouse, restartBtn);
        Raylib.DrawRectangleRec(restartBtn, btnHover ? new Color(200, 45, 55, 255) : new Color(140, 30, 40, 255));
        Raylib.DrawRectangleLinesEx(restartBtn, 1.5f, btnHover ? new Color(255, 120, 120, 255) : new Color(200, 60, 70, 255));

        const string btnText = "RESTART RUN  [SPACE]";
        int btnTextW = Raylib.MeasureText(btnText, 15);
        Raylib.DrawText(btnText, (int)restartBtn.X + ((int)restartBtn.Width - btnTextW) / 2, (int)restartBtn.Y + 18, 15, Color.White);

        if ((btnHover && Raylib.IsMouseButtonPressed(MouseButton.Left)) || Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            onRestart();
        }
    }

    private static void RunVisualCapture(string captureDir, VirtualViewport viewport, GameSettings settings)
    {
        Directory.CreateDirectory(captureDir);

        // 1. 01_start_choice.png — Fresh run paused in arena with 0/3 slots & LVL 1 - XP 0/10 HUD
        var sim = GameSimulation.CreateInteractive(enableDamageText: false);
        var roomRenderer = new RoomRenderSystem(sim.World, sim.WaveSystem, sim.DamageTexts, sim.WeaponSystem);
        var cameraService = new CameraService(ScreenWidth, ScreenHeight, new Vector2(GameSimulation.DefaultSpawnX, GameSimulation.DefaultSpawnY));

        viewport.Begin();
        Raylib.ClearBackground(new Color(10, 10, 15, 255));
        roomRenderer.Draw(cameraService.Camera);
        sim.LevelUpScreen.DrawStartChoice(new Vector2(-100f, -100f), inputEnabled: false);
        if (settings.ShowFPS) Raylib.DrawFPS(ScreenWidth - 90, 20);
        viewport.End();
        CaptureViewportPng(viewport, Path.Combine(captureDir, "01_start_choice.png"));

        // 2. 02_combat_hud.png — Showcase frame with all 6 weapon visuals, 3 enemy roles, telegraph, bullets, XP orbs & HUD
        sim.SelectStarterWeapon(0); // forward_cone in slot 0
        ref HealthComponent pHp = ref sim.World.GetComponent<HealthComponent>(sim.Player);
        pHp.Current = 84f;

        ref PlayerComponent pc = ref sim.World.GetComponent<PlayerComponent>(sim.Player);
        pc.FacingAngle = 0f;
        pc.Level = 1;
        pc.Xp = 0;
        pc.XpToNextLevel = 10;

        ref EquipmentComponent equip = ref sim.World.GetComponent<EquipmentComponent>(sim.Player);
        equip.Slots[0].CooldownTimer = 0.40f;
        equip.Slots[0].VisualDuration = 0.20f;
        equip.Slots[0].VisualTimer = 0.16f;
        equip.Slots[0].AttackAngle = 0f;
        equip.Slots[0].AttackRange = 145f;

        equip.Equip(1, WeaponDatabase.CreateItem("outer_ring", level: 2)!);
        equip.Equip(2, WeaponDatabase.CreateItem("sideways_broadside", level: 1)!);
        equip.Slots[2].CooldownTimer = 0.25f;
        equip.Slots[2].VisualDuration = 0.14f;
        equip.Slots[2].VisualTimer = 0.12f;
        equip.Slots[2].AttackAngle = 0f;

        // Orbiting orbs on the 155px outer ring
        float[] orbAngles = { 0.4f, 2.0f, 3.6f, 5.1f };
        for (int i = 0; i < orbAngles.Length; i++)
        {
            float a = orbAngles[i];
            Entity orb = sim.World.CreateEntity();
            sim.World.AddComponent(orb, new PositionComponent(750f + MathF.Cos(a) * 155f, 550f + MathF.Sin(a) * 155f));
            sim.World.AddComponent(orb, new OrbitProjectileComponent
            {
                Owner = sim.Player,
                SlotIndex = 1,
                OrbIndex = i,
                TotalOrbs = orbAngles.Length,
                Angle = a,
                OrbitRadius = 155f,
                DeadzoneRadius = 125f,
                OrbitSpeed = 2.8f,
                Damage = 22.5f,
                Knockback = 60f,
                OrbRadius = 10f
            });
        }

        // Sideways Broadside twin flank shots (perpendicular to FacingAngle = 0)
        SpawnShowcaseProjectile(sim.World, 750f, 445f, -MathF.PI * 0.5f, ProjectileShape.BroadsideBolt, 16f, 6f, new Color(255, 165, 55, 255));
        SpawnShowcaseProjectile(sim.World, 750f, 655f, MathF.PI * 0.5f, ProjectileShape.BroadsideBolt, 16f, 6f, new Color(255, 165, 55, 255));

        // Piercing Line aim line + cyan/white elongated shot aimed at Spitter (1110, 420)
        float pierceAngle = MathF.Atan2(420f - 550f, 1110f - 750f);
        sim.WeaponSystem.ActiveAimLines.Add(new AimLineVisual
        {
            From = new Vector2(750f, 550f),
            To = new Vector2(1110f, 420f),
            Timer = 0.24f,
            MaxTimer = 0.28f
        });
        SpawnShowcaseProjectile(sim.World, 950f, 478f, pierceAngle, ProjectileShape.PiercingLine, 34f, 6f, new Color(90, 235, 255, 255));

        // Movement Trail green/yellow pulsing hazard zones behind player
        (float hx, float hy, float rem)[] trailSpots =
        {
            (715f, 550f, 2.2f),
            (675f, 556f, 1.9f),
            (635f, 566f, 1.6f),
            (595f, 578f, 1.3f)
        };
        foreach (var (hx, hy, rem) in trailSpots)
        {
            Entity hz = sim.World.CreateEntity();
            sim.World.AddComponent(hz, new PositionComponent(hx, hy));
            sim.World.AddComponent(hz, new HazardZoneComponent
            {
                Owner = sim.Player,
                Radius = 28f,
                RemainingDuration = rem,
                MaxDuration = 2.5f,
                Damage = 12f,
                TickInterval = 0.30f
            });
        }

        // Returning Boomerang spinning visual
        Entity boom = sim.World.CreateEntity();
        sim.World.AddComponent(boom, new PositionComponent(915f, 650f));
        sim.World.AddComponent(boom, new ProjectileComponent(20f, 4f, 10f, 50f, 999));
        sim.World.AddComponent(boom, new BoomerangComponent
        {
            Owner = sim.Player,
            OutboundDistanceRemaining = 90f,
            Speed = 440f,
            IsReturning = false
        });
        sim.World.AddComponent(boom, new ProjectileVisualComponent(ProjectileShape.Boomerang, 0.5f, 15f, 5f, spinAngle: 0.7f, spinSpeed: 14f));
        sim.World.AddComponent(boom, new RenderColorComponent(255, 205, 70, 255, 10f));

        // 3 Enemy Roles + Charger Telegraph + Spitter Fan Bullets
        EnemySpawner.Spawn(sim.World, EnemyType.Swarmer, 855f, 520f);
        EnemySpawner.Spawn(sim.World, EnemyType.Swarmer, 885f, 575f);
        EnemySpawner.Spawn(sim.World, EnemyType.Swarmer, 830f, 610f);

        Entity telegraphCharger = EnemySpawner.Spawn(sim.World, EnemyType.Charger, 520f, 665f);
        ref ChargerComponent chComp = ref sim.World.GetComponent<ChargerComponent>(telegraphCharger);
        chComp.State = EnemyState.Telegraph;
        chComp.StateTimer = 0.35f;
        chComp.DirX = 0.89f;
        chComp.DirY = -0.45f;
        sim.World.GetComponent<RenderColorComponent>(telegraphCharger) = new RenderColorComponent(255, 135, 30, 255, 13f);

        EnemySpawner.Spawn(sim.World, EnemyType.Charger, 560f, 430f);
        EnemySpawner.Spawn(sim.World, EnemyType.Spitter, 1110f, 420f);

        (float bx, float by)[] spitterShots =
        {
            (995f, 455f),
            (985f, 480f),
            (990f, 508f),
            (1008f, 532f)
        };
        foreach (var (bx, by) in spitterShots)
        {
            Entity ep = sim.World.CreateEntity();
            sim.World.AddComponent(ep, new PositionComponent(bx, by));
            sim.World.AddComponent(ep, new EnemyProjectileComponent { Damage = 10f, Radius = 6f, Lifetime = 3f });
        }

        // XP Orbs (mint-emerald diamonds)
        (float ox, float oy, bool mag)[] xpOrbs =
        {
            (810f, 470f, false),
            (695f, 495f, true),
            (620f, 635f, false),
            (875f, 655f, false),
            (590f, 505f, false),
            (935f, 595f, false)
        };
        foreach (var (ox, oy, mag) in xpOrbs)
        {
            Entity xp = sim.World.CreateEntity();
            sim.World.AddComponent(xp, new PositionComponent(ox, oy));
            sim.World.AddComponent(xp, new XpOrbComponent(1, mag));
        }

        sim.WaveSystem.SyncActiveEnemyCount();

        viewport.Begin();
        Raylib.ClearBackground(new Color(10, 10, 15, 255));
        roomRenderer.Draw(cameraService.Camera);
        if (settings.ShowFPS) Raylib.DrawFPS(ScreenWidth - 90, 20);
        viewport.End();
        CaptureViewportPng(viewport, Path.Combine(captureDir, "02_combat_hud.png"));

        // 3. 03_level_up.png — Paused mid-run showing 3-card Level-Up overlay
        // Unequip slot 2 so GenerateChoices showcases both NEW WEAPON and UPGRADE cards cleanly
        equip.Unequip(2);
        pc.Level = 2;
        pc.Xp = 3;
        pc.XpToNextLevel = XpSystem.GetXpThresholdForLevel(2);
        pc.PendingLevelUps = 1;
        sim.LevelUpScreen.GenerateChoices(sim.World, sim.Player);

        viewport.Begin();
        Raylib.ClearBackground(new Color(10, 10, 15, 255));
        roomRenderer.Draw(cameraService.Camera);
        sim.LevelUpScreen.DrawLevelUp(sim.World, sim.Player, new Vector2(-100f, -100f), inputEnabled: false);
        if (settings.ShowFPS) Raylib.DrawFPS(ScreenWidth - 90, 20);
        viewport.End();
        CaptureViewportPng(viewport, Path.Combine(captureDir, "03_level_up.png"));
    }

    private static void SpawnShowcaseProjectile(
        World world,
        float x,
        float y,
        float angle,
        ProjectileShape shape,
        float length,
        float thickness,
        Color color)
    {
        Entity e = world.CreateEntity();
        world.AddComponent(e, new PositionComponent(x, y));
        world.AddComponent(e, new ProjectileComponent(20f, 1f, thickness, 0f, 1));
        world.AddComponent(e, new ProjectileVisualComponent(shape, angle, length, thickness));
        world.AddComponent(e, new RenderColorComponent(color.R, color.G, color.B, color.A, thickness));
    }

    private static void CaptureViewportPng(VirtualViewport viewport, string outputPath)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        viewport.RenderToScreen();
        Raylib.EndDrawing();

        Image img = Raylib.LoadImageFromTexture(viewport.Target.Texture);
        Raylib.ImageFlipVertical(ref img);
        Raylib.ExportImage(img, outputPath);
        Raylib.UnloadImage(img);
    }
}
