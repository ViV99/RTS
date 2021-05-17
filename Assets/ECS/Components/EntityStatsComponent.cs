using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    [GenerateAuthoringComponent]
    public struct EntityStatsComponent : IComponentData
    {
        public float MaxHealth;
        public float CurrentHealth;
        public float Armor;
        public float TurnSpeed;
        public float MoveSpeed;
        public float Damage;
        public float ProjectileSpeed;
        public int ReloadTime;
        public int CurrentLoad;
        public float AttackRange;
        public float SightRange;

        public EntityStatsComponent WithHealth(float health)
        {
            return new EntityStatsComponent
            {
                MaxHealth = MaxHealth,
                CurrentHealth = health,
                Armor = Armor,
                TurnSpeed = TurnSpeed,
                MoveSpeed = MoveSpeed,
                Damage = Damage,
                ProjectileSpeed = ProjectileSpeed,
                ReloadTime = ReloadTime,
                CurrentLoad = CurrentLoad,
                AttackRange = AttackRange,
                SightRange = SightRange
            };
        }
    }
}