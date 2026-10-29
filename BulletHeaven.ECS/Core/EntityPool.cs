namespace BulletHeaven.ECS.Core;
internal sealed class EntityPool
{
    private readonly Queue<int> _recycled = new();
    private readonly List<int>  _versions;
    private int _nextId = 1;

    public EntityPool(int initialCapacity = 1024)
    {
        _versions = new List<int>(initialCapacity) { 0 };
    }

    public Entity Rent()
    {
        if (_recycled.TryDequeue(out int recycledId))
        {
            return new Entity(recycledId, _versions[recycledId]);
        }

        int id = _nextId++;
        _versions.Add(1);
        return new Entity(id, 1);
    }

    public void Return(Entity entity)
    {
        if (!entity.IsValid) return;

        _versions[entity.Id]++;
        _recycled.Enqueue(entity.Id);
    }

    public bool IsAlive(Entity entity) =>
        entity.IsValid &&
        entity.Id < _versions.Count &&
        _versions[entity.Id] == entity.Version;

    public int GetVersion(int id) => id < _versions.Count ? _versions[id] : 0;

    public int AliveCount => _nextId - 1 - _recycled.Count;
}

