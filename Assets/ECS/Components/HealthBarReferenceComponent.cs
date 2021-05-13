using Unity.Entities;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct HealthBarReferenceComponent : IComponentData
    {
        public Entity HealthBarEntity;
    }
}
