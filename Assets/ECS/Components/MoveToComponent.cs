using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    public struct MoveToComponent : IComponentData
    {
        public int2 Position;
        public bool IsMoving;
        public float3 LastMoveDirection;
        public float2 LastStatePosition;
        public int FramesCount;
    }
}