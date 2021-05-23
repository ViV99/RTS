using ECS.Components;
using ECS.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems
{
    [UpdateBefore(typeof(AttackSystem))]
    public class IdleProcessSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }
        
        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            var units = GetUnits();
            var translationGroup = GetComponentDataFromEntity<Translation>();
            var ownerGroup = GetComponentDataFromEntity<OwnerComponent>();
            var attackTargetGroup = GetComponentDataFromEntity<AttackTargetComponent>();
            Entities
                .WithAll<UnitTag>()
                .WithNone<AttackOrderTag, AttackMoveOrderTag>()
                .ForEach((Entity entity, int entityInQueryIndex, in OrderQueueInfoComponent orderInfo,
                        in EntityStatsComponent stats) =>
                {
                    if (orderInfo.Count != 0)
                    {
                        parallelWriter.RemoveComponent<AttackTargetComponent>(entityInQueryIndex, entity);
                        return;
                    }
                    var target = Entity.Null;
                    var ownerNumber = ownerGroup[entity].PlayerNumber;
                    foreach (var unit in units)
                    {
                        if (ownerGroup[unit].PlayerNumber != ownerNumber 
                            && math.distance(translationGroup[entity].Value.xy, translationGroup[unit].Value.xy) 
                            < stats.AttackRange)
                        {
                            target = unit;
                            break;
                        }
                    }

                    if (attackTargetGroup.HasComponent(entity))
                    {
                        if (translationGroup.HasComponent(attackTargetGroup[entity].Target) 
                            && math.distance(translationGroup[attackTargetGroup[entity].Target].Value.xy,
                                translationGroup[entity].Value.xy) < stats.AttackRange)
                        {
                            return;
                        }

                        if (target != Entity.Null)
                        {
                            attackTargetGroup[entity] = new AttackTargetComponent {Target = target};
                        }
                        else
                        {
                            parallelWriter.RemoveComponent<AttackTargetComponent>(entityInQueryIndex, entity);
                        }
                    }
                    else
                    {
                        if (target != Entity.Null)
                        {
                            parallelWriter.AddComponent(entityInQueryIndex, entity, new AttackTargetComponent {Target = target});
                        }
                    }
                }).Schedule();
            CompleteDependency();
            units.Dispose();
        }
        
        private NativeList<Entity> GetUnits()
        {
            var result = new NativeList<Entity>(Allocator.TempJob);
            Entities
                .WithAny<UnitTag, BuildingTag>()
                .ForEach((Entity entity, in OwnerComponent owner) =>
                {
                    result.Add(entity);
                }).Schedule();
            return result;
        }
    }
}
