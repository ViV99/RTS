using Unity.Entities;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct UnitPrefabsComponent : IComponentData
    {
        public Entity ProjectilePrefab;
        public Entity HealthBarPrefab;
        public Entity SelectedLabelPrefab;
    }
}
