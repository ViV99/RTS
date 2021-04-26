using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ECS.MonoBehaviours;
using ECS.Other;
using ECS.Tags;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    public class OnUpdateRoadMapSystem : SystemBase
    {
        private static readonly List<int2> Moves = new List<int2>
        {
            new int2(0, -1), new int2(-1, 0), new int2(1, 0),  new int2(0, 1)
        };
        
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .ForEach((in RoadMapUpdateTag roadMapUpdateTag) =>
            {
                Entities
                    .WithoutBurst()
                    .WithAll<SolidTag>()
                    .ForEach((in Translation translation, in CompositeScale scale, in Rotation rotation) =>
                    {
                        var rectangle = new Utilities.Rectanglef2(translation, scale, rotation);
                        UpdateRoadMapBfs(rectangle);
                    }).Schedule();
            }).Run();
        }

        private static void UpdateRoadMapBfs(Utilities.Rectanglef2 rectangle)
        {
            var q = new Queue<int2>();
            q.Enqueue(Utilities.GetRoundedPoint(rectangle.O));
            while (q.Any())
            {
                var v = q.Dequeue();
                foreach (var move in Moves)
                {
                    var u = v + move;
        
                    if (!Utilities.IsInRectangle(u, MapHandler.Instance.UpperRightCorner, MapHandler.Instance.BottomLeftCorner))
                        continue;
                    
                    var distance = rectangle.GetDistanceToRectangle(u);
                    if (!MapHandler.Instance.RoadMap.ContainsKey(u) || MapHandler.Instance.RoadMap[u] > distance)
                    {
                        MapHandler.Instance.RoadMap[u] = distance;
                        q.Enqueue(u); 
                    }
                }
            }
        }
    }
}