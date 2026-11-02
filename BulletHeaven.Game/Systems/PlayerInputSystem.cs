using BulletHeaven.ECS;
using BulletHeaven.ECS.Core;
using BulletHeaven.ECS.Storage;
using BulletHeaven.ECS.Systems;
using BulletHeaven.Game.Components;
using Raylib_cs;

namespace BulletHeaven.Game.Systems;

public sealed class PlayerInputSystem : ISystem
{
    private World? _world;
    private ComponentStore<PlayerComponent>? _players;
    private ComponentStore<VelocityComponent>? _velocities;

    public void Initialize(World world)
    {
        _world = world;
        _players = world.GetStore<PlayerComponent>();
        _velocities = world.GetStore<VelocityComponent>();
    }

    public void Update(float deltaTime)
    {
        if (_world == null || _players == null || _velocities == null) return;

        float dx = 0f;
        float dy = 0f;

        if (Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.Up)) dy -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.Down)) dy += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) dx -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) dx += 1f;

        float lenSq = dx * dx + dy * dy;
        if (lenSq > 0f)
        {
            float inv = 1f / MathF.Sqrt(lenSq);
            dx *= inv;
            dy *= inv;
        }

        foreach (Entity e in _world.Query().With<PlayerComponent>().With<VelocityComponent>())
        {
            ref PlayerComponent player = ref _players.Get(e);
            ref VelocityComponent vel = ref _velocities.Get(e);

            vel.X = dx * player.Speed;
            vel.Y = dy * player.Speed;

            if (lenSq > 0f)
            {
                player.FacingAngle = MathF.Atan2(dy, dx);
            }
        }
    }
}

