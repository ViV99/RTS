using ECS.Components;
using ECS.Other;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using ECS.Tags;

namespace ECS.Systems
{
    public class SelectedLabelSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            Entities
                .ForEach((
                Entity entity,
                int entityInQueryIndex,
                ref HealthBarReferenceComponent healthBar,
                ref Translation translation,
                ref CompositeScale compositeScale,
                ref EntityStatsComponent stats, 
                ref SelectedLabelReferenceComponent selectedLabel) =>
            {
                if (selectedLabel.SelectedLabelEntity != Entity.Null)
                {
                    if (HasComponent<SelectedTag>(entity))
                    {
                         var selectedLabelTranslation = translation;
                         parallelWriter.SetComponent(entityInQueryIndex, selectedLabel.SelectedLabelEntity, selectedLabelTranslation);
                         var selectedLabelCompositeScale = compositeScale;
                         parallelWriter.SetComponent(entityInQueryIndex, selectedLabel.SelectedLabelEntity, selectedLabelCompositeScale);
                    }
                    else
                    {
                        var selectedLabelCompositeScale = compositeScale;
                        selectedLabelCompositeScale.Value.c0.x = 0;
                        parallelWriter.SetComponent(entityInQueryIndex, selectedLabel.SelectedLabelEntity, selectedLabelCompositeScale);
                    }   
                }

            }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
        }
    }
}