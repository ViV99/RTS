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
                ref EntityStatsComponent stats) =>
            {
                if (healthBar.HealthBarEntity != Entity.Null)
                {
                    if (stats.MaxHealth != stats.CurrentHealth)
                    {
						var healthBarTranslation = translation;
                        healthBarTranslation.Value +=
                            new float3(-compositeScale.Value.c0.x * (stats.MaxHealth - stats.CurrentHealth) / stats.MaxHealth / 2,
                                compositeScale.Value.c0.x / 8 * 5, 0);
                        parallelWriter.SetComponent(entityInQueryIndex, healthBar.HealthBarEntity, healthBarTranslation);
						var healthBarCompositeScale = compositeScale;
						healthBarCompositeScale.Value.c0.x = compositeScale.Value.c0.x * stats.CurrentHealth / stats.MaxHealth;
						healthBarCompositeScale.Value.c1.y = compositeScale.Value.c0.x / 7;
						healthBarCompositeScale.Value.c2.z = 1024;
                        parallelWriter.SetComponent(entityInQueryIndex, healthBar.HealthBarEntity, healthBarCompositeScale);
                    }
                    else
                    {
                        var healthBarCompositeScale = compositeScale;
						healthBarCompositeScale.Value.c0.x = 0;
                        parallelWriter.SetComponent(entityInQueryIndex, healthBar.HealthBarEntity, healthBarCompositeScale);
                    }    
                }
            }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
            Ecb.Update();
        }
    }
}