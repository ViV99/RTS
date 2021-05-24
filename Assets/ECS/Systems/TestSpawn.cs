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
            
            Spawn(GetSingleton<PrefabsComponent>().HQ1);
            //Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab1);
            //Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab1);
            //Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab1);
			//Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab2);
            /*
            Spawn(GetSingleton<PrefabsComponent>().FighterPrefab1);
            Spawn(GetSingleton<PrefabsComponent>().BattleshipPrefab1);
            Spawn(GetSingleton<PrefabsComponent>().DestroyerAAPrefab1);
            Spawn(GetSingleton<PrefabsComponent>().TorpedoCruiserPrefab1);
            Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab1);
			Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab1);
			Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab1);
			Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab1);
			*/
			/*
            Spawn(GetSingleton<PrefabsComponent>().FighterPrefab2);
            Spawn(GetSingleton<PrefabsComponent>().BattleshipPrefab2);
            Spawn(GetSingleton<PrefabsComponent>().DestroyerAAPrefab2);
            Spawn(GetSingleton<PrefabsComponent>().TorpedoCruiserPrefab2);
            Spawn(GetSingleton<PrefabsComponent>().JuggernautPrefab2);
            
            */
            
            cnt++;
        }

        private void Spawn(Entity entity)
        {
            var eCopy = EntityManager.Instantiate(entity);
            var health = GetSingleton<PrefabsComponent>().HealthBarPrefab;
			var label = GetSingleton<PrefabsComponent>().SelectedLabelPrefab;
            EntityManager.AddBuffer<MoveQueueElementComponent>(eCopy);
            EntityManager.AddBuffer<OrderQueueElementComponent>(eCopy);
            EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<OrderQueueInfoComponent>());
            EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<MoveQueueInfoComponent>());
            EntityManager.AddComponent(eCopy, ComponentType.ReadWrite<MoveToComponent>());
            EntityManager.AddComponentData(eCopy, new HealthBarReferenceComponent
            {    
                HealthBarEntity = EntityManager.Instantiate(health)
            });
			EntityManager.AddComponentData(eCopy, new SelectedLabelReferenceComponent
            {    
                SelectedLabelEntity = EntityManager.Instantiate(label)
            });
        }
    }
}
