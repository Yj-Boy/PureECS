//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Transforms;

//[UpdateAfter(typeof(MoveSystem))]
//[UpdateBefore(typeof(TimeDestorySystem))]
//public class CollisionSystem : JobComponentSystem
//{
//    //定义组来承接要找的entitiy
//    EntityQuery enemyGroup;
//    EntityQuery playerGroup;

//    protected override void OnCreate()
//    {
//        //通过GetEntityQuery来确定要找的entity
//        playerGroup = GetEntityQuery(typeof(HealthComponent),ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<PlayerTagComponent>());
//        enemyGroup = GetEntityQuery(typeof(HealthComponent),ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<EnemyTagComponent>());
//    }

//    [BurstCompile]
//    struct CollisionJob : IJobChunk
//    {
//        public float radius;

//        public ArchetypeChunkComponentType<HealthComponent> healthType;
//        [ReadOnly] public ArchetypeChunkComponentType<Translation> translationType;

//        [DeallocateOnJobCompletion]
//        [ReadOnly] public NativeArray<Translation> transToTestAgainst;

//        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
//        {
//            var chunkHealths = chunk.GetNativeArray(healthType);
//            var chunkTranslations = chunk.GetNativeArray(translationType);

//            for (int i = 0; i < chunk.Count; i++)
//            {
//                float damage = 0f;
//                HealthComponent healthComponent = chunkHealths[i];
//                Translation pos = chunkTranslations[i];

//                for (int j = 0; j < transToTestAgainst.Length; j++)
//                {
//                    Translation pos2 = transToTestAgainst[j];
//                    if (CheckCollision(pos.Value, pos2.Value, radius))
//                    {
//                        damage += 1;
//                    }
//                }

//                if (damage > 0)
//                {
//                    healthComponent.health -= damage;
//                    chunkHealths[i] = healthComponent;
//                }
//            }
//        }
//    }
//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        var healthType = GetArchetypeChunkComponentType<HealthComponent>(false);
//        var translationType = GetArchetypeChunkComponentType<Translation>(true);

//        float enemyRadius = 1;
//        float playerRadius = 1;

//        var jobEvB = new CollisionJob()
//        {
//            radius = enemyRadius * enemyRadius,
//            healthType = healthType,
//            translationType = translationType,
//            transToTestAgainst = playerGroup.ToComponentDataArray<Translation>(Allocator.TempJob)
//        };

//        JobHandle jobHandle = jobEvB.Schedule(enemyGroup, inputDeps);

//        var jobPvE = new CollisionJob()
//        {
//            radius = playerRadius * playerRadius,
//            healthType = healthType,
//            translationType = translationType,
//            transToTestAgainst = enemyGroup.ToComponentDataArray<Translation>(Allocator.TempJob)
//        };

//        return jobEvB.Schedule(playerGroup, jobHandle);
//    }

//    static bool CheckCollision(float3 posA, float3 posB, float radiusSqr)
//    {
//        float3 delta = posA - posB;
//        float distanceSquare = delta.x * delta.x + delta.z * delta.z;

//        return distanceSquare <= radiusSqr;
//    }
//}

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

//在移动系统后、时间销毁系统之前去执行碰撞检测
[UpdateAfter(typeof(MoveSystem))]
[UpdateBefore(typeof(TimeDestorySystem))]
public class CollisionSystem : JobComponentSystem
{
    //定义组去承接要检测碰撞的entity
    EntityQuery enemyGroup;
    EntityQuery playerGroup;

    protected override void OnCreate()
    {
        //通过GetEntityQuery寻找确定的entity，并赋值给定义好的组
        playerGroup = GetEntityQuery(typeof(HealthComponent), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<PlayerTagComponent>());
        enemyGroup = GetEntityQuery(typeof(HealthComponent), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<EnemyTagComponent>());
    }

    //定义IJobChunk结构体，碰撞检测的逻辑将在这里面完成
    [BurstCompile]
    struct CollisionJob : IJobChunk
    {
        //检测碰撞范围的半径
        public float radius;

        //定义相关的的ArchetypeChunkComponentType
        public ArchetypeChunkComponentType<HealthComponent> healthType;
        [ReadOnly] public ArchetypeChunkComponentType<Translation> translationType;

        //定义一个translation数组
        [DeallocateOnJobCompletion]
        [ReadOnly] public NativeArray<Translation> transToTestAgainst;


        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            //定义与之前ArchetypeChunkComponentType相关的变量
            var chunkHealths = chunk.GetNativeArray(healthType);
            var chunkTranslations = chunk.GetNativeArray(translationType);

            //遍历每一个chunk，进行碰撞检测
            for (int i = 0; i < chunk.Count; i++)
            {
                float damage = 0f;
                HealthComponent health = chunkHealths[i];
                Translation pos = chunkTranslations[i];

                for (int j = 0; j < transToTestAgainst.Length; j++)
                {
                    Translation pos2 = transToTestAgainst[j];

                    //判断是否碰撞
                    if (CheckCollision(pos.Value, pos2.Value, radius))
                    {
                        damage += 1;
                    }
                }

                if (damage > 0)
                {
                    health.health -= damage;
                    chunkHealths[i] = health;
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var healthType = GetArchetypeChunkComponentType<HealthComponent>(false);
        var translationType = GetArchetypeChunkComponentType<Translation>(true);

        float enemyRadius = 1;
        float playerRadius = 1;

        var jobEvB = new CollisionJob()
        {
            radius = enemyRadius * enemyRadius,
            healthType = healthType,
            translationType = translationType,
            transToTestAgainst = playerGroup.ToComponentDataArray<Translation>(Allocator.TempJob)
        };

        JobHandle jobHandle = jobEvB.Schedule(enemyGroup, inputDependencies);

        var jobPvE = new CollisionJob()
        {
            radius = playerRadius * playerRadius,
            healthType = healthType,
            translationType = translationType,
            transToTestAgainst = enemyGroup.ToComponentDataArray<Translation>(Allocator.TempJob)
        };

        return jobPvE.Schedule(playerGroup, jobHandle);
    }

    static bool CheckCollision(float3 posA, float3 posB, float radiusSqr)
    {
        float3 delta = posA - posB;
        float distanceSquare = delta.x * delta.x + delta.z * delta.z;

        return distanceSquare <= radiusSqr;
    }
}
