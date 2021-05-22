using System;
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

        public Entity ActiveBuildingType { get; set; }
        public GameObject ActiveBuildingSprite { get; set; }

        private void Awake()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            UpdateNavMesh = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<OnUpdateNavMeshSystem>();
            ActiveBuildingType = Entity.Null;
        }

        private void Update()
        {
            if (ActiveBuildingType == Entity.Null)
                return;
            var roundedMousePosition = Utilities.GetRoundedPoint(Utilities.GetMouseWorldPosition().xy);
            ActiveBuildingSprite.transform.position = new Vector3(roundedMousePosition.x, roundedMousePosition.y);

            var isBuildAvailable = CheckPositionForBuilding(roundedMousePosition);
            SetAppropriateColor(isBuildAvailable);
            
            if (Input.GetMouseButtonDown(1))
            {
                DestroyActive();
                return;
            }
            if (!isBuildAvailable)
                return;
            
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                var mousePos = Utilities.GetMouseWorldPosition();
                var entity = EntityManager.Instantiate(ActiveBuildingType);
                EntityManager.SetComponentData(entity, new Translation
                {
                    Value = mousePos
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

        private bool CheckPositionForBuilding(int2 position)
        {
            var navMeshHandler = EntityManager
                .CreateEntityQuery(ComponentType.ReadOnly<NavMeshInfoComponent>())
                .GetSingletonEntity();
            var navMesh = EntityManager.GetBuffer<NavMeshElementComponent>(navMeshHandler);
            var info = EntityManager.GetComponentData<NavMeshInfoComponent>(navMeshHandler);
            
            var index = Utilities.GetFlattenedIndex(position - info.Corners.c0,
                info.Corners.c1.x - info.Corners.c0.x + 1);
            var stats = EntityManager.GetComponentData<EntityStatsComponent>(ActiveBuildingType);
            return Utilities.IsInRectangle(position, info.Corners.c0, info.Corners.c1)
                   && navMesh[index].DistanceToSolid > stats.BaseRadius * math.SQRT2
                   && navMesh[index].DistanceToBuilding < stats.SightRange;
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
