using ECS.Components;
using ECS.Flags;
using ECS.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems
{
    public class MoveWithTargetSystem : SystemBase
    {
        private const float ReachedPositionDistance = 4f;
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }
        
        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            var navMeshHandler = GetSingletonEntity<NavMeshInfoComponent>();
            var gameStateHandler = GetSingletonEntity<GameStateComponent>();
            var deposits = EntityManager.GetBuffer<DepositsElementComponent>(navMeshHandler);
            var statsGroup = GetComponentDataFromEntity<EntityStatsComponent>();
            var translationGroup = GetComponentDataFromEntity<Translation>();
            Entities
                .WithAll<ProjectileTag>()
                .ForEach((Entity entity, int entityInQueryIndex, 
                    ref MoveToComponent moveTo, ref PhysicsVelocity physicsVelocity) =>
                {
                    if (!translationGroup.HasComponent(moveTo.Target)
                        || math.distance(moveTo.LastStatePosition, translationGroup[entity].Value.xy)
                        > statsGroup[entity].AttackRange)
                    {
                        parallelWriter.DestroyEntity(entityInQueryIndex, entity);
                        return;
                    }
                    var targetStats = statsGroup[moveTo.Target];
                    var targetTranslation = translationGroup[moveTo.Target].Value;
                    if (math.distance(targetTranslation.xy, translationGroup[entity].Value.xy)
                        < ReachedPositionDistance)
                    {
                        targetStats = targetStats.WithHealth(targetStats.CurrentHealth
                                                             - math.max(statsGroup[entity].Damage - targetStats.Armor,
                                                                 1));

                        if (targetStats.CurrentHealth <= 0)
                        {
                            var gameState = GetComponent<GameStateComponent>(gameStateHandler);
                            if (HasComponent<BuildingTag>(moveTo.Target))
                            {
                                var type = GetComponent<BuildingTypeComponent>(moveTo.Target).Type;
                                if (type == BuildingType.Extractor)
                                {
                                    for (var i = 0; i < deposits.Length; i++)
                                    {
                                        if (math.distance(deposits[i].Position,
                                                translationGroup[moveTo.Target].Value.xy)
                                            < statsGroup[moveTo.Target].SightRange)
                                        {
                                            deposits[i] = new DepositsElementComponent
                                            {
                                                Position = deposits[i].Position,
                                                IsAvailable = true
                                            };
                                        }
                                    }
                                } 
                                else if (type == BuildingType.Shipyard || type == BuildingType.CounterShipyard)
                                {
                                    var orderInfo = GetComponent<OrderQueueInfoComponent>(moveTo.Target);
                                    if (orderInfo.Count != 0)
                                    {
                                        var orderQueue = GetBuffer<OrderQueueElementComponent>(moveTo.Target);
                                        if (GetComponent<OwnerComponent>(orderQueue[orderInfo.L].Target).PlayerNumber == 1)
                                        {
                                            gameState.Pop1 -= statsGroup[orderQueue[orderInfo.L].Target].Pop;
                                        }
                                        else
                                        {
                                            gameState.Pop2 -= statsGroup[orderQueue[orderInfo.L].Target].Pop;
                                        }
                                        parallelWriter.SetComponent(entityInQueryIndex, gameStateHandler, gameState);
                                    }
                                }
                                var flag = parallelWriter.CreateEntity(entityInQueryIndex);
                                parallelWriter.AddComponent(entityInQueryIndex, flag,
                                    ComponentType.ReadOnly<NavMeshUpdateFlag>());
                            }
                            else
                            {
                                if (GetComponent<OwnerComponent>(moveTo.Target).PlayerNumber == 1)
                                {
                                    gameState.Pop1 -= statsGroup[moveTo.Target].Pop;
                                }
                                else
                                {
                                    gameState.Pop2 -= statsGroup[moveTo.Target].Pop;
                                }
                                parallelWriter.SetComponent(entityInQueryIndex, gameStateHandler, gameState);
                            }
                            parallelWriter.DestroyEntity(entityInQueryIndex, moveTo.Target);
                        }
                        else
                        {
                            statsGroup[moveTo.Target] = targetStats;
                        }
                        parallelWriter.DestroyEntity(entityInQueryIndex, entity);
                    }

                    moveTo.LastMoveDirection = new float3(math.normalizesafe(targetTranslation.xy
                                                                             - translationGroup[entity].Value.xy), 0);
                    physicsVelocity.Linear = moveTo.LastMoveDirection * statsGroup[entity].MoveSpeed;
                }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
            Ecb.Update();
        }
    }
}
