using ECS.Components;
using ECS.Flags;
using ECS.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace ECS.MonoBehaviours
{
    public class BuildingTypeSelectUI : MonoBehaviour
    {
        [SerializeField] private Sprite listeningPost;
        [SerializeField] private Sprite shipyard;
        [SerializeField] private Sprite counterShipyard;
        [SerializeField] private Sprite extractor;
        // [SerializeField] private Sprite simpleBuildingSprite5;
        // [SerializeField] private Sprite simpleBuildingSprite6;
        [SerializeField] private BuildingManager buildingManager;

        private EntityManager EntityManager { get; set; }
        private UnitControlSystem UnitControl { get; set; }

        private void Awake()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            UnitControl = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<UnitControlSystem>();
        }

        private void Start()
        {
            var prefabs = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PrefabsComponent>())
                .GetSingleton<PrefabsComponent>();

            var buildingButtonTemplate = transform.Find("BuildingButtonTemplate");
            buildingButtonTemplate.gameObject.SetActive(false);

            CreateButton(buildingButtonTemplate, Vector2.zero, listeningPost, new Vector2(0, 0),
                prefabs.ListeningPost1);
            CreateButton(buildingButtonTemplate, new Vector2(0, -120), shipyard, new Vector2(0, 0),
                prefabs.Shipyard1);
            CreateButton(buildingButtonTemplate, new Vector2(-120, 0), counterShipyard, new Vector2(0, 0),
                prefabs.CounterShipyard1);
            CreateButton(buildingButtonTemplate, new Vector2(-120, -120), extractor, new Vector2(0, 0),
                prefabs.Extractor1);
            // CreateButton(buildingButtonTemplate, new Vector2(-240, 0), simpleBuildingSprite2, new Vector2(10, 10),
            //     prefabs.TorpedoCruiserPrefab1);
            // CreateButton(buildingButtonTemplate, new Vector2(-240, -120), simpleBuildingSprite2, new Vector2(10, 10),
            //     prefabs.TorpedoCruiserPrefab1);
        }

        private void CreateButton(Transform buttonTemplate, Vector2 offset, Sprite buttonImage, Vector2 imageSizeDelta,
            Entity spawnEntity)
        {
            var buildingButtonTransform = Instantiate(buttonTemplate, transform);
            buildingButtonTransform.gameObject.SetActive(true);
            buildingButtonTransform.GetComponent<RectTransform>().anchoredPosition += offset;
            var imageTransform = buildingButtonTransform.Find("Image");
            imageTransform.GetComponent<Image>().sprite = buttonImage;
            imageTransform.GetComponent<RectTransform>().sizeDelta += imageSizeDelta;
            buildingButtonTransform.GetComponent<Button>().onClick.AddListener(() =>
            {
                buildingManager.DestroyActive();
                UnitControl.ResetSelection();
                buildingManager.ActiveBuildingType = spawnEntity;
                var spriteGameObj = new GameObject();
                var spriteRenderer = spriteGameObj.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = buttonImage;
                spriteRenderer.color = new Color(0, 0.5f, 1, 0.75f);
                spriteGameObj.transform.localScale =
                    EntityManager.GetComponentData<CompositeScale>(spawnEntity).Value.c0.xxw;
                buildingManager.ActiveBuildingSprite = spriteGameObj;
            });
        }
    }
}