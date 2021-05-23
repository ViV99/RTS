using System;
using System.Collections.Generic;
using System.Linq;
using ECS.Components;
using ECS.Systems;
using ECS.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ECS.MonoBehaviours
{
    public class SelectedMenuUI : MonoBehaviour
    {
        [SerializeField] private Sprite fighterSprite;
        [SerializeField] private Sprite battleshipSprite;
        [SerializeField] private Sprite destroyerAASprite;
        [SerializeField] private Sprite torpedoCruiserSprite;
        [SerializeField] private Sprite juggernautSprite;
        [SerializeField] private Sprite HQSprite;
        [SerializeField] private Sprite listeningPostSprite;
        [SerializeField] private Sprite shipyardSprite;
        [SerializeField] private Sprite counterShipyardSprite;
        [SerializeField] private Sprite extractorSprite;

        private EntityManager EntityManager { get; set; }
        
        private Dictionary<Transform, Entity> ButtonToEntity { get; set; }
        private Dictionary<string, Transform> EntityNameToView { get; set; }

        private PrefabsComponent prefabs;
        private Entity lastSelectedEntity;
        private Entity gameStateHandler;

        private void Awake()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            lastSelectedEntity = Entity.Null;
            ButtonToEntity = new Dictionary<Transform, Entity>();
            EntityNameToView = new Dictionary<string, Transform>();
        }

        private void Start()
        {
            prefabs = EntityManager
                .CreateEntityQuery(ComponentType.ReadOnly<PrefabsComponent>())
                .GetSingleton<PrefabsComponent>();
            gameStateHandler = EntityManager
                .CreateEntityQuery(ComponentType.ReadWrite<GameStateComponent>())
                .GetSingletonEntity();
        }

        private void Update()
        {
            var selectedEntities = EntityManager
                .CreateEntityQuery(ComponentType.ReadOnly<SelectedTag>())
                .ToEntityArray(Allocator.Temp);
            var singleMenu = transform.Find("SingleMenu");
            var groupMenu = transform.Find("GroupMenu");
            if (selectedEntities.Length == 0)
            {
                lastSelectedEntity = Entity.Null;
                singleMenu.gameObject.SetActive(false);
                groupMenu.gameObject.SetActive(false);
            }
            else if (selectedEntities.Length == 1)
            {
                singleMenu.gameObject.SetActive(true);
                groupMenu.gameObject.SetActive(false);
                var isBuilding = EntityManager.HasComponent<BuildingTag>(selectedEntities[0]);
                if (lastSelectedEntity == selectedEntities[0])
                {
                    if (isBuilding)
                    {
                        SetSingleEntityMenu(singleMenu, SerializeBuildingStats);
                        SetButtonUnitCount();
                        return;
                    }
                    SetSingleEntityMenu(singleMenu, SerializeUnitStats);
                    return;
                }
                lastSelectedEntity = selectedEntities[0];
                DeleteSpawnUnitButtons();
                if (!isBuilding)
                {
                    SetSingleEntityMenu(singleMenu, SerializeUnitStats);
                }
                else
                {
                    SetSingleEntityMenu(singleMenu, SerializeBuildingStats);
                    
                    var type = EntityManager.GetComponentData<BuildingTypeComponent>(lastSelectedEntity).Type;
                    var buttonTemplate = singleMenu.Find("SpawnUnitButtonTemplate");
                    if (type == BuildingType.Shipyard)
                    {
                        CreateShipyardSpawnUnitButtons(singleMenu, buttonTemplate);
                    }
                    else if (type == BuildingType.CounterShipyard)
                    {
                        CreateCounterShipyardSpawnUnitButtons(singleMenu, buttonTemplate);
                    }
                }
            }
            else
            {
                singleMenu.gameObject.SetActive(false);
                groupMenu.gameObject.SetActive(true);
                lastSelectedEntity = Entity.Null;
                DeleteSpawnUnitButtons();
                var selectedCount = new SortedDictionary<string, int>();
                foreach (var entityName in selectedEntities.Select(entity => EntityManager.GetName(entity)))
                {
                    if (!selectedCount.ContainsKey(entityName))
                    {
                        selectedCount[entityName] = 0;
                    }
                    selectedCount[entityName]++;
                }
                CleanUpUnitViews(selectedCount);
                SetGroupEntityMenu(groupMenu, selectedCount);
            }

            selectedEntities.Dispose();
        }

        private void SetSingleEntityMenu(Transform singleMenu, Func<Entity, string> serializer)
        {
            if (lastSelectedEntity == Entity.Null)
                return;
            singleMenu
                .Find("EntityView")
                .Find("Image")
                .GetComponent<Image>().sprite = GetSpriteFromEntity(EntityManager.GetName(lastSelectedEntity));
            singleMenu
                .Find("EntityStats")
                .GetComponent<Text>().text = serializer(lastSelectedEntity);
        }

        private void SetButtonUnitCount()
        {
            var unitCount = new Dictionary<Entity, int>();
            var orderQueue = EntityManager.GetBuffer<OrderQueueElementComponent>(lastSelectedEntity);
            var orderInfo = EntityManager.GetComponentData<OrderQueueInfoComponent>(lastSelectedEntity);
            var avtomat = false;
            for (var i = orderInfo.L; i != orderInfo.R || (!avtomat && orderInfo.Count == orderQueue.Capacity); 
                i = (i + 1) % orderQueue.Capacity)
            {
                if (orderInfo.L == orderInfo.R)
                {
                    avtomat = true;
                }
                if (!unitCount.ContainsKey(orderQueue[i].Target))
                {
                    unitCount[orderQueue[i].Target] = 0;
                }
                unitCount[orderQueue[i].Target]++;
            }
            foreach (var button in ButtonToEntity.Keys)
            {
                var countTransform = button.Find("UnitCount");
                countTransform.gameObject.SetActive(false);
                if (unitCount.TryGetValue(ButtonToEntity[button], out var currentButtonUnitCount)
                    && currentButtonUnitCount != 0)
                {
                    countTransform.gameObject.SetActive(true);
                    countTransform.GetComponent<Text>().text = $"x{currentButtonUnitCount}";
                }
            }
        }

        private void DeleteSpawnUnitButtons()
        {
            foreach (var button in ButtonToEntity.Keys)
            {
                Destroy(button.gameObject);
            }
            ButtonToEntity.Clear();
        }

        private void CreateShipyardSpawnUnitButtons(Transform singleMenu, Transform buttonTemplate)
        {
            CreateButton(singleMenu, buttonTemplate, new Vector2(40, 0), Vector2.zero, prefabs.FighterPrefab1);
            CreateButton(singleMenu, buttonTemplate, new Vector2(190, 0), Vector2.zero, prefabs.BattleshipPrefab1);
        }
        
        private void CreateCounterShipyardSpawnUnitButtons(Transform singleMenu, Transform buttonTemplate)
        {
            CreateButton(singleMenu, buttonTemplate, new Vector2(-5, 0), Vector2.zero, prefabs.DestroyerAAPrefab1);
            CreateButton(singleMenu, buttonTemplate, new Vector2(110, 0), Vector2.zero, prefabs.TorpedoCruiserPrefab1);
            CreateButton(singleMenu, buttonTemplate, new Vector2(225, 0), Vector2.zero, prefabs.JuggernautPrefab1);
        }

        private void CreateButton(Transform singleMenu, Transform buttonTemplate, Vector2 offset, Vector2 imageSizeDelta,
            Entity spawnEntity)
        {
            var buttonTransform = Instantiate(buttonTemplate, singleMenu);
            buttonTransform.gameObject.SetActive(true);
            buttonTransform.GetComponent<RectTransform>().anchoredPosition += offset;
            var imageTransform = buttonTransform.Find("Image");
            imageTransform.GetComponent<Image>().sprite = GetSpriteFromEntity(EntityManager.GetName(spawnEntity));
            imageTransform.GetComponent<RectTransform>().sizeDelta += imageSizeDelta;
            buttonTransform.Find("UnitDescription").GetComponent<Text>().text = SerializeButtonDescription(spawnEntity);

            buttonTransform.GetComponent<SpawnUnitButtonListener>().onLeftClick.AddListener(() =>
            {
                var orderQueue = EntityManager.GetBuffer<OrderQueueElementComponent>(lastSelectedEntity);
                var orderInfo = EntityManager.GetComponentData<OrderQueueInfoComponent>(lastSelectedEntity);
                SetSpawnOrder(orderQueue, orderInfo, spawnEntity);
            });
            buttonTransform.GetComponent<SpawnUnitButtonListener>().onRightClick.AddListener(() =>
            {
                var orderQueue = EntityManager.GetBuffer<OrderQueueElementComponent>(lastSelectedEntity);
                var orderInfo = EntityManager.GetComponentData<OrderQueueInfoComponent>(lastSelectedEntity);
                var stats = EntityManager.GetComponentData<EntityStatsComponent>(lastSelectedEntity);
                stats.CurrentLoad = 0;
                stats.ReloadTime = 0;
                EntityManager.SetComponentData(lastSelectedEntity, stats);
                ResetSpawnOrderQueue(orderQueue, orderInfo);
            });
            ButtonToEntity[buttonTransform] = spawnEntity;
        }
        
        private void SetGroupEntityMenu(Transform groupMenu, SortedDictionary<string, int> selectedCount)
        {
            var viewTemplate = groupMenu.Find("EntityViewTemplate");
            var offset = Vector2.zero;
            foreach (var entityName in selectedCount.Keys)
            {
                if (offset.x > 150)
                {
                    offset.x = 0;
                    offset.y -= 140;
                }
                if (EntityNameToView.TryGetValue(entityName, out var view) && view != null)
                {
                    view.GetComponent<RectTransform>().anchoredPosition =
                        viewTemplate.GetComponent<RectTransform>().anchoredPosition;
                    view.GetComponent<RectTransform>().anchoredPosition += offset;
                    view.Find("UnitCount").GetComponent<Text>().text = $"x{selectedCount[entityName]}";
                } 
                else
                {
                    CreateView(groupMenu, viewTemplate, offset, new Vector2(0, 0), entityName);
                }
                offset.x += 150;
            }
            
        }
        
        private void CreateView(Transform groupMenu, Transform viewTemplate, Vector2 offset, Vector2 imageSizeDelta,
            string entityName)
        {
            var viewTransform = Instantiate(viewTemplate, groupMenu);
            viewTransform.gameObject.SetActive(true);
            viewTransform.GetComponent<RectTransform>().anchoredPosition += offset;
            var imageTransform = viewTransform.Find("Image");
            imageTransform.GetComponent<Image>().sprite = GetSpriteFromEntity(entityName);
            imageTransform.GetComponent<RectTransform>().sizeDelta += imageSizeDelta;
            EntityNameToView[entityName] = viewTransform;
        }
        
        private void CleanUpUnitViews(SortedDictionary<string, int> selectedCount)
        {
            foreach (var entityName in EntityNameToView.Keys.Where(entityName => !selectedCount.ContainsKey(entityName)))
            {
                if (EntityNameToView[entityName] == null)
                    continue;
                Destroy(EntityNameToView[entityName].gameObject);
            }
        }

        private void ResetSpawnOrderQueue(DynamicBuffer<OrderQueueElementComponent> orderQueue,
            OrderQueueInfoComponent orderInfo)
        {
            if (orderInfo.Count == 0)
            {
                orderInfo.Count = 0;
                orderInfo.L = 0;
                orderInfo.R = 0;
            }
            else
            {
                var gameState = EntityManager.GetComponentData<GameStateComponent>(gameStateHandler);
                gameState.Pop1 -= EntityManager.GetComponentData<EntityStatsComponent>(orderQueue[orderInfo.L].Target).Pop;
                gameState.Resources1 += EntityManager.GetComponentData<EntityStatsComponent>(orderQueue[orderInfo.L].Target).SpawnCost;
                EntityManager.SetComponentData(gameStateHandler, gameState);
                orderInfo.Count = 1;
                orderInfo.R = orderInfo.L + 1;
                orderQueue[orderInfo.L] = orderQueue[orderInfo.L].WithState(OrderState.Complete);
            }
            EntityManager.SetComponentData(lastSelectedEntity, orderInfo);
        }
        
        private void SetSpawnOrder(DynamicBuffer<OrderQueueElementComponent> orderQueue, 
            OrderQueueInfoComponent orderInfo, Entity spawnEntity)
        {
            if (orderInfo.Count == orderQueue.Capacity)
                return;
            var order = new OrderQueueElementComponent(OrderType.Spawn, OrderState.New, int2.zero, spawnEntity);
            if (orderInfo.R < orderQueue.Length)
            {
                orderQueue[orderInfo.R] = order;
            }
            else
            {
                orderQueue.Add(order);
            }
            orderInfo.R = (orderInfo.R + 1) % orderQueue.Capacity;
            orderInfo.Count++;
            EntityManager.SetComponentData(lastSelectedEntity, orderInfo);
        }

        private string SerializeUnitStats(Entity entity)
        {
            var stats = EntityManager.GetComponentData<EntityStatsComponent>(entity);
            var entityName = EntityManager.GetName(entity);
            return $"    {entityName.Substring(0, entityName.Length - 1)}\n" +
                   $"Health : {stats.CurrentHealth} / {stats.MaxHealth}\n" +
                   $"Armor : {stats.Armor}\n" +
                   $"Speed : {stats.MoveSpeed}\n" +
                   $"RPM : {10800 / stats.ReloadTime}\n" +
                   $"Damage : {stats.Damage}\n" +
                   $"Attack range : {stats.AttackRange}\n";
        }
        
        private string SerializeBuildingStats(Entity entity)
        {
            var stats = EntityManager.GetComponentData<EntityStatsComponent>(entity);
            var entityName = EntityManager.GetName(entity);
            return $"    {entityName.Substring(0, entityName.Length - 1)}\n" +
                   $"Health : {stats.CurrentHealth} / {stats.MaxHealth}\n" +
                   $"Armor : {stats.Armor}\n";
            //$"Load state : {stats.CurrentLoad * 60} / {stats.ReloadTime * 60}\n";
        }

        private string SerializeButtonDescription(Entity entity)
        {
            var stats = EntityManager.GetComponentData<EntityStatsComponent>(entity);
            var entityName = EntityManager.GetName(entity);
            return $"{entityName.Substring(0, entityName.Length - 1)}\n" +
                   $"Cost : {stats.SpawnCost}\n" +
                   $"Time : {10800 / stats.SpawnTime}\n" +
                   $"Pop Count : {stats.Pop}";
        }

        private Sprite GetSpriteFromEntity(string entityName)
        {
            if (entityName == EntityManager.GetName(prefabs.FighterPrefab1))
                return fighterSprite;
            if (entityName == EntityManager.GetName(prefabs.BattleshipPrefab1))
                return battleshipSprite;
            if (entityName == EntityManager.GetName(prefabs.DestroyerAAPrefab1))
                return destroyerAASprite;
            if (entityName == EntityManager.GetName(prefabs.TorpedoCruiserPrefab1))
                return torpedoCruiserSprite;
            if (entityName == EntityManager.GetName(prefabs.JuggernautPrefab1)) 
                return juggernautSprite;
            if (entityName == EntityManager.GetName(prefabs.HQ1)) 
                return HQSprite;
            if (entityName == EntityManager.GetName(prefabs.ListeningPost1))
                return listeningPostSprite;
            if (entityName == EntityManager.GetName(prefabs.Shipyard1)) 
                return shipyardSprite;
            if (entityName == EntityManager.GetName(prefabs.CounterShipyard1)) 
                return counterShipyardSprite;
            if (entityName == EntityManager.GetName(prefabs.Extractor1)) 
                return extractorSprite;
            return default(Sprite);
        }
    }
}
