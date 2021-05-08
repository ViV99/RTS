using Unity.Entities;
using Unity.Rendering;

namespace ECS.Components
{
    public struct MoveQueueInfoComponent : IComponentData
    {
        public int Index;
        public int Count;
    }
}
