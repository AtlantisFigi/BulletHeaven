using Raylib_cs;

namespace BulletHeaven.Game.Arena;

public static class ArenaConfig
{
    public static readonly Rectangle Bounds = new(80f, 60f, 1120f, 600f);
    public const float WallThickness = 8f;

    public static readonly Color FloorColor = new(20, 22, 30, 255);
    public static readonly Color GridColor = new(28, 32, 45, 255);
    public static readonly Color WallColor = new(65, 80, 105, 255);
    public static readonly Color WallGlowColor = new(80, 180, 200, 255);
}

