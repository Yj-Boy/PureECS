/***
 * 
 *   Pure ECS 项目演示
 *   
 *   系统： 主角的移动处理
 * 
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Unity.Transforms;

public class MovingSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Translation translation,ref Moving moving)=> {
            //实体进行向上运动
            translation.Value.y += moving.MoveSpeed * Time.deltaTime;
            //运动边界判断
            //Debug.Log("translation.Value.y="+ translation.Value.y);
            if (translation.Value.y>8F)
            {
                moving.MoveSpeed = -Mathf.Abs(moving.MoveSpeed);
            }
            if (translation.Value.y <-6F)
            {
                moving.MoveSpeed = Mathf.Abs(moving.MoveSpeed);
            }
        });
    }
}
