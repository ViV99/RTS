using ECS.Systems;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    public enum OrderState
    {
        New,
        InProgress,
        Complete,
    }
    
    public enum OrderType
    {
        Move,
        Attack,
    }
    
    [InternalBufferCapacity(30)]
    public struct OrderQueueElementComponent : IBufferElementData
    {
        public OrderType Type;
        public OrderState State;
        public int2 MovePosition;
        public Entity Target;

        public OrderQueueElementComponent(OrderType type, OrderState state, int2 movePosition) 
            : this(type, state, movePosition, Entity.Null)
        {
        }
        
        public OrderQueueElementComponent(OrderType type, OrderState state, int2 movePosition, Entity target)
        {
            Type = type;
            State = state;
            MovePosition = movePosition;
            Target = target;
        }

        public OrderQueueElementComponent WithState(OrderState state)
        {
            return new OrderQueueElementComponent(Type, state, MovePosition, Target);
        }
        
        public OrderQueueElementComponent WithMovePosition(int2 movePosition)
        {
            return new OrderQueueElementComponent(Type, State, movePosition, Target);
        }
    }
}