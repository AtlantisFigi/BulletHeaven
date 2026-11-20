using BulletHeaven.ECS.Core;

namespace BulletHeaven.Game.Components;

public struct HazardZoneComponent
{
    public Entity Owner;
    public float Radius;
    public float RemainingDuration;
    public float MaxDuration;
    public float Damage;
    public float TickInterval;
    public float TickTimer;
}
