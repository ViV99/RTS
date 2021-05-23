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
        private const float ReachedPositionDistance = 1f;
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }
        
        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer();
            var navMeshHandler = GetSingletonEntity<NavMeshInfoComponent>();
            var deposits = EntityManager.GetBuffer<DepositsElementComponent>(navMeshHandler);
            var statsGroup = GetComponentDataFromEntity<EntityStatsComponent>();
            var translationGroup = GetComponentDataFromEntity<Translation>();
            Entities
                .WithAll<ProjectileTag>()
                .ForEach((Entity entity, ref MoveToComponent moveTo, ref PhysicsVelocity physicsVelocity) =>
                {
                    if (!translationGroup.HasComponent(moveTo.Target)
                        || math.distance(moveTo.LastStatePosition, translationGroup[entity].Value.xy)
                        > statsGroup[entity].AttackRange)
                    {
                        parallelWriter.DestroyEntity(entity);
                        return;
                    }
                    var targetStats = statsGroup[moveTo.Target];
                    var targetTranslation = GetComponent<Translation>(moveTo.Target).Value;
                    if (math.distance(targetTranslation.xy, translationGroup[entity].Value.xy)
                        < ReachedPositionDistance)
                    {
                        targetStats = targetStats.WithHealth(targetStats.CurrentHealth
                                                             - math.max(statsGroup[entity].Damage - targetStats.Armor, 1));

                        if (targetStats.CurrentHealth <= 0)
                        {
                            if (HasComponent<BuildingTag>(moveTo.Target))
                            {
                                if (GetComponent<BuildingTypeComponent>(moveTo.Target).Type == BuildingType.Extractor)
                                {
                                    for (var i = 0; i < deposits.Length; i++)
                                    {
                                        if (math.distance(deposits[i].Position, translationGroup[moveTo.Target].Value.xy)
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
                            }
                            parallelWriter.DestroyEntity(moveTo.Target);
                            var flag = parallelWriter.CreateEntity();
                            parallelWriter.AddComponent(flag, ComponentType.ReadOnly<NavMeshUpdateFlag>());
                        }
                        else
                        {
                            parallelWriter.SetComponent(moveTo.Target, targetStats);
                        }
                        parallelWriter.DestroyEntity(entity);
                    }
                    
                    moveTo.LastMoveDirection = new float3(math.normalizesafe(targetTranslation.xy
                                                                  - translationGroup[entity].Value.xy), 0);
                    physicsVelocity.Linear = moveTo.LastMoveDirection * statsGroup[entity].MoveSpeed;
                }).Run();
            CompleteDependency();
        }
    }
}
