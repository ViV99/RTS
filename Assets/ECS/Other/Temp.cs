using Unity.Mathematics;

namespace ECS.Other
{
    public static class Temp
    {
        public static float SkewProduct(float2 v1, float2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }
    }
}