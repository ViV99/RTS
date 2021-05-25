using Unity.Entities;

namespace ECS.Components
{
    public enum EntityType
    {
        Fighter,
        Battleship,
        DestroyerAA,
        TorpedoCruiser,
        Juggernaut,
        Shipyard,
        CounterShipyard,
        Extractor,
        HQ,
        ListeningPost
    }
    
    [GenerateAuthoringComponent]
    public struct EntityTypeComponent : IComponentData
    {
        public EntityType Type;
    }
}
