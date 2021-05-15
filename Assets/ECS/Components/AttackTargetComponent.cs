using Unity.Entities;

namespace ECS.Components
{
    public struct AttackTargetComponent : IComponentData
    {
        public Entity Target;
    }
}
