using ECS.Components;
using ECS.Flags;
using ECS.MonoBehaviours;
using ECS.Other;
using ECS.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;


namespace ECS.Systems
{
    public class UnitControlSystem : SystemBase
    {
        private const float SelectionAreaMinSize = 1f;
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }
        private OnUpdateNavMeshSystem UpdateNavMesh { get; set; }
        private float3 StartMousePosition { get; set; }

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            UpdateNavMesh = World.GetOrCreateSystem<OnUpdateNavMeshSystem>();
        }

        protected override void OnUpdate()
        {
            if (Input.GetKey(KeyCode.Delete))
                DestroyAllSelected();
            
            var isMouseOnUI = EventSystem.current.IsPointerOverGameObject();
            
            var currentMousePosition = Utilities.GetMouseWorldPosition();
            
            if (Input.GetMouseButtonDown(0) && !isMouseOnUI)
                ProcessLeftButtonDown(currentMousePosition);
            else if (Input.GetMouseButton(0))
                ProcessLeftButtonHeldDown(currentMousePosition);
            else if (Input.GetMouseButtonUp(0))
                ProcessLeftButtonUp(currentMousePosition, isMouseOnUI);
            else if (Input.GetMouseButtonDown(1) && !isMouseOnUI)
                ProcessRightButtonDown(currentMousePosition);
        }

        private void DestroyAllSelected()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            if (TryGetSingletonEntity<SelectedTag>(out var selected) 
                && HasComponent<BuildingTag>(selected))
            {
				//TODO: доделать это 
				//EntityManager.DestroyEntity(GetComponent<HealthBarReferenceComponent>(selected).HealthBarEntity);
                EntityManager.DestroyEntity(GetComponent<SelectedLabelReferenceComponent>(selected).SelectedLabelEntity);

				EntityManager.DestroyEntity(selected);
                UpdateNavMesh.RaiseNavMeshUpdateFlag();
                return;
            }
            Entities
                .WithAll<SelectedTag>()
                .ForEach((Entity entity, int entityInQueryIndex) =>
                {
					parallelWriter.DestroyEntity(entityInQueryIndex, GetComponent<HealthBarReferenceComponent>(entity).HealthBarEntity);
                    parallelWriter.DestroyEntity(entityInQueryIndex, GetComponent<SelectedLabelReferenceComponent>(entity).SelectedLabelEntity);
                    parallelWriter.DestroyEntity(entityInQueryIndex, entity);
                }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
            Ecb.Update();
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

        private void ProcessLeftButtonUp(float3 mousePosition, bool isMouseOnUI)
        {
            CameraHandler.Instance.selectionAreaTransform.gameObject.SetActive(false);
            CameraHandler.Instance.selectionAreaTransform.localScale = Vector3.zero;
            if (isMouseOnUI)
                return;
            var lowerLeftCorner = new float2(
                math.min(StartMousePosition.x, mousePosition.x),
                math.min(StartMousePosition.y, mousePosition.y));
            var upperRightCorner = new float2(
                math.max(StartMousePosition.x, mousePosition.x),
                math.max(StartMousePosition.y, mousePosition.y));
            if(!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) 
               || TryGetSingletonEntity<SelectedTag>(out var selected) && HasComponent<BuildingTag>(selected))
            {
                ResetSelection();
            }
            if (math.distance(lowerLeftCorner, upperRightCorner) < SelectionAreaMinSize)
            {
                var entity = GetClosestEntity(mousePosition);
                if (entity != Entity.Null && GetComponent<OwnerComponent>(entity).PlayerNumber != 2)
                {
                    EntityManager.AddComponent(entity, ComponentType.ReadWrite<SelectedTag>());
                }
            }
            else
            {
                SelectAllEntitiesInArea(lowerLeftCorner, upperRightCorner);
            }
        }

        public void ResetSelection()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithAll<SelectedTag>()
                .ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
                {
                    parallelWriter.RemoveComponent(entityInQueryIndex, entity,
                        ComponentType.ReadWrite<SelectedTag>());
                }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
            Ecb.Update();
        }
        
        private Entity GetClosestEntity(float3 mousePosition)
        {
            var e = Entity.Null;
            var maxScale = float.MinValue;
            Entities
                .ForEach((Entity entity, in Translation translation, in EntityStatsComponent stats) =>
                {
                    var curScale = stats.BaseRadius;
                    if (math.distance(translation.Value, mousePosition) < curScale && curScale > maxScale)
                    {
                        maxScale = curScale;
                        e = entity;
                    }
                }).Run();
            return e;
        }

        private void SelectAllEntitiesInArea(float2 lowerLeftCorner, float2 upperRightCorner)
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithAll<UnitTag>()
                .ForEach((Entity entity, int entityInQueryIndex, in Translation translation, in OwnerComponent owner) =>
                {
                    if (Utilities.IsInRectangle(translation.Value.xy, lowerLeftCorner, upperRightCorner) 
                        && owner.PlayerNumber != 2)
                    {
                        parallelWriter.AddComponent(entityInQueryIndex, entity,
                            ComponentType.ReadWrite<SelectedTag>());
                        
                    }
                }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
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
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithAll<UnitTag, SelectedTag>()
                .ForEach((Entity entity, int entityInQueryIndex,
                    DynamicBuffer<OrderQueueElementComponent> orderQueue, ref OrderQueueInfoComponent orderInfo) =>
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
                        parallelWriter.RemoveComponent<AttackTargetComponent>(entityInQueryIndex, entity);
                    }
                }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
        }

        private void SetOrders(OrderType type, float2 movePosition, Entity target)
        {
            var position = Utilities.GetRoundedPoint(movePosition);
            Entities
                .WithAll<UnitTag, SelectedTag>()
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
                    orderInfo.Count++;
                    if (orderInfo.R == (orderInfo.L + 1) % orderQueue.Capacity 
                        && orderInfo.Count == orderQueue.Capacity + 1) 
                    {
                        orderInfo.R = (orderInfo.R + 1) % orderQueue.Capacity;
                        orderInfo.Count--;
                    }
                }).Schedule();
        }
    }
}