using System;
using System.Collections.Generic;
using ECS.Other;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.MonoBehaviours
{
    public class MapHandler : MonoBehaviour
    {
        public static MapHandler Instance { get; private set; }
        
        [SerializeField]private Transform mapTransform;
        
        public Dictionary<int2, float> RoadMap { get; set; }

        public float2 BottomLeftCorner { get; set; }
        
        public float2 UpperRightCorner { get; set; }

        private void Awake()
        {
            Instance = this;
            RoadMap = new Dictionary<int2, float>();
            SetCorners();
        }

        private void SetCorners()
        {
            BottomLeftCorner = (Vector2)mapTransform.position;
            UpperRightCorner = (Vector2)(mapTransform.position + mapTransform.localScale);
        }
    }
}