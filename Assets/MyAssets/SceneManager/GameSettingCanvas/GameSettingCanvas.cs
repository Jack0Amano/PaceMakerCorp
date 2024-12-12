using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using DG.Tweening;
using static Utility;

namespace GameSetting {

    /// <summary>
    /// Gameの大本のデータ(セーブデータ,設定データ等)の管理を行う Escキーで表示
    /// </summary>
    public class GameSettingCanvas : MonoBehaviour
    {
        [Tooltip("Tabのclass")]
        [SerializeField] List<GameSettingTab> gameSettingTabs;

        [Tooltip("Tabの表示する際のアニメーションのカーブ")]
        [SerializeField] AnimationCurve showTabCurve;

        [Tooltip("Tabの非表示の際のアニメーションのカーブ")]
        [SerializeField] AnimationCurve hideTabCurve;

        [Tooltip("Save&LoadData用のPanel")]
        [SerializeField] SaveLoadData.DataPanel dataPanel;

        internal CanvasGroup canvasGroup;

        private readonly float animationDuration = 0.3f;
        /// <summary>
        /// SettingCavnasが表示中か
        /// </summary>
        public bool IsEnable { private set; get; } = false;

        private bool isAnimating = false;

        // Start is called before the first frame update
        void Start()
        {
            gameSettingTabs.ForEach(t =>
            {
                t.showAnimationCurve = showTabCurve;
                t.hideAnimationCurve = hideTabCurve;
                t.SetToHide();
                t.TabButton.onClick.AddListener(() => TabButtonOnClick(t));
            });

            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
            dataPanel.parentCanvas = this;
            dataPanel.OnLoadData += () =>
            {
                Hide();
            };
        }

        // Update is called once per frame
        void Update()
        {
        }

        /// <summary>
        /// TabUIが選択された
        /// </summary>
        /// <param name="tab"></param>
        private void TabButtonOnClick(GameSettingTab tab)
        {
            foreach(var t in gameSettingTabs)
            {
                if (t == tab)
                    t.IsSelected = true;
                else if (t.IsSelected)
                    t.IsSelected = false;
            }

            if (tab.type == GameSettingTab.GameSettingType.Save)
            {
                dataPanel.Show(GameSettingTab.GameSettingType.Save);
            }
            else if (tab.type == GameSettingTab.GameSettingType.Load)
            {
                dataPanel.Show(GameSettingTab.GameSettingType.Load);
            }
            else
            {

            }
        }

        /// <summary>
        /// EscキーでGameデータ設定画面を表示する
        /// </summary>
        public void Show()
        {
            // メッセージやダイアログなどの何らかのイベントWindowが表示中の場合GameSettingCanvasを表示しない
            if (GameManager.Instance.EventSceneController.IsEventActive)
                return;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (isAnimating)
                return;

            var delay = 0f;
            foreach (var t in gameSettingTabs)
            {
                t.AnimationToShow(delay, t.type == GameSettingTab.GameSettingType.Save ? true : false);
                delay += 0.1f;
            }

            IsEnable = true;
            isAnimating = true;

            canvasGroup.alpha = 0;
            gameObject.SetActive(true);
            canvasGroup.DOFade(1, animationDuration).OnComplete(() => isAnimating = false);
            dataPanel.Show(GameSettingTab.GameSettingType.Save);
        }

        /// <summary>
        /// Gameデータ設定画面をとじる
        /// </summary>
        public void Hide()
        {
            if (dataPanel.IsKeyLocked || GameManager.Instance.EventSceneController.messageEvent.IsEventSceneActive)
                return;

            if (isAnimating)
                return;

            isAnimating = true;

            var delay = 0f;
            foreach (var t in gameSettingTabs)
            {
                t.AnimationToHide(delay);
                delay += 0.1f;
            }

            canvasGroup.DOFade(0, animationDuration).OnComplete(() =>
            {
                gameObject.SetActive(false);
                IsEnable = false;
                isAnimating = false;
            });
        }
    
    }

    /// <summary>
    /// Escタブのまとめ
    /// </summary>
    [Serializable]
    public class GameSettingTab
    {
        [Tooltip("TabのType")]
        [SerializeField] internal GameSettingType type;

        [Tooltip("TabのGameObject")]
        [SerializeField] GameObject tabObject;

        [Tooltip("Tabの表示用のPos")]
        [SerializeField] RectTransform showTabTransform;

        [Tooltip("Tabの非表示用のPos")]
        [SerializeField] RectTransform hideTabTransform;

        [NonSerialized] internal AnimationCurve showAnimationCurve;
        [NonSerialized] internal AnimationCurve hideAnimationCurve;

        /// <summary>
        /// アニメーション時間
        /// </summary>
        private readonly float animationDuration = 0.3f;
        /// <summary>
        /// Tabが選択状態にあるときどの程度XPositionを移動させるか
        /// </summary>
        private readonly float selectedXPositionAdding = 20;

        /// <summary>
        /// Tabが表示状態にあるか
        /// </summary>
        internal bool IsEnable { private set; get; } = false;
        /// <summary>
        /// Tabの選択などを管理するSequence
        /// </summary>
        Sequence animationSequence;

        /// <summary>
        /// Tabが選択状態にあるか
        /// </summary>
        internal bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    if (animationSequence != null && animationSequence.IsActive())
                        animationSequence.Kill();
                    isSelected = value;

                    if (value)
                        RectTransform.DOAnchorPos(new Vector2(selectedXPositionAdding, 0), animationDuration / 2);
                    else
                        RectTransform.DOAnchorPos(Vector2.zero, animationDuration / 2);

                }
                else if (value && isSelected)
                {
                    // SelectをすでにされているのにSelectされた
                    animationSequence = tabObject.transform.DOShakeX();
                }
            }
        }

        private bool isSelected = false;

        /// <summary>
        /// TabのButton
        /// </summary>
        public Button TabButton
        {
            get
            {
                if (tabButton == null)
                    tabButton = tabObject.GetComponent<Button>();
                return tabButton;
            }
        }
        private Button tabButton;

        /// <summary>
        /// TabのRecttransform
        /// </summary>
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = tabObject.GetComponent<RectTransform>();
                return rectTransform;
            }
        }
        private RectTransform rectTransform;

        /// <summary>
        /// タブタイプ
        /// </summary>
        [Serializable]
        public enum GameSettingType
        {
            None,
            Save,
            Load,
            Setting
        }

        /// <summary>
        /// Tabを隠れた上部に移動させる
        /// </summary>
        public void SetToHide()
        {
            IsEnable = false;
            tabObject.transform.SetParent(hideTabTransform);
            RectTransform.DOAnchorPos(Vector2.zero, 0);
        }

        /// <summary>
        /// Tabを表示中にする
        /// </summary>
        public void AnimationToShow(float delay, bool isSelected)
        {
            this.isSelected = isSelected;
            tabObject.transform.SetParent(showTabTransform);
            var pos = Vector2.zero;
            if (isSelected)
                pos = new Vector2(selectedXPositionAdding, 0);
            RectTransform.DOAnchorPos(pos, animationDuration).SetEase(showAnimationCurve).SetDelay(delay).OnComplete(() =>
            {
                IsEnable = true;
            });
        }

        /// <summary>
        /// Tabを非表示にする
        /// </summary>
        public void AnimationToHide(float delay)
        {
            isSelected = false;
            tabObject.transform.SetParent(hideTabTransform);
            RectTransform.DOAnchorPos(Vector2.zero, animationDuration).SetEase(hideAnimationCurve).SetDelay(delay).OnComplete(() =>
            {
                IsEnable = false;
            });
        }

        public override string ToString()
        {
            return tabObject.name;
        }
    }

}