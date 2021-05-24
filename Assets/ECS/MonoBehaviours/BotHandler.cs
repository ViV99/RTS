using System;
using System.Collections.Generic;
using System.Linq;
using ECS.Components;
using ECS.Other;
using ECS.Systems;
using ECS.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = System.Random;

namespace ECS.MonoBehaviours
{
    public class BotHandler : MonoBehaviour
    {
        private EntityManager EntityManager { get; set; }
        private OnUpdateNavMeshSystem UpdateNavMesh { get; set; }
        private List<Entity> Shipyards { get; set; }
        private List<Entity> CounterShipyards { get; set; }
        private List<int> ExtractorOrder { get; set; }
        private List<int2> ShipyardPos { get; set; }
        private List<int2> AttackPos { get; set; }

        private Entity navMeshHandler;
        private Entity gameStateHandler;
        private PrefabsComponent prefabs;

        private int currentBuildingReload;
        private int currentAttackMoveReload;

        private void Awake()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            UpdateNavMesh = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<OnUpdateNavMeshSystem>();
            Shipyards = new List<Entity>();
            CounterShipyards = new List<Entity>();
            ExtractorOrder = new List<int> {9, 6, 8, 7, 5, 4};
            ShipyardPos = new List<int2>
            {
                new int2(405, 320), new int2(220, 310), new int2 (495, -115), new int2(245, -125),
                new int2(215, -365), new int2(95, -145), new int2(550, 160), new int2(535, -355)
            };
            AttackPos = new List<int2>
            {
                new int2(480, 305), new int2(-500, -285), new int2 (1, 1), new int2(-90, -20),
                new int2(-150, 115), new int2(-280, 225), new int2(-515, -95), new int2(95, -220)
            };
            currentBuildingReload = 100;
            currentAttackMoveReload = 100;
        }

        private void Start()
        {
            navMeshHandler = EntityManager
                .CreateEntityQuery(ComponentType.ReadWrite<NavMeshInfoComponent>())
                .GetSingletonEntity();
            gameStateHandler = EntityManager
                .CreateEntityQuery(ComponentType.ReadWrite<GameStateComponent>())
                .GetSingletonEntity();
            prefabs = EntityManager
                .CreateEntityQuery(ComponentType.ReadWrite<PrefabsComponent>())
                .GetSingleton<PrefabsComponent>();
        }

        private void Update()
        {
            currentBuildingReload--;
            currentAttackMoveReload--;

            if (currentBuildingReload <= 0)
            {
                var reload = SpawnRandomBuilding();
                currentBuildingReload = reload;
            }
            
            GiveRandomSpawnOrders();

            if (currentAttackMoveReload <= 0)
            {
                GiveAttackMoveOrders();
                currentAttackMoveReload = 900;
            }
        }

        private void GiveRandomSpawnOrders()
        {
            for (var i = 0; i < Shipyards.Count; i++)
            {
                if (!EntityManager.HasComponent<Translation>(Shipyards[i]))
                    continue;
                var orderInfo = EntityManager.GetComponentData<OrderQueueInfoComponent>(Shipyards[i]);
                if (orderInfo.Count == 0)
                {
                    var target = GetRandomShipyardUnit();
                    var orderQueue = EntityManager.GetBuffer<OrderQueueElementComponent>(Shipyards[i]);
                    var order = new OrderQueueElementComponent(OrderType.Spawn, OrderState.New, int2.zero, target);
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
                    EntityManager.SetComponentData(Shipyards[i], orderInfo);
                }
            }

            for (var i = 0; i < CounterShipyards.Count; i++)
            {
                if (!EntityManager.HasComponent<Translation>(CounterShipyards[i]))
                    continue;
                var orderInfo = EntityManager.GetComponentData<OrderQueueInfoComponent>(CounterShipyards[i]);
                if (orderInfo.Count == 0)
                {
                    var spawnEntity = GetRandomCounterShipyardUnit();
                    if (spawnEntity == Entity.Null)
                        continue;
                    var orderQueue = EntityManager.GetBuffer<OrderQueueElementComponent>(CounterShipyards[i]);
                    var order = new OrderQueueElementComponent(OrderType.Spawn, OrderState.New, int2.zero, spawnEntity);
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
                    EntityManager.SetComponentData(CounterShipyards[i], orderInfo);
                }
            }
        }

        private void GiveAttackMoveOrders()
        {
            var rnd = new Random();
            var types = new ComponentType[2] {ComponentType.ReadWrite<OwnerComponent>(), ComponentType.ReadOnly<UnitTag>()};
            var units = EntityManager.CreateEntityQuery(types).ToEntityArray(Allocator.Temp);
            for (var i = 0; i < units.Length; i++)
            {
                if (!EntityManager.HasComponent<Translation>(units[i]) 
                    || EntityManager.GetComponentData<OwnerComponent>(units[i]).PlayerNumber != 2)
                    continue;
                if (rnd.Next(1, 101) > 70)
                {
                    continue;
                }
                var orderInfo = EntityManager.GetComponentData<OrderQueueInfoComponent>(units[i]);
                if (orderInfo.Count == 0)
                {
                    var orderQueue = EntityManager.GetBuffer<OrderQueueElementComponent>(units[i]);
                    var order = new OrderQueueElementComponent(
                        OrderType.AttackMove, 
                        OrderState.New, 
                        AttackPos[rnd.Next(0, AttackPos.Count)], 
                        Entity.Null);
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
                    EntityManager.SetComponentData(units[i], orderInfo);
                }
            }

            units.Dispose();
        }
        
        private int SpawnRandomBuilding()
        {
            var rnd = new Random();
            var number = rnd.Next(1, 101);
            if (number <= 40)
            {
                SpawnExtractor();
                return 3500;
            }
            if (number <= 85)
            {
                SpawnShipyard();
                return 6000;
            }
            SpawnCounterShipyard();
            return 7500;
        }
        
        private Entity GetRandomShipyardUnit()
        {
            var rnd = new Random();
            var number = rnd.Next(1, 101);
            if (number <= 85)
            {
                return prefabs.FighterPrefab2;
            }

            return prefabs.BattleshipPrefab2;
        }
        
        private Entity GetRandomCounterShipyardUnit()
        {
            var rnd = new Random();
            var number = rnd.Next(1, 101);
            if (number <= 16)
            {
                return prefabs.DestroyerAAPrefab2;
            }
            if (number <= 22)
            {
                return prefabs.TorpedoCruiserPrefab2;
            }
            if (number <= 26)
            {
                return prefabs.JuggernautPrefab2;
            }

            return Entity.Null;
        }

        private void SpawnExtractor()
        {
            var deposits = EntityManager.GetBuffer<DepositsElementComponent>(navMeshHandler);
            foreach (var i in ExtractorOrder.Where(i => deposits[i].IsAvailable))
            {
                deposits[i] = new DepositsElementComponent
                {
                    Position = deposits[i].Position,
                    IsAvailable = false
                };
                SpawnBuilding(prefabs.Extractor2, deposits[i].Position);
                return;
            }
        }

        private void SpawnShipyard()
        {
            var navMesh = EntityManager.GetBuffer<NavMeshElementComponent>(navMeshHandler);
            var info = EntityManager.GetComponentData<NavMeshInfoComponent>(navMeshHandler);
            foreach (var pos in ShipyardPos)
            {
                var index = Utilities.GetFlattenedIndex(pos + new int2(1, 1) - info.Corners.c0,
                    info.Corners.c1.x - info.Corners.c0.x + 1);
                if (navMesh[index].DistanceToSolid 
                    > EntityManager.GetComponentData<EntityStatsComponent>(prefabs.Shipyard2).BaseRadius)
                {
                    Shipyards.Add(SpawnBuilding(prefabs.Shipyard2, pos));
                    return;
                }
            }
        }
        
        private void SpawnCounterShipyard()
        {
            var navMesh = EntityManager.GetBuffer<NavMeshElementComponent>(navMeshHandler);
            var info = EntityManager.GetComponentData<NavMeshInfoComponent>(navMeshHandler);
            foreach (var pos in ShipyardPos)
            {
                var index = Utilities.GetFlattenedIndex(pos + new int2(1, 1) - info.Corners.c0,
                    info.Corners.c1.x - info.Corners.c0.x + 1);
                if (navMesh[index].DistanceToSolid 
                    > EntityManager.GetComponentData<EntityStatsComponent>(prefabs.CounterShipyard2).BaseRadius)
                {
                    CounterShipyards.Add(SpawnBuilding(prefabs.CounterShipyard2, pos));
                    return;
                }
            }
        }

        private Entity SpawnBuilding(Entity building, int2 position)
        {
            var buildingCopy = EntityManager.Instantiate(building);
            EntityManager.SetComponentData(buildingCopy, new Translation
            {
                Value = new float3(position, EntityManager.GetComponentData<Translation>(buildingCopy).Value.z)
            });
            EntityManager.AddBuffer<OrderQueueElementComponent>(buildingCopy);
            EntityManager.AddComponent(buildingCopy, ComponentType.ReadWrite<OrderQueueInfoComponent>());
            UpdateNavMesh.RaiseNavMeshUpdateFlag();
            return buildingCopy;
        }
    }
}
