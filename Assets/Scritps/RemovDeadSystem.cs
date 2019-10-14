using Unity.Entities;

public class RemovDeadSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref Times times) =>
        {
            if (times.timeByComponet > 2)
            {
                PostUpdateCommands.DestroyEntity(entity);
            }
        });
    }
}
