using System;
using System.Globalization;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.Game.Arena;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Components.AI;
using BulletHeaven.Game.Rendering;
using BulletHeaven.Game.Systems.Combat;
using BulletHeaven.Game.Waves;
using BulletHeaven.Game.Weapons;
using Raylib_cs;

namespace BulletHeaven.Game.Systems;

public sealed class RoomRenderSystem
{
    private readonly World _world;
    private readonly WaveSystem _waveSystem;
    private readonly DamageTextService _damageTexts;
    private readonly WeaponSystem? _weaponSystem;

    private readonly ComponentStore<PositionComponent>? _positions;
    private readonly ComponentStore<PlayerComponent>? _players;
    private readonly ComponentStore<RenderColorComponent>? _colors;
    private readonly ComponentStore<EquipmentComponent>? _equipments;
    private readonly ComponentStore<HealthComponent>? _healths;
    private readonly ComponentStore<ProjectileComponent>? _projectiles;
    private readonly ComponentStore<ProjectileVisualComponent>? _projVisuals;
    private readonly ComponentStore<HazardZoneComponent>? _hazards;
    private readonly ComponentStore<OrbitProjectileComponent>? _orbits;
    private readonly ComponentStore<BoomerangComponent>? _boomerangs;
    private readonly ComponentStore<EnemyComponent>? _enemies;
    private readonly ComponentStore<ChargerComponent>? _chargers;
    private readonly ComponentStore<EnemyProjectileComponent>? _enemyProjectiles;
    private readonly ComponentStore<XpOrbComponent>? _xpOrbs;

    public RoomRenderSystem(
        World world,
        WaveSystem waveSystem,
        DamageTextService damageTexts,
        WeaponSystem? weaponSystem = null)
    {
        _world = world;
        _waveSystem = waveSystem;
        _damageTexts = damageTexts;
        _weaponSystem = weaponSystem;

        _positions = world.GetStore<PositionComponent>();
        _players = world.GetStore<PlayerComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        _equipments = world.GetStore<EquipmentComponent>();
        _healths = world.GetStore<HealthComponent>();
        _projectiles = world.GetStore<ProjectileComponent>();
        _projVisuals = world.GetStore<ProjectileVisualComponent>();
        _hazards = world.GetStore<HazardZoneComponent>();
        _orbits = world.GetStore<OrbitProjectileComponent>();
        _boomerangs = world.GetStore<BoomerangComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _chargers = world.GetStore<ChargerComponent>();
        _enemyProjectiles = world.GetStore<EnemyProjectileComponent>();
        _xpOrbs = world.GetStore<XpOrbComponent>();
    }

    public void Draw(Camera2D camera)
    {
        float zoom = camera.Zoom > 0.001f ? camera.Zoom : 1f;
        float viewW = 1280f / zoom;
        float viewH = 720f / zoom;
        float viewLeft = camera.Target.X - (camera.Offset.X / zoom);
        float viewTop = camera.Target.Y - (camera.Offset.Y / zoom);
        float viewRight = viewLeft + viewW;
        float viewBottom = viewTop + viewH;

        Raylib.BeginMode2D(camera);

        // ============================================================
        // PASS 1: Arena Floor, Grid, Walls & Floor-Layer Weapon Visuals
        // ============================================================
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

        // Pass 1a: Movement Trail hazard zones (pulsing green/yellow circles)
        if (_positions != null && _hazards != null)
        {
            foreach (Entity hzEntity in _world.Query().With<PositionComponent>().With<HazardZoneComponent>())
            {
                ref readonly PositionComponent pos = ref _positions.Get(hzEntity);
                ref readonly HazardZoneComponent hz = ref _hazards.Get(hzEntity);

                float pad = hz.Radius + 8f;
                if (pos.X + pad < viewLeft || pos.X - pad > viewRight ||
                    pos.Y + pad < viewTop || pos.Y - pad > viewBottom)
                {
                    continue;
                }

                float lifeRatio = Math.Clamp(hz.RemainingDuration / MathF.Max(0.001f, hz.MaxDuration), 0f, 1f);
                float age = hz.MaxDuration - hz.RemainingDuration;
                float pulse = 0.5f + 0.5f * MathF.Sin(age * 9f);
                float fade = Math.Clamp(lifeRatio * 1.4f, 0f, 1f);

                byte cr = (byte)(110 + (225 - 110) * pulse);
                byte cg = 235;
                byte cb = (byte)(85 + (75 - 85) * pulse);

                float r = hz.Radius * (0.90f + 0.12f * pulse);
                Vector2 center = new(pos.X, pos.Y);

                Raylib.DrawCircleV(center, r, new Color((int)cr, (int)cg, (int)cb, (int)((65 + 40 * pulse) * fade)));
                Raylib.DrawCircleV(center, r * 0.55f, new Color(220, 250, 110, (int)(55 * fade)));
                Raylib.DrawCircleLinesV(center, r, new Color((int)cr, (int)cg, (int)cb, (int)((150 + 65 * pulse) * fade)));
            }
        }

        // Pass 1b: Outer Ring subtle 155px circle indicator around player
        if (_positions != null && _players != null && _equipments != null)
        {
            foreach (Entity p in _world.Query().With<PositionComponent>().With<PlayerComponent>().With<EquipmentComponent>())
            {
                ref readonly PositionComponent pPos = ref _positions.Get(p);
                ref readonly EquipmentComponent equip = ref _equipments.Get(p);
                for (int s = 0; s < equip.Slots.Length; s++)
                {
                    var slot = equip.Slots[s];
                    var weapon = slot.Weapon;
                    if (weapon != null && weapon.Definition.Type == WeaponType.OuterRing)
                    {
                        float orbitRadius = weapon.Definition.OrbitRadius;
                        Vector2 pCenter = new(pPos.X, pPos.Y);
                        bool bladeStorm = slot.SpecialActiveTimer > 0f;
                        Color ringFill = bladeStorm ? new Color(255, 220, 90, 48) : new Color(80, 200, 255, 22);
                        Color ringLine = bladeStorm ? new Color(255, 235, 130, 175) : new Color(105, 220, 255, 65);
                        Raylib.DrawRing(pCenter, orbitRadius - 2.5f, orbitRadius + 2.5f, 0f, 360f, 48, ringFill);
                        Raylib.DrawCircleLinesV(pCenter, orbitRadius, ringLine);
                        break;
                    }
                }
            }
        }

        // Pass 1c: Piercing Line subtle aiming lines & Special Explosions
        if (_weaponSystem != null)
        {
            var aimLines = _weaponSystem.ActiveAimLines;
            for (int i = 0; i < aimLines.Count; i++)
            {
                var line = aimLines[i];
                float ratio = Math.Clamp(line.Timer / MathF.Max(0.001f, line.MaxTimer), 0f, 1f);
                Raylib.DrawLineEx(line.From, line.To, 2.2f, new Color(90, 235, 255, (int)(135 * ratio)));
                Raylib.DrawLineEx(line.From, line.To, 0.9f, new Color(235, 255, 255, (int)(105 * ratio)));
            }

            var explosions = _weaponSystem.ActiveExplosions;
            for (int i = 0; i < explosions.Count; i++)
            {
                var exp = explosions[i];
                float ratio = Math.Clamp(exp.Timer / MathF.Max(0.001f, exp.MaxTimer), 0f, 1f);
                float progress = 1f - ratio;
                float r = exp.Radius * (0.45f + 0.60f * progress);
                Raylib.DrawCircleV(exp.Center, r, new Color(exp.Color.R, exp.Color.G, exp.Color.B, (byte)(95 * ratio)));
                Raylib.DrawCircleLinesV(exp.Center, r, new Color(255, 250, 180, (int)(220 * ratio)));
                Raylib.DrawCircleLinesV(exp.Center, r * 0.72f, new Color(exp.Color.R, exp.Color.G, exp.Color.B, (byte)(170 * ratio)));
            }
        }

        // Pass 1d: Charger telegraph bright red floor warning line
        if (_positions != null && _chargers != null)
        {
            foreach (Entity chEntity in _world.Query().With<PositionComponent>().With<ChargerComponent>())
            {
                ref readonly ChargerComponent charger = ref _chargers.Get(chEntity);
                if (charger.State != EnemyState.Telegraph) continue;
                if (_healths != null && _healths.Has(chEntity) && _healths.Get(chEntity).IsDead) continue;

                ref readonly PositionComponent pos = ref _positions.Get(chEntity);

                float dirX = charger.DirX;
                float dirY = charger.DirY;
                float lenSq = dirX * dirX + dirY * dirY;
                if (lenSq > 0.0001f)
                {
                    float inv = 1f / MathF.Sqrt(lenSq);
                    dirX *= inv;
                    dirY *= inv;
                }
                else
                {
                    dirX = 1f;
                    dirY = 0f;
                }

                float lineLen = MathF.Max(280f, charger.ChargeSpeed * 0.75f);
                Vector2 start = new(pos.X, pos.Y);
                Vector2 end = new(pos.X + dirX * lineLen, pos.Y + dirY * lineLen);

                float minLineX = MathF.Min(start.X, end.X);
                float maxLineX = MathF.Max(start.X, end.X);
                float minLineY = MathF.Min(start.Y, end.Y);
                float maxLineY = MathF.Max(start.Y, end.Y);
                if (maxLineX < viewLeft - 32f || minLineX > viewRight + 32f ||
                    maxLineY < viewTop - 32f || minLineY > viewBottom + 32f)
                {
                    continue;
                }

                Raylib.DrawLineEx(start, end, 10f, new Color(255, 30, 30, 80));
                Raylib.DrawLineEx(start, end, 3.8f, new Color(255, 45, 45, 235));
                Raylib.DrawLineEx(start, end, 1.5f, new Color(255, 200, 200, 195));
            }
        }

        // Pass 1e: XP Orbs (distinct bright mint-emerald 4-sided diamonds)
        if (_positions != null && _xpOrbs != null)
        {
            foreach (Entity xpEntity in _world.Query().With<PositionComponent>().With<XpOrbComponent>())
            {
                ref readonly PositionComponent pos = ref _positions.Get(xpEntity);
                if (pos.X < viewLeft - 24f || pos.X > viewRight + 24f ||
                    pos.Y < viewTop - 24f || pos.Y > viewBottom + 24f)
                {
                    continue;
                }

                ref readonly XpOrbComponent orb = ref _xpOrbs.Get(xpEntity);
                Vector2 center = new(pos.X, pos.Y);
                float glowR = orb.IsMagnetized ? 11f : 8.5f;
                byte glowAlpha = orb.IsMagnetized ? (byte)115 : (byte)55;

                Raylib.DrawCircleV(center, glowR, new Color((byte)35, (byte)255, (byte)175, glowAlpha));
                Raylib.DrawPoly(center, 4, 6.2f, 0f, new Color(45, 255, 175, 245));
                Raylib.DrawPoly(center, 4, 3.0f, 0f, new Color(230, 255, 245, 255));
                Raylib.DrawPolyLinesEx(center, 4, 6.2f, 0f, 1.2f, new Color(185, 255, 230, 230));
            }
        }

        // ============================================================
        // PASS 2: Enemies & Non-Projectile Colored Entities
        // ============================================================
        if (_positions != null && _colors != null)
        {
            foreach (Entity e in _world.Query()
                .With<PositionComponent>()
                .With<RenderColorComponent>()
                .Without<PlayerComponent>()
                .Without<ProjectileComponent>()
                .Without<ProjectileVisualComponent>()
                .Without<OrbitProjectileComponent>()
                .Without<HazardZoneComponent>()
                .Without<EnemyProjectileComponent>()
                .Without<XpOrbComponent>())
            {
                ref readonly PositionComponent pos = ref _positions.Get(e);
                ref readonly RenderColorComponent col = ref _colors.Get(e);

                if (pos.X + col.Radius < viewLeft - 32f || pos.X - col.Radius > viewRight + 32f ||
                    pos.Y + col.Radius < viewTop - 32f || pos.Y - col.Radius > viewBottom + 32f)
                {
                    continue;
                }

                Vector2 center = new(pos.X, pos.Y);
                Color bodyColor = new(col.R, col.G, col.B, col.A);
                Raylib.DrawCircleV(center, col.Radius, bodyColor);

                if (_enemies != null && _enemies.Has(e))
                {
                    switch (_enemies.Get(e).Type)
                    {
                        case EnemyType.Swarmer:
                            Raylib.DrawCircleLinesV(center, col.Radius, new Color(255, 130, 90, 220));
                            break;

                        case EnemyType.Charger:
                        {
                            bool telegraphing = _chargers != null && _chargers.Has(e) && _chargers.Get(e).State == EnemyState.Telegraph;
                            Color rimColor = telegraphing ? new Color(255, 70, 50, 255) : new Color(255, 230, 110, 235);
                            Raylib.DrawCircleLinesV(center, col.Radius, rimColor);
                            Raylib.DrawCircleLinesV(center, MathF.Max(2f, col.Radius - 2f), new Color(255, 185, 55, 180));
                            break;
                        }

                        case EnemyType.Spitter:
                            Raylib.DrawCircleV(center, col.Radius * 0.42f, new Color(95, 20, 145, 235));
                            Raylib.DrawCircleLinesV(center, col.Radius, new Color(235, 150, 255, 235));
                            break;
                    }
                }
            }
        }

        // ============================================================
        // PASS 3: Player, Forward Cone Fan-Slash, Broadside Muzzle & HP
        // ============================================================
        if (_positions != null && _players != null && _colors != null)
        {
            bool hasEquipments = _equipments != null;
            bool hasHealths = _healths != null;

            foreach (Entity e in _world.Query().With<PositionComponent>().With<PlayerComponent>().With<RenderColorComponent>())
            {
                ref readonly PositionComponent pos = ref _positions.Get(e);
                ref readonly RenderColorComponent col = ref _colors.Get(e);
                ref readonly PlayerComponent player = ref _players.Get(e);
                Vector2 playerCenter = new(pos.X, pos.Y);

                // Weapon 1 & Weapon 6 player-anchored attack visuals
                if (hasEquipments && _equipments!.Has(e))
                {
                    ref readonly EquipmentComponent equip = ref _equipments.Get(e);
                    for (int s = 0; s < equip.Slots.Length; s++)
                    {
                        var slot = equip.Slots[s];
                        if (slot.VisualTimer <= 0f || slot.Weapon == null) continue;

                        float t = Math.Clamp(slot.VisualTimer / MathF.Max(0.001f, slot.VisualDuration), 0f, 1f);

                        if (slot.Weapon.Definition.Type == WeaponType.ForwardCone)
                        {
                            bool isSpecialCone = slot.SpecialVisualTimer > 0f;
                            float arcDeg = isSpecialCone ? 140f : slot.Weapon.Definition.MeleeArcDegrees;
                            float halfArcRad = (arcDeg * 0.5f) * (MathF.PI / 180f);
                            float startAngle = slot.AttackAngle - halfArcRad;
                            float endAngle = slot.AttackAngle + halfArcRad;
                            float outerR = slot.AttackRange;
                            float innerR = outerR * (isSpecialCone ? 0.65f : 0.78f);
                            const int segments = 16;

                            Color wedgeFill = isSpecialCone
                                ? new Color(100, 240, 255, (int)(95 * t))
                                : new Color(255, 225, 130, (int)(75 * t));
                            Color outerArcColor = isSpecialCone
                                ? new Color(255, 240, 120, (int)(255 * t))
                                : new Color(255, 240, 180, (int)(245 * t));
                            Color innerArcColor = isSpecialCone
                                ? new Color(140, 250, 255, (int)(220 * t))
                                : new Color(255, 255, 255, (int)(170 * t));
                            Color edgeColor = isSpecialCone
                                ? new Color(255, 225, 110, (int)(200 * t))
                                : new Color(255, 220, 120, (int)(150 * t));

                            Vector2 prevOuter = new(
                                pos.X + MathF.Cos(startAngle) * outerR,
                                pos.Y + MathF.Sin(startAngle) * outerR);
                            Vector2 prevInner = new(
                                pos.X + MathF.Cos(startAngle) * innerR,
                                pos.Y + MathF.Sin(startAngle) * innerR);

                            Raylib.DrawLineEx(playerCenter, prevOuter, isSpecialCone ? 2.6f : 1.5f, edgeColor);

                            for (int seg = 1; seg <= segments; seg++)
                            {
                                float frac = seg / (float)segments;
                                float a = startAngle + (endAngle - startAngle) * frac;
                                float cosA = MathF.Cos(a);
                                float sinA = MathF.Sin(a);

                                Vector2 nextOuter = new(pos.X + cosA * outerR, pos.Y + sinA * outerR);
                                Vector2 nextInner = new(pos.X + cosA * innerR, pos.Y + sinA * innerR);

                                Raylib.DrawTriangle(playerCenter, nextOuter, prevOuter, wedgeFill);
                                Raylib.DrawLineEx(prevOuter, nextOuter, isSpecialCone ? 6.5f : 4.5f, outerArcColor);
                                Raylib.DrawLineEx(prevInner, nextInner, isSpecialCone ? 3.2f : 2.0f, innerArcColor);

                                prevOuter = nextOuter;
                                prevInner = nextInner;
                            }

                            Raylib.DrawLineEx(playerCenter, prevOuter, isSpecialCone ? 2.6f : 1.5f, edgeColor);
                        }
                        else if (slot.Weapon.Definition.Type == WeaponType.SidewaysBroadside)
                        {
                            float leftAngle = slot.AttackAngle - MathF.PI * 0.5f;
                            float rightAngle = slot.AttackAngle + MathF.PI * 0.5f;
                            float muzzleStart = col.Radius + 2f;
                            float muzzleEnd = col.Radius + 16f;
                            Color flashColor = new(255, 195, 85, (int)(220 * t));

                            Vector2 l0 = new(pos.X + MathF.Cos(leftAngle) * muzzleStart, pos.Y + MathF.Sin(leftAngle) * muzzleStart);
                            Vector2 l1 = new(pos.X + MathF.Cos(leftAngle) * muzzleEnd, pos.Y + MathF.Sin(leftAngle) * muzzleEnd);
                            Vector2 r0 = new(pos.X + MathF.Cos(rightAngle) * muzzleStart, pos.Y + MathF.Sin(rightAngle) * muzzleStart);
                            Vector2 r1 = new(pos.X + MathF.Cos(rightAngle) * muzzleEnd, pos.Y + MathF.Sin(rightAngle) * muzzleEnd);

                            Raylib.DrawLineEx(l0, l1, 3.0f, flashColor);
                            Raylib.DrawLineEx(r0, r1, 3.0f, flashColor);
                        }
                    }
                }

                // Graze zone indicator & Trail Sprint ring
                Color grazeRingColor = player.GrazeFlashTimer > 0f
                    ? new Color(115, 250, 255, 220)
                    : (player.GrazeStacks > 0 ? new Color(255, 215, 95, 85) : new Color(120, 220, 240, 48));
                Raylib.DrawCircleLinesV(playerCenter, PlayerComponent.DefaultGrazeRadius, grazeRingColor);

                if (player.TrailSprintTimer > 0f)
                {
                    Raylib.DrawCircleLinesV(playerCenter, col.Radius + 5f, new Color(165, 255, 95, 225));
                }

                // Player body
                Color bodyColor = new(col.R, col.G, col.B, col.A);
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

                Raylib.DrawCircleV(playerCenter, col.Radius, bodyColor);
                Raylib.DrawCircleLinesV(playerCenter, col.Radius, new Color(255, 255, 255, 180));
                float coreRadius = player.HitboxRadius > 0f ? player.HitboxRadius : PlayerComponent.CoreHitboxRadius;
                Raylib.DrawCircleV(playerCenter, coreRadius, new Color(255, 255, 255, 235));
                Raylib.DrawCircleLinesV(playerCenter, coreRadius, new Color(255, 75, 100, 240));

                float pointerDist = col.Radius - 2f;
                Vector2 pointerPos = new(
                    pos.X + MathF.Cos(player.FacingAngle) * pointerDist,
                    pos.Y + MathF.Sin(player.FacingAngle) * pointerDist);
                Raylib.DrawCircleV(pointerPos, 3f, new Color(255, 220, 80, 255));

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

                    Color hpColor = fraction > 0.5f
                        ? new Color(80, 230, 110, 240)
                        : (fraction > 0.25f ? new Color(245, 195, 65, 240) : new Color(245, 65, 65, 240));

                    int fillW = (int)(barW * fraction);
                    if (fillW > 0)
                    {
                        Raylib.DrawRectangle((int)barX, (int)barY, fillW, (int)barH, hpColor);
                    }
                    Raylib.DrawRectangleLines((int)barX - 1, (int)barY - 1, (int)barW + 2, (int)barH + 2, new Color(120, 135, 160, 120));
                }
            }
        }

        // ============================================================
        // PASS 4: Projectiles, Boomerangs & Orbiting Orbs
        // ============================================================
        if (_positions != null && _orbits != null)
        {
            foreach (Entity orbEntity in _world.Query().With<PositionComponent>().With<OrbitProjectileComponent>())
            {
                ref readonly PositionComponent pos = ref _positions.Get(orbEntity);
                if (pos.X < viewLeft - 32f || pos.X > viewRight + 32f ||
                    pos.Y < viewTop - 32f || pos.Y > viewBottom + 32f)
                {
                    continue;
                }

                ref readonly OrbitProjectileComponent orb = ref _orbits.Get(orbEntity);
                bool isBladeStorm = false;
                if (_equipments != null && _equipments.Has(orb.Owner))
                {
                    var ownerEquip = _equipments.Get(orb.Owner);
                    if (ownerEquip.Slots != null && orb.SlotIndex >= 0 && orb.SlotIndex < ownerEquip.Slots.Length)
                    {
                        isBladeStorm = ownerEquip.Slots[orb.SlotIndex].SpecialActiveTimer > 0f;
                    }
                }

                Vector2 orbPos = new(pos.X, pos.Y);
                float baseR = MathF.Max(7f, orb.OrbRadius * 0.72f);
                if (isBladeStorm)
                {
                    Raylib.DrawCircleV(orbPos, baseR + 7f, new Color(255, 220, 90, 95));
                    Raylib.DrawCircleV(orbPos, baseR, new Color(255, 235, 115, 250));
                    Raylib.DrawCircleV(orbPos, baseR * 0.5f, Color.White);
                    Raylib.DrawCircleLinesV(orbPos, baseR + 1f, new Color(140, 250, 255, 240));
                }
                else
                {
                    Raylib.DrawCircleV(orbPos, baseR + 5f, new Color(80, 210, 255, 70));
                    Raylib.DrawCircleV(orbPos, baseR, new Color(110, 235, 255, 240));
                    Raylib.DrawCircleV(orbPos, baseR * 0.5f, new Color(245, 255, 255, 255));
                    Raylib.DrawCircleLinesV(orbPos, baseR, new Color(220, 250, 255, 220));
                }
            }
        }

        if (_positions != null && _projectiles != null)
        {
            foreach (Entity projEntity in _world.Query().With<PositionComponent>().With<ProjectileComponent>())
            {
                ref readonly PositionComponent pos = ref _positions.Get(projEntity);
                if (pos.X < viewLeft - 48f || pos.X > viewRight + 48f ||
                    pos.Y < viewTop - 48f || pos.Y > viewBottom + 48f)
                {
                    continue;
                }

                ref readonly ProjectileComponent proj = ref _projectiles.Get(projEntity);
                ProjectileShape shape = ProjectileShape.Circle;
                ProjectileVisualComponent vis = default;

                if (_projVisuals != null && _projVisuals.Has(projEntity))
                {
                    vis = _projVisuals.Get(projEntity);
                    shape = vis.Shape;
                }

                switch (shape)
                {
                    case ProjectileShape.PiercingLine:
                    {
                        float deg = vis.Angle * (180f / MathF.PI);
                        float glowL = vis.Length + 8f;
                        float glowW = vis.Thickness + 4f;

                        Raylib.DrawRectanglePro(
                            new Rectangle(pos.X, pos.Y, glowL, glowW),
                            new Vector2(glowL * 0.5f, glowW * 0.5f),
                            deg,
                            new Color(60, 230, 255, 190));

                        Raylib.DrawRectanglePro(
                            new Rectangle(pos.X, pos.Y, vis.Length, vis.Thickness),
                            new Vector2(vis.Length * 0.5f, vis.Thickness * 0.5f),
                            deg,
                            new Color(240, 255, 255, 255));

                        Vector2 nose = new(
                            pos.X + MathF.Cos(vis.Angle) * (vis.Length * 0.45f),
                            pos.Y + MathF.Sin(vis.Angle) * (vis.Length * 0.45f));
                        Raylib.DrawCircleV(nose, 3f, Color.White);
                        break;
                    }

                    case ProjectileShape.Boomerang:
                    {
                        float spinDeg = vis.SpinAngle * (180f / MathF.PI);
                        bool isReturning = _boomerangs != null && _boomerangs.Has(projEntity) && _boomerangs.Get(projEntity).IsReturning;
                        float armLen = vis.Length > 0f ? vis.Length : 15f;
                        float armThick = vis.Thickness > 0f ? vis.Thickness : 5f;
                        bool isGiant = armLen > 18f;
                        Color armColor = isGiant
                            ? (isReturning ? new Color(255, 165, 65, 255) : new Color(255, 235, 95, 255))
                            : (isReturning ? new Color(255, 145, 55, 255) : new Color(255, 205, 70, 255));

                        Raylib.DrawCircleLinesV(new Vector2(pos.X, pos.Y), armLen - 1f, new Color(255, 225, 130, isGiant ? 155 : 85));
                        Raylib.DrawRectanglePro(new Rectangle(pos.X, pos.Y, armLen, armThick), new Vector2(armThick * 0.3f, armThick * 0.5f), spinDeg, armColor);
                        Raylib.DrawRectanglePro(new Rectangle(pos.X, pos.Y, armLen, armThick), new Vector2(armThick * 0.3f, armThick * 0.5f), spinDeg + 115f, armColor);
                        if (isGiant)
                        {
                            Raylib.DrawRectanglePro(new Rectangle(pos.X, pos.Y, armLen, armThick), new Vector2(armThick * 0.3f, armThick * 0.5f), spinDeg + 230f, armColor);
                        }
                        Raylib.DrawCircleV(new Vector2(pos.X, pos.Y), isGiant ? 5f : 3f, new Color(255, 252, 225, 255));
                        break;
                    }

                    case ProjectileShape.BroadsideBolt:
                    {
                        float deg = vis.Angle * (180f / MathF.PI);
                        Raylib.DrawRectanglePro(
                            new Rectangle(pos.X, pos.Y, vis.Length, vis.Thickness),
                            new Vector2(vis.Length * 0.5f, vis.Thickness * 0.5f),
                            deg,
                            new Color(255, 165, 55, 255));

                        Raylib.DrawRectanglePro(
                            new Rectangle(pos.X, pos.Y, vis.Length * 0.65f, vis.Thickness * 0.45f),
                            new Vector2(vis.Length * 0.325f, vis.Thickness * 0.225f),
                            deg,
                            new Color(255, 245, 195, 255));
                        break;
                    }

                    default:
                    {
                        Color col = Color.White;
                        float r = proj.Radius;
                        if (_colors != null && _colors.Has(projEntity))
                        {
                            ref readonly RenderColorComponent rc = ref _colors.Get(projEntity);
                            col = new Color(rc.R, rc.G, rc.B, rc.A);
                            r = rc.Radius;
                        }
                        Raylib.DrawCircleV(new Vector2(pos.X, pos.Y), r, col);
                        break;
                    }
                }
            }
        }

        if (_positions != null && _enemyProjectiles != null)
        {
            foreach (Entity epEntity in _world.Query().With<PositionComponent>().With<EnemyProjectileComponent>())
            {
                ref readonly PositionComponent pos = ref _positions.Get(epEntity);
                ref readonly EnemyProjectileComponent proj = ref _enemyProjectiles.Get(epEntity);
                if (proj.Lifetime <= 0f) continue;

                float r = MathF.Max(4f, proj.Radius);
                float pad = r + 16f;
                if (pos.X < viewLeft - pad || pos.X > viewRight + pad ||
                    pos.Y < viewTop - pad || pos.Y > viewBottom + pad)
                {
                    continue;
                }

                Vector2 bPos = new(pos.X, pos.Y);
                Raylib.DrawCircleV(bPos, r + 3.5f, new Color(185, 55, 255, 85));
                Raylib.DrawCircleV(bPos, r, new Color(205, 75, 255, 255));
                Raylib.DrawCircleV(bPos, r * 0.5f, new Color(250, 225, 255, 255));
                Raylib.DrawCircleLinesV(bPos, r, new Color(245, 185, 255, 240));
            }
        }

        // ============================================================
        // PASS 5: World Damage Texts & Screen-Space Top HUD
        // ============================================================
        _damageTexts.Draw();

        Raylib.EndMode2D();

        DrawTopHud();
    }

    private void DrawTopHud()
    {
        Entity? activePlayer = null;
        if (_players != null)
        {
            foreach (Entity p in _world.Query().With<PlayerComponent>())
            {
                activePlayer = p;
                break;
            }
        }

        // 1. Left Panel: HP bar + XP bar (x = 20..250)
        const int leftX = 20;
        const int leftW = 230;

        if (activePlayer.HasValue && _healths != null && _healths.Has(activePlayer.Value))
        {
            ref readonly HealthComponent php = ref _healths.Get(activePlayer.Value);
            float maxH = MathF.Max(1f, php.Max);
            float frac = Math.Clamp(php.Current / maxH, 0f, 1f);

            const int hpY = 14;
            const int hpH = 20;
            Raylib.DrawRectangle(leftX - 1, hpY - 1, leftW + 2, hpH + 2, new Color(15, 18, 25, 230));
            Raylib.DrawRectangle(leftX, hpY, leftW, hpH, new Color(45, 20, 25, 220));

            Color barColor = frac > 0.5f
                ? new Color(75, 225, 105, 240)
                : (frac > 0.25f ? new Color(245, 190, 60, 240) : new Color(245, 60, 60, 240));
            int currentFill = (int)(leftW * frac);
            if (currentFill > 0)
            {
                Raylib.DrawRectangle(leftX, hpY, currentFill, hpH, barColor);
            }
            Raylib.DrawRectangleLines(leftX - 1, hpY - 1, leftW + 2, hpH + 2, new Color(140, 160, 190, 150));

            string hpString = $"HP {MathF.Ceiling(php.Current)} / {MathF.Round(php.Max)}";
            int hpTextW = Raylib.MeasureText(hpString, 12);
            int hpTx = leftX + (leftW - hpTextW) / 2;
            Raylib.DrawText(hpString, hpTx + 1, hpY + 5, 12, new Color(0, 0, 0, 180));
            Raylib.DrawText(hpString, hpTx, hpY + 4, 12, Color.White);
        }

        if (activePlayer.HasValue && _players != null && _players.Has(activePlayer.Value))
        {
            ref readonly PlayerComponent pc = ref _players.Get(activePlayer.Value);
            int level = Math.Max(1, pc.Level);
            int xp = Math.Max(0, pc.Xp);
            int xpNext = Math.Max(1, pc.XpToNextLevel);
            float xpFrac = Math.Clamp(xp / (float)xpNext, 0f, 1f);

            const int xpY = 40;
            const int xpH = 22;
            Raylib.DrawRectangle(leftX - 1, xpY - 1, leftW + 2, xpH + 2, new Color(12, 18, 24, 235));
            Raylib.DrawRectangle(leftX, xpY, leftW, xpH, new Color(14, 24, 30, 235));

            int xpFill = (int)(leftW * xpFrac);
            if (xpFill > 0)
            {
                Raylib.DrawRectangle(leftX, xpY, xpFill, xpH, new Color(40, 215, 180, 240));
            }
            Raylib.DrawRectangleLines(leftX - 1, xpY - 1, leftW + 2, xpH + 2, new Color(85, 205, 195, 180));

            string xpString = $"LVL {level} - XP {xp}/{xpNext}";
            int xpTextW = Raylib.MeasureText(xpString, 12);
            int xpTx = leftX + (leftW - xpTextW) / 2;
            Raylib.DrawText(xpString, xpTx + 1, xpY + 6, 12, new Color(0, 0, 0, 190));
            Raylib.DrawText(xpString, xpTx, xpY + 5, 12, Color.White);

            // Graze Stack bar & 3 stack pips (y = 66..90)
            const int grY = 68;
            const int grH = 22;
            int stacks = Math.Clamp(pc.GrazeStacks, 0, PlayerComponent.MaxGrazeStacks);
            int gProg = Math.Clamp(pc.GrazeProgress, 0, PlayerComponent.GrazesPerStack);
            float gFrac = stacks >= PlayerComponent.MaxGrazeStacks ? 1f : gProg / (float)PlayerComponent.GrazesPerStack;

            Raylib.DrawRectangle(leftX - 1, grY - 1, leftW + 2, grH + 2, new Color(12, 16, 26, 235));
            Raylib.DrawRectangle(leftX, grY, leftW, grH, new Color(18, 22, 34, 235));

            int gFill = (int)(leftW * gFrac);
            if (gFill > 0)
            {
                Color gColor = stacks >= PlayerComponent.MaxGrazeStacks
                    ? new Color(255, 200, 60, 235)
                    : new Color(70, 210, 245, 215);
                Raylib.DrawRectangle(leftX, grY, gFill, grH, gColor);
            }
            Raylib.DrawRectangleLines(leftX - 1, grY - 1, leftW + 2, grH + 2,
                stacks > 0 ? new Color(255, 215, 85, 220) : new Color(75, 130, 170, 175));

            for (int pip = 0; pip < PlayerComponent.MaxGrazeStacks; pip++)
            {
                bool filled = pip < stacks;
                int px = leftX + 6 + pip * 14;
                int py = grY + 5;
                Raylib.DrawRectangle(px, py, 10, 12, filled ? new Color(255, 220, 75, 255) : new Color(26, 34, 48, 230));
                Raylib.DrawRectangleLines(px, py, 10, 12, filled ? Color.White : new Color(80, 105, 135, 200));
            }

            string grazeStr = stacks >= PlayerComponent.MaxGrazeStacks
                ? "MAX 3/3 [1-3]!"
                : $"GRAZE {stacks}/3 ({gProg}/{PlayerComponent.GrazesPerStack})";
            int gTw = Raylib.MeasureText(grazeStr, 11);
            int gTx = leftX + 52 + ((leftW - 56) - gTw) / 2;
            Raylib.DrawText(grazeStr, gTx + 1, grY + 6, 11, new Color(0, 0, 0, 195));
            Raylib.DrawText(grazeStr, gTx, grY + 5, 11, stacks > 0 ? new Color(255, 245, 190, 255) : Color.White);
        }

        // 2. Center Row 1: Wave | Time | Enemies pill (y = 12..36)
        string waveText = $"WAVE {_waveSystem.CurrentWaveNumber}";
        string timeText = string.Create(CultureInfo.InvariantCulture, $"TIME: {_waveSystem.TimeRemaining:0.0}s");
        string enemiesText = $"ENEMIES: {_waveSystem.ActiveEnemyCount}";
        string hud = $"{waveText}   |   {timeText}   |   {enemiesText}";

        const int hudFontSize = 16;
        int textWidth = Raylib.MeasureText(hud, hudFontSize);
        int pillW = textWidth + 32;
        int pillX = (1280 - pillW) / 2;
        Rectangle pillRect = new(pillX, 12f, pillW, 24f);
        Raylib.DrawRectangleRounded(pillRect, 0.25f, 4, new Color(12, 15, 22, 200));
        Raylib.DrawRectangleRoundedLinesEx(pillRect, 0.25f, 4, 1f, new Color(55, 72, 98, 175));
        Raylib.DrawText(hud, (1280 - textWidth) / 2, 16, hudFontSize, new Color(225, 235, 248, 255));

        // 3. Center Row 2: 3 Weapon Slots (x = 285..995, y = 42..86; leaves x >= 1190 clear for FPS)
        const int slotW = 230;
        const int slotH = 44;
        const int slotGap = 10;
        const int startX = 285;
        const int startY = 42;

        bool hasEquip = activePlayer.HasValue && _equipments != null && _equipments.Has(activePlayer.Value);
        EquipmentComponent equip = hasEquip ? _equipments!.Get(activePlayer!.Value) : default;
        bool hasSpecialStack = activePlayer.HasValue && _players != null && _players.Has(activePlayer.Value) && _players.Get(activePlayer.Value).GrazeStacks > 0;

        for (int s = 0; s < EquipmentComponent.MaxWeaponSlots; s++)
        {
            int sx = startX + s * (slotW + slotGap);
            Rectangle slotRect = new(sx, startY, slotW, slotH);
            WeaponSlot? slot = (hasEquip && equip.Slots != null && s < equip.Slots.Length) ? equip.Slots[s] : null;
            WeaponItem? weapon = slot?.Weapon;

            if (weapon == null)
            {
                Raylib.DrawRectangleRec(slotRect, new Color(13, 17, 25, 195));
                Raylib.DrawRectangleLinesEx(slotRect, 1f, new Color(42, 50, 66, 175));

                string badge = $"[{s + 1}]";
                Raylib.DrawText(badge, sx + 8, startY + 16, 11, new Color(85, 100, 125, 200));

                string emptyLabel = "EMPTY SLOT";
                int ew = Raylib.MeasureText(emptyLabel, 11);
                Raylib.DrawText(emptyLabel, sx + (slotW - ew) / 2 + 8, startY + 16, 11, new Color(95, 110, 135, 200));
                continue;
            }

            bool specialRunning = slot!.SpecialActiveTimer > 0f;
            Color wColor = weapon.Definition.Color;
            Color borderColor = specialRunning
                ? new Color(140, 255, 125, 240)
                : (hasSpecialStack ? new Color(255, 215, 80, 240) : new Color(75, 95, 125, 200));

            Raylib.DrawRectangleRec(slotRect, new Color(13, 17, 25, 225));
            Raylib.DrawRectangleLinesEx(slotRect, (hasSpecialStack || specialRunning) ? 1.8f : 1.2f, borderColor);
            Raylib.DrawRectangle(sx + 1, startY, slotW - 2, 2, wColor);

            // Line 1: Slot key badge + Weapon Name + Optional [SP] badge
            Raylib.DrawRectangle(sx + 6, startY + 5, 16, 15, hasSpecialStack ? new Color(255, 210, 70, 255) : new Color(26, 34, 48, 255));
            Raylib.DrawRectangleLines(sx + 6, startY + 5, 16, 15, hasSpecialStack ? Color.White : wColor);
            string slotNum = $"{s + 1}";
            int snW = Raylib.MeasureText(slotNum, 10);
            Raylib.DrawText(slotNum, sx + 6 + (16 - snW) / 2, startY + 7, 10, hasSpecialStack ? new Color(15, 18, 25, 255) : Color.White);

            int nameMaxW = (hasSpecialStack || specialRunning) ? slotW - 88 : slotW - 34;
            DrawFittedHudText(weapon.Definition.Name, sx + 27, startY + 7, 10, 8, nameMaxW, Color.White);

            if (specialRunning)
            {
                const string actTag = "ACTIVE!";
                int aw = Raylib.MeasureText(actTag, 9);
                Raylib.DrawText(actTag, sx + slotW - aw - 7, startY + 7, 9, new Color(140, 255, 130, 255));
            }
            else if (hasSpecialStack)
            {
                string spTag = $"[{s + 1}] SPEC";
                int spw = Raylib.MeasureText(spTag, 9);
                Raylib.DrawText(spTag, sx + slotW - spw - 7, startY + 7, 9, new Color(255, 225, 95, 255));
            }

            // Line 2: Level label + Cooldown fill bar
            string lvlLabel = $"Lv.{weapon.Level}";
            Raylib.DrawText(lvlLabel, sx + 7, startY + 26, 10, new Color(255, 220, 105, 255));

            const int cdXOffset = 44;
            const int cdW = 178;
            const int cdH = 12;
            int cdX = sx + cdXOffset;
            int cdY = startY + 25;

            Raylib.DrawRectangle(cdX, cdY, cdW, cdH, new Color(20, 26, 38, 235));

            if (weapon.Definition.Type == WeaponType.OuterRing)
            {
                Raylib.DrawRectangle(cdX, cdY, cdW, cdH, specialRunning ? new Color(255, 205, 70, 225) : new Color(55, 205, 235, 185));
                Raylib.DrawRectangleLines(cdX, cdY, cdW, cdH, specialRunning ? new Color(255, 240, 150, 230) : new Color(95, 230, 255, 190));
                string orbitText = specialRunning
                    ? string.Create(CultureInfo.InvariantCulture, $"BLADE STORM {slot.SpecialActiveTimer:0.0}s")
                    : "ORBIT";
                int ow = Raylib.MeasureText(orbitText, 9);
                Raylib.DrawText(orbitText, cdX + (cdW - ow) / 2, cdY + 2, 9, specialRunning ? new Color(15, 18, 25, 255) : Color.White);
            }
            else
            {
                float interval = MathF.Max(0.05f, weapon.AttackInterval);
                float cdRemaining = MathF.Max(0f, slot.CooldownTimer);
                float readyFrac = Math.Clamp(1f - (cdRemaining / interval), 0f, 1f);
                int fillW = (int)(cdW * readyFrac);
                if (fillW > 0)
                {
                    byte alpha = cdRemaining <= 0.01f ? (byte)220 : (byte)155;
                    Raylib.DrawRectangle(cdX, cdY, fillW, cdH, new Color(wColor.R, wColor.G, wColor.B, alpha));
                }
                Raylib.DrawRectangleLines(cdX, cdY, cdW, cdH, new Color(75, 95, 125, 190));

                string cdText = specialRunning
                    ? string.Create(CultureInfo.InvariantCulture, $"SPRINT {slot.SpecialActiveTimer:0.0}s")
                    : (cdRemaining <= 0.01f ? "READY" : string.Create(CultureInfo.InvariantCulture, $"{cdRemaining:0.0}s"));
                int cdTw = Raylib.MeasureText(cdText, 9);
                int cdTx = cdX + (cdW - cdTw) / 2;
                Raylib.DrawText(cdText, cdTx + 1, cdY + 3, 9, new Color(0, 0, 0, 190));
                Raylib.DrawText(cdText, cdTx, cdY + 2, 9, Color.White);
            }
        }
    }

    private static void DrawFittedHudText(string text, int x, int y, int preferredSize, int minSize, int maxWidth, Color color)
    {
        int size = preferredSize;
        while (size > minSize && Raylib.MeasureText(text, size) > maxWidth)
        {
            size--;
        }

        string drawText = text;
        if (Raylib.MeasureText(drawText, size) > maxWidth && drawText.Length > 3)
        {
            while (drawText.Length > 3 && Raylib.MeasureText(drawText + "..", size) > maxWidth)
            {
                drawText = drawText[..^1];
            }
            drawText += "..";
        }

        Raylib.DrawText(drawText, x, y, size, color);
    }
}
