using System;
using System.Diagnostics;
using ECS.Components;
using ECS.MonoBehaviours;
using ECS.Other;
using ECS.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;

namespace ECS.Systems
{
    [UpdateAfter(typeof(OrderQueueUpdateSystem))]
    public class MoveOrderProcessSystem : SystemBase
    {
        private const float ReachedPositionDistance = 5f;
        private uint frameCount;

        protected override void OnUpdate()
        {
            var navMeshHandler = GetSingletonEntity<NavMeshInfoComponent>();
            var navMesh = GetBuffer<NavMeshElementComponent>(navMeshHandler);
            var info = GetComponent<NavMeshInfoComponent>(navMeshHandler);
            var frame = ++frameCount;
            Entities
                .WithAll<MoveOrderTag>()
                .ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<OrderQueueElementComponent> orderQueue, 
                    ref DynamicBuffer<MoveQueueElementComponent> moveQueue, 
                    ref MoveQueueInfoComponent moveInfo, ref OrderQueueInfoComponent orderInfo,
                    ref PhysicsMass physicsMass, in Translation translation) =>
                {
                    var rnd = Random.CreateFromIndex((uint)entityInQueryIndex + frame);
                    var r = rnd.NextInt(1, 10);
                    switch (orderQueue[orderInfo.L].State)
                    {
                        case OrderState.Complete:
                            return;
                        case OrderState.New:
                            if (r == 1)
                                orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.InProgress);
                            physicsMass.InverseMass = 1 / math.pow(2, rnd.NextInt(4, 7));
                            break;
                        case OrderState.InProgress 
                            when math.distance(translation.Value.xy, orderQueue[orderInfo.L].MovePosition) 
                                 < ReachedPositionDistance:
                            orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.Complete);
                            physicsMass.InverseMass = 1;
                            return;
                        case OrderState.InProgress when moveInfo.Index < moveInfo.Count:
                            return;
                    }
                    if (r != 1)
                        return;
                    var path = Utilities.AStar(
                        Utilities.GetRoundedPoint(translation.Value.xy),
                        orderQueue[orderInfo.L].MovePosition,
                        GetComponent<CompositeScale>(entity).Value.c0.x,
                        info.Corners,
                        rnd,
                        info.MovesBlobAssetRef,
                        navMesh);

                    if (!path[0].Equals(orderQueue[orderInfo.L].MovePosition))
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
