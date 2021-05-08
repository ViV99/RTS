using System.Diagnostics;
using ECS.Components;
using ECS.Flags;
using ECS.Other;
using ECS.Tags;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ECS.Systems
{
    public class OnUpdateNavMeshSystem : SystemBase
    {
        protected override void OnStartRunning()
        {
            RaiseRoadMapUpdateFlag();
        }

        protected override void OnUpdate()
        {
            if (!TryGetSingletonEntity<NavMeshUpdateFlag>(out var flagEntity))
                return;

            EntityManager.DestroyEntity(flagEntity);
            
            var navMeshHandler = GetSingletonEntity<NavMeshInfoComponent>();
            var navMesh = GetBuffer<NavMeshElementComponent>(navMeshHandler);
            var info = GetComponent<NavMeshInfoComponent>(navMeshHandler);
            Entities
                .WithAll<SolidTag>()
                .ForEach((in Translation translation, in CompositeScale scale, in Rotation rotation) =>
                {
                    var rectangle = new Utilities.Rectanglef2(translation.Value.xy, scale.Value.c0.x, rotation.Value);
                    var used = new NativeHashSet<int2>(1, Allocator.Temp) {Utilities.GetRoundedPoint(rectangle.O)};
                    var q = new NativeList<int2>(Allocator.Temp) {Utilities.GetRoundedPoint(rectangle.O)};
                    var l = 0;
                    while (l != q.Length)
                    {
                        var v = q[l++];
                        for (var i = 0; i < 4; i++)
                        {
                            var u = v + info.MovesBlobAssetRef.Value.MoveArray[i].Delta;
                            if (used.Contains(u) || !Utilities.IsInRectangle(u, info.Corners.c0, info.Corners.c1))
                                continue;
                            
                            var dist = rectangle.GDFromPointToRectangle(u);
                            
                            var index = Utilities.GetFlattenedIndex(u - info.Corners.c0, info.Corners.c1.x - info.Corners.c0.x + 1);
                            
                            if (navMesh[index].Distance > dist)
                            {
                                navMesh[index] = new NavMeshElementComponent {Distance = dist};
                                q.Add(u);
                                used.Add(u);
                            }
                        }
                    }
                    q.Dispose();
                    used.Dispose();
                }).Schedule();
            CompleteDependency();
        }

        private void RaiseRoadMapUpdateFlag()
        {
           EntityManager.CreateEntity(ComponentType.ReadWrite<NavMeshUpdateFlag>());
        }
    }
}