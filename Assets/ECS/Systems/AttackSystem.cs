using ECS.Components;
using ECS.Tags;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace ECS.Systems
{
    public class AttackSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            Entities
                .ForEach((Entity entity, int entityInQueryIndex, ref AttackTargetComponent target, 
                    ref EntityStatsComponent stats, in UnitPrefabsComponent prefabs, in Translation translation,
                    in OwnerComponent owner) =>
                {
                    stats.CurrentLoad++;
                    if (stats.ReloadTime > stats.CurrentLoad) 
                        return;
                    stats.CurrentLoad = 0;
                    
                    var projectile = parallelWriter.Instantiate(entityInQueryIndex, prefabs.ProjectilePrefab);
                    parallelWriter.SetComponent(entityInQueryIndex, projectile, new EntityStatsComponent
                    {
                        MoveSpeed = stats.ProjectileSpeed,
                        TurnSpeed = 1,
                        Damage = stats.Damage,
                        AttackRange = stats.AttackRange
                    });
                    parallelWriter.SetComponent(entityInQueryIndex, projectile, translation);
                    parallelWriter.AddComponent(entityInQueryIndex, projectile, new MoveToComponent
                    {
                        IsMoving = true,
                        Target = target.Target,
                        LastStatePosition = translation.Value.xy
                    });
                    parallelWriter.AddComponent(entityInQueryIndex, projectile, ComponentType.ReadWrite<ProjectileTag>());
                    parallelWriter.SetComponent(entityInQueryIndex, projectile, new OwnerComponent
                    {
                        PlayerNumber = owner.PlayerNumber
                    });
                }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
        }
    }
}
