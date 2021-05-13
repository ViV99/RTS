using ECS.Components;
using ECS.Other;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems
{
    public class HealthBarSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            Entities.ForEach((
                Entity entity,
                int entityInQueryIndex,
                ref HealthBarReferenceComponent healthBar,
                ref Translation translation,
                ref CompositeScale compositeScale,
                ref UnitStatsComponent stats) =>
            {
                if (stats.MaxHealth != stats.Health)
                {
                    var healthBarTranslation = translation;
                    healthBarTranslation.Value +=
                        new float3(-compositeScale.Value.c0.x * (stats.MaxHealth - stats.Health) / stats.MaxHealth / 2,
                            compositeScale.Value.c0.x / 8 * 5, 0);
                    parallelWriter.SetComponent(entityInQueryIndex, healthBar.HealthBarEntity, healthBarTranslation);
                    parallelWriter.SetComponent(entityInQueryIndex, healthBar.HealthBarEntity, 
                        new NonUniformScale
                        {
                            Value = new float3(compositeScale.Value.c0.x * stats.Health / stats.MaxHealth,
                                compositeScale.Value.c0.x / 7, 0)
                        });
                }
                else
                {
                    parallelWriter.SetComponent(entityInQueryIndex, healthBar.HealthBarEntity, 
                        new NonUniformScale{Value = new float3(0, 0, 0)});

                }
                
            }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
        }
    }
}
