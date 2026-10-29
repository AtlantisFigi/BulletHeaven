using BulletHeaven.ECS.Core;

namespace BulletHeaven.ECS.Storage;
public sealed class ComponentStore<T> : IComponentStore where T : struct
{
    private const int DefaultSparseCapacity = 1024;

    private int[]    _sparse;
    private T[]      _dense;     
    private int[]    _denseIds;   
    private int      _count; 

    public ComponentStore(int initialDenseCapacity = 256)
    {
        _sparse   = new int[DefaultSparseCapacity];
        _dense    = new T[initialDenseCapacity];
        _denseIds = new int[initialDenseCapacity];
        _count    = 0;

        Array.Fill(_sparse, -1);
    }

    public int Count => _count;

    public ReadOnlySpan<int> EntityIds => new(_denseIds, 0, _count);

    bool IComponentStore.Has(int entityId) => Has(entityId);

    void IComponentStore.Remove(int entityId) => Remove(entityId);


    public void Add(Entity entity, T component)
    {
        int id = entity.Id;
        EnsureSparse(id);

        if (_sparse[id] != -1)
            throw new InvalidOperationException(
                $"Entity {entity} already has component {typeof(T).Name}.");

        if (_count == _dense.Length) Grow();

        _sparse[id]       = _count;
        _dense[_count]    = component;
        _denseIds[_count] = id;
        _count++;
    }

    public void Remove(Entity entity) => Remove(entity.Id);

    public void Remove(int entityId)
    {
        if (entityId >= _sparse.Length || _sparse[entityId] < 0) return;

        int removedIndex = _sparse[entityId];
        int lastIndex    = _count - 1;

        if (removedIndex != lastIndex)
        {
            _dense[removedIndex]    = _dense[lastIndex];
            _denseIds[removedIndex] = _denseIds[lastIndex];
            _sparse[_denseIds[removedIndex]] = removedIndex;
        }

        _sparse[entityId] = -1;
        _dense[lastIndex] = default;
        _count--;
    }

    public bool Has(Entity entity) => Has(entity.Id);

    public bool Has(int entityId) =>
        entityId < _sparse.Length && _sparse[entityId] >= 0;


    public ref T Get(Entity entity)
    {
        int id = entity.Id;
        if (!Has(id))
            throw new InvalidOperationException(
                $"Entity {entity} does not have component {typeof(T).Name}.");

        return ref _dense[_sparse[id]];
    }


    public ref T TryGet(Entity entity, out bool found)
    {
        int id = entity.Id;
        if (Has(id))
        {
            found = true;
            return ref _dense[_sparse[id]];
        }
        found = false;
        return ref System.Runtime.CompilerServices.Unsafe.NullRef<T>();
    }


    public Span<T> GetSpan() => new(_dense, 0, _count);


    public ReadOnlySpan<int> GetEntityIds() => new(_denseIds, 0, _count);


    private void EnsureSparse(int id)
    {
        if (id >= _sparse.Length)
        {
            int newSize = Math.Max(id + 1, _sparse.Length * 2);
            int old = _sparse.Length;
            Array.Resize(ref _sparse, newSize);
            Array.Fill(_sparse, -1, old, newSize - old);
        }
    }

    private void Grow()
    {
        int newSize = _dense.Length * 2;
        Array.Resize(ref _dense, newSize);
        Array.Resize(ref _denseIds, newSize);
    }
}

