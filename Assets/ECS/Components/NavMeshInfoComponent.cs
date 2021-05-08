using ECS.BlobAssets;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct NavMeshInfoComponent : IComponentData
    {
        public int2x2 Corners;
        public BlobAssetReference<MovesBlobAsset> MovesBlobAssetRef;
    }
}