using ECS.Components;
using ECS.Other;
using ECS.Tags;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Material = UnityEngine.Material;

namespace ECS.Systems
{
    public class SelectLabelSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }
        public Material material;

        protected Mesh CreateMesh(float width, float height)
        {
            var midWidth = width / 2f;
            var midHeight = height / 2f;
            var verticies = new Vector3[4];
            var uv = new Vector2[4];
            var triangles = new int[6];

            verticies[0] = new Vector3(-midWidth, midWidth);
            verticies[1] = new Vector3(midWidth, midWidth);
            verticies[2] = new Vector3(-midWidth, -midWidth);
            verticies[3] = new Vector3(midWidth, -midWidth);
            
            uv[0] = new Vector2(0, 1);
            uv[1] = new Vector2(1, 1);
            uv[2] = new Vector2(0, 0);
            uv[3] = new Vector2(1, 0);
            
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 1;
            triangles[5] = 3;
            
            var mesh = new Mesh();
            mesh.vertices = verticies;
            mesh.uv = uv;
            mesh.triangles = triangles;
            return mesh;
        }

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            var manager = EntityManager;
            Entities.ForEach((
                Entity entity,
                int entityInQueryIndex,
                ref Translation translation,
                ref CompositeScale compositeScale,
                ref EntityStatsComponent stats, 
                ref UnitPrefabsComponent unitPrefabs) =>
            {
                if (unitPrefabs.SelectedLabelPrefab != Entity.Null)
                {
                    parallelWriter.SetComponent(entityInQueryIndex, unitPrefabs.SelectedLabelPrefab, translation);
                }
            }).WithoutBurst().Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
            CompleteDependency();
        }

    }
}
