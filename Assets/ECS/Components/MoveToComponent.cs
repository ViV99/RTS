using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct MoveToComponent : IComponentData
    {
        public float3 Position;
        public float TurnSpeed;
        public float MoveSpeed;
        public bool IsMoving;
        public float3 LastMoveDirection;
    }
}