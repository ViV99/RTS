using ECS.Components;
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
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer();
            
            Entities
                .WithAll<ProjectileTag>()
                .ForEach((Entity entity, 
                    ref MoveToComponent moveTo, ref PhysicsVelocity physicsVelocity,
                    in EntityStatsComponent stats, in Translation translation) =>
                {
                    if (!HasComponent<Translation>(moveTo.Target) 
                        || math.distance(moveTo.LastStatePosition, translation.Value.xy) > stats.AttackRange)
                    {
                        parallelWriter.DestroyEntity(entity);
                        return;
                    }
                    if (math.distance(GetComponent<Translation>(moveTo.Target).Value, translation.Value)
                        < GetComponent<CompositeScale>(moveTo.Target).Value.c0.x)
                    {
                        var targetStats = GetComponent<EntityStatsComponent>(moveTo.Target);
                        targetStats = targetStats.WithHealth(targetStats.CurrentHealth
                                                             - math.max(stats.Damage - targetStats.Armor, 1));

                        if (targetStats.CurrentHealth <= 0)
                        {
                            parallelWriter.DestroyEntity(moveTo.Target);
                        }
                        else
                        {
                            parallelWriter.SetComponent(moveTo.Target, targetStats);
                        }
                        parallelWriter.DestroyEntity(entity);
                    }
                    
                    moveTo.LastMoveDirection = math.normalizesafe(GetComponent<Translation>(moveTo.Target).Value
                                                                  - translation.Value);
                    physicsVelocity.Linear = moveTo.LastMoveDirection * stats.MoveSpeed;
                }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
        }
    }
}
