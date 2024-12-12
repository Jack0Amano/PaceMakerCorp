using System.Collections;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Linq;

namespace MainMap.UI
{
    public class MainMapTabController : MonoBehaviour
    {

        [SerializeField] Button squadTabButton;
        TextMeshProUGUI squadTabLabel;
        Image squadTabHL;

        [SerializeField] Button hqTabButton;
        TextMeshProUGUI hqTabLabel;
        Image hqTabHL;

        [SerializeField] Button shopTabButton;
        TextMeshProUGUI shopTabLabel;
        Image shopTabHL;

        [SerializeField] Button itemTabButton;
        TextMeshProUGUI itemTabLabel;
        Image itemTabHL;

        private Button previousButton;

        private Dictionary<WindowType, (Button button, TextMeshProUGUI label, Image hlImage)> windowTypeAndButtons;

        internal EventHandler<OpenWindowArgs> requestOpenWindowHandler;

        private CanvasGroup canvasGroup;

        private Color highlightColor;

        private const float Duration = 0.5f;

        protected private void Awake()
        {
            (TextMeshProUGUI, Image) GetComponents(GameObject parent)
            {
                return (parent.GetComponentInChildren<TextMeshProUGUI>(), parent.GetComponentsInChildren<Image>()[1]);
            }

            (squadTabLabel, squadTabHL) = GetComponents(squadTabButton.gameObject);
            (hqTabLabel, hqTabHL) = GetComponents(hqTabButton.gameObject);
            (shopTabLabel, shopTabHL) = GetComponents(shopTabButton.gameObject);
            (itemTabLabel, itemTabHL) = GetComponents(itemTabButton.gameObject);

            highlightColor = squadTabHL.color;

            windowTypeAndButtons = new Dictionary<WindowType, (Button button, TextMeshProUGUI label, Image hlImage)>
            {
                {WindowType.SquadsWindow, (squadTabButton, squadTabLabel, squadTabHL) },
                {WindowType.HQ, (hqTabButton, hqTabLabel, hqTabHL) },
                {WindowType.ShopWindow, (shopTabButton, shopTabLabel, shopTabHL) },
                {WindowType.ItemWindow, (itemTabButton, itemTabLabel, itemTabHL) },
            };

            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// MainUIでそれぞれのWIndowの遷移を全て管理しているため遷移の際はRequestEventをMainUIControllerで
        /// </summary>
        internal void SetUp()
        {
            /*
             * 
             *   MainUIController - BaseWinwos
             *                    L DataController
             *                    L SquadsController
             *                    L ...
             * 
             *  こんな感じにBaseWindowと各WIndowの内容物達が同列に存在しているため
             *  BaseWindowのTabButtonからWindowを切り替える際は一度RequestをMainUIControllerに送る必要がある
             *  
             */

            squadTabButton.onClick.AddListener(() =>
            {
                requestOpenWindowHandler?.Invoke(this, new OpenWindowArgs() { windowType = WindowType.SquadsWindow });
                InteractiveTabButton(WindowType.SquadsWindow, true);
            });
            shopTabButton.onClick.AddListener(() =>
            {
                requestOpenWindowHandler?.Invoke(this, new OpenWindowArgs() { windowType = WindowType.ShopWindow });
                InteractiveTabButton(WindowType.ShopWindow, true);
            });
            itemTabButton.onClick.AddListener(() =>
            {
                requestOpenWindowHandler?.Invoke(this, new OpenWindowArgs() { windowType = WindowType.ItemWindow });
                InteractiveTabButton(WindowType.ItemWindow, true);
            });
            hqTabButton.onClick.AddListener(() =>
            {
                requestOpenWindowHandler?.Invoke(this, new OpenWindowArgs() { windowType = WindowType.HQ });
                InteractiveTabButton(WindowType.HQ, true);
            });

        }


        internal void Show(WindowType showType, Action onComplete = null)
        {
            gameObject.SetActive(true);
            canvasGroup.DOFade(1, Duration).OnComplete(() =>
            {
                onComplete?.Invoke();
            }).Play();

            InteractiveTabButton(showType);
        }
        
        internal void Hide(Action onComplete = null)
        {
            canvasGroup.DOFade(0, Duration).OnComplete(() =>
            {
                onComplete?.Invoke();
                gameObject.SetActive(false);
            }).Play();
        }

        /// <summary>
        /// 指定されたWindowTypeのボタンをIneractiveする
        /// </summary>
        /// <param name="windowType"></param>
        internal void InteractiveTabButton(WindowType windowType, bool animation=false)
        {
            var button = windowTypeAndButtons[windowType].button;
            if (previousButton != null)
                previousButton.interactable = true;
            button.interactable = false;
            windowTypeAndButtons.ToList().ForEach(d =>
            {
                if (animation)
                {
                    if (d.Key != windowType)
                    {
                        d.Value.hlImage.DOColor(Color.clear, 0.25f);
                        d.Value.label.DOColor(Color.white, 0.25f);
                    }
                    else
                    {
                        d.Value.hlImage.DOColor(highlightColor, 0.25f);
                        d.Value.label.DOColor(Color.black, 0.25f);
                    }
                        
                }
                else
                {
                    if (d.Key != windowType)
                    {
                        d.Value.hlImage.color = Color.clear;
                        d.Value.label.color = Color.white;
                    }
                    else
                    {
                        d.Value.hlImage.color = highlightColor;
                        d.Value.label.color = Color.black;
                    }
                }
            });
            previousButton = button;
        }
    }
}