using System;
using System.Linq;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Arena;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Rendering;
using BulletHeaven.Game.Systems;
using BulletHeaven.Game.Systems.AI;
using BulletHeaven.Game.Systems.Combat;
using BulletHeaven.Game.UI;
using BulletHeaven.Game.Weapons;

namespace BulletHeaven.Game;

public sealed class GameSimulation
{
    public const float DefaultSpawnX = 750f;
    public const float DefaultSpawnY = 550f;

    public GameState State { get; private set; }
    public GameState PreviousState { get; private set; }

    public World World { get; }
    public CommandBuffer Cmd { get; }
    public SystemScheduler Scheduler { get; }

    public WaveSystem WaveSystem { get; }
    public DamageTextService DamageTexts { get; }
    public WeaponSystem WeaponSystem { get; }
    public ProjectileSystem ProjectileSystem { get; }
    public OrbitSystem OrbitSystem { get; }
    public XpSystem XpSystem { get; }
    public LevelUpScreen LevelUpScreen { get; }

    public Entity Player { get; private set; }

    private readonly ComponentStore<PlayerComponent>? _players;
    private readonly ComponentStore<HealthComponent>? _healths;
    private readonly ComponentStore<PositionComponent>? _positions;
    private readonly ComponentStore<VelocityComponent>? _velocities;
    private readonly ComponentStore<KnockbackComponent>? _knockbacks;
    private readonly ComponentStore<EquipmentComponent>? _equipments;

    public GameSimulation() : this(enableDamageText: false)
    {
    }

    private GameSimulation(bool enableDamageText)
    {
        World = new World();
        Cmd = new CommandBuffer();
        Scheduler = new SystemScheduler();
        DamageTexts = new DamageTextService { Enabled = enableDamageText };
        LevelUpScreen = new LevelUpScreen();

        var inputSystem = new PlayerInputSystem();
        WaveSystem = new WaveSystem();
        var swarmSystem = new SwarmAISystem();
        var chargerSystem = new ChargerSystem();
        var spitterSystem = new SpitterSystem(Cmd);
        var spatialSystem = new SpatialGridSystem();
        WeaponSystem = new WeaponSystem(Cmd, spatialSystem.Grid);
        OrbitSystem = new OrbitSystem(Cmd, spatialSystem.Grid);
        var hazardZoneSystem = new HazardZoneSystem(Cmd, spatialSystem.Grid);
        var boomerangSystem = new BoomerangSystem(Cmd);
        ProjectileSystem = new ProjectileSystem(Cmd, spatialSystem.Grid);
        var knockbackSystem = new KnockbackSystem();
        var movementSystem = new MovementSystem();
        var boundsSystem = new ArenaBoundsSystem(Cmd);
        var playerDamageSystem = new PlayerDamageSystem(spatialSystem.Grid, DamageTexts, Cmd);
        XpSystem = new XpSystem(Cmd);

        Scheduler.Register(inputSystem)
                 .Register(WaveSystem)
                 .Register(swarmSystem)
                 .Register(chargerSystem)
                 .Register(spitterSystem)
                 .Register(spatialSystem)
                 .Register(WeaponSystem)
                 .Register(OrbitSystem)
                 .Register(hazardZoneSystem)
                 .Register(boomerangSystem)
                 .Register(ProjectileSystem)
                 .Register(knockbackSystem)
                 .Register(movementSystem)
                 .Register(boundsSystem)
                 .Register(playerDamageSystem)
                 .Register(XpSystem);

        Scheduler.Initialize(World);

        _players = World.GetStore<PlayerComponent>();
        _healths = World.GetStore<HealthComponent>();
        _positions = World.GetStore<PositionComponent>();
        _velocities = World.GetStore<VelocityComponent>();
        _knockbacks = World.GetStore<KnockbackComponent>();
        _equipments = World.GetStore<EquipmentComponent>();

        Player = SpawnPlayer(World);
        State = GameState.StartChoice;
        PreviousState = GameState.StartChoice;
    }

    public static GameSimulation CreateHeadless() => new(enableDamageText: false);

    public static GameSimulation CreateInteractive(bool enableDamageText = true) => new(enableDamageText);

    public bool SelectStarterWeapon(int index)
    {
        if (State != GameState.StartChoice) return false;
        if (index < 0 || index >= 6) return false;

        WeaponDefinition? def = WeaponDatabase.GetByIndex(index);
        if (def == null) return false;
        if (!World.IsAlive(Player) || _equipments == null || !_equipments.Has(Player)) return false;

        ref EquipmentComponent equip = ref _equipments.Get(Player);
        if (equip.Slots != null)
        {
            for (int i = 0; i < equip.Slots.Length; i++)
            {
                equip.Slots[i].Reset();
            }
        }

        if (!equip.Equip(0, new WeaponItem(def, level: 1)))
        {
            return false;
        }

        WaveSystem.StartCurrentWave();
        State = GameState.Playing;
        PreviousState = GameState.Playing;
        return true;
    }

    public void Step(float dt)
    {
        if (State != GameState.Playing) return;

        Scheduler.UpdateAll(dt);
        Cmd.Flush(World);
        DamageTexts.Update(dt);

        if (!World.IsAlive(Player))
        {
            State = GameState.GameOver;
            return;
        }

        if (_healths != null && _healths.Has(Player) && _healths.Get(Player).IsDead)
        {
            State = GameState.GameOver;
            return;
        }

        if (_players != null && _players.Has(Player))
        {
            ref readonly PlayerComponent pc = ref _players.Get(Player);
            if (pc.PendingLevelUps > 0)
            {
                LevelUpScreen.GenerateChoices(World, Player);
                State = GameState.LevelUp;
            }
        }
    }

    public bool SelectLevelUpCard(int index)
    {
        if (State != GameState.LevelUp) return false;
        if (!LevelUpScreen.ApplyChoice(World, Player, index)) return false;

        if (_players != null && _players.Has(Player))
        {
            ref PlayerComponent pc = ref _players.Get(Player);
            if (pc.PendingLevelUps > 0)
            {
                pc.PendingLevelUps--;
            }

            if (pc.PendingLevelUps > 0)
            {
                LevelUpScreen.GenerateChoices(World, Player);
                State = GameState.LevelUp;
                return true;
            }
        }

        LevelUpScreen.Clear();
        State = GameState.Playing;
        return true;
    }

    public void RestartGame()
    {
        DamageTexts.Clear();
        ProjectileSystem.ClearHitTracking();
        OrbitSystem.Clear();
        WeaponSystem.ClearTransientVisuals();
        XpSystem.Clear();
        LevelUpScreen.Clear();

        foreach (Entity e in World.Query().With<EnemyComponent>().ToList())
        {
            World.DestroyEntity(e);
        }

        foreach (Entity p in World.Query().With<ProjectileComponent>().ToList())
        {
            World.DestroyEntity(p);
        }

        foreach (Entity hz in World.Query().With<HazardZoneComponent>().ToList())
        {
            World.DestroyEntity(hz);
        }

        foreach (Entity orb in World.Query().With<OrbitProjectileComponent>().ToList())
        {
            World.DestroyEntity(orb);
        }

        foreach (Entity boom in World.Query().With<BoomerangComponent>().ToList())
        {
            World.DestroyEntity(boom);
        }

        foreach (Entity pv in World.Query().With<ProjectileVisualComponent>().ToList())
        {
            World.DestroyEntity(pv);
        }

        foreach (Entity ep in World.Query().With<EnemyProjectileComponent>().ToList())
        {
            World.DestroyEntity(ep);
        }

        foreach (Entity xp in World.Query().With<XpOrbComponent>().ToList())
        {
            World.DestroyEntity(xp);
        }

        if (!World.IsAlive(Player))
        {
            Player = SpawnPlayer(World);
        }
        else
        {
            if (_healths != null && _healths.Has(Player))
            {
                ref HealthComponent hp = ref _healths.Get(Player);
                hp.Max = 100f;
                hp.Current = hp.Max;
                hp.DeathHandled = false;
            }

            if (_positions != null && _positions.Has(Player))
            {
                ref PositionComponent pos = ref _positions.Get(Player);
                pos.X = DefaultSpawnX;
                pos.Y = DefaultSpawnY;
            }

            if (_velocities != null && _velocities.Has(Player))
            {
                ref VelocityComponent vel = ref _velocities.Get(Player);
                vel.X = 0f;
                vel.Y = 0f;
            }

            if (_knockbacks != null && _knockbacks.Has(Player))
            {
                _knockbacks.Remove(Player);
            }

            if (_players != null && _players.Has(Player))
            {
                ref PlayerComponent pc = ref _players.Get(Player);
                pc.Speed = 280f;
                pc.HitboxRadius = PlayerComponent.CoreHitboxRadius;
                pc.FacingAngle = 0f;
                pc.InvulnerabilityTimer = 0f;
                pc.FlashTimer = 0f;
                pc.Level = 1;
                pc.Xp = 0;
                pc.XpToNextLevel = 10;
                pc.PendingLevelUps = 0;
                pc.GrazeProgress = 0;
                pc.GrazeStacks = 0;
                pc.GrazeFlashTimer = 0f;
                pc.TrailSprintTimer = 0f;
            }

            if (_equipments != null && _equipments.Has(Player))
            {
                ref EquipmentComponent equip = ref _equipments.Get(Player);
                if (equip.Slots != null)
                {
                    for (int s = 0; s < equip.Slots.Length; s++)
                    {
                        equip.Slots[s].Reset();
                    }
                }
            }
        }

        WaveSystem.Reset();
        State = GameState.StartChoice;
        PreviousState = GameState.StartChoice;
    }

    public void HandleEscape()
    {
        if (State == GameState.Settings)
        {
            State = PreviousState;
        }
        else if (State != GameState.GameOver)
        {
            PreviousState = State;
            State = GameState.Settings;
        }
    }

    private static Entity SpawnPlayer(World world)
    {
        Entity player = world.CreateEntity();
        world.AddComponent(player, new PositionComponent(DefaultSpawnX, DefaultSpawnY));
        world.AddComponent(player, new VelocityComponent(0f, 0f));
        world.AddComponent(player, new PlayerComponent(280f));
        world.AddComponent(player, new HealthComponent(100f));
        world.AddComponent(player, new RenderColorComponent(60, 200, 190, 255, 16f));
        world.AddComponent(player, new EquipmentComponent(EquipmentComponent.MaxWeaponSlots));
        return player;
    }
}
