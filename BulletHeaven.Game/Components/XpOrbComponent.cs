namespace BulletHeaven.Game.Components;

public struct XpOrbComponent
{
    public int Value;
    public bool IsMagnetized;

    public XpOrbComponent(int value = 1, bool isMagnetized = false)
    {
        Value = value;
        IsMagnetized = isMagnetized;
    }
}
