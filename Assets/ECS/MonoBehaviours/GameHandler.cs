using System;
using UnityEngine;

namespace ECS.MonoBehaviours
{
    public class GameHandler : MonoBehaviour
    {
        public static GameHandler Instance { get; private set; }

        public Transform selectionAreaTransform;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // TODO: ебануть камеру
            
            
            
            
            // TODO: заспавнить юнитов
            
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
