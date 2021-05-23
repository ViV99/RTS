using System;
using ECS.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace ECS.MonoBehaviours
{
    public class GameStateUI : MonoBehaviour
    {
        private EntityManager EntityManager { get; set; }

        private Entity gameStateHandler;

        private void Awake()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Start()
        {
            gameStateHandler = EntityManager
                .CreateEntityQuery(ComponentType.ReadWrite<GameStateComponent>())
                .GetSingletonEntity();
        }

        private void Update()
        {
            var gameStateDescription = transform.Find("GameStateDescription");
            var gameState = EntityManager.GetComponentData<GameStateComponent>(gameStateHandler);
            gameStateDescription.Find("Description").GetComponent<Text>().text = 
                $"Population : {gameState.Pop1} / {gameState.MaxPop1}\n" + 
                $"Resources : {gameState.Resources1}";
        }
    }
}
