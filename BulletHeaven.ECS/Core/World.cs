using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Query;
using BulletHeaven.ECS.Storage;

namespace BulletHeaven.ECS;

public sealed class World
{
    private readonly EntityPool _pool = new();
    private readonly Dictionary<Type, IComponentStore> _stores = new();

    public Entity CreateEntity() => _pool.Rent();

    public void DestroyEntity(Entity entity)
    {
        if (!IsAlive(entity)) return;

        foreach (IComponentStore store in _stores.Values)
            store.Remove(entity.Id);

        _pool.Return(entity);
    }

    public bool IsAlive(Entity entity) => _pool.IsAlive(entity);

    internal Entity GetEntity(int id) => new(id, _pool.GetVersion(id));

    public ComponentStore<T> GetStore<T>() where T : struct
    {
        Type key = typeof(T);
        if (!_stores.TryGetValue(key, out IComponentStore? store))
        {
            store = new ComponentStore<T>();
            _stores[key] = store;
        }
        return (ComponentStore<T>)store;
    }

    public void AddComponent<T>(Entity entity, T component) where T : struct
        => GetStore<T>().Add(entity, component);

    public void RemoveComponent<T>(Entity entity) where T : struct
        => GetStore<T>().Remove(entity);

    public ref T GetComponent<T>(Entity entity) where T : struct
        => ref GetStore<T>().Get(entity);

    public bool HasComponent<T>(Entity entity) where T : struct
        => GetStore<T>().Has(entity);

    public EntityQuery Query() => new(this);

    public int EntityCount => _pool.AliveCount;

    public int ComponentTypeCount => _stores.Count;
}

