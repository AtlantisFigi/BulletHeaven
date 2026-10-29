namespace BulletHeaven.Game.Benchmarking;

public sealed class BenchmarkResult
{
    public required string Scenario { get; init; }
    public required int EntityCount { get; init; }
    public required float AvgFps { get; init; }
    public required float MinFps { get; init; }
    public required float AvgFrameTimeMs { get; init; }
    public required float P99FrameTimeMs { get; init; }
    public required float MaxFrameTimeMs { get; init; }
    public required int Gen0Collections { get; init; }
    public required float MemoryMb { get; init; }
}

