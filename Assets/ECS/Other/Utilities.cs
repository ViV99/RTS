using Unity.Mathematics;
using UnityEngine;

namespace ECS.Other
{
    public static class Utilities
    {
        public static float3 GetMouseWorldPosition()
        {
            var vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
            vec.z = 0f;
            return vec;
        }

        private static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
        {
            var worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            return worldPosition;
        }
        
        public static float GetAngleFromVectorFloat(float3 dir) {
            var n = Mathf.Atan2(dir.y, dir.x);

            return n;
        }
    }
}