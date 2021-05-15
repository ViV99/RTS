using Unity.Entities;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct OwnerComponent : IComponentData
    {
        public int PlayerNumber;
    }
}
