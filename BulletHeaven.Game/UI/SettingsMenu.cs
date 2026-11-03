using System;
using System.Numerics;
using BulletHeaven.Game.Rendering;
using BulletHeaven.Game.Settings;
using Raylib_cs;

namespace BulletHeaven.Game.UI;

public sealed class SettingsMenu
{
    private bool _isDraggingSlider = false;
    private int _activeSliderId = -1;

    public void Draw(GameSettings settings, VirtualViewport viewport, Vector2 virtualMouse, Action onClose)
    {
        bool mouseClicked = Raylib.IsMouseButtonPressed(MouseButton.Left);
        bool mouseHeld = Raylib.IsMouseButtonDown(MouseButton.Left);
        bool mouseReleased = Raylib.IsMouseButtonReleased(MouseButton.Left);

        if (mouseReleased)
        {
            _isDraggingSlider = false;
            _activeSliderId = -1;
            settings.Save();
        }

        Raylib.DrawRectangle(0, 0, 1280, 720, new Color(6, 8, 14, 235));

        string title = "SYSTEM CONFIGURATION";
        int tw = Raylib.MeasureText(title, 24);
        Raylib.DrawText(title, (1280 - tw) / 2, 45, 24, new Color(80, 235, 215, 255));

        string subtitle = "ADJUST VIDEO, AUDIO AND COMBAT PREFERENCES";
        int sw = Raylib.MeasureText(subtitle, 12);
        Raylib.DrawText(subtitle, (1280 - sw) / 2, 75, 12, new Color(135, 160, 195, 255));

        Rectangle card = new(340f, 110f, 600f, 475f);
        Raylib.DrawRectangleRec(card, new Color(13, 16, 24, 255));
        Raylib.DrawRectangleLinesEx(card, 1.5f, new Color(38, 52, 74, 220));

        float itemX = 380f;
        float itemY = 135f;
        float labelW = 200f;

        Raylib.DrawText("VIDEO & DISPLAY", (int)itemX, (int)itemY, 11, new Color(80, 230, 210, 255));
        itemY += 26f;

        bool fullscreen = settings.IsFullscreen;
        if (DrawToggle("FULLSCREEN MODE", ref fullscreen, new Rectangle(itemX + labelW, itemY - 4f, 120f, 28f), virtualMouse, mouseClicked))
        {
            settings.IsFullscreen = fullscreen;
            viewport.SetFullscreen(fullscreen);
            settings.Save();
        }
        itemY += 38f;

        bool showFps = settings.ShowFPS;
        if (DrawToggle("SHOW FPS COUNTER", ref showFps, new Rectangle(itemX + labelW, itemY - 4f, 120f, 28f), virtualMouse, mouseClicked))
        {
            settings.ShowFPS = showFps;
            settings.Save();
        }
        itemY += 38f;

        float screenShake = settings.ScreenShake;
        if (DrawSlider(1, "SCREEN SHAKE", ref screenShake, new Rectangle(itemX + labelW, itemY + 2f, 200f, 14f), virtualMouse, mouseHeld))
        {
            settings.ScreenShake = screenShake;
        }
        itemY += 46f;

        Raylib.DrawLine((int)itemX, (int)itemY, (int)(itemX + 520f), (int)itemY, new Color(32, 42, 60, 200));
        itemY += 16f;

        Raylib.DrawText("AUDIO LEVELS", (int)itemX, (int)itemY, 11, new Color(80, 230, 210, 255));
        itemY += 26f;

        float masterVol = settings.MasterVolume;
        if (DrawSlider(2, "MASTER VOLUME", ref masterVol, new Rectangle(itemX + labelW, itemY + 2f, 200f, 14f), virtualMouse, mouseHeld))
        {
            settings.MasterVolume = masterVol;
        }
        itemY += 38f;

        float sfxVol = settings.SfxVolume;
        if (DrawSlider(3, "SFX VOLUME", ref sfxVol, new Rectangle(itemX + labelW, itemY + 2f, 200f, 14f), virtualMouse, mouseHeld))
        {
            settings.SfxVolume = sfxVol;
        }
        itemY += 38f;

        float musicVol = settings.MusicVolume;
        if (DrawSlider(4, "MUSIC VOLUME", ref musicVol, new Rectangle(itemX + labelW, itemY + 2f, 200f, 14f), virtualMouse, mouseHeld))
        {
            settings.MusicVolume = musicVol;
        }
        itemY += 46f;

        Raylib.DrawLine((int)itemX, (int)itemY, (int)(itemX + 520f), (int)itemY, new Color(32, 42, 60, 200));
        itemY += 16f;

        Raylib.DrawText("COMBAT FEEDBACK", (int)itemX, (int)itemY, 11, new Color(80, 230, 210, 255));
        itemY += 26f;

        bool dmgNumbers = settings.DamageNumbers;
        if (DrawToggle("DAMAGE NUMBERS", ref dmgNumbers, new Rectangle(itemX + labelW, itemY - 4f, 120f, 28f), virtualMouse, mouseClicked))
        {
            settings.DamageNumbers = dmgNumbers;
            settings.Save();
        }

        Rectangle backBtn = new(490f, 605f, 300f, 52f);
        bool backHover = Raylib.CheckCollisionPointRec(virtualMouse, backBtn);

        Color backBg = backHover ? new Color(26, 175, 140, 255) : new Color(16, 135, 108, 255);
        Raylib.DrawRectangleRec(backBtn, backBg);
        Raylib.DrawRectangleLinesEx(backBtn, 2f, new Color(80, 250, 205, 255));

        string backText = "RETURN TO GAME  (ESC)";
        int bbw = Raylib.MeasureText(backText, 14);
        Raylib.DrawText(backText, (int)backBtn.X + ((int)backBtn.Width - bbw) / 2, (int)backBtn.Y + 18, 14, Color.White);

        if ((backHover && mouseClicked) || Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            settings.Save();
            onClose();
        }
    }

    private bool DrawToggle(string label, ref bool value, Rectangle rect, Vector2 mousePos, bool mouseClicked)
    {
        Raylib.DrawText(label, (int)rect.X - 200, (int)rect.Y + 6, 12, new Color(185, 200, 220, 255));

        bool hovered = Raylib.CheckCollisionPointRec(mousePos, rect);
        Color bg = value ? (hovered ? new Color(25, 160, 130, 255) : new Color(18, 125, 100, 255))
                         : (hovered ? new Color(45, 55, 75, 255) : new Color(30, 36, 50, 255));

        Raylib.DrawRectangleRec(rect, bg);
        Raylib.DrawRectangleLinesEx(rect, 1.5f, value ? new Color(75, 235, 185, 255) : new Color(60, 75, 100, 255));

        string stateText = value ? "ENABLED" : "DISABLED";
        int tw = Raylib.MeasureText(stateText, 11);
        Raylib.DrawText(stateText, (int)rect.X + ((int)rect.Width - tw) / 2, (int)rect.Y + 8, 11, value ? Color.White : new Color(140, 155, 175, 255));

        if (hovered && mouseClicked)
        {
            value = !value;
            return true;
        }

        return false;
    }

    private bool DrawSlider(int sliderId, string label, ref float value, Rectangle rect, Vector2 mousePos, bool mouseHeld)
    {
        Raylib.DrawText(label, (int)rect.X - 200, (int)rect.Y, 12, new Color(185, 200, 220, 255));

        Rectangle track = rect;
        Raylib.DrawRectangleRec(track, new Color(24, 30, 44, 255));
        Raylib.DrawRectangleLinesEx(track, 1f, new Color(45, 58, 80, 255));

        Rectangle fill = new(track.X, track.Y, track.Width * value, track.Height);
        Raylib.DrawRectangleRec(fill, new Color(65, 190, 160, 255));

        Rectangle knob = new(track.X + track.Width * value - 4f, track.Y - 3f, 8f, track.Height + 6f);
        Raylib.DrawRectangleRec(knob, Color.White);

        string percentText = $"{(int)(value * 100f)}%";
        Raylib.DrawText(percentText, (int)track.X + (int)track.Width + 14, (int)track.Y, 11, new Color(175, 190, 215, 255));

        Rectangle interactRect = new(track.X - 10f, track.Y - 8f, track.Width + 20f, track.Height + 16f);
        bool hovered = Raylib.CheckCollisionPointRec(mousePos, interactRect);

        if (hovered && mouseHeld && !_isDraggingSlider)
        {
            _isDraggingSlider = true;
            _activeSliderId = sliderId;
        }

        if (_isDraggingSlider && _activeSliderId == sliderId && mouseHeld)
        {
            float newVal = Math.Clamp((mousePos.X - track.X) / track.Width, 0f, 1f);
            if (MathF.Abs(newVal - value) > 0.001f)
            {
                value = newVal;
                return true;
            }
        }

        return false;
    }
}
