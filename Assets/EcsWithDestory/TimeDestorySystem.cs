using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(MoveSystem))]
public class TimeDestorySystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem buffer;

    protected override void OnCreate()
    {
        buffer = World.Active.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    struct CullingJob : IJobForEachWithEntity<TimeToLiveComponent>
    {
        public EntityCommandBuffer.Concurrent commands;
        public float dt;

        public void Execute(Entity entity, int index, ref TimeToLiveComponent timeToLiveComponent)
        {
            timeToLiveComponent.timeToLive -= dt;
            if (timeToLiveComponent.timeToLive <= 0f)
                commands.DestroyEntity(index, entity);
        }
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        CullingJob job = new CullingJob
        {
            commands = buffer.CreateCommandBuffer().ToConcurrent(),
            dt = Time.deltaTime
        };

        JobHandle handle = job.Schedule(this, inputDeps);
        buffer.AddJobHandleForProducer(handle);

        return handle;
    }
}
