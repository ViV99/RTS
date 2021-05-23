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
            var gameStateHandler = GetSingletonEntity<GameStateComponent>();
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            var statsGroup = GetComponentDataFromEntity<EntityStatsComponent>();
            Entities
                .WithAll<SpawnOrderTag>()
                .ForEach((Entity entity, int entityInQueryIndex,
                    DynamicBuffer<OrderQueueElementComponent> orderQueue, ref OrderQueueInfoComponent orderInfo, 
                    in Translation translation, in OwnerComponent owner) =>
                {
                    var stats = statsGroup[entity];
                    switch (orderQueue[orderInfo.L].State)
                    {
                        case OrderState.Complete:
                            return;
                        case OrderState.New:
                            var gameState = GetComponent<GameStateComponent>(gameStateHandler);
                            if (owner.PlayerNumber == 1)
                            {
                                if (gameState.Pop1 + statsGroup[orderQueue[orderInfo.L].Target].Pop <= gameState.MaxPop1 
                                    && statsGroup[orderQueue[orderInfo.L].Target].SpawnCost <= gameState.Resources1)
                                {
                                    gameState.Pop1 += statsGroup[orderQueue[orderInfo.L].Target].Pop;
                                    gameState.Resources1 -= statsGroup[orderQueue[orderInfo.L].Target].SpawnCost;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else
                            {
                                if (gameState.Pop2 + statsGroup[orderQueue[orderInfo.L].Target].Pop <= gameState.MaxPop2
                                    && statsGroup[orderQueue[orderInfo.L].Target].SpawnCost <= gameState.Resources2)
                                {
                                    gameState.Pop2 += statsGroup[orderQueue[orderInfo.L].Target].Pop;
                                    gameState.Resources2 -= statsGroup[orderQueue[orderInfo.L].Target].SpawnCost;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            parallelWriter.SetComponent(entityInQueryIndex, gameStateHandler, gameState);
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
                                    GetComponent<Translation>(orderQueue[orderInfo.L].Target).Value.z) 
                            });
                            // TODO: BFS
                            orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.Complete);
                            return;
                    }
                }).Schedule();
        }
    }
}
