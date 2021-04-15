using System;
using UnityEngine;

namespace ECS.MonoBehaviours
{
    public class GameHandler : MonoBehaviour
    {
        private const float CameraMoveSpeed = 100f;
        private const float CameraZoomSpeed = 1000f;
        public static GameHandler Instance { get; private set; }

        [SerializeField] private CameraFollow cameraFollow;
        private Vector3 CameraFollowPosition { get; set; }
        private float CameraFollowZoom { get; set; }

        public Transform selectionAreaTransform;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CameraFollowZoom = 4f;
            cameraFollow.Setup(() => CameraFollowPosition, () => CameraFollowZoom, true, true);
            // TODO: заспавнить юнитов
        }

        // Update is called once per frame
        void Update()
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
            CameraFollowPosition += moveDir * CameraMoveSpeed * Time.deltaTime;

            if (Input.mouseScrollDelta.y > 0)
            {
                CameraFollowZoom -= 1 * CameraZoomSpeed * Time.deltaTime;
            }
            else if (Input.mouseScrollDelta.y < 0)
            {
                CameraFollowZoom += 1 * CameraZoomSpeed * Time.deltaTime;
            }

            CameraFollowZoom = Mathf.Clamp(CameraFollowZoom, 1f, 8f);
        }
    }
}