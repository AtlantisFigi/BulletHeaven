using System;
using Raylib_cs;

namespace BulletHeaven.Game.Rendering;

public sealed class DamageTextService
{
    private struct FloatingText
    {
        public float X;
        public float Y;
        public float VelY;
        public string Text;
        public Color BaseColor;
        public float Lifetime;
        public float MaxLifetime;
        public bool Active;
    }

    private readonly FloatingText[] _pool;
    private int _nextIndex;

    public bool Enabled { get; set; } = true;

    public DamageTextService(int capacity = 128)
    {
        _pool = new FloatingText[capacity];
        _nextIndex = 0;
    }

    public void Spawn(float x, float y, string text, Color color, float lifetime = 0.65f)
    {
        if (!Enabled) return;

        int index = _nextIndex;
        _nextIndex = (_nextIndex + 1) % _pool.Length;

        _pool[index] = new FloatingText
        {
            X = x,
            Y = y,
            VelY = -40f,
            Text = text,
            BaseColor = color,
            Lifetime = lifetime,
            MaxLifetime = lifetime,
            Active = true
        };
    }

    public void Update(float deltaTime)
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].Active) continue;

            _pool[i].Y += _pool[i].VelY * deltaTime;
            _pool[i].Lifetime -= deltaTime;

            if (_pool[i].Lifetime <= 0f)
            {
                _pool[i].Active = false;
            }
        }
    }

    public void Draw()
    {
        if (!Enabled) return;

        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_pool[i].Active) continue;

            float alphaRatio = Math.Clamp(_pool[i].Lifetime / _pool[i].MaxLifetime, 0f, 1f);
            byte alpha = (byte)(_pool[i].BaseColor.A * alphaRatio);
            Color drawCol = new(_pool[i].BaseColor.R, _pool[i].BaseColor.G, _pool[i].BaseColor.B, alpha);

            int textW = Raylib.MeasureText(_pool[i].Text, 14);
            int drawX = (int)(_pool[i].X - textW * 0.5f);
            int drawY = (int)_pool[i].Y;

            Raylib.DrawText(_pool[i].Text, drawX + 1, drawY + 1, 14, new Color(0, 0, 0, (int)alpha));
            Raylib.DrawText(_pool[i].Text, drawX, drawY, 14, drawCol);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            _pool[i].Active = false;
        }
    }
}
