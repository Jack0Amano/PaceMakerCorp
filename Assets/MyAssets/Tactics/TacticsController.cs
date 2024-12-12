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
        [SerializeField] UI.TacticsTablet tacticsTablet;

        [Header("Result UI")]
        [SerializeField] UI.ResultPanel resultPanel;
        [Tooltip("負けた場合にTabletが地面に置かれる位置")]
        [SerializeField] Transform loseResultTransform;
        [Tooltip("負けた場合にTabletが置かれそれを映すカメラ")]
        [SerializeField] CinemachineVirtualCamera loseResultCamera;
        [SerializeField] float LoseTabletScale;
        [SerializeField] float WinTabletScale;

        [Header("Preparing UI")]
        [SerializeField] Prepare.PreparePanel preparePanel;

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
        [SerializeField] bool IsStandaloneMode = false;
        [Tooltip("Debug用のTacticsの属してるMapのname")]
        [SerializeField] string DebugMapSceneName;
        /// <summary>
        /// ゲームの進行状況
        /// </summary>
        [SerializeField, ReadOnly] private SceneState sceneState = SceneState.INIT;

        GeneralParameter Parameters;
        GameManager GameManager;

        private Character.UnitsController UnitsController;
        /// <summary>
        /// 現在行動中のUnit
        /// </summary>
        private Character.UnitController ActiveUnit
        {
            get => UnitsController.activeUnit;
        }
        /// <summary>
        /// Tactics画面のゲームが動作状態にあるかどうか
        /// </summary>
        static public bool IsGameRunning { private set; get; } = false;

        /// <summary>
        /// 勝利条件Controller
        /// </summary>
        private VictoryConditions VictoryConditions;

        /// <summary>
        /// ロードが完了して最初の一回だけ実行
        /// </summary>
        private bool LoadCompleted = false;
        /// <summary>
        /// ターン経過をカウントする
        /// </summary>
        public int TurnCount
        {
            get => UnitsController.AttributeTurnCount;
        }

        /// <summary>
        /// デバッグ用のコントロール画面
        /// </summary>
        private DebugController DebugController;
        /// <summary>
        /// <c>debugController</c>が有効化されているか
        /// </summary>
        private bool PrepareDebugMode = false;

        /// <summary>
        /// Tactics画面がポーズされている状態かどうか
        /// </summary>
        bool pauseTactics
        {
            get => pauseTactics;
            set
            {
                UnitsController.PauseAllAnimation = value;
                Physics.simulationMode = value ?  SimulationMode.Script : SimulationMode.FixedUpdate;
                PauseTactics = value;
            }
        }
        /// <summary>
        /// Tactics画面がポーズされている状態かどうか
        /// </summary>
        static public bool PauseTactics = false;

        /// <summary>
        /// SettingCanvasが表示中か
        /// </summary>
        private bool IsSettingCanvasEnable = false;

        /// <summary>
        /// OverlayCanvasのalpha
        /// </summary>
        CanvasGroup OverlayCanvasGroup;

        /// <summary>
        /// 割り込み型のストーリーイベントを管理する
        /// </summary>
        EventScene.EventSceneController eventSceneController;

        /// <summary>
        /// 時間や天気を管理する
        /// </summary>
        NaturalEnvironmentController naturalEnvironmentController;
        #endregion


        #region Init
        // Start is called before the first frame update
        private void Awake()
        {

            IsGameRunning = false;

            // Get script in UnitContainer
            UnitsController = unitsObject.GetComponent<Character.UnitsController>();
            UnitsController.tilesController = tilesController;
            UnitsController.selectItemPanel = SelectItemPanel;

            // Victory conditions
            VictoryConditions = gameObject.GetComponent<VictoryConditions>();
            VictoryConditions.endGameAction += (arg => StartCoroutine(EndGame(arg)));
            // TODO: victoryConditionsの仮の勝利条件
            VictoryConditions.mode = VictoryConditions.Mode.KILL_ALL;

            naturalEnvironmentController = GetComponent<NaturalEnvironmentController>();

            GameManager = GameManager.Instance;
            Parameters = GameManager.GeneralParameter;

            preparePanel.TilesController = tilesController;

            if (IsStandaloneMode)
                GameManager.LoadDebugData(DebugMapSceneName);

            tacticsTablet.tilesController = tilesController;
            tacticsTablet.cameraController = cameraController;
            cameraController.TacticsTablet = tacticsTablet;
        }

        void Start()
        {
            eventSceneController = GameManager.EventSceneController;
            sceneState = SceneState.PREPARE;
            preparePanel.startBattleButton.onClick.AddListener(() => StartGame());
            // Prepareが終わるまでここで待ってる
            StartCoroutine(Load());

            //timeController.PauseTime();
            // var gameTime = GameManager.DataSavingController.SaveDataInfo.gameTime.Value;
            //if (GameController.Instance.data.saveDataInfo != null)
            //    gameTime = GameController.Instance.data.saveDataInfo.gameTime;
            //timeController.SetDate(gameTime.Year, gameTime.Month, gameTime.Day);
            //var timeline = (float)gameTime.Hour + ((float)gameTime.Minute / 60);
            //timeController.SetTimeline(timeline);

            DebugController = GameManager.debugController;
            DebugController.disableTileWalls += DebugDisableTileWalls;
            DebugController.getSquareInUnit += DebugGetSquareInUnit;
            DebugController.endTactics += DebugEndTactics;
            DebugController.showPrepare += DebugShowPrepare;
            DebugController.tacticsAI += DebugAiControlling;
            DebugController.unitInfo += DebugShowUnitInfo;
            DebugController.rotation += DebugRotate;
            

            unitListWindow.hideButtonAction += (() =>
            {
                Time.timeScale = 1;
                cameraController.enableCameraControlling = true;
                UserController.enableCursor = false;
                sceneState = SceneState.WAIT;
            });

            OverlayCanvasGroup = overlayCanvas.GetComponent<CanvasGroup>();
            OverlayCanvasGroup.alpha = 0;
            overlayCanvas.gameObject.SetActive(false);

            resultPanel.ReturnToPrepare = (() => ReturnToPrepare());

            UnitsController.endAllUnitsActionHandler += CalledEndActiveUnitAction;

            
        }

        /// <summary>
        /// 各種データのロード
        /// </summary>
        private IEnumerator Load()
        {
            // Preparepanelからエンカウント内容を取得
            while (preparePanel.Encount == null)
                yield return null;
            var encount = preparePanel.Encount;
            
            // encount時刻に設定する
            naturalEnvironmentController.SetDateTime(encount.DateTime);

            var tilesCoroutine = StartCoroutine(tilesController.AsyncLoad());
            // 敵ユニットを配置
            var unitsCoroutine = StartCoroutine(UnitsController.SetUnitsAsEnemy(encount.Enemy.tileAndUnits));
            
            // CameraUserControllerが初期化されるまで待つ
            while (!cameraController.IsActivated)
                yield return null;

            cameraController.ChangeModeStartTablet();
            // StandAloneModeではTabletの位置は初期位置でイベントはない
            if (!IsStandaloneMode)
            {
                // Tabletの位置をUnitのスタート位置にする
                tacticsTablet.SetStartPosition(encount.PlayerPositions.First());

                // Eventがある場合は待つ
                StartCoroutine(eventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.OnPrepare,
                                      encount.Enemy.encounmtSpawnID));
                GameManager.NortifyCompleteToLoad(0.5f);
                if (eventSceneController.IsEventWindowActive)
                {
                    Print($"Event is active");
                    sceneState = SceneState.EVENT;
                    while (eventSceneController.IsEventWindowActive)
                        yield return null;
                    sceneState = SceneState.PREPARE;
                }
            }

            // ここでPreparePanelのStartを待つ
            while (sceneState != SceneState.PREPARE_LOAD)
                yield return null;

            // tabletを下げて非表示にする
            StartCoroutine( tacticsTablet.Hide());

            yield return unitsCoroutine;
            Print($"Try to set {preparePanel.PlayerResults.Count} units\n", string.Join("\n", preparePanel.PlayerResults));
            unitsCoroutine = StartCoroutine( UnitsController.SetUnits(preparePanel.PlayerResults));

            yield return tilesCoroutine;
            yield return unitsCoroutine;

            Print($"Complate to load units");
            UnitsController.CompleteLoad();

            IEnumerator AttachSquareAndUnits()
            {
                yield return null;
                yield return StartCoroutine(tilesController.UnitLoaded());
            }

            yield return StartCoroutine(AttachSquareAndUnits());

            LoadCompleted = true;

            sceneState = SceneState.INIT;

            StartCoroutine(FirstTurn());
        }

        #endregion

        /// <summary>
        /// Preapareパネルのゲーム開始ボタンが選択されたとき呼び出し
        /// </summary>
        private void StartGame()
        {
            sceneState = SceneState.PREPARE_LOAD;
        }

        // Update is called once per frame
        private void Update()
        {
            if (sceneState == SceneState.PREPARE || sceneState == SceneState.PREPARE_LOAD || sceneState == SceneState.EVENT)
                return;

            if (!LoadCompleted) return;

            if (PrepareDebugMode != DebugController.IsActive)
            {
                PrepareDebugMode = DebugController.IsActive;
                if (DebugController.IsActive)
                {
                    // デバッグモード開始
                    sceneState = SceneState.DEBUG;
                    cameraController.enableCameraControlling = false;
                    pauseTactics = true;
                }
                else
                {
                    // デバッグモード終了
                    sceneState = SceneState.WAIT;
                    cameraController.enableCameraControlling = true;
                    pauseTactics = false;
                }
            }

            if (GameManager.StartCanvasController.IsEnable != IsSettingCanvasEnable)
            {
                if (GameManager.StartCanvasController.IsEnable)
                {
                    // GameSettingCanvasが表示中になった
                    cameraController.enableCameraControlling = false;
                    UserController.enableCursor = true;
                    pauseTactics = true;
                    IsSettingCanvasEnable = true;
                }
                else
                {
                    // GameSettingCanvasが非表示になった
                    cameraController.enableCameraControlling = true;
                    UserController.enableCursor = false;
                    pauseTactics = false;
                    IsSettingCanvasEnable = false;
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

            // Sceneがロックされている場合break
            if (sceneState == SceneState.LOCK)
                return;


            // ===================== ユニットのターン遷移 =====================
            // Skip turn
            if (UserController.KeyCodeSpace)
            {
                if (!ActiveUnit.isAiControlled)
                {
                    // Playerのturnをspaceでスキップしようとしている
                    UnitsController.EndTurnFromPlayerControl();
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

                    if (!UnitsController.IsFocusMode)
                    {
                        // フォーカスモードに入る
                        UnitsController.StartFocusMode();
                    }
                    else
                    {
                        // フォーカスモードから出る
                        UnitsController.EndFocusMode();
                    }
                }
                else if (UserController.KeyUseItem &&
                         ActiveUnit.CanUseGimmickObject && 
                         !UnitsController.IsFocusMode && 
                         !ActiveUnit.isAlreadyActioned)
                {
                    // Gimmickを使用する
                    UnitsController.StartGimmickMode(ActiveUnit.GimmickObject);
                }
                else if (UserController.KeyUseItem && ActiveUnit.IsUsingGimmickObject && !UnitsController.IsFocusMode)
                {
                    // Gimmickを使用中であるためこれから離れる動作
                    UnitsController.CancelGimmickMode();
                }
            }

            // MouseWheelはカメラの距離移動
            // 使用アイテムをMouseWheelで切り替える
            ChangeItemControl();
        }

        #region Function for control

        /// <summary>
        /// 最初のターンを開始する
        /// </summary>
        private IEnumerator FirstTurn()
        {

            if (!IsStandaloneMode)
            {
                // Eventがある場合は待つ
                StartCoroutine(eventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.BeforeBattle,
                                                                        GameManager.Encounter.Enemy.encounmtSpawnID));
                if (eventSceneController.IsEventWindowActive)
                {
                    Print($"FirstTurn UnitCount:{string.Join(",", UnitsController.UnitsList)}");
                    cameraController.enableCameraControlling = false;
                    sceneState = SceneState.EVENT;
                    while (eventSceneController.IsEventWindowActive)
                        yield return null;
                    cameraController.enableCameraControlling = true;
                }
            }

            var unitsCoroutine = StartCoroutine(UnitsController.FirstTurn());
            tilesController.StartTurn(ActiveUnit);
            yield return unitsCoroutine;

            // 各種OverlayUIを有効化する
            overlayCanvas.gameObject.SetActive(true);
            OverlayCanvasGroup.DOFade(1, 0.5f);

            UserController.enableCursor = false;

            StartCoroutine( SelectItemPanel.SetItems(ActiveUnit));
            sceneState = SceneState.WAIT;
            IsGameRunning = true;

            if (ActiveUnit.Attribute == UnitAttribute.PLAYER)
            {
                orderOfActionController.SetOrder(UnitsController.GetTurnList());
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

            var previousUnit = UnitsController.activeUnit;

            //yield return StartCoroutine(UnitsController.WaitActionUnitAnimations());

            // 勝利条件の確認 (Kill, Reach等の勝利条件を確認)
            VictoryConditions.CheckGameState();
            if (VictoryConditions.sceneState != VictoryConditions.GameResult.Playing)
            {
                sceneState = SceneState.FINISHED;
                // VictoryConditionsによってGameが終了したことを伝える
                Print("===> Endgame", VictoryConditions.sceneState);
                yield break ;
            }

            yield return StartCoroutine(UnitsController.NextTurn());

            if (UnitsController.activeUnit == null)
            {
                // ActiveUnitがnullになり
                // ターンが終了した
                VictoryConditions.CheckGameState();
                sceneState = SceneState.FINISHED;
                Print("===> Endgame2", VictoryConditions.sceneState);
                yield break;
            }

            if (previousUnit.Attribute == UnitAttribute.ENEMY && 
                UnitsController.activeUnit.Attribute == UnitAttribute.PLAYER)
            {
                // EnemyからPlayerへターンが移った
                myInfoCanvasGroup.DOFade(1, 0.5f);
                orderOfActionController.SetOrder(UnitsController.GetTurnList());


            }else if (previousUnit.Attribute == UnitAttribute.PLAYER &&
                      UnitsController.activeUnit.Attribute == UnitAttribute.ENEMY)
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
            VictoryConditions.CheckGameState();
            if (VictoryConditions.sceneState != VictoryConditions.GameResult.Playing)
            {
                // VictoryConditionsによってGameが終了したことを伝える
                sceneState = SceneState.FINISHED;
                Print("===> Endgame3", VictoryConditions.sceneState);
                yield break;
            }

            // SquareControllerで現地点のSquareから出られるようにする
            tilesController.StartTurn(UnitsController.activeUnit);

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
                                StartCoroutine(UnitsController.focusModeUI.ShowBottomMessage("No ammo", newHolder.Data.Name, 3));
                                return;
                            }
                            else
                            {
                                StartCoroutine(ActiveUnit.itemController.SetItem(newHolder));
                                UnitsController.NortifyItemChanged();
                                SelectItemPanel.SetItemToUse(newHolder);
                                isSetNewItem = true;
                            }
                        }
                    }
                }
            }

            if (isSetNewItem && ActiveUnit.WorkState == WorkState.Focus)
                UnitsController.StartFocusMode();
        }

        /// <summary>
        /// すべてのTactisが一時停止している
        /// </summary>
        internal bool IsStopped
        {
            get => _isStopped;
            set
            {
                if (_isStopped == value) return;
                _isStopped = value;
                if (value)
                {
                    Physics.autoSimulation = false;
                    UnitsController.IsStopped = true;
                    
                }
                else
                {
                    Physics.autoSimulation = true;
                    UnitsController.IsStopped = false;
                }

            }
        }
        private bool _isStopped = false;

        #endregion

        #region Debugmode
        /// <summary>
        /// コライダーの付いた壁とトリガーを無効化　プレイヤーが樹有に動ける
        /// </summary>
        /// <param name="attribute"></param>
        private void DebugDisableTileWalls(List<string> command)
        {
            if (command.Count < 2)
                return;
            if (command[1].Equals("disable"))
            {
                tilesController.DisableAllTiles();
                DebugController.AddText("All tiles are disabled");
            }
            else if (command[1].Equals("enable"))
            {
                var tile = tilesController.StartTurn(ActiveUnit, true);
                if (tile == null)
                    DebugController.AddText($"Unit {ActiveUnit.CurrentParameter.Data.Name} isn't in tile.");
                else
                    DebugController.AddText($"Enable tiles: {ActiveUnit.CurrentParameter.Data.Name} in {tile.id}");
            }
        }

        /// <summary>
        /// ユニットがどのTileに存在するかチェック
        /// </summary>
        private void DebugGetSquareInUnit(List<string> command)
        {
            var tile = ActiveUnit.tileCell;
            if (tile == null)
                DebugController.AddText($"Unit {ActiveUnit.CurrentParameter.Data.Name} isn't in tile.");
            else
                DebugController.AddText($"{ActiveUnit.CurrentParameter.Data.Name} is in {tile.id}");
        }

        /// <summary>
        /// Tactics画面を終了する
        /// </summary>
        private void DebugEndTactics(List<string> command)
        {
            var state = VictoryConditions.GameResult.Playing;
            if (command.Count >= 2 && command[1] == "win")
                state = VictoryConditions.GameResult.Win;
            else if (command.Count >= 2 && command[1] == "lose")
                state = VictoryConditions.GameResult.Lose;
            else
                return;

            StartCoroutine(EndGame(state));

            StartCoroutine( DebugController.Hide(1.5f, () =>
            {
                UserController.enableCursor = true;
            }));
        }

        /// <summary>
        /// Tactics画面から準備画面に戻る
        /// </summary>
        private void DebugShowPrepare(List<string> command)
        {
            ReturnToPrepare();
        }
        
        private void DebugAiControlling(UnitAttribute attribute, bool isControlled)
        { 
            UnitsController.EnableAIControlling(isControlled, attribute);
        }

        private void DebugShowUnitInfo(List<string> command)
        {
            if (ActiveUnit == null)
            {
                DebugController.AddText("Active Unit is null.");
                return;
            }
            
            DebugController.AddText(ActiveUnit.GetInfo());
        }

        private void DebugRotate(List<string> command)
        {

            //if (command.Count == 2)
            //{
            //    if (int.TryParse(command[1], out int degree))
            //    {
                    
            //    }
            //}
            //else
            //    debugController.AddText("Arguments error: rotation (degree)");
        }

        private void DebugKill(List<string> command)
        {

        }
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
            VictoryConditions.CheckGameState();
            if (VictoryConditions.sceneState != VictoryConditions.GameResult.Playing)
            {
                sceneState = SceneState.FINISHED;
                // VictoryConditionsによってGameが終了したことを伝える
                Print("===> Endgame", VictoryConditions.sceneState);
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
            OverlayCanvasGroup.DOFade(0, 0.3f).OnComplete(() =>
            {
                overlayCanvas.gameObject.SetActive(false);
            });

            UnitsController.EndGame();

            IsGameRunning = false;
            sceneState = SceneState.FINISHED;

            yield return new WaitForSeconds(easeDuration);

            // Tactics終了後イベント
            if (!IsStandaloneMode)
            {
                StartCoroutine(eventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.AfterBattle,
                                        GameManager.Encounter.Enemy.encounmtSpawnID,
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
            result.units = UnitsController.UnitsList.FindAll(u => u.Attribute == UnitAttribute.PLAYER).ConvertAll(u => u.CurrentParameter.Data);

            result.deadUnits = UnitsController.OriginUnitsList.ToList().FindAll(u =>
            {
                return u.Attribute == UnitAttribute.PLAYER && u.CurrentParameter.HealthPoint <= 0;
            }).ConvertAll(c => c.CurrentParameter.Data);

            result.killedEnemies = UnitsController.OriginUnitsList.ToList().FindAll(u =>
            {
                return u.Attribute == UnitAttribute.ENEMY && u.CurrentParameter.HealthPoint <= 0;
            }).ConvertAll(c => c.CurrentParameter.Data);

            if (GameManager.Encounter == null)
                result.baseExp = 0;
            else
                result.baseExp = GameManager.Encounter.Enemy.exp;

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
                else if (UnitsController.UnitsList.TryFindFirst(u => u.Attribute == UnitAttribute.PLAYER, out var u))
                    SetTabletWinPosition(u);
                else
                    SetTabletLosePosition();

                tacticsTablet.Show();
                resultPanel.Show(result);
            }
            else if (state == VictoryConditions.GameResult.Lose)
            {
                SetTabletLosePosition();
                tacticsTablet.Show();
                resultPanel.Show(result);
            }
        }
        #endregion


        /// <summary>
        /// Tacticsを初期状態にし準備画面に戻る
        /// </summary>
        private void ReturnToPrepare()
        {
            sceneState = SceneState.PREPARE;
            tilesController.Clear();
            UnitsController.ClearUnitsController();
            preparePanel.gameObject.SetActive(true);
            resultPanel.Hide();
            overlayCanvas.gameObject.SetActive(false);
            tacticsTablet.Show();
            StartCoroutine(Load());
            preparePanel.CanvasGroup.DOFade(1, 0.5f);

            StartCoroutine( DebugController.Hide(1, () =>
            {
                sceneState = SceneState.PREPARE;
                UserController.enableCursor = true;
                UserController.updateKeyInput = true;
            }));
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
        EVENT
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