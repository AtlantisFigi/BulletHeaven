namespace BulletHeaven.ECS.Systems;

public sealed class SystemScheduler
{
    private readonly List<ISystem> _systems = new();
    private ECS.World? _world;

    public SystemScheduler Register(ISystem system)
    {
        _systems.Add(system);
        return this;
    }

    public void Initialize(ECS.World world)
    {
        _world = world;
        foreach (ISystem system in _systems)
            system.Initialize(world);
    }

    public void UpdateAll(float deltaTime)
    {
        foreach (ISystem system in _systems)
            system.Update(deltaTime);
    }

    public int Count => _systems.Count;
}

