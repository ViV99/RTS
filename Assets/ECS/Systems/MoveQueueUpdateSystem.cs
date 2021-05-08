using System;
using ECS.Components;
using ECS.Other;
using ECS.Tags;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Systems
{
    public class MoveQueueUpdateSystem : SystemBase
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
                .ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<MoveQueueElementComponent> moveQueue,
                    ref MoveQueueInfoComponent info, ref MoveToComponent moveTo) =>
                {
                    if (moveTo.IsMoving)
                        return;
                    
                    if (info.Index != info.Count)
                    {
                        //info.L = (info.L + 1) % moveQueue.Capacity;
                        info.Index++;
                    }

                    if (info.Index != info.Count)
                    {
                        moveTo.Position = moveQueue[info.Index].MovePosition;
                        moveTo.IsMoving = true;
                    }
                }).Schedule();
            
            Ecb.AddJobHandleForProducer(Dependency);
        }
    }
}
