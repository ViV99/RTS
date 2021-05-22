using ECS.Components;
using ECS.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems
{
    public class MoveWithPathSystem : SystemBase
    {
        private const float ReachedPositionDistance = 0.5f;
        
        protected override void OnUpdate()
        {
            Entities
                .WithNone<ProjectileTag>()
                .ForEach((ref Translation translation, ref MoveToComponent moveTo, ref PhysicsVelocity physicsVelocity,
                    in EntityStatsComponent stats) =>
                {
                    if (!moveTo.IsMoving)
                        return;
                    
                    var moveToPosFloat = new float3(moveTo.Position, 0);
                    var distToGoal = math.distance(translation.Value, moveToPosFloat);
                    var distFromLast = math.distance(moveTo.LastStatePosition, translation.Value.xy);
                    if (distToGoal > ReachedPositionDistance)
                    {
                        if (moveTo.FramesCount == 6)
                        {
                            if (distFromLast < ReachedPositionDistance / 7)
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