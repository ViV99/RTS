using Unity.Entities;

namespace ECS.Components
{
    public struct OrderQueueInfoComponent : IComponentData
    {
        public int L;
        public int R;
        public int Count;
    }
}
