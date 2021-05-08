using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;

namespace ECS.Systems
{
    public class TestSpawn : SystemBase
    {
        private int cnt = 0;
        
        protected override void OnUpdate()
        {
            if (cnt > 0)
                return;
            var rnd = new Random();
            rnd.InitState();
            for (var i = 0; i < 400; i++)
            {
                var e = GetSingleton<PrefabsComponent>().SimpleUnitPrefab;
                var eCopy = EntityManager.Instantiate(e);
                EntityManager.AddBuffer<MoveQueueElementComponent>(eCopy);
                EntityManager.AddBuffer<OrderQueueElementComponent>(eCopy);
                EntityManager.AddComponentData(eCopy, new OrderQueueInfoComponent {L = 0, R = 0, Count = 0});
                EntityManager.AddComponentData(eCopy, new MoveQueueInfoComponent {Index = 0, Count = 0});
            }
            cnt++;
        }
    }
}
