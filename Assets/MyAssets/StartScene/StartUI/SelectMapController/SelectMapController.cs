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
        [Tooltip("���̃p�l�������button")]
        [SerializeField] UnityEngine.UI.Button closeButton;

        [Header("Difficulty")]
        [Tooltip("��Փx�I���̃p�l��")]
        [SerializeField] private GameObject difficultyPanel;
        [Tooltip("Map�̖��O�̃e�L�X�g")]
        [SerializeField] private TextMeshProUGUI mapNameText;
        [Tooltip("��Փx�����̃e�L�X�gAnimator")]
        [SerializeField] private TextMeshProUGUI difficultyDescriptionText;
        [Tooltip("��Փx�����̃e�L�X�gAnimator")]
        [SerializeField] private TypewriterByCharacter difficultyDescriptionTextAnimator;
        [Tooltip("DifficultyPanel�����Button")]
        [SerializeField] private UnityEngine.UI.Button difficultyCloseButton;

        [Tooltip("Easy��Փx��Button")]
        [SerializeField] private ButtonEvents easyButton;
        [Tooltip("Normal��Փx��Button")]
        [SerializeField] private ButtonEvents normalButton;
        [Tooltip("Hard��Փx��Button")]
        [SerializeField] private ButtonEvents hardButton;


        [Header("Map�̑I���{�^��")]
        [Tooltip("Button�̐e�I�u�W�F�N�g")]
        [SerializeField] private GameObject mapButtonParent;
        [Tooltip("Button��Prefab")]
        [SerializeField] private GameObject selectMapButtonPrefab;

        private List<SelectMapButton> selectMapButtons = new List<SelectMapButton>();

        /// <summary>
        /// DifficaltyPanel��canvasGroup
        /// </summary>
        private CanvasGroup difficultyCanvasGroup;

        private CanvasGroup canvasGroup;

        /// <summary>
        /// �}�b�v�ƃQ�[����Փx���I�����ꂽ���̋���
        /// </summary>
        public Action<MapData, GameDifficulty> OnSelectMap;
        /// <summary>
        /// �I�����ꂽMapData
        /// </summary>
        private MapData selectedMapData;
        /// <summary>
        /// Item�̃X�i�b�v���I���������index
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
                // SelectMapListAdapter���ʂ�MapData�ɃX�i�b�v���ꂽ��
                if (selectMapButtons.IndexAt(selectMapListAdapter.snapper8._LastSnappedItemIndex, out var map))
                {
                    SetSelectedMapButton(map.MapData);
                }
                lastSnappedIndex = selectMapListAdapter.snapper8._LastSnappedItemIndex;
            }
        }

        #region SelectMapButton
        /// <summary>
        /// �I�����ꂽMapData������IsSelected�ɂ���
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
        /// ListAdapter��\�������̒��őI�����ꂽMap��Ԃ�
        /// </summary>
        public void ShowMapList()
        {
            if (gameObject.activeSelf)
                return;
            canvasGroup.alpha = 0;
            gameObject.SetActive(true);
            selectMapListAdapter.gameObject.SetActive(true);
            canvasGroup.DOFade(1, 0.3f);

            // MapData���擾
            IEnumerator GetMapData()
            {
                yield return null;
                var mapDataList = MapDataSaveUtility.Load().MapDataList;
                selectMapListAdapter.SetItems(mapDataList);

                // MapData�̐�����Button�𐶐�
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
        /// ��Փx�I���̃p�l����\������
        /// </summary>
        private void ShowDifficultyPanel()
        {
            mapNameText.text = selectedMapData.Name;
            difficultyPanel.SetActive(true);
            difficultyCanvasGroup.DOFade(1, 0.3f);
        }

        /// <summary>
        /// ��Փx�I���̃p�l�����\���ɂ���
        /// </summary>
        private void HideDifficultyPanel()
        {
            difficultyCanvasGroup.DOFade(0, 0.3f).OnComplete(() => difficultyPanel.SetActive(false));
        }

        /// <summary>
        /// ��Փx�I���{�^�����I�����ꂽ���̌Ăяo��
        /// </summary>
        private void OnSelectDifficultyButton(GameDifficulty difficulty)
        {
            OnSelectMap?.Invoke(selectedMapData, difficulty);
        }

        /// <summary>
        /// ��Փx�I���{�^���Ƀ}�E�X����������̌Ăяo��
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
        /// ��Փx�I���{�^������}�E�X�����ꂽ���̌Ăяo��
        /// </summary>
        /// <param name="eventData"></param>
        private void OnPointerExitDifficultyButton(UnityEngine.EventSystems.PointerEventData eventData)
        {
            difficultyDescriptionTextAnimator.StartDisappearingText();
        }

        #endregion
    }


    /// <summary>
    /// SelectMapButton�̊eelement���Ǘ�����N���X
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