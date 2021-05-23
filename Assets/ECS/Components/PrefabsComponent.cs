using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct PrefabsComponent : IComponentData
    {
        public Entity FighterPrefab1;
        public Entity BattleshipPrefab1;
        public Entity DestroyerAAPrefab1;
        public Entity TorpedoCruiserPrefab1;
        public Entity JuggernautPrefab1;
        
        public Entity FighterPrefab2;
        public Entity BattleshipPrefab2;
        public Entity DestroyerAAPrefab2;
        public Entity TorpedoCruiserPrefab2;
        public Entity JuggernautPrefab2;

        public Entity HQ1;
        public Entity ListeningPost1;
        public Entity Shipyard1;
        public Entity CounterShipyard1;
        public Entity Extractor1;
        
        public Entity HQ2;
        public Entity Shipyard2;
        public Entity CounterShipyard2;
        public Entity Extractor2;
        
        public Entity SimplePlayer2UnitPrefab;
        public Entity SimpleSolidPrefab;
        
        public Entity HealthBarPrefab;
        public Entity SelectedLabelPrefab;
    }
}
