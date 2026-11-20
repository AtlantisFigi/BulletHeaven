using System;
using System.Collections.Generic;
using System.Numerics;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Spatial;
using BulletHeaven.Game.Weapons;
using Raylib_cs;

namespace BulletHeaven.Game.Systems.Combat;

public struct AimLineVisual
{
    public Vector2 From;
    public Vector2 To;
    public float Timer;
    public float MaxTimer;
}

public struct ExplosionVisual
{
    public Vector2 Center;
    public float Radius;
    public float Timer;
    public float MaxTimer;
    public Color Color;
}

public sealed class WeaponSystem : ISystem
{
    public readonly List<AimLineVisual> ActiveAimLines = new();
    public readonly List<ExplosionVisual> ActiveExplosions = new();

    private World? _world;
    private readonly CommandBuffer _cmd;
    private readonly SpatialGrid? _grid;

    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<EquipmentComponent>? _equipments;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<VelocityComponent>? _velocities;
    private ComponentStore<KnockbackComponent>? _knockbacks;
    private ComponentStore<HazardZoneComponent>? _hazards;
    private ComponentStore<EnemyProjectileComponent>? _enemyProjectiles;
    private ComponentStore<RenderColorComponent>? _colors;

    private readonly Dictionary<Entity, Vector2> _lastPlayerPositions = new();

    public WeaponSystem(CommandBuffer cmd, SpatialGrid? grid = null)
    {
        _cmd = cmd;
        _grid = grid;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _players = world.GetStore<PlayerComponent>();
        _equipments = world.GetStore<EquipmentComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _healths = world.GetStore<HealthComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _knockbacks = world.GetStore<KnockbackComponent>();
        _hazards = world.GetStore<HazardZoneComponent>();
        _enemyProjectiles = world.GetStore<EnemyProjectileComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        ClearTransientVisuals();
    }

    public void ClearTransientVisuals()
    {
        ActiveAimLines.Clear();
        ActiveExplosions.Clear();
        _lastPlayerPositions.Clear();
    }

    public void Update(float deltaTime)
    {
        for (int a = ActiveAimLines.Count - 1; a >= 0; a--)
        {
            var line = ActiveAimLines[a];
            line.Timer -= deltaTime;
            if (line.Timer <= 0f)
            {
                ActiveAimLines.RemoveAt(a);
            }
            else
            {
                ActiveAimLines[a] = line;
            }
        }

        for (int e = ActiveExplosions.Count - 1; e >= 0; e--)
        {
            var exp = ActiveExplosions[e];
            exp.Timer -= deltaTime;
            if (exp.Timer <= 0f)
            {
                ActiveExplosions.RemoveAt(e);
            }
            else
            {
                ActiveExplosions[e] = exp;
            }
        }

        if (_world == null || _positions == null || _players == null || _equipments == null) return;

        bool canPollKeyboard = Raylib.IsWindowReady();

        foreach (Entity playerEntity in _world.Query().With<PositionComponent>().With<EquipmentComponent>().With<PlayerComponent>())
        {
            if (_healths != null && _healths.Has(playerEntity) && _healths.Get(playerEntity).IsDead) continue;

            ref readonly PositionComponent playerPos = ref _positions.Get(playerEntity);
            ref PlayerComponent player = ref _players.Get(playerEntity);
            ref readonly EquipmentComponent equip = ref _equipments.Get(playerEntity);

            Vector2 currentPos = new(playerPos.X, playerPos.Y);
            bool isMoving = false;
            if (_velocities != null && _velocities.Has(playerEntity))
            {
                ref readonly VelocityComponent vel = ref _velocities.Get(playerEntity);
                isMoving = vel.X * vel.X + vel.Y * vel.Y > 1f;
            }
            else if (_lastPlayerPositions.TryGetValue(playerEntity, out Vector2 prevPos))
            {
                float pdx = currentPos.X - prevPos.X;
                float pdy = currentPos.Y - prevPos.Y;
                isMoving = pdx * pdx + pdy * pdy > 0.0001f;
            }
            _lastPlayerPositions[playerEntity] = currentPos;

            for (int i = 0; i < equip.Slots.Length; i++)
            {
                var slot = equip.Slots[i];
                if (slot.VisualTimer > 0f)
                {
                    slot.VisualTimer = MathF.Max(0f, slot.VisualTimer - deltaTime);
                }
                if (slot.SpecialActiveTimer > 0f)
                {
                    slot.SpecialActiveTimer = MathF.Max(0f, slot.SpecialActiveTimer - deltaTime);
                }
                if (slot.SpecialVisualTimer > 0f)
                {
                    slot.SpecialVisualTimer = MathF.Max(0f, slot.SpecialVisualTimer - deltaTime);
                }
            }

            if (canPollKeyboard)
            {
                for (int s = 0; s < EquipmentComponent.MaxWeaponSlots && s < equip.Slots.Length; s++)
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.One + s) || Raylib.IsKeyPressed(KeyboardKey.Kp1 + s))
                    {
                        TryActivateSpecial(playerEntity, s);
                    }
                }
            }

            for (int i = 0; i < equip.Slots.Length; i++)
            {
                var slot = equip.Slots[i];
                if (slot.Weapon == null) continue;

                slot.CooldownTimer -= deltaTime;
                var weapon = slot.Weapon;
                var def = weapon.Definition;

                switch (def.Type)
                {
                    case WeaponType.ForwardCone:
                        if (slot.CooldownTimer <= 0f)
                        {
                            slot.CooldownTimer = MathF.Max(0.08f, weapon.AttackInterval);
                            float effectiveRange = weapon.Range;
                            float angle = player.FacingAngle;

                            slot.AttackAngle = angle;
                            slot.AttackRange = effectiveRange;
                            slot.VisualDuration = 0.20f;
                            slot.VisualTimer = slot.VisualDuration;

                            float kbTime = def.KnockbackTime > 0f ? def.KnockbackTime : 0.22f;
                            ExecuteConeStrike(playerPos.X, playerPos.Y, angle, effectiveRange, def.MeleeArcDegrees, weapon.Damage, def.Knockback, kbTime);
                        }
                        break;

                    case WeaponType.PiercingLine:
                        if (slot.CooldownTimer <= 0f)
                        {
                            float effectiveRange = weapon.Range;
                            if (TryFindFurthestEnemy(playerPos.X, playerPos.Y, effectiveRange, out _, out float tx, out float ty))
                            {
                                slot.CooldownTimer = MathF.Max(0.08f, weapon.AttackInterval);
                                float angle = MathF.Atan2(ty - playerPos.Y, tx - playerPos.X);

                                slot.AttackAngle = angle;
                                slot.AttackRange = effectiveRange;
                                slot.VisualDuration = 0.18f;
                                slot.VisualTimer = slot.VisualDuration;

                                ActiveAimLines.Add(new AimLineVisual
                                {
                                    From = new Vector2(playerPos.X, playerPos.Y),
                                    To = new Vector2(tx, ty),
                                    Timer = 0.28f,
                                    MaxTimer = 0.28f
                                });

                                int shotCount = 1 + weapon.BonusProjectiles;
                                ExecutePiercingLine(playerPos.X, playerPos.Y, angle, weapon, weapon.Damage, shotCount);
                            }
                        }
                        break;

                    case WeaponType.OuterRing:
                        // Managed continuously by OrbitSystem
                        break;

                    case WeaponType.MovementTrail:
                        if (slot.CooldownTimer < 0f)
                        {
                            slot.CooldownTimer = 0f;
                        }

                        bool isSprinting = player.TrailSprintTimer > 0f;
                        float interval = MathF.Max(0.05f, isSprinting ? weapon.AttackInterval * 0.5f : weapon.AttackInterval);
                        if (isSprinting && slot.CooldownTimer > interval)
                        {
                            slot.CooldownTimer = interval;
                        }

                        if (isMoving && slot.CooldownTimer <= 0f)
                        {
                            slot.CooldownTimer = interval;
                            float zoneRadius = isSprinting ? weapon.HazardRadius * 1.5f : weapon.HazardRadius;
                            float zoneDamage = isSprinting ? weapon.Damage * 1.5f : weapon.Damage;
                            SpawnHazardZone(playerEntity, playerPos.X, playerPos.Y, weapon, zoneRadius, zoneDamage);
                        }
                        break;

                    case WeaponType.ReturningBoomerang:
                        if (slot.CooldownTimer <= 0f)
                        {
                            float effectiveRange = weapon.Range;
                            if (TryFindClosestEnemy(playerPos.X, playerPos.Y, effectiveRange, out _, out float tx, out float ty))
                            {
                                slot.CooldownTimer = MathF.Max(0.08f, weapon.AttackInterval);
                                float angle = MathF.Atan2(ty - playerPos.Y, tx - playerPos.X);

                                slot.AttackAngle = angle;
                                slot.AttackRange = effectiveRange;
                                slot.VisualDuration = 0.18f;
                                slot.VisualTimer = slot.VisualDuration;

                                int shotCount = 1 + weapon.BonusProjectiles;
                                ExecuteReturningBoomerang(playerEntity, playerPos.X, playerPos.Y, angle, weapon, weapon.Damage, effectiveRange, shotCount);
                            }
                        }
                        break;

                    case WeaponType.SidewaysBroadside:
                        if (slot.CooldownTimer <= 0f)
                        {
                            slot.CooldownTimer = MathF.Max(0.08f, weapon.AttackInterval);
                            float facing = player.FacingAngle;

                            slot.AttackAngle = facing;
                            slot.AttackRange = 24f;
                            slot.VisualDuration = 0.14f;
                            slot.VisualTimer = slot.VisualDuration;

                            ExecuteSidewaysBroadside(playerPos.X, playerPos.Y, facing, weapon, weapon.Damage);
                        }
                        break;
                }
            }
        }
    }

    public bool TryActivateSpecial(Entity playerEntity, int slotIndex)
    {
        if (_world == null || _positions == null || _players == null || _equipments == null) return false;
        if (!_world.IsAlive(playerEntity) || !_positions.Has(playerEntity) || !_players.Has(playerEntity) || !_equipments.Has(playerEntity)) return false;
        if (_healths != null && _healths.Has(playerEntity) && _healths.Get(playerEntity).IsDead) return false;

        ref PlayerComponent player = ref _players.Get(playerEntity);
        if (player.GrazeStacks <= 0) return false;

        ref readonly EquipmentComponent equip = ref _equipments.Get(playerEntity);
        if (equip.Slots == null || slotIndex < 0 || slotIndex >= equip.Slots.Length) return false;

        var slot = equip.Slots[slotIndex];
        var weapon = slot.Weapon;
        if (weapon == null) return false;

        player.GrazeStacks--;
        ref readonly PositionComponent playerPos = ref _positions.Get(playerEntity);

        switch (weapon.Definition.Type)
        {
            case WeaponType.ForwardCone:
                ExecuteConeSpecial(slot, playerPos.X, playerPos.Y, player.FacingAngle, weapon);
                return true;

            case WeaponType.PiercingLine:
                ExecuteSniperSpecial(slot, playerPos.X, playerPos.Y, player.FacingAngle, weapon);
                return true;

            case WeaponType.OuterRing:
                slot.SpecialActiveTimer = 4.0f;
                slot.SpecialVisualDuration = 4.0f;
                slot.SpecialVisualTimer = 4.0f;
                return true;

            case WeaponType.MovementTrail:
                ExecuteTrailSpecial(playerEntity, ref player, slot, playerPos.X, playerPos.Y, weapon);
                return true;

            case WeaponType.ReturningBoomerang:
                ExecuteBoomerangSpecial(playerEntity, slot, playerPos.X, playerPos.Y, player.FacingAngle, weapon);
                return true;

            case WeaponType.SidewaysBroadside:
                ExecuteTwinShotsSpecial(slot, playerPos.X, playerPos.Y, player.FacingAngle, weapon);
                return true;

            default:
                player.GrazeStacks++;
                return false;
        }
    }

    private void ExecuteConeSpecial(WeaponSlot slot, float px, float py, float facingAngle, WeaponItem weapon)
    {
        float specialRange = 250f * (1f + (weapon.Level - 1) * 0.08f);
        const float specialArcDeg = 140f;
        float damage = weapon.Damage * 3.5f;
        const float knockback = 1350f;
        const float kbTime = 0.42f;

        slot.AttackAngle = facingAngle;
        slot.AttackRange = specialRange;
        slot.VisualDuration = 0.35f;
        slot.VisualTimer = 0.35f;
        slot.SpecialVisualDuration = 0.35f;
        slot.SpecialVisualTimer = 0.35f;

        ExecuteConeStrike(px, py, facingAngle, specialRange, specialArcDeg, damage, knockback, kbTime);

        if (_enemyProjectiles != null && _positions != null)
        {
            float halfArc = (specialArcDeg * 0.5f) * (MathF.PI / 180f);
            foreach (Entity epEntity in _world!.Query().With<PositionComponent>().With<EnemyProjectileComponent>())
            {
                ref EnemyProjectileComponent ep = ref _enemyProjectiles.Get(epEntity);
                if (ep.Lifetime <= 0f) continue;

                ref readonly PositionComponent epPos = ref _positions.Get(epEntity);
                float dx = epPos.X - px;
                float dy = epPos.Y - py;
                float maxDist = specialRange + MathF.Max(2f, ep.Radius);
                float distSq = dx * dx + dy * dy;

                if (distSq <= maxDist * maxDist)
                {
                    bool insideCone = distSq < 16f || MathF.Abs(NormalizeAngle(MathF.Atan2(dy, dx) - facingAngle)) <= halfArc;
                    if (insideCone)
                    {
                        ep.Lifetime = 0f;
                        _cmd.DestroyEntity(epEntity);
                    }
                }
            }
        }
    }

    private void ExecuteSniperSpecial(WeaponSlot slot, float px, float py, float fallbackAngle, WeaponItem weapon)
    {
        float centerAngle = fallbackAngle;
        if (TryFindFurthestEnemy(px, py, 750f, out _, out float tx, out float ty))
        {
            centerAngle = MathF.Atan2(ty - py, tx - px);
        }

        slot.AttackAngle = centerAngle;
        slot.AttackRange = 720f;
        slot.VisualDuration = 0.30f;
        slot.VisualTimer = 0.30f;
        slot.SpecialVisualDuration = 0.30f;
        slot.SpecialVisualTimer = 0.30f;

        const int beamCount = 5;
        const float spreadDeg = 28f;
        const float speed = 1350f;
        float damage = weapon.Damage * 3.0f;
        const float radius = 10f;
        const float lifetime = 0.90f;
        const float knockback = 120f;
        const float kbTime = 0.16f;
        var color = new Color(180, 250, 255, 255);

        float startOffset = -spreadDeg * 0.5f;
        float step = spreadDeg / (beamCount - 1);

        for (int b = 0; b < beamCount; b++)
        {
            float beamAngle = centerAngle + (startOffset + step * b) * (MathF.PI / 180f);
            float cosA = MathF.Cos(beamAngle);
            float sinA = MathF.Sin(beamAngle);

            ActiveAimLines.Add(new AimLineVisual
            {
                From = new Vector2(px, py),
                To = new Vector2(px + cosA * 720f, py + sinA * 720f),
                Timer = 0.40f,
                MaxTimer = 0.40f
            });

            _cmd.CreateEntity((world, entity) =>
            {
                world.AddComponent(entity, new PositionComponent(px, py));
                world.AddComponent(entity, new VelocityComponent(cosA * speed, sinA * speed));
                world.AddComponent(entity, new ProjectileComponent(damage, lifetime, radius, knockback, hitsLeft: 999, kbTime));
                world.AddComponent(entity, new ProjectileVisualComponent(ProjectileShape.PiercingLine, beamAngle, length: 56f, thickness: 10f));
                world.AddComponent(entity, new RenderColorComponent(color.R, color.G, color.B, color.A, radius));
            });
        }
    }

    private void ExecuteTrailSpecial(Entity playerEntity, ref PlayerComponent player, WeaponSlot slot, float px, float py, WeaponItem weapon)
    {
        float explosionDamage = weapon.Damage * 3.5f;
        const float knockback = 550f;
        const float kbTime = 0.25f;
        var color = new Color(165, 255, 85, 255);

        int detonatedCount = 0;

        if (_hazards != null && _positions != null)
        {
            foreach (Entity hzEntity in _world!.Query().With<PositionComponent>().With<HazardZoneComponent>())
            {
                ref HazardZoneComponent hz = ref _hazards.Get(hzEntity);
                if (hz.Owner != playerEntity || hz.RemainingDuration <= 0f) continue;

                ref readonly PositionComponent hzPos = ref _positions.Get(hzEntity);
                ApplyRadialExplosion(hzPos.X, hzPos.Y, 80f, explosionDamage, knockback, kbTime, color);

                hz.RemainingDuration = 0f;
                _cmd.DestroyEntity(hzEntity);
                detonatedCount++;
            }
        }

        if (detonatedCount == 0)
        {
            ApplyRadialExplosion(px, py, 95f, explosionDamage, knockback, kbTime, color);
        }

        player.TrailSprintTimer = 3.0f;
        slot.SpecialActiveTimer = 3.0f;
        slot.SpecialVisualDuration = 3.0f;
        slot.SpecialVisualTimer = 3.0f;
    }

    private void ApplyRadialExplosion(float cx, float cy, float radius, float damage, float knockback, float kbTime, Color color)
    {
        ActiveExplosions.Add(new ExplosionVisual
        {
            Center = new Vector2(cx, cy),
            Radius = radius,
            Timer = 0.35f,
            MaxTimer = 0.35f,
            Color = color
        });

        if (_world == null || _positions == null) return;

        if (_enemies != null)
        {
            void DamageEnemy(Entity enemy)
            {
                if (!_world.IsAlive(enemy) || !_enemies.Has(enemy)) return;
                if (_healths != null && _healths.Has(enemy) && _healths.Get(enemy).IsDead) return;

                ref readonly PositionComponent ePos = ref _positions.Get(enemy);
                float enemyRadius = (_colors != null && _colors.Has(enemy)) ? MathF.Max(10f, _colors.Get(enemy).Radius) : 10f;
                float hitDist = radius + enemyRadius;
                float dx = ePos.X - cx;
                float dy = ePos.Y - cy;
                float distSq = dx * dx + dy * dy;

                if (distSq <= hitDist * hitDist)
                {
                    if (_healths != null && _healths.Has(enemy))
                    {
                        EnemyDeathHelper.ApplyDamage(_world, _cmd, _healths, _positions, enemy, damage);
                    }

                    if (knockback > 0f && _knockbacks != null)
                    {
                        if (distSq > 0.0001f)
                        {
                            float inv = 1f / MathF.Sqrt(distSq);
                            CombatKnockback.Apply(_world, _knockbacks, enemy, dx * inv, dy * inv, knockback, kbTime);
                        }
                        else
                        {
                            CombatKnockback.Apply(_world, _knockbacks, enemy, 1f, 0f, knockback, kbTime);
                        }
                    }
                }
            }

            if (_grid != null)
            {
                CellRange cellRange = _grid.GetRange(cx, cy, radius + 32f);
                for (int r = cellRange.MinRow; r <= cellRange.MaxRow; r++)
                {
                    for (int c = cellRange.MinCol; c <= cellRange.MaxCol; c++)
                    {
                        List<Entity> cell = _grid.GetCell(c, r);
                        for (int i = 0; i < cell.Count; i++)
                        {
                            DamageEnemy(cell[i]);
                        }
                    }
                }
            }
            else
            {
                foreach (Entity enemy in _world.Query().With<PositionComponent>().With<EnemyComponent>())
                {
                    DamageEnemy(enemy);
                }
            }
        }

        if (_enemyProjectiles != null)
        {
            foreach (Entity epEntity in _world.Query().With<PositionComponent>().With<EnemyProjectileComponent>())
            {
                ref EnemyProjectileComponent ep = ref _enemyProjectiles.Get(epEntity);
                if (ep.Lifetime <= 0f) continue;

                ref readonly PositionComponent epPos = ref _positions.Get(epEntity);
                float clearDist = radius + MathF.Max(2f, ep.Radius);
                float dx = epPos.X - cx;
                float dy = epPos.Y - cy;
                if (dx * dx + dy * dy <= clearDist * clearDist)
                {
                    ep.Lifetime = 0f;
                    _cmd.DestroyEntity(epEntity);
                }
            }
        }
    }

    private void ExecuteBoomerangSpecial(Entity owner, WeaponSlot slot, float px, float py, float fallbackAngle, WeaponItem weapon)
    {
        float centerAngle = fallbackAngle;
        if (TryFindClosestEnemy(px, py, 380f, out _, out float tx, out float ty))
        {
            centerAngle = MathF.Atan2(ty - py, tx - px);
        }

        slot.AttackAngle = centerAngle;
        slot.AttackRange = 380f;
        slot.VisualDuration = 0.28f;
        slot.VisualTimer = 0.28f;
        slot.SpecialVisualDuration = 0.28f;
        slot.SpecialVisualTimer = 0.28f;

        float[] angleOffsets = { -25f, 0f, 25f };
        const float outboundRange = 380f;
        const float speed = 520f;
        float damage = weapon.Damage * 2.5f;
        const float radius = 18f;
        const float lifetime = 6.0f;
        const float knockback = 320f;
        const float kbTime = 0.22f;
        var color = new Color(255, 225, 90, 255);

        for (int i = 0; i < angleOffsets.Length; i++)
        {
            float projAngle = centerAngle + angleOffsets[i] * (MathF.PI / 180f);
            float vx = MathF.Cos(projAngle) * speed;
            float vy = MathF.Sin(projAngle) * speed;

            _cmd.CreateEntity((world, entity) =>
            {
                world.AddComponent(entity, new PositionComponent(px, py));
                world.AddComponent(entity, new VelocityComponent(vx, vy));
                world.AddComponent(entity, new ProjectileComponent(damage, lifetime, radius, knockback, hitsLeft: 999, kbTime));
                world.AddComponent(entity, new BoomerangComponent
                {
                    Owner = owner,
                    OutboundDistanceRemaining = outboundRange,
                    Speed = speed,
                    RotationAngle = 0f,
                    IsReturning = false,
                    MaxReturnDistance = outboundRange + 220f,
                    ReturnTargetX = px,
                    ReturnTargetY = py,
                    PassedReturnTarget = false
                });
                world.AddComponent(entity, new ProjectileVisualComponent(ProjectileShape.Boomerang, projAngle, length: 26f, thickness: 8f, spinAngle: 0f, spinSpeed: 22f));
                world.AddComponent(entity, new RenderColorComponent(color.R, color.G, color.B, color.A, radius));
            });
        }
    }

    private void ExecuteTwinShotsSpecial(WeaponSlot slot, float px, float py, float facingAngle, WeaponItem weapon)
    {
        slot.AttackAngle = facingAngle;
        slot.AttackRange = 40f;
        slot.VisualDuration = 0.28f;
        slot.VisualTimer = 0.28f;
        slot.SpecialVisualDuration = 0.28f;
        slot.SpecialVisualTimer = 0.28f;

        const int shotsPerSide = 6;
        const float fanDeg = 40f; // -20 to +20 around each flank
        const float speed = 660f;
        const float radius = 8f;
        const float lifetime = 0.90f;
        float damage = weapon.Damage * 2.4f;
        const float knockback = 360f;
        const float kbTime = 0.22f;
        var color = new Color(255, 195, 75, 255);

        float forwardX = MathF.Cos(facingAngle);
        float forwardY = MathF.Sin(facingAngle);
        float leftBase = facingAngle - MathF.PI * 0.5f;
        float rightBase = facingAngle + MathF.PI * 0.5f;

        SpawnFlankVolley(leftBase);
        SpawnFlankVolley(rightBase);

        void SpawnFlankVolley(float baseFlankAngle)
        {
            for (int i = 0; i < shotsPerSide; i++)
            {
                float t = i / (float)(shotsPerSide - 1) - 0.5f; // -0.5 to +0.5
                float shotAngle = baseFlankAngle + t * fanDeg * (MathF.PI / 180f);
                float longOffset = t * 50f; // -25px to +25px along player facing axis
                float sx = px + forwardX * longOffset;
                float sy = py + forwardY * longOffset;
                float vx = MathF.Cos(shotAngle) * speed;
                float vy = MathF.Sin(shotAngle) * speed;

                _cmd.CreateEntity((world, entity) =>
                {
                    world.AddComponent(entity, new PositionComponent(sx, sy));
                    world.AddComponent(entity, new VelocityComponent(vx, vy));
                    world.AddComponent(entity, new ProjectileComponent(damage, lifetime, radius, knockback, hitsLeft: 99, kbTime));
                    world.AddComponent(entity, new ProjectileVisualComponent(ProjectileShape.BroadsideBolt, shotAngle, length: 26f, thickness: 9f));
                    world.AddComponent(entity, new RenderColorComponent(color.R, color.G, color.B, color.A, radius));
                });
            }
        }
    }

    private void ExecuteConeStrike(float px, float py, float centerAngle, float effectiveRange, float arcDegrees, float damage, float knockback, float kbTime)
    {
        if (_world == null || _positions == null || _enemies == null) return;

        float rangeSq = effectiveRange * effectiveRange;
        float halfArc = (arcDegrees * 0.5f) * (MathF.PI / 180f);

        void ProcessEnemy(Entity e)
        {
            if (!_world.IsAlive(e) || !_enemies.Has(e)) return;
            if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) return;

            ref readonly PositionComponent pos = ref _positions.Get(e);
            float dx = pos.X - px;
            float dy = pos.Y - py;
            float distSq = dx * dx + dy * dy;

            if (distSq <= rangeSq)
            {
                bool insideCone;
                float kbDirX;
                float kbDirY;

                if (distSq < 0.0001f)
                {
                    insideCone = true;
                    kbDirX = MathF.Cos(centerAngle);
                    kbDirY = MathF.Sin(centerAngle);
                }
                else
                {
                    float angleToEnemy = MathF.Atan2(dy, dx);
                    float angleDiff = MathF.Abs(NormalizeAngle(angleToEnemy - centerAngle));
                    insideCone = angleDiff <= halfArc;
                    float invDist = 1f / MathF.Sqrt(distSq);
                    kbDirX = dx * invDist;
                    kbDirY = dy * invDist;
                }

                if (insideCone)
                {
                    if (_healths != null && _healths.Has(e))
                    {
                        EnemyDeathHelper.ApplyDamage(_world, _cmd, _healths, _positions, e, damage);
                    }

                    if (knockback > 0f && _knockbacks != null)
                    {
                        CombatKnockback.Apply(_world, _knockbacks, e, kbDirX, kbDirY, knockback, kbTime);
                    }
                }
            }
        }

        if (_grid != null)
        {
            CellRange range = _grid.GetRange(px, py, effectiveRange + 16f);
            for (int r = range.MinRow; r <= range.MaxRow; r++)
            {
                for (int c = range.MinCol; c <= range.MaxCol; c++)
                {
                    List<Entity> cell = _grid.GetCell(c, r);
                    for (int i = 0; i < cell.Count; i++)
                    {
                        ProcessEnemy(cell[i]);
                    }
                }
            }
        }
        else
        {
            foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyComponent>())
            {
                ProcessEnemy(e);
            }
        }
    }

    private void ExecutePiercingLine(float px, float py, float angle, WeaponItem weapon, float damage, int count)
    {
        var def = weapon.Definition;
        float speed = def.ProjectileSpeed;
        float radius = def.ProjectileRadius;
        float lifetime = def.ProjectileLifetime;
        float knockback = def.Knockback;
        float kbTime = def.KnockbackTime > 0f ? def.KnockbackTime : 0.12f;
        int hitsLeft = Math.Max(1, def.PiercingHits);
        var color = def.Color;
        const float spreadDeg = 10f;

        for (int p = 0; p < count; p++)
        {
            float projAngle = angle;
            if (count > 1)
            {
                float startOffset = -spreadDeg * 0.5f;
                float step = spreadDeg / (count - 1);
                projAngle = angle + (startOffset + step * p) * (MathF.PI / 180f);
            }

            float vx = MathF.Cos(projAngle) * speed;
            float vy = MathF.Sin(projAngle) * speed;

            _cmd.CreateEntity((world, entity) =>
            {
                world.AddComponent(entity, new PositionComponent(px, py));
                world.AddComponent(entity, new VelocityComponent(vx, vy));
                world.AddComponent(entity, new ProjectileComponent(damage, lifetime, radius, knockback, hitsLeft, kbTime));
                world.AddComponent(entity, new ProjectileVisualComponent(ProjectileShape.PiercingLine, projAngle, length: 34f, thickness: 6f));
                world.AddComponent(entity, new RenderColorComponent(color.R, color.G, color.B, color.A, radius));
            });
        }
    }

    private void SpawnHazardZone(Entity owner, float px, float py, WeaponItem weapon, float radius, float damage)
    {
        var def = weapon.Definition;
        float duration = def.HazardDuration;
        float tickInterval = def.HazardTickInterval;

        _cmd.CreateEntity((world, entity) =>
        {
            world.AddComponent(entity, new PositionComponent(px, py));
            world.AddComponent(entity, new HazardZoneComponent
            {
                Owner = owner,
                Radius = radius,
                RemainingDuration = duration,
                MaxDuration = duration,
                Damage = damage,
                TickInterval = tickInterval,
                TickTimer = 0f
            });
        });
    }

    private void ExecuteReturningBoomerang(Entity owner, float px, float py, float angle, WeaponItem weapon, float damage, float outboundRange, int count)
    {
        var def = weapon.Definition;
        float speed = def.ProjectileSpeed;
        float radius = def.ProjectileRadius;
        float lifetime = def.ProjectileLifetime;
        float knockback = def.Knockback;
        float kbTime = def.KnockbackTime > 0f ? def.KnockbackTime : 0.12f;
        int hitsLeft = Math.Max(1, def.PiercingHits);
        var color = def.Color;
        const float spreadDeg = 18f;

        for (int p = 0; p < count; p++)
        {
            float projAngle = angle;
            if (count > 1)
            {
                float startOffset = -spreadDeg * 0.5f;
                float step = spreadDeg / (count - 1);
                projAngle = angle + (startOffset + step * p) * (MathF.PI / 180f);
            }

            float vx = MathF.Cos(projAngle) * speed;
            float vy = MathF.Sin(projAngle) * speed;

            _cmd.CreateEntity((world, entity) =>
            {
                world.AddComponent(entity, new PositionComponent(px, py));
                world.AddComponent(entity, new VelocityComponent(vx, vy));
                world.AddComponent(entity, new ProjectileComponent(damage, lifetime, radius, knockback, hitsLeft, kbTime));
                world.AddComponent(entity, new BoomerangComponent
                {
                    Owner = owner,
                    OutboundDistanceRemaining = outboundRange,
                    Speed = speed,
                    RotationAngle = 0f,
                    IsReturning = false,
                    MaxReturnDistance = outboundRange + 180f,
                    ReturnTargetX = px,
                    ReturnTargetY = py,
                    PassedReturnTarget = false
                });
                world.AddComponent(entity, new ProjectileVisualComponent(ProjectileShape.Boomerang, projAngle, length: 15f, thickness: 5f, spinAngle: 0f, spinSpeed: 14f));
                world.AddComponent(entity, new RenderColorComponent(color.R, color.G, color.B, color.A, radius));
            });
        }
    }

    private void ExecuteSidewaysBroadside(float px, float py, float facingAngle, WeaponItem weapon, float damage)
    {
        var def = weapon.Definition;
        float speed = def.ProjectileSpeed;
        float radius = def.ProjectileRadius;
        float lifetime = def.ProjectileLifetime;
        float knockback = def.Knockback;
        float kbTime = def.KnockbackTime > 0f ? def.KnockbackTime : 0.15f;
        int hitsLeft = Math.Max(1, def.PiercingHits);
        var color = def.Color;

        float leftAngle = facingAngle - MathF.PI * 0.5f;
        float rightAngle = facingAngle + MathF.PI * 0.5f;

        SpawnBroadsideShot(leftAngle);
        SpawnBroadsideShot(rightAngle);

        void SpawnBroadsideShot(float shotAngle)
        {
            float vx = MathF.Cos(shotAngle) * speed;
            float vy = MathF.Sin(shotAngle) * speed;

            _cmd.CreateEntity((world, entity) =>
            {
                world.AddComponent(entity, new PositionComponent(px, py));
                world.AddComponent(entity, new VelocityComponent(vx, vy));
                world.AddComponent(entity, new ProjectileComponent(damage, lifetime, radius, knockback, hitsLeft, kbTime));
                world.AddComponent(entity, new ProjectileVisualComponent(ProjectileShape.BroadsideBolt, shotAngle, length: 16f, thickness: 6f));
                world.AddComponent(entity, new RenderColorComponent(color.R, color.G, color.B, color.A, radius));
            });
        }
    }

    private bool TryFindClosestEnemy(float px, float py, float maxRange, out Entity closest, out float targetX, out float targetY)
    {
        closest = default;
        targetX = 0f;
        targetY = 0f;

        if (_world == null || _positions == null || _enemies == null) return false;

        float maxRangeSq = maxRange * maxRange;
        float bestDistSq = maxRangeSq;
        bool found = false;

        if (_grid != null)
        {
            CellRange range = _grid.GetRange(px, py, maxRange);
            for (int r = range.MinRow; r <= range.MaxRow; r++)
            {
                for (int c = range.MinCol; c <= range.MaxCol; c++)
                {
                    List<Entity> cell = _grid.GetCell(c, r);
                    for (int i = 0; i < cell.Count; i++)
                    {
                        Entity e = cell[i];
                        if (!_world.IsAlive(e) || !_enemies.Has(e)) continue;
                        if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) continue;

                        ref readonly PositionComponent pos = ref _positions.Get(e);
                        float dx = pos.X - px;
                        float dy = pos.Y - py;
                        float distSq = dx * dx + dy * dy;

                        if (distSq <= bestDistSq)
                        {
                            bestDistSq = distSq;
                            closest = e;
                            targetX = pos.X;
                            targetY = pos.Y;
                            found = true;
                        }
                    }
                }
            }
            return found;
        }

        foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyComponent>())
        {
            if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) continue;

            ref readonly PositionComponent pos = ref _positions.Get(e);
            float dx = pos.X - px;
            float dy = pos.Y - py;
            float distSq = dx * dx + dy * dy;

            if (distSq <= bestDistSq)
            {
                bestDistSq = distSq;
                closest = e;
                targetX = pos.X;
                targetY = pos.Y;
                found = true;
            }
        }

        return found;
    }

    private bool TryFindFurthestEnemy(float px, float py, float maxRange, out Entity furthest, out float targetX, out float targetY)
    {
        furthest = default;
        targetX = 0f;
        targetY = 0f;

        if (_world == null || _positions == null || _enemies == null) return false;

        float maxRangeSq = maxRange * maxRange;
        float bestDistSq = -1f;
        bool found = false;

        if (_grid != null)
        {
            CellRange range = _grid.GetRange(px, py, maxRange);
            for (int r = range.MinRow; r <= range.MaxRow; r++)
            {
                for (int c = range.MinCol; c <= range.MaxCol; c++)
                {
                    List<Entity> cell = _grid.GetCell(c, r);
                    for (int i = 0; i < cell.Count; i++)
                    {
                        Entity e = cell[i];
                        if (!_world.IsAlive(e) || !_enemies.Has(e)) continue;
                        if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) continue;

                        ref readonly PositionComponent pos = ref _positions.Get(e);
                        float dx = pos.X - px;
                        float dy = pos.Y - py;
                        float distSq = dx * dx + dy * dy;

                        if (distSq <= maxRangeSq && distSq > bestDistSq)
                        {
                            bestDistSq = distSq;
                            furthest = e;
                            targetX = pos.X;
                            targetY = pos.Y;
                            found = true;
                        }
                    }
                }
            }
            return found;
        }

        foreach (Entity e in _world.Query().With<PositionComponent>().With<EnemyComponent>())
        {
            if (_healths != null && _healths.Has(e) && _healths.Get(e).IsDead) continue;

            ref readonly PositionComponent pos = ref _positions.Get(e);
            float dx = pos.X - px;
            float dy = pos.Y - py;
            float distSq = dx * dx + dy * dy;

            if (distSq <= maxRangeSq && distSq > bestDistSq)
            {
                bestDistSq = distSq;
                furthest = e;
                targetX = pos.X;
                targetY = pos.Y;
                found = true;
            }
        }

        return found;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.PI * 2f;
        while (angle < -MathF.PI) angle += MathF.PI * 2f;
        return angle;
    }
}
