using Unity.Entities;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct PrefabsComponent : IComponentData
    {
        public Entity SimpleUnitPrefab;
        public Entity SimpleSolidPrefab;
        public Entity SimpleHealthPrefab;
    }
}
