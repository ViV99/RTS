using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems
{
    public class TestSpawn : SystemBase
    {
        private int cnt = 0;
        
        protected override void OnUpdate()
        {
            if (cnt > 0)
                return;

            // var e1 = GetSingleton<PrefabsComponent>().HQ1;
            // var e1Copy = EntityManager.Instantiate(e1);
            // EntityManager.AddBuffer<OrderQueueElementComponent>(e1Copy);
            // EntityManager.AddComponent(e1Copy, ComponentType.ReadWrite<OrderQueueInfoComponent>());
            // EntityManager.SetComponentData(e1Copy, new Translation { Value = new float3(100, -100, 0)});
            //
            // // Spawn(GetSingleton<PrefabsComponent>().FighterPrefab1, float2.zero);
            // // Spawn(GetSingleton<PrefabsComponent>().BattleshipPrefab1, float2.zero);
            // // Spawn(GetSingleton<PrefabsComponent>().DestroyerAAPrefab1, float2.zero);
            // // Spawn(GetSingleton<PrefabsComponent>().TorpedoCruiserPrefab1, float2.zero);
            // // Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab1, float2.zero);
            // for (var i = 0; i < 10; i++) 
            //     Spawn(GetSingleton<PrefabsComponent>().FighterPrefab2, new float2(300, 300));
            // // Spawn(GetSingleton<PrefabsComponent>().BattleshipPrefab2, new float2(300, 300));
            // // Spawn(GetSingleton<PrefabsComponent>().DestroyerAAPrefab2, new float2(300, 300));
            // // Spawn(GetSingleton<PrefabsComponent>().TorpedoCruiserPrefab2, new float2(300, 300));
            // // Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab2, new float2(300, 300));
            // for (var i = 0; i < 50; i++)
            //     Spawn(GetSingleton<PrefabsComponent>().DestroyerAAPrefab1, float2.zero);
            // for (var i = 0; i < 10; i++)
            //     Spawn(GetSingleton<PrefabsComponent>().TorpedoCruiserPrefab1, float2.zero);
            
            cnt++;
        }

        private void Spawn(Entity entity, float2 translation)
        {
            var eCopy = EntityManager.Instantiate(entity);
            EntityManager.AddBuffer<MoveQueueElementComponent>(eCopy);
            EntityManager.AddBuffer<OrderQueueElementComponent>(eCopy);
            EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<OrderQueueInfoComponent>());
            EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<MoveQueueInfoComponent>());
            EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<MoveToComponent>());
            EntityManager.SetComponentData(eCopy, new Translation
            {
                Value = new float3(translation, EntityManager.GetComponentData<Translation>(eCopy).Value.z)
            });
        }
    }
}
