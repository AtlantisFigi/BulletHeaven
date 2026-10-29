using System.Collections;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;

namespace BulletHeaven.ECS.Query;

public sealed class EntityQuery : IEnumerable<Entity>
{
    private readonly World _world;
    private readonly List<IComponentStore> _with = new();
    private readonly List<IComponentStore> _without = new();

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

    public Enumerator GetEnumerator()
    {
        if (_with.Count == 0) return new Enumerator(_world, ReadOnlySpan<int>.Empty, _with, _without);

        IComponentStore primary = _with[0];
        for (int i = 1; i < _with.Count; i++)
        {
            if (_with[i].Count < primary.Count)
            {
                primary = _with[i];
            }
        }

        return new Enumerator(_world, primary.EntityIds, _with, _without);
    }

    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
    {
        if (_with.Count == 0) yield break;

        IComponentStore primary = _with[0];
        for (int i = 1; i < _with.Count; i++)
        {
            if (_with[i].Count < primary.Count)
            {
                primary = _with[i];
            }
        }

        int count = primary.Count;
        for (int i = 0; i < count; i++)
        {
            int id = primary.EntityIds[i];
            bool pass = true;

            for (int j = 0; j < _with.Count; j++)
            {
                if (!_with[j].Has(id))
                {
                    pass = false;
                    break;
                }
            }
            if (!pass) continue;

            for (int j = 0; j < _without.Count; j++)
            {
                if (_without[j].Has(id))
                {
                    pass = false;
                    break;
                }
            }
            if (!pass) continue;

            yield return _world.GetEntity(id);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Entity>)this).GetEnumerator();

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly ReadOnlySpan<int> _ids;
        private readonly List<IComponentStore> _with;
        private readonly List<IComponentStore> _without;
        private int _index;
        private Entity _current;

        public Enumerator(World world, ReadOnlySpan<int> ids, List<IComponentStore> with, List<IComponentStore> without)
        {
            _world = world;
            _ids = ids;
            _with = with;
            _without = without;
            _index = -1;
            _current = default;
        }

        public bool MoveNext()
        {
            while (++_index < _ids.Length)
            {
                int id = _ids[_index];
                bool pass = true;

                for (int i = 0; i < _with.Count; i++)
                {
                    if (!_with[i].Has(id))
                    {
                        pass = false;
                        break;
                    }
                }
                if (!pass) continue;

                for (int i = 0; i < _without.Count; i++)
                {
                    if (_without[i].Has(id))
                    {
                        pass = false;
                        break;
                    }
                }
                if (!pass) continue;

                _current = _world.GetEntity(id);
                return true;
            }

            return false;
        }

        public Entity Current => _current;
    }
}
