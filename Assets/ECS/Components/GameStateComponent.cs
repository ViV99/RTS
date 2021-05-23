using Unity.Entities;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct GameStateComponent : IComponentData
    {
        public int MaxPop1;
        public int MaxPop2;
        public int Pop1;
        public int Pop2;
        public int Resources1;
        public int Resources2;
    }
}
