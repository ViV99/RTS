using Unity.Collections;
using Unity.Entities;

namespace ECS.Components
{
    public struct NavMeshElementComponent : IBufferElementData
    {
        public float Distance;
    }
}
