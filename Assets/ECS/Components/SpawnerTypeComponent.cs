using Unity.Entities;

namespace ECS.Components
{
    public enum SpawnerType
    {
        Shipyard,
        CounterShipyard
    }
    
    [GenerateAuthoringComponent]
    public struct SpawnerTypeComponent : IComponentData
    {
        public SpawnerType Type;
    }
}
