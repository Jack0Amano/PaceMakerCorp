using Cinemachine;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using GameSetting;
using GameSetting.SaveLoadData;
using Parameters.MapData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Utility;

namespace StartUI
{
    /// <summary>
    /// SceneMangaerScene�̃Q�[���J�n����UI
    /// </summary>
    public class StartCanvasController : MonoBehaviour
    {
        [Header("Start tablet")]
        [SerializeField] GameObject startTablet;
        [SerializeField] SelectMapUI.SelectMapController selectMapController;
        [SerializeField] CinemachineBrain cinemachineBrain;
        [Tooltip("StartPosition��enum�Ƃ�����f��Virtualcamera�̑Ή�")]
        [SerializeField] List<StartPositionData> startPositionDatas;

        [Header("UI components")]
        [Tooltip("�V���ȃZ�[�u���s���ۂ̃{�^��")]
        [SerializeField] Button newSaveButton;
        [Tooltip("isRunningmode��false�̎��ɕ\�������NewGame�{�^��")]
        [SerializeField] Button newGameButton;
        [Tooltip("isRunningmode��true�̎��ɕ\�������SaveGame�{�^��")]
        [SerializeField] Button saveButton;
        [SerializeField] Button loadGameButton;
        [SerializeField] Button settingButton;

        [SerializeField] SaveLoadDataCell saveLoadDataCell;

        /// <summary>
        /// StartUI��root��CanvasGroup
        /// </summary>
        CanvasGroup startCanvasGroup;

        Canvas startCanvas;

        /// <summary>
        /// Data��LoadSave���s��Panel
        /// </summary>
        [NonSerialized] public DataPanel DataPanel;

        /// <summary>
        /// ���݂�startPositionData
        /// </summary>
        private StartPositionData startPositionData;

        public bool IsEnable { get; private set; } = false;

        public bool IsAnimating { get; private set; } = false;

        public bool IsSaving { get; private set; } = false;

        GameManager gameManager;

        private void Awake()
        {
            startCanvas = startTablet.GetComponentInChildren<Canvas>();
            startCanvasGroup = startCanvas.GetComponent<CanvasGroup>();
            startCanvasGroup.alpha = 0;
            DataPanel = startCanvas.GetComponentInChildren<DataPanel>();
            DataPanel.OnLoadData += OnLoadData;

            gameManager = GameManager.Instance;
            newSaveButton.onClick.AddListener(NewSaveButtonAction);
            newGameButton.onClick.AddListener(ShowMapSelection);
            saveButton.onClick.AddListener(ShowSavePanel);
            loadGameButton.onClick.AddListener(ShowLoadPanel);
            settingButton.onClick.AddListener(ShowSettingPanel);
            selectMapController.OnSelectMap += ((mapData, difficulty) =>
            {
                StartCoroutine(OnSelectMapToStartNewGame(mapData, difficulty));
            });
        }

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(CalledAtFirstFrame());
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// �ŏ��̃t���[���ŌĂяo�����
        /// </summary>
        private IEnumerator CalledAtFirstFrame()
        {
            yield return null;

            // OSA�̏��������Hide����
            DataPanel.Hide(false);
            selectMapController.HideMapList(false);
        }

        #region Show hide canvas
        /// <summary>
        /// StartCanvas��\������StartMenu�̑I�����J�n����
        /// </summary>
        /// <param name="startPosition">�X�^�[�g�^�u���b�g���u����Ă���ʒu</param>
        /// <param name="isRunningmode">Game���i�s����NewGame�����������SaveGame�����݂��郂�[�h</param>
        public void Show(StartPosition startPosition , bool isRunningmode = false)
        {
            if (IsAnimating || IsEnable)
                return;

            IsEnable = true;
            IsAnimating = true;

            gameObject.SetActive(true);

            // NewGame��SaveGame�̃{�^����\�����邩��ݒ�
            newSaveButton.gameObject.SetActive(isRunningmode);
            newGameButton.gameObject.SetActive(!isRunningmode);
            saveButton.gameObject.SetActive(isRunningmode);

            // Canvas�̈ʒu��tablet�̈ʒu�ɍ��킹��
            if (!startPositionDatas.TryFindFirst(s => s.StartPosition == startPosition, out var startPositionData))
            {
                startPositionData = startPositionDatas.Find(s => s.StartPosition == StartPosition.EnterBuilding);
            }
            startTablet.transform.SetPositionAndRotation(startPositionData.CanvasTransform.position, startPositionData.CanvasTransform.rotation);
            this.startPositionData = startPositionData;

            // ����VirtualCamera��Priority��0�ɂ���
            SetCameraTransition(startPositionData);

            startCanvasGroup.DOFade(1, 1f).OnComplete(() =>
            {
                IsAnimating = false;
            });
        }

        /// <summary>
        /// �}�b�v�̑I����ʂ�\������ (NewGame)
        /// </summary>
        private void ShowMapSelection()
        {
            DataPanel.Hide(true);
            selectMapController.ShowMapList();
        }

        /// <summary>
        /// �ݒ��ʂ�\������
        /// </summary>
        private void ShowSettingPanel()
        {

        }

        /// <summary>
        /// �V���ȃZ�[�u���s���ۂ̃{�^���������ꂽ���̏���
        /// </summary>
        private void NewSaveButtonAction()
        {
            if (IsSaving)
                return;
            IEnumerator NewSaveButtonActionAsync()
            {
                IsSaving = true;
                gameManager.EventSceneController.SaveStories();
                var path = gameManager.DataSavingController.NewSave();
                yield return new WaitForSeconds(0.3f);
                DataPanel.CreateNewSave(gameManager.DataSavingController.SaveData, path);
                yield return new WaitForSeconds(InsertDeleteAnimationState.ANIMATION_DURATION);
                IsSaving = false;
            }

            if(DataPanel.Type != GameSettingTab.GameSettingType.None)
            {
                StartCoroutine(NewSaveButtonActionAsync());
            }
            else
            {
                Print("New save");
                // SaveLoadDataCell�𒆐S�ɕ\�����ĕۑ�
                var cell = Instantiate(saveLoadDataCell, startCanvas.transform);
                cell.transform.localPosition = Vector3.zero;
                StartCoroutine(cell.ShowAndHide(DataListItemModel.Create(gameManager.DataSavingController.SaveData)));
                gameManager.EventSceneController.SaveStories();
                gameManager.DataSavingController.NewSave();
            }
        }

        /// <summary>
        /// �Z�[�u��ʂ�\������
        /// </summary>
        private void ShowSavePanel()
        {
            selectMapController.HideMapList(false);
            DataPanel.Show(GameSettingTab.GameSettingType.Save);
        }

        /// <summary>
        /// ���[�h��ʂ�\������
        /// </summary>
        private void ShowLoadPanel()
        {
            selectMapController.HideMapList(false);
            DataPanel.Show(GameSettingTab.GameSettingType.Load);
        }

        /// <summary>
        /// StartcanvasController���\���ɂ���
        /// </summary>
        public IEnumerator Hide()
        {
            Print("Hide StartCanvasController", IsAnimating, !IsEnable);
            if (IsAnimating || !IsEnable)
                yield break;

            selectMapController.HideMapList(true);
            DataPanel.Hide(true);

            var duration = 1f;
            SetAllVirtualCameraPriorityTo0(duration);

            IsAnimating = true;
            startCanvasGroup.DOFade(0, 1f).OnComplete(() =>
            {
                
                IsAnimating = false;
            });
            yield return null;
            // cinemachinebrain�̃J������activevirtualcamera�Ɉړ�����܂ő҂�
            var activeVirtualCameraTransform = cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject.transform;
            while (true)
            {
                yield return null;
                if (Vector3.Distance(cinemachineBrain.transform.position, activeVirtualCameraTransform.position) < 0.005f)
                    break;
            }
            yield return new WaitForSeconds(0.5f);
            
            gameManager.MainMapScene.CompleteToLoad();
            yield return new WaitForSeconds(0.5f);
            IsEnable = false;
            gameObject.SetActive(false);
        }
        #endregion

        #region Load events 
        /// <summary>
        /// DataPanel�ɂ�郍�[�h�����������ۂɌĂяo����� DataPanel���ŃQ�[���f�[�^��ǂݍ���ł���
        /// </summary>
        private void OnLoadData()
        {
            startPositionData.CinemachineVirtualCamera.Priority = 0;
            StartCoroutine(Hide());
        }

        /// <summary>
        /// �V�����Q�[���Ƃ���Map�Ɠ�Փx���I�����ꂽ���ɌĂяo�����
        /// </summary>
        private IEnumerator OnSelectMapToStartNewGame(MapData mapData, GameDifficulty difficulty)
        {
            Print($"Start new game of {mapData.Name}, difficulty: {difficulty}");
            // TODO: Loading��ʂ�\������
            yield return StartCoroutine(GameManager.Instance.StartNewGame(mapData, difficulty));
            Print("Complete to make new game data. Hide StartCanvasController.");
            
            StartCoroutine( Hide());
        }
        #endregion

        #region Camera
        /// <summary>
        /// Camera�̑J�ڂ��s��
        /// </summary>
        private void SetCameraTransition(StartPositionData startPositionData)
        {
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Style = startPositionData.BlendStyle;
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Time = startPositionData.BlendDuration;

            startPositionDatas.ForEach(s =>
            {
                if (s == startPositionData)
                    s.CinemachineVirtualCamera.Priority = 100;
                else
                    s.CinemachineVirtualCamera.Priority = 0;
            });
        }

        /// <summary>
        /// StartPositionDatas��VirtualCamera��Priority��0�ɂ���
        /// </summary>
        private void SetAllVirtualCameraPriorityTo0(float duration)
        {
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Time = duration;
            startPositionDatas.ForEach(s => s.CinemachineVirtualCamera.Priority = 0);
        }

        /// <summary>
        /// CinemachineBlend�̃J�����J�ڂɃJ�b�g���g�p
        /// </summary>
        private void SetCutBlend()
        {
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Style = CinemachineBlendDefinition.Style.Cut;
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Time = 0;
        }

        /// <summary>
        /// CinemachineBlend�̃J�����J�ڂ�EaseInOut�ړ����g�p
        /// </summary>
        private void SetEraceInOutBlend(float duration)
        {
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Time = duration;
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Tablet��startPositionData�Ɏw�肳�ꂽ�ʒu�Ɉړ�������
        /// </summary>
        /// <param name="tileID"></param>
        public void DebugSetTabletToPosition(StartPosition startPosition)
        {
            if (!startPositionDatas.TryFindFirst(s => s.StartPosition == startPosition, out var startPositionData))
            {
                startPositionData = startPositionDatas.Find(s => s.StartPosition == StartPosition.EnterBuilding);
            }
            startTablet.transform.SetPositionAndRotation(startPositionData.CanvasTransform.position, startPositionData.CanvasTransform.rotation);
            this.startPositionData = startPositionData;

            // ����VirtualCamera��Priority��0�ɂ���
            startPositionDatas.ForEach(s =>
            {
                if (s == startPositionData)
                    s.CinemachineVirtualCamera.Priority = 100;
                else
                    s.CinemachineVirtualCamera.Priority = 0;
            });
            print($"Set tablet to {startPosition}");
        }
#endif

        /// <summary>
        /// StartPosition��enum�Ǝ��ۂ�Transform�̑Ή���ێ�����
        /// </summary>
        [Serializable]
        private class StartPositionData
        {
            /// <summary>
            /// �X�^�[�g�J�����̈ʒu��enum
            /// </summary>
            public StartPosition StartPosition;
            /// <summary>
            /// StartPosition�ɑΉ�����VirtualCamera
            /// </summary>
            public CinemachineVirtualCamera CinemachineVirtualCamera;

            /// <summary>
            /// VirtualCamera�ɉf��Canvas�̈ʒu
            /// </summary>
            public Transform CanvasTransform;

            /// <summary>
            /// VirtualCamera��BlendStyle
            /// </summary>
            public CinemachineBlendDefinition.Style BlendStyle;
            /// <summary>
            /// VirtualCamera��BlendDuration
            /// </summary>
            public float BlendDuration;
        }
    }

    /// <summary>
    /// �X�^�[�g�J�����̈ʒu��enum
    /// </summary>
    public enum StartPosition
    {
        /// <summary>
        /// MainMapTable�̋߂��ɔz�u����Ă����
        /// </summary>
        NearTable,
        /// <summary>
        /// ��Ѓr���̓�����ɔz�u����Ă����
        /// </summary>
        EnterBuilding,
    }

    /// <summary>
    /// StartCanvasController��Inspector���J�X�^�}�C�Y����
    /// </summary>
    [CustomEditor(typeof(StartCanvasController))]
    public class TilesControllerScriptEditor : Editor
    {

        StartCanvasController startCanvasController;

        int popupIndex = 0;

        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI()
        {
            
            return base.CreateInspectorGUI();
        }

        /// <summary>
        /// Inspector��GUI���X�V
        /// </summary>
        public override void OnInspectorGUI()
        {
            //����Inspector������\��
            base.OnInspectorGUI();

            startCanvasController = target as StartCanvasController;

            GUILayout.Space(10);
            GUILayout.Label("Debug");
            GUILayout.Label("Tablet�̈ʒu��StartPosition�Ɉړ�");
            popupIndex = EditorGUILayout.Popup(label: new GUIContent("StartPosition"),
                                               selectedIndex: popupIndex,
                                               displayedOptions: Enum.GetNames(typeof(StartPosition)));

            //�{�^����\��
            if (GUILayout.Button("Change tablet position"))
            {
                var selectedStartPosition = (StartPosition)popupIndex;
                startCanvasController.DebugSetTabletToPosition(selectedStartPosition);
            }
        }

    }

}