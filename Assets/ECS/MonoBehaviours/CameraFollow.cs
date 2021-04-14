using System;
using UnityEngine;

namespace ECS.MonoBehaviours
{
    public class CameraFollow : MonoBehaviour
    {
        private const float CameraMoveSpeed = 3f;
        private const float CameraZoomSpeed = 3f;

        private Camera MyCamera { get; set; }
        private Func<Vector3> GetCameraFollowPositionFunc { get; set; }
        private Func<float> GetCameraZoomFunc { get; set; }

        public void Setup(Func<Vector3> getCameraFollowPositionFunc, Func<float> getCameraZoomFunc,
            bool teleportToFollowPosition, bool instantZoom)
        {
            GetCameraFollowPositionFunc = getCameraFollowPositionFunc;
            GetCameraZoomFunc = getCameraZoomFunc;

            if (teleportToFollowPosition)
            {
                var cameraFollowPosition = getCameraFollowPositionFunc();
                var currentTransform = transform;
                cameraFollowPosition.z = currentTransform.position.z;
                currentTransform.position = cameraFollowPosition;
            }

            if (instantZoom)
            {
                MyCamera.orthographicSize = getCameraZoomFunc();
            }
        }

        private void Start()
        {
            MyCamera = transform.GetComponent<Camera>();
        }

        public void SetCameraFollowPosition(Vector3 cameraFollowPosition)
        {
            GetCameraFollowPositionFunc = () => cameraFollowPosition;
        }
        
        public void SetCameraZoom(float cameraZoom)
        {
            GetCameraZoomFunc = () => cameraZoom;
        }
        
        private void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            if (GetCameraFollowPositionFunc == null) return;
            
            var cameraFollowPosition = GetCameraFollowPositionFunc();
            var currentTransform = transform;
            var position = currentTransform.position;
            cameraFollowPosition.z = position.z;

            var cameraMoveDir = (cameraFollowPosition - position).normalized;
            var distance = Vector3.Distance(cameraFollowPosition, position);

            if (distance <= 0) return;
            
            var newCameraPosition =
                transform.position + cameraMoveDir * (distance * CameraMoveSpeed * Time.deltaTime);

            var distanceAfterMoving = Vector3.Distance(newCameraPosition, cameraFollowPosition);

            if (distanceAfterMoving > distance)
            {
                newCameraPosition = cameraFollowPosition;
            }

            transform.position = newCameraPosition;
        }

        private void HandleZoom()
        {
            if (GetCameraZoomFunc == null) return;
            
            var cameraZoom = GetCameraZoomFunc();

            var orthographicSize = MyCamera.orthographicSize;
            var cameraZoomDifference = cameraZoom - orthographicSize;
            

            orthographicSize += cameraZoomDifference * CameraZoomSpeed * Time.deltaTime;
            MyCamera.orthographicSize = orthographicSize;

            if (cameraZoomDifference > 0)
            {
                if (MyCamera.orthographicSize > cameraZoom)
                {
                    MyCamera.orthographicSize = cameraZoom;
                }
            }
            else
            {
                if (MyCamera.orthographicSize < cameraZoom)
                {
                    MyCamera.orthographicSize = cameraZoom;
                }
            }
        }
    }
}