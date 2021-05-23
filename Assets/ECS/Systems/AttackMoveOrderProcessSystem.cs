using ECS.Components;
using ECS.Other;
using ECS.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ECS.Systems
{
    [UpdateBefore(typeof(AttackSystem))]
    public class AttackMoveOrderProcessSystem : SystemBase
    {
        private const float ReachedPositionDistance = 5f;
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }

        private uint frameCount;

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            var navMeshHandler = GetSingletonEntity<NavMeshInfoComponent>();
            var navMesh = GetBuffer<NavMeshElementComponent>(navMeshHandler);
            var info = GetComponent<NavMeshInfoComponent>(navMeshHandler);
            var units = GetUnits();
            var translationGroup = GetComponentDataFromEntity<Translation>();
            var ownerGroup = GetComponentDataFromEntity<OwnerComponent>();
            var frame = ++frameCount;
            Entities
                .WithAll<AttackMoveOrderTag>()
                .ForEach((Entity entity, int entityInQueryIndex,
                    DynamicBuffer<OrderQueueElementComponent> orderQueue,
                    DynamicBuffer<MoveQueueElementComponent> moveQueue,
                    ref OrderQueueInfoComponent orderInfo, ref MoveQueueInfoComponent moveInfo, 
                    ref PhysicsMass physicsMass, in EntityStatsComponent stats) =>
                {
                    if (orderQueue[orderInfo.L].State == OrderState.Complete)
                        return;
                    
                    var rnd = Random.CreateFromIndex((uint)entityInQueryIndex + frame);
                    var r = rnd.NextInt(1, 10);
                    float2 targetPosition = orderQueue[orderInfo.L].MovePosition;
                    if (orderQueue[orderInfo.L].State == OrderState.New)
                    {
                        if (r == 1)
                            orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.InProgress);
                        physicsMass.InverseMass = 1 / math.pow(2, rnd.NextInt(4, 7));
                    }
                    else if (orderQueue[orderInfo.L].State == OrderState.InProgress)
                    {
                        if (orderQueue[orderInfo.L].Target == Entity.Null)
                        {
                            if (math.distance(translationGroup[entity].Value.xy, orderQueue[orderInfo.L].MovePosition)
                            < ReachedPositionDistance)
                            {
                                orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.Complete);
                                physicsMass.InverseMass = 1 / stats.BaseMass;
                                return;
                            }
                            var target = Entity.Null;
                            var ownerNumber = ownerGroup[entity].PlayerNumber;
                            foreach (var unit in units)
                            {
                                if (ownerGroup[unit].PlayerNumber != ownerNumber 
                                    && math.distance(translationGroup[entity].Value.xy, translationGroup[unit].Value.xy) 
                                    < math.max(stats.SightRange, stats.AttackRange))
                                {
                                    target = unit;
                                    break;
                                }
                            }
                            if (target == Entity.Null)
                            {
                                if (moveInfo.Index < moveInfo.Count)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithTarget(target);
                                return;
                            }
                        }
                        else
                        {
                            if (!translationGroup.HasComponent(orderQueue[orderInfo.L].Target))
                            {
                                parallelWriter.RemoveComponent<AttackTargetComponent>(entityInQueryIndex, entity);
                                orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithTarget(Entity.Null);
                                return;
                            }
                            var targetPos = translationGroup[orderQueue[orderInfo.L].Target].Value;
                            var distanceToTarget = math.distance(translationGroup[entity].Value.xy, targetPos.xy);
                            if (distanceToTarget < stats.AttackRange)
                            {
                                if (!HasComponent<AttackTargetComponent>(entity))
                                {
                                    parallelWriter.AddComponent(entityInQueryIndex, entity, new AttackTargetComponent
                                    {
                                        Target = orderQueue[orderInfo.L].Target
                                    });
                                    moveInfo.Count = 0;
                                    moveInfo.Index = 0;
                                }
                                return;
                            }
                            parallelWriter.RemoveComponent<AttackTargetComponent>(entityInQueryIndex, entity);
                            if (distanceToTarget > stats.SightRange)
                            {
                                orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithTarget(Entity.Null);
                                return;
                            }

                            targetPosition = targetPos.xy;
                        }
                    }
                    
                    if (r != 1)
                        return;
                    var roundedTargetPosition = Utilities.GetRoundedPoint(targetPosition);
                    var path = Utilities.AStar(
                        Utilities.GetRoundedPoint(translationGroup[entity].Value.xy),
                        roundedTargetPosition,
                        stats.BaseRadius,
                        info.Corners,
                        rnd,
                        info.MovesBlobAssetRef,
                        navMesh);
            
                    if (path[0].Equals(path[path.Length - 1]))
                    {
                        orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.Complete);
                        return;
                    }
                    
                    var size = math.min(path.Length, 100);
                    moveInfo.Index = 0;
                    moveInfo.Count = size;
                    for (var i = 0; i < size; i++)
                    {
                        if (i < moveQueue.Length) 
                        {
                            moveQueue[i] = new MoveQueueElementComponent(path[path.Length - i - 1]);
                        } 
                        else
                        {
                            moveQueue.Add(new MoveQueueElementComponent(path[path.Length - i - 1]));
                        }
                    }
                    
                    path.Dispose();
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
