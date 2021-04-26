using System;
using System.Collections.Generic;
using System.Linq;
using ECS.Components;
using ECS.MonoBehaviours;
using ECS.Tags;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Other
{
    public static class Utilities
    {
        public const double EPS = 1e-10;
        
        public static float3 GetMouseWorldPosition()
        {
            var vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
            vec.z = 0f;
            return vec;
        }

        private static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
        {
            return worldCamera.ScreenToWorldPoint(screenPosition);
        }

        public static float GetAngleFromVectorFloat(float3 dir)
        {
            return math.atan2(dir.y, dir.x);
        }


        private static readonly List<(int dx, int dy, float cost)> Moves = new List<(int dx, int dy, float cost)>
        {
            (-2, -2, 2.78f), (-1, -2, 2.23f), (0, -2, 1.99f), (1, -2, 2.23f), (2, -2, 2.78f),
            (-2, -1, 2.23f), (-1, -1, 1.41f), (0, -1, 1.00f), (1, -1, 1.41f), (2, -1, 2.23f),
            (-2, 0, 1.99f), (-1, 0, 1.00f), (1, 0, 1.00f), (2, 0, 1.99f),
            (-2, 1, 2.23f), (-1, 1, 1.41f), (0, 1, 1.00f), (1, 1, 1.41f), (2, 1, 2.23f),
            (-2, 2, 2.78f), (-1, 2, 2.23f), (0, 2, 1.99f), (1, 2, 2.23f), (2, 2, 2.78f)
        };

        // Оценка расстояния между двумя точками - evalFunc
        public static LinkedList<int2> AStar(
            int2 start,
            int2 goal,
            Func<int2, int2, float> evalFunc, 
            in CompositeScale scale)
        {
            const int maxIterCount = 100500;
            var iterCount = 0;
            var end = start;
            var minEval = float.MaxValue;
            
            // Прямой предок состояния на пути 
            var p = new Dictionary<int2, int2>();
            // Полученная глубина состояния
            var d = new Dictionary<int2, float> {{start, 0}};
            // Состояния на рассмотрении
            var online = new Dictionary<int2, float> {{start, math.distance(start, goal)}};
            // "Очередь" перебора с сортировкой по минимуму счёта
            var q = new SortedSet<(float, int2)> {(evalFunc(start, goal), start)};

            while (q.Any() && iterCount < maxIterCount)
            {
                var v = q.First();
                if (v.Item2.Equals(goal))
                {
                    end = goal;
                    break;
                }

                q.Remove(v);
                foreach (var (dx, dy, cost) in Moves)
                {
                    // Применение хода и проверка на возможность этого хода
                    var u = v.Item2 + new int2(dx, dy);
                    if (MapHandler.Instance.RoadMap[u] < scale.Value.c0.x / 2)
                        continue;
                    
                    //Эвристииииииииииииииическая хуета
                    var evaluation = evalFunc(u, goal);
                    if (minEval > evaluation)
                    {
                        minEval = evaluation;
                        end = u;
                    }
                    // Оценка счёта
                    var newScore = d[v.Item2] + cost + evaluation;
                    // Постобработка, применение результатов
                    if (online.ContainsKey(u) && online[u] <= newScore)
                        continue;
                    if (online.ContainsKey(u))
                    {
                        q.Remove((online[u], u));
                    }

                    p[u] = v.Item2;
                    d[u] = d[v.Item2] + cost;
                    online[u] = newScore;
                    q.Add((newScore, u));
                }

                iterCount++;
            }
            
            var result = new LinkedList<int2>();
            var tek = end;
            while (!tek.Equals(start))
            {
                result.AddFirst(tek);
                tek = p[tek];
            }

            
            return result;
        }

        public struct Line
        {
            public float2 V;
            public float2 P;

            public Line(float2 point1, float2 point2)
            {
                V = point1 - point2;
                P = point1;
            }
        }
        
        public struct Rectanglef2
        {
            public float2 A;
            public float2 B;
            public float2 C;
            public float2 D;
            public float2 O => (C + A) / 2;

            public Rectanglef2(in Translation translation, in CompositeScale scale, in Rotation rotation)
            {
                var A = translation.Value.xy + new float2(-scale.Value.c0.x / 2, -scale.Value.c1.y / 2);
                var B = translation.Value.xy + new float2(-scale.Value.c0.x / 2, scale.Value.c1.y / 2);
                var C = translation.Value.xy + new float2(scale.Value.c0.x / 2, scale.Value.c1.y / 2);
                var D = translation.Value.xy + new float2(scale.Value.c0.x / 2, -scale.Value.c1.y / 2);
                var angle = GetRawAngle(rotation.Value);
                var angleVector = new float2(math.sin(angle), math.cos(angle));
                this.A = new float2(SkewProduct(A, angleVector), math.dot(A, angleVector));
                this.B = new float2(SkewProduct(B, angleVector), math.dot(B, angleVector));
                this.C = new float2(SkewProduct(C, angleVector), math.dot(C, angleVector));
                this.D = new float2(SkewProduct(D, angleVector), math.dot(D, angleVector));
            }

            public Rectanglef2(float2 A, float2 B, float2 C, float2 D)
            {
                this.A = A;
                this.B = B;
                this.C = C;
                this.D = D;
            }

            public float GetDistanceToRectangle(float2 P)
            {
                var PO = new Line(P, O);
                var AB = new Line(A, B);
                var BC = new Line(B, C);
                var CD = new Line(C, D);
                var DA = new Line(D, A);
            
                if (IsInRectangle(LinesIntersection(AB, PO), A, B))
                {
                    if (Sign(GetOrientedDistance(P, DA)) != Sign(GetOrientedDistance(P, new Line(C, B))))
                    {
                        return GetDistance(P, AB);
                    }

                    return math.min(math.distance(P, A), math.distance(P, B));
                }
            
                if (IsInRectangle(LinesIntersection(BC, PO), B, C))
                {
                    if (Sign(GetOrientedDistance(P, AB)) != Sign(GetOrientedDistance(P, new Line(D, C))))
                    {
                        return GetDistance(P, BC);
                    }

                    return math.min(math.distance(P, B), math.distance(P, C));
                }
            
                if (IsInRectangle(LinesIntersection(CD, PO), C, D))
                {
                    if (Sign(GetOrientedDistance(P, BC)) != Sign(GetOrientedDistance(P, new Line(A, D))))
                    {
                        return GetDistance(P, CD);
                    }

                    return math.min(math.distance(P, C), math.distance(P, D));
                }
            
                if (IsInRectangle(LinesIntersection(DA, PO), D, A))
                {
                    if (Sign(GetOrientedDistance(P, CD)) != Sign(GetOrientedDistance(P, new Line(B, A))))
                    {
                        return GetDistance(P, DA);
                    }

                    return math.min(math.distance(P, D), math.distance(P, A));
                }
            
                return 0.0f;
            }
        }

        public static float2 LinesIntersection(Line l1, Line l2)
        {
            var product = SkewProduct(l1.V, l2.V);
            if (product < 1e-10)
                return new float2(float.PositiveInfinity, float.PositiveInfinity);
            var t1 = SkewProduct(l2.V, l1.P - l2.P) / product;
            return l1.V * t1 + l1.P;
        }

        public static float SkewProduct(float2 v1, float2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }

        public static bool IsInRectangle(float2 p, float2 a, float2 b)
        {
            return p.x >= math.min(a.x, b.x)
                   && p.x <= math.max(a.x, b.x)
                   && p.y >= math.min(a.y, b.y)
                   && p.y <= math.max(a.y, b.y);
        }

        public static float GetOrientedDistance(float2 p, Line l)
        {
            var a = l.V.y;
            var b = -l.V.x;
            var c = SkewProduct(l.V, l.P);
            return (a * p.x + b * p.y + c) / math.sqrt(a * a + b * b);
        }

        public static float GetDistance(float2 p, Line l)
        {
            return math.abs(GetOrientedDistance(p, l));
        }
        
        public static int Sign(float x)
        {
            return (x > 0 ? 1 : 0) - (x < 0 ? 1 : 0);
        }

        public static float GetRawAngle(quaternion q)
        {
            var angle = 2 * math.acos(q.value.w);
            if (q.value.z * q.value.w < 0)
            {
                angle += 360;
            }

            return angle;
        }

        public static int2 GetRoundedPoint(float2 point)
        {
            var res = new int2();
            if (point.x - math.floor(point.x) < 0.5)
                res.x = (int) (math.floor(point.x) + EPS);
            else
                res.x = (int) (math.ceil(point.x) + EPS);
            if (point.y - math.floor(point.y) < 0.5)
                res.y = (int) (math.floor(point.y) + EPS);
            else
                res.y = (int) (math.ceil(point.y) + EPS);
            return res;
        }
    }
}