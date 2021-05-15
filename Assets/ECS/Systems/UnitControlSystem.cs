using ECS.Components;
using ECS.Flags;
using ECS.MonoBehaviours;
using ECS.Other;
using ECS.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


namespace ECS.Systems
{
    public class UnitControlSystem : SystemBase
    {
        private const float SelectionAreaMinSize = 1f;
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }
        private float3 StartMousePosition { get; set; }

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var currentMousePosition = Utilities.GetMouseWorldPosition();
            
            if (Input.GetMouseButtonDown(0))
                ProcessLeftButtonDown(currentMousePosition);
            else if (Input.GetMouseButton(0))
                ProcessLeftButtonHeldDown(currentMousePosition);
            else if (Input.GetMouseButtonUp(0))
                ProcessLeftButtonUp(currentMousePosition);
            else if (Input.GetMouseButtonDown(1))
                ProcessRightButtonDown(currentMousePosition);
        }

        private void ProcessLeftButtonDown(float3 mousePosition)
        {
            StartMousePosition = mousePosition;
            CameraHandler.Instance.selectionAreaTransform.position = StartMousePosition;
            CameraHandler.Instance.selectionAreaTransform.gameObject.SetActive(true);
        }

        private void ProcessLeftButtonHeldDown(float3 mousePosition)
        {
            var selectionAreaSize = mousePosition - StartMousePosition;
            CameraHandler.Instance.selectionAreaTransform.localScale = selectionAreaSize;
        }

        private void ProcessLeftButtonUp(float3 mousePosition)
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            CameraHandler.Instance.selectionAreaTransform.gameObject.SetActive(false);
            var lowerLeftCorner = new float2(
                math.min(StartMousePosition.x, mousePosition.x),
                math.min(StartMousePosition.y, mousePosition.y));
            var upperRightCorner = new float2(
                math.max(StartMousePosition.x, mousePosition.x),
                math.max(StartMousePosition.y, mousePosition.y));
            if(!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                ResetSelection(parallelWriter);
                Ecb.AddJobHandleForProducer(Dependency);
                Ecb.Update();
                parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            }
            if (math.distance(lowerLeftCorner, upperRightCorner) < SelectionAreaMinSize)
            {
                var entity = GetClosestEntity(mousePosition);
                if (entity != Entity.Null)
                    EntityManager.AddComponent(entity, ComponentType.ReadWrite<SelectedTag>());
            }
            else
            {
                SelectAllEntitiesInArea(parallelWriter, lowerLeftCorner, upperRightCorner);
            }
            Ecb.AddJobHandleForProducer(Dependency);
        }

        private void ResetSelection(EntityCommandBuffer.ParallelWriter parallelWriter)
        {
            Entities
                .WithAll<SelectedTag>()
                .ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
                {
                    parallelWriter.RemoveComponent(entityInQueryIndex, entity,
                        ComponentType.ReadWrite<SelectedTag>());
                }).Schedule();
        }
        
        private Entity GetClosestEntity(float3 mousePosition)
        {
            var e = Entity.Null;
            var maxScale = float.MinValue;
            Entities
                .ForEach((Entity entity, in Translation translation, in CompositeScale scale) =>
                {
                    var curScale = scale.Value.c0.x / 2;
                    if (math.distance(translation.Value, mousePosition) < curScale && curScale > maxScale)
                    {
                        maxScale = curScale;
                        e = entity;
                    }
                }).Run();
            return e;
        }

        private void SelectAllEntitiesInArea(EntityCommandBuffer.ParallelWriter parallelWriter, 
            float2 lowerLeftCorner, float2 upperRightCorner)
        {
            Entities
                .ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
                {
                    if (Utilities.IsInRectangle(translation.Value.xy, lowerLeftCorner, upperRightCorner))
                    {
                        parallelWriter.AddComponent(entityInQueryIndex, entity,
                            ComponentType.ReadWrite<SelectedTag>());
                    }
                }).Schedule();
        }

        private void ProcessRightButtonDown(float3 mousePosition)
        {
            if(!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                ResetOrderQueue();
            }
            var entity = GetClosestEntity(mousePosition);
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                SetOrders(OrderType.AttackMove, mousePosition.xy, Entity.Null);
            }
            else
            {
                if (entity != Entity.Null
                    && HasComponent<OwnerComponent>(entity) 
                    && GetComponent<OwnerComponent>(entity).PlayerNumber == 2)
                {
                    SetOrders(OrderType.Attack, mousePosition.xy, entity);
                }
                else
                {
                    SetOrders(OrderType.Move, mousePosition.xy, Entity.Null);
                }
            }
        }

        private void ResetOrderQueue()
        {
            Entities
                .WithAll<SelectedTag>()
                .ForEach((DynamicBuffer<OrderQueueElementComponent> orderQueue, ref OrderQueueInfoComponent orderInfo) =>
                {
                    if (orderInfo.Count == 0)
                    {
                        orderInfo.Count = 0;
                        orderInfo.L = 0;
                        orderInfo.R = 0;
                    }
                    else
                    {
                        orderInfo.Count = 1;
                        orderInfo.R = orderInfo.L + 1;
                        orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.Complete);
                    }
                }).Schedule();
        }

        private void SetOrders(OrderType type, float2 movePosition, Entity target)
        {
            var position = Utilities.GetRoundedPoint(movePosition);
            Entities
                .WithAll<SelectedTag>()
                .ForEach((DynamicBuffer<OrderQueueElementComponent> orderQueue, ref OrderQueueInfoComponent orderInfo) =>
                {
                    var order = new OrderQueueElementComponent(type, OrderState.New, position, target);
                    if (orderInfo.R < orderQueue.Length)
                    {
                        orderQueue[orderInfo.R] = order;
                    }
                    else
                    {
                        orderQueue.Add(order);
                    }
                    orderInfo.R = (orderInfo.R + 1) % orderQueue.Capacity;
                    orderInfo.Count = (orderInfo.Count + 1) % orderQueue.Capacity;
                    if (orderInfo.R == (orderInfo.L + 1) % orderQueue.Capacity 
                        && orderInfo.Count == orderQueue.Capacity + 1) 
                    {
                        orderInfo.R = (orderInfo.R + 1) % orderQueue.Capacity;
                        orderInfo.Count = (orderInfo.Count - 1) % orderQueue.Capacity;
                    }
                }).Schedule();
        }
    }
}