using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;


public class GamePlayerManager : MonoBehaviour
{
    public int amount;
    public int left;
    public int right;
    public int spawnZ;
    public int speed;

    EntityManager manager;

    //player渲染组件
    [SerializeField]
    private Mesh _Mesh;
    [SerializeField]
    private Material _Material;

    //enemy渲染组件
    [SerializeField]
    private Mesh _enemyMesh;
    [SerializeField]
    private Material _enemyMaterial;

    private void Start()
    {
        manager = World.Active.EntityManager;

        EntityArchetype entityArc = manager.CreateArchetype(
            typeof(Translation),
            typeof(MoveSpeedComponent),
            typeof(MoveForwardComponent),
            typeof(TimeToLiveComponent),
            typeof(RenderMesh),
            typeof(LocalToWorld)
            );

        //创建player
        NativeArray<Entity> entityArr = new NativeArray<Entity>(amount, Allocator.Persistent);

        manager.CreateEntity(entityArc, entityArr);

        for(int i=0;i<entityArr.Length;i++)
        {
            manager.SetComponentData(entityArr[i], new Translation { Value = new float3(UnityEngine.Random.Range(left, right), 0f, UnityEngine.Random.Range(spawnZ, -10f)) });
            manager.SetComponentData(entityArr[i], new MoveSpeedComponent { moveSpeed = speed });
            manager.SetComponentData(entityArr[i], new MoveForwardComponent { moveForward = 1f });
            manager.SetComponentData(entityArr[i], new TimeToLiveComponent { timeToLive = 20f });

            manager.SetSharedComponentData(entityArr[i], new RenderMesh { material = _Material, mesh = _Mesh });

            manager.AddComponent(entityArr[i], typeof(PlayerTagComponent));

            manager.AddComponentData(entityArr[i], new HealthComponent { health = 10 });
        }
        entityArr.Dispose();

        //创建enemy
        NativeArray<Entity> entityEmenyArr = new NativeArray<Entity>(amount, Allocator.Persistent);

        manager.CreateEntity(entityArc, entityEmenyArr);

        for (int i = 0; i < entityEmenyArr.Length; i++)
        {
            manager.SetComponentData(entityEmenyArr[i], new Translation { Value = new float3(UnityEngine.Random.Range(left, right), 0f, UnityEngine.Random.Range(5f, 10f)) });
            manager.SetComponentData(entityEmenyArr[i], new MoveSpeedComponent { moveSpeed = speed });
            manager.SetComponentData(entityEmenyArr[i], new MoveForwardComponent { moveForward = -1f });
            manager.SetComponentData(entityEmenyArr[i], new TimeToLiveComponent { timeToLive = 20f });

            manager.SetSharedComponentData(entityEmenyArr[i], new RenderMesh { material = _enemyMaterial, mesh = _enemyMesh });

            manager.AddComponent(entityEmenyArr[i], typeof(EnemyTagComponent));
            manager.AddComponentData(entityEmenyArr[i], new HealthComponent { health = 1 });
        }
        entityEmenyArr.Dispose();
    }
}
