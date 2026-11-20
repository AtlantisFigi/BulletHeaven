using System;
using System.Collections.Generic;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class XpSystem : ISystem
{
    public const float MagnetRadius = 95f;
    public const float PickupRadius = 18f;
    public const float MagnetSpeed = 460f;

    private World? _world;
    private readonly CommandBuffer? _cmd;

    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<XpOrbComponent>? _xpOrbs;

    private readonly List<Entity> _toDestroy = new();
    private readonly List<(float X, float Y, int Value)> _toSpawn = new();

    public XpSystem(CommandBuffer? cmd = null)
    {
        _cmd = cmd;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _players = world.GetStore<PlayerComponent>();
        _healths = world.GetStore<HealthComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _xpOrbs = world.GetStore<XpOrbComponent>();
        Clear();
    }

    public void Clear()
    {
        _toDestroy.Clear();
        _toSpawn.Clear();
    }

    public static int GetXpThresholdForLevel(int level)
    {
        if (level <= 1) return 10;
        return 10 + (level - 1) * 8;
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _players == null) return;
        if (!(deltaTime > 0f)) deltaTime = 0f;

        // 1. Clean up any enemies whose HP was zeroed externally without going through combat systems
        if (_enemies != null && _healths != null)
        {
            foreach (Entity enemy in _world.Query().With<EnemyComponent>().With<HealthComponent>())
            {
                ref HealthComponent hp = ref _healths.Get(enemy);
                if (hp.IsDead && !hp.DeathHandled)
                {
                    EnemyDeathHelper.HandleDeadEnemy(
                        _world,
                        _cmd,
                        ref hp,
                        _positions,
                        enemy,
                        xpValue: 1,
                        _toDestroy,
                        _toSpawn);
                }
            }
        }

        if (_xpOrbs == null)
        {
            FlushDeferred();
            return;
        }

        // 2. Locate active alive player
        Entity playerEntity = default;
        bool hasAlivePlayer = false;
        foreach (Entity p in _world.Query().With<PlayerComponent>().With<PositionComponent>())
        {
            if (_healths != null && _healths.Has(p) && _healths.Get(p).IsDead) continue;
            playerEntity = p;
            hasAlivePlayer = true;
            break;
        }

        if (!hasAlivePlayer)
        {
            FlushDeferred();
            return;
        }

        ref readonly PositionComponent pPos = ref _positions.Get(playerEntity);
        ref PlayerComponent player = ref _players.Get(playerEntity);

        float magnetSq = MagnetRadius * MagnetRadius;
        float pickupSq = PickupRadius * PickupRadius;

        foreach (Entity orbEntity in _world.Query().With<PositionComponent>().With<XpOrbComponent>())
        {
            ref PositionComponent oPos = ref _positions.Get(orbEntity);
            ref XpOrbComponent orb = ref _xpOrbs.Get(orbEntity);

            float dx = pPos.X - oPos.X;
            float dy = pPos.Y - oPos.Y;
            float distSq = dx * dx + dy * dy;

            if (!orb.IsMagnetized && distSq <= magnetSq)
            {
                orb.IsMagnetized = true;
            }

            if (distSq <= pickupSq)
            {
                CollectOrb(ref player, orb.Value);
                QueueDestroy(orbEntity);
                continue;
            }

            if (orb.IsMagnetized)
            {
                if (distSq > 0.0001f)
                {
                    float dist = MathF.Sqrt(distSq);
                    float step = MathF.Min(dist, MagnetSpeed * deltaTime);
                    float invDist = 1f / dist;
                    oPos.X += dx * invDist * step;
                    oPos.Y += dy * invDist * step;
                }

                float ndx = pPos.X - oPos.X;
                float ndy = pPos.Y - oPos.Y;
                float ndistSq = ndx * ndx + ndy * ndy;
                if (ndistSq <= pickupSq)
                {
                    CollectOrb(ref player, orb.Value);
                    QueueDestroy(orbEntity);
                }
            }
        }

        FlushDeferred();
    }

    private static void CollectOrb(ref PlayerComponent player, int orbValue)
    {
        if (orbValue > 0)
        {
            player.Xp += orbValue;
        }

        if (player.XpToNextLevel <= 0)
        {
            player.XpToNextLevel = 10;
        }

        while (player.Xp >= player.XpToNextLevel)
        {
            player.Xp -= player.XpToNextLevel;
            player.Level++;
            player.PendingLevelUps++;
            player.XpToNextLevel = GetXpThresholdForLevel(player.Level);
        }
    }

    private void QueueDestroy(Entity entity)
    {
        if (_cmd != null)
        {
            _cmd.DestroyEntity(entity);
        }
        else
        {
            _toDestroy.Add(entity);
        }
    }

    private void FlushDeferred()
    {
        if (_world == null || _cmd != null) return;

        for (int i = 0; i < _toSpawn.Count; i++)
        {
            var s = _toSpawn[i];
            Entity orb = _world.CreateEntity();
            _world.AddComponent(orb, new PositionComponent(s.X, s.Y));
            _world.AddComponent(orb, new XpOrbComponent(s.Value, false));
        }
        _toSpawn.Clear();

        for (int i = 0; i < _toDestroy.Count; i++)
        {
            Entity e = _toDestroy[i];
            if (_world.IsAlive(e))
            {
                _world.DestroyEntity(e);
            }
        }
        _toDestroy.Clear();
    }
}
