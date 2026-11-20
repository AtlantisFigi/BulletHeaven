using System.Collections.Generic;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Arena;
using BulletHeaven.Game.Components;

namespace BulletHeaven.Game.Systems;

public sealed class ArenaBoundsSystem : ISystem
{
    private readonly CommandBuffer? _cmd;
    private readonly List<Entity> _toDestroy = new();
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<RenderColorComponent>? _colors;
    private ComponentStore<EnemyProjectileComponent>? _enemyProjectiles;

    public ArenaBoundsSystem(CommandBuffer? cmd = null)
    {
        _cmd = cmd;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        _enemyProjectiles = world.GetStore<EnemyProjectileComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null) return;

        float minX = ArenaConfig.Bounds.X;
        float minY = ArenaConfig.Bounds.Y;
        float maxX = ArenaConfig.Bounds.X + ArenaConfig.Bounds.Width;
        float maxY = ArenaConfig.Bounds.Y + ArenaConfig.Bounds.Height;

        foreach (Entity e in _world.Query().With<PositionComponent>().With<PlayerComponent>())
        {
            ref PositionComponent pos = ref _positions.Get(e);
            float radius = _colors != null && _colors.Has(e) ? _colors.Get(e).Radius : 16f;

            pos.X = Math.Clamp(pos.X, minX + radius, maxX - radius);
            pos.Y = Math.Clamp(pos.Y, minY + radius, maxY - radius);
        }

        foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyComponent>())
        {
            ref PositionComponent pos = ref _positions.Get(e);
            float radius = _colors != null && _colors.Has(e) ? _colors.Get(e).Radius : 12f;

            pos.X = Math.Clamp(pos.X, minX + radius, maxX - radius);
            pos.Y = Math.Clamp(pos.Y, minY + radius, maxY - radius);
        }

        if (_enemyProjectiles != null)
        {
            _toDestroy.Clear();
            foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyProjectileComponent>())
            {
                ref readonly PositionComponent pos = ref _positions.Get(e);
                ref EnemyProjectileComponent proj = ref _enemyProjectiles.Get(e);

                proj.Lifetime -= deltaTime;

                if (proj.Lifetime <= 0f || pos.X < minX || pos.X > maxX || pos.Y < minY || pos.Y > maxY)
                {
                    proj.Lifetime = 0f;
                    if (_cmd != null)
                    {
                        _cmd.DestroyEntity(e);
                    }
                    else
                    {
                        _toDestroy.Add(e);
                    }
                }
            }

            for (int i = 0; i < _toDestroy.Count; i++)
            {
                if (_world.IsAlive(_toDestroy[i]))
                {
                    _world.DestroyEntity(_toDestroy[i]);
                }
            }
        }
    }
}

