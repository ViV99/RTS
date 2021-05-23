using ECS.Components;
using ECS.Flags;
using ECS.Other;
using ECS.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

namespace ECS.Systems
{
    public class OnUpdateNavMeshSystem : SystemBase
    {
        protected override void OnStartRunning()
        {
            RaiseNavMeshUpdateFlag();
        }

        protected override void OnUpdate()
        {
            if (!TryGetSingletonEntity<NavMeshUpdateFlag>(out var flagEntity))
                return;
            EntityManager.DestroyEntity(flagEntity);
            var navMeshHandler = GetSingletonEntity<NavMeshInfoComponent>();
            var navMesh = GetBuffer<NavMeshElementComponent>(navMeshHandler);
            var info = GetComponent<NavMeshInfoComponent>(navMeshHandler);
            SolidBfs(navMesh, info);
            BuildingBfs(navMesh, info);
            CompleteDependency();
        }

        private void SolidBfs(DynamicBuffer<NavMeshElementComponent> navMesh, NavMeshInfoComponent info)
        {
            Entities
                .WithAll<SolidTag>()
                .ForEach((Entity entity, in Translation translation, in EntityStatsComponent stats, 
                    in Rotation rotation, in PhysicsCollider collider) =>
                {
                    var rectangle = new Utilities.Rectanglef2(translation.Value.xy, stats.BaseRadius * 2, rotation.Value);
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
                            
                            float dist;
                            if (collider.Value.Value.Type == ColliderType.Box)
                                dist = rectangle.GDFromPointToRectangle(u);
                            else
                                dist = math.distance(u, translation.Value.xy) - stats.BaseRadius;
                            
                            var index = Utilities.GetFlattenedIndex(u - info.Corners.c0, 
                                info.Corners.c1.x - info.Corners.c0.x + 1);
                            
                            if (!HasComponent<Translation>(navMesh[index].ClosestSolid) 
                                || navMesh[index].DistanceToSolid + Utilities.EPS > dist)
                            {
                                navMesh[index] = new NavMeshElementComponent
                                {
                                    DistanceToSolid = dist,
                                    ClosestSolid = entity,
                                    DistanceToBuilding = navMesh[index].DistanceToBuilding,
                                    ClosestBuilding = navMesh[index].ClosestBuilding
                                };
                                q.Add(u);
                                used.Add(u);
                            }
                        }
                    }
                    q.Dispose();
                    used.Dispose();
                }).Schedule();
        }
        
        private void BuildingBfs(DynamicBuffer<NavMeshElementComponent> navMesh, NavMeshInfoComponent info)
        {
            Entities
                .WithAll<BuildingTag>()
                .ForEach((Entity entity, in Translation translation, in EntityStatsComponent stats, 
                    in Rotation rotation, in OwnerComponent owner, in PhysicsCollider collider) =>
                {
                    if (owner.PlayerNumber == 2)
                        return;
                    var rectangle = new Utilities.Rectanglef2(translation.Value.xy, stats.BaseRadius * 2, rotation.Value);
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
                            
                            float dist;
                            if (collider.Value.Value.Type == ColliderType.Box)
                                dist = rectangle.GDFromPointToRectangle(u);
                            else
                                dist = math.distance(u, translation.Value.xy) - stats.BaseRadius;
                            
                            var index = Utilities.GetFlattenedIndex(u - info.Corners.c0, 
                                info.Corners.c1.x - info.Corners.c0.x + 1);
                            
                            if (!HasComponent<Translation>(navMesh[index].ClosestBuilding) 
                                || navMesh[index].DistanceToBuilding + Utilities.EPS > dist)
                            {
                                navMesh[index] = new NavMeshElementComponent
                                {
                                    DistanceToSolid = navMesh[index].DistanceToSolid,
                                    ClosestSolid = navMesh[index].ClosestSolid,
                                    DistanceToBuilding = dist,
                                    ClosestBuilding = entity
                                };
                                q.Add(u);
                                used.Add(u);
                            }
                        }
                    }
                    q.Dispose();
                    used.Dispose();
                }).Schedule();
        }

        public void RaiseNavMeshUpdateFlag()
        {
           EntityManager.CreateEntity(ComponentType.ReadWrite<NavMeshUpdateFlag>());
        }
    }
}