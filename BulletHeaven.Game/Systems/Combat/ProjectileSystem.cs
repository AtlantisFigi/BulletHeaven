using System;
using System.Collections.Generic;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Spatial;

namespace BulletHeaven.Game.Systems.Combat;

public sealed class ProjectileSystem : ISystem
{
    private struct HitRecord
    {
        public int Epoch;
        public HashSet<Entity> HitEnemies;
    }

    private World? _world;
    private readonly CommandBuffer _cmd;
    private readonly SpatialGrid? _grid;

    private ComponentStore<PositionComponent>? _positions;
    private ComponentStore<ProjectileComponent>? _projectiles;
    private ComponentStore<EnemyComponent>? _enemies;
    private ComponentStore<HealthComponent>? _healths;
    private ComponentStore<VelocityComponent>? _velocities;
    private ComponentStore<KnockbackComponent>? _knockbacks;
    private ComponentStore<BoomerangComponent>? _boomerangs;
    private ComponentStore<RenderColorComponent>? _colors;

    private readonly Dictionary<Entity, HitRecord> _hitRecords = new();
    private readonly Stack<HashSet<Entity>> _pool = new();
    private readonly List<Entity> _trackedKeys = new();

    public ProjectileSystem(CommandBuffer cmd, SpatialGrid? grid = null)
    {
        _cmd = cmd;
        _grid = grid;
    }

    public void Initialize(World world)
    {
        _world = world;
        _positions = world.GetStore<PositionComponent>();
        _projectiles = world.GetStore<ProjectileComponent>();
        _enemies = world.GetStore<EnemyComponent>();
        _healths = world.GetStore<HealthComponent>();
        _velocities = world.GetStore<VelocityComponent>();
        _knockbacks = world.GetStore<KnockbackComponent>();
        _boomerangs = world.GetStore<BoomerangComponent>();
        _colors = world.GetStore<RenderColorComponent>();
        ClearHitTracking();
    }

    public void ClearHitTracking()
    {
        for (int i = 0; i < _trackedKeys.Count; i++)
        {
            if (_hitRecords.TryGetValue(_trackedKeys[i], out var rec))
            {
                rec.HitEnemies.Clear();
                _pool.Push(rec.HitEnemies);
            }
        }
        _hitRecords.Clear();
        _trackedKeys.Clear();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _positions == null || _projectiles == null || _enemies == null) return;

        SweepDeadTrackedProjectiles();

        foreach (Entity projEntity in _world.Query().With<PositionComponent>().With<ProjectileComponent>())
        {
            ref ProjectileComponent proj = ref _projectiles.Get(projEntity);
            proj.Lifetime -= deltaTime;

            if (proj.Lifetime <= 0f)
            {
                ReleaseHitRecord(projEntity);
                _cmd.DestroyEntity(projEntity);
                continue;
            }

            bool trackHits = proj.HitsLeft > 1 || _hitRecords.ContainsKey(projEntity) || (_boomerangs != null && _boomerangs.Has(projEntity));
            HashSet<Entity>? hitEnemies = trackHits ? GetOrCreateHitSet(projEntity, proj.HitEpoch) : null;

            ref readonly PositionComponent projPos = ref _positions.Get(projEntity);
            float projDirX = 0f;
            float projDirY = 0f;
            bool hasProjVel = false;
            if (_velocities != null && _velocities.Has(projEntity))
            {
                ref readonly VelocityComponent pVel = ref _velocities.Get(projEntity);
                float pSpeedSq = pVel.X * pVel.X + pVel.Y * pVel.Y;
                if (pSpeedSq > 0.001f)
                {
                    float invSpeed = 1f / MathF.Sqrt(pSpeedSq);
                    projDirX = pVel.X * invSpeed;
                    projDirY = pVel.Y * invSpeed;
                    hasProjVel = true;
                }
            }

            float queryRadius = proj.Radius + 32f;
            bool hitConsumed = false;

            if (_grid != null)
            {
                CellRange range = _grid.GetRange(projPos.X, projPos.Y, queryRadius);
                for (int r = range.MinRow; r <= range.MaxRow && !hitConsumed; r++)
                {
                    for (int c = range.MinCol; c <= range.MaxCol && !hitConsumed; c++)
                    {
                        List<Entity> cell = _grid.GetCell(c, r);
                        for (int i = 0; i < cell.Count; i++)
                        {
                            Entity enemyEntity = cell[i];
                            if (!_world.IsAlive(enemyEntity) || !_enemies.Has(enemyEntity)) continue;
                            if (_healths != null && _healths.Has(enemyEntity) && _healths.Get(enemyEntity).IsDead) continue;
                            if (hitEnemies != null && hitEnemies.Contains(enemyEntity)) continue;

                            ref readonly PositionComponent enemyPos = ref _positions.Get(enemyEntity);
                            float enemyRadius = (_colors != null && _colors.Has(enemyEntity)) ? MathF.Max(10f, _colors.Get(enemyEntity).Radius) : 12f;
                            float hitRadius = proj.Radius + enemyRadius;
                            float dx = enemyPos.X - projPos.X;
                            float dy = enemyPos.Y - projPos.Y;
                            float distSq = dx * dx + dy * dy;

                            if (distSq <= hitRadius * hitRadius)
                            {
                                ApplyHit(projEntity, ref proj, enemyEntity, dx, dy, distSq, projDirX, projDirY, hasProjVel, hitEnemies);
                                if (proj.HitsLeft <= 0)
                                {
                                    hitConsumed = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (Entity enemyEntity in _world.Query().With<PositionComponent>().With<EnemyComponent>())
                {
                    if (_healths != null && _healths.Has(enemyEntity) && _healths.Get(enemyEntity).IsDead) continue;
                    if (hitEnemies != null && hitEnemies.Contains(enemyEntity)) continue;

                    ref readonly PositionComponent enemyPos = ref _positions.Get(enemyEntity);
                    float enemyRadius = (_colors != null && _colors.Has(enemyEntity)) ? MathF.Max(10f, _colors.Get(enemyEntity).Radius) : 12f;
                    float hitRadius = proj.Radius + enemyRadius;
                    float dx = enemyPos.X - projPos.X;
                    float dy = enemyPos.Y - projPos.Y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= hitRadius * hitRadius)
                    {
                        ApplyHit(projEntity, ref proj, enemyEntity, dx, dy, distSq, projDirX, projDirY, hasProjVel, hitEnemies);
                        if (proj.HitsLeft <= 0)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }

    private void ApplyHit(
        Entity projEntity,
        ref ProjectileComponent proj,
        Entity enemyEntity,
        float dx,
        float dy,
        float distSq,
        float projDirX,
        float projDirY,
        bool hasProjVel,
        HashSet<Entity>? hitEnemies)
    {
        if (_world == null) return;

        hitEnemies?.Add(enemyEntity);

        if (_healths != null && _healths.Has(enemyEntity))
        {
            EnemyDeathHelper.ApplyDamage(_world, _cmd, _healths, _positions, enemyEntity, proj.Damage);
        }

        if (proj.Knockback > 0f && _knockbacks != null)
        {
            float kbDirX;
            float kbDirY;
            if (distSq > 0.001f)
            {
                float invDist = 1f / MathF.Sqrt(distSq);
                kbDirX = dx * invDist;
                kbDirY = dy * invDist;
            }
            else if (hasProjVel)
            {
                kbDirX = projDirX;
                kbDirY = projDirY;
            }
            else
            {
                kbDirX = 1f;
                kbDirY = 0f;
            }

            float kbTime = proj.KnockbackTime > 0f ? proj.KnockbackTime : 0.15f;
            CombatKnockback.Apply(_world, _knockbacks, enemyEntity, kbDirX, kbDirY, proj.Knockback, kbTime);
        }

        proj.HitsLeft--;
        if (proj.HitsLeft <= 0)
        {
            ReleaseHitRecord(projEntity);
            _cmd.DestroyEntity(projEntity);
        }
    }

    private HashSet<Entity> GetOrCreateHitSet(Entity projEntity, int epoch)
    {
        if (_hitRecords.TryGetValue(projEntity, out HitRecord rec))
        {
            if (rec.Epoch != epoch)
            {
                rec.HitEnemies.Clear();
                rec.Epoch = epoch;
                _hitRecords[projEntity] = rec;
            }
            return rec.HitEnemies;
        }

        HashSet<Entity> set = _pool.Count > 0 ? _pool.Pop() : new HashSet<Entity>(16);
        _hitRecords[projEntity] = new HitRecord { Epoch = epoch, HitEnemies = set };
        _trackedKeys.Add(projEntity);
        return set;
    }

    private void ReleaseHitRecord(Entity projEntity)
    {
        if (_hitRecords.Remove(projEntity, out HitRecord rec))
        {
            rec.HitEnemies.Clear();
            _pool.Push(rec.HitEnemies);
            int idx = _trackedKeys.IndexOf(projEntity);
            if (idx >= 0)
            {
                int last = _trackedKeys.Count - 1;
                _trackedKeys[idx] = _trackedKeys[last];
                _trackedKeys.RemoveAt(last);
            }
        }
    }

    private void SweepDeadTrackedProjectiles()
    {
        if (_world == null || _projectiles == null) return;

        for (int i = _trackedKeys.Count - 1; i >= 0; i--)
        {
            Entity key = _trackedKeys[i];
            if (!_world.IsAlive(key) || !_projectiles.Has(key))
            {
                if (_hitRecords.Remove(key, out HitRecord rec))
                {
                    rec.HitEnemies.Clear();
                    _pool.Push(rec.HitEnemies);
                }
                int last = _trackedKeys.Count - 1;
                _trackedKeys[i] = _trackedKeys[last];
                _trackedKeys.RemoveAt(last);
            }
        }
    }
}
