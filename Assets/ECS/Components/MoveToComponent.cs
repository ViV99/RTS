using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct MoveToComponent : IComponentData
    {
        public int2 Position;
        public float TurnSpeed;
        public float MoveSpeed;
        public bool IsMoving;
        public float3 LastMoveDirection;
        public float2 LastStatePosition;
        public int FramesCount;
    }
}