using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;

public class GamePlayManager : MonoBehaviour
{
    public float speed = 1f;
    EntityManager entityManager;

    //字段（序列化字段）
    [SerializeField]
    private Mesh _Mesh;
    [SerializeField]
    private Material _Material;

    private void Start()
    {
        entityManager = World.Active.EntityManager;
        //创建“实体原型类型”    
        this.AddShips(100);
    }

    private void Update()
    {
        if (Input.GetKeyDown("space"))
            AddShips(100);
    }

    private void AddShips(int amount)
    {
        EntityArchetype entityAr = entityManager.CreateArchetype(
            typeof(MoveSpeed),         //自定义移动组件
            typeof(Translation),    //系统移动组件
            typeof(Rotation),       //系统选抓组件
            typeof(RenderMesh),     //渲染组件
            typeof(LocalToWorld)    //渲染支持组件
            );
        NativeArray<Entity> entities = new NativeArray<Entity>(amount, Allocator.Persistent);
        //创建实体，且连接“实体原型类型”与“本地集合类型”结合起来
        entityManager.CreateEntity(entityAr, entities);

        for (int i=0;i<entities.Length;i++)
        {
            float x = UnityEngine.Random.Range(-10, 10);
            float z = UnityEngine.Random.Range(-10, 10);

            entityManager.SetComponentData(entities[i], new MoveSpeed { Value = speed });
            entityManager.SetComponentData(entities[i], new Translation { Value = new float3(x, 0f, z) });
            entityManager.SetComponentData(entities[i], new Rotation { Value = new quaternion(0, 1, 0, 0) });

            //添加共享组件，进行（每一个）实体的渲染。
            entityManager.SetSharedComponentData(entities[i], new RenderMesh
            {
                material = _Material,
                mesh = _Mesh
            });
        }
        entities.Dispose();
    }
}
