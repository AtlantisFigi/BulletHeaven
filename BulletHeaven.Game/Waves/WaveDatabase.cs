namespace BulletHeaven.Game.Waves;

public static class WaveDatabase
{
    public static WaveDefinition GetWave(int waveNumber)
    {
        if (waveNumber < 1) waveNumber = 1;

        return waveNumber switch
        {
            1 => new WaveDefinition
            {
                WaveNumber = 1,
                DurationSeconds = 25f,
                SpawnInterval = 1.80f,
                BatchMin = 1,
                BatchMax = 2,
                MaxActiveEnemies = 14,
                EnemyPool = new[]
                {
                    new EnemySpawnWeight(EnemyType.Swarmer, 75),
                    new EnemySpawnWeight(EnemyType.Charger, 15),
                    new EnemySpawnWeight(EnemyType.Spitter, 10)
                }
            },
            2 => new WaveDefinition
            {
                WaveNumber = 2,
                DurationSeconds = 25f,
                SpawnInterval = 1.55f,
                BatchMin = 1,
                BatchMax = 2,
                MaxActiveEnemies = 20,
                EnemyPool = new[]
                {
                    new EnemySpawnWeight(EnemyType.Swarmer, 72),
                    new EnemySpawnWeight(EnemyType.Charger, 16),
                    new EnemySpawnWeight(EnemyType.Spitter, 12)
                }
            },
            3 => new WaveDefinition
            {
                WaveNumber = 3,
                DurationSeconds = 25f,
                SpawnInterval = 1.35f,
                BatchMin = 1,
                BatchMax = 2,
                MaxActiveEnemies = 26,
                EnemyPool = new[]
                {
                    new EnemySpawnWeight(EnemyType.Swarmer, 68),
                    new EnemySpawnWeight(EnemyType.Charger, 18),
                    new EnemySpawnWeight(EnemyType.Spitter, 14)
                }
            },
            _ => new WaveDefinition
            {
                WaveNumber = waveNumber,
                DurationSeconds = 25f + MathF.Min(15f, (waveNumber - 3) * 2f),
                SpawnInterval = MathF.Max(0.75f, 1.35f - (waveNumber - 3) * 0.06f),
                BatchMin = 1 + (waveNumber - 3) / 4,
                BatchMax = 2 + (waveNumber - 3) / 2,
                MaxActiveEnemies = 26 + (waveNumber - 3) * 7,
                EnemyPool = new[]
                {
                    new EnemySpawnWeight(EnemyType.Swarmer, 65),
                    new EnemySpawnWeight(EnemyType.Charger, 20),
                    new EnemySpawnWeight(EnemyType.Spitter, 15)
                }
            }
        };
    }
}
