using System;
using System.Collections.Generic;
using ECS.Components;
using ECS.Flags;
using ECS.Other;
using ECS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ECS.MonoBehaviours
{
    public class BuildingManager : MonoBehaviour
    {
        private EntityManager EntityManager { get; set; }
        
        private OnUpdateNavMeshSystem UpdateNavMesh { get; set; }
        
        private Entity navMeshHandler;
        private Entity gameStateHandler;

        public Entity ActiveBuildingType { get; set; }
        public GameObject ActiveBuildingSprite { get; set; }
        

        private void Awake()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            UpdateNavMesh = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<OnUpdateNavMeshSystem>();
            ActiveBuildingType = Entity.Null;
        }

        private void Start()
        {
            navMeshHandler = EntityManager
                .CreateEntityQuery(ComponentType.ReadOnly<NavMeshInfoComponent>())
                .GetSingletonEntity();
            gameStateHandler = EntityManager
                .CreateEntityQuery(ComponentType.ReadOnly<GameStateComponent>())
                .GetSingletonEntity();
        }

        private void Update()
        {
            if (ActiveBuildingType == Entity.Null)
                return;
            
            var mousePosition = Utilities.GetMouseWorldPosition();
            var roundedMousePosition = Utilities.GetRoundedPoint(mousePosition.xy);
            var typeCoordZ = EntityManager.GetComponentData<Translation>(ActiveBuildingType).Value.z;
            ActiveBuildingSprite.transform.position = new Vector3(roundedMousePosition.x, roundedMousePosition.y);
            ActiveBuildingSprite.GetComponent<SpriteRenderer>().sortingOrder = 100;

            var gameState = EntityManager.GetComponentData<GameStateComponent>(gameStateHandler);
            var isBuildAvailable = CheckPositionForBuilding(roundedMousePosition, gameState);
            SetAppropriateColor(isBuildAvailable.Item1);
            
            if (Input.GetMouseButtonDown(1))
            {
                DestroyActive();
                return;
            }
            if (!isBuildAvailable.Item1)
                return;
            
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                if (!isBuildAvailable.Item2.Equals(new int2(100000, 100000)))
                {
                    var depositBuffer = EntityManager.GetBuffer<DepositsElementComponent>(navMeshHandler);
                    for (var i = 0; i < depositBuffer.Length; i++)
                    {
                        if (isBuildAvailable.Item2.Equals(depositBuffer[i].Position))
                        {
                            depositBuffer[i] = new DepositsElementComponent
                            {
                                Position = depositBuffer[i].Position,
                                IsAvailable = false
                            };
                        }
                    }
                }

                gameState.Resources1 -=
                    EntityManager.GetComponentData<EntityStatsComponent>(ActiveBuildingType).SpawnCost;
                EntityManager.SetComponentData(gameStateHandler, gameState);
                var entity = EntityManager.Instantiate(ActiveBuildingType);
                EntityManager.SetComponentData(entity, new Translation
                {
                    Value = new float3(mousePosition.xy, typeCoordZ)
                });
                EntityManager.AddBuffer<OrderQueueElementComponent>(entity);
                EntityManager.AddComponent(entity, ComponentType.ReadWrite<OrderQueueInfoComponent>());
                DestroyActive();
                UpdateNavMesh.RaiseNavMeshUpdateFlag();
            }
        }

        public void DestroyActive()
        {
            ActiveBuildingType = Entity.Null;
            Destroy(ActiveBuildingSprite);
            ActiveBuildingSprite = null;
        }

        private (bool, int2) CheckPositionForBuilding(int2 position, GameStateComponent gameState)
        {
            var stats = EntityManager.GetComponentData<EntityStatsComponent>(ActiveBuildingType);
            if (gameState.Resources1 < stats.SpawnCost)
                return (false, new int2(100000, 100000));
            
            var navMesh = EntityManager.GetBuffer<NavMeshElementComponent>(navMeshHandler);
            var deposits = EntityManager.GetBuffer<DepositsElementComponent>(navMeshHandler);
            var info = EntityManager.GetComponentData<NavMeshInfoComponent>(navMeshHandler);

            var index = Utilities.GetFlattenedIndex(position - info.Corners.c0,
                info.Corners.c1.x - info.Corners.c0.x + 1);

            if (Utilities.IsInRectangle(position, info.Corners.c0, info.Corners.c1))
            {
                if (EntityManager.GetComponentData<BuildingTypeComponent>(ActiveBuildingType).Type !=
                    BuildingType.Extractor)
                {
                    return (navMesh[index].DistanceToSolid > stats.BaseRadius
                           && navMesh[index].DistanceToBuilding < stats.SightRange, new int2(100000, 100000));
                }
                
                for (var i = 0; i < deposits.Length; i++)
                {
                    if (deposits[i].IsAvailable && math.distance(deposits[i].Position, position) < stats.SightRange)
                    {
                        return (true, deposits[i].Position);
                    }
                }
            }
            return (false, new int2(100000, 100000));
        }

        private void SetAppropriateColor(bool isBuildAvailable)
        {
            var spriteRenderer = ActiveBuildingSprite.GetComponent<SpriteRenderer>();
            
            if (isBuildAvailable)
            {
                spriteRenderer.color = new Color(0, 1, 0.5f, 0.75f);
            }
            else
            {
                spriteRenderer.color = new Color(1, 0, 0, 0.75f);
            }
        }
    }
}
