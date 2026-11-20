using BulletHeaven.ECS.Core;

namespace BulletHeaven.Game.Components;

public struct OrbitProjectileComponent
{
    public Entity Owner;
    public int SlotIndex;
    public int OrbIndex;
    public int TotalOrbs;
    public float Angle;
    public float OrbitRadius;
    public float DeadzoneRadius;
    public float OrbitSpeed;
    public float Damage;
    public float Knockback;
    public float OrbRadius;
}
