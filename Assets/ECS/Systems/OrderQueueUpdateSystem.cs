using System;
using System.Collections.Generic;
using ECS.Components;
using ECS.Other;
using ECS.Tags;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace ECS.Systems
{
    [UpdateAfter(typeof(UnitControlSystem))]
    public class OrderQueueUpdateSystem : SystemBase
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
                .ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<OrderQueueElementComponent> orderQueue,
                    ref OrderQueueInfoComponent orderInfo) =>
                {
                    if (orderInfo.Count != 0)
                    {
                        if (orderQueue[orderInfo.L].State == OrderState.Complete)
                        {
                            parallelWriter.RemoveComponent(entityInQueryIndex, entity, 
                                GetOrderComponentType(orderQueue[orderInfo.L].Type));
                            orderInfo.Count--;
                            orderInfo.L = (orderInfo.L + 1) % orderQueue.Capacity;
                        }
                    }
                    if (orderInfo.Count != 0)
                    {
                        if (orderQueue[orderInfo.L].State == OrderState.New)
                        {
                            parallelWriter.AddComponent(entityInQueryIndex, entity, 
                                GetOrderComponentType(orderQueue[orderInfo.L].Type));
                        }
                    }
                }).Schedule();
            
            Ecb.AddJobHandleForProducer(Dependency);
            Ecb.Update();
        }
        
        private static ComponentType GetOrderComponentType(OrderType type)
        {
            return type switch
            {
                OrderType.Move => ComponentType.ReadWrite<MoveOrderTag>(),
                OrderType.Attack => ComponentType.ReadWrite<AttackOrderTag>(),
                OrderType.AttackMove => ComponentType.ReadWrite<AttackMoveOrderTag>(),
                OrderType.HoldPosition => ComponentType.ReadWrite<HoldPositionOrderTag>(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}