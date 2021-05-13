using ECS.Components;
using ECS.Flags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;

namespace ECS.Systems
{
    public class TestSpawn : SystemBase
    {
        private int cnt = 0;
        private bool spawned = false;
        
        protected override void OnUpdate()
        {
            if (cnt > 0)
                return;
            
            
            var building = GetSingleton<PrefabsComponent>().SimpleBuilding1Prefab;
            var buildingCopy = EntityManager.Instantiate(building);
            var health = GetSingleton<PrefabsComponent>().HealthBarPrefab;
            EntityManager.SetComponentData(buildingCopy, new Translation {Value = new float3(35, 35, 0)});
            EntityManager.AddComponentData(buildingCopy, new HealthBarReferenceComponent
            {
                HealthBarEntity = EntityManager.Instantiate(health)
            });
            EntityManager.CreateEntity(ComponentType.ReadWrite<NavMeshUpdateFlag>());
            for (var i = 0; i < 1; i++)
            {
                var e = GetSingleton<PrefabsComponent>().SimplePlayer2UnitPrefab;
                var eCopy = EntityManager.Instantiate(e);
                EntityManager.AddBuffer<MoveQueueElementComponent>(eCopy);
                EntityManager.AddBuffer<OrderQueueElementComponent>(eCopy);
                EntityManager.AddComponentData(eCopy, new OrderQueueInfoComponent {L = 0, R = 0, Count = 0});
                EntityManager.AddComponentData(eCopy, new MoveQueueInfoComponent {Index = 0, Count = 0});
                EntityManager.SetComponentData(eCopy, new Translation {Value = new float3(25, 25, 0)});
            }

            cnt++;
        }
    }
}
