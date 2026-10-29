namespace BulletHeaven.ECS.Core;
public readonly struct Entity : IEquatable<Entity>
{
    public static readonly Entity Invalid = new(0, 0);

    public readonly int Id;
    public readonly int Version;

    internal Entity(int id, int version)
    {
        Id      = id;
        Version = version;
    }

    public bool IsValid => Id > 0;

    public bool Equals(Entity other) => Id == other.Id && Version == other.Version;
    public override bool Equals(object? obj) => obj is Entity e && Equals(e);
    public override int GetHashCode() => HashCode.Combine(Id, Version);
    public override string ToString() => $"Entity({Id}:v{Version})";

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}

