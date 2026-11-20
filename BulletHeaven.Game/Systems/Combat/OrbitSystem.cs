using System;
using System.Collections.Generic;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Spatial;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class OrbitSystem : ISystem
{
    private World? _world;
    private readonly CommandBuffer _cmd;
    private readonly SpatialGrid? _grid;

    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<EquipmentComponent>? _equipments;
    private ComponentStore<OrbitProjectileComponent>? _orbits;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<KnockbackComponent>? _knockbacks;
    private ComponentStore<RenderColorComponent>? _colors;
    private ComponentStore<EnemyProjectileComponent>? _enemyProjectiles;

    private readonly Dictionary<(Entity Owner, Entity Enemy), float> _hitCooldowns = new();
    private readonly List<(Entity Owner, Entity Enemy)> _expiredCooldowns = new();
    private readonly List<Entity> _orbBuffer = new();
    private readonly Dictionary<(Entity Owner, int SlotIndex), float> _slotHitCooldowns = new();
    private readonly Dictionary<(Entity Owner, int SlotIndex), bool> _slotSpecialActive = new();

    public OrbitSystem(CommandBuffer cmd, SpatialGrid? grid = null)
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
        _orbits = world.GetStore<OrbitProjectileComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _healths = world.GetStore<HealthComponent>();
        _knockbacks = world.GetStore<KnockbackComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        _enemyProjectiles = world.GetStore<EnemyProjectileComponent>();
        Clear();
    }

    public void Clear()
    {
        _hitCooldowns.Clear();
        _expiredCooldowns.Clear();
        _orbBuffer.Clear();
        _slotHitCooldowns.Clear();
        _slotSpecialActive.Clear();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _players == null || _equipments == null || _orbits == null || _enemies == null)
            return;

        UpdateHitCooldowns(deltaTime);
        SyncOrbits();

        _orbBuffer.Clear();
        foreach (Entity orbEntity in _world.Query().With<PositionComponent>().With<OrbitProjectileComponent>())
        {
            _orbBuffer.Add(orbEntity);
        }

        for (int idx = 0; idx < _orbBuffer.Count; idx++)
        {
            Entity orbEntity = _orbBuffer[idx];
            if (!_world.IsAlive(orbEntity) || !_orbits.Has(orbEntity)) continue;

            ref OrbitProjectileComponent orb = ref _orbits.Get(orbEntity);
            if (!_world.IsAlive(orb.Owner) || !_positions.Has(orb.Owner))
            {
                _world.DestroyEntity(orbEntity);
                continue;
            }

            if (_healths != null && _healths.Has(orb.Owner) && _healths.Get(orb.Owner).IsDead)
            {
                _world.DestroyEntity(orbEntity);
                continue;
            }

            ref readonly PositionComponent ownerPos = ref _positions.Get(orb.Owner);
            orb.Angle += orb.OrbitSpeed * deltaTime;
            if (orb.Angle > MathF.PI * 2f)
            {
                orb.Angle -= MathF.PI * 2f;
            }

            float ox = ownerPos.X + MathF.Cos(orb.Angle) * orb.OrbitRadius;
            float oy = ownerPos.Y + MathF.Sin(orb.Angle) * orb.OrbitRadius;

            ref PositionComponent orbPos = ref _positions.Get(orbEntity);
            orbPos.X = ox;
            orbPos.Y = oy;

            float deadzoneSq = orb.DeadzoneRadius * orb.DeadzoneRadius;
            float hitCooldown = _slotHitCooldowns.TryGetValue((orb.Owner, orb.SlotIndex), out float cd) ? cd : 0.35f;

            if (_grid != null)
            {
                CellRange range = _grid.GetRange(ox, oy, orb.OrbRadius + 32f);
                for (int r = range.MinRow; r <= range.MaxRow; r++)
                {
                    for (int c = range.MinCol; c <= range.MaxCol; c++)
                    {
                        List<Entity> cell = _grid.GetCell(c, r);
                        for (int i = 0; i < cell.Count; i++)
                        {
                            TryHitEnemy(orbEntity, ref orb, ownerPos, ox, oy, deadzoneSq, hitCooldown, cell[i]);
                        }
                    }
                }
            }
            else
            {
                foreach (Entity enemyEntity in _world.Query().With<PositionComponent>().With<EnemyComponent>())
                {
                    TryHitEnemy(orbEntity, ref orb, ownerPos, ox, oy, deadzoneSq, hitCooldown, enemyEntity);
                }
            }

            if (_enemyProjectiles != null && _slotSpecialActive.TryGetValue((orb.Owner, orb.SlotIndex), out bool isSpecial) && isSpecial)
            {
                foreach (Entity epEntity in _world.Query().With<PositionComponent>().With<EnemyProjectileComponent>())
                {
                    ref EnemyProjectileComponent ep = ref _enemyProjectiles.Get(epEntity);
                    if (ep.Lifetime <= 0f) continue;

                    ref readonly PositionComponent epPos = ref _positions.Get(epEntity);
                    float clearDist = orb.OrbRadius + MathF.Max(4f, ep.Radius) + 8f;
                    float pdx = epPos.X - ox;
                    float pdy = epPos.Y - oy;
                    if (pdx * pdx + pdy * pdy <= clearDist * clearDist)
                    {
                        ep.Lifetime = 0f;
                        _cmd.DestroyEntity(epEntity);
                    }
                }
            }
        }
    }

    private void TryHitEnemy(
        Entity orbEntity,
        ref OrbitProjectileComponent orb,
        in PositionComponent ownerPos,
        float ox,
        float oy,
        float deadzoneSq,
        float hitCooldown,
        Entity enemyEntity)
    {
        if (_world == null || _positions == null || _enemies == null) return;
        if (!_world.IsAlive(enemyEntity) || !_enemies.Has(enemyEntity)) return;
        if (_healths != null && _healths.Has(enemyEntity) && _healths.Get(enemyEntity).IsDead) return;

        ref readonly PositionComponent enemyPos = ref _positions.Get(enemyEntity);

        float pdx = enemyPos.X - ownerPos.X;
        float pdy = enemyPos.Y - ownerPos.Y;
        float playerDistSq = pdx * pdx + pdy * pdy;
        if (playerDistSq < deadzoneSq) return;

        float enemyRadius = (_colors != null && _colors.Has(enemyEntity)) ? MathF.Max(10f, _colors.Get(enemyEntity).Radius) : 10f;
        float hitRadius = orb.OrbRadius + enemyRadius;
        float edx = enemyPos.X - ox;
        float edy = enemyPos.Y - oy;
        if (edx * edx + edy * edy > hitRadius * hitRadius) return;

        var key = (orb.Owner, enemyEntity);
        if (_hitCooldowns.ContainsKey(key)) return;

        if (_healths != null && _healths.Has(enemyEntity))
        {
            EnemyDeathHelper.ApplyDamage(_world, _cmd, _healths, _positions, enemyEntity, orb.Damage);
        }

        if (orb.Knockback > 0f && _knockbacks != null)
        {
            CombatKnockback.Apply(_world, _knockbacks, enemyEntity, pdx, pdy, orb.Knockback, 0.12f);
        }

        _hitCooldowns[key] = hitCooldown;
    }

    private void UpdateHitCooldowns(float deltaTime)
    {
        if (_world == null || _hitCooldowns.Count == 0) return;

        _expiredCooldowns.Clear();
        foreach (var kvp in _hitCooldowns)
        {
            if (!_world.IsAlive(kvp.Key.Owner) || !_world.IsAlive(kvp.Key.Enemy))
            {
                _expiredCooldowns.Add(kvp.Key);
            }
        }

        for (int i = 0; i < _expiredCooldowns.Count; i++)
        {
            _hitCooldowns.Remove(_expiredCooldowns[i]);
        }

        _expiredCooldowns.Clear();
        foreach (var key in _hitCooldowns.Keys)
        {
            _expiredCooldowns.Add(key);
        }

        for (int i = 0; i < _expiredCooldowns.Count; i++)
        {
            var key = _expiredCooldowns[i];
            float next = _hitCooldowns[key] - deltaTime;
            if (next <= 0f)
            {
                _hitCooldowns.Remove(key);
            }
            else
            {
                _hitCooldowns[key] = next;
            }
        }
    }

    private void SyncOrbits()
    {
        if (_world == null || _positions == null || _equipments == null || _orbits == null) return;

        _orbBuffer.Clear();
        foreach (Entity e in _world.Query().With<OrbitProjectileComponent>())
        {
            _orbBuffer.Add(e);
        }

        _slotHitCooldowns.Clear();
        _slotSpecialActive.Clear();

        foreach (Entity playerEntity in _world.Query().With<PositionComponent>().With<EquipmentComponent>().With<PlayerComponent>())
        {
            bool playerDead = _healths != null && _healths.Has(playerEntity) && _healths.Get(playerEntity).IsDead;
            ref readonly PositionComponent playerPos = ref _positions.Get(playerEntity);
            ref readonly EquipmentComponent equip = ref _equipments.Get(playerEntity);

            for (int s = 0; s < equip.Slots.Length; s++)
            {
                var slot = equip.Slots[s];
                var weapon = slot.Weapon;
                bool isOuterRing = !playerDead && weapon != null && weapon.Definition.Type == Weapons.WeaponType.OuterRing;

                if (!isOuterRing)
                {
                    DestroySlotOrbs(playerEntity, s);
                    continue;
                }

                bool isSpecialActive = slot.SpecialActiveTimer > 0f;
                int expectedCount = Math.Max(1, weapon!.OrbitCount) + (isSpecialActive ? 4 : 0);
                float orbitRadius = weapon.Definition.OrbitRadius;
                float deadzoneRadius = weapon.Definition.DeadzoneRadius;
                float orbitSpeed = weapon.Definition.OrbitSpeed * MathF.Pow(1.12f, weapon.Level - 1) * (isSpecialActive ? 2.2f : 1f);
                float damage = weapon.Damage * (isSpecialActive ? 1.75f : 1f);
                float knockback = weapon.Definition.Knockback * (isSpecialActive ? 1.35f : 1f);
                float orbRadius = MathF.Max(6f, weapon.Definition.ProjectileRadius) * (isSpecialActive ? 1.35f : 1f);
                _slotHitCooldowns[(playerEntity, s)] = MathF.Max(0.10f, weapon.AttackInterval);
                _slotSpecialActive[(playerEntity, s)] = isSpecialActive;

                int existingCount = 0;
                float baseAngle = 0f;
                bool capturedBaseAngle = false;
                bool needsRebuild = false;

                for (int i = 0; i < _orbBuffer.Count; i++)
                {
                    Entity orbEntity = _orbBuffer[i];
                    if (!_world.IsAlive(orbEntity) || !_orbits.Has(orbEntity)) continue;
                    ref OrbitProjectileComponent existing = ref _orbits.Get(orbEntity);
                    if (existing.Owner == playerEntity && existing.SlotIndex == s)
                    {
                        if (!capturedBaseAngle)
                        {
                            baseAngle = existing.Angle;
                            capturedBaseAngle = true;
                        }
                        existingCount++;
                        if (existing.TotalOrbs != expectedCount)
                        {
                            needsRebuild = true;
                        }
                        else
                        {
                            existing.OrbitRadius = orbitRadius;
                            existing.DeadzoneRadius = deadzoneRadius;
                            existing.OrbitSpeed = orbitSpeed;
                            existing.Damage = damage;
                            existing.Knockback = knockback;
                            existing.OrbRadius = orbRadius;
                        }
                    }
                }

                if (existingCount != expectedCount || needsRebuild)
                {
                    DestroySlotOrbs(playerEntity, s);

                    float step = (MathF.PI * 2f) / expectedCount;
                    for (int orbIdx = 0; orbIdx < expectedCount; orbIdx++)
                    {
                        float angle = baseAngle + orbIdx * step;
                        float ox = playerPos.X + MathF.Cos(angle) * orbitRadius;
                        float oy = playerPos.Y + MathF.Sin(angle) * orbitRadius;

                        Entity orbEntity = _world.CreateEntity();
                        _positions.Add(orbEntity, new PositionComponent(ox, oy));
                        _orbits.Add(orbEntity, new OrbitProjectileComponent
                        {
                            Owner = playerEntity,
                            SlotIndex = s,
                            OrbIndex = orbIdx,
                            TotalOrbs = expectedCount,
                            Angle = angle,
                            OrbitRadius = orbitRadius,
                            DeadzoneRadius = deadzoneRadius,
                            OrbitSpeed = orbitSpeed,
                            Damage = damage,
                            Knockback = knockback,
                            OrbRadius = orbRadius
                        });
                    }
                }
            }
        }

        // Destroy any orphaned orbs whose owner is dead or no longer has EquipmentComponent
        for (int i = 0; i < _orbBuffer.Count; i++)
        {
            Entity orbEntity = _orbBuffer[i];
            if (!_world.IsAlive(orbEntity) || !_orbits.Has(orbEntity)) continue;
            ref readonly OrbitProjectileComponent orb = ref _orbits.Get(orbEntity);
            if (!_world.IsAlive(orb.Owner) || !_equipments.Has(orb.Owner) ||
                (_healths != null && _healths.Has(orb.Owner) && _healths.Get(orb.Owner).IsDead))
            {
                _world.DestroyEntity(orbEntity);
            }
        }
    }

    private void DestroySlotOrbs(Entity owner, int slotIndex)
    {
        if (_world == null || _orbits == null) return;
        for (int i = 0; i < _orbBuffer.Count; i++)
        {
            Entity orbEntity = _orbBuffer[i];
            if (!_world.IsAlive(orbEntity) || !_orbits.Has(orbEntity)) continue;
            ref readonly OrbitProjectileComponent orb = ref _orbits.Get(orbEntity);
            if (orb.Owner == owner && orb.SlotIndex == slotIndex)
            {
                _world.DestroyEntity(orbEntity);
            }
        }
    }
}
