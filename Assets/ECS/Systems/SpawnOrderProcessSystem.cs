using ECS.Components;
using ECS.Tags;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace ECS.Systems
{
    [UpdateAfter(typeof(OrderQueueUpdateSystem))]
    public class SpawnOrderProcessSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }

        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override void OnUpdate()
        {
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            var statsGroup = GetComponentDataFromEntity<EntityStatsComponent>();
            Entities
                .WithAll<SpawnOrderTag>()
                .ForEach((Entity entity, int entityInQueryIndex,
                    DynamicBuffer<OrderQueueElementComponent> orderQueue, ref OrderQueueInfoComponent orderInfo, 
                    in Translation translation) =>
                {
                    var stats = statsGroup[entity];
                    switch (orderQueue[orderInfo.L].State)
                    {
                        case OrderState.Complete:
                            return;
                        case OrderState.New:
                            orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.InProgress);
                            stats.ReloadTime = statsGroup[orderQueue[orderInfo.L].Target].SpawnTime;
                            statsGroup[entity] = stats;
                            return;
                        case OrderState.InProgress:
                            stats.CurrentLoad++;
                            if (stats.CurrentLoad < stats.ReloadTime)
                            {
                                statsGroup[entity] = stats;
                                return;
                            }
                            stats.CurrentLoad = 0;
                            stats.ReloadTime = 0;
                            statsGroup[entity] = stats;
                            var spawned = parallelWriter.Instantiate(entityInQueryIndex, orderQueue[orderInfo.L].Target);
                            parallelWriter.AddBuffer<MoveQueueElementComponent>(entityInQueryIndex, spawned);
                            parallelWriter.AddBuffer<OrderQueueElementComponent>(entityInQueryIndex, spawned);
                            parallelWriter.AddComponent(entityInQueryIndex, spawned, ComponentType.ReadWrite<OrderQueueInfoComponent>());
                            parallelWriter.AddComponent(entityInQueryIndex, spawned, ComponentType.ReadWrite<MoveQueueInfoComponent>());
                            parallelWriter.AddComponent(entityInQueryIndex, spawned, ComponentType.ReadWrite<MoveToComponent>());
                            parallelWriter.SetComponent(entityInQueryIndex, spawned, new Translation
                            {
                                Value = translation.Value + new float3(
                                    statsGroup[orderQueue[orderInfo.L].Target].BaseRadius, 
                                    statsGroup[orderQueue[orderInfo.L].Target].BaseRadius, 
                                    0) 
                            });
                            orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.Complete);
                            return;
                    }
                }).Schedule();
        }
    }
}
