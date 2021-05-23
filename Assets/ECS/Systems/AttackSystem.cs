using ECS.Components;
using ECS.Tags;
using Unity.Entities;
using Unity.Mathematics;
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
            var translationGroup = GetComponentDataFromEntity<Translation>();
            Entities
                .ForEach((Entity entity, int entityInQueryIndex, ref AttackTargetComponent target, 
                    ref EntityStatsComponent stats, ref MoveToComponent moveTo, in UnitPrefabsComponent prefabs,
                    in Rotation rotation,in OwnerComponent owner) =>
                {
                    if (!translationGroup.HasComponent(target.Target))
                        return;
                    moveTo.LastMoveDirection = new float3(math.normalizesafe(
                        translationGroup[target.Target].Value.xy - translationGroup[entity].Value.xy), 0);
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
                    parallelWriter.SetComponent(entityInQueryIndex, projectile, translationGroup[entity]);
                    parallelWriter.SetComponent(entityInQueryIndex, projectile, rotation);
                    parallelWriter.AddComponent(entityInQueryIndex, projectile, new MoveToComponent
                    {
                        IsMoving = true,
                        Target = target.Target,
                        LastStatePosition = translationGroup[entity].Value.xy
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
