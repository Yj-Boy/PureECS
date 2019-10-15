using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class RemoveDeadSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref HealthComponent healthComponent) =>
        {
            if(healthComponent.health<=0)
            {
                PostUpdateCommands.DestroyEntity(entity);
            }
        });
    }
}
