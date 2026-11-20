namespace BulletHeaven.Game.Components;

public enum ProjectileShape : byte
{
    Circle = 0,
    PiercingLine = 1,
    Boomerang = 2,
    BroadsideBolt = 3
}

public struct ProjectileVisualComponent
{
    public ProjectileShape Shape;
    public float Angle;
    public float Length;
    public float Thickness;
    public float SpinAngle;
    public float SpinSpeed;

    public ProjectileVisualComponent(
        ProjectileShape shape,
        float angle = 0f,
        float length = 12f,
        float thickness = 4f,
        float spinAngle = 0f,
        float spinSpeed = 0f)
    {
        Shape = shape;
        Angle = angle;
        Length = length;
        Thickness = thickness;
        SpinAngle = spinAngle;
        SpinSpeed = spinSpeed;
    }
}
