/***
 * 
 *   Pure ECS 项目演示
 *   
 *   组件： 时间
 * 
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public struct Times :IComponentData
{
    public float timeByComponet;
}
