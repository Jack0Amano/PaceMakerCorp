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
    /// SceneMangaerSceneのゲーム開始時のUI
    /// </summary>
    public class StartCanvasController : MonoBehaviour
    {
        [Header("Start tablet")]
        [SerializeField] GameObject startTablet;
        [SerializeField] SelectMapUI.SelectMapController selectMapController;
        [SerializeField] CinemachineBrain cinemachineBrain;
        [Tooltip("StartPositionのenumとそれを映すVirtualcameraの対応")]
        [SerializeField] List<StartPositionData> startPositionDatas;

        [Header("UI components")]
        [Tooltip("新たなセーブを行う際のボタン")]
        [SerializeField] Button newSaveButton;
        [Tooltip("isRunningmodeがfalseの時に表示されるNewGameボタン")]
        [SerializeField] Button newGameButton;
        [Tooltip("isRunningmodeがtrueの時に表示されるSaveGameボタン")]
        [SerializeField] Button saveButton;
        [SerializeField] Button loadGameButton;
        [SerializeField] Button settingButton;

        [SerializeField] SaveLoadDataCell saveLoadDataCell;

        /// <summary>
        /// StartUIのrootのCanvasGroup
        /// </summary>
        CanvasGroup startCanvasGroup;

        Canvas startCanvas;

        /// <summary>
        /// DataのLoadSaveを行うPanel
        /// </summary>
        [NonSerialized] public DataPanel DataPanel;

        /// <summary>
        /// 現在のstartPositionData
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
        /// 最初のフレームで呼び出される
        /// </summary>
        private IEnumerator CalledAtFirstFrame()
        {
            yield return null;

            // OSAの初期化後にHideする
            DataPanel.Hide(false);
            selectMapController.HideMapList(false);
        }

        #region Show hide canvas
        /// <summary>
        /// StartCanvasを表示してStartMenuの選択を開始する
        /// </summary>
        /// <param name="startPosition">スタートタブレットが置かれている位置</param>
        /// <param name="isRunningmode">Gameが進行中でNewGameが無く代わりにSaveGameが存在するモード</param>
        public void Show(StartPosition startPosition , bool isRunningmode = false)
        {
            if (IsAnimating || IsEnable)
                return;

            IsEnable = true;
            IsAnimating = true;

            gameObject.SetActive(true);

            // NewGameとSaveGameのボタンを表示するかを設定
            newSaveButton.gameObject.SetActive(isRunningmode);
            newGameButton.gameObject.SetActive(!isRunningmode);
            saveButton.gameObject.SetActive(isRunningmode);

            // Canvasの位置をtabletの位置に合わせる
            if (!startPositionDatas.TryFindFirst(s => s.StartPosition == startPosition, out var startPositionData))
            {
                startPositionData = startPositionDatas.Find(s => s.StartPosition == StartPosition.EnterBuilding);
            }
            startTablet.transform.SetPositionAndRotation(startPositionData.CanvasTransform.position, startPositionData.CanvasTransform.rotation);
            this.startPositionData = startPositionData;

            // 他のVirtualCameraのPriorityを0にする
            SetCameraTransition(startPositionData);

            startCanvasGroup.DOFade(1, 1f).OnComplete(() =>
            {
                IsAnimating = false;
            });
        }

        /// <summary>
        /// マップの選択画面を表示する (NewGame)
        /// </summary>
        private void ShowMapSelection()
        {
            DataPanel.Hide(true);
            selectMapController.ShowMapList();
        }

        /// <summary>
        /// 設定画面を表示する
        /// </summary>
        private void ShowSettingPanel()
        {

        }

        /// <summary>
        /// 新たなセーブを行う際のボタンが押された時の処理
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
                // SaveLoadDataCellを中心に表示して保存
                var cell = Instantiate(saveLoadDataCell, startCanvas.transform);
                cell.transform.localPosition = Vector3.zero;
                StartCoroutine(cell.ShowAndHide(DataListItemModel.Create(gameManager.DataSavingController.SaveData)));
                gameManager.EventSceneController.SaveStories();
                gameManager.DataSavingController.NewSave();
            }
        }

        /// <summary>
        /// セーブ画面を表示する
        /// </summary>
        private void ShowSavePanel()
        {
            selectMapController.HideMapList(false);
            DataPanel.Show(GameSettingTab.GameSettingType.Save);
        }

        /// <summary>
        /// ロード画面を表示する
        /// </summary>
        private void ShowLoadPanel()
        {
            selectMapController.HideMapList(false);
            DataPanel.Show(GameSettingTab.GameSettingType.Load);
        }

        /// <summary>
        /// StartcanvasControllerを非表示にする
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
            // cinemachinebrainのカメラがactivevirtualcameraに移動するまで待つ
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
        /// DataPanelによるロードが完了した際に呼び出される DataPanel内でゲームデータを読み込んでいる
        /// </summary>
        private void OnLoadData()
        {
            startPositionData.CinemachineVirtualCamera.Priority = 0;
            StartCoroutine(Hide());
        }

        /// <summary>
        /// 新しいゲームとしてMapと難易度が選択された時に呼び出される
        /// </summary>
        private IEnumerator OnSelectMapToStartNewGame(MapData mapData, GameDifficulty difficulty)
        {
            Print($"Start new game of {mapData.Name}, difficulty: {difficulty}");
            // TODO: Loading画面を表示する
            yield return StartCoroutine(GameManager.Instance.StartNewGame(mapData, difficulty));
            Print("Complete to make new game data. Hide StartCanvasController.");
            
            StartCoroutine( Hide());
        }
        #endregion

        #region Camera
        /// <summary>
        /// Cameraの遷移を行う
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
        /// StartPositionDatasのVirtualCameraのPriorityを0にする
        /// </summary>
        private void SetAllVirtualCameraPriorityTo0(float duration)
        {
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Time = duration;
            startPositionDatas.ForEach(s => s.CinemachineVirtualCamera.Priority = 0);
        }

        /// <summary>
        /// CinemachineBlendのカメラ遷移にカットを使用
        /// </summary>
        private void SetCutBlend()
        {
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Style = CinemachineBlendDefinition.Style.Cut;
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Time = 0;
        }

        /// <summary>
        /// CinemachineBlendのカメラ遷移にEaseInOut移動を使用
        /// </summary>
        private void SetEraceInOutBlend(float duration)
        {
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Time = duration;
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// TabletをstartPositionDataに指定された位置に移動させる
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

            // 他のVirtualCameraのPriorityを0にする
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
        /// StartPositionのenumと実際のTransformの対応を保持する
        /// </summary>
        [Serializable]
        private class StartPositionData
        {
            /// <summary>
            /// スタートカメラの位置のenum
            /// </summary>
            public StartPosition StartPosition;
            /// <summary>
            /// StartPositionに対応するVirtualCamera
            /// </summary>
            public CinemachineVirtualCamera CinemachineVirtualCamera;

            /// <summary>
            /// VirtualCameraに映るCanvasの位置
            /// </summary>
            public Transform CanvasTransform;

            /// <summary>
            /// VirtualCameraのBlendStyle
            /// </summary>
            public CinemachineBlendDefinition.Style BlendStyle;
            /// <summary>
            /// VirtualCameraのBlendDuration
            /// </summary>
            public float BlendDuration;
        }
    }

    /// <summary>
    /// スタートカメラの位置のenum
    /// </summary>
    public enum StartPosition
    {
        /// <summary>
        /// MainMapTableの近くに配置されている状況
        /// </summary>
        NearTable,
        /// <summary>
        /// 会社ビルの入り口に配置されている状況
        /// </summary>
        EnterBuilding,
    }

    /// <summary>
    /// StartCanvasControllerのInspectorをカスタマイズする
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
        /// InspectorのGUIを更新
        /// </summary>
        public override void OnInspectorGUI()
        {
            //元のInspector部分を表示
            base.OnInspectorGUI();

            startCanvasController = target as StartCanvasController;

            GUILayout.Space(10);
            GUILayout.Label("Debug");
            GUILayout.Label("Tabletの位置をStartPositionに移動");
            popupIndex = EditorGUILayout.Popup(label: new GUIContent("StartPosition"),
                                               selectedIndex: popupIndex,
                                               displayedOptions: Enum.GetNames(typeof(StartPosition)));

            //ボタンを表示
            if (GUILayout.Button("Change tablet position"))
            {
                var selectedStartPosition = (StartPosition)popupIndex;
                startCanvasController.DebugSetTabletToPosition(selectedStartPosition);
            }
        }

    }

}