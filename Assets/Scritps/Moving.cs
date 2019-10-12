/***
 * 
 *   Pure ECS 项目演示
 *   
 *   组件： 主角移动
 * 
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public struct Moving :IComponentData
{
    //运动的速度
    public float MoveSpeed;
}
