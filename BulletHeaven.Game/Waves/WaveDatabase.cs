namespace BulletHeaven.Game.Waves;

public static class WaveDatabase
{
    public static WaveDefinition GetWave(int waveNumber)
    {
        return waveNumber switch
        {
            1 => new WaveDefinition
            {
                WaveNumber = 1,
                DurationSeconds = 20f,
                SpawnInterval = 0.85f,
                BatchMin = 2,
                BatchMax = 3,
                MaxActiveEnemies = 100,
                EnemyPool = new[]
                {
                    new EnemySpawnWeight(EnemyType.Swarmer, 100)
                }
            },
            2 => new WaveDefinition
            {
                WaveNumber = 2,
                DurationSeconds = 25f,
                SpawnInterval = 0.8f,
                BatchMin = 2,
                BatchMax = 3,
                MaxActiveEnemies = 80,
                EnemyPool = new[]
                {
                    new EnemySpawnWeight(EnemyType.Swarmer, 75),
                    new EnemySpawnWeight(EnemyType.Charger, 25)
                }
            },
            3 => new WaveDefinition
            {
                WaveNumber = 3,
                DurationSeconds = 30f,
                SpawnInterval = 0.75f,
                BatchMin = 2,
                BatchMax = 4,
                MaxActiveEnemies = 100,
                EnemyPool = new[]
                {
                    new EnemySpawnWeight(EnemyType.Swarmer, 50),
                    new EnemySpawnWeight(EnemyType.Charger, 30),
                    new EnemySpawnWeight(EnemyType.Spitter, 20)
                }
            },
            _ => new WaveDefinition
            {
                WaveNumber = waveNumber,
                DurationSeconds = 30f + (waveNumber - 3) * 5f,
                SpawnInterval = MathF.Max(0.4f, 0.75f - (waveNumber - 3) * 0.05f),
                BatchMin = 3,
                BatchMax = 4 + (waveNumber - 3),
                MaxActiveEnemies = 100 + (waveNumber - 3) * 20,
                EnemyPool = new[]
                {
                    new EnemySpawnWeight(EnemyType.Swarmer, 40),
                    new EnemySpawnWeight(EnemyType.Charger, 35),
                    new EnemySpawnWeight(EnemyType.Spitter, 25)
                }
            }
        };
    }
}

