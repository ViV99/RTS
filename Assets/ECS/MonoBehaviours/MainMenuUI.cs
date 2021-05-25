using System;
using System.Collections;
using System.Collections.Generic;
using ECS.Components;
using ECS.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ECS.MonoBehaviours
{
    public class MainMenuUI : MonoBehaviour
    {
        private EntityManager EntityManager { get; set; }
        
        private Transform mainMenuTransform;
        private Transform controlsViewTransform;
        private Transform creditsViewTransform;
        private Transform backgroundTransform;
        private Transform voidTransform;
        private Transform victoryViewTransform;
        private Transform defeatViewTransform;

        private Entity HQ1;
        private Entity HQ2;

        [SerializeField]private Transform botHandler;
        [SerializeField]private Transform UI;
        
        private void Awake()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            mainMenuTransform = transform.Find("MainMenu");
            controlsViewTransform = transform.Find("ControlsView");
            creditsViewTransform = transform.Find("CreditsView");
            backgroundTransform = transform.Find("Background");
            voidTransform = transform.Find("Void");
            defeatViewTransform = transform.Find("DefeatView");
            victoryViewTransform = transform.Find("VictoryView");
        }

        private void Start()
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ExtractionProcessSystem>().Enabled = false;
            UI.gameObject.SetActive(false);
            botHandler.gameObject.SetActive(false);
            SetMainMenuButtons();
            SetControlsViewButtons();
            SetCreditsViewButtons();
            SetHQ();
            victoryViewTransform.Find("QuitButton").GetComponent<Button>().onClick.AddListener(Application.Quit);
            defeatViewTransform.Find("QuitButton").GetComponent<Button>().onClick.AddListener(Application.Quit);
        }

        private bool gameEnded;
        private void Update()
        {
            if (gameEnded)
                return;
            if (!EntityManager.HasComponent<Translation>(HQ1))
            {
                defeatViewTransform.gameObject.SetActive(true);
                voidTransform.gameObject.SetActive(true);
                backgroundTransform.gameObject.SetActive(true);
                StartCoroutine(Fade(defeatViewTransform.GetComponent<CanvasGroup>(), 1f));
                StartCoroutine(Fade(voidTransform.GetComponent<CanvasGroup>(), 1f));
                StartCoroutine(Fade(backgroundTransform.GetComponent<CanvasGroup>(), 1f));
                gameEnded = true;
                botHandler.gameObject.SetActive(false);
                UI.gameObject.SetActive(false);
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ExtractionProcessSystem>().Enabled = false;
            } 
            else if (!EntityManager.HasComponent<Translation>(HQ2))
            {
                victoryViewTransform.gameObject.SetActive(true);
                voidTransform.gameObject.SetActive(true);
                backgroundTransform.gameObject.SetActive(true);
                StartCoroutine(Fade(victoryViewTransform.GetComponent<CanvasGroup>(), 1f));
                StartCoroutine(Fade(voidTransform.GetComponent<CanvasGroup>(), 1f));
                StartCoroutine(Fade(backgroundTransform.GetComponent<CanvasGroup>(), 1f));
                gameEnded = true;
                botHandler.gameObject.SetActive(false);
                UI.gameObject.SetActive(false);
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ExtractionProcessSystem>().Enabled = false;
            }
        }

        private void SetHQ()
        {
            var hqs = EntityManager
                .CreateEntityQuery(ComponentType.ReadWrite<OwnerComponent>())
                .ToEntityArray(Allocator.Temp);
            foreach (var entity in hqs)
            {
                if (EntityManager.GetComponentData<OwnerComponent>(entity).PlayerNumber == 1)
                {
                    HQ1 = entity;
                }
                else
                {
                    HQ2 = entity;
                }
            }
            hqs.Dispose();
        }

        private void SetMainMenuButtons()
        {
            mainMenuTransform.Find("PlayButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(ShowLoadViews());
            });
            mainMenuTransform.Find("QuitButton").GetComponent<Button>().onClick.AddListener(Application.Quit);
            mainMenuTransform.Find("ControlsButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(Fade(mainMenuTransform.GetComponent<CanvasGroup>(), 0.4f));
                mainMenuTransform.gameObject.SetActive(false);
                controlsViewTransform.gameObject.SetActive(true);
                StartCoroutine(Fade(controlsViewTransform.GetComponent<CanvasGroup>(), 0.4f));
            });
            mainMenuTransform.Find("CreditsButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(Fade(mainMenuTransform.GetComponent<CanvasGroup>(), 0.4f));
                mainMenuTransform.gameObject.SetActive(false);
                creditsViewTransform.gameObject.SetActive(true);
                StartCoroutine(Fade(creditsViewTransform.GetComponent<CanvasGroup>(), 0.4f));
            });
        }
        
        private void SetControlsViewButtons()
        {
            controlsViewTransform.Find("BackButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(Fade(controlsViewTransform.GetComponent<CanvasGroup>(), 0.4f));
                controlsViewTransform.gameObject.SetActive(false);
                mainMenuTransform.gameObject.SetActive(true);
                StartCoroutine(Fade(mainMenuTransform.GetComponent<CanvasGroup>(), 0.4f));
            });
        }
        
        private void SetCreditsViewButtons()
        {
            creditsViewTransform.Find("BackButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                StartCoroutine(Fade(creditsViewTransform.GetComponent<CanvasGroup>(), 0.4f));
                creditsViewTransform.gameObject.SetActive(false);
                mainMenuTransform.gameObject.SetActive(true);
                StartCoroutine(Fade(mainMenuTransform.GetComponent<CanvasGroup>(), 0.4f));
            });
        }

        private IEnumerator Fade(CanvasGroup canvasGroup, float duration)
        {
            var end = canvasGroup.alpha == 0 ? 1 : 0;
            var count = 0f;
            while (count < duration)
            {
                count += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, end, count / duration);
                yield return null;
            }
        }

        private IEnumerator ShowLoadViews()
        {
            StartCoroutine(Fade(backgroundTransform.GetComponent<CanvasGroup>(), 5f));
            yield return StartCoroutine(Fade(mainMenuTransform.GetComponent<CanvasGroup>(), 5f));
            mainMenuTransform.gameObject.SetActive(false);
            backgroundTransform.gameObject.SetActive(false);

            yield return StartCoroutine(ShowLoadView(1, 8));
            yield return StartCoroutine(ShowLoadView(2, 35));
            yield return StartCoroutine(ShowLoadView(3, 35));
            yield return StartCoroutine(ShowLoadView(4, 30));
            yield return StartCoroutine(ShowLoadView(5, 30));
            yield return StartCoroutine(ShowLoadView(6, 15));
            yield return StartCoroutine(ShowLoadView(7, 5));

            yield return StartCoroutine(Fade(voidTransform.GetComponent<CanvasGroup>(), 5f));
            voidTransform.gameObject.SetActive(false);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ExtractionProcessSystem>().Enabled = true;
            UI.gameObject.SetActive(true);
            botHandler.gameObject.SetActive(true);
        }

        private IEnumerator ShowLoadView(int num, int duration)
        {
            var view = transform.Find("LoadView" + num);
            view.gameObject.SetActive(true);
            yield return StartCoroutine(Fade(view.GetComponent<CanvasGroup>(), 3f));
            yield return new WaitForSeconds(duration);
            yield return StartCoroutine(Fade(view.GetComponent<CanvasGroup>(), 3f));
            view.gameObject.SetActive(false);
        }
    }
}
