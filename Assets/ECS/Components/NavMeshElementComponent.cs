using Unity.Collections;
using Unity.Entities;

namespace ECS.Components
{
    public struct NavMeshElementComponent : IBufferElementData
    {
        public float DistanceToSolid;
        public Entity ClosestSolid;
        public float DistanceToBuilding;
        public Entity ClosestBuilding;
    }
}
