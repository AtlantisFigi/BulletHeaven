namespace BulletHeaven.ECS.Systems;

public interface ISystem
{
    void Initialize(ECS.World world);

    void Update(float deltaTime);
}

