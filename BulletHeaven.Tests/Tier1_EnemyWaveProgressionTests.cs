using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.Game;
using BulletHeaven.Game.Arena;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;
using BulletHeaven.Game.Systems;
using BulletHeaven.Game.Systems.AI;
using BulletHeaven.Game.Systems.Combat;
using BulletHeaven.Game.Waves;
using BulletHeaven.Game.Weapons;
using Xunit;

namespace BulletHeaven.Tests;

/// <summary>
/// Clean reflection adapter for M4 headless entry points (GameSimulation / Program / LevelUpScreen)
/// and optional const-vs-instance fields per PROJECT.md Interface Contracts §3-§4.
/// </summary>
internal static class M4HeadlessAdapter
{
    public static float GetPlayerHitboxRadius(PlayerComponent pc)
    {
        Type t = typeof(PlayerComponent);
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        FieldInfo? field = t.GetField("HitboxRadius", flags);
        if (field != null)
        {
            object? val = field.IsStatic ? field.GetValue(null) : field.GetValue(pc);
            if (val != null) return Convert.ToSingle(val);
        }

        PropertyInfo? prop = t.GetProperty("HitboxRadius", flags);
        if (prop != null)
        {
            object? val = prop.GetMethod?.IsStatic == true ? prop.GetValue(null) : prop.GetValue(pc);
            if (val != null) return Convert.ToSingle(val);
        }

        throw new Xunit.Sdk.XunitException("PlayerComponent.HitboxRadius member not found (expected 6f per PROJECT.md §Contracts §3).");
    }

    public static bool HasGrazeMember()
    {
        Type[] targets =
        {
            typeof(PlayerComponent),
            typeof(PlayerDamageSystem),
            typeof(Program)
        };

        foreach (Type t in targets)
        {
            if (t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                 .Any(m => m.Name.Contains("Graze", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return TestHarness.FindGameTypes(t => t.Name.Contains("Graze", StringComparison.OrdinalIgnoreCase)).Count > 0;
    }

    public static GameState ParseGameState(string name)
    {
        Assert.True(Enum.TryParse<GameState>(name, out GameState state), $"GameState.{name} must exist per PROJECT.md §F1/§Contracts §4.");
        return state;
    }

    public static M4SimSession CreateSession()
    {
        Type? simType = TestHarness.GetGameType("BulletHeaven.Game.GameSimulation")
            ?? TestHarness.FindGameTypes(t => t.Name.Equals("GameSimulation", StringComparison.Ordinal)).FirstOrDefault();

        object? instance = null;
        if (simType != null)
        {
            MethodInfo? factory = simType.GetMethod("CreateHeadless", BindingFlags.Public | BindingFlags.Static)
                ?? simType.GetMethod("CreateRun", BindingFlags.Public | BindingFlags.Static)
                ?? simType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

            if (factory != null && factory.GetParameters().Length == 0)
            {
                instance = factory.Invoke(null, null);
            }
            else if (simType.GetConstructor(Type.EmptyTypes) != null)
            {
                instance = Activator.CreateInstance(simType);
            }
            else
            {
                ConstructorInfo? boolCtor = simType.GetConstructor(new[] { typeof(bool) });
                if (boolCtor != null)
                {
                    instance = boolCtor.Invoke(new object[] { false });
                }
            }
        }

        if (instance == null)
        {
            Type prog = typeof(Program);
            MethodInfo? progFactory = prog.GetMethod("CreateHeadlessSimulation", BindingFlags.Public | BindingFlags.Static)
                ?? prog.GetMethod("CreateSimulation", BindingFlags.Public | BindingFlags.Static)
                ?? prog.GetMethod("CreateHeadlessRun", BindingFlags.Public | BindingFlags.Static);

            if (progFactory != null && progFactory.GetParameters().Length == 0)
            {
                instance = progFactory.Invoke(null, null);
            }
        }

        Assert.True(instance != null, "Expected headless GameSimulation or Program simulation factory per PROJECT.md §Contracts §4.");
        return new M4SimSession(instance!);
    }

    public static IReadOnlyList<M4CardInfo> GetLevelUpCardsFromScreen(World world, Entity player)
    {
        Type? screenType = TestHarness.GetGameType("BulletHeaven.Game.UI.LevelUpScreen")
            ?? TestHarness.FindGameTypes(t => t.Name.Equals("LevelUpScreen", StringComparison.Ordinal)).FirstOrDefault();

        Assert.True(screenType != null, "Expected LevelUpScreen type per PROJECT.md §Code Layout & §Contracts §4.");
        object screen = Activator.CreateInstance(screenType!)!;

        MethodInfo? genMethod = screenType!.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m =>
                (m.Name.Contains("Generate", StringComparison.OrdinalIgnoreCase) ||
                 m.Name.Contains("Build", StringComparison.OrdinalIgnoreCase) ||
                 m.Name.Contains("Open", StringComparison.OrdinalIgnoreCase) ||
                 m.Name.Contains("Prepare", StringComparison.OrdinalIgnoreCase)) &&
                m.GetParameters().Length is 1 or 2);

        if (genMethod != null)
        {
            var parms = genMethod.GetParameters();
            object? result = parms.Length == 2
                ? genMethod.Invoke(screen, new object[] { world, player })
                : genMethod.Invoke(screen, new object[] { world });

            if (result is IEnumerable enumerableResult && result is not string)
            {
                return ExtractCards(enumerableResult);
            }
        }

        return ReadCardsFromObject(screen);
    }

    public static List<M4CardInfo> ReadCardsFromObject(object container)
    {
        Type t = container.GetType();
        string[] candidateNames = { "Cards", "Choices", "Options", "CurrentChoices", "CurrentCards", "LevelUpChoices" };

        foreach (string name in candidateNames)
        {
            PropertyInfo? prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop?.GetValue(container) is IEnumerable list && list is not string)
            {
                return ExtractCards(list);
            }

            FieldInfo? field = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field?.GetValue(container) is IEnumerable flist && flist is not string)
            {
                return ExtractCards(flist);
            }
        }

        return new List<M4CardInfo>();
    }

    private static List<M4CardInfo> ExtractCards(IEnumerable items)
    {
        var cards = new List<M4CardInfo>();
        foreach (object item in items)
        {
            if (item == null) continue;
            Type ct = item.GetType();

            string? weaponId = ReadStringMember(item, ct, "WeaponId", "Id");
            if (string.IsNullOrEmpty(weaponId))
            {
                object? def = ReadMember(item, ct, "Definition", "WeaponDefinition", "Weapon");
                if (def != null)
                {
                    weaponId = ReadStringMember(def, def.GetType(), "Id", "WeaponId");
                }
            }

            bool isNew = ReadBoolMember(item, ct, "IsNew", "IsNewWeapon");
            bool isUpgrade = ReadBoolMember(item, ct, "IsUpgrade");
            int targetSlot = ReadIntMember(item, ct, "SlotIndex", "TargetSlot", "Slot") ?? -1;
            int nextLevel = ReadIntMember(item, ct, "NextLevel", "NewLevel", "TargetLevel", "Level") ?? 1;
            string? title = ReadStringMember(item, ct, "Title", "Name", "Label");

            cards.Add(new M4CardInfo(weaponId ?? "", isNew, isUpgrade, targetSlot, nextLevel, title ?? "", item));
        }
        return cards;
    }

    internal static object? ReadMember(object obj, Type t, params string[] names)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        foreach (string name in names)
        {
            PropertyInfo? p = t.GetProperty(name, flags);
            if (p != null) return p.GetValue(obj);
            FieldInfo? f = t.GetField(name, flags);
            if (f != null) return f.GetValue(obj);
        }
        return null;
    }

    private static string? ReadStringMember(object obj, Type t, params string[] names)
        => ReadMember(obj, t, names) as string;

    private static bool ReadBoolMember(object obj, Type t, params string[] names)
    {
        object? val = ReadMember(obj, t, names);
        return val is bool b && b;
    }

    private static int? ReadIntMember(object obj, Type t, params string[] names)
    {
        object? val = ReadMember(obj, t, names);
        return val != null ? Convert.ToInt32(val) : null;
    }
}

internal sealed record M4CardInfo(
    string WeaponId,
    bool IsNewWeapon,
    bool IsUpgrade,
    int TargetSlot,
    int NextLevel,
    string Title,
    object Raw);

internal sealed class M4SimSession
{
    public object Instance { get; }
    private readonly Type _type;

    public M4SimSession(object instance)
    {
        Instance = instance;
        _type = instance.GetType();
    }

    public GameState State
    {
        get
        {
            object? val = M4HeadlessAdapter.ReadMember(Instance, _type, "State", "CurrentState", "GameState");
            Assert.NotNull(val);
            return (GameState)val!;
        }
    }

    public World World
    {
        get
        {
            object? val = M4HeadlessAdapter.ReadMember(Instance, _type, "World");
            Assert.NotNull(val);
            return (World)val!;
        }
    }

    public WaveSystem WaveSystem
    {
        get
        {
            object? val = M4HeadlessAdapter.ReadMember(Instance, _type, "WaveSystem");
            Assert.NotNull(val);
            return (WaveSystem)val!;
        }
    }

    public Entity Player
    {
        get
        {
            object? val = M4HeadlessAdapter.ReadMember(Instance, _type, "Player", "PlayerEntity");
            if (val is Entity e && World.IsAlive(e))
            {
                return e;
            }

            foreach (Entity p in World.Query().With<PlayerComponent>())
            {
                return p;
            }

            throw new Xunit.Sdk.XunitException("Player entity not found in GameSimulation world.");
        }
    }

    public bool SelectStarter(int index)
    {
        return InvokeIndexBoolOrVoid(index,
            "SelectStarterWeapon", "SelectStarter", "ChooseStarterWeapon", "ChooseStarter", "PickStarterWeapon", "PickStarter");
    }

    public bool SelectLevelUpCard(int index)
    {
        return InvokeIndexBoolOrVoid(index,
            "SelectLevelUpCard", "SelectLevelUp", "ChooseLevelUpCard", "PickLevelUpCard", "ApplyLevelUpChoice", "SelectCard");
    }

    public void Step(float dt = TestHarness.DefaultDt, int count = 1)
    {
        MethodInfo? m = _type.GetMethod("Step", new[] { typeof(float) })
            ?? _type.GetMethod("Update", new[] { typeof(float) })
            ?? _type.GetMethod("Tick", new[] { typeof(float) });

        Assert.True(m != null, $"Expected Step(float)/Update(float)/Tick(float) on {_type.Name}.");
        for (int i = 0; i < count; i++)
        {
            m!.Invoke(Instance, new object[] { dt });
        }
    }

    public void Restart()
    {
        MethodInfo? m = _type.GetMethod("RestartGame", Type.EmptyTypes)
            ?? _type.GetMethod("Restart", Type.EmptyTypes)
            ?? _type.GetMethod("Reset", Type.EmptyTypes);
        Assert.True(m != null, $"Expected RestartGame()/Restart() on {_type.Name}.");
        m!.Invoke(Instance, null);
    }

    public bool ToggleEscapeOrSettings()
    {
        MethodInfo? m = _type.GetMethod("HandleEscape", Type.EmptyTypes)
            ?? _type.GetMethod("PressEscape", Type.EmptyTypes)
            ?? _type.GetMethod("ToggleSettings", Type.EmptyTypes);
        if (m == null) return false;
        m.Invoke(Instance, null);
        return true;
    }

    public IReadOnlyList<M4CardInfo> GetLevelUpCards()
    {
        var direct = M4HeadlessAdapter.ReadCardsFromObject(Instance);
        if (direct.Count > 0) return direct;

        object? screen = M4HeadlessAdapter.ReadMember(Instance, _type, "LevelUpScreen", "LevelUp");
        if (screen != null)
        {
            var fromScreen = M4HeadlessAdapter.ReadCardsFromObject(screen);
            if (fromScreen.Count > 0) return fromScreen;
        }

        return M4HeadlessAdapter.GetLevelUpCardsFromScreen(World, Player);
    }

    private bool InvokeIndexBoolOrVoid(int index, params string[] methodNames)
    {
        foreach (string name in methodNames)
        {
            MethodInfo? m = _type.GetMethod(name, new[] { typeof(int) });
            if (m != null)
            {
                object? res = m.Invoke(Instance, new object[] { index });
                return res is bool b ? b : true;
            }
        }

        throw new Xunit.Sdk.XunitException($"Expected method ({string.Join("/", methodNames)})(int) on {_type.Name}.");
    }
}

public sealed class Tier1_EnemyWaveProgressionTests
{
    private static readonly string[] ExpectedStarterOrder =
    {
        "forward_cone",
        "piercing_line",
        "outer_ring",
        "movement_trail",
        "returning_boomerang",
        "sideways_broadside"
    };

    // =========================================================================
    // F13: 3 Enemy Roles & Spawner
    // =========================================================================

    [Fact]
    public void T1_F13_01_EnemyTypeEnumContainsSwarmerChargerSpitter()
    {
        string[] names = Enum.GetNames<EnemyType>();

        Assert.Equal(3, names.Length);
        Assert.Contains("Swarmer", names);
        Assert.Contains("Charger", names);
        Assert.Contains("Spitter", names);
        Assert.DoesNotContain("Basic", names);
    }

    [Fact]
    public void T1_F13_02_SwarmerSpawnHasSwarmAIAndRedOrangeColor()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        Entity swarmer = EnemySpawner.Spawn(run.World, EnemyType.Swarmer, 120f, 220f);

        Assert.True(run.World.HasComponent<EnemyComponent>(swarmer));
        Assert.Equal(EnemyType.Swarmer, run.World.GetComponent<EnemyComponent>(swarmer).Type);
        Assert.True(run.World.HasComponent<SwarmAIComponent>(swarmer));
        Assert.True(run.World.HasComponent<HealthComponent>(swarmer));

        ref readonly RenderColorComponent col = ref run.World.GetComponent<RenderColorComponent>(swarmer);
        Assert.True(col.R >= 180 && col.R > col.G && col.R > col.B,
            $"Swarmer should be Red/Orange, got RGB({col.R},{col.G},{col.B}).");
    }

    [Fact]
    public void T1_F13_03_ChargerSpawnHasChargerComponentAndYellowOrangeColor()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        Entity charger = EnemySpawner.Spawn(run.World, EnemyType.Charger, 220f, 320f);

        Assert.True(run.World.HasComponent<EnemyComponent>(charger));
        Assert.Equal(EnemyType.Charger, run.World.GetComponent<EnemyComponent>(charger).Type);
        Assert.True(run.World.HasComponent<ChargerComponent>(charger));
        Assert.True(run.World.HasComponent<HealthComponent>(charger));

        ref readonly RenderColorComponent col = ref run.World.GetComponent<RenderColorComponent>(charger);
        Assert.True(col.R >= 180 && col.G >= 100 && col.R > col.B,
            $"Charger should be Yellow/Orange, got RGB({col.R},{col.G},{col.B}).");
    }

    [Fact]
    public void T1_F13_04_SpitterSpawnHasSpitterComponentAndPurpleColor()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        Entity spitter = EnemySpawner.Spawn(run.World, EnemyType.Spitter, 320f, 420f);

        Assert.True(run.World.HasComponent<EnemyComponent>(spitter));
        Assert.Equal(EnemyType.Spitter, run.World.GetComponent<EnemyComponent>(spitter).Type);
        Assert.True(run.World.HasComponent<SpitterComponent>(spitter));
        Assert.True(run.World.HasComponent<HealthComponent>(spitter));

        ref readonly RenderColorComponent col = ref run.World.GetComponent<RenderColorComponent>(spitter);
        Assert.True(col.B > col.G && col.R > col.G && col.B >= 140,
            $"Spitter should be Purple, got RGB({col.R},{col.G},{col.B}).");
    }

    [Fact]
    public void T1_F13_05_SimulationSchedulerDrivesAllThreeEnemyAISystems()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 750f, y: 550f);

        Entity swarmer = run.SpawnEnemy(EnemyType.Swarmer, 200f, 550f);
        Entity charger = run.SpawnEnemy(EnemyType.Charger, 1300f, 550f);
        Entity spitter = run.SpawnEnemy(EnemyType.Spitter, 750f, 100f);

        run.Step(TestHarness.DefaultDt, 15);

        Assert.True(run.World.GetComponent<PositionComponent>(swarmer).X > 200f, "Swarmer should advance East toward player.");
        Assert.True(run.World.GetComponent<PositionComponent>(charger).X < 1300f, "Charger should advance West toward player.");
        Assert.True(run.World.GetComponent<PositionComponent>(spitter).Y > 100f, "Spitter at 450px should advance South toward ~320px band.");
    }

    // =========================================================================
    // F14: Charger AI & Red Telegraph Line
    // =========================================================================

    [Fact]
    public void T1_F14_01_ChargerStartsIdleAndWalksTowardPlayerUntilMediumRange()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 750f, y: 550f);
        Entity charger = run.SpawnEnemy(EnemyType.Charger, 150f, 550f);

        ref readonly ChargerComponent initial = ref run.World.GetComponent<ChargerComponent>(charger);
        Assert.Equal(EnemyState.Idle, initial.State);

        run.Step(TestHarness.DefaultDt, 10);

        ref readonly ChargerComponent afterWalk = ref run.World.GetComponent<ChargerComponent>(charger);
        ref readonly PositionComponent pos = ref run.World.GetComponent<PositionComponent>(charger);
        Assert.Equal(EnemyState.Idle, afterWalk.State);
        Assert.True(pos.X > 150f, "Charger far outside medium range should walk toward player.");
    }

    [Fact]
    public void T1_F14_02_ChargerTransitionsToTelegraphAtMediumRangeWith055sTimerAndZeroVelocity()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 750f, y: 550f);
        Entity charger = run.SpawnEnemy(EnemyType.Charger, 560f, 550f);

        bool enteredTelegraph = false;
        for (int i = 0; i < 180; i++)
        {
            run.Step(TestHarness.DefaultDt, 1);
            ref readonly ChargerComponent c = ref run.World.GetComponent<ChargerComponent>(charger);
            if (c.State == EnemyState.Telegraph)
            {
                enteredTelegraph = true;
                ref readonly VelocityComponent vel = ref run.World.GetComponent<VelocityComponent>(charger);
                Assert.Equal(0f, vel.X, precision: 3);
                Assert.Equal(0f, vel.Y, precision: 3);
                Assert.InRange(c.StateTimer, 0.50f, 0.56f);
                Assert.True(c.DirX > 0.9f, $"Expected DirX pointing East toward player, got {c.DirX}.");
                break;
            }
        }

        Assert.True(enteredTelegraph, "Charger at medium range should enter EnemyState.Telegraph.");
    }

    [Fact]
    public void T1_F14_03_ChargerDashesAt420PxPerSecAfter055sTelegraph()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 750f, y: 550f);
        Entity charger = run.SpawnEnemy(EnemyType.Charger, 560f, 550f);

        bool enteredCharge = false;
        for (int i = 0; i < 240; i++)
        {
            run.Step(TestHarness.DefaultDt, 1);
            ref readonly ChargerComponent c = ref run.World.GetComponent<ChargerComponent>(charger);
            if (c.State == EnemyState.Charge)
            {
                enteredCharge = true;
                ref readonly VelocityComponent vel = ref run.World.GetComponent<VelocityComponent>(charger);
                float speed = MathF.Sqrt(vel.X * vel.X + vel.Y * vel.Y);
                Assert.InRange(speed, 415f, 425f);
                Assert.Equal(420f, c.ChargeSpeed, precision: 1);
                break;
            }
        }

        Assert.True(enteredCharge, "Charger should transition from Telegraph to Charge at 420 px/s.");
    }

    [Fact]
    public void T1_F14_04_ChargerTransitionsFromChargeToCooldownWithZeroVelThenBackToIdle()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 1100f, y: 550f);
        Entity charger = run.SpawnEnemy(EnemyType.Charger, 900f, 550f);

        bool sawTelegraph = false;
        bool sawCharge = false;
        bool sawCooldown = false;
        bool returnedToIdle = false;

        for (int i = 0; i < 360; i++)
        {
            run.Step(TestHarness.DefaultDt, 1);
            ref readonly ChargerComponent c = ref run.World.GetComponent<ChargerComponent>(charger);
            ref readonly VelocityComponent vel = ref run.World.GetComponent<VelocityComponent>(charger);

            if (c.State == EnemyState.Telegraph) sawTelegraph = true;
            else if (sawTelegraph && c.State == EnemyState.Charge) sawCharge = true;
            else if (sawCharge && c.State == EnemyState.Cooldown)
            {
                sawCooldown = true;
                Assert.Equal(0f, vel.X, precision: 3);
                Assert.Equal(0f, vel.Y, precision: 3);
            }
            else if (sawCooldown && c.State == EnemyState.Idle)
            {
                returnedToIdle = true;
                break;
            }
        }

        Assert.True(sawTelegraph && sawCharge && sawCooldown && returnedToIdle,
            $"Expected full cycle Idle->Telegraph->Charge->Cooldown->Idle, got T={sawTelegraph}, Ch={sawCharge}, Co={sawCooldown}, I={returnedToIdle}.");
    }

    [Fact]
    public void T1_F14_05_ChargerExposesNormalizedTelegraphDirectionDuringTelegraph()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 750f, y: 550f);
        Entity charger = run.SpawnEnemy(EnemyType.Charger, 600f, 400f);

        bool verifiedTelegraphVector = false;
        for (int i = 0; i < 180; i++)
        {
            run.Step(TestHarness.DefaultDt, 1);
            ref readonly ChargerComponent c = ref run.World.GetComponent<ChargerComponent>(charger);
            if (c.State == EnemyState.Telegraph)
            {
                float len = MathF.Sqrt(c.DirX * c.DirX + c.DirY * c.DirY);
                Assert.InRange(len, 0.99f, 1.01f);
                Assert.True(c.DirX > 0.5f && c.DirY > 0.5f, "DirX/DirY should point SE toward player.");
                verifiedTelegraphVector = true;
                break;
            }
        }

        Assert.True(verifiedTelegraphVector);
    }

    // =========================================================================
    // F15: Spitter AI & Enemy Projectiles
    // =========================================================================

    [Fact]
    public void T1_F15_01_SpitterAt600PxMovesCloserToward320PxBand()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 200f, y: 550f);
        Entity spitter = run.SpawnEnemy(EnemyType.Spitter, 800f, 550f);

        run.Step(TestHarness.DefaultDt, 30);

        float newX = run.World.GetComponent<PositionComponent>(spitter).X;
        Assert.True(newX < 800f, $"Spitter at 600px should move closer toward 320px band, X={newX}.");
    }

    [Fact]
    public void T1_F15_02_SpitterAt100PxRetreatsAwayToward320PxBand()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 500f, y: 550f);
        Entity spitter = run.SpawnEnemy(EnemyType.Spitter, 600f, 550f);

        run.Step(TestHarness.DefaultDt, 30);

        float newX = run.World.GetComponent<PositionComponent>(spitter).X;
        Assert.True(newX > 600f, $"Spitter at 100px should retreat away from player, X={newX}.");
    }

    [Fact]
    public void T1_F15_03_SpitterFiresFanOf3To5EnemyProjectilesEvery22Seconds()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 500f, y: 550f);
        Entity spitter = run.SpawnEnemy(EnemyType.Spitter, 820f, 550f);

        ref readonly SpitterComponent sc = ref run.World.GetComponent<SpitterComponent>(spitter);
        Assert.Equal(2.2f, sc.ActionInterval, precision: 2);
        Assert.InRange(sc.PreferredDistance, 300f, 340f);

        // Step up to 2.3s until first fan fires
        List<Entity> fan = new();
        for (int i = 0; i < 150; i++)
        {
            run.Step(TestHarness.DefaultDt, 1);
            fan = run.GetEnemyProjectiles();
            if (fan.Count > 0) break;
        }

        Assert.InRange(fan.Count, 3, 5);

        var angles = fan.Select(b =>
        {
            ref readonly VelocityComponent v = ref run.World.GetComponent<VelocityComponent>(b);
            return MathF.Atan2(v.Y, v.X);
        }).OrderBy(a => a).ToList();

        for (int i = 1; i < angles.Count; i++)
        {
            Assert.True(angles[i] - angles[i - 1] > 0.05f, "Fan projectiles must have readable angular gaps.");
        }
    }

    [Fact]
    public void T1_F15_04_EnemyProjectilesDecrementLifetimeAndDespawnOnExpiry()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 100f, y: 100f);

        Entity ep = run.World.CreateEntity();
        run.World.AddComponent(ep, new PositionComponent(700f, 550f));
        run.World.AddComponent(ep, new VelocityComponent(10f, 0f));
        run.World.AddComponent(ep, new EnemyProjectileComponent { Damage = 5f, Radius = 4f, Lifetime = 0.25f });

        run.Step(TestHarness.DefaultDt, 5); // ~0.08s
        Assert.True(run.World.IsAlive(ep));

        run.Step(TestHarness.DefaultDt, 20); // +0.33s -> expired
        Assert.False(run.World.IsAlive(ep));
    }

    [Fact]
    public void T1_F15_05_SpitterContinuesFiringBeyond500CumulativeShots()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 750f, y: 550f);

        // 15 spitters firing fans of 3-5 every 2.2s => >500 shots within ~25s
        for (int i = 0; i < 15; i++)
        {
            float angle = i * (MathF.PI * 2f / 15f);
            run.SpawnEnemy(EnemyType.Spitter, 750f + MathF.Cos(angle) * 320f, 550f + MathF.Sin(angle) * 320f);
        }

        int totalSpawned = 0;
        var seenIds = new HashSet<(int Id, int Version)>();

        for (int i = 0; i < 1600; i++) // ~26.6s
        {
            run.Step(TestHarness.DefaultDt, 1);
            foreach (Entity b in run.GetEnemyProjectiles())
            {
                if (seenIds.Add((b.Id, b.Version)))
                {
                    totalSpawned++;
                }
            }
        }

        Assert.True(totalSpawned > 500, $"Expected > 500 cumulative enemy projectiles with cap removed, got {totalSpawned}.");
    }

    // =========================================================================
    // F16: Endless Seamless Waves
    // =========================================================================

    [Fact]
    public void T1_F16_01_Wave1SpawnsMixIncludingAllThreeEnemyRoles()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 750f, y: 550f);
        run.WaveSystem.StartCurrentWave();

        var seenTypes = new HashSet<EnemyType>();
        for (int i = 0; i < 600 && seenTypes.Count < 3; i++) // up to 10s
        {
            run.Step(TestHarness.DefaultDt, 1);
            foreach (Entity e in run.GetEnemies())
            {
                seenTypes.Add(run.World.GetComponent<EnemyComponent>(e).Type);
            }
        }

        Assert.Contains(EnemyType.Swarmer, seenTypes);
        Assert.Contains(EnemyType.Charger, seenTypes);
        Assert.Contains(EnemyType.Spitter, seenTypes);
    }

    [Fact]
    public void T1_F16_02_WaveNumberIncrementsSeamlesslyAcrossWave1To2To3()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer();
        run.WaveSystem.StartCurrentWave();

        Assert.Equal(1, run.WaveSystem.CurrentWaveNumber);

        float w1 = WaveDatabase.GetWave(1).DurationSeconds;
        run.WaveSystem.Update(w1 + 0.05f);
        Assert.Equal(2, run.WaveSystem.CurrentWaveNumber);
        Assert.True(run.WaveSystem.IsWaveRunning);

        float w2 = WaveDatabase.GetWave(2).DurationSeconds;
        run.WaveSystem.Update(w2 + 0.05f);
        Assert.Equal(3, run.WaveSystem.CurrentWaveNumber);
        Assert.True(run.WaveSystem.IsWaveRunning);
    }

    [Fact]
    public void T1_F16_03_WaveDatabaseScalesCountsAndRatesFromWave1ToWave4()
    {
        WaveDefinition w1 = WaveDatabase.GetWave(1);
        WaveDefinition w4 = WaveDatabase.GetWave(4);

        Assert.True(w4.MaxActiveEnemies >= w1.MaxActiveEnemies);
        Assert.True(w4.SpawnInterval <= w1.SpawnInterval);
        Assert.True(w4.BatchMax >= w1.BatchMax);
        Assert.True(w4.MaxActiveEnemies > w1.MaxActiveEnemies || w4.SpawnInterval < w1.SpawnInterval || w4.BatchMax > w1.BatchMax,
            "Wave 4 must scale pressure above Wave 1.");
    }

    [Fact]
    public void T1_F16_04_WaveRolloverNeverDestroysActiveEnemies()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 750f, y: 550f);

        for (int i = 0; i < 10; i++)
        {
            run.SpawnEnemy(EnemyType.Swarmer, 100f + i * 30f, 100f);
        }

        run.WaveSystem.StartCurrentWave();
        float w1 = WaveDatabase.GetWave(1).DurationSeconds;
        run.WaveSystem.Update(w1 - 0.02f);
        int countBefore = run.GetEnemies().Count;

        run.WaveSystem.Update(0.05f);
        int countAfter = run.GetEnemies().Count;

        Assert.Equal(2, run.WaveSystem.CurrentWaveNumber);
        Assert.True(countAfter >= countBefore, $"Enemy count dropped on rollover ({countBefore} -> {countAfter}).");
    }

    [Fact]
    public void T1_F16_05_WaveSystemSpawnsEnemiesInsideArenaBounds()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 750f, y: 550f);
        run.WaveSystem.StartCurrentWave();

        run.Step(TestHarness.DefaultDt, 300); // 5.0s

        var enemies = run.GetEnemies();
        Assert.NotEmpty(enemies);

        foreach (Entity e in enemies)
        {
            ref readonly PositionComponent pos = ref run.World.GetComponent<PositionComponent>(e);
            Assert.InRange(pos.X, ArenaConfig.Bounds.X, ArenaConfig.Bounds.X + ArenaConfig.Bounds.Width);
            Assert.InRange(pos.Y, ArenaConfig.Bounds.Y, ArenaConfig.Bounds.Y + ArenaConfig.Bounds.Height);
        }
    }

    // =========================================================================
    // F17: Compact 6px Core Hitbox & No Graze
    // =========================================================================

    [Fact]
    public void T1_F17_01_PlayerComponentHitboxRadiusIs6Px()
    {
        var pc = new PlayerComponent(280f);
        float hitboxRadius = M4HeadlessAdapter.GetPlayerHitboxRadius(pc);
        Assert.Equal(6f, hitboxRadius, precision: 3);
    }

    [Fact]
    public void T1_F17_02_EnemyContactWithin6PlusEnemyRadiusDealsDamageAndTriggersIFrames()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        Entity player = run.SpawnPlayer(x: 500f, y: 500f, hp: 100f);
        Entity enemy = run.SpawnEnemy(EnemyType.Swarmer, 515.5f, 500f, overrideHp: 500f, overrideContactDamage: 12f);
        run.World.GetComponent<SwarmAIComponent>(enemy).Speed = 0f;

        // 15.5px <= 6f + 10f (16f)
        run.Step(TestHarness.DefaultDt, 2);

        ref readonly HealthComponent hp = ref run.World.GetComponent<HealthComponent>(player);
        ref readonly PlayerComponent pc = ref run.World.GetComponent<PlayerComponent>(player);
        Assert.Equal(88f, hp.Current, precision: 2);
        Assert.InRange(pc.InvulnerabilityTimer, 0.15f, 0.45f);
        Assert.True(pc.FlashTimer > 0f);
    }

    [Fact]
    public void T1_F17_03_EnemyProjectileWithin6PlusProjRadiusDamagesPlayerAndDespawns()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        Entity player = run.SpawnPlayer(x: 500f, y: 500f, hp: 100f);

        Entity proj = run.World.CreateEntity();
        run.World.AddComponent(proj, new PositionComponent(509.5f, 500f));
        run.World.AddComponent(proj, new VelocityComponent(0f, 0f));
        run.World.AddComponent(proj, new EnemyProjectileComponent { Damage = 8f, Radius = 4f, Lifetime = 3f });

        // 9.5px <= 6f + 4f (10f)
        run.Step(TestHarness.DefaultDt, 2);

        Assert.Equal(92f, run.World.GetComponent<HealthComponent>(player).Current, precision: 2);
        Assert.False(run.World.IsAlive(proj), "Hit enemy projectile should be consumed/destroyed.");
    }

    [Fact]
    public void T1_F17_04_ZeroGrazeMechanicOnNearMissProjectile()
    {
        Assert.False(M4HeadlessAdapter.HasGrazeMember(), "Graze mechanic must not exist in prototype.");

        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        Entity player = run.SpawnPlayer(x: 500f, y: 500f, hp: 100f);

        Entity proj = run.World.CreateEntity();
        run.World.AddComponent(proj, new PositionComponent(512f, 500f)); // 12px > 6 + 4, inside 16px visual body
        run.World.AddComponent(proj, new VelocityComponent(0f, 0f));
        run.World.AddComponent(proj, new EnemyProjectileComponent { Damage = 8f, Radius = 4f, Lifetime = 3f });

        run.Step(TestHarness.DefaultDt, 2);

        Assert.Equal(100f, run.World.GetComponent<HealthComponent>(player).Current, precision: 3);
        Assert.True(run.World.IsAlive(proj));
    }

    [Fact]
    public void T1_F17_05_IFramesBlockSecondHitWithin015Seconds()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        Entity player = run.SpawnPlayer(x: 500f, y: 500f, hp: 100f);
        Entity enemy = run.SpawnEnemy(EnemyType.Swarmer, 502f, 500f, overrideHp: 500f, overrideContactDamage: 15f);
        run.World.GetComponent<SwarmAIComponent>(enemy).Speed = 0f;

        run.Step(TestHarness.DefaultDt, 1); // Hit #1 -> 85 HP
        Assert.Equal(85f, run.World.GetComponent<HealthComponent>(player).Current, precision: 2);

        run.Step(TestHarness.DefaultDt, 9); // +0.15s while inside i-frames
        Assert.Equal(85f, run.World.GetComponent<HealthComponent>(player).Current, precision: 2);
    }

    // =========================================================================
    // F18: Starting Weapon Choice (StartChoice)
    // =========================================================================

    [Fact]
    public void T1_F18_01_NewRunInitializesInStartChoicePausedState()
    {
        M4SimSession sim = M4HeadlessAdapter.CreateSession();
        GameState startChoice = M4HeadlessAdapter.ParseGameState("StartChoice");

        Assert.Equal(startChoice, sim.State);
        Assert.False(sim.WaveSystem.IsWaveRunning);
        Assert.Equal(0, sim.World.GetComponent<EquipmentComponent>(sim.Player).CountEquippedWeapons());
    }

    [Fact]
    public void T1_F18_02_SelectingAnyStarterIndex0To5EquipsCorrespondingWeaponAtLevel1()
    {
        for (int idx = 0; idx < ExpectedStarterOrder.Length; idx++)
        {
            M4SimSession sim = M4HeadlessAdapter.CreateSession();
            bool selected = sim.SelectStarter(idx);
            Assert.True(selected, $"Starter selection index {idx} should succeed.");

            ref EquipmentComponent equip = ref sim.World.GetComponent<EquipmentComponent>(sim.Player);
            Assert.NotNull(equip.Slots[0].Weapon);
            Assert.Equal(ExpectedStarterOrder[idx], equip.Slots[0].Weapon!.Definition.Id);
            Assert.Equal(1, equip.Slots[0].Weapon!.Level);
        }
    }

    [Fact]
    public void T1_F18_03_ConfirmingStarterTransitionsToPlayingAndStartsWave1()
    {
        M4SimSession sim = M4HeadlessAdapter.CreateSession();
        sim.SelectStarter(0);

        Assert.Equal(GameState.Playing, sim.State);
        Assert.True(sim.WaveSystem.IsWaveRunning);
        Assert.Equal(1, sim.WaveSystem.CurrentWaveNumber);
    }

    [Fact]
    public void T1_F18_04_SixWeaponsOrderMatchesKeys1Through6()
    {
        var allIds = WeaponDatabase.GetAll().Select(w => w.Id).ToList();
        Assert.Equal(6, allIds.Count);

        for (int i = 0; i < ExpectedStarterOrder.Length; i++)
        {
            Assert.Equal(ExpectedStarterOrder[i], allIds[i]);
        }
    }

    [Fact]
    public void T1_F18_05_SimulationDoesNotAdvanceOrSpawnEnemiesWhileInStartChoice()
    {
        M4SimSession sim = M4HeadlessAdapter.CreateSession();
        float initialTime = sim.WaveSystem.TimeRemaining;

        sim.Step(TestHarness.DefaultDt, 180); // 3.0s while in StartChoice

        Assert.Equal(M4HeadlessAdapter.ParseGameState("StartChoice"), sim.State);
        Assert.False(sim.WaveSystem.IsWaveRunning);
        Assert.Equal(initialTime, sim.WaveSystem.TimeRemaining, precision: 3);

        int enemyCount = 0;
        foreach (Entity _ in sim.World.Query().With<EnemyComponent>()) enemyCount++;
        Assert.Equal(0, enemyCount);
    }

    // =========================================================================
    // F19: XP Orbs & 95px Magnet
    // =========================================================================

    [Fact]
    public void T1_F19_01_EnemyDeathSpawnsXpOrbAtExactDeathCoordinates()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 100f, y: 100f);
        Entity enemy = run.SpawnEnemy(EnemyType.Swarmer, 420f, 310f, overrideHp: 10f);
        run.World.GetComponent<SwarmAIComponent>(enemy).Speed = 0f;

        // Kill enemy directly
        run.World.GetComponent<HealthComponent>(enemy).Current = 0f;
        run.Step(TestHarness.DefaultDt, 1);

        var orbs = run.GetXpOrbs();
        Assert.Single(orbs);
        ref readonly PositionComponent orbPos = ref run.World.GetComponent<PositionComponent>(orbs[0]);
        Assert.Equal(420f, orbPos.X, precision: 2);
        Assert.Equal(310f, orbPos.Y, precision: 2);
    }

    [Fact]
    public void T1_F19_02_OrbWithin95PxMagnetizesAndMovesTowardPlayer()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        run.SpawnPlayer(x: 500f, y: 500f);

        Entity orb = run.World.CreateEntity();
        run.World.AddComponent(orb, new PositionComponent(590f, 500f)); // 90px <= 95px
        run.World.AddComponent(orb, new XpOrbComponent { Value = 1, IsMagnetized = false });

        run.Step(TestHarness.DefaultDt, 3);

        Assert.True(run.World.IsAlive(orb));
        Assert.True(run.World.GetComponent<XpOrbComponent>(orb).IsMagnetized);
        Assert.True(run.World.GetComponent<PositionComponent>(orb).X < 590f);
    }

    [Fact]
    public void T1_F19_03_CollectedXpOrbDestroysEntityAndIncrementsPlayerXp()
    {
        HeadlessRun run = TestHarness.CreateHeadlessPipeline();
        Entity player = run.SpawnPlayer(x: 500f, y: 500f);

        Entity orb = run.World.CreateEntity();
        run.World.AddComponent(orb, new PositionComponent(505f, 500f));
        run.World.AddComponent(orb, new XpOrbComponent { Value = 4, IsMagnetized = true });

        run.Step(TestHarness.DefaultDt, 10);

        Assert.False(run.World.IsAlive(orb));
        Assert.Equal(4, run.World.GetComponent<PlayerComponent>(player).Xp);
    }

    [Fact]
    public void T1_F19_04_Collecting10XpFromZeroTriggersLevelUp()
    {
        M4SimSession sim = M4HeadlessAdapter.CreateSession();
        sim.SelectStarter(0);
        ref readonly PositionComponent pPos = ref sim.World.GetComponent<PositionComponent>(sim.Player);

        Entity orb = sim.World.CreateEntity();
        sim.World.AddComponent(orb, new PositionComponent(pPos.X + 4f, pPos.Y));
        sim.World.AddComponent(orb, new XpOrbComponent { Value = 10, IsMagnetized = true });

        sim.Step(TestHarness.DefaultDt, 5);

        Assert.Equal(M4HeadlessAdapter.ParseGameState("LevelUp"), sim.State);
        Assert.True(sim.World.GetComponent<PlayerComponent>(sim.Player).Level >= 2);
    }

    [Fact]
    public void T1_F19_05_AllWeaponKillSourcesSpawnXpOrbsAtDeathCoordinates()
    {
        string[] weaponIds = { "forward_cone", "piercing_line", "outer_ring", "movement_trail", "returning_boomerang" };

        foreach (string weaponId in weaponIds)
        {
            HeadlessRun run = TestHarness.CreateHeadlessPipeline();
            Entity player = run.SpawnPlayer(x: 500f, y: 500f, starterWeaponId: weaponId);
            ref PlayerComponent pc = ref run.World.GetComponent<PlayerComponent>(player);
            pc.FacingAngle = 0f;

            // Place low-HP enemy at a valid target spot for that weapon
            float ex = weaponId == "outer_ring" ? 655f : 580f;
            float ey = 500f;
            Entity enemy = run.SpawnEnemy(EnemyType.Swarmer, ex, ey, overrideHp: 1f);
            run.World.GetComponent<SwarmAIComponent>(enemy).Speed = 0f;

            if (weaponId == "movement_trail")
            {
                // Move player over enemy start spot so hazard zone spawns right on enemy
                ref PositionComponent pPos = ref run.World.GetComponent<PositionComponent>(player);
                pPos.X = 560f;
                ref VelocityComponent vel = ref run.World.GetComponent<VelocityComponent>(player);
                vel.X = 280f;
                run.World.GetComponent<PositionComponent>(enemy).X = 570f;
            }

            bool orbSpawned = false;
            for (int i = 0; i < 180; i++)
            {
                run.Step(TestHarness.DefaultDt, 1);
                if (weaponId == "movement_trail" && i > 18)
                {
                    run.World.GetComponent<VelocityComponent>(player).X = 0f;
                }

                if (run.GetXpOrbs().Count > 0)
                {
                    orbSpawned = true;
                    break;
                }
            }

            Assert.True(orbSpawned, $"Weapon '{weaponId}' killing an enemy should spawn an XpOrbComponent.");
        }
    }

    // =========================================================================
    // F20: Top HUD & 3-Card Level-Up Overlay
    // =========================================================================

    [Fact]
    public void T1_F20_01_InitialPlayerHudStateIsLevel1Xp0Of10And3EmptySlots()
    {
        M4SimSession sim = M4HeadlessAdapter.CreateSession();
        ref readonly PlayerComponent pc = ref sim.World.GetComponent<PlayerComponent>(sim.Player);
        ref readonly EquipmentComponent equip = ref sim.World.GetComponent<EquipmentComponent>(sim.Player);

        Assert.Equal(1, pc.Level);
        Assert.Equal(0, pc.Xp);
        Assert.Equal(10, pc.XpToNextLevel);
        Assert.Equal(3, equip.Slots.Length);
        Assert.Equal(0, equip.CountEquippedWeapons());
    }

    [Fact]
    public void T1_F20_02_LevelUpWithLessThan3WeaponsOffers3ChoiceCardsWithNewWeapons()
    {
        M4SimSession sim = M4HeadlessAdapter.CreateSession();
        sim.SelectStarter(0); // forward_cone in slot 0 (1/3 slots)

        ref readonly PositionComponent pPos = ref sim.World.GetComponent<PositionComponent>(sim.Player);
        Entity orb = sim.World.CreateEntity();
        sim.World.AddComponent(orb, new PositionComponent(pPos.X + 2f, pPos.Y));
        sim.World.AddComponent(orb, new XpOrbComponent { Value = 10, IsMagnetized = true });
        sim.Step(TestHarness.DefaultDt, 5);

        Assert.Equal(M4HeadlessAdapter.ParseGameState("LevelUp"), sim.State);
        IReadOnlyList<M4CardInfo> cards = sim.GetLevelUpCards();
        Assert.Equal(3, cards.Count);
        Assert.Contains(cards, c => !string.IsNullOrEmpty(c.WeaponId) && c.WeaponId != "forward_cone");
    }

    [Fact]
    public void T1_F20_03_SelectingNewWeaponCardEquipsInNextOpenSlotAndResumesPlaying()
    {
        M4SimSession sim = M4HeadlessAdapter.CreateSession();
        sim.SelectStarter(0);

        ref readonly PositionComponent pPos = ref sim.World.GetComponent<PositionComponent>(sim.Player);
        Entity orb = sim.World.CreateEntity();
        sim.World.AddComponent(orb, new PositionComponent(pPos.X + 2f, pPos.Y));
        sim.World.AddComponent(orb, new XpOrbComponent { Value = 10, IsMagnetized = true });
        sim.Step(TestHarness.DefaultDt, 5);

        IReadOnlyList<M4CardInfo> cards = sim.GetLevelUpCards();
        int newWeaponIdx = -1;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].WeaponId != "forward_cone")
            {
                newWeaponIdx = i;
                break;
            }
        }
        Assert.True(newWeaponIdx >= 0, "Expected at least one new weapon card when < 3 slots filled.");

        string pickedId = cards[newWeaponIdx].WeaponId;
        Assert.True(sim.SelectLevelUpCard(newWeaponIdx));
        Assert.Equal(GameState.Playing, sim.State);

        ref EquipmentComponent equip = ref sim.World.GetComponent<EquipmentComponent>(sim.Player);
        Assert.Equal(2, equip.CountEquippedWeapons());
        Assert.Equal(pickedId, equip.Slots[1].Weapon!.Definition.Id);
        Assert.Equal(1, equip.Slots[1].Weapon!.Level);
    }

    [Fact]
    public void T1_F20_04_LevelUpWith3EquippedWeaponsOffersOnlyUpgradesForEquippedSlots()
    {
        M4SimSession sim = M4HeadlessAdapter.CreateSession();
        sim.SelectStarter(0);

        ref EquipmentComponent equip = ref sim.World.GetComponent<EquipmentComponent>(sim.Player);
        equip.Equip(1, WeaponDatabase.CreateItem("piercing_line", 1)!);
        equip.Equip(2, WeaponDatabase.CreateItem("outer_ring", 1)!);
        var equippedIds = new HashSet<string> { "forward_cone", "piercing_line", "outer_ring" };

        ref readonly PositionComponent pPos = ref sim.World.GetComponent<PositionComponent>(sim.Player);
        Entity orb = sim.World.CreateEntity();
        sim.World.AddComponent(orb, new PositionComponent(pPos.X + 2f, pPos.Y));
        sim.World.AddComponent(orb, new XpOrbComponent { Value = 10, IsMagnetized = true });
        sim.Step(TestHarness.DefaultDt, 5);

        Assert.Equal(M4HeadlessAdapter.ParseGameState("LevelUp"), sim.State);
        IReadOnlyList<M4CardInfo> cards = sim.GetLevelUpCards();
        Assert.Equal(3, cards.Count);

        foreach (M4CardInfo card in cards)
        {
            Assert.Contains(card.WeaponId, equippedIds);
            Assert.False(card.IsNewWeapon);
        }
    }

    [Fact]
    public void T1_F20_05_SelectingUpgradeCardIncrementsWeaponLevelScalesStatsAndResumesPlaying()
    {
        M4SimSession sim = M4HeadlessAdapter.CreateSession();
        sim.SelectStarter(0);

        ref EquipmentComponent equip = ref sim.World.GetComponent<EquipmentComponent>(sim.Player);
        equip.Equip(1, WeaponDatabase.CreateItem("piercing_line", 1)!);
        equip.Equip(2, WeaponDatabase.CreateItem("outer_ring", 1)!);

        float dmgBefore = equip.Slots[0].Weapon!.Damage;
        float cdBefore = equip.Slots[0].Weapon!.AttackInterval;

        ref readonly PositionComponent pPos = ref sim.World.GetComponent<PositionComponent>(sim.Player);
        Entity orb = sim.World.CreateEntity();
        sim.World.AddComponent(orb, new PositionComponent(pPos.X + 2f, pPos.Y));
        sim.World.AddComponent(orb, new XpOrbComponent { Value = 10, IsMagnetized = true });
        sim.Step(TestHarness.DefaultDt, 5);

        IReadOnlyList<M4CardInfo> cards = sim.GetLevelUpCards();
        string upgradedWeaponId = cards[0].WeaponId;
        WeaponItem targetBefore = Enumerable.Range(0, 3)
            .Select(s => sim.World.GetComponent<EquipmentComponent>(sim.Player).Slots[s].Weapon!)
            .First(w => w.Definition.Id == upgradedWeaponId);
        float targetDmgBefore = targetBefore.Damage;
        float targetCdBefore = targetBefore.AttackInterval;

        Assert.True(sim.SelectLevelUpCard(0));
        Assert.Equal(GameState.Playing, sim.State);
        Assert.Equal(2, targetBefore.Level);
        Assert.Equal(targetDmgBefore * 1.25f, targetBefore.Damage, precision: 2);
        Assert.Equal(targetCdBefore * 0.85f, targetBefore.AttackInterval, precision: 3);
        Assert.True(dmgBefore > 0f && cdBefore > 0f);
    }
}
