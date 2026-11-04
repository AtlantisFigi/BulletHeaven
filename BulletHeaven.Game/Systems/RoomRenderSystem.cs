using System;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.Game.Arena;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Rendering;
using BulletHeaven.Game.Systems.Combat;
using BulletHeaven.Game.Weapons;
using Raylib_cs;

namespace BulletHeaven.Game.Systems;

public sealed class RoomRenderSystem
{
    private readonly World _world;
    private readonly WaveSystem _waveSystem;
    private readonly DamageTextService _damageTexts;
    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<PlayerStatsComponent>? _stats;
    private ComponentStore<RenderColorComponent>? _colors;
    private ComponentStore<EquipmentComponent>? _equipments;
    private ComponentStore<HealthComponent>? _healths;

    public RoomRenderSystem(World world, WaveSystem waveSystem, DamageTextService damageTexts)
    {
        _world = world;
        _waveSystem = waveSystem;
        _damageTexts = damageTexts;
        _positions = world.GetStore<PositionComponent>();
        _players = world.GetStore<PlayerComponent>();
        _stats = world.GetStore<PlayerStatsComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        _equipments = world.GetStore<EquipmentComponent>();
        _healths = world.GetStore<HealthComponent>();
    }

    public void Draw(Camera2D camera)
    {
        Raylib.BeginMode2D(camera);

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

        bool hasPlayers = _players != null;
        bool hasStats = _stats != null;
        bool hasEquipments = _equipments != null;
        bool hasHealths = _healths != null;

        float zoom = camera.Zoom > 0.001f ? camera.Zoom : 1f;
        float viewW = 1280f / zoom;
        float viewH = 720f / zoom;
        float viewLeft = camera.Target.X - (camera.Offset.X / zoom);
        float viewTop = camera.Target.Y - (camera.Offset.Y / zoom);
        float viewRight = viewLeft + viewW;
        float viewBottom = viewTop + viewH;

        if (_positions != null && _colors != null)
        {
            foreach (Entity e in _world.Query().With<PositionComponent>().With<RenderColorComponent>())
            {
                ref readonly PositionComponent pos = ref _positions.Get(e);
                ref readonly RenderColorComponent col = ref _colors.Get(e);
                bool isPlayer = hasPlayers && _players!.Has(e);

                if (!isPlayer)
                {
                    if (pos.X + col.Radius < viewLeft - 32f || pos.X - col.Radius > viewRight + 32f ||
                        pos.Y + col.Radius < viewTop - 32f || pos.Y - col.Radius > viewBottom + 32f)
                    {
                        continue;
                    }
                }

                Color bodyColor = new(col.R, col.G, col.B, col.A);

                if (isPlayer)
                {
                    ref readonly PlayerComponent player = ref _players!.Get(e);

                    if (player.FlashTimer > 0f)
                    {
                        bodyColor = new Color(255, 90, 90, 255);
                    }
                    else if (player.IsInvulnerable)
                    {
                        int blink = (int)(player.InvulnerabilityTimer * 28f) % 2;
                        byte alpha = blink == 0 ? (byte)100 : (byte)245;
                        bodyColor = new Color(col.R, col.G, col.B, alpha);
                    }

                    Raylib.DrawCircle((int)pos.X, (int)pos.Y, col.Radius, bodyColor);
                    Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, col.Radius, new Color(255, 255, 255, 180));
                    Raylib.DrawCircle((int)pos.X, (int)pos.Y, 4f, new Color(255, 255, 255, 240));

                    float pointerDist = col.Radius - 2f;
                    float px = pos.X + MathF.Cos(player.FacingAngle) * pointerDist;
                    float py = pos.Y + MathF.Sin(player.FacingAngle) * pointerDist;
                    Raylib.DrawCircle((int)px, (int)py, 3f, new Color(255, 220, 80, 255));

                    if (hasStats && _stats!.Has(e))
                    {
                        ref readonly PlayerStatsComponent s = ref _stats.Get(e);
                        if (s.IsStationary)
                        {
                            Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, col.Radius + 6f, new Color(130, 255, 160, 180));
                        }
                        if (s.IsBursting)
                        {
                            Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, col.Radius + 9f, new Color(80, 220, 255, 220));
                        }
                    }

                    if (hasEquipments && _equipments!.Has(e))
                    {
                        var equip = _equipments.Get(e);
                        for (int s = 0; s < equip.Slots.Length; s++)
                        {
                            var slot = equip.Slots[s];
                            if (slot.VisualTimer > 0f && slot.Weapon != null && slot.Weapon.Definition.Type == WeaponType.Melee)
                            {
                                float halfArc = (slot.Weapon.Definition.MeleeArcDegrees * 0.5f) * (MathF.PI / 180f);
                                float startDeg = (slot.AttackAngle - halfArc) * (180f / MathF.PI);
                                float endDeg = (slot.AttackAngle + halfArc) * (180f / MathF.PI);
                                int alpha = (int)((slot.VisualTimer / slot.VisualDuration) * 220f);
                                Raylib.DrawCircleSectorLines(new Vector2(pos.X, pos.Y), slot.AttackRange, startDeg, endDeg, 16, new Color(245, 230, 210, alpha));
                                Raylib.DrawCircleSectorLines(new Vector2(pos.X, pos.Y), slot.AttackRange * 0.82f, startDeg, endDeg, 16, new Color(255, 255, 255, (int)(alpha * 0.5f)));
                            }
                        }
                    }

                    if (hasHealths && _healths!.Has(e))
                    {
                        ref readonly HealthComponent hp = ref _healths.Get(e);
                        float maxHp = MathF.Max(1f, hp.Max);
                        float fraction = Math.Clamp(hp.Current / maxHp, 0f, 1f);

                        float barW = 34f;
                        float barH = 5f;
                        float barX = pos.X - barW * 0.5f;
                        float barY = pos.Y - col.Radius - 12f;

                        Raylib.DrawRectangle((int)barX - 1, (int)barY - 1, (int)barW + 2, (int)barH + 2, new Color(15, 18, 25, 220));
                        Raylib.DrawRectangle((int)barX, (int)barY, (int)barW, (int)barH, new Color(40, 20, 25, 200));

                        Color hpColor;
                        if (fraction > 0.5f)
                        {
                            hpColor = new Color(80, 230, 110, 240);
                        }
                        else if (fraction > 0.25f)
                        {
                            hpColor = new Color(245, 195, 65, 240);
                        }
                        else
                        {
                            hpColor = new Color(245, 65, 65, 240);
                        }

                        int fillW = (int)(barW * fraction);
                        if (fillW > 0)
                        {
                            Raylib.DrawRectangle((int)barX, (int)barY, fillW, (int)barH, hpColor);
                        }
                        Raylib.DrawRectangleLines((int)barX - 1, (int)barY - 1, (int)barW + 2, (int)barH + 2, new Color(120, 135, 160, 120));
                    }
                }
                else
                {
                    Raylib.DrawCircle((int)pos.X, (int)pos.Y, col.Radius, bodyColor);
                }
            }
        }

        for (int i = 0; i < WeaponSystem.ActiveArcs.Count; i++)
        {
            var arc = WeaponSystem.ActiveArcs[i];
            float alphaRatio = Math.Clamp(arc.Timer / MathF.Max(0.001f, arc.MaxTimer), 0f, 1f);
            int arcAlpha = (int)(255 * alphaRatio);
            Raylib.DrawLineEx(arc.From, arc.To, 3f, new Color(80, 200, 255, arcAlpha));
            Raylib.DrawLineEx(arc.From, arc.To, 1.2f, new Color(240, 250, 255, arcAlpha));
        }

        _damageTexts.Draw();

        Raylib.EndMode2D();

        if (hasHealths && hasPlayers)
        {
            foreach (Entity p in _world.Query().With<PlayerComponent>().With<HealthComponent>())
            {
                ref readonly HealthComponent php = ref _healths!.Get(p);
                float maxH = MathF.Max(1f, php.Max);
                float frac = Math.Clamp(php.Current / maxH, 0f, 1f);

                int hudBarX = 24;
                int hudBarY = 20;
                int hudBarW = 200;
                int hudBarH = 20;

                Raylib.DrawRectangle(hudBarX - 1, hudBarY - 1, hudBarW + 2, hudBarH + 2, new Color(15, 18, 25, 230));
                Raylib.DrawRectangle(hudBarX, hudBarY, hudBarW, hudBarH, new Color(45, 20, 25, 220));

                Color barColor = frac > 0.5f ? new Color(75, 225, 105, 240) : (frac > 0.25f ? new Color(245, 190, 60, 240) : new Color(245, 60, 60, 240));
                int currentFill = (int)(hudBarW * frac);
                if (currentFill > 0)
                {
                    Raylib.DrawRectangle(hudBarX, hudBarY, currentFill, hudBarH, barColor);
                }
                Raylib.DrawRectangleLines(hudBarX - 1, hudBarY - 1, hudBarW + 2, hudBarH + 2, new Color(140, 160, 190, 140));

                string hpString = $"HP {MathF.Ceiling(php.Current)} / {MathF.Round(php.Max)}";
                int hpTextW = Raylib.MeasureText(hpString, 12);
                Raylib.DrawText(hpString, hudBarX + (hudBarW - hpTextW) / 2, hudBarY + 4, 12, Color.White);
                break;
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
    }
}
