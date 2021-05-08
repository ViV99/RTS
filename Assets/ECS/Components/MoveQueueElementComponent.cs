using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    [InternalBufferCapacity(100)]
    public struct MoveQueueElementComponent : IBufferElementData
    {
        public int2 MovePosition;

        public MoveQueueElementComponent(int2 movePosition)
        {
            MovePosition = movePosition;
        }
    }
}
