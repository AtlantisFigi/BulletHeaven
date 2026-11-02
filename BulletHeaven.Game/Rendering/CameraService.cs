using System.Numerics;
using BulletHeaven.Game.Arena;
using Raylib_cs;

namespace BulletHeaven.Game.Rendering;

public sealed class CameraService
{
    private readonly float _screenWidth;
    private readonly float _screenHeight;
    private readonly float _wallPadding;
    private readonly float _followSharpness;

    public Camera2D Camera;

    public CameraService(float screenWidth, float screenHeight, Vector2 initialTarget, float wallPadding = 48f, float followSharpness = 9f)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _wallPadding = wallPadding;
        _followSharpness = followSharpness;

        Camera = new Camera2D
        {
            Offset = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f),
            Target = initialTarget,
            Rotation = 0f,
            Zoom = 1.0f
        };
    }

    public void Update(Vector2 targetPos, float deltaTime)
    {
        float t = 1f - MathF.Exp(-_followSharpness * deltaTime);
        Vector2 current = Camera.Target;
        Vector2 smoothed = Vector2.Lerp(current, targetPos, t);

        float halfW = (_screenWidth * 0.5f) / Camera.Zoom;
        float halfH = (_screenHeight * 0.5f) / Camera.Zoom;

        float minX = ArenaConfig.Bounds.X + halfW - _wallPadding;
        float maxX = ArenaConfig.Bounds.X + ArenaConfig.Bounds.Width - halfW + _wallPadding;
        float minY = ArenaConfig.Bounds.Y + halfH - _wallPadding;
        float maxY = ArenaConfig.Bounds.Y + ArenaConfig.Bounds.Height - halfH + _wallPadding;

        if (minX <= maxX)
        {
            smoothed.X = Math.Clamp(smoothed.X, minX, maxX);
        }
        else
        {
            smoothed.X = ArenaConfig.Bounds.X + ArenaConfig.Bounds.Width * 0.5f;
        }

        if (minY <= maxY)
        {
            smoothed.Y = Math.Clamp(smoothed.Y, minY, maxY);
        }
        else
        {
            smoothed.Y = ArenaConfig.Bounds.Y + ArenaConfig.Bounds.Height * 0.5f;
        }

        Camera.Target = smoothed;
    }
}

