using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ECS.BlobAssets;
using ECS.Components;
using ECS.MonoBehaviours;
using ECS.Systems;
using ECS.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Math = System.Math;
using Random = Unity.Mathematics.Random;

namespace ECS.Other
{
    public static class Utilities
    {
        public const double EPS = 1e-10;
        
        public static float3 GetMouseWorldPosition()
        {
            var pos = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
            pos.z = 0;
            return pos;
        }

        private static float3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
        {
            return worldCamera.ScreenToWorldPoint(screenPosition);
        }

        public static float GetAngleFromVectorFloat(float3 dir)
        {
            return math.atan2(dir.y, dir.x);
        }
        
        public struct AStarHeapNode : IComparable<AStarHeapNode>
        {
            public float Score;
            public int2 Vertex;

            public AStarHeapNode(float score, int2 vertex)
            {
                Score = score;
                Vertex = vertex;
            }

            public int CompareTo(AStarHeapNode node)
            {
                var res = Score.CompareTo(node.Score);
                if (res != 0)
                    return res;
                if (Vertex.x < node.Vertex.x || Vertex.x == node.Vertex.x && Vertex.y < node.Vertex.y)
                    return -1;
                if (Vertex.x == node.Vertex.x && Vertex.y == node.Vertex.y)
                    return 0;
                return 1;
            }
        }
        // Оценка расстояния между двумя точками - evalFunc
        public static NativeList<int2> AStar(int2 start, int2 goal, float scale, int2x2 corners,
            BlobAssetReference<MovesBlobAsset> movesBlobAssetRef, DynamicBuffer<NavMeshElementComponent> navMesh)
        {
            const int maxIterCount = 1488;
            var iterCount = 0;
            var end = start;
            var minEval = float.MaxValue;
            // Прямой предок состояния на пути
            var p = new NativeHashMap<int2, int2>(1, Allocator.Temp);
            // Полученная глубина состояния
            var d = new NativeHashMap<int2, float>(1, Allocator.Temp) {{start, 0}};
            // Состояния на рассмотрении
            var online = new NativeHashMap<int2, float>(1, Allocator.Temp) {{start, math.distance(start, goal)}};
            // Рассмотренные состояния
            var used = new NativeHashSet<int2>(1, Allocator.Temp);
            //  "Очередь" перебора с сортировкой по минимуму счёта
            var q = new NativeBinaryHeap<AStarHeapNode>(Allocator.Temp);
            q.Insert(new AStarHeapNode(math.distance(start, goal), start));
            var rnd = new Random();
            rnd.InitState();
             while (q.Count != 0 && iterCount < maxIterCount)
             {
                 var node = q.ExtractMin();
                 if (used.Contains(node.Vertex))
                     continue;
                 used.Add(node.Vertex);
                 if (node.Vertex.Equals(goal))
                 {
                     end = goal;
                     break;
                 }
                 for (var i = 0; i < movesBlobAssetRef.Value.MoveArray.Length; i++)
                 {
                     // Применение хода и проверка на возможность этого хода
                     var u = node.Vertex + movesBlobAssetRef.Value.MoveArray[i].Delta;
                     var index = GetFlattenedIndex(u - corners.c0, corners.c1.x - corners.c0.x + 1);
                     if (!IsInRectangle(u, corners.c0, corners.c1) || navMesh[index].Distance < scale / 2) 
                         continue;
                     
                     // Эвристика против ПФа в препятствие
                     var evaluation = math.distance(u, goal);
                     if (minEval > evaluation)
                     {
                         minEval = evaluation;
                         end = u;
                     }
                     
                     // Оценка счёта
                     var tekCost = movesBlobAssetRef.Value.MoveArray[i].Cost;
                     //Х*йня
                     var randomState = rnd.NextInt(0, 2);
                     switch (randomState)
                     {
                         case 0:
                             tekCost /= 1.5f;
                             break;
                         case 1:
                             tekCost *= 1.25f;
                             break;
                     }
                     var newScore = d[node.Vertex] + tekCost + evaluation;
                     
                     // Постобработка, применение результатов
                     if (online.ContainsKey(u) && online[u] <= newScore)
                         continue;

                     p[u] = node.Vertex;
                     d[u] = d[node.Vertex] + tekCost;
                     online[u] = newScore;
                     q.Insert(new AStarHeapNode(newScore, u));
                 }
                 iterCount++;
             }
            var result = new NativeList<int2>(Allocator.Temp) {end};
            var tek = end;
            while (!tek.Equals(start))
            {
                tek = p[tek];
                result.Add(tek);
            }

            p.Dispose();
            d.Dispose();
            online.Dispose();
            q.Dispose();
            used.Dispose();
            return result;
        }

        public static int GetFlattenedIndex(int2 p, int sizeX)
        {
            //return sizeX * 24 * p.y + 24 * p.x + deltaNum;
            return sizeX * p.y + p.x;
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
            public float2 O => (A + C) / 2;

            public Rectanglef2(float2 centerPos, float scale, quaternion rotation)
            {
                var A = new float2(-scale / 2, -scale / 2);
                var B = new float2(-scale / 2, scale / 2);
                var C = new float2(scale / 2, scale / 2);
                var D = new float2(scale / 2, -scale / 2);
                var angle = GetRawAngle(rotation);
                var angleVector = new float2(math.sin(angle), math.cos(angle));
                this.A = centerPos + new float2(SkewProduct(A, angleVector), math.dot(A, angleVector));
                this.B = centerPos + new float2(SkewProduct(B, angleVector), math.dot(B, angleVector));
                this.C = centerPos + new float2(SkewProduct(C, angleVector), math.dot(C, angleVector));
                this.D = centerPos + new float2(SkewProduct(D, angleVector), math.dot(D, angleVector));
            }

            public Rectanglef2(float2 A, float2 B, float2 C, float2 D)
            {
                this.A = A;
                this.B = B;
                this.C = C;
                this.D = D;
            }

            public float GDFromPointToRectangle(float2 P)
            {
                var PO = new Line(P, O);
                var AB = new Line(A, B);
                var BC = new Line(B, C);
                var CD = new Line(C, D);
                var DA = new Line(D, A);

                var interAB = LinesIntersection(AB, PO);
                var interBC = LinesIntersection(BC, PO);
                var interCD = LinesIntersection(CD, PO);
                var interDA = LinesIntersection(DA, PO);
            
                if (IsInRectangle(interAB, A, B) && IsInRectangle(interAB, P, O))
                {
                    if (Sign(GODFromPointToLine(P, DA)) != Sign(GODFromPointToLine(P, new Line(C, B))))
                    {
                        return GDFromPointToLine(P, AB);
                    }

                    return math.min(math.distance(P, A), math.distance(P, B));
                }
            
                if (IsInRectangle(interBC, B, C) && IsInRectangle(interBC, P, O))
                {
                    if (Sign(GODFromPointToLine(P, AB)) != Sign(GODFromPointToLine(P, new Line(D, C))))
                    {
                        return GDFromPointToLine(P, BC);
                    }

                    return math.min(math.distance(P, B), math.distance(P, C));
                }
            
                if (IsInRectangle(interCD, C, D) && IsInRectangle(interCD, P, O))
                {
                    if (Sign(GODFromPointToLine(P, BC)) != Sign(GODFromPointToLine(P, new Line(A, D))))
                    {
                        return GDFromPointToLine(P, CD);
                    }

                    return math.min(math.distance(P, C), math.distance(P, D));
                }
            
                if (IsInRectangle(interDA, D, A) && IsInRectangle(interDA, P, O))
                {
                    if (Sign(GODFromPointToLine(P, CD)) != Sign(GODFromPointToLine(P, new Line(B, A))))
                    {
                        return GDFromPointToLine(P, DA);
                    }

                    return math.min(math.distance(P, D), math.distance(P, A));
                }
            
                return 0.0f;
            }
        }

        public static float2 LinesIntersection(Line l1, Line l2)
        {
            var product = SkewProduct(l1.V, l2.V);
            if (math.abs(product) < 1e-10)
                return new float2(float.PositiveInfinity, float.PositiveInfinity);
            var t1 = SkewProduct(l2.V, l1.P - l2.P) / product;
            return l1.V * t1 + l1.P;
        }

        public static float SkewProduct(float2 v1, float2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }

        public static bool IsInRectangle(float2 P, float2 A, float2 B)
        {
            return P.x >= math.min(A.x, B.x)
                   && P.x <= math.max(A.x, B.x)
                   && P.y >= math.min(A.y, B.y)
                   && P.y <= math.max(A.y, B.y);
        }

        public static float GODFromPointToLine(float2 P, Line l)
        {
            var a = l.V.y;
            var b = -l.V.x;
            var c = SkewProduct(l.V, l.P);
            return (a * P.x + b * P.y + c) / math.sqrt(a * a + b * b);
        }

        public static float GDFromPointToLine(float2 P, Line l)
        {
            return math.abs(GODFromPointToLine(P, l));
        }

        public static float GDFromPointToSegment(float2 P, float2 M, float2 N)
        {
            if (math.dot(P - M, N - M) > 0 && math.dot(P - N, M - N) > 0)
            {
                return GDFromPointToLine(P, new Line(M, N));
            }

            return math.min(math.distance(P, M), math.distance(P, N));
        }

        public static float GDFromSegmentToSegment(float2 A, float2 B, float2 C, float2 D)
        {
            var inter = LinesIntersection(new Line(A, B), new Line(C, D));
            if (IsInRectangle(inter, A, B) && IsInRectangle(inter, C, D))
                return 0;
            return math.min(
                math.min(GDFromPointToSegment(A, C, D), GDFromPointToSegment(B, C, D)), 
                math.min(GDFromPointToSegment(C, A, B), GDFromPointToSegment(D, A, B)));
        }

        public static float GDFromSegmentToRectangle(float2 M, float2 N, Rectanglef2 rectangle)
        {
            return math.min(
                math.min(
                    GDFromSegmentToSegment(M, N, rectangle.A, rectangle.B), 
                    GDFromSegmentToSegment(M, N, rectangle.B, rectangle.C)),
                math.min(
                    GDFromSegmentToSegment(M, N, rectangle.C, rectangle.D), 
                    GDFromSegmentToSegment(M, N, rectangle.D, rectangle.A)));
        }
        
        public static int Sign(float x)
        {
            return (x > 0 ? 1 : 0) - (x < 0 ? 1 : 0);
        }

        public static float GetRawAngle(quaternion q)
        {
            var sin = 2 * q.value.w * q.value.z;
            var cos = q.value.w * q.value.w - q.value.z * q.value.z;
            var a = math.asin(sin);
            var b = math.acos(cos);
            if (2 * q.value.w * q.value.z * (q.value.w * q.value.w - q.value.z * q.value.z) > 0)
                return a > 0 ? a : math.PI - a;
            else
                return a > 0 ? b : a;
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