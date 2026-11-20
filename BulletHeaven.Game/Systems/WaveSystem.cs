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
    private ComponentStore<EnemyComponent>? _enemies;

    private WaveDefinition _currentWave;
    private float _waveTimer;
    private float _spawnCooldown;
    private int _activeEnemies;

    public int CurrentWaveNumber => _currentWave.WaveNumber;
    public float TimeRemaining => MathF.Max(0f, _waveTimer);
    public int ActiveEnemyCount => _activeEnemies;
    public bool IsWaveRunning { get; private set; }

    public WaveSystem()
    {
        _currentWave = WaveDatabase.GetWave(1);
    }

    public void Initialize(World world)
    {
        _world = world;
        _enemies = world.GetStore<EnemyComponent>();
        _waveTimer = _currentWave.DurationSeconds;
        IsWaveRunning = false;
    }

    public void StartWave(int waveNumber)
    {
        _currentWave = WaveDatabase.GetWave(waveNumber);
        _waveTimer = _currentWave.DurationSeconds;
        _spawnCooldown = 0f;
    }

    public void StartCurrentWave()
    {
        IsWaveRunning = true;
        _waveTimer = _currentWave.DurationSeconds;
        _spawnCooldown = 0f;
    }

    public void StartNextWave()
    {
        StartWave(_currentWave.WaveNumber + 1);
        IsWaveRunning = true;
    }

    public void Reset()
    {
        StartWave(1);
        _activeEnemies = 0;
        IsWaveRunning = false;
    }

    public void SyncActiveEnemyCount()
    {
        _activeEnemies = _enemies != null ? _enemies.Count : 0;
    }

    public void Update(float deltaTime)
    {
        if (!IsWaveRunning || _world == null) return;

        _activeEnemies = _enemies != null ? _enemies.Count : 0;

        _waveTimer -= deltaTime;

        while (_waveTimer <= 0f)
        {
            _currentWave = WaveDatabase.GetWave(_currentWave.WaveNumber + 1);
            _waveTimer += _currentWave.DurationSeconds;
        }

        _spawnCooldown -= deltaTime;

        if (_spawnCooldown <= 0f && _activeEnemies < _currentWave.MaxActiveEnemies)
        {
            _spawnCooldown = _currentWave.SpawnInterval;
            int batch = Random.Shared.Next(_currentWave.BatchMin, _currentWave.BatchMax + 1);

            for (int i = 0; i < batch; i++)
            {
                if (_activeEnemies >= _currentWave.MaxActiveEnemies) break;

                EnemyType type = _currentWave.SelectRandomEnemy(Random.Shared);
                GetPerimeterSpawnPos(out float x, out float y);

                EnemySpawner.Spawn(_world, type, x, y);
                _activeEnemies++;
            }
        }
    }

    private static void GetPerimeterSpawnPos(out float x, out float y)
    {
        int side = Random.Shared.Next(4);
        float minX = ArenaConfig.Bounds.X + 20f;
        float maxX = ArenaConfig.Bounds.X + ArenaConfig.Bounds.Width - 20f;
        float minY = ArenaConfig.Bounds.Y + 20f;
        float maxY = ArenaConfig.Bounds.Y + ArenaConfig.Bounds.Height - 20f;

        switch (side)
        {
            case 0:
                x = minX + (float)Random.Shared.NextDouble() * (maxX - minX);
                y = minY;
                break;
            case 1:
                x = minX + (float)Random.Shared.NextDouble() * (maxX - minX);
                y = maxY;
                break;
            case 2:
                x = minX;
                y = minY + (float)Random.Shared.NextDouble() * (maxY - minY);
                break;
            default:
                x = maxX;
                y = minY + (float)Random.Shared.NextDouble() * (maxY - minY);
                break;
        }
    }
}
