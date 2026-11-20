namespace BulletHeaven.Game.Waves;

public sealed class WaveDefinition
{
    public int WaveNumber { get; init; }
    public float DurationSeconds { get; init; }
    public float SpawnInterval { get; init; }
    public int BatchMin { get; init; }
    public int BatchMax { get; init; }
    public int MaxActiveEnemies { get; init; }
    public IReadOnlyList<EnemySpawnWeight> EnemyPool { get; init; } = Array.Empty<EnemySpawnWeight>();

    public EnemyType SelectRandomEnemy(Random rng)
    {
        if (EnemyPool.Count == 0) return EnemyType.Swarmer;

        int totalWeight = 0;
        for (int i = 0; i < EnemyPool.Count; i++)
        {
            totalWeight += EnemyPool[i].Weight;
        }

        if (totalWeight <= 0) return EnemyPool[0].Type;

        int roll = rng.Next(totalWeight);
        int accumulated = 0;

        for (int i = 0; i < EnemyPool.Count; i++)
        {
            accumulated += EnemyPool[i].Weight;
            if (roll < accumulated)
            {
                return EnemyPool[i].Type;
            }
        }

        return EnemyPool[0].Type;
    }
}

