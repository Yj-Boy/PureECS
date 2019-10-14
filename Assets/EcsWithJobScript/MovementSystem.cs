using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;

public class MovementSystem : JobComponentSystem
{
    struct MovementJob : IJobForEach<Translation, Rotation, MoveSpeed>
    {
        public float topBound;
        public float bottomBound;
        public float deltaTime;

        public void Execute(ref Translation translation, ref Rotation rotation, ref MoveSpeed speed)
        {
            float3 value = translation.Value;

            value += deltaTime * speed.Value * math.forward(rotation.Value);

            if (value.z < bottomBound)
                value.z = topBound;

            translation.Value = value;
        }
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        MovementJob moveJob = new MovementJob
        {
            topBound = 10,
            bottomBound = -10,
            deltaTime = Time.deltaTime
        };

        JobHandle moveHandle = moveJob.Schedule(this, inputDeps);

        return moveHandle;
    }
}
