using Unity.Entities;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct SelectedLabelReferenceComponent : IComponentData
    {
        public Entity SelectedLabelEntity;
    }
}