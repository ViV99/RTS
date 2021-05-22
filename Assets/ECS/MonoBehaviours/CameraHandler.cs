using System;
using System.Collections.Generic;
using ECS.Components;
using ECS.Other;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.MonoBehaviours
{
    public class CameraHandler : MonoBehaviour
    {
        private float CameraMoveSpeed = 80f;
        private float CameraZoomSpeed;
        public static CameraHandler Instance { get; private set; }

        [SerializeField] private CameraFollow cameraFollow;
        private float3 CameraFollowPosition { get; set; }
        private float CameraFollowZoom { get; set; }

        public Transform selectionAreaTransform;

        public EntityManager EntityManager { get; set; }

        private void Awake()
        {
            Instance = this;
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Start()
        {
            CameraFollowZoom = 80f;
            cameraFollow.Setup(() => CameraFollowPosition, () => CameraFollowZoom, true, true);
        }

        
        private void Update()
        {
            HandleCamera();
        }

        private void HandleCamera()
        {
            var moveDir = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                moveDir.y = +1f;
            }

            if (Input.GetKey(KeyCode.S))
            {
                moveDir.y = -1f;
            }

            if (Input.GetKey(KeyCode.A))
            {
                moveDir.x = -1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                moveDir.x = +1f;
            }

            moveDir = moveDir.normalized;
            CameraFollowPosition += (float3)moveDir * CameraMoveSpeed * Time.deltaTime;
            
            var mapCorners = EntityManager
                .CreateEntityQuery(ComponentType.ReadOnly<NavMeshInfoComponent>())
                .GetSingleton<NavMeshInfoComponent>().Corners;
            CameraFollowPosition = new float3(
                math.clamp(CameraFollowPosition.xy, mapCorners.c0, mapCorners.c1),
                0);
            if (Input.mouseScrollDelta.y > 0)
            {
                CameraFollowZoom -= CameraZoomSpeed * Time.deltaTime;
            }
            else if (Input.mouseScrollDelta.y < 0)
            {
                CameraFollowZoom += CameraZoomSpeed * Time.deltaTime;
            }

            CameraFollowZoom = Mathf.Clamp(CameraFollowZoom, 10f, 400f);
            CameraZoomSpeed = CameraFollowZoom * 10;
            CameraMoveSpeed = CameraFollowZoom;
        }
    }
}