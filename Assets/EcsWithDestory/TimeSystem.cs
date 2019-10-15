using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

public class TimeSystem : ComponentSystem
{
    //struct TimeJob : IJobForEach<TimeToLiveComponent>
    //{
    //    public float deltaTime;
    //    public void Execute(ref TimeToLiveComponent timeToLiveComponent)
    //    {
    //        timeToLiveComponent.timeToLive += 1f * deltaTime;
    //    }
    //}

    //protected override JobHandle OnUpdate(JobHandle inputDeps)
    //{
    //    TimeJob timejob = new TimeJob
    //    {
    //        deltaTime = Time.deltaTime
    //    };

    //    JobHandle timeHandle = timejob.Schedule(this, inputDeps);

    //    return timeHandle;
    //}
    protected override void OnUpdate()
    {
        //Entities.ForEach((ref TimeToLiveComponent timeToLiveComponent) =>
        //{
        //    timeToLiveComponent.timeToLive += 1f * Time.deltaTime;
        //});
    }
}
