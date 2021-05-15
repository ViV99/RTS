// using ECS.Components;
// using ECS.Tags;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Physics.Systems;
// using Unity.Transforms;
//
// namespace ECS.Systems
// {
//     [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//     [UpdateAfter(typeof(EndFramePhysicsSystem))]
//     public class HitTrackingSystem : JobComponentSystem
//     {
//         private BeginSimulationEntityCommandBufferSystem Ecb { get; set; }
//         private BuildPhysicsWorld buildPhysicsWorld;
//         private StepPhysicsWorld stepPhysicsWorld;
//     
//         protected override void OnCreate()
//         {
//             Ecb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
//             buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
//             stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
//         }
//     
//         //[BurstCompile]
//         private struct HitTrackingSystemJob : ICollisionEventsJob
//         {
//             [ReadOnly] public ComponentDataFromEntity<OwnerComponent> OwnerEntityGroup;
//             [ReadOnly] public ComponentDataFromEntity<ProjectileTag> ProjectileEntityGroup;
//     
//             public ComponentDataFromEntity<EntityStatsComponent> StatsGroup;
//     
//             public EntityCommandBuffer CommandBuffer;
//             
//             public void Execute(CollisionEvent collisionEvent)
//             {
//                 var entityA = collisionEvent.EntityA;
//                 var entityB = collisionEvent.EntityB;
//                 var isProjectileA = ProjectileEntityGroup.HasComponent(entityA);
//                 var isProjectileB = ProjectileEntityGroup.HasComponent(entityB);
//                 if (isProjectileA == isProjectileB)
//                     return;
//     
//                 if (!isProjectileA)
//                 {
//                     var e = entityA;
//                     entityA = entityB;
//                     entityB = e;
//                 }
//     
//                 if (OwnerEntityGroup.HasComponent(entityB))
//                 {
//                     if (OwnerEntityGroup[entityB].PlayerNumber != OwnerEntityGroup[entityA].PlayerNumber)
//                     {
//                         var statsB = StatsGroup[entityB];
//                         statsB.CurrentHealth -= math.max(StatsGroup[entityA].Damage - statsB.Armor, 1);
//                         StatsGroup[entityB] = statsB;
//                     }
//                     else
//                     {
//                         return;
//                     }
//                 }
//                 
//                 if (StatsGroup[entityB].CurrentHealth <= 0)
//                 {
//                     CommandBuffer.DestroyEntity(entityB);
//                 }
//                 CommandBuffer.DestroyEntity(entityA);
//             }
//         }
//         
//         
//         protected override JobHandle OnUpdate(JobHandle inputDeps)
//         {
//             var job = new HitTrackingSystemJob
//             {
//                 CommandBuffer = Ecb.CreateCommandBuffer(),
//                 StatsGroup = GetComponentDataFromEntity<EntityStatsComponent>(),
//                 OwnerEntityGroup = GetComponentDataFromEntity<OwnerComponent>(true),
//                 ProjectileEntityGroup = GetComponentDataFromEntity<ProjectileTag>(true)
//             };
//     
//             var jobHandle = job.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, inputDeps);
//             jobHandle.Complete();
//             return jobHandle;
//         }
//     }
