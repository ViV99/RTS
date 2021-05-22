using System.Collections.Generic;
using ECS.BlobAssets;
using ECS.Components;
using ECS.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Systems.Conversion
{
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    public class NavMeshHandlerInitSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            var movesBlobAssetReference = CreateMovesBlobAsset();
            var corners = GetMapCorners();
            var navMeshHandler = DstEntityManager
                .CreateEntityQuery(typeof(NavMeshInfoComponent))
                .GetSingletonEntity();
            DstEntityManager.AddComponentData(navMeshHandler, new NavMeshInfoComponent
            {
                Corners = corners,
                MovesBlobAssetRef = movesBlobAssetReference
            });
            var navMeshBuffer = DstEntityManager.AddBuffer<NavMeshElementComponent>(navMeshHandler);
            InitNavMeshBuffer(navMeshBuffer, corners);
        }

        private BlobAssetReference<MovesBlobAsset> CreateMovesBlobAsset()
        {
            var deltas = new List<int2>
            {
                new int2(0, -1), new int2(-1, 0), new int2(1, 0), new int2(0, 1),
                new int2(-2, -2), new int2(-1, -2), new int2(0, -2), new int2(1, -2), new int2(2, -2),
                new int2(-2, -1), new int2(-1, -1), new int2(1, -1), new int2(2, -1),
                new int2(-2, 0), new int2(2, 0),
                new int2(-2, 1), new int2(-1, 1), new int2(1, 1), new int2(2, 1),
                new int2(-2, 2), new int2(-1, 2), new int2(0, 2), new int2(1, 2), new int2(2, 2)
            };
            var costs = new List<float>
            {
                1.00f, 1.00f, 1.00f, 1.00f,
                2.78f, 2.23f, 1.99f, 2.23f, 2.78f,
                2.23f, 1.41f, 1.41f, 2.23f,
                1.99f, 1.99f,
                2.23f, 1.41f, 1.41f, 2.23f,
                2.78f, 2.23f, 1.99f, 2.23f, 2.78f
            };
            using var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var movesBlobAsset = ref blobBuilder.ConstructRoot<MovesBlobAsset>();
            var movesArray = blobBuilder.Allocate(ref movesBlobAsset.MoveArray, 24);
            for (var i = 0; i < 24; i++)
            {
                movesArray[i] = new Move {Delta = deltas[i], Cost = costs[i]};
            }
            return blobBuilder.CreateBlobAssetReference<MovesBlobAsset>(Allocator.Persistent);
        }

        private int2x2 GetMapCorners()
        {
            var mapEntity = DstEntityManager.CreateEntityQuery(typeof(MapTag)).GetSingletonEntity();
            var translation = DstEntityManager.GetComponentData<Translation>(mapEntity);
            var scale = DstEntityManager.GetComponentData<NonUniformScale>(mapEntity);
            var corners = new int2x2(
                (int2)(translation.Value.xy - scale.Value.x / 2), 
                (int2)(translation.Value.xy + scale.Value.x / 2));
            return corners;
        }
        
        private void InitNavMeshBuffer(DynamicBuffer<NavMeshElementComponent> navMeshBuffer, int2x2 corners)
        {
            navMeshBuffer.ResizeUninitialized((corners.c1.x - corners.c0.x + 1) * (corners.c1.y - corners.c0.y + 1));
            for (var i = 0; i < navMeshBuffer.Length; i++)
            {
                navMeshBuffer[i] = new NavMeshElementComponent
                {
                    DistanceToBuilding = float.MaxValue,
                    ClosestBuilding = Entity.Null,
                    DistanceToSolid = float.MaxValue,
                    ClosestSolid = Entity.Null,
                };
            }
        }
    }
}
