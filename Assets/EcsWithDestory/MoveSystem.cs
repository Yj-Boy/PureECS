using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;

public class MoveSystem : JobComponentSystem
{
    struct MovementJob : IJobForEach<Translation, MoveSpeedComponent,MoveForwardComponent>
    {
        public float deltaTime;

        public void Execute(ref Translation translation, ref MoveSpeedComponent moveSpeedComponent, ref MoveForwardComponent moveForwardComponent)
        {
            translation.Value.z+= moveSpeedComponent.moveSpeed*deltaTime*moveForwardComponent.moveForward;
        }
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        MovementJob movementJob = new MovementJob
        {
            deltaTime = Time.deltaTime
        };

        JobHandle moveHandle = movementJob.Schedule(this, inputDeps);

        return moveHandle;
    }
}
