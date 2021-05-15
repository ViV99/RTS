using ECS.Components;
using ECS.Other;
using ECS.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ECS.Systems
{
    [UpdateAfter(typeof(OrderQueueUpdateSystem))]
    public class AttackOrderProcessSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }
        
        private uint frameCount; 

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            var manager = EntityManager;
            var navMeshHandler = GetSingletonEntity<NavMeshInfoComponent>();
            var navMesh = GetBuffer<NavMeshElementComponent>(navMeshHandler);
            var info = GetComponent<NavMeshInfoComponent>(navMeshHandler);
            var frame = ++frameCount;
            Entities
                .WithAll<AttackOrderTag>()
                .ForEach((Entity entity, int entityInQueryIndex, 
                    ref DynamicBuffer<OrderQueueElementComponent> orderQueue, 
                    ref DynamicBuffer<MoveQueueElementComponent> moveQueue, 
                    ref MoveQueueInfoComponent moveInfo, ref PhysicsMass physicsMass, 
                    in Translation translation, in EntityStatsComponent stats) =>
                {
                    var orderInfo = GetComponent<OrderQueueInfoComponent>(entity);
                    if (orderQueue[orderInfo.L].State == OrderState.Complete)
                        return;
                    if (!HasComponent<Translation>(orderQueue[orderInfo.L].Target))
                    {
                        orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.Complete);
                        parallelWriter.RemoveComponent<AttackTargetComponent>(entityInQueryIndex, entity);
                        physicsMass.InverseMass = 1;
                        return;
                    }
                    var rnd = Random.CreateFromIndex((uint)entityInQueryIndex + frame);
                    var r = rnd.NextInt(1, 10);
                    var targetPosition = GetComponent<Translation>(orderQueue[orderInfo.L].Target).Value;
                    var distanceToTarget = math.distance(translation.Value.xy, targetPosition.xy);
                    if (orderQueue[orderInfo.L].State == OrderState.New)
                    {
                        if (r == 1)
                            orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.InProgress);
                        physicsMass.InverseMass = 1 / math.pow(2, rnd.NextInt(4, 7));
                    }    
                    else if (orderQueue[orderInfo.L].State == OrderState.InProgress)
                    {
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
                        if (distanceToTarget > stats.SightRange && moveInfo.Index < moveInfo.Count)
                        {
                            return;
                        }
                    }
                    if (r != 1)
                        return;
                    var roundedTargetPosition = Utilities.GetRoundedPoint(targetPosition.xy);
                    var path = Utilities.AStar(
                        Utilities.GetRoundedPoint(translation.Value.xy),
                        roundedTargetPosition,
                        GetComponent<CompositeScale>(entity).Value.c0.x,
                        info.Corners,
                        rnd,
                        info.MovesBlobAssetRef,
                        navMesh);
            
                    if (!path[0].Equals(roundedTargetPosition))
                    {
                        orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithMovePosition(path[0]);
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
                }).WithoutBurst().Schedule();
            CompleteDependency();
        }
    }
}
