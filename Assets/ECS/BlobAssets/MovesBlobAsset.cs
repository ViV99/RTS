using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.BlobAssets
{
    public struct Move
    {
        public int2 Delta;
        public float Cost;
    }
    
    public struct MovesBlobAsset
    {
        public BlobArray<Move> MoveArray;
    }
}