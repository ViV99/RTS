using ECS.Components;
using ECS.Other;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems
{
    public class MoveSystem : SystemBase
    {
        private const float ReachedPositionDistance = 0.5f;
        
        protected override void OnUpdate()
        {
            Entities.ForEach((
                ref Translation translation,
                ref MoveToComponent moveTo,
                ref PhysicsVelocity physicsVelocity,
                ref UnitStatsComponent stats) =>
                {
                    if (!moveTo.IsMoving)
                        return;
                    
                    var moveToPosFloat = new float3(moveTo.Position, 0);
                    if (math.distance(translation.Value, moveToPosFloat) > ReachedPositionDistance)
                    {
                        if (moveTo.FramesCount == 6)
                        {
                            if (math.distance(moveTo.LastStatePosition, translation.Value.xy) 
                                < ReachedPositionDistance / 7)
                                moveTo.IsMoving = false;
                            moveTo.LastStatePosition = translation.Value.xy;
                            moveTo.FramesCount = 0;
                        }
                        moveTo.LastMoveDirection = math.normalizesafe(moveToPosFloat - translation.Value);
                        physicsVelocity.Linear = moveTo.LastMoveDirection * stats.MoveSpeed;
                        moveTo.FramesCount++;
                    }
                    else
                    {
                        moveTo.IsMoving = false;
                    }
                }).Schedule();
        }
    }
}