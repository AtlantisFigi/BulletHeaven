using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Arena;
using BulletHeaven.Game.Components;

namespace BulletHeaven.Game.Systems;

public sealed class ArenaBoundsSystem : ISystem
{
    private World? _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<RenderColorComponent>? _colors;

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _colors = world.GetStore<RenderColorComponent>();
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

        foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyComponent>())
        {
            ref PositionComponent pos = ref _positions.Get(e);
            float radius = _colors != null && _colors.Has(e) ? _colors.Get(e).Radius : 12f;

            pos.X = Math.Clamp(pos.X, minX + radius, maxX - radius);
            pos.Y = Math.Clamp(pos.Y, minY + radius, maxY - radius);
        }
    }
}
