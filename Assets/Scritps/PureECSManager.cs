/***
 * 
 *   Pure ECS 项目演示
 *   
 *   实体管理器控制
 * 
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;

public class PureECSManager : MonoBehaviour
{
    //生成精灵的数量
    public int intCreatePlaneNumber = 100;
    //Unity集合
    private NativeArray<Entity> entityArray;

    //字段（序列化字段）
    [SerializeField]  
    private Mesh _Mesh;
    [SerializeField]
    private Material _Material;


    private void Start()
    {
        //创建实体管理器
        EntityManager entityMgr = World.Active.EntityManager;
        //创建“实体原型类型”
        EntityArchetype entityAr = entityMgr.CreateArchetype(
            typeof(Times),          //时间组件
            typeof(Moving),         //自定义移动组件
            typeof(Translation),    //系统移动组件
            typeof(RenderMesh),     //渲染组件
            typeof(LocalToWorld)    //渲染支持组件
            );
        //建立本地集合类型
        entityArray = new NativeArray<Entity>(intCreatePlaneNumber, Allocator.Persistent);
        //创建实体，且连接“实体原型类型”与“本地集合类型”结合起来
        entityMgr.CreateEntity(entityAr, entityArray);
        //循环迭代，给本地结合中每一个实体，添加组件。
        for (int i = 0; i < entityArray.Length; i++)
        {
            /*  设置组件  */
            //设置时间组件
            entityMgr.SetComponentData(entityArray[i],new Times {timeByComponet=1F});
            //设置自定义移动组件
            entityMgr.SetComponentData(entityArray[i], new Moving { MoveSpeed = UnityEngine.Random.Range(1,5)});
            //设置系统移动组件(确定每个实体随机方位)
            entityMgr.SetComponentData(entityArray[i], new Translation { Value=new Unity.Mathematics.float3(UnityEngine.Random.Range(-6,12), UnityEngine.Random.Range(-6,6),0) });

            //添加共享组件，进行（每一个）实体的渲染。
            entityMgr.SetSharedComponentData(entityArray[i], new RenderMesh {
                material = _Material,
                mesh = _Mesh
            });
        }



    }

    /// <summary>
    /// 销毁Unity集合
    /// </summary>
    private void OnDestroy()
    {
        if (entityArray!=null)
        {
            entityArray.Dispose();
        }
    }
}
