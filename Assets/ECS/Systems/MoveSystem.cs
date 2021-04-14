using ECS.Components;
using ECS.Other;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems
{
    public class MoveSystem : SystemBase
    {
        private const float ReachedPositionDistance = 1f;

        protected override void OnUpdate()
        {
            var deltaTime = Time.DeltaTime;

            Entities.ForEach(
                (ref Translation translation, ref MoveToComponent moveToComponent, ref Rotation rotation) =>
                {
                    if (!moveToComponent.IsMoving)
                        return;

                    if (math.distance(translation.Value, moveToComponent.Position) > ReachedPositionDistance)
                    {
                        moveToComponent.LastMoveDirection =
                            math.normalizesafe(moveToComponent.Position - translation.Value);
                        translation.Value += moveToComponent.LastMoveDirection * moveToComponent.MoveSpeed * deltaTime;
                        var directionQuaternion = quaternion.Euler(
                            0,
                            0,
                            Utilities.GetAngleFromVectorFloat(moveToComponent.LastMoveDirection));
                        rotation.Value = math.slerp(rotation.Value, directionQuaternion, moveToComponent.TurnSpeed);
                    }
                    else
                    {
                        moveToComponent.IsMoving = false;
                    }
                }).Schedule();
        }
    }
}