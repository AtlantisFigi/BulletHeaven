using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.Game.Arena;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;
using Raylib_cs;

namespace BulletHeaven.Game.Systems;

public sealed class RoomRenderSystem
{
    private readonly World _world;
    private readonly WaveSystem _waveSystem;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<RenderColorComponent>? _colors;
    private ComponentStore<ChargerComponent>? _chargers;

    public RoomRenderSystem(World world, WaveSystem waveSystem)
    {
        _world = world;
        _waveSystem = waveSystem;
        _positions = world.GetStore<PositionComponent>();
        _players = world.GetStore<PlayerComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        _chargers = world.GetStore<ChargerComponent>();
    }

    public void Draw()
    {
        Raylib.DrawRectangleRec(ArenaConfig.Bounds, ArenaConfig.FloorColor);

        for (float x = ArenaConfig.Bounds.X + 64f; x < ArenaConfig.Bounds.X + ArenaConfig.Bounds.Width; x += 64f)
        {
            Raylib.DrawLine((int)x, (int)ArenaConfig.Bounds.Y, (int)x, (int)(ArenaConfig.Bounds.Y + ArenaConfig.Bounds.Height), ArenaConfig.GridColor);
        }

        for (float y = ArenaConfig.Bounds.Y + 64f; y < ArenaConfig.Bounds.Y + ArenaConfig.Bounds.Height; y += 64f)
        {
            Raylib.DrawLine((int)ArenaConfig.Bounds.X, (int)y, (int)(ArenaConfig.Bounds.X + ArenaConfig.Bounds.Width), (int)y, ArenaConfig.GridColor);
        }

        Raylib.DrawRectangleLinesEx(ArenaConfig.Bounds, ArenaConfig.WallThickness, ArenaConfig.WallColor);

        Rectangle inner = new(ArenaConfig.Bounds.X + 2f, ArenaConfig.Bounds.Y + 2f, ArenaConfig.Bounds.Width - 4f, ArenaConfig.Bounds.Height - 4f);
        Raylib.DrawRectangleLinesEx(inner, 1f, ArenaConfig.WallGlowColor);

        if (_positions == null || _colors == null) return;

        bool hasPlayers = _players != null;
        bool hasChargers = _chargers != null;

        foreach (Entity e in _world.Query().With<PositionComponent>().With<RenderColorComponent>())
        {
            ref readonly PositionComponent pos = ref _positions.Get(e);
            ref readonly RenderColorComponent col = ref _colors.Get(e);

            if (hasChargers && _chargers!.Has(e))
            {
                ref readonly ChargerComponent charger = ref _chargers.Get(e);
                if (charger.State == EnemyState.Telegraph)
                {
                    Vector2 start = new(pos.X, pos.Y);
                    Vector2 end = new(pos.X + charger.DirX * 130f, pos.Y + charger.DirY * 130f);
                    Raylib.DrawLineEx(start, end, 2f, new Color(255, 60, 60, 180));
                }
            }

            Color bodyColor = new(col.R, col.G, col.B, col.A);
            Raylib.DrawCircle((int)pos.X, (int)pos.Y, col.Radius, bodyColor);

            if (hasPlayers && _players!.Has(e))
            {
                ref readonly PlayerComponent player = ref _players.Get(e);

                Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, col.Radius, new Color(255, 255, 255, 180));
                Raylib.DrawCircle((int)pos.X, (int)pos.Y, 4f, new Color(255, 255, 255, 240));

                float pointerDist = col.Radius - 2f;
                float px = pos.X + MathF.Cos(player.FacingAngle) * pointerDist;
                float py = pos.Y + MathF.Sin(player.FacingAngle) * pointerDist;
                Raylib.DrawCircle((int)px, (int)py, 3f, new Color(255, 220, 80, 255));
            }
        }

        string waveText = $"WAVE {_waveSystem.CurrentWaveNumber}";
        string timeText = $"TIME: {_waveSystem.TimeRemaining:0.0}s";
        string enemiesText = $"ENEMIES: {_waveSystem.ActiveEnemyCount}";
        string hud = $"{waveText}    |    {timeText}    |    {enemiesText}";

        int fontSize = 20;
        int textWidth = Raylib.MeasureText(hud, fontSize);
        int hudX = (1280 - textWidth) / 2;
        Raylib.DrawText(hud, hudX, 20, fontSize, new Color(220, 230, 245, 255));

        Raylib.DrawText("WASD / ARROW KEYS TO MOVE", 510, 680, 16, new Color(130, 145, 170, 255));
    }
}
