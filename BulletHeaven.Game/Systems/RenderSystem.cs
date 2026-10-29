using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.Game.Components;
using Raylib_cs;

namespace BulletHeaven.Game.Systems;

public sealed class RenderSystem
{
    private readonly World _world;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<RenderColorComponent>? _colors;

    public bool Headless { get; set; } = false;

    public RenderSystem(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _colors = world.GetStore<RenderColorComponent>();
    }

    public void Draw()
    {
        if (Headless || _positions == null || _colors == null) return;

        foreach (Entity e in _world.Query().With<PositionComponent>().With<RenderColorComponent>())
        {
            ref readonly PositionComponent pos = ref _positions.Get(e);
            ref readonly RenderColorComponent col = ref _colors.Get(e);

            Color rayColor = new(col.R, col.G, col.B, col.A);
            Raylib.DrawCircle((int)pos.X, (int)pos.Y, col.Radius, rayColor);
        }
    }
}

