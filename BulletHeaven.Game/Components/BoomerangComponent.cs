using BulletHeaven.ECS.Core;

namespace BulletHeaven.Game.Components;

public struct BoomerangComponent
{
    public Entity Owner;
    public float OutboundDistanceRemaining;
    public float Speed;
    public float RotationAngle;
    public bool IsReturning;
    public float MaxReturnDistance;
    public float ReturnTargetX;
    public float ReturnTargetY;
    public bool PassedReturnTarget;
}
