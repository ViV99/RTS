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
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            var currentMousePosition = Utilities.GetMouseWorldPosition();

            ProcessLeftButtonDown(currentMousePosition);
            ProcessLeftButtonHeldDown(currentMousePosition);
            ProcessLeftButtonUp(parallelWriter, currentMousePosition);

            ProcessRightButtonDown(currentMousePosition);
        }

        private void ProcessLeftButtonDown(float3 mousePosition)
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            StartMousePosition = mousePosition;
            GameHandler.Instance.selectionAreaTransform.position = StartMousePosition;
            GameHandler.Instance.selectionAreaTransform.gameObject.SetActive(true);
        }

        private void ProcessLeftButtonHeldDown(float3 mousePosition)
        {
            if (!Input.GetMouseButton(0))
                return;

            var selectionAreaSize = mousePosition - StartMousePosition;
            GameHandler.Instance.selectionAreaTransform.localScale = selectionAreaSize;
        }

        private void ProcessLeftButtonUp(EntityCommandBuffer.ParallelWriter parallelWriter, float3 mousePosition)
        {
            if (!Input.GetMouseButtonUp(0))
                return;

            GameHandler.Instance.selectionAreaTransform.gameObject.SetActive(false);
            var lowerLeftCorner = new float2(
                math.min(StartMousePosition.x, mousePosition.x),
                math.min(StartMousePosition.y, mousePosition.y));
            var upperRightCorner = new float2(
                math.max(StartMousePosition.x, mousePosition.x),
                math.max(StartMousePosition.y, mousePosition.y));

            var selectionAreaSize = math.distance(lowerLeftCorner, upperRightCorner);
            if (selectionAreaSize < SelectionAreaMinSize)
            {
                lowerLeftCorner += new float2(-0.5f, -0.5f) * (SelectionAreaMinSize - selectionAreaSize);
                upperRightCorner += new float2(0.5f, 0.5f) * (SelectionAreaMinSize - selectionAreaSize);
            }

            Entities
                .WithAll<SelectedTag>()
                .ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
                {
                    parallelWriter.RemoveComponent(entityInQueryIndex, entity, ComponentType.ReadWrite<SelectedTag>());
                }).Schedule();

            Entities
                .ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
                {
                    var entityPos = translation.Value.xy;

                    if (Utilities.IsInRectangle(entityPos, lowerLeftCorner, upperRightCorner))
                    {
                        parallelWriter.AddComponent(entityInQueryIndex, entity, ComponentType.ReadWrite<SelectedTag>());
                    }
                }).Schedule();

            Ecb.AddJobHandleForProducer(Dependency);
        }

        private void ProcessRightButtonDown(float3 mousePosition)
        {
            if (!Input.GetMouseButtonDown(1))
                return;
            
            // var e = GetSingleton<PrefabsComponent>().SimpleSolidPrefab;
            // var eCopy = EntityManager.Instantiate(e);
            // EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<SolidTag>());
            // EntityManager.SetComponentData(eCopy, new Translation {Value = mousePosition});
            // EntityManager.CreateEntity(ComponentType.ReadWrite<NavMeshUpdateFlag>());
                Entities
                .WithAll<SelectedTag>()
                .ForEach((Entity entity, int entityInQueryIndex, 
                    ref DynamicBuffer<OrderQueueElementComponent> orderQueue, 
                    ref OrderQueueInfoComponent orderInfo) =>
                {
                    orderQueue.Add(new OrderQueueElementComponent(
                        OrderType.Move, 
                        OrderState.New,
                        Utilities.GetRoundedPoint(mousePosition.xy)));
                    orderInfo.Count++;
                    orderInfo.R = (orderInfo.R + 1) % orderQueue.Capacity;
                }).Schedule();
        }
    }
}