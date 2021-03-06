//#define LIGHT_DEBUG
 
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;

#if HDRP_6_EXISTS
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
#elif HDRP_7_EXISTS
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Unity.Rendering
{
    struct LightSystemData : ISystemStateComponentData
    {
        public int LightPoolIndex;
    }

    class PooledUnityLight
    {
        public GameObject m_Object;
        public Light m_Light;
#if HDRP_6_EXISTS
        public HDAdditionalLightData m_HdData;
        public AdditionalShadowData m_HdShadow;        
#elif HDRP_7_EXISTS
        public HDAdditionalLightData m_HdData;
#endif
        public Entity m_Entity;
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(RenderMeshSystemV2))]
    public class LightSystem : ComponentSystem
    {
        static ProfilerMarker ProfileNewLights = new ProfilerMarker("LightSystem.NewLights");
        static ProfilerMarker ProfileDeletedLights = new ProfilerMarker("LightSystem.DeletedLights");
        static ProfilerMarker ProfileUpdateLights = new ProfilerMarker("LightSystem.UpdateLights");

        EntityQuery m_DeletedLights;
        EntityQuery m_NewLights;
        EntityQuery m_ActiveLights;

        List<PooledUnityLight> m_UnityLights = new List<PooledUnityLight>();
        int m_ActiveLightCounter = 0;

        protected override void OnCreate()
        {
            m_DeletedLights = GetEntityQuery(
            new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<LightSystemData>()},
                None = new[] {ComponentType.ReadOnly<LightComponent>()},
            },
            new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<LightSystemData>(), ComponentType.ReadOnly<Disabled>()},
            });
            
            m_NewLights = GetEntityQuery(
                ComponentType.ReadOnly<LightComponent>(),
#if HDRP_6_EXISTS || HDRP_7_EXISTS
                ComponentType.ReadOnly<HDLightData>(),
#endif
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.Exclude<LightSystemData>());

            m_ActiveLights = GetEntityQuery(
                ComponentType.ReadOnly<LightComponent>(),
#if HDRP_6_EXISTS || HDRP_7_EXISTS
                ComponentType.ReadOnly<HDLightData>(),
#endif
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<LightSystemData>());
        }

        protected override void OnDestroy()
        {
            foreach (var light in m_UnityLights)
            {
                Object.DestroyImmediate(light.m_Object);
            }
        }

        protected override void OnUpdate()
        {
            UpdateDeletedLights();
            UpdateNewLights();
            UpdateModifiedLights();
        }

        private void UpdateDeletedLights()
        {
            ProfileDeletedLights.Begin();
            Entities.With(m_DeletedLights).ForEach((Entity entity, ref LightSystemData lightData) =>
            {
                var unityLight = m_UnityLights[lightData.LightPoolIndex];
                unityLight.m_Light.enabled = false;

                // Reuse: Swap disabled light with last
                m_ActiveLightCounter--;
                EntityManager.SetComponentData<LightSystemData>(m_UnityLights[m_ActiveLightCounter].m_Entity, new LightSystemData { LightPoolIndex = lightData.LightPoolIndex });
                m_UnityLights[lightData.LightPoolIndex] = m_UnityLights[m_ActiveLightCounter];
                m_UnityLights[m_ActiveLightCounter] = unityLight;
            });
            PostUpdateCommands.RemoveComponent(m_DeletedLights, ComponentType.ReadWrite<LightSystemData>());
            ProfileDeletedLights.End();
        }
        
        int GetLightFromPool()
        {
            // No lights left in pool: Create a new light game object
            if (m_UnityLights.Count == m_ActiveLightCounter)
            {
                var GO = new GameObject("HybridPooledLight");

#if LIGHT_DEBUG
                GO.hideFlags |= HideFlags.DontSave;
#else
                GO.hideFlags |= HideFlags.HideAndDontSave;
#endif

                var pooledLight = new PooledUnityLight
                {
                    m_Object = GO,
                    m_Light = GO.AddComponent<UnityEngine.Light>(),
#if HDRP_6_EXISTS
                    m_HdData = GO.AddComponent<HDAdditionalLightData>(),
                    m_HdShadow = GO.AddComponent<AdditionalShadowData>(),
#elif HDRP_7_EXISTS
                    m_HdData = GO.AddComponent<HDAdditionalLightData>(),
#endif
                };

                m_UnityLights.Add(pooledLight);
            }
          
            return m_ActiveLightCounter++;
        }

        private void UpdateNewLights()
        {
            ProfileNewLights.Begin();
            var chunks = m_NewLights.CreateArchetypeChunkArray(Allocator.TempJob);

            var chunkEntityType = GetArchetypeChunkEntityType();
            var chunkLocalToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(true);
            var chunkLightComponentType = GetArchetypeChunkComponentType<LightComponent>(true);
            var chunkLightCookieType = GetArchetypeChunkSharedComponentType<LightCookie>();
#if HDRP_6_EXISTS || HDRP_7_EXISTS
            var chunkHdDataType = GetArchetypeChunkComponentType<HDLightData>(true);
#endif

            for (int i = 0; i < chunks.Length; ++i)
            {
                var chunk = chunks[i];

                var chunkEntities = chunk.GetNativeArray(chunkEntityType);
                var chunkLocalToWorlds = chunk.GetNativeArray(chunkLocalToWorldType);
                var chunkLightComponents = chunk.GetNativeArray(chunkLightComponentType);
#if HDRP_6_EXISTS || HDRP_7_EXISTS
                var chunkHdDatas = chunk.GetNativeArray(chunkHdDataType);
#endif

                bool hasCookie = chunk.Has(chunkLightCookieType);
                var cookie = hasCookie ? chunk.GetSharedComponentData(chunkLightCookieType, EntityManager) : new LightCookie();

                for (int j = 0; j < chunk.Count; ++j)
                {
                    var poolIndex = GetLightFromPool();

                    m_UnityLights[poolIndex].m_Entity = chunkEntities[j];

                    // Transform
                    var GO = m_UnityLights[poolIndex].m_Object;
                    GO.transform.position = chunkLocalToWorlds[j].Position;
                    GO.transform.forward = chunkLocalToWorlds[j].Forward;

                    // Light properties
                    var unityLight = m_UnityLights[poolIndex].m_Light;
                    UpdateUnityLight(unityLight, chunkLightComponents[j]);

                    // Optional: Light cookie
                    if (hasCookie)
                    {
                        UpdateUnityLightCookie(unityLight, cookie);
                    }

#if HDRP_6_EXISTS
                    // HD light data
                    var unityHdData = m_UnityLights[poolIndex].m_HdData;
                    var unityHdShadow = m_UnityLights[poolIndex].m_HdShadow;
                    HDAdditionalLightData.InitDefaultHDAdditionalLightData(unityHdData);
                    UpdateUnityHdData(unityHdData, unityHdShadow, chunkHdDatas[j]);
#elif HDRP_7_EXISTS
                    // HD light data
                    var unityHdData = m_UnityLights[poolIndex].m_HdData;
                    HDAdditionalLightData.InitDefaultHDAdditionalLightData(unityHdData);
                    UpdateUnityHdData(unityHdData, chunkHdDatas[j]);
#endif

                    var lightSystemData = new LightSystemData { LightPoolIndex = poolIndex };
                    PostUpdateCommands.AddComponent(chunkEntities[j], lightSystemData);
                }
            }

            chunks.Dispose();
            ProfileNewLights.End();
        }

        private void UpdateModifiedLights()
        {
            ProfileUpdateLights.Begin();
            var chunks = m_ActiveLights.CreateArchetypeChunkArray(Allocator.TempJob);

            var chunkLightSystemDataType = GetArchetypeChunkComponentType<LightSystemData>(true);
            var chunkLocalToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(true);
            var chunkLightComponentType = GetArchetypeChunkComponentType<LightComponent>(true);
            var chunkLightCookieType = GetArchetypeChunkSharedComponentType<LightCookie>();
#if HDRP_6_EXISTS || HDRP_7_EXISTS
            var chunkHdDataType = GetArchetypeChunkComponentType<HDLightData>(true);
#endif

            for (int i = 0; i < chunks.Length; ++i)
            {
                var chunk = chunks[i];
                var chunkLightSystemDatas = chunk.GetNativeArray(chunkLightSystemDataType);

                // Transform
                if (chunk.DidChange(chunkLocalToWorldType, LastSystemVersion))
                {
                    var chunkLocalToWorlds = chunk.GetNativeArray(chunkLocalToWorldType);
                    for (int j = 0; j < chunk.Count; ++j)
                    {
                        int poolIndex = chunkLightSystemDatas[j].LightPoolIndex;
                        var GO = m_UnityLights[poolIndex].m_Object;
                        var localToWorld = chunkLocalToWorlds[j];

                        GO.transform.position = localToWorld.Position;
                        GO.transform.forward = localToWorld.Forward;
                    }
                }

                // Light properties
                if (chunk.DidChange(chunkLightComponentType, LastSystemVersion))
                {
                    var chunkLightComponents = chunk.GetNativeArray(chunkLightComponentType);
                    for (int j = 0; j < chunk.Count; ++j)
                    {
                        int poolIndex = chunkLightSystemDatas[j].LightPoolIndex;
                        UpdateUnityLight(m_UnityLights[poolIndex].m_Light, chunkLightComponents[j]);
                    }
                }

                // Optional: Light cookie
                if (chunk.Has(chunkLightCookieType) && chunk.DidChange(chunkLightCookieType, LastSystemVersion))
                {
                    var cookie = chunk.GetSharedComponentData(chunkLightCookieType, EntityManager);
                    for (int j = 0; j < chunk.Count; ++j)
                    {
                        int poolIndex = chunkLightSystemDatas[j].LightPoolIndex;
                        UpdateUnityLightCookie(m_UnityLights[poolIndex].m_Light, cookie);
                    }
                }

#if HDRP_6_EXISTS
                // HD light data
                if (chunk.DidChange(chunkHdDataType, LastSystemVersion))
                {
                    var chunkHdDatas = chunk.GetNativeArray(chunkHdDataType);
                    for (int j = 0; j < chunk.Count; ++j)
                    {
                        int poolIndex = chunkLightSystemDatas[j].LightPoolIndex;
                        UpdateUnityHdData(m_UnityLights[poolIndex].m_HdData, m_UnityLights[poolIndex].m_HdShadow, chunkHdDatas[j]);
                    }
                }
#elif HDRP_7_EXISTS
                // HD light data
                if (chunk.DidChange(chunkHdDataType, LastSystemVersion))
                {
                    var chunkHdDatas = chunk.GetNativeArray(chunkHdDataType);
                    for (int j = 0; j < chunk.Count; ++j)
                    {
                        int poolIndex = chunkLightSystemDatas[j].LightPoolIndex;
                        UpdateUnityHdData(m_UnityLights[poolIndex].m_HdData, chunkHdDatas[j]);
                    }
                }
#endif
            }

            chunks.Dispose();
            ProfileUpdateLights.End();
        }

        private static void UpdateUnityLight(UnityEngine.Light unityLight, LightComponent light)
        {
            unityLight.enabled = true;
            unityLight.type                     = light.type;
            unityLight.color                    = light.color;
            unityLight.colorTemperature         = light.colorTemperature;
            unityLight.range                    = light.range;
            unityLight.intensity                = light.intensity;
            unityLight.cullingMask              = light.cullingMask;
            unityLight.renderingLayerMask       = light.renderingLayerMask;
            unityLight.shadows                  = light.shadows;
            unityLight.shadowCustomResolution   = light.shadowCustomResolution;
            unityLight.shadowNearPlane          = light.shadowNearPlane;
            unityLight.shadowBias               = light.shadowBias;
            unityLight.shadowNormalBias         = light.shadowNormalBias;
            unityLight.shadowStrength           = light.shadowStrength;

            if (light.type == LightType.Spot)
            {
                unityLight.spotAngle = light.spotAngle;
                unityLight.innerSpotAngle = light.innerSpotAngle;
            }
        }

        private static void UpdateUnityLightCookie(UnityEngine.Light unityLight, LightCookie cookie)
        {
            unityLight.cookie = cookie.texture;
        }

#if HDRP_6_EXISTS
        private static void UpdateUnityHdData(HDAdditionalLightData unityHdData, AdditionalShadowData unityShadowData, HDLightData hdData)
        {
            unityHdData.lightTypeExtent = hdData.lightTypeExtent;
            unityHdData.lightDimmer = hdData.lightDimmer;
            unityHdData.fadeDistance = hdData.fadeDistance;
            unityHdData.affectDiffuse = hdData.affectDiffuse;
            unityHdData.affectSpecular = hdData.affectSpecular;
            unityHdData.shapeWidth = hdData.shapeWidth;
            unityHdData.shapeHeight = hdData.shapeHeight;
            unityHdData.aspectRatio = hdData.aspectRatio;
            unityHdData.shapeRadius = hdData.shapeRadius;
            unityHdData.maxSmoothness = hdData.maxSmoothness;
            unityHdData.applyRangeAttenuation = hdData.applyRangeAttenuation;

            // Spot light specific
            // NOTE: Can't add branch here, because HD light doesn't itself know whether it's a spot. That's stored in Unity.Light.
            unityHdData.enableSpotReflector = hdData.enableSpotReflector;
            unityHdData.m_InnerSpotPercent = hdData.innerSpotPercent;
            unityHdData.spotLightShape = hdData.spotLightShape;

            // Intensity is a property. It setups lots of things and assumes data is already set. Must be called last!
            unityHdData.intensity = hdData.intensity;

            // HDShadowData
            unityShadowData.shadowResolution = hdData.shadowResolution;
            unityShadowData.shadowDimmer = hdData.shadowDimmer;
            unityShadowData.volumetricShadowDimmer = hdData.volumetricShadowDimmer;
            unityShadowData.shadowFadeDistance = hdData.shadowFadeDistance;
            unityShadowData.contactShadows = hdData.contactShadows;
            unityShadowData.viewBiasMin = hdData.viewBiasMin;
            unityShadowData.viewBiasMax = hdData.viewBiasMax;
            unityShadowData.viewBiasScale = hdData.viewBiasScale;
            unityShadowData.normalBiasMin = hdData.normalBiasMin;
            unityShadowData.normalBiasMax = hdData.normalBiasMax;
            unityShadowData.normalBiasScale = hdData.normalBiasScale;
            unityShadowData.sampleBiasScale = hdData.sampleBiasScale;
            unityShadowData.edgeLeakFixup = hdData.edgeLeakFixup;
            unityShadowData.edgeToleranceNormal = hdData.edgeToleranceNormal;
            unityShadowData.edgeTolerance = hdData.edgeTolerance;
        }
#elif HDRP_7_EXISTS
        private static void UpdateUnityHdData(HDAdditionalLightData unityHdData, HDLightData hdData)
        {
            unityHdData.lightTypeExtent         = hdData.lightTypeExtent;
            unityHdData.lightDimmer             = hdData.lightDimmer;
            unityHdData.fadeDistance            = hdData.fadeDistance;
            unityHdData.affectDiffuse           = hdData.affectDiffuse;
            unityHdData.affectSpecular          = hdData.affectSpecular;
            unityHdData.shapeWidth              = hdData.shapeWidth;
            unityHdData.shapeHeight             = hdData.shapeHeight;
            unityHdData.aspectRatio             = hdData.aspectRatio;
            unityHdData.shapeRadius             = hdData.shapeRadius;
            unityHdData.maxSmoothness           = hdData.maxSmoothness;
            unityHdData.applyRangeAttenuation   = hdData.applyRangeAttenuation;

            // Spot light specific
            // NOTE: Can't add branch here, because HD light doesn't itself know whether it's a spot. That's stored in Unity.Light.
            unityHdData.enableSpotReflector     = hdData.enableSpotReflector;
            unityHdData.innerSpotPercent        = hdData.innerSpotPercent;
            unityHdData.spotLightShape          = hdData.spotLightShape;

            // Shadow data   
            unityHdData.customResolution = hdData.customResolution;
            unityHdData.shadowDimmer = hdData.shadowDimmer;
            unityHdData.volumetricShadowDimmer = hdData.volumetricShadowDimmer;
            unityHdData.shadowFadeDistance = hdData.shadowFadeDistance;
            unityHdData.contactShadows = hdData.contactShadows;
            unityHdData.shadowTint = hdData.shadowTint;
            unityHdData.normalBias = hdData.normalBias;
            unityHdData.constantBias = hdData.constantBias;
            unityHdData.shadowUpdateMode = hdData.shadowUpdateMode;

            // Intensity is a property. It setups lots of things and assumes data is already set. Must be called last!
            unityHdData.intensity               = hdData.intensity;
        }
#endif
    }
}
