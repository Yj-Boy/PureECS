/***
 * 
 *   Pure ECS 项目演示
 *   
 *   系统： 时间的处理（仅仅做时间的累加）
 * 
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;

public class TimesSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Times timeObj)=> {
            //简单的时间累加
            timeObj.timeByComponet += 1F * Time.deltaTime;
        });
    }
}
