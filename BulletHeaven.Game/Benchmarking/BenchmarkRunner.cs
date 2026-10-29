using System.Diagnostics;
using System.Globalization;
using System.Text;
using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Scenarios;
using BulletHeaven.Game.Systems;
using BulletHeaven.Game.Systems.AI;

namespace BulletHeaven.Game.Benchmarking;

public static class BenchmarkRunner
{
    public static BenchmarkResult Run(ScenarioType scenario, int count, float durationSeconds = 5f, float warmupSeconds = 1f)
    {
        var world = new World();
        var cmd = new CommandBuffer();
        var scheduler = new SystemScheduler();

        scheduler.Register(new SwarmAISystem())
                 .Register(new StatefulAISystem(cmd))
                 .Register(new ChargerSystem())
                 .Register(new SpitterSystem(cmd))
                 .Register(new SummonerSystem(cmd))
                 .Register(new MovementSystem());

        scheduler.Initialize(world);

        switch (scenario)
        {
            case ScenarioType.Swarm:
                ScenarioSpawner.SpawnSwarm(world, count);
                break;
            case ScenarioType.StatefulMonolith:
                ScenarioSpawner.SpawnStatefulMonolith(world, count);
                break;
            case ScenarioType.StatefulSegregated:
                ScenarioSpawner.SpawnStatefulSegregated(world, count);
                break;
        }

        var frameTimes = new List<float>(5000);
        var sw = new Stopwatch();

        float elapsedWarmup = 0f;
        while (elapsedWarmup < warmupSeconds)
        {
            sw.Restart();
            scheduler.UpdateAll(0.016f);
            cmd.Flush(world);
            elapsedWarmup += (float)sw.Elapsed.TotalSeconds;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        int startGen0 = GC.CollectionCount(0);
        long startMemory = GC.GetTotalMemory(false);

        float elapsedTest = 0f;
        while (elapsedTest < durationSeconds)
        {
            sw.Restart();
            scheduler.UpdateAll(0.016f);
            cmd.Flush(world);
            float frameMs = (float)sw.Elapsed.TotalMilliseconds;
            frameTimes.Add(frameMs);
            elapsedTest += frameMs / 1000f;
        }

        int endGen0 = GC.CollectionCount(0) - startGen0;
        long endMemory = GC.GetTotalMemory(false);
        float memoryMb = (float)(endMemory - startMemory) / (1024f * 1024f);
        if (memoryMb < 0f) memoryMb = (float)endMemory / (1024f * 1024f);

        frameTimes.Sort();
        float avgMs = frameTimes.Average();
        float maxMs = frameTimes[^1];

        int p99Index = (int)(frameTimes.Count * 0.99f);
        p99Index = Math.Clamp(p99Index, 0, frameTimes.Count - 1);
        float p99Ms = frameTimes[p99Index];

        float avgFps = avgMs > 0 ? 1000f / avgMs : 0f;
        float minFps = maxMs > 0 ? 1000f / maxMs : 0f;

        return new BenchmarkResult
        {
            Scenario = scenario.ToString(),
            EntityCount = count,
            AvgFps = avgFps,
            MinFps = minFps,
            AvgFrameTimeMs = avgMs,
            P99FrameTimeMs = p99Ms,
            MaxFrameTimeMs = maxMs,
            Gen0Collections = endGen0,
            MemoryMb = (float)endMemory / (1024f * 1024f)
        };
    }

    public static List<BenchmarkResult> RunSuite(string outputPath, float durationPerRun = 4f)
    {
        var scenarios = new[] { ScenarioType.Swarm, ScenarioType.StatefulMonolith, ScenarioType.StatefulSegregated };
        var counts = new[] { 1000, 5000, 10000, 25000 };
        var results = new List<BenchmarkResult>();

        Console.WriteLine("================================================================================");
        Console.WriteLine("          BULLET HEAVEN ECS ARCHITECTURAL BENCHMARK (MONOLITH VS SEGREGATED)    ");
        Console.WriteLine("================================================================================");

        foreach (var sc in scenarios)
        {
            foreach (var count in counts)
            {
                Console.Write($"Running {sc,-18} with {count,5} entities... ");
                var res = Run(sc, count, durationPerRun);
                results.Add(res);
                Console.WriteLine($"Avg: {res.AvgFrameTimeMs,6:F2} ms | 1% Low: {res.P99FrameTimeMs,6:F2} ms | FPS: {res.AvgFps,6:F0} | GC0: {res.Gen0Collections,2}");
            }
        }

        SaveCsv(results, outputPath);
        Console.WriteLine("================================================================================");
        Console.WriteLine($"Benchmark voltooid! Resultaten opgeslagen in: {outputPath}");
        Console.WriteLine("================================================================================");

        return results;
    }

    public static void SaveCsv(IEnumerable<BenchmarkResult> results, string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var sb = new StringBuilder();
        sb.AppendLine("Scenario,EntityCount,AvgFps,MinFps,AvgFrameTimeMs,P99FrameTimeMs,MaxFrameTimeMs,Gen0Collections,MemoryMB");

        foreach (var r in results)
        {
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2:F2},{3:F2},{4:F3},{5:F3},{6:F3},{7},{8:F2}",
                r.Scenario, r.EntityCount, r.AvgFps, r.MinFps, r.AvgFrameTimeMs, r.P99FrameTimeMs, r.MaxFrameTimeMs, r.Gen0Collections, r.MemoryMb));
        }

        File.WriteAllText(outputPath, sb.ToString());
    }
}
