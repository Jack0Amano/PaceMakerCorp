using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using System;
using Parameters.MapData;

namespace SelectMapUI
{
    public class SelectMapController : MonoBehaviour
    {
        [SerializeField] SelectMapListAdapter selectMapListAdapter;
        [Tooltip("このパネルを閉じるbutton")]
        [SerializeField] UnityEngine.UI.Button closeButton;

        [Header("Difficulty")]
        [Tooltip("難易度選択のパネル")]
        [SerializeField] private GameObject difficultyPanel;
        [Tooltip("Mapの名前のテキスト")]
        [SerializeField] private TextMeshProUGUI mapNameText;
        [Tooltip("難易度説明のテキストAnimator")]
        [SerializeField] private TextMeshProUGUI difficultyDescriptionText;
        [Tooltip("難易度説明のテキストAnimator")]
        [SerializeField] private TypewriterByCharacter difficultyDescriptionTextAnimator;
        [Tooltip("DifficultyPanelを閉じるButton")]
        [SerializeField] private UnityEngine.UI.Button difficultyCloseButton;

        [Tooltip("Easy難易度のButton")]
        [SerializeField] private ButtonEvents easyButton;
        [Tooltip("Normal難易度のButton")]
        [SerializeField] private ButtonEvents normalButton;
        [Tooltip("Hard難易度のButton")]
        [SerializeField] private ButtonEvents hardButton;


        [Header("Mapの選択ボタン")]
        [Tooltip("Buttonの親オブジェクト")]
        [SerializeField] private GameObject mapButtonParent;
        [Tooltip("ButtonのPrefab")]
        [SerializeField] private GameObject selectMapButtonPrefab;

        private List<SelectMapButton> selectMapButtons = new List<SelectMapButton>();

        /// <summary>
        /// DifficaltyPanelのcanvasGroup
        /// </summary>
        private CanvasGroup difficultyCanvasGroup;

        private CanvasGroup canvasGroup;

        /// <summary>
        /// マップとゲーム難易度が選択された時の挙動
        /// </summary>
        public Action<MapData, GameDifficulty> OnSelectMap;
        /// <summary>
        /// 選択されたMapData
        /// </summary>
        private MapData selectedMapData;
        /// <summary>
        /// Itemのスナップが終わった時のindex
        /// </summary>
        private int lastSnappedIndex = -1;

        private GameManager gameManager;

        // Start is called before the first frame update
        void Start()
        {
            selectMapListAdapter.onSelectMap = (mapData) =>
            {
                selectedMapData = mapData;
                ShowDifficultyPanel();
            };
            canvasGroup = GetComponent<CanvasGroup>();
            difficultyCanvasGroup = difficultyPanel.GetComponent<CanvasGroup>();
            difficultyPanel.SetActive(false);
            difficultyCanvasGroup.alpha = 0;

            easyButton.Button.onClick.AddListener(() => OnSelectDifficultyButton(GameDifficulty.Easy));
            normalButton.Button.onClick.AddListener(() => OnSelectDifficultyButton(GameDifficulty.Normal));
            hardButton.Button.onClick.AddListener(() => OnSelectDifficultyButton(GameDifficulty.Hard));

            easyButton.onPointerEnter += OnPointerEnterDifficultyButton;
            normalButton.onPointerEnter += OnPointerEnterDifficultyButton;
            hardButton.onPointerEnter += OnPointerEnterDifficultyButton;
            hardButton.onPointerExit += OnPointerExitDifficultyButton;
            normalButton.onPointerExit += OnPointerExitDifficultyButton;
            easyButton.onPointerExit += OnPointerExitDifficultyButton;
            hardButton.onPointerExit += OnPointerExitDifficultyButton;

            selectMapButtonPrefab.SetActive(false);

            closeButton.onClick.AddListener(() => HideMapList(true));
            difficultyCloseButton.onClick.AddListener(() => HideDifficultyPanel());

            gameObject.SetActive(false);
            gameManager = GameManager.Instance;
        }

        // Update is called once per frame
        void Update()
        {
            if (selectMapListAdapter.snapper8 && lastSnappedIndex != selectMapListAdapter.snapper8._LastSnappedItemIndex)
            {
                // SelectMapListAdapterが別のMapDataにスナップされた時
                if (selectMapButtons.IndexAt(selectMapListAdapter.snapper8._LastSnappedItemIndex, out var map))
                {
                    SetSelectedMapButton(map.MapData);
                }
                lastSnappedIndex = selectMapListAdapter.snapper8._LastSnappedItemIndex;
            }
        }

        #region SelectMapButton
        /// <summary>
        /// 選択されたMapDataだけをIsSelectedにする
        /// </summary>
        /// <param name="selectedMapData"></param>
        void SetSelectedMapButton(MapData selectedMapData)
        {
            foreach (var button in selectMapButtons)
            {
                button.IsSelected = button.MapData == selectedMapData;
            }
        }

        /// <summary>
        /// ListAdapterを表示しこの中で選択されたMapを返す
        /// </summary>
        public void ShowMapList()
        {
            if (gameObject.activeSelf)
                return;
            canvasGroup.alpha = 0;
            gameObject.SetActive(true);
            selectMapListAdapter.gameObject.SetActive(true);
            canvasGroup.DOFade(1, 0.3f);

            // MapDataを取得
            IEnumerator GetMapData()
            {
                yield return null;
                var mapDataList = MapDataSaveUtility.Load().MapDataList;
                selectMapListAdapter.SetItems(mapDataList);

                // MapDataの数だけButtonを生成
                selectMapButtons.Clear();
                foreach (var mapData in mapDataList)
                {
                    var button = Instantiate(selectMapButtonPrefab, mapButtonParent.transform);
                    button.SetActive(true);
                    var selectMapButton = new SelectMapButton(button);
                    selectMapButton.SetMapData(mapData, (mapData) =>
                    {
                        selectMapListAdapter.ScrollToPage(mapData);
                        SetSelectedMapButton(mapData);
                    });
                    selectMapButtons.Add(selectMapButton);
                }
                SetSelectedMapButton(selectMapButtons[0].MapData);
            }
            StartCoroutine(GetMapData());
        }


        public void HideMapList(bool animation)
        {
            if (!gameObject.activeSelf)
                return;
            canvasGroup.DOFade(0, 0.3f).OnComplete(() =>
            {
                selectMapListAdapter.gameObject.SetActive(false);
                gameObject.SetActive(false);
            });
        }
        #endregion

        #region DifficultyPanel
        /// <summary>
        /// 難易度選択のパネルを表示する
        /// </summary>
        private void ShowDifficultyPanel()
        {
            mapNameText.text = selectedMapData.Name;
            difficultyPanel.SetActive(true);
            difficultyCanvasGroup.DOFade(1, 0.3f);
        }

        /// <summary>
        /// 難易度選択のパネルを非表示にする
        /// </summary>
        private void HideDifficultyPanel()
        {
            difficultyCanvasGroup.DOFade(0, 0.3f).OnComplete(() => difficultyPanel.SetActive(false));
        }

        /// <summary>
        /// 難易度選択ボタンが選択された時の呼び出し
        /// </summary>
        private void OnSelectDifficultyButton(GameDifficulty difficulty)
        {
            OnSelectMap?.Invoke(selectedMapData, difficulty);
        }

        /// <summary>
        /// 難易度選択ボタンにマウスが乗った時の呼び出し
        /// </summary>
        private void OnPointerEnterDifficultyButton(UnityEngine.EventSystems.PointerEventData eventData)
        {
            var button = eventData.pointerEnter.GetComponent<ButtonEvents>();
            if (button != null)
            {
                difficultyDescriptionTextAnimator.ShowText(button.name);
            }
            difficultyDescriptionTextAnimator.ShowText(selectedMapData.Description);
        }

        /// <summary>
        /// 難易度選択ボタンからマウスが離れた時の呼び出し
        /// </summary>
        /// <param name="eventData"></param>
        private void OnPointerExitDifficultyButton(UnityEngine.EventSystems.PointerEventData eventData)
        {
            difficultyDescriptionTextAnimator.StartDisappearingText();
        }

        #endregion
    }


    /// <summary>
    /// SelectMapButtonの各elementを管理するクラス
    /// </summary>
    class SelectMapButton
    {

        private TextMeshProUGUI mapNameText;
        private UnityEngine.UI.Button button;
        private GameObject gameObject;

        public MapData MapData { get; private set; }

        public bool IsSelected
        {
            get => !button.interactable;
            set => button.interactable = !value;
        }

        internal SelectMapButton(GameObject gameObject)
        {
            this.gameObject = gameObject;
            mapNameText = gameObject.GetComponentInChildren<TextMeshProUGUI>();
            button = gameObject.GetComponent<UnityEngine.UI.Button>();
        }

        internal void SetMapData(MapData mapData, Action<MapData> onSelectMap)
        {
            gameObject.name = mapData.Name;
            mapNameText.text = mapData.Name;
            MapData = mapData;
            button.onClick.AddListener(() => onSelectMap?.Invoke(mapData));
        }
    }
}