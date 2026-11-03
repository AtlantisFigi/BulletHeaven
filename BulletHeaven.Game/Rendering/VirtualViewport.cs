using System;
using System.Numerics;
using Raylib_cs;

namespace BulletHeaven.Game.Rendering;

public sealed class VirtualViewport : IDisposable
{
    public const int VirtualWidth = 1280;
    public const int VirtualHeight = 720;

    public RenderTexture2D Target { get; private set; }
    public float Scale { get; private set; } = 1f;
    public float OffsetX { get; private set; } = 0f;
    public float OffsetY { get; private set; } = 0f;

    public void Initialize()
    {
        Target = Raylib.LoadRenderTexture(VirtualWidth, VirtualHeight);
        Raylib.SetTextureFilter(Target.Texture, TextureFilter.Bilinear);
    }

    public void Begin()
    {
        Raylib.BeginTextureMode(Target);
    }

    public void End()
    {
        Raylib.EndTextureMode();
    }

    public void RenderToScreen()
    {
        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        Scale = MathF.Min((float)screenW / VirtualWidth, (float)screenH / VirtualHeight);
        OffsetX = (screenW - VirtualWidth * Scale) * 0.5f;
        OffsetY = (screenH - VirtualHeight * Scale) * 0.5f;

        Rectangle source = new(0f, 0f, Target.Texture.Width, -Target.Texture.Height);
        Rectangle dest = new(OffsetX, OffsetY, VirtualWidth * Scale, VirtualHeight * Scale);

        Raylib.DrawTexturePro(Target.Texture, source, dest, Vector2.Zero, 0f, Color.White);
    }

    public Vector2 GetVirtualMousePosition()
    {
        Vector2 screenMouse = Raylib.GetMousePosition();
        if (Scale <= 0.001f) return screenMouse;

        float vx = (screenMouse.X - OffsetX) / Scale;
        float vy = (screenMouse.Y - OffsetY) / Scale;

        return new Vector2(Math.Clamp(vx, 0f, VirtualWidth), Math.Clamp(vy, 0f, VirtualHeight));
    }

    public void SetFullscreen(bool enable)
    {
        if (enable && !Raylib.IsWindowFullscreen())
        {
            int monitor = Raylib.GetCurrentMonitor();
            Raylib.SetWindowSize(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
            Raylib.ToggleFullscreen();
        }
        else if (!enable && Raylib.IsWindowFullscreen())
        {
            Raylib.ToggleFullscreen();
            Raylib.SetWindowSize(VirtualWidth, VirtualHeight);
        }
    }

    public void Dispose()
    {
        Raylib.UnloadRenderTexture(Target);
    }
}

