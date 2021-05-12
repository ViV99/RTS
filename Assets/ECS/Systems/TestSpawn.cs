using ECS.Components;
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
        
        protected override void OnUpdate()
        {
            if (cnt > 0)
                return;
            for (var i = 0; i < 5; i++)
            {
                var e = GetSingleton<PrefabsComponent>().SimplePlayer1UnitPrefab;
                var e1 = GetSingleton<PrefabsComponent>().HealthBarPrefab;
                var eCopy = EntityManager.Instantiate(e);
                EntityManager.AddBuffer<MoveQueueElementComponent>(eCopy);
                EntityManager.AddBuffer<OrderQueueElementComponent>(eCopy);
                EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<OrderQueueInfoComponent>());
                EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<MoveQueueInfoComponent>());
                EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<MoveToComponent>());
                EntityManager.AddComponentData(eCopy, new HealthBarReferenceComponent
                {
                    HealthBarEntity = EntityManager.Instantiate(e1)
                });
            }
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
