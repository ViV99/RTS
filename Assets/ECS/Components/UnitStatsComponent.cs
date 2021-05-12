using Unity.Entities;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct UnitStatsComponent : IComponentData
    {
        public float MaxHealth;
        public float Health;
        public float Armor;
        public float TurnSpeed;
        public float MoveSpeed;
        public float Damage;
        public int ReloadTime;
        public int CurrentLoad;
    }
}