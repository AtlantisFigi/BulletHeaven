using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;

namespace BulletHeaven.ECS.Query;

public sealed class EntityQuery : IEnumerable<Entity>
{
    private readonly World _world;
    private readonly List<IComponentStore>  _with    = new();
    private readonly List<IComponentStore>  _without = new();

    internal EntityQuery(World world) => _world = world;

    public EntityQuery With<T>() where T : struct
    {
        _with.Add(_world.GetStore<T>());
        return this;
    }

    public EntityQuery Without<T>() where T : struct
    {
        _without.Add(_world.GetStore<T>());
        return this;
    }

    public IEnumerator<Entity> GetEnumerator()
    {
        if (_with.Count == 0) yield break;

        IComponentStore primary = _with[0];
        for (int i = 1; i < _with.Count; i++)
            if (_with[i].Count < primary.Count) primary = _with[i];

        int[] ids = primary.EntityIds.ToArray();

        for (int i = 0; i < ids.Length; i++)
        {
            int id = ids[i];

            bool pass = true;

            foreach (IComponentStore store in _with)
            {
                if (!store.Has(id)) { pass = false; break; }
            }
            if (!pass) continue;

            foreach (IComponentStore store in _without)
            {
                if (store.Has(id)) { pass = false; break; }
            }
            if (!pass) continue;

            yield return _world.GetEntity(id);
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        => GetEnumerator();
}
