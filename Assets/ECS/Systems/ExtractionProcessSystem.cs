using ECS.Components;
using ECS.Tags;
using Unity.Entities;
using UnityEngine;

namespace ECS.Systems
{
    public class ExtractionProcessSystem : SystemBase
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
            Entities
                .WithAll<BuildingTag>()
                .ForEach((Entity entity, int entityInQueryIndex, ref EntityStatsComponent stats, 
                    in BuildingTypeComponent type, in OwnerComponent owner) =>
                {
                    if (type.Type != BuildingType.Extractor)
                        return;
                    stats.CurrentLoad++;
                    if (stats.ReloadTime > stats.CurrentLoad) 
                        return;
                    stats.CurrentLoad = 0;
                    var gameState = GetComponent<GameStateComponent>(gameStateHandler);
                    if (owner.PlayerNumber == 1)
                        gameState.Resources1 += (int)stats.Damage;
                    else
                        gameState.Resources2 += (int)stats.Damage;

                    parallelWriter.SetComponent(entityInQueryIndex, gameStateHandler, gameState);
                }).Schedule();
            Ecb.AddJobHandleForProducer(Dependency);
        }
    }
}
