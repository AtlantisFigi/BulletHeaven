using System;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Arena;
using BulletHeaven.Game.Components;
using BulletHeaven.Game.Waves;

namespace BulletHeaven.Game.Systems;

public sealed class WaveSystem : ISystem
{
    private World? _world;
    private readonly CommandBuffer _cmd;
    private ComponentStore<EnemyComponent>? _enemies;
    private readonly Random _rng = new(1337);

    private WaveDefinition _currentWave;
    private float _waveTimer;
    private float _spawnCooldown;
    private int _activeEnemies;

    public int CurrentWaveNumber => _currentWave.WaveNumber;
    public float TimeRemaining => MathF.Max(0f, _waveTimer);
    public int ActiveEnemyCount => _activeEnemies;
    public bool IsWaveRunning { get; private set; }
    public bool WaveCompleted { get; private set; }

    public WaveSystem(CommandBuffer cmd)
    {
        _cmd = cmd;
        _currentWave = WaveDatabase.GetWave(1);
    }

    public void Initialize(World world)
    {
        _world = world;
        _enemies = world.GetStore<EnemyComponent>();
        _waveTimer = _currentWave.DurationSeconds;
        IsWaveRunning = false;
        WaveCompleted = false;
    }

    public void StartWave(int waveNumber)
    {
        _currentWave = WaveDatabase.GetWave(waveNumber);
        _waveTimer = _currentWave.DurationSeconds;
        _spawnCooldown = 0.3f;
    }

    public void StartCurrentWave()
    {
        IsWaveRunning = true;
        WaveCompleted = false;
        _waveTimer = _currentWave.DurationSeconds;
        _spawnCooldown = 0.3f;
    }

    public void StartNextWave()
    {
        StartWave(_currentWave.WaveNumber + 1);
        IsWaveRunning = true;
        WaveCompleted = false;
    }

    public void Reset()
    {
        StartWave(1);
        IsWaveRunning = false;
        WaveCompleted = false;
    }

    public void Update(float deltaTime)
    {
        if (!IsWaveRunning || _world == null) return;

        _activeEnemies = _enemies != null ? _enemies.Count : 0;

        _waveTimer -= deltaTime;

        if (_waveTimer <= 0f)
        {
            foreach (Entity e in _world.Query().With<EnemyComponent>())
            {
                _cmd.DestroyEntity(e);
            }

            IsWaveRunning = false;
            WaveCompleted = true;
            return;
        }

        _spawnCooldown -= deltaTime;

        if (_spawnCooldown <= 0f && _activeEnemies < _currentWave.MaxActiveEnemies)
        {
            _spawnCooldown = _currentWave.SpawnInterval;
            int batch = _rng.Next(_currentWave.BatchMin, _currentWave.BatchMax + 1);

            for (int i = 0; i < batch; i++)
            {
                if (_activeEnemies >= _currentWave.MaxActiveEnemies) break;

                EnemyType type = _currentWave.SelectRandomEnemy(_rng);
                GetPerimeterSpawnPos(out float x, out float y);

                EnemySpawner.Spawn(_world, type, x, y);
                _activeEnemies++;
            }
        }
    }

    private void GetPerimeterSpawnPos(out float x, out float y)
    {
        int side = _rng.Next(4);
        float minX = ArenaConfig.Bounds.X + 20f;
        float maxX = ArenaConfig.Bounds.X + ArenaConfig.Bounds.Width - 20f;
        float minY = ArenaConfig.Bounds.Y + 20f;
        float maxY = ArenaConfig.Bounds.Y + ArenaConfig.Bounds.Height - 20f;

        switch (side)
        {
            case 0:
                x = minX + (float)_rng.NextDouble() * (maxX - minX);
                y = minY;
                break;
            case 1:
                x = minX + (float)_rng.NextDouble() * (maxX - minX);
                y = maxY;
                break;
            case 2:
                x = minX;
                y = minY + (float)_rng.NextDouble() * (maxY - minY);
                break;
            default:
                x = maxX;
                y = minY + (float)_rng.NextDouble() * (maxY - minY);
                break;
        }
    }
}
