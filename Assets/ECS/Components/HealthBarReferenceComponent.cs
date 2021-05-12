using Unity.Entities;

namespace ECS.Components
{
    public struct HealthBarReferenceComponent : IComponentData
    {
        public Entity HealthBarEntity;
    }
}
