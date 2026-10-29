using BulletHeaven.ECS.Core;

namespace BulletHeaven.ECS.Core;

public sealed class CommandBuffer
{
    private readonly List<Action<World>>         _creates  = new();
    private readonly List<Entity>                _destroys = new();
    private readonly List<Action<World>>         _generic  = new();

    public void CreateEntity(Action<World, Entity> setup)
        => _creates.Add(w => setup(w, w.CreateEntity()));

    public void DestroyEntity(Entity entity)
        => _destroys.Add(entity);

    public void AddComponent<T>(Entity entity, T component) where T : struct
        => _generic.Add(w => w.AddComponent(entity, component));

    public void RemoveComponent<T>(Entity entity) where T : struct
        => _generic.Add(w => w.RemoveComponent<T>(entity));

    public void Flush(World world)
    {
        foreach (Action<World> create in _creates)
            create(world);

        foreach (Action<World> cmd in _generic)
            cmd(world);

        foreach (Entity entity in _destroys)
            world.DestroyEntity(entity);

        _creates.Clear();
        _generic.Clear();
        _destroys.Clear();
    }

    public int PendingCount => _creates.Count + _generic.Count + _destroys.Count;
}

