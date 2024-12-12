/* 命名規則について
 * ファイル名及びクラス名の命名規則は以下の通り
 * ~ Scene :- シーン全体を管理するMainのClass。1シーンあたり1つのみで、主な役割は毎フレームごとにUserの入力を検知し必要なControllerに値を渡す
 * ~ User ~ :- SceneClassからのUser入力を必要とするクラス
 */

/*
 * 各ファイルのロード順 Information - SRPGUserScene - UnitCon - UnitsController - SquaresController - SquareCell
 * SquareCellのStartでは位置しているUnitの探索を行うためUnitConの後
 * InformationはGameConに自身を渡しているため読み込み順は最初
 * SRPGConはSRPG部分の導入なためInformationの次程度の2番目
 * UnitsConのStartではUnitsのリスト化をしているためここまでにUnitPrefabを用意しなければならない X UnitsController.UnitLoaded()で動的読み込み
 * UnitConのロードは特に順序はない
 * 
 * SRPGControllerはInformationでのロードを監視し、各Controllerのロード後の実行を管理する
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static Utility;
using System.Linq;
using DG.Tweening;
using Cinemachine;
using UnityEngine.AzureSky;
using Tactics.Character;
using Tactics.Object;
using Tactics;
using MainMap;
using Tactics.UI;
using Tactics.Control;

namespace Tactics
{

    public class TacticsController : MonoBehaviour
    {

        #region Properties

        [Header("Controllers")]
        [SerializeField] public GameObject unitsObject;
        [SerializeField] internal Control.CameraUserController cameraController;
        [SerializeField] internal Map.TilesController tilesController;
        [SerializeField] Canvas overlayCanvas;
        [SerializeField] TacticsTablet.TacticsTablet tacticsTablet;

        [Header("Result UI")]
        [SerializeField] UI.ResultPanel resultPanel;
        [Tooltip("負けた場合にTabletが地面に置かれる位置")]
        [SerializeField] Transform loseResultTransform;
        [Tooltip("負けた場合にTabletが置かれそれを映すカメラ")]
        [SerializeField] CinemachineVirtualCamera loseResultCamera;

        [Header("UI when playing")]
        [Tooltip("Tactics中にUnitの一覧を表示するWindow")]
        [SerializeField] UI.UnitListWindow unitListWindow;
        [SerializeField] UI.Overlay.OrderOfAction orderOfActionController;
        [Tooltip("味方陣営の情報を表示するCanvasGroup")]
        [SerializeField] CanvasGroup myInfoCanvasGroup;
        [Tooltip("Tactics中にOverlayCanvasにUnitが使用しているItemを表示する")]
        [SerializeField] UI.Overlay.SelectItemPanel SelectItemPanel;

        [Header("Debug")]
        [Tooltip("Debug用にTacticsを単独で動かせるかどうか")]
        [SerializeField] bool isStandaloneMode = false;
        [Tooltip("Debug用のTacticsの属してるMapのname")]
        [SerializeField] string DebugMapSceneName;
        /// <summary>
        /// ゲームの進行状況
        /// </summary>
        [SerializeField, ReadOnly] private SceneState sceneState = SceneState.INIT;

        private GameManager gameManager;

        internal UnitsController unitsController;
        /// <summary>
        /// 現在行動中のUnit
        /// </summary>
        public Character.UnitController ActiveUnit
        {
            get => unitsController.activeUnit;
        }
        /// <summary>
        /// Tactics画面のゲームが動作状態にあるかどうか
        /// </summary>
        static public bool IsGameRunning { private set; get; } = false;

        /// <summary>
        /// 勝利条件Controller
        /// </summary>
        private VictoryConditions victoryConditions;

        /// <summary>
        /// ロードが完了して最初の一回だけ実行
        /// </summary>
        private bool isLoadCompleted = false;
        /// <summary>
        /// ターン経過をカウントする
        /// </summary>
        public int TurnCount
        {
            get => unitsController.AttributeTurnCount;
        }

        /// <summary>
        /// Tactics画面がポーズされている状態かどうか
        /// </summary>
        public bool PauseTactics
        {
            get => PauseTactics;
            set
            {
                unitsController.PauseAllAnimation = value;
                Physics.simulationMode = value ?  SimulationMode.Script : SimulationMode.FixedUpdate;
                StaticPauseTactics = value;
            }
        }
        /// <summary>
        /// Tactics画面がポーズされている状態かどうか
        /// </summary>
        static public bool StaticPauseTactics = false;

        /// <summary>
        /// SettingCanvasが表示中か
        /// </summary>
        private bool isSettingCanvasEnable = false;

        /// <summary>
        /// OverlayCanvasのalpha
        /// </summary>
        CanvasGroup overlayCanvasGroup;

        /// <summary>
        /// 割り込み型のストーリーイベントを管理する
        /// </summary>
        EventScene.EventSceneController eventSceneController;

        /// <summary>
        /// 時間や天気を管理する
        /// </summary>
        NaturalEnvironmentController naturalEnvironmentController;

        DebugController debugController;

        private CameraMode previousCameraMode;
        #endregion


        #region Init
        // Start is called before the first frame update
        private void Awake()
        {

            IsGameRunning = false;

            // Get script in UnitContainer
            unitsController = unitsObject.GetComponent<Character.UnitsController>();
            unitsController.tilesController = tilesController;
            unitsController.selectItemPanel = SelectItemPanel;

            // Victory conditions
            victoryConditions = gameObject.GetComponent<VictoryConditions>();
            victoryConditions.endGameAction += (arg => StartCoroutine(EndGame(arg)));
            // TODO: victoryConditionsの仮の勝利条件
            victoryConditions.mode = VictoryConditions.Mode.KILL_ALL;

            naturalEnvironmentController = GetComponent<NaturalEnvironmentController>();

            gameManager = GameManager.Instance;

            tacticsTablet.TilesController = tilesController;

            if (isStandaloneMode)
                gameManager.LoadDebugData(DebugMapSceneName);

            tacticsTablet.TilesController = tilesController;
            tacticsTablet.CameraController = cameraController;
            cameraController.TacticsTablet = tacticsTablet;
            debugController = new DebugController(this);

            loseResultCamera.Priority = 0;  
        }

        void Start()
        {
            eventSceneController = gameManager.EventSceneController;
            sceneState = SceneState.PREPARE;
            tacticsTablet.PreparePanel.startBattleButton.onClick.AddListener(() => StartGame());
            // Prepareが終わるまでここで待ってる
            StartCoroutine(Load());

            //timeController.PauseTime();
            // var gameTime = GameManager.DataSavingController.SaveDataInfo.gameTime.Value;
            //if (GameController.Instance.data.saveDataInfo != null)
            //    gameTime = GameController.Instance.data.saveDataInfo.gameTime;
            //timeController.SetDate(gameTime.Year, gameTime.Month, gameTime.Day);
            //var timeline = (float)gameTime.Hour + ((float)gameTime.Minute / 60);
            //timeController.SetTimeline(timeline);

            unitListWindow.hideButtonAction += (() =>
            {
                Time.timeScale = 1;
                cameraController.enableCameraControlling = true;
                UserController.enableCursor = false;
                sceneState = SceneState.WAIT;
            });

            overlayCanvasGroup = overlayCanvas.GetComponent<CanvasGroup>();
            overlayCanvasGroup.alpha = 0;
            overlayCanvas.gameObject.SetActive(false);

            unitsController.endAllUnitsActionHandler += CalledEndActiveUnitAction;
        }

        /// <summary>
        /// 各種データのロード
        /// </summary>
        private IEnumerator Load()
        {
            PauseTactics = true;

            // Preparepanelからエンカウント内容を取得
            while (tacticsTablet.ReachedEventArgs == null)
                yield return null;
            var reachedEvent = tacticsTablet.ReachedEventArgs;
            
            // encount時刻に設定する
            naturalEnvironmentController.SetDateTime(reachedEvent.ReachedDateTime.Value);
            Print($"Encounter at {reachedEvent.ReachedDateTime}");

            var tilesCoroutine = StartCoroutine(tilesController.AsyncLoad());
            // 敵ユニットを配置  
            var unitsCoroutine = StartCoroutine(unitsController.SetUnitsAsEnemy(reachedEvent.Enemy.tileAndUnits));
            
            // CameraUserControllerが初期化されるまで待つ
            while (!cameraController.IsActivated)
                yield return null;

            tacticsTablet.ShowEmptyScreen(false);
            cameraController.ChangeModeTacticsTablet();
            
            StartCoroutine(gameManager.FadeInOutCanvas.Hide(delay: 0.5f));

            // StandAloneModeではTabletの位置は初期位置でイベントはない
            if (!isStandaloneMode)
            {
                // Tabletの位置をStartPositionに対応したPlayerのTileに設定
                tacticsTablet.SetStartPosition(reachedEvent.SpawnRequestData.StartPosition);
                // Eventがある場合は待つ
                StartCoroutine(eventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.OnPrepare,
                                      reachedEvent.Enemy.ID));
                
                if (eventSceneController.IsEventWindowActive)
                {
                    Print($"Event is active");
                    sceneState = SceneState.EVENT;
                    while (eventSceneController.IsEventWindowActive)
                        yield return null;
                    sceneState = SceneState.PREPARE;
                }
            }

            gameManager.QuickSave("BeforeTactics", reachedEvent);
            tacticsTablet.ShowPreparePanel();

            // ここでPreparePanelからStartGameが呼ばれるまで待つ
            gameManager.NortifyCompleteToLoad();
            Print("Complete to set data on TacticsController. It's waiting to prepare");
            while (sceneState != SceneState.PREPARE_LOAD)
            {
                yield return null;
            }

            // tabletを下げて非表示にする
            StartCoroutine( tacticsTablet.Hide());

            yield return unitsCoroutine;
            unitsCoroutine = StartCoroutine( unitsController.SetUnits(tacticsTablet.PreparePanel.PlayerResults));

            yield return tilesCoroutine;
            yield return unitsCoroutine;

            Print($"Complate to load {unitsController.UnitsList.Count} units");
            print(string.Join(",", unitsController.UnitsList));
            unitsController.CompleteLoad();

            IEnumerator AttachSquareAndUnits()
            {
                yield return null;
                yield return StartCoroutine(tilesController.UnitLoaded());
            }

            yield return StartCoroutine(AttachSquareAndUnits());

            isLoadCompleted = true;

            sceneState = SceneState.INIT;

            StartCoroutine(FirstTurn());
        }

        /// <summary>
        /// Preapareパネルのゲーム開始ボタンが選択されたときPreparePanelから呼び出し
        /// </summary>
        private void StartGame()
        {
            sceneState = SceneState.PREPARE_LOAD;
        }

        #endregion

        // Update is called once per frame
        private void Update()
        {
            if (sceneState == SceneState.PREPARE || sceneState == SceneState.PREPARE_LOAD || sceneState == SceneState.EVENT)
                return;

            if (!isLoadCompleted) return;

            //if (PrepareDebugMode != DebugController.IsActive)
            //{
            //    PrepareDebugMode = DebugController.IsActive;
            //    if (DebugController.IsActive)
            //    {
            //        // デバッグモード開始
            //        sceneState = SceneState.DEBUG;
            //        cameraController.enableCameraControlling = false;
            //        pauseTactics = true;
            //    }
            //    else
            //    {
            //        // デバッグモード終了
            //        sceneState = SceneState.WAIT;
            //        cameraController.enableCameraControlling = true;
            //        pauseTactics = false;
            //    }
            //}

            if (gameManager.StartCanvasController.IsEnable != isSettingCanvasEnable)
            {
                if (gameManager.StartCanvasController.IsEnable)
                {
                    // GameSettingCanvasが表示中になった
                    cameraController.enableCameraControlling = false;
                    UserController.enableCursor = true;
                    PauseTactics = true;
                    isSettingCanvasEnable = true;
                }
                else
                {
                    // GameSettingCanvasが非表示になった
                    cameraController.enableCameraControlling = true;
                    UserController.enableCursor = false;
                    PauseTactics = false;
                    isSettingCanvasEnable = false;
                }
                return;
            }

            // Show map
            if (UserController.KeyCodeM)
            {
                PrintWarning("Map is not implemented yet. Put map on TacticsTablet");
            }
            else if (UserController.KeyCodeI)
            {
                if (unitListWindow.gameObject.activeSelf)
                {
                    // 表示中なので非表示にする
                    Time.timeScale = 1;
                    unitListWindow.Hide(true);
                    cameraController.enableCameraControlling = true;
                    UserController.enableCursor = false;    
                    sceneState = SceneState.WAIT;
                }
                else
                {
                    // 非表示なので表示する
                    StartCoroutine( unitListWindow.Show(() =>
                    {
                        cameraController.enableCameraControlling = false;
                        UserController.enableCursor = true;
                        Time.timeScale = 0;
                    }));
                    sceneState = SceneState.LOCK;
                }
            }

            CheckToShowTacticsTablet();

            // Sceneがロックされている場合break
            if (sceneState == SceneState.LOCK || sceneState == SceneState.TABLET)
                return;


            // ===================== ユニットのターン遷移 =====================
            // Skip turn
            if (UserController.KeyCodeSpace)
            {
                if (!ActiveUnit.isAiControlled)
                {
                    // Playerのturnをspaceでスキップしようとしている
                    unitsController.EndTurnFromPlayerControl();
                }
                else
                {
                    // Enemyのturnをspaceでスキップしようとしている 要skip機能
                }

            }

            if (ActiveUnit != null)
            {
                // ActiveUnitのUserControlを行う
                // Attack フォーカスモード
                if (UserController.KeyForcusDown && 
                    !ActiveUnit.IsUsingGimmickObject && 
                    !ActiveUnit.isAlreadyActioned)
                {

                    if (!unitsController.IsFocusMode)
                    {
                        // フォーカスモードに入る
                        unitsController.StartFocusMode();
                    }
                    else
                    {
                        // フォーカスモードから出る
                        unitsController.EndFocusMode();
                    }
                }
                else if (UserController.KeyUseItem &&
                         ActiveUnit.CanUseGimmickObject && 
                         !unitsController.IsFocusMode && 
                         !ActiveUnit.isAlreadyActioned)
                {
                    // Gimmickを使用する
                    unitsController.StartGimmickMode(ActiveUnit.GimmickObject);
                }
                else if (UserController.KeyUseItem && ActiveUnit.IsUsingGimmickObject && !unitsController.IsFocusMode)
                {
                    // Gimmickを使用中であるためこれから離れる動作
                    unitsController.CancelGimmickMode();
                }
            }

            // MouseWheelはカメラの距離移動
            // 使用アイテムをMouseWheelで切り替える
            ChangeItemControl();
        }

        #region Function for control
        TacticsTablet.OnTactics.WindowTabType windowTabType = TacticsTablet.OnTactics.WindowTabType.None;
        /// <summary>
        /// TacticsTabletを表示する
        /// </summary>
        private void CheckToShowTacticsTablet()
        {
            if (sceneState == SceneState.FINISHED || sceneState == SceneState.PREPARE || sceneState == SceneState.PREPARE_LOAD)
                return;
            if (tacticsTablet.IsAnimating)
                return;

            if (UserController.KeyShowInfo)
                windowTabType = TacticsTablet.OnTactics.WindowTabType.Tactics;
            else if (UserController.KeyCodeSetting)
                windowTabType = TacticsTablet.OnTactics.WindowTabType.Setting;
            else
                return;
            
            // Tacticsモードでtabletを表示する
            if (tacticsTablet.IsVisible)
            {

                // Tabletが表示中なので非表示にする
                cameraController.DisableOverlayTacticsTablet();
                overlayCanvas.gameObject.SetActive(true);
                overlayCanvasGroup.DOFade(1, 0.5f);
                StartCoroutine(tacticsTablet.Hide());
                PauseTactics = false;
                sceneState = SceneState.WAIT;
            }
            else
            {
                // Tabletが非表示なので表示する
                PauseTactics = true;
                overlayCanvasGroup.DOFade(0, 0.5f).OnComplete(() =>
                {
                    overlayCanvas.gameObject.SetActive(false);
                });
                var unitWatchingTablet = cameraController.ChangeModeOverlayTacticsTablet();
                tacticsTablet.ShowOnTacticsPanel(unitWatchingTablet, windowTabType);
                sceneState = SceneState.TABLET;
            }
        }

        /// <summary>
        /// 最初のターンを開始する
        /// </summary>
        private IEnumerator FirstTurn()
        {
            if (!isStandaloneMode)
            {

                // Eventがある場合は待つ
                StartCoroutine(eventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.BeforeBattle,
                                                                        gameManager.ReachedEventArgs.Enemy.ID));
                if (eventSceneController.IsEventWindowActive)
                {
                    cameraController.enableCameraControlling = false;
                    sceneState = SceneState.EVENT;
                    while (eventSceneController.IsEventWindowActive)
                        yield return null;
                    cameraController.enableCameraControlling = true;
                }
            }
            Print("Start first turn");
            PauseTactics = false;

            var unitsCoroutine = StartCoroutine(unitsController.FirstTurn());
            tilesController.StartTurn(ActiveUnit);
            yield return unitsCoroutine;

            // 各種OverlayUIを有効化する
            overlayCanvas.gameObject.SetActive(true);
            overlayCanvasGroup.DOFade(1, 0.5f);

            UserController.enableCursor = false;

            StartCoroutine( SelectItemPanel.SetItems(ActiveUnit));
            sceneState = SceneState.WAIT;
            IsGameRunning = true;

            if (ActiveUnit.Attribute == UnitAttribute.PLAYER)
            {
                orderOfActionController.SetOrder(unitsController.GetTurnList());
            }
        }

        /// <summary>
        /// ターンを次のUnitに送る
        /// </summary>
        /// <returns></returns>
        private IEnumerator NextTurn()
        {
            while (sceneState == SceneState.EVENT)
                yield return null;

            if (sceneState == SceneState.FINISHED)
                yield break;

            sceneState = SceneState.LOCK;

            var previousUnit = unitsController.activeUnit;

            //yield return StartCoroutine(UnitsController.WaitActionUnitAnimations());

            // 勝利条件の確認 (Kill, Reach等の勝利条件を確認)
            victoryConditions.CheckGameState();
            if (victoryConditions.sceneState != VictoryConditions.GameResult.Playing)
            {
                sceneState = SceneState.FINISHED;
                // VictoryConditionsによってGameが終了したことを伝える
                Print("===> Endgame", victoryConditions.sceneState);
                yield break ;
            }

            yield return StartCoroutine(unitsController.NextTurn());

            if (unitsController.activeUnit == null)
            {
                // ActiveUnitがnullになり
                // ターンが終了した
                victoryConditions.CheckGameState();
                sceneState = SceneState.FINISHED;
                Print("===> Endgame2", victoryConditions.sceneState);
                yield break;
            }

            if (previousUnit.Attribute == UnitAttribute.ENEMY && 
                unitsController.activeUnit.Attribute == UnitAttribute.PLAYER)
            {
                // EnemyからPlayerへターンが移った
                myInfoCanvasGroup.DOFade(1, 0.5f);
                orderOfActionController.SetOrder(unitsController.GetTurnList());


            }else if (previousUnit.Attribute == UnitAttribute.PLAYER &&
                      unitsController.activeUnit.Attribute == UnitAttribute.ENEMY)
            {
                // PlayerからEnemyにTurnが移った
                if (previousUnit.isAiControlled)
                    myInfoCanvasGroup.DOFade(0, 0.5f);
                orderOfActionController.Clear();
            }
            else if(previousUnit.Attribute == UnitAttribute.PLAYER)
            {
                // Playerのターンが進行中
                orderOfActionController.RemoveFirst(null);
            }

            // 勝利条件の確認 Turn経過等を見る
            victoryConditions.CheckGameState();
            if (victoryConditions.sceneState != VictoryConditions.GameResult.Playing)
            {
                // VictoryConditionsによってGameが終了したことを伝える
                sceneState = SceneState.FINISHED;
                Print("===> Endgame3", victoryConditions.sceneState);
                yield break;
            }

            // SquareControllerで現地点のSquareから出られるようにする
            tilesController.StartTurn(unitsController.activeUnit);

            yield return new WaitForSeconds(1f);
            if (ActiveUnit.WorkState == WorkState.Gimmick)
            {
                if (ActiveUnit.GimmickObject is MortarGimmick mortarGimmick)
                {
                    StartCoroutine(SelectItemPanel.SetItem(mortarGimmick));
                }
            }
            else
            {
                StartCoroutine(SelectItemPanel.SetItems(ActiveUnit));
            }

            //cameraController.enableCameraControlling = ActiveUnit.isAiControlled;

            sceneState = SceneState.WAIT;
        }

        /// <summary>
        /// キーボードのNumber入力を監視して 変更があればアイテムの所持を切り替える
        /// FocusModeの場合Focusのタイプを変更する
        /// </summary>
        private void ChangeItemControl()
        {
            // Wait以外では何らかの動作中
            if (sceneState != SceneState.WAIT || ActiveUnit == null)
                return;

            if (ActiveUnit == null || ActiveUnit.WorkState == WorkState.Gimmick)
                return;

            var isSetNewItem = false;
            for(var i=1; i < 5; i++)
            {
                if (!UserController.NumberCodeDown[i])
                    continue;

                var items = ActiveUnit.itemController.GetAllItemHolders();
                if (items.Count != 0) 
                {
                    var currentHolder = ActiveUnit.itemController.CurrentItemHolder;
                    if (currentHolder != null || currentHolder.Data != null)
                    {
                        if (items.IndexAt(i-1, out var newHolder))
                        {
                            if (newHolder.RemainingActionCount == 0)
                            {
                                // 残弾数が0の場合は使用できない
                                SelectItemPanel.ShakeItemHolder(newHolder);
                                StartCoroutine(unitsController.focusModeUI.ShowBottomMessage("No ammo", newHolder.Data.Name, 3));
                                return;
                            }
                            else
                            {
                                StartCoroutine(ActiveUnit.itemController.SetItem(newHolder));
                                unitsController.NortifyItemChanged();
                                SelectItemPanel.SetItemToUse(newHolder);
                                isSetNewItem = true;
                            }
                        }
                    }
                }
            }

            if (isSetNewItem && ActiveUnit.WorkState == WorkState.Focus)
                unitsController.StartFocusMode();
        }

        /// <summary>
        /// すべてのTactisが一時停止している
        /// </summary>
        internal bool IsStopped
        {
            get => isStopped;
            set
            {
                if (isStopped == value) return;
                isStopped = value;
                if (value)
                {
                    Physics.autoSimulation = false;
                    unitsController.IsStopped = true;
                    
                }
                else
                {
                    Physics.autoSimulation = true;
                    unitsController.IsStopped = false;
                }

            }
        }
        private bool isStopped = false;

        #endregion


        #region Callled

        /// <summary>
        /// AIの操作が終了したことを伝えるHandlerFunction
        /// </summary>
        private void EndAIControlling(object o, EventArgs args)
        {
            if (sceneState == SceneState.FINISHED)
                return;
            sceneState = SceneState.LOCK;
            StartCoroutine(NextTurn());
        }

        /// <summary>
        /// ActiveUnitのactionが終了しそれにたいするすべてのUnitのanimationが終了した再呼び出し
        /// </summary>
        /// <param name="allUnitsActionArgs"></param>
        private void CalledEndActiveUnitAction(object sender, EndAllUnitsActionArgs allUnitsActionArgs)
        {
            // 勝利条件の確認 (Kill, Reach等の勝利条件を確認)
            victoryConditions.CheckGameState();
            if (victoryConditions.sceneState != VictoryConditions.GameResult.Playing)
            {
                sceneState = SceneState.FINISHED;
                // VictoryConditionsによってGameが終了したことを伝える
                Print("===> Endgame", victoryConditions.sceneState);
                return;
            }

            // ターン終了している場合は次のターンへ
            if (allUnitsActionArgs.isTurnCompleted)
                StartCoroutine(NextTurn());
        }

        /// <summary>
        /// 勝敗がついた際にVictoryConditionsから呼び出される
        /// </summary>
        private IEnumerator EndGame(VictoryConditions.GameResult state)
        {
            cameraController.enableCameraControlling = false;

            // TODO 勝利時のアニメーション

            var easeDuration = 0.3f;
            overlayCanvasGroup.DOFade(0, 0.3f).OnComplete(() =>
            {
                overlayCanvas.gameObject.SetActive(false);
            });

            unitsController.EndGame();

            IsGameRunning = false;
            sceneState = SceneState.FINISHED;

            yield return new WaitForSeconds(easeDuration);

            // Tactics終了後イベント
            if (!isStandaloneMode)
            {
                StartCoroutine(eventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.AfterBattle,
                                        gameManager.ReachedEventArgs.Enemy.ID,
                                        state));
                if (eventSceneController.IsEventWindowActive)
                {
                    sceneState = SceneState.EVENT;
                    while (eventSceneController.IsEventWindowActive)
                        yield return null;
                    sceneState = SceneState.FINISHED;
                }
            }


            var result = new UI.BattleResult();
            result.state = state;
            result.units = unitsController.UnitsList.FindAll(u => u.Attribute == UnitAttribute.PLAYER).ConvertAll(u => u.CurrentParameter.Data);

            result.deadUnits = unitsController.OriginUnitsList.ToList().FindAll(u =>
            {
                return u.Attribute == UnitAttribute.PLAYER && u.CurrentParameter.HealthPoint <= 0;
            }).ConvertAll(c => c.CurrentParameter.Data);

            result.killedEnemies = unitsController.OriginUnitsList.ToList().FindAll(u =>
            {
                return u.Attribute == UnitAttribute.ENEMY && u.CurrentParameter.HealthPoint <= 0;
            }).ConvertAll(c => c.CurrentParameter.Data);

            if (gameManager.ReachedEventArgs == null)
                result.baseExp = 0;
            else
                result.baseExp = gameManager.ReachedEventArgs.Enemy.exp;

            result.numberOfTurn = TurnCount;

            // Tabletの表示
            void SetTabletLosePosition()
            {
                tacticsTablet.transform.SetPositionAndRotation(loseResultTransform.position, loseResultTransform.rotation);
                loseResultCamera.Priority = 100;
            }

            void SetTabletWinPosition(Character.UnitController unit)
            {
                // TODO アニメーション付きでUnitが取り出したかのようにTabletを表示する
                tacticsTablet.transform.SetPositionAndRotation(unit.tabletPosition.position, unit.tabletPosition.rotation);
                unit.watchTabletCamera.Priority = 100;
            }

            if (state == VictoryConditions.GameResult.Win)
            {
                if (!ActiveUnit.IsDead && ActiveUnit.Attribute == UnitAttribute.PLAYER)
                    SetTabletWinPosition(ActiveUnit);
                else if (unitsController.UnitsList.TryFindFirst(u => u.Attribute == UnitAttribute.PLAYER, out var u))
                    SetTabletWinPosition(u);
                else
                    SetTabletLosePosition();
                StartCoroutine(tacticsTablet.Show());
                resultPanel.Show(result);
            }
            else if (state == VictoryConditions.GameResult.Lose)
            {
                SetTabletLosePosition();
                StartCoroutine(tacticsTablet.Show());
                resultPanel.Show(result);
            }
        }
        #endregion


        private void OnGUI()
        {
            debugController?.Run();
        }
    }

    /// <summary>
    /// 操作の状態
    /// </summary>
    enum SceneState
    {
        INIT = 0,
        /// <summary>
        /// ユーザーの入力待機
        /// </summary>
        WAIT,
        /// <summary>
        /// 入力をロック
        /// </summary>
        LOCK,
        /// <summary>
        /// AIが入力中　ユーザー入力はロック
        /// </summary>
        AI_CONTROL,
        /// <summary>
        /// 準備画面を表示しユーザーの入力を待機中
        /// </summary>
        PREPARE,
        /// <summary>
        /// 準備終了後のロード中
        /// </summary>
        PREPARE_LOAD,
        /// <summary>
        /// 勝敗が決している状態
        /// </summary>
        FINISHED,
        DEBUG,
        /// <summary>
        /// EventWindowが表示されている状態
        /// </summary>
        EVENT,
        /// <summary>
        /// TacticsTabletが表示されている状態
        /// </summary>
        TABLET,
    }

    /// <summary>
    /// ユニットの敵味方識別用
    /// </summary>
    [Serializable]
    public enum UnitAttribute: int
    {
        PLAYER,
        NPC,
        ENEMY,
    }
}


/// <summary>
/// Debug用の関数をまとめたクラス
/// </summary>
public class DebugController
{
    private MainMapController mainMapController;
    private TacticsController tacticsController;
    private GameManager gameManager;

    public DebugController(TacticsController tacticsController)
    {
       this.tacticsController = tacticsController;
        gameManager = GameManager.Instance;
    }

    public DebugController(MainMapController mainMapController)
    {
        this.mainMapController = mainMapController;
        gameManager = GameManager.Instance;
    }


    /// <summary>
    /// DebugFunctionが必要な場合はこれを呼び出す OnGUIで呼び出す
    /// </summary>
    public void Run()
    {

        if (gameManager.DebugLogManager == null)
            return;
        if (!gameManager.DebugLogManager.PopupEnabled)
            return;
        string command = Console.ReadLine();
        if (command == null || command.Length == 0)
            return;
        command.ToLower();
        var commandList = command.Split(' ').ToList();

        if (tacticsController)
        {
            switch (commandList[0])
            {
                case "disable":
                    DebugDisableTileWalls(commandList);
                    break;
                case "get":
                    GetSquareInUnit(commandList);
                    break;
                case "ai":
                    DebugAiControlling(tacticsController.ActiveUnit.Attribute, true);
                    break;
                case "showinfo":
                    DebugShowUnitInfo(commandList);
                    break;
                case "kill":
                    DebugKill(commandList);
                    break;
            }
        }
        else if (mainMapController)
        {

        }
    }

    #region Debug On Tactics
    /// <summary>
    /// コライダーの付いた壁とトリガーを無効化　プレイヤーが自由に動ける
    /// </summary>
    /// <param name="attribute"></param>
    private void DebugDisableTileWalls(List<string> command)
    {
        if (command.Count < 2)
            return;
        if (command[1].Equals("disable"))
        {
            tacticsController.tilesController.DisableAllTiles();
            Print("All tiles are disabled");
        }
        else if (command[1].Equals("enable"))
        {
            var tile = tacticsController.tilesController.StartTurn(tacticsController.ActiveUnit, true);
            if (tile == null)
                Print($"Unit {tacticsController.ActiveUnit.CurrentParameter.Data.Name} isn't in tile.");
            else
                Print($"Enable tiles: {tacticsController.ActiveUnit.CurrentParameter.Data.Name} in {tile.id}");
        }
    }

    /// <summary>
    /// ユニットがどのTileに存在するかチェック
    /// </summary>
    private void GetSquareInUnit(List<string> command)
    {
        var tile = tacticsController.ActiveUnit.tileCell;
        if (tile == null)
            Print($"Unit {tacticsController.ActiveUnit.CurrentParameter.Data.Name} isn't in tile.");
        else
            Print($"{tacticsController.ActiveUnit.CurrentParameter.Data.Name} is in {tile.id}");
    }

    private void DebugAiControlling(UnitAttribute attribute, bool isControlled)
    {
        tacticsController.unitsController.EnableAIControlling(isControlled, attribute);
    }

    private void DebugShowUnitInfo(List<string> command)
    {
        if (tacticsController.ActiveUnit == null)
        {
            Print("Active Unit is null.");
            return;
        }

        Print(tacticsController.ActiveUnit.GetInfo());
    }

    private void DebugKill(List<string> command)
    {

    }
    #endregion
}
