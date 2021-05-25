using ECS.Components;
using ECS.Tags;
using Unity.Entities;
using UnityEngine;

namespace ECS.Systems
{
    public class ExtractionProcessSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem Ecb { get; set; }

        private int mainExtractionReload;
        protected override void OnCreate()
        {
            Ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            mainExtractionReload = 180;
        }
        
        protected override void OnUpdate()
        {
            mainExtractionReload--;
            var gameStateHandler = GetSingletonEntity<GameStateComponent>();
            if (mainExtractionReload <= 0)
            {
                mainExtractionReload = 180;
                var gameState = GetComponent<GameStateComponent>(gameStateHandler);
                gameState.Resources1 += 25;
                gameState.Resources2 += 50;
                SetComponent(gameStateHandler, gameState);
            }
            var parallelWriter = Ecb.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithAll<BuildingTag>()
                .ForEach((Entity entity, int entityInQueryIndex, ref EntityStatsComponent stats, 
                    in EntityTypeComponent type, in OwnerComponent owner) =>
                {
                    if (type.Type != EntityType.Extractor)
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
