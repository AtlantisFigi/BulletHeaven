using System;
using System.Collections.Generic;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Spatial;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class HazardZoneSystem : ISystem
{
    private World? _world;
    private readonly CommandBuffer _cmd;
    private readonly SpatialGrid? _grid;

    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<HazardZoneComponent>? _hazards;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<RenderColorComponent>? _colors;

    public HazardZoneSystem(CommandBuffer cmd, SpatialGrid? grid = null)
    {
        _cmd = cmd;
        _grid = grid;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _hazards = world.GetStore<HazardZoneComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _healths = world.GetStore<HealthComponent>();
        _colors = world.GetStore<RenderColorComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _hazards == null || _enemies == null) return;

        foreach (Entity zoneEntity in _world.Query().With<PositionComponent>().With<HazardZoneComponent>())
        {
            ref HazardZoneComponent zone = ref _hazards.Get(zoneEntity);
            zone.RemainingDuration -= deltaTime;

            if (zone.RemainingDuration <= 0f)
            {
                _cmd.DestroyEntity(zoneEntity);
                continue;
            }

            zone.TickTimer -= deltaTime;
            if (zone.TickTimer > 0f) continue;

            zone.TickTimer = MathF.Max(0.05f, zone.TickInterval);
            ref readonly PositionComponent zonePos = ref _positions.Get(zoneEntity);

            if (_grid != null)
            {
                CellRange range = _grid.GetRange(zonePos.X, zonePos.Y, zone.Radius + 32f);
                for (int r = range.MinRow; r <= range.MaxRow; r++)
                {
                    for (int c = range.MinCol; c <= range.MaxCol; c++)
                    {
                        List<Entity> cell = _grid.GetCell(c, r);
                        for (int i = 0; i < cell.Count; i++)
                        {
                            TryDamageEnemy(zonePos.X, zonePos.Y, zone.Radius, zone.Damage, cell[i]);
                        }
                    }
                }
            }
            else
            {
                foreach (Entity enemyEntity in _world.Query().With<PositionComponent>().With<EnemyComponent>())
                {
                    TryDamageEnemy(zonePos.X, zonePos.Y, zone.Radius, zone.Damage, enemyEntity);
                }
            }
        }
    }

    private void TryDamageEnemy(float zx, float zy, float zoneRadius, float damage, Entity enemyEntity)
    {
        if (_world == null || _positions == null || _enemies == null) return;
        if (!_world.IsAlive(enemyEntity) || !_enemies.Has(enemyEntity)) return;
        if (_healths != null && _healths.Has(enemyEntity) && _healths.Get(enemyEntity).IsDead) return;

        ref readonly PositionComponent enemyPos = ref _positions.Get(enemyEntity);
        float enemyRadius = (_colors != null && _colors.Has(enemyEntity)) ? MathF.Max(10f, _colors.Get(enemyEntity).Radius) : 10f;
        float hitRadius = zoneRadius + enemyRadius;
        float dx = enemyPos.X - zx;
        float dy = enemyPos.Y - zy;

        if (dx * dx + dy * dy <= hitRadius * hitRadius)
        {
            if (_healths != null && _healths.Has(enemyEntity))
            {
                EnemyDeathHelper.ApplyDamage(_world, _cmd, _healths, _positions, enemyEntity, damage);
            }
        }
    }
}
