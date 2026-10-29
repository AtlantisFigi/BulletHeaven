namespace BulletHeaven.Game.Components;

public struct RenderColorComponent
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;
    public float Radius;

    public RenderColorComponent(byte r, byte g, byte b, byte a = 255, float radius = 3f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
        Radius = radius;
    }
}

