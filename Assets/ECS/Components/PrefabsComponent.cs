using Unity.Entities;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct PrefabsComponent : IComponentData
    {
        public Entity SimplePlayer1UnitPrefab;
        public Entity SimplePlayer2UnitPrefab;
        public Entity SimpleSolidPrefab;
        public Entity SimpleHealthPrefab;
    }
}
