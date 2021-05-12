using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct MoveToComponent : IComponentData
    {
        public float TurnSpeed;
        public float MoveSpeed; 
        [NonSerialized]public int2 Position;
        [NonSerialized]public bool IsMoving;
        [NonSerialized]public float3 LastMoveDirection;
        [NonSerialized]public float2 LastStatePosition;
        [NonSerialized]public int FramesCount;
    }
}