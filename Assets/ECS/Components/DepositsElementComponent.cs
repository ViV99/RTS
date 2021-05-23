using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    public struct DepositsElementComponent : IBufferElementData
    {
        public int2 Position;
        public bool IsAvailable;
    }
}
