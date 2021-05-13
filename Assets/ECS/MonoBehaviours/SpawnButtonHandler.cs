using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;


public class SpawnButtonHandler : MonoBehaviour
{
    private EntityManager entityManager;

    private void Awake()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    
    public void OnUpdate()
    {
        for (var i = 0; i < 5; i++)
        {
            var huj = entityManager.CreateEntityQuery(ComponentType.ReadWrite<PrefabsComponent>());

            var e = huj.GetSingleton<PrefabsComponent>().SimplePlayer1UnitPrefab;
            var e1 = huj.GetSingleton<PrefabsComponent>().HealthBarPrefab;
            var eCopy = entityManager.Instantiate(e);
            entityManager.AddBuffer<MoveQueueElementComponent>(eCopy);
            entityManager.AddBuffer<OrderQueueElementComponent>(eCopy);
            entityManager.AddComponent(eCopy, ComponentType.ReadWrite<OrderQueueInfoComponent>());
            entityManager.AddComponent(eCopy, ComponentType.ReadWrite<MoveQueueInfoComponent>());
            entityManager.AddComponent(eCopy, ComponentType.ReadWrite<MoveToComponent>());
            entityManager.AddComponentData(eCopy, new HealthBarReferenceComponent
            {
                HealthBarEntity = entityManager.Instantiate(e1)
            });
        }
    }
    
}
