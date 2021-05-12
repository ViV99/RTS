using ECS.Components;
using ECS.Other;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace ECS.Systems
{
    public class RotationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((
                ref MoveToComponent moveTo,
                ref Rotation rotation,
                ref UnitStatsComponent stats) =>
            {
                if (!moveTo.IsMoving)
                    return;
            
                var directionQuaternion = quaternion.Euler(
                    0,
                    0,
                    Utilities.GetAngleFromVectorFloat(moveTo.LastMoveDirection));
                rotation.Value = math.slerp(rotation.Value, directionQuaternion, stats.TurnSpeed);
            }).Schedule();
        }
    }
}