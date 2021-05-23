using Unity.Entities;

namespace ECS.Components
{
    public enum BuildingType
    {
        Shipyard,
        CounterShipyard,
        Extractor,
        HQ,
        ListeningPost
    }
    
    [GenerateAuthoringComponent]
    public struct BuildingTypeComponent : IComponentData
    {
        public BuildingType Type;
    }
}
