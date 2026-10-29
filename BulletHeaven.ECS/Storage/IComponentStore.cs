namespace BulletHeaven.ECS.Storage;
public interface IComponentStore
{
    void Remove(int entityId);

    bool Has(int entityId);

    int Count { get; }

    ReadOnlySpan<int> EntityIds { get; }
}

