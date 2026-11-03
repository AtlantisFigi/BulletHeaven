using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Arena;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Spatial;

namespace BulletHeaven.Game.Systems;

public sealed class SpatialGridSystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<HealthComponent>? _healths;

    public SpatialGrid Grid { get; }

    public SpatialGridSystem(float cellSize = 128f)
    {
        Grid = new SpatialGrid(ArenaConfig.Bounds.X, ArenaConfig.Bounds.Y, ArenaConfig.Bounds.Width, ArenaConfig.Bounds.Height, cellSize);
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _healths = world.GetStore<HealthComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _enemies == null) return;

        Grid.Clear();

        foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyComponent>())
        {
            if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) continue;

            ref readonly PositionComponent pos = ref _positions.Get(e);
            Grid.Insert(e, pos.X, pos.Y);
        }
    }
}

