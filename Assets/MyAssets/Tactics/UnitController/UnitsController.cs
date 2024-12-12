using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;
using Tactics.Control;
using UnityEngine.AI;
using static Utility;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using Tactics.Prepare;
using AIGraph.Editor;
using Unity.Logging;
using UnityEngine.Rendering;
using Tactics.Object;
using DG.Tweening;
using Unity.VisualScripting;
using Tactics.UI.Overlay;

namespace Tactics.Character
{
    //[RequireComponent(typeof(Button))]

    /// <summary>
    /// 主にユニットのターン経過を管理する
    /// </summary>
    public class UnitsController : MonoBehaviour
    {
        #region SerializeField
        [Header("Controllers")]
        [SerializeField] public CameraUserController CameraUserController;
        [SerializeField] public UI.Overlay.BottomUIPanel focusModeUI;
        [SerializeField] public UI.Overlay.HUDWindow hudWindow;
        [SerializeField] UI.Overlay.MyInfoWindow myInfoWindow;
        [SerializeField] AimLineController aimLineController;
        [SerializeField] Arc.DrawArc arcController;
        [SerializeField] UI.Overlay.FindOutUI.FindOutPanel FindOutPanel;
        
        [Header("GameObjects")]
        [Tooltip("Worldに設置されたアイテムのParent")]
        [SerializeField] GameObject WorldItemParent;
        [SerializeField] GameObject waysParent;
        
        [Header("Turn")]
        [Tooltip("現在進行中の勢力のAttribute")]
        [SerializeField, ReadOnly] public UnitAttribute TurnOfAttribute = UnitAttribute.PLAYER;
        [Tooltip("勢力のターン経過のカウント")]
        [SerializeField, ReadOnly] public int AttributeTurnCount = 0;
        [Tooltip("勢力内のターンがどの程度進んでいるかのIndex")]
        [SerializeField, ReadOnly] internal int TurnCount = 0;

        [Header("UnitがMoveを開始したと判断するための移動距離")]
        [SerializeField] GameObject notMoveCircle;
        [SerializeField] Color notMoveCircleColor;
        [SerializeField] float maxDistForNotMoving = 0.6f;

        [Header("Parameters")]
        [Tooltip("Tpsのシーンでの環境による視界不良での目視距離の減衰係数")]
        public float VisibilityCoefficient = 1;

        [Header("Debug")]
        [Tooltip("AIの歩行ラインを表示")]
        [SerializeField] bool DrawAimWalkingLine = false;
        [SerializeField] bool ContinueTurnAtOneUnit = false;

        #endregion

        #region Parameters
        /// <summary>
        /// ユニットの行動順リスト ReloadListで更新可能
        /// </summary>
        /// Warning: TurnListを兼ねているためTurn経過等で現在Indexを使う場合は其のたびにUnitのIndexを検索する
        public List<UnitController> UnitsList { private set; get; } = new List<UnitController>();
        /// <summary>
        /// ユニットの一覧 死亡したUnitが自動で除かれるunitsListと違い開始時すべてのUnitを保持する
        /// </summary>
        internal ReadOnlyCollection<UnitController> OriginUnitsList { private set; get; }

        /// <summary>
        /// ターン経過のリスト attributeのターンが終了するたびに新たなattributeのlistが置かれる
        /// </summary>
        public List<UnitController> TurnList { private set; get; } = new List<UnitController>();

        /// <summary>
        /// 現在行動中のUnit
        /// </summary>
        internal UnitController activeUnit;
        /// <summary>
        /// Tile系をコントロールする
        /// </summary>
        internal Map.TilesController tilesController;
        /// <summary>
        /// フォーカスモードになっているか
        /// </summary>
        public bool IsFocusMode 
        { 
            get
            {
                return !(CurrentFocusMode == FocusModeType.None || CurrentFocusMode == FocusModeType.Mortar);
            }
        }
        /// <summary>
        /// ActiveUnitが現在位置から狙える敵Unit
        /// </summary>
        private List<UnitController> targetedUnits = new List<UnitController>();
        /// <summary>
        /// TilesControllerから敵の巡回ルート
        /// </summary>
        internal List<List<(Transform pos, Map.TileCell tile)>> WaysPassPoints;

        /// <summary>
        /// すべてのUnitの1ターン内での行動が終了した際の呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal delegate void EndAllUnitsActionHandler(object sender, EndAllUnitsActionArgs e);
        /// <summary>
        /// すべてのUnitの1ターン内での行動が終了した際の呼び出し
        /// </summary>
        internal EndAllUnitsActionHandler endAllUnitsActionHandler;

        /// <summary>
        /// すべてのUnitのアニメーションを停止する
        /// </summary>
        public bool PauseAllAnimation
        {
            get => TacticsController.PauseTactics;
            set
            {
                OriginUnitsList.ToList().ForEach(u => u.PauseAnimation = value);
            }
        }
        /// <summary>
        /// Unitが移動していないとみなされる範囲のサークルのマテリアル
        /// </summary>
        Material NotMoveCircleMaterial;

        GameManager GameManager;

        Dictionary<AIGraphDataContainer, AIGraph.AIGraphView> AIGraphViews = new Dictionary<AIGraphDataContainer, AIGraph.AIGraphView>();

        /// <summary>
        /// 現在のItemの使用状況
        /// </summary>
        internal FocusModeType CurrentFocusMode = FocusModeType.None;

        /// <summary>
        /// Raycastで距離を測るためのactiveUnitに対する敵Unitのリスト
        /// </summary>
        List<UnitController> TmpEnemiesToRay = new List<UnitController>();
        /// <summary>
        /// TacticsControllerで設定 アイテム選択状態を表示するPanel
        /// </summary>
        internal SelectItemPanel selectItemPanel;
        #endregion

        private void Awake()
        {
            GameManager = GameManager.Instance;
            NotMoveCircleMaterial = notMoveCircle.GetComponent<MeshRenderer>().material;
            aimLineController.unitsController = this;
        }

        [SerializeField, ReadOnly] float RayDistToEnemy;

        protected private void FixedUpdate()
        {

            // ActiveなUnitとその敵Enemyとの距離を測定
            if (activeUnit != null && activeUnit.IsActive)
            {
                TmpEnemiesToRay.ForEach(e =>
                {
                    
                    if (!e.IsDestroyed())
                    {
                        activeUnit.EnemyAndDistanceDict[e] = activeUnit.GetRayDistanceTo(e);
                        RayDistToEnemy = activeUnit.EnemyAndDistanceDict[e];
                    }
                });
            }
            else
            {
                RayDistToEnemy = 0;
            }

            // Check Not move distance
            if (activeUnit != null && !activeUnit.isAlreadyMoved)
            {
                // UnitがGimmickを使用中でそのGimmick使用中はAlreadyMovedを更新しない
                if (activeUnit.IsUsingGimmickObject && activeUnit.GimmickObject.UpdateNotMoveCirclePos)
                {
                    // そのためにnotMoveCircleの位置を更新する
                    notMoveCircle.transform.position = activeUnit.transform.position;
                }
                else
                {
                    var unitStartPos = notMoveCircle.transform.position;
                    if (Vector3.Distance(activeUnit.transform.position, unitStartPos) > maxDistForNotMoving)
                    {
                        // ActiveUnitがmaxDistForNotMoving以上に動いた場合の
                        if (activeUnit.Attribute == UnitAttribute.PLAYER)
                            StartCoroutine(NotMoveCircleMaterial.SetColor("_EmissionColor", Color.clear, GameManager.GeneralParameter.TileColorAnimationCurve));
                        activeUnit.isAlreadyMoved = true;
                    }
                }
            }

            DrawAimLines();

            UpdateCounteAttackIcon();
        }

        /// <summary>
        /// 現在カウンター攻撃可能かどうかを判定しmyInfoWindow.IsCounterAttackを更新する
        /// </summary>
        void UpdateCounteAttackIcon()
        {
            if (activeUnit == null) return;

            if (activeUnit.CurrentParameter.Data.UnitType == UnitType.Type.Tank)
            {
            }
            else
            {
                myInfoWindow.CounterAttackAutoShowHideIcon.ShowActiveIcon(activeUnit.IsCounterAttackable);
                myInfoWindow.OnCoverAutoShowHideIcon.ShowActiveIcon(activeUnit.TpsController.FollowingGimmickObject);
            }
        }

        #region Set Units
        /// <summary>
        /// TileAndUnitsPairを敵ユニットとして配置する
        /// </summary>
        /// <param name="tileAndUnitsPairs"></param>
        /// <returns></returns>
        public IEnumerator SetUnitsAsEnemy(List<Parameters.SpawnSquad.SpawnSquadData.TileAndEnemiesPair> tileAndUnitsPairs)
        {
            var results = tileAndUnitsPairs.ConvertAll(p => new PreparesResult() 
            { 
                tileID = p.tileID, 
                isPlayer = false, 
                units = p.UnitsData
            });
            yield return StartCoroutine(SetUnits(results));
        }

        /// <summary>
        /// Unitsをロードしシーンに追加する
        /// </summary> 
        public IEnumerator SetUnits(List<PreparesResult> preparedUnits )
        {
            var completedCoroutinesCount = 0;
            IEnumerator SetObjectAsUnit(UnitData unit, Transform spawnPoint, UnitAttribute attribute)
            {
                var unitObject = Instantiate(unit.Prefab, spawnPoint.position, spawnPoint.rotation, transform);
                unitObject.name = $"{unit.Name}.{attribute}";
                var unitController = unitObject.GetComponent<UnitController>();
                yield return StartCoroutine(unitController.SetUnit(unit, attribute));
                unitController.aiController.TilesController = tilesController;
                unitController.aiController.UnitsController = this;
                unitController.aiController.EndAIControllingAction = ((useItemType) => StartCoroutine(CallWhenActiveActionIsCompleted(useItemType)));
                unitController.aiController.FindOutPanel = FindOutPanel;
                unitController.aiController.MainAIGraphView = GetAIView(unit.Data.MainAIGraphDataContainer);
                unitController.aiController.AfterActionAIGraphView = GetAIView(unit.Data.AfterActionAIGraphDataContainer);
                unitController.aiController.WhileMovingAIGraphView = GetAIView(unit.Data.WhileMovingAIGraphDataContainer);
                unitController.cameraUserController = CameraUserController;
                unitController.TpsController.IsTPSControllActive = false;
                unitController.hudWindow = hudWindow;
                unitController.focusModeUI = focusModeUI;

                if (GameManager != null)
                {
                    if (attribute == UnitAttribute.ENEMY)
                        unitController.isAiControlled = GameManager.GeneralParameter.enemyAiEnable;
                    else
                        unitController.isAiControlled = GameManager.GeneralParameter.playerAiEnable;
                }
                else
                {
                    unitController.isAiControlled = attribute == UnitAttribute.ENEMY;
                }

                unitController.itemController.WorldItemParent = WorldItemParent;

                // Unit一覧に登録
                var damageHandler = new UnitController.DamageEventHandler(DamageEvent);
                unitController.damageHandler = damageHandler;

                UnitsList.Add(unitController);

                if (unitController.Attribute == UnitAttribute.ENEMY)
                {
                    if (WaysPassPoints.IndexAt(unit.RoutineWayIndex, out var passPoints))
                        unitController.aiController.WayPassPoints = passPoints;
                }

                Print($"Complete to set {unitController}");

                completedCoroutinesCount++;
            }

            var coroutines = new List<Coroutine>();

            var totalCoroutinesCount = 0;
            // UnitのPositionを決定して設置
            foreach (var r in preparedUnits)
            {
                var tileID = r.tileID;

                var tile = tilesController.GetTileCellWithID(r.tileID);
                if (tile == null)
                    PrintError($"{tileID} is missing");
                var containTank = r.units.Find(u => u.UnitType == UnitType.Type.Tank) != null;
                var spawnPoints = containTank ? tile.withTankSpawnPoints : tile.onlyManSpawnPoints;

                Transform spawnPoint = transform;
                var manCount = 0;
                for(var i =0; i<r.units.Count; i++)
                {
                    var unit = r.units[i];

                    if (unit.UnitType == UnitType.Type.Tank)
                    {
                        spawnPoint = spawnPoints.tankSpawnPoints[0];
                    }
                    else
                    {
                        if (spawnPoints.manSpawnPoints.IndexAt(manCount, out var t))
                            spawnPoint = t;
                        else
                            Log.Error($"Infanry of {unit} is out of range; manCount {manCount}");
                        manCount++;
                    }
                    StartCoroutine(SetObjectAsUnit(unit, spawnPoint, r.isPlayer ? UnitAttribute.PLAYER : UnitAttribute.ENEMY));
                    totalCoroutinesCount++;
                }
            }

            while(completedCoroutinesCount != totalCoroutinesCount)
            {
                yield return null;
            }
        }

        /// <summary>
        /// すべてのデータのロードと設置が終わった際にTacticsControllerから呼び出し
        /// </summary>
        internal void CompleteLoad()
        {
            UnitsList = UnitsList.OrderBy(x => x.CurrentParameter.Data.Speed + x.CurrentParameter.Data.AdditionalSpeed).ToList();
            OriginUnitsList = new List<UnitController>(UnitsList).AsReadOnly();

            // SpeedをList順に微量増やすことでSpeedの衝突を回避
            const float offset = 0.0001f;
            int count = 0;
            foreach (var unit in UnitsList)
            {
                unit.CurrentParameter.Speed += count * offset;
                count++;
            }
            ReloadTurn();
            activeUnit = UnitsList.First();
        }

        /// <summary>
        /// UnitsControllerを初期化する
        /// </summary>
        public void ClearUnitsController()
        {
            UnitsList.ForEach(u => Destroy(u.gameObject));
            UnitsList.Clear();
            activeUnit = null;
            OriginUnitsList = null;
            targetedUnits.Clear();
            CurrentFocusMode = FocusModeType.None;
        }

        /// <summary>
        /// DataContainerからAiGraphViewを取得
        /// </summary>
        AIGraph.AIGraphView GetAIView(AIGraphDataContainer aIGraphDataContainer)
        {
            if (aIGraphDataContainer == null)
                return null;
            if (!AIGraphViews.TryGetValue(aIGraphDataContainer, out var view))
            {
                view = AIGraphSaveUtility.LoadGraph(aIGraphDataContainer);
                AIGraphViews[aIGraphDataContainer] = view;
            }
            return view;
        }
        #endregion



        #region Unitの行動に関する処理
        /*
         * 各動作系はStart****TypeMode()系を開始するとその中でcoroutineを回し処理
         * 攻撃を決定するとcorutine内で攻撃をactionを開始 -> CallWhenActiveActionIsCompleted()でアニメーション等が終了するまで待つ
         * すべて終わったらCallWhenActiveActionIsCompleted()内でendAllUnitsActionHandlerをactionする
         */

        /// <summary>
        /// FOCUSモードに移行
        /// </summary>
        /// <param name="OnComplete"></param>
        public void StartFocusMode()
        {
            /*
             * FOのVASTみたいな部位による確率判定 
             * Fキーを押すとその場でフォーカスモードに入り、画面に映る全てのUnitの部位ごとの確率が出る
             * Canvasを貼り付けて場所にLabelを貼る
             * このフォーカスモードではマウスがフリーになって部位を選択できるようになる
             * 部位確率は武器ごとに異なる
             * 例えば 遠距離武器を遠距離で使用した場合部位確率ではなく全体確率しか出ない (UIが小さくなり過ぎで選択しにくいため)
             * また、戦車砲やグレネード系の範囲武器では全体確率かつ、場所により複数のターゲットが選択される
             */

            // フォーカスモードで狙える敵がいない場合はフォーカスモードに入れない
            if (!activeUnit.EnemyAndDistanceDict.TryFindFirst(p => p.Value != 0, out var _))
            {
                print("No enemy in sight");
                StartCoroutine(focusModeUI.ShowBottomMessage("No enemy in sight", "", 1));
                return;
            }

            if (activeUnit.itemController.CurrentItemHolder.RemainingActionCount <= 0)
            {
                // アイテムの使用回数が残っていない場合はフォーカスモードに入れない
                StartCoroutine(focusModeUI.ShowBottomMessage("No ammo", activeUnit.itemController.CurrentItemHolder.Data.Name, 1));
                return;
            }

            activeUnit.TpsController.IsTPSControllActive = false;
            activeUnit.WorkState = WorkState.Focus;

            var itemType = activeUnit.itemController.CurrentItemHolder.Data.FocusModeType;
            if (CurrentFocusMode != FocusModeType.None && CurrentFocusMode != itemType)
            {
                // ItemTypeが異なる場合現在のフォーカスモードを終了
                if (CurrentFocusMode == FocusModeType.Throw)
                    EndThrowTypeForcusMode();
                else if (CurrentFocusMode == FocusModeType.Gun)
                    EndGunTypeForcusMode();
            }

            if (itemType == FocusModeType.Throw)
            {
                // Grenadeタイプの道具
                StartCoroutine(StartThrowTypeForcusMode());
            }
            else if (itemType == FocusModeType.Gun)
            {
                // Gunタイプの道具
                StartGunTypeFocusMode();
            }
        }

        /// <summary>
        /// Focusモードを終了
        /// </summary>
        public void EndFocusMode()
        {
            if (activeUnit.IsAutoControlling)
                return;

            activeUnit.TpsController.IsTPSControllActive = true;
            if (CurrentFocusMode == FocusModeType.Throw)
            {
                // GrenadeAimingModeの場合の終了
                EndThrowTypeForcusMode();
            }
            else if (CurrentFocusMode == FocusModeType.Gun)
            {
                EndGunTypeForcusMode();
            }
            CurrentFocusMode = FocusModeType.None;
            StartCoroutine(CameraUserController.ChangeModeFollowTarget(activeUnit.TpsController));
            activeUnit.WorkState = WorkState.Wait;
        }

        /// <summary>
        /// 銃で攻撃する際のフォーカスモード
        /// </summary>
        /// <param name="OnComplete"></param>
        private void StartGunTypeFocusMode()
        {

            CurrentFocusMode = FocusModeType.Gun;
            CameraUserController.ChangeModeSubjective();
            UserController.enableCursor = true;

            targetedUnits.Sort((a, b) =>
            {
                var o = activeUnit.transform.forward.xz();
                var _b = (b.transform.position - activeUnit.transform.position).xz().normalized;
                var _a = (a.transform.position - activeUnit.transform.position).xz().normalized;
                return (int)((RadianOfTwoVector(o, _a) - RadianOfTwoVector(o, _b)) * 10000f);
            });

            Action<UI.Overlay.Target> shotAction = ((t) =>
            {
                int damage = 0;
                if (GameManager != null)
                    damage = GameManager.RandomController.Probability(t.percentage) ? t.damage : 0;
                else
                    damage = RandomController.StaticProbability(t.percentage) ? t.damage : 0;

                Action OnTurnCompleted = (() =>
                {
                    UserController.enableCursor = false;
                    CurrentFocusMode = FocusModeType.None;
                    StartCoroutine(CallWhenActiveActionIsCompleted(FocusModeType.Gun));
                });
                StartCoroutine(activeUnit.RifleAttackTo(t.targetUnit, damage, OnTurnCompleted));

            });
            focusModeUI.Show(activeUnit, targetedUnits, shotAction);
        }

        /// <summary>
        /// 銃で攻撃する際のフォーカスモードを終了する
        /// </summary>
        private void EndGunTypeForcusMode()
        {
            focusModeUI.Hide();
            CurrentFocusMode = FocusModeType.None;
            UserController.enableCursor = false;
        }

        /// <summary>
        /// グレネードを投げた様のForcusMode
        /// </summary>
        /// <param name="OnComplete"></param>
        private IEnumerator StartThrowTypeForcusMode()
        {
            CurrentFocusMode = FocusModeType.Throw;
            focusModeUI.Hide();

            activeUnit.TpsController.IsTPSControllActive = false;
            activeUnit.TpsController.IsMouseHandleMode = true;
            CameraUserController.ChangeModeOverShoulder(activeUnit);
            //cameraController.ChangeModeFree();
            //
            arcController.calcType = Arc.CalcType.FromVelocity;
            arcController.arcStartTransform = activeUnit.ThrowFromPosition;
            arcController.ThrowItemAngleAndForceCurve = GameManager.GeneralParameter.ThrowItemAngleAndForceCurve;
            arcController.ShowArc = true;

            LayerMask layer = LayerMask.GetMask("Ground");
            while (true)
            {
                // アニメーションが停止中のためUpdateを止める
                if (PauseAllAnimation)
                {
                    yield return null;
                    continue;
                }

                arcController.ThrowAngle = -(CameraUserController.OverShoulderCameraRotationX - 30) / 50;

                yield return null;

                if (UserController.MouseClickUp)
                {
                    // Grenadeを投げる
                    yield return StartCoroutine(activeUnit.GrenadeAttackTo(arcController, UnitsList, tilesController, hudWindow, null));
                    activeUnit.TpsController.IsTPSControllActive = true;
                    EndThrowTypeForcusMode();
                    activeUnit.isAlreadyActioned = true;
                    yield return StartCoroutine(CallWhenActiveActionIsCompleted(FocusModeType.Throw));

                    break;
                }

                if (!IsFocusMode)
                    break;
            }
        }

        /// <summary>
        /// 投げる形のFocusModeを終了する
        /// </summary>
        private void EndThrowTypeForcusMode()
        {
            CurrentFocusMode = FocusModeType.None;
            arcController.ShowArc = false;
            activeUnit.TpsController.IsMouseHandleMode = false;
        }

        /// <summary>
        /// <c>position</c>位置にGrenadeを投げる  ArcPositionの位置に投げる
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        private IEnumerator AIGrenadeAttack(UnitController active)
        {
            yield return null;
        }

        #region Gimmick
        /// <summary>
        /// ギミックモードを開始する
        /// </summary>
        /// <param name="gimmickObject"></param>
        public void StartGimmickMode(GimmickObject gimmickObject)
        {
            activeUnit.TpsController.IsTPSControllActive = false;
            if (gimmickObject is MortarGimmick mortorGimmick)
                StartCoroutine(StartMortarTypeMode(mortorGimmick));
        }

        /// <summary>
        /// Mortarから打ち出されるタイプの攻撃
        /// </summary>
        /// <param name="mortarGimmick"></param>
        /// <returns></returns>
        private IEnumerator StartMortarTypeMode(MortarGimmick mortarGimmick)
        {
            StartCoroutine(selectItemPanel.SetItem(mortarGimmick));

            //UserController.enableCursor = false;
            CurrentFocusMode = FocusModeType.Mortar;
            StartCoroutine(activeUnit.UseMotarGimmick(mortarGimmick));
            //StartCoroutine(CameraUserController.ChangeModeFollowMortar(mortarGimmick));
            CameraUserController.ChangeModeOverShoulderFar(activeUnit);

            ClearAimLine();

            arcController.arcStartTransform = mortarGimmick.ArcStartFromObject.transform;
            arcController.ThrowItemAngleAndForceCurve = mortarGimmick.ThrowItemAngleAndForceCurve;
            arcController.calcType = Arc.CalcType.FromVelocity;

            yield return new WaitForSeconds(CameraUserController.CameraChangeDuration + 0.1f);
            arcController.ShowArc = !activeUnit.isAlreadyActioned;

            while (true)
            {
                if (PauseAllAnimation)
                {
                    yield return null;
                    continue;
                }

                if (arcController.ShowArc)
                {
                    arcController.ThrowAngle = -(CameraUserController.OverShoulderCameraRotationX - 30) / 50;
                    arcController.ThrowAngle += 0.8f;
                    arcController.ThrowAngle /= 1 + 0.8f;
                    arcController.ThrowAngle *= 3.5f;

                    arcController.PointerRadius = mortarGimmick.DamageCircleRadius;
                }

                if (activeUnit == null)
                    break;

                activeUnit.transform.SetPositionAndRotation(mortarGimmick.UnitPosition.position, mortarGimmick.UnitPosition.rotation);
                mortarGimmick.transform.Rotate(transform.up, UserController.MouseDeltaX * 1.5f);

                yield return null;

                if (UserController.MouseClickDown && !activeUnit.isAlreadyActioned)
                {
                    // 発射
                    //CameraUserController.enableCameraControlling = false;
                    yield return ShootMortar(mortarGimmick, arcController.ArcPositions, arcController.InitialVelocity, arcController.HitPosition);
                    //CameraUserController.enableCameraControlling = true;
                    //EndMortarTypeGimmickMode(true);
                    //break;
                    if (activeUnit.isAlreadyMoved)
                    {
                        // 既に移動している場合はターン終了
                        CurrentFocusMode = FocusModeType.None;
                        arcController.ShowArc = false;
                        break;
                    }
                    else
                    {
                        // まだ移動していない場合は移動を開始
                        // 攻撃は出来ないためarcは消す
                        arcController.ShowArc = false;
                    }
                }

                if (CurrentFocusMode != FocusModeType.Mortar)
                    break;
            }
        }

        /// <summary>
        /// <c>arc</c>の地点を通る投射運動をアニメーションベースで再現し該当Unitsにダメージを与える
        /// </summary>
        /// <param name="arc"></param>
        /// <returns></returns>
        private IEnumerator ShootMortar(MortarGimmick mortarGimmick, List<Vector3> arc, Vector3 initialVelocity, Vector3 hitPosition)
        {
            var ammo = Instantiate(mortarGimmick.MortarAmmo, WorldItemParent.transform);
            ammo.transform.SetPositionAndRotation(mortarGimmick.MortarAmmoStartPosition.position, mortarGimmick.MortarAmmoStartPosition.rotation);

            var seq = DOTween.Sequence();
            var horizontalVelocity = Vector2.Distance(initialVelocity.xz(), Vector2.zero);

            const int deletePartsIndex = 3;
            const int purgePartsIndex = 5;

            var arcLinesLength = arc.Select((v, i) => i == 0 ? 0 : Vector3.Distance(v, arc[i - 1])).ToList();
            arcLinesLength.RemoveAt(0);
            var maxArcLineLength = arcLinesLength.Max();

            var originAmmoSize = ammo.transform.localScale;
            // 迫力を重視するために遠くなるほどAmmoが大きくなる
            const float MortarAmmoScale = 0.001f;

            // Mortarの加害範囲内にあるGimmickとUnitを取得
            Print("Hit position:", arcController.HitPosition);
            var hitUnits = arcController.GetUnitsWithinRadius(UnitsList);
            var hitGimmicks = tilesController.GetGimmicksWithinRadius(arcController.HitPosition, mortarGimmick.DamageCircleRadius, true);
            // Mortarの加害範囲内にあるDestuctibleObjectを取得
            //var hitDestructibleObjects = tilesController.GetDestructibleObjectsWithinRadius(arcController.HitPosition, mortarGimmick.DamageCircleRadius, true);

            // arcから最も高い地点を取得
            var maxHeight = arc.Select(v => v.y).Max();
            // indexを取得
            var maxHeightIndex = arc.FindIndex(v => v.y == maxHeight);
            var maxPosition = arc[maxHeightIndex];
            // maxHeightの高さにmortarVirtualCameraを設置してhitPositionとhitUnitsのすべてを映るようにする
            mortarGimmick.MortarVirtualCamera.transform.position = new Vector3(maxPosition.x, maxHeight + 0.3f, maxPosition.z);
            mortarGimmick.MortarVirtualCamera.transform.LookAt(hitPosition);
            mortarGimmick.MortarVirtualCamera.Priority = 100;

            mortarGimmick.UseGimmick();
            selectItemPanel.UpdateItemsState();

            DOTween.SetTweensCapacity(arc.Count * 2, arc.Count * 5);
            for (var i = 1; i < arc.Count; i++)
            {
                var p1 = arc[i - 1];
                var p2 = arc[i];

                // Ammoが目標に十分近づいたため爆発
                if (Vector3.Distance(p2, hitPosition) < mortarGimmick.ExplosionDistance)
                    break;

                var speedCurveX = Vector3.Distance(p1, p2) / maxArcLineLength;
                var t = mortarGimmick.AmmoSpeedCurve.Evaluate(speedCurveX) * mortarGimmick.AmmoSpeed;
                var distFromOrigin = Vector3.Distance(p1, mortarGimmick.MortarAmmoStartPosition.position);

                // Time 時間で p1 から p2へと移動すればほぼ投射運動と同じ
                var move = ammo.transform.DOMove(p2, t).SetEase(Ease.Linear);
                var rotation = ammo.transform.DOLookAt(p2, t).SetEase(Ease.Linear);
                var size = ammo.transform.DOBlendableScaleBy(originAmmoSize * distFromOrigin * MortarAmmoScale, t).SetEase(Ease.Linear);
                if (deletePartsIndex == i)
                {
                    //action.OnComplete(() => mortarAmmo.DisappearParts());
                }
                else if (purgePartsIndex == i)
                {
                    move.OnComplete(() =>
                    {
                        // Ammoから分離されるパーツ
                        //var purged = mortarAmmo.PurgeParts();
                        //purged.ForEach(p => p.AddForce((p2 - p1) * 10, ForceMode.Impulse));
                    });
                }

                seq.Append(move);
                seq.Join(rotation);
                seq.Join(size);
            }

            var completeToExplosion = false;

            // 進行方向に破片をばらまくeffectと実際にUnitにダメージを与える処理
            seq.OnComplete(() =>
            {
                var explosionPosition = ammo.transform.position;
                StartCoroutine(mortarGimmick.Explosion(ammo));

                // 爆発の位置からhitUnitsとhitGimmicksに対してダメージを与える
                var layer = LayerMask.GetMask("Object", "Unit");
                hitUnits.ForEach(u =>
                {
                    Physics.Raycast(explosionPosition, u.transform.position - explosionPosition, out RaycastHit hitInfo, 10, layer);
                    var damage = mortarGimmick.GetDamage(u, hitInfo);
                    StartCoroutine(u.RecieveDamage(activeUnit, mortarGimmick.AttackType, damage));
                });

                // 爆発の位置からhitGimmicksに対してダメージを与える
                hitGimmicks.ForEach(g =>
                {
                    StartCoroutine(g.DestroyAnimation());
                });

                completeToExplosion = true;
            });
            seq.Play();
            yield return new WaitUntil(() => completeToExplosion);

            // ダメージを受けたUnitのアニメーションが終了するまで待つ
            if (hitUnits.Count == 0 && hitGimmicks.Count == 0)
            {
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Print($"Shoot Mortar from {activeUnit} HitUnits: {hitUnits.Count}, HitGimmick: {hitGimmicks.Count}");
                foreach (var u in hitUnits)
                {
                    yield return new WaitUntil(() => u.IsDamageAnimating);
                }
                foreach (var g in hitGimmicks)
                {
                    yield return new WaitUntil(() => g.IsDestroyed);
                }
            }

            // 爆発が終わったらMortarVirtualCameraを元に戻す
            mortarGimmick.MortarVirtualCamera.Priority = 0;
            yield return new WaitForSeconds(CameraUserController.CameraChangeDuration);

            // すでに移動が完了しているためActiveUnitのItemsを表示しなおして終了
            if (activeUnit.isAlreadyMoved)
                StartCoroutine(selectItemPanel.SetItems(activeUnit));

            StartCoroutine(CallWhenActiveActionIsCompleted(FocusModeType.Mortar));
        }

        /// <summary>
        /// ギミックの仕様を中止する
        /// </summary>
        public void CancelGimmickMode()
        {
            StartCoroutine(selectItemPanel.SetItems(activeUnit));
            activeUnit.TpsController.IsTPSControllActive = true;
            CancelMortarTypeGimmickMode(false);
        }

        /// <summary>
        /// Mortarタイプのギミックを終了
        /// </summary>
        private void CancelMortarTypeGimmickMode(bool endTurn)
        {
            if (CurrentFocusMode != FocusModeType.Mortar) return;

            StartCoroutine(CameraUserController.ChangeModeFollowTarget(activeUnit.TpsController));
            CurrentFocusMode = FocusModeType.None;
            arcController.ShowArc = false;
            activeUnit.TpsController.IsMouseHandleMode = false;
            StartCoroutine(activeUnit.RemoveFromMortarGimmick());

            // activeUnitの現在持っているアイテムがShowAimLineの場合はAimLineを再描写
            if (activeUnit.itemController.CurrentItemHolder.Data.ShowAimLine)
            {
                DrawAimLines(true);
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// 指定した属性のUnitのAIControlを有効化する
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="attribute"></param>
        internal void EnableAIControlling(bool enable, UnitAttribute attribute)
        {
            UnitsList.ForEach(u =>
            {
                if (u.Attribute == attribute)
                    u.isAiControlled = enable;
            });
        }

        #region AimLine

        private UnitController previousActiveUnit;
        /// <summary>
        /// AimLineを描写する
        /// </summary>
        /// <param name="forceDrawing">通常は歩行時のみAimLineを更新するがこれをtrueにすると強制的に更新する</param>
        private void DrawAimLines(bool forceDrawing = false)
        {
            if (!TacticsController.IsGameRunning)
                return;

            if (activeUnit == null || activeUnit.gameObject == null || activeUnit.isAiControlled)
                return;

            if (previousActiveUnit != activeUnit)
            {
                previousActiveUnit = activeUnit;
            }

            if (activeUnit.itemController.CurrentItemHolder.Data.ShowAimLine)
            {
                // Rifle型のAimLineを描写
                if (activeUnit.WorkState == WorkState.Walk || forceDrawing)
                {
                    var aimed = activeUnit.EnemyAndDistanceDict.ToList().FindAll(ed => ed.Value != 0).ConvertAll(e => e.Key);
                    
                    if (targetedUnits == null)
                        targetedUnits = new List<UnitController>(aimed);
                    else
                    {
                        targetedUnits.Clear();
                        targetedUnits.AddRange(aimed);
                    }

                    aimLineController.SetAimLine(activeUnit, aimed);

                    if (activeUnit.RaderTargetPosition != null)
                    {
                        // ! UIで横に表示する形にする
                        // レーダーターゲットの位置を更新
                        //if (RaderTargetObject == null)
                        //    RaderTargetObject = aimLineController.MakeRaderTarget();
                        //RaderTargetObject.transform.position = activeUnit.RaderTargetPosition.GetValueOrDefault();
                    }
                    else
                    {
                        // レーダーターゲットを消す
                        //Destroy(RaderTargetObject);
                    }
                }
            }
        }

        /// <summary>
        /// AimLineを消す
        /// </summary>
        public void ClearAimLine()
        {
              aimLineController.ClearAll();
        }

        /// <summary>
        /// アイテムの変更を通知されてくる from TacticsController
        /// </summary>
        internal void NortifyItemChanged()
        {
            if (activeUnit.itemController.CurrentItemHolder.Data.ItemType == ItemType.Rifle)
            {
                // Itemが変わったため必要であればRifleAimLineを再描写する
                DrawAimLines(true);
            }
            else
            {
                // Itemが変わったため RifleAimLineを描写中であれば消す
                ClearAimLine();
            }
        }

        #endregion

        #region Turn User Controller
        /// <summary>
        /// 現在進行中の勢力のターンリストを取得する
        /// </summary>
        /// <returns></returns>
        internal List<UnitController> GetTurnList()
        {
            return UnitsList.FindAll(u => u.Attribute == TurnOfAttribute);
        }

        /// <summary>
        /// 最初のターンを開始する activeUnit.TpsController.isActiveはfalseになっているため手動でactiveにしてuserControlできるようにすること
        /// </summary>
        /// <returns></returns>
        internal IEnumerator FirstTurn()
        {
            if (TurnOfAttribute == UnitAttribute.PLAYER)
            {
                AttributeTurnCount++;
                TurnList = UnitsList.FindAll(u => u.Attribute == TurnOfAttribute);
                UnitsList.ForEach(u => u.StartFirstTurn());

                if (TurnList.IndexAt_Bug(TurnCount, out var activeUnit))
                {
                    Print(TurnOfAttribute == UnitAttribute.PLAYER ? "== Start Player Turn ==" : "== Start Enemy Turn ==");
                    UnitsList.ForEach(u =>
                    {
                        u.TpsController.aimCamera.Priority = 1;
                        u.TpsController.followCamera.Priority = 1;
                    });

                     yield return StartCoroutine(ActiveControl(activeUnit));
                }
            }

            yield break;
        }
        
        /// <summary>
        /// 次のUnitにターンを続ける
        /// </summary>
        /// <returns></returns>
        internal IEnumerator NextTurn()
        {
            // NPCターンが終了した際に呼び出し
            // この通報はUnitTurnが進行した際に実行
            IEnumerator EndFindOutNPCRoutine(UnitController lastUnit)
            {
                var npcs = UnitsList.FindAll(u => u.Attribute == UnitAttribute.ENEMY);
                var alertUnits = lastUnit.aiController.EndUnitTurn();
                
                foreach(var alert in alertUnits)
                {
                    npcs.ForEach(npc =>
                    {
                        npc.aiController.SetForceFindOut(alert.Enemy);
                    });
                }

                if (alertUnits.Count != 0)
                {
                    Print($"{alertUnits[0].thisUnit} finds new enemy of player");
                    yield return new WaitForSeconds(1);
                }
            }

            // PlayerのAttributeTurnが終了した際に呼び出し
            IEnumerator EndFindOutPlayerRoutine()
            {
                var npcs = UnitsList.FindAll(u => u.Attribute == UnitAttribute.ENEMY);
                var alertUnits = new List<AI.FindOutLevel>();
                npcs.ForEach(n =>
                {
                    alertUnits.AddRange(n.aiController.EndUnitTurn());
                });

                foreach(var alert in alertUnits)
                {
                    npcs.ForEach(npcs =>
                    {
                        npcs.aiController.SetForceFindOut(alert.Enemy);
                    });
                }

                if (alertUnits.Count != 0)
                {
                    Print($"{alertUnits[0].thisUnit} finds new enemy of player");
                    yield return new WaitForSeconds(1);
                }
            }

            // Disactive unit controlling
            DisactiveControl(activeUnit);
            var lastUnit = activeUnit;

            if (lastUnit.OnTriggerObject)
            {
                lastUnit.TpsController.SwitchCrouching();
                yield return new WaitForSeconds(1f);
            }

            // Active next unit
            TurnCount++;
            if (TurnList.IndexAt(TurnCount, out activeUnit))
            {
                // activeUnitのEnemyが現在行動中のAttributeのUnitを発見　指定TurnStep経過後もしくは勢力のターン終了時に全EnemyUnitに通知
                // 発見された場合指定ターン以内に撃破しないと見つかって通報される
                if (lastUnit.Attribute == UnitAttribute.ENEMY)
                {
                    yield return StartCoroutine(EndFindOutNPCRoutine(lastUnit));
                }else if (lastUnit.Attribute == UnitAttribute.PLAYER)
                {
                    // lastUnit.AiController.EndFindOutRoutineWithoutTurnChanging();
                }

                //if (activeUnit.attribute == UnitAttribute.PLAYER)
                //    lastPlayer = activeUnit;

                yield return StartCoroutine(ActiveControl(activeUnit));
            }
            else
            {
                // 勢力のターンが変わったため強制的にすべてのEnemyを通知
                if (lastUnit.Attribute == UnitAttribute.PLAYER)
                {
                    yield return StartCoroutine(EndFindOutPlayerRoutine());
                }
               
                // UnitAttributeが次のターンになる
                TurnOfAttribute = TurnOfAttribute == UnitAttribute.PLAYER ? UnitAttribute.ENEMY : UnitAttribute.PLAYER;
                Print(TurnOfAttribute == UnitAttribute.PLAYER ? "== Start Player Turn ==" : "== Start Enemy Turn ==");
                TurnList = UnitsList.FindAll(u => u.Attribute == TurnOfAttribute);
                TurnCount = 0;
                AttributeTurnCount++;
                if (TurnList.IndexAt(TurnCount, out activeUnit))
                {
                    if (activeUnit.Attribute == UnitAttribute.ENEMY)
                        ClearAimLine();

                    yield return StartCoroutine(ActiveControl(activeUnit));

                }else
                {
                    // TODO Debug様にunitが１つでもあれば続ける
                    Print($"===> All {TurnOfAttribute} are dead <===");
                    if (ContinueTurnAtOneUnit)
                    {
                        TurnOfAttribute = TurnOfAttribute == UnitAttribute.PLAYER ? UnitAttribute.ENEMY : UnitAttribute.PLAYER;
                        Print($"Restart turn of {TurnOfAttribute}");
                        TurnList = GetTurnList();
                        TurnCount = 0;
                        if (!TurnList.IndexAt_Bug(TurnCount, out activeUnit))
                        {
                            yield break;
                        }

                        yield return StartCoroutine(ActiveControl(activeUnit));
                    }
                    else
                    {
                        yield break;
                    }

                }
            }
            yield break;
        }
        
        /// <summary>
        /// 対象のユニットのTurnを終了する
        /// </summary>
        /// <param name="target"></param>
        private void DisactiveControl(UnitController target)
        {
            // 該当UnitがGimmickを使用中であれば使用したままターン終了

            // target.unitController.LockTarget(false);
            target.TpsController.IsTPSControllActive = false;
            // ターン終了時に速度を0に
            target.Rigidbody.velocity = Vector3.zero;
            target.Rigidbody.isKinematic = true;
            target.Rigidbody.WakeUp();
            target.EndTurn();

            // Targetの至近距離を進入不可にするためObstacleを有効化
            target.NavMeshAgent.enabled = false;
            target.NavMeshObstacle.enabled = true;

            activeUnit.SetTargetPartsActive(true);

            Print($"{target} end turn in {target.tileCell}");
        }
        
        /// <summary>
        /// 指定したUnitの操作をActiveにする
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private IEnumerator ActiveControl(UnitController target)
        {
            Print("==> Start Trun:", target, "UsingGimmick", target.IsUsingGimmickObject, "<==");

            // isKinematic = true;でもオブジェクト同士が押される理由として
            // NavAgentが干渉している　NavAgent = true;なら isKinematic = true;でなければならない
            activeUnit = target;
            TmpEnemiesToRay = GetEnemiesFrom(activeUnit);
            target.Rigidbody.isKinematic = false;
            target.Rigidbody.WakeUp();
            activeUnit.SetTargetPartsActive(false);

            // Unitがこれ以上動いたら移動完了とするMoveColliderを設定
            activeUnit.isAlreadyMoved = false;
            activeUnit.isAlreadyActioned = false;
            if (!activeUnit.isAiControlled)
            {
                notMoveCircle.SetActive(true);
                NotMoveCircleMaterial.SetColor("_EmissionColor", Color.clear);
                StartCoroutine(NotMoveCircleMaterial.SetColor("_EmissionColor", notMoveCircleColor, GameManager.GeneralParameter.TileColorAnimationCurve));
            }
            else
            {
                notMoveCircle.SetActive(false);
            }
            notMoveCircle.transform.position = activeUnit.transform.position;

            // 射線と距離の値を更新
            TmpEnemiesToRay.ForEach(e =>
            {
                if (!e.IsDead)
                {
                    activeUnit.EnemyAndDistanceDict[e] = activeUnit.GetRayDistanceTo(e);
                    RayDistToEnemy = activeUnit.EnemyAndDistanceDict[e];
                }
            });

            // PlayerならFollowCameraで、NPCなら近いPlayerから見たようなStationaryCameraになる
            if (activeUnit.isAiControlled)
            {
                CameraUserController.ChangeModeStationaryAtNear(activeUnit);
            }
            else
            {
                if (target.IsUsingGimmickObject)
                {
                    // ギミック使用中であればギミックの使用再開する
                    StartGimmickMode(target.GimmickObject);
                }
                else
                {
                    // ギミック使用中でなければ通常のTPSモードにする
                    yield return StartCoroutine(CameraUserController.ChangeModeFollowTarget(activeUnit.TpsController));
                    target.TpsController.IsTPSControllActive = true;

                    // AimLineを書く
                    DrawAimLines(true);
                }
            }

            // しゃがんでいる状態ならもとに戻る
            if (activeUnit.TpsController.IsCrouching)
            {
                activeUnit.TpsController.SwitchCrouching();
            }

            // ActiveUnitと敵までの距離を更新
            GetEnemiesFrom(activeUnit).ForEach(e =>
            {
                activeUnit.EnemyAndDistanceDict[e] = activeUnit.GetRayDistanceTo(e);
            });

            target.StartTurn();

            // AI Coroutines
            IEnumerator _AICoroutines()
            {
                tilesController.Tiles.ForEach(t =>
                {
                    t.pointsInTile.ForEach(l =>
                    {
                        l.DebugScore = 0;
                    });
                });

                //var forcastCoroutines = UnitsList.FindAll(u => u.IsEnemyFromMe(target)) .ConvertAll(u =>
                //{
                //    return StartCoroutine(u.aiController.CalcForcastAction());
                //});

                //foreach(var c in forcastCoroutines)
                //    yield return c;

                //GetEnemiesFrom(target).ForEach(u =>
                //{
                //    u.aiController.aiCore.PrintDebugText();
                //    Print("ForcastAction:",u.aiController.aiCore.Result);
                //});

                //print("-> End forcast calcurations <-");

                yield return StartCoroutine(target.aiController.Run());
            }

            // UnitがAI移動できるようにするため邪魔なObstacleを無効化
            target.NavMeshObstacle.enabled = false;
            if (target.isAiControlled)
            {
                // TODO: AIで操作されるユニット
                target.NavMeshAgent.enabled = true;
                StartCoroutine(_AICoroutines());
            }

            // UIに行動開始するUnitの情報を書く
            myInfoWindow.SetParameter(activeUnit);

            // Debug用のWalkingLineを書く
            target.debugDrawAimWalkingLine = DrawAimWalkingLine;

            //if (target.attribute == UnitAttribute.PLAYER)
            //    target.TpsController.IsTPSControllActive = true;
            //else
            //    target.TpsController.IsTPSControllActive = false;
        }

        /// <summary>
        /// Tacticsを終了する
        /// </summary>
        internal void EndGame()
        {
            CurrentFocusMode = FocusModeType.None;
            UnitsList.ForEach(u => u.aiController.EndFindOutRoutine());
        }

        /// <summary>
        /// ActiveUnitのActionを選択した後にすべてのUnitのAnimationや反応が終わっているかどうかチェックし待ち、移動して否ならFollowに切り替える
        /// </summary>
        /// <param name="forceEnd">強制的にターンを終了させる</param>
        /// <returns></returns>
        private IEnumerator CallWhenActiveActionIsCompleted(FocusModeType useItemType, bool forceEnd =false)
        {
            activeUnit.isAlreadyActioned = true;

            // すべてのUnitのAnimationが終了するまで待つ
            yield return StartCoroutine(WaitActionUnitAnimations());

            var actionArgs = new EndAllUnitsActionArgs();

            if (!activeUnit.isAlreadyMoved && !activeUnit.isAiControlled　&& !forceEnd)
            {
                Print("Action done:", activeUnit, "is not moved yet");
                // まだ移動してないため動ける
                // Action -> Move -> EndTurnの形になる 今はMoveの段階
                if (CameraUserController.Mode != CameraUserController.CameraMode.Follow)
                    yield return StartCoroutine(CameraUserController.ChangeModeFollowTarget(activeUnit.TpsController));

                if (useItemType == FocusModeType.Mortar)
                {
                    activeUnit.TpsController.IsTPSControllActive = false;
                }
                else
                {
                    activeUnit.TpsController.IsTPSControllActive = true;
                }

                actionArgs.isTurnCompleted = false;
            }
            else
            {
                Print("Action done:", activeUnit, "is already moved");
                // 既に移動しているため終了
                actionArgs.isTurnCompleted = true;
                // ターン終了時にGimmickのRemainActionCountが0になっている場合はGimmickを終了する
                if (activeUnit.WorkState == WorkState.Gimmick && activeUnit.GimmickObject.RemainingActionCount <= 0)
                {
                    // CancelGimmickModeでUseItemType.Mortarを設定していないと動かないため個々で設定
                    CurrentFocusMode = FocusModeType.Mortar;
                    CancelGimmickMode();
                }
                    
            }
            endAllUnitsActionHandler?.Invoke(this, actionArgs);
        }

        /// <summary>
        /// Userの操作によってTurnを終了する
        /// </summary>
        public void EndTurnFromPlayerControl()
        {
            if (IsFocusMode)
                EndFocusMode();

            UserController.enableCursor = false;

            StartCoroutine(CallWhenActiveActionIsCompleted(FocusModeType.None, true));
        }

        #endregion

        #region State
        /// <summary>
        /// このCoroutineはすべてのUnitのAnimationが終了するまで待つ
        /// </summary>
        /// <returns></returns>
        public IEnumerator WaitActionUnitAnimations()
        {
            while (activeUnit.WorkState == WorkState.Animating)
                yield return null;

            foreach (var u in UnitsList)
                yield return new WaitWhile(() => u.IsDamageAnimating);
        }

        /// <summary>
        /// myUnitの敵となるUnitがどれだけいるかカウントする
        /// </summary>
        /// <param name="myUnit"></param>
        /// <returns></returns>
        public int EnemyCount(UnitController myUnit)
        {
            return UnitsList.FindAll(u => myUnit.IsEnemyFromMe(u)).Count;
        }

        /// <summary>
        /// <c>myUnit</c>の味方がどれだけいるかカウントする
        /// </summary>
        /// <param name="myUnit"></param>
        /// <returns></returns>
        public int FirendCount(UnitController myUnit)
        {
            return UnitsList.FindAll(u => !myUnit.IsEnemyFromMe(u)).Count;
        }

        /// <summary>
        /// <c>unit</c>の敵をすべて取得する
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public List<UnitController> GetEnemiesFrom(UnitController unit)
        {
            return UnitsList.FindAll(u => u.IsEnemyFromMe(unit));
        }

        /// <summary>
        /// <c>unit</c>の味方をすべて取得する
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public List<UnitController> GetFriendsOf(UnitController unit)
        {
            var allFriends = UnitsList.FindAll(u => !u.IsEnemyFromMe(unit));
            allFriends.Remove(unit);
            return allFriends;
        }

        
        /// <summary>
        /// 何らかのUnitにダメージが加わった際に呼び出される
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void DamageEvent(object o, DamageEventArgs args)
        {
            var damagedUnit = (UnitController)o;
            IEnumerator Wait()
            {
                yield return StartCoroutine(WaitActionUnitAnimations());
                // 死亡->Disappearをアニメーション化
                damagedUnit.UnitDead();
                Destroy(damagedUnit.gameObject);
            }

            var damagedUnitsFirends = GetFriendsOf(damagedUnit);

            // ダメージを受けた味方に近い仲間が攻撃者を発見する
            if (damagedUnit.Attribute == UnitAttribute.ENEMY)
            {
                const float forceFindOutDistance = 6;
                damagedUnitsFirends.ForEach(u =>
                {
                    var dist = Vector3.Distance(damagedUnit.ThisObject.transform.position, u.ThisObject.transform.position);
                    if (dist < forceFindOutDistance)
                        u.aiController.SetForceFindOut(args.damageFrom, true);
                });
            }

            if (damagedUnit.IsDead)
            {
                // DamagedUnitが死亡時
                aimLineController.Destroy(damagedUnit);
                ReloadTurn();
                // 死亡アニメーションを待つ
                StartCoroutine(Wait());

                
            }
            else
            {
                // 対象ユニットの被ダメージ時
            }
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
                    UnitsList.ForEach(u =>
                    {
                        u.aiController.PauseFindOutRoutine = true;
                        u.TpsController.PauseAnimation = true;
                    });
                }
                else
                {
                    UnitsList.ForEach(u => 
                    { 
                        u.aiController.PauseFindOutRoutine = false;
                        u.TpsController.PauseAnimation = false; 
                    });

                }

            }
        }
        private bool isStopped = false;

        /// <summary>
        /// Unitがこれ以上動いたら移動完了とするMoveColliderを設定
        /// </summary>
        private void SetNotMoveCollider()
        {
            if (activeUnit == null) return;
            activeUnit.isAlreadyMoved = false;
            activeUnit.isAlreadyActioned = false;

            if (activeUnit.Attribute == UnitAttribute.PLAYER)
            {
                StartCoroutine(NotMoveCircleMaterial.SetColor("_EmissionColor", notMoveCircleColor, GameManager.GeneralParameter.TileColorAnimationCurve));
            }

            notMoveCircle.transform.position = activeUnit.transform.position;
        }
        #endregion

        #region Turn List Control
        /// <summary>
        /// unitsListのReload
        /// </summary>
        internal void ReloadTurn()
        {
            const int reshapeOffset = 10000;

            static int GetSpeed(UnitController t)
            {
                return (int)(t.CurrentParameter.Speed * reshapeOffset);
            }

            UnitsList.Clear();

            var tmpList = OriginUnitsList.ToList();
            tmpList.Sort((a, b) => GetSpeed(b) - GetSpeed(a));
            tmpList.ForEach(u =>
            {
                if (u.CurrentParameter.HealthPoint > 0)
                    UnitsList.Add(u);
                else
                    TurnList.Remove(u);
            });

        }

        #endregion
    }

    /// <summary>
    /// 1ターン内ですべてのUnitの行動が終了した際に呼び出し
    /// </summary>
    class EndAllUnitsActionArgs: EventArgs
    {
        /// <summary>
        /// このHandlerの送られるturnでactiveなUnit
        /// </summary>
        internal UnitController activeUnit;

        /// <summary>
        /// 既に移動済みか
        /// </summary>
        internal bool IsAlreadyMoved
        {
            get => activeUnit.isAlreadyMoved;
        }

        /// <summary>
        /// UnitのActionに酔ってTurnが終了したか
        /// </summary>
        internal bool isTurnCompleted;

        /// <summary>
        /// Turn内で死亡したUnit
        /// </summary>
        internal List<UnitController> deadUnitsInTurn;
        /// <summary>
        /// AIのコントロールであるか
        /// </summary>
        internal bool IsAiControlling
        {
            get => activeUnit.isAiControlled;
        }
    }
}