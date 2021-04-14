using System;
using ECS.Components;
using ECS.MonoBehaviours;
using ECS.Other;
using ECS.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

namespace ECS.Systems
{
    public class UnitControlSystem : SystemBase
    {
        private static float SelectionAreaMinSize { get; } = 1f;
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
            var lowerLeftCorner = new float3(
                math.min(StartMousePosition.x, mousePosition.x),
                math.min(StartMousePosition.y, mousePosition.y),
                0);
            var upperRightCorner = new float3(
                math.max(StartMousePosition.x, mousePosition.x),
                math.max(StartMousePosition.y, mousePosition.y),
                0);

            var selectionAreaSize = math.distance(lowerLeftCorner, upperRightCorner);
            if (selectionAreaSize < SelectionAreaMinSize)
            {
                lowerLeftCorner += new float3(-0.5f, -0.5f, 0) * (SelectionAreaMinSize - selectionAreaSize);
                upperRightCorner += new float3(0.5f, 0.5f, 0) * (SelectionAreaMinSize - selectionAreaSize);
            }

            Entities
                .WithAll<SelectedTag>()
                .ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
                {
                    parallelWriter.RemoveComponent(entityInQueryIndex, entity, ComponentType.ReadWrite<SelectedTag>());
                }).Schedule();

            Entities.ForEach((Entity entity, int entityInQueryIndex, in Translation translation) =>
                {
                    var entityPos = translation.Value;
                    
                    if (entityPos.x >= lowerLeftCorner.x
                        && entityPos.y >= lowerLeftCorner.y
                        && entityPos.x <= upperRightCorner.x
                        && entityPos.y <= upperRightCorner.y)
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
            
            Entities
                .WithAll<SelectedTag>()
                .ForEach((ref MoveToComponent moveComponent) =>
                {
                    moveComponent.Position = mousePosition;
                    moveComponent.IsMoving = true;
                }).Schedule();
        }
    }
}