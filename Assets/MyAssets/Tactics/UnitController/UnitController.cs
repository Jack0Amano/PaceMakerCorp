using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;
using Cinemachine;
using System.Linq;
using static Utility;
using Unity.VisualScripting;
using Unity.Logging;
using Tactics.Control;
using UnityEngine.Rendering;
using Tactics.Object;
using DG.Tweening;
using Tactics.Items;
using Tactics.Map;


namespace Tactics.Character
{
    /// <summary>
    /// Tactics画面の個別のユニットを管理する 
    /// </summary>
    public class UnitController : MonoBehaviour
    {
        #region Serialized Properties
        [Header("Info")]
        [SerializeField, ReadOnly] private string UnitName;
        [SerializeField, ReadOnly] bool isFreeMode = true;
        [Tooltip("Unitの現在の状態")]
        [SerializeField, ReadOnly] internal WorkState WorkState = WorkState.Wait;
        [Tooltip("ターン開始時からNotMoveCicle外に出たか")]
        [SerializeField, ReadOnly] internal bool isAlreadyMoved = false;
        [Tooltip("ターン開始時から行動済みか")]
        [SerializeField, ReadOnly] internal bool isAlreadyActioned = false;
        [Tooltip("ターン開始時から異なるTileに移動したか")]
        [SerializeField, ReadOnly] internal bool IsAlreadyMovedDifferentTile = false;
        [Tooltip("Unitの敵味方の属性")]
        [SerializeField, ReadOnly] public UnitAttribute Attribute = UnitAttribute.PLAYER;
        [Tooltip("UnitのAIを使用するか")]
        [SerializeField] internal bool isAiControlled = false;
        /// <summary>
        /// Unitの現在のパラメータ値 Readonly
        /// </summary>
        [Tooltip("Unitの現在のパラメータ値")]
        [SerializeField] public CurrentUnitParameter CurrentParameter;

        [Header("Item")]
        [Tooltip("UnitControllerのItem関連をまとめた")]
        [SerializeField] public Items.ItemController itemController;

        [Header("Positions")]
        [Tooltip("UnitのHUDを表示する位置")]
        [SerializeField] public Transform hudPosition;
        [Tooltip("Unitの!とか?を出す位置")]
        [SerializeField] public Transform headUpIconPosition;
        [Tooltip("Unitが物を投げる位置の開始位置")]
        [SerializeField] internal Transform ThrowFromPosition;

        [Header("Virtual Camera")]
        [Tooltip("UnitがTabletを見る際のカメラ")]
        [SerializeField] internal CinemachineVirtualCamera watchTabletCamera;
        [Tooltip("UnitがTabletを見る際のTabletの位置")]
        [SerializeField] internal Transform tabletPosition;
        [Tooltip("Unitが遠くを見る際の少し頭上にあるカメラの位置")]
        [SerializeField] internal CinemachineVirtualCamera stationaryCamera;

        [Header("Trigger系")]
        [Tooltip("UnitCursorのサイズ UnitのTileの侵入時の検知に使用")]
        [SerializeField] internal float UnitCursorSize = 1f;
        [Tooltip("Damageを計算する際にHit可能な場所")]
        [SerializeField] internal List<RaycastPart> bodyParts;
        [Tooltip("GimmickObject Layerに反応する接触感知")]
        [SerializeField] NortifyTrigger circleTrigger;
        [Tooltip("UnitのメインのMeshRenderer")]
        [SerializeField] internal SkinnedMeshRenderer meshRenderer;

        [Header("Debug")]
        [Tooltip("ダメージを受けないモード")]
        [SerializeField] bool isGodMode = false;
        #endregion

        #region Properties
        /// <summary>
        /// CameraUserController UnitsControllerからSetUnitの際に配置される
        /// </summary>
        internal CameraUserController cameraUserController;

        public GameObject ThisObject { private set; get; }
        /// <summary>
        /// 現在Animation等によりautoのcontrolが行われているか
        /// </summary>
        internal bool IsAutoControlling { private set; get; } = false;
        /// <summary>
        /// 攻撃時にSupportAttackを行うユニット 同時攻撃は実装するか未定
        /// </summary>
        // internal UnitController supportAttackUnit;
        /// <summary>
        /// UnitがActiveなTurnであるかどうか
        /// </summary>
        internal bool IsActive { private set; get; } = false;
        // DamageEvent
        internal delegate void DamageEventHandler(object sender, DamageEventArgs e);
        /// <summary>
        /// ユニットがダメージを受けた場合UnitsControllerにEventを送る
        /// </summary>
        internal DamageEventHandler damageHandler;
        private DamageEventArgs damageEventArgs;
        /// <summary>
        /// ダメージ表現のアニメーションを再生中
        /// </summary>
        internal bool IsDamageAnimating { private set; get; } = false;
        /// <summary>
        /// UnitがTPS視点で操作される時のController
        /// </summary>
        public ThirdPersonUserControl TpsController { private set; get; }
        /// <summary>
        /// Unit's NanigationMesh
        /// </summary>
        public NavMeshAgent NavMeshAgent { private set; get; }
        /// <summary>
        /// Unitの物理ボディ
        /// </summary>
        public Rigidbody Rigidbody { private set; get; }
        /// <summary>
        /// NavMeshに穴を開けUnitの周り至近距離を進入不可にする
        /// </summary>
        internal NavMeshObstacle NavMeshObstacle { private set; get; }
        /// <summary>
        /// 共通パラメーター
        /// </summary>
        private GeneralParameter generalParameters;
        /// <summary>
        /// UnitがCrouchingの際の隠れているカバーオブジェクト
        /// </summary>
        internal GameObject coverObject;
        /// <summary>
        /// GimickObjectの位置を表示するRader風の奴の位置
        /// </summary>
        public Vector3? RaderTargetPosition = null;
        /// <summary>
        /// Unitの目線位置
        /// </summary>
        private GameObject myEyesLocationObject;
        /// <summary>
        ///  Debug用のWalkingLineを書く
        /// </summary>
        internal bool debugDrawAimWalkingLine = false;
        /// <summary>
        /// UnitのAI
        /// </summary>
        internal AI.AIController aiController;
        /// <summary>
        /// hudを描写するためのWindow
        /// </summary>
        internal UI.Overlay.HUDWindow hudWindow;
        /// <summary>
        /// ForcusModeを表示する系のUI
        /// </summary>
        internal UI.Overlay.BottomUIPanel focusModeUI;
        /// <summary>
        /// 射線を飛ばす際のレイヤーマスク
        /// </summary>
        private int shootTargetLayerMask;
        /// <summary>
        /// Unitが死亡しているか
        /// </summary>
        public bool IsDead
        {
            get
            {
                return (gameObject == null || CurrentParameter.HealthPoint <= 0);
            }
        }
        /// <summary>
        /// UnitのCircletriggerがGimmickObjectに触れているか
        /// </summary>
        internal bool OnTriggerObject
        {
            get
            {
                return circleTrigger.objectsInTrigger.Count != 0;
            }
        }

        /// <summary>
        /// 毎フレーム更新される現在ActiveなUnitからの敵ユニットとの距離 UnitsControllerからUnitがActiveな間更新される disactiveなら0
        /// </summary>
        public Dictionary<UnitController, float> EnemyAndDistanceDict = new Dictionary<UnitController, float>();
        /// <summary>
        /// Unitの位置しているTileCell (TilesControllerから更新される)
        /// </summary>
        internal Map.TileCell tileCell;

        /// <summary>
        /// UnitとそのItemのアニメーションを停止する
        /// </summary>
        public bool PauseAnimation
        {
            get => TpsController.PauseAnimation;
            set
            {
                TpsController.PauseAnimation = value;
                itemController.PauseAnimation = value;
            }
        }

        /// <summary>
        /// 使用中もしくは近くのGimmickObject
        /// </summary>
        public GimmickObject GimmickObject { private set; get; } 

        /// <summary>
        /// Gimmickを現在使用中であるか
        /// </summary>
        public bool IsUsingGimmickObject
        {
            get
            {
                if (GimmickObject == null) return false;
                return GimmickObject.IsUsingGimmick(this);
            }
        }

        /// <summary>
        /// カウンター攻撃可能であるか
        /// </summary>
        public bool IsCounterAttackable
        {
            get
            {
                return !(IsUsingGimmickObject || TpsController.FollowingGimmickObject) && itemController.CanCounterAttack;
            }
        }

        /// <summary>
        /// Gimmickを現在使用可能であるか
        /// </summary>
        public bool CanUseGimmickObject
        {
            get
            {
                if (GimmickObject == null) return false;
                if (GimmickObject.IsUsingGimmick(this)) return false;
                return GimmickObject.CanUseIt;
            }
        }

        /// <summary>
        /// アイテムホルダーの中で最も攻撃力が高くCounterattack可能なもの
        /// </summary>
        public ItemHolder CounterAttackableItemHolder
        {
            get
            {
                itemController.ItemHolders.FindAll(h => h.Data.Counterattack).OrderByDescending(h => h.Data.Attack).FirstOrDefault();
                return itemController.ItemHolders.Find(h => h.Data.Counterattack);
            }
        }

        #endregion

        #region  Init
        private void Awake()
        {
            ThisObject = gameObject;

            TpsController = ThisObject.GetComponent<Control.ThirdPersonUserControl>();
            itemController.tpsController = TpsController;
            NavMeshObstacle = ThisObject.GetComponent<NavMeshObstacle>();
            Rigidbody = ThisObject.GetComponent<Rigidbody>();

            // Navigation
            NavMeshAgent = ThisObject.GetComponent<NavMeshAgent>();
            // アニメーション移動の際はコメントアウト
            NavMeshAgent.updatePosition = false;
            NavMeshAgent.updateRotation = false;
            NavMeshAgent.enabled = false;

            generalParameters = GameManager.Instance.GeneralParameter;

            aiController = GetComponent<AI.AIController>();
            shootTargetLayerMask = LayerMask.GetMask(new string[] { "Object", "ShootTarget" });
        }
            
        /// <summary>
        /// dataをtacticsScene上のUnitに展開する
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal IEnumerator SetUnit(UnitData data, UnitAttribute unitAttribute)
        {
            isFreeMode = false;

            watchTabletCamera.Priority = 0;
            Attribute = unitAttribute;

            CurrentParameter = new CurrentUnitParameter(data);
            UnitName = CurrentParameter.Data.Name;
            if (data.MyItems.Find(h => h.Data != null) != null)
                itemController.Initialize(data.MyItems);
            else
                itemController.Initialize(data.Data.BaseItems);
            yield return StartCoroutine(itemController.SetItem(itemController.CurrentItemHolder));

            circleTrigger.OnTriggerEnterAction += TriggerEnter;
            circleTrigger.OnTriggerStayPositionAction += TriggerStay;
            circleTrigger.OnTriggerExitAction += TriggerExit;

            var originHead = bodyParts.Find(p => p.partType.Equals(TargetPartType.Head));
            if (originHead.partObjects.IndexAt_Bug(0, out var myHead))
                myEyesLocationObject = myHead;
            else
                PrintError(this.ToString(), ": Eyes location is not set. Set object as TargetPartType.Head.");
        }

        private void FixedUpdate()
        {
            if (!IsActive) return;
            if (isFreeMode)
                return;

            if (WorkState != WorkState.Gimmick)
            {
                // Update character state
                if (TpsController.IsMoving && !IsAutoControlling)
                {
                    WorkState = WorkState.Walk;

                }
                else if (!TpsController.IsMoving && WorkState == WorkState.Walk && !IsAutoControlling)
                {
                    WorkState = WorkState.Wait;
                }
            }

            // ギミックへの対処
            if (!isAiControlled)
            {
                UpdateUIOfGimmick();
            }
            
        }

        #endregion

        #region BottomMessages UI

        /// <summary>
        /// GimmickObjectに接近した際や使用中に表示するUIを表示する
        /// </summary>
        private void UpdateUIOfGimmick()
        {
            if (WorkState != WorkState.Gimmick)
            {
                // 現在ギミックを使用していない
                GimmickObject = tileCell.GetNearlyGimmickObject(this.transform);
                // 近くにギミックがある場合
                if (GimmickObject)
                {
                    if (GimmickObject.CanUseIt)
                    {
                        // 使用可のギミックを選択中
                        if (isAlreadyActioned)
                            focusModeUI.ShowBottomMessage("Unit has already actioned", GimmickObject.DetailMessageWhenCloseGimmick);
                        else
                            focusModeUI.ShowBottomMessage(GimmickObject.MessageWhenCloseGimmick, GimmickObject.DetailMessageWhenCloseGimmick);
                    }
                    else if (GimmickObject.IsDestroyed)
                    {
                        // 破壊済みのギミックを選択中
                        focusModeUI.ShowBottomMessage(GimmickObject.MessageWhenDestroyed, GimmickObject.DetailMessageWhenDestroyed);
                    }
                    else
                    {
                        // 使用不可のギミックを選択中
                        focusModeUI.ShowBottomMessage(GimmickObject.MessageWhenActionCountIsZero, GimmickObject.DetailMessageWhenCloseGimmick);
                    }
                }
            }
            else
            {
                if (!isAlreadyActioned)
                {
                    // ギミックを使用中
                    focusModeUI.ShowBottomMessage(GimmickObject.MessageWhileUsing, GimmickObject.DetailMessageWhileUsing);
                }
                else
                {
                    // ギミックを使用済み 
                    focusModeUI.ShowBottomMessage("Unit has already actioned", GimmickObject.DetailMessageWhileUsing);
                }
            }
        }

        #endregion

        // Unitの行動を操作する関数群
        #region Action Controller
        /// <summary>
        /// 攻撃モーションを開始する
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        public IEnumerator RifleAttackTo(UnitController target, int damage, Action OnComplete)
        {
            IsAutoControlling = true;
            WorkState = WorkState.Animating;
            yield return StartCoroutine(TpsController.RotateTo(target.ThisObject.transform.position));
            target.TpsController.OverShoulderCamera.Priority = 20;
            TpsController.aimCamera.Priority = 0;
            yield return StartCoroutine(WaitForSeconds(cameraUserController.CameraChangeDuration));
            
            TpsController.AimItem(true);
            yield return StartCoroutine(WaitForSeconds(1));

            yield return StartCoroutine(TpsController.UseItem());
            itemController.ShootAction();
            hudWindow.ShowDamage(target.hudPosition.position, damage);
            StartCoroutine(target.RecieveDamage(this, this.itemController.CurrentItemHolder.Data.AttackType, damage));
            yield return StartCoroutine(WaitForSeconds(0.5f));

            TpsController.AimItem(false);
            yield return StartCoroutine(WaitForSeconds(1f));
            target.TpsController.OverShoulderCamera.Priority = 0;
            yield return StartCoroutine(WaitForSeconds(cameraUserController.CameraChangeDuration));

            WorkState = WorkState.Wait;
            OnComplete?.Invoke();
            isAlreadyActioned = true;
            IsAutoControlling = false;
        }

        /// <summary>
        /// ライフルによるカウンター攻撃
        /// </summary>
        /// <param name="target"></param>
        /// <param name="damage"></param>
        /// <param name="OnComplete"></param>
        /// <returns></returns>
        public IEnumerator RifleCounterAttackTo(UnitController target, int damage, Action OnComplete)
        {
            IsAutoControlling = true;
            WorkState = WorkState.Animating;
            yield return StartCoroutine(TpsController.RotateTo(target.ThisObject.transform.position));
            TpsController.AimItem(true);

            yield return StartCoroutine(TpsController.UseItem());
            itemController.ShootAction();
            hudWindow.ShowDamage(target.hudPosition.position, damage);
            StartCoroutine(target.RecieveDamage(this, this.itemController.CurrentItemHolder.Data.AttackType, damage));

            TpsController.AimItem(false);
            yield return StartCoroutine(WaitForSeconds(1f));

            // End of animation
            WorkState = WorkState.Wait;
            OnComplete?.Invoke();
            IsAutoControlling = false;
        }

        /// <summary>
        /// activeからtargetに攻撃する
        /// </summary>
        /// <param name="target"></param>
        public IEnumerator AIRifleAttack(UnitController target, bool hit)
        {
            if (target == null)
                yield break;

            IsAutoControlling = true;
            WorkState = WorkState.Animating;

            yield return StartCoroutine(TpsController.RotateTo(target.ThisObject.transform.position));
            //var distance = Vector3.Distance(target.transform.position, transform.position);
            yield return new WaitForSeconds(0.7f);

            var damage = hit ? (float)itemController.CurrentItemHolder.Data.Attack : 0;
            yield return StartCoroutine(RifleAttackTo(target, (int)damage, null));

            yield return new WaitForSeconds(0.2f);
            WorkState = WorkState.Wait;
        }


        /// <summary>
        /// Grenadeでの攻撃モーションを開始する
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="units">すべてのUnits</param>
        /// <returns></returns>
        public IEnumerator GrenadeAttackTo(Arc.DrawArc arc, List<UnitController> units, TilesController tilesController, UI.Overlay.HUDWindow hud, Action OnComplete)
        {
            IsAutoControlling = true;
            WorkState = WorkState.Animating;
            var hitPosition = arc.HitPosition;
            var arcPositions = arc.ArcPositions;

            yield return StartCoroutine(WaitForSeconds(0.2f));
            yield return StartCoroutine(TpsController.RotateTo(hitPosition));
            yield return StartCoroutine(WaitForSeconds(0.5f));

            // グレネードを投げる
            var throwVelocity = arc.InitialVelocity;
            var itemCoroutine = itemController.ThrowAction(arcPositions, throwVelocity);
            yield return StartCoroutine(itemCoroutine);
            Vector3 position = (Vector3)itemCoroutine.Current;

            // 爆発したため範囲内の敵にダメージを与える
            var damagedUnits = GetGrenadeAttackPoint(position, units);
            damagedUnits.ForEach(u =>
            {
                var damage = GameManager.Instance.RandomController.Probability(u.rate) ? u.damage : 0;
                Print($"Grenade Hit: {u.target}, Dam{damage}");
                StartCoroutine( u.target.RecieveDamage(this, itemController.CurrentItemHolder.Data.AttackType, (int)damage));
                hud.ShowDamage(u.target.hudPosition.position, (int)damage);
            });
            // 爆発したため範囲内のGimmickを破壊する
            var destroiedGimmicks = tilesController.GetGimmicksWithinRadius(position, itemController.CurrentItemHolder.Data.Range, false);
            destroiedGimmicks.ForEach(g =>
            {
                StartCoroutine(g.DestroyAnimation());
            });

            yield return new WaitForSeconds(2);

            // TODO グレネードの再取得アニメーション
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(itemController.ResetItem());

            WorkState = WorkState.Wait;
            OnComplete?.Invoke();
            IsAutoControlling = false;
        }


        /// <summary>
        /// NavMeshを使用してLocationに移動
        /// </summary>
        /// <param name="location"></param>
        public IEnumerator MoveTo(Vector3 location)
        {
            IsAutoControlling = true;
            WorkState = WorkState.Walk;
            NavMeshObstacle.enabled = false;
            NavMeshAgent.enabled = true;
            yield return StartCoroutine(WaitForSeconds(1.5f));

            // navMeshAgent.SetDestination(location);

            location.y = ThisObject.transform.position.y;
            yield return StartCoroutine(TpsController.AutoMove(location, NavMeshAgent, debugDrawAimWalkingLine));

            // yield return new WaitUntil(() => navMeshAgent.remainingDistance == 0);

            NavMeshAgent.isStopped = true;

            NavMeshAgent.enabled = false;
            IsAutoControlling = false;
            WorkState = WorkState.Wait;

            yield return true;
        }

        /// <summary>
        /// Mortarの使用位置に移動する
        /// </summary>
        /// <param name="mortorGimmick"></param>
        /// <returns></returns>
        public IEnumerator UseMotarGimmick(MortarGimmick mortorGimmick)
        {
            GimmickObject = mortorGimmick;
            GimmickObject.AddUnitToUse(this);
            WorkState = WorkState.Gimmick;
            transform.DOMove(mortorGimmick.UnitPosition.transform.position, 0.3f);
            transform.DORotate(mortorGimmick.UnitPosition.transform.rotation.eulerAngles, 0.3f);
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>
        /// Mortarギミックの発射アニメーション
        /// </summary>
        /// <param name="mortarGimmick"></param>
        /// <returns></returns>
        public IEnumerator ShootMortarGimmick(MortarGimmick mortarGimmick)
        {
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// UnitをMortarから離れる動作をする
        /// </summary>
        /// <returns></returns>
        public IEnumerator RemoveFromMortarGimmick()
        {
            if (GimmickObject == null)
            {
                PrintError($"RemoveFromMortoarGimmick: GimmickObject is null in {this}");
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
            WorkState = WorkState.Wait;
            GimmickObject.RemoveUnitToUse(this);
            GimmickObject = null;
        }

        /// <summary>
        /// Rotation分回転する
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public IEnumerator RotateTo(Quaternion rotation)
        {
            var yDegree = rotation.eulerAngles.y * Mathf.Deg2Rad;
            var targetPosition = new Vector3(MathF.Sin(yDegree), 0, MathF.Cos(yDegree));
            targetPosition += transform.position;
            yield return StartCoroutine(TpsController.RotateTo(targetPosition));

            yield return true;
        }

        /// <summary>
        /// <c>MoveTo(Vector3)</c>での歩行状態を停止する
        /// </summary>
        public void CancelAIWalking()
        {
            TpsController.CancelAutoMoving();
        }

        #endregion

        #region CounterAttack
        /// <summary>
        /// 敵が侵入してきた際のカウンター攻撃
        /// </summary>
        public IEnumerator CounterattackWhenEnemyEnter(UnitController target)
        {
            var counterAttackWeapon = itemController.GetCounterAttackWeapon();

            if (counterAttackWeapon == null)
            {
                // カウンター攻撃可能な武器を持っていない
                Log.Info($"Counterattack: {this}, No item to counterattack.");
                yield break;
            }
            else if (counterAttackWeapon != itemController.CurrentItemHolder)
            {
                // カウンター攻撃可能な武器を持っているが現在装備している武器と異なる
                Log.Info($"Counterattack: {this}, Can't counterattack with {itemController.CurrentItem}. Change to {counterAttackWeapon}");
                yield return StartCoroutine(itemController.SetItem(counterAttackWeapon));
            }

            TpsController.AimItem(true);
            if (TpsController.FollowingGimmickObject == null)
                yield return StartCoroutine(TpsController.RotateTo(target.ThisObject.transform.position));
            var dist = target.EnemyAndDistanceDict[this];
            if (dist == 0)
                yield break;

            if (itemController.CurrentItemHolder.Data != null)
            {
                var damage = itemController.CurrentItemHolder.Data.Attack;
                var hitRate = generalParameters.weaponReductionCurve.Evaluate(dist) * generalParameters.counterattackHitRate;
                damage = GameManager.Instance.RandomController.Probability(hitRate) ? damage : 0;

                Log.Info($"Cunterattack: {this} counterattack to {target} with {itemController.CurrentItem}");

                if (itemController.CurrentItemHolder.Data.FocusModeType == FocusModeType.Gun)
                {
                    yield return StartCoroutine(RifleCounterAttackTo(target, damage, null));
                }
            }
        }

        /// <summary>
        /// このユニットがカウンター攻撃を受けるときにReceiveDamageが行われるまで移動を停止してダメージアニメーション終了まで待つ
        /// </summary>
        internal IEnumerator WaitUntilCounterAttack()
        {
            // 現在進行中のアニメーションを一時停止する
            TpsController.PauseAnimation = true;

            // RecieveDamageが呼び出されるまで待つ
            IsDamageAnimating = true;

            IsAutoControlling = true;
            WorkState = WorkState.Wait;
            yield return new WaitUntil(() => IsDamageAnimating == false);
            IsAutoControlling = false;
            TpsController.PauseAnimation = false;
        }
        #endregion

        // 状態変化によって呼び出される関数郡
        #region Unit behavior
        /// <summary>
        ///  UnitsControllerより最初のターンが開始した時すべてのUnitが呼び出されるFunc
        /// </summary>
        internal void StartFirstTurn()
        {
            aiController.FindOutRoutine();
        }

        /// <summary>
        /// ターン開始時に呼び出される
        /// </summary>
        internal void StartTurn()
        {
            IsAlreadyMovedDifferentTile = false;
            IsActive = true;
        }


        /// <summary>
        /// ターン終了時に呼び出される
        /// </summary>
        internal void EndTurn()
        {
            IsActive = false;
            EnemyAndDistanceDict.Keys.ToList().ForEach(e => EnemyAndDistanceDict[e] = 0);
        }

        // <summary>
        /// Unitが別のTileに移動した際に呼び出される
        /// </summary>
        internal void MovedDifferentTile(TileCell tileCell)
        {
            this.tileCell = tileCell;
            IsAlreadyMovedDifferentTile = true;
        }

        /// <summary>
        /// ユニット死亡時に呼び出される
        /// </summary>
        /// UnitsControllerから呼び出している
        internal void UnitDead()
        {
            aiController.EndFindOutRoutine();
            IsActive = false;
        }


        /// <summary>
        /// 攻撃が敵から試みられてユニットがダメージを受けた場合
        /// </summary>
        /// <param name="from">攻撃を試みたUnit グレネードなど発射元が確認できないのはnull</param>
        /// <param name="point">ダメージ量</param>
        /// <returns></returns>
        public IEnumerator RecieveDamage(UnitController from, AttackType attackType, int point)
        {
            print(WorkState);
            var previousState = WorkState == WorkState.Animating ? WorkState.Wait : WorkState;
            WorkState = WorkState.Animating;
            // Flag of damage animation
            IsDamageAnimating = true;

            // Change parameters
            if (!isGodMode)
                CurrentParameter.HealthPoint -= point;

            // Send Event
            if (damageEventArgs == null)
                damageEventArgs = new DamageEventArgs();
            damageEventArgs.point = point;
            damageEventArgs.transform = ThisObject.transform;
            damageEventArgs.damageFrom = from;
            damageEventArgs.attackType = attackType;

            damageHandler(this, damageEventArgs);

            //TODO: ダメージアニメーションを再生
            if (CurrentParameter.HealthPoint <= 0)
            {
                // TODO: 死亡時アニメーション
                yield return new WaitForSeconds(3);
            }
            else
            {
                // TODO: 通常ダメージアニメーション

                yield return new WaitForSeconds(1);

                if (TpsController.FollowingGimmickObject || IsUsingGimmickObject)
                {
                    // Gimmick使用時もしくはGimmickタイプの壁に沿っている場合
                    var destroyable = attackType.IsDestroyable(IsUsingGimmickObject ? GimmickObject : TpsController.FollowingGimmickObject); 
                    if (destroyable)
                    {
                        // 使用中のGimmickが破壊されたときのアニメーション
                        // 爆発に飛ばされる等のアニメーション

                        // Gimmickの使用を中止する 棒立ちモードに移行
                    }
                    else
                    {
                        // 使用中のGimmickが破壊されなかったときのアニメーション
                        // 身をかがめる等のアニメーション
                    }
                }
                else
                {
                    if (attackType.IsKnownSource)
                    {
                        // 攻撃された敵の場所が即座に判別可能
                        StartCoroutine(TpsController.RotateTo(from.transform.position));
                        aiController.SetForceFindOut(from, true);

                    }
                    else if (attackType.IsKnownDirection)
                    {
                        yield return StartCoroutine(TpsController.RotateTo(from.transform.position));
                        if (from.EnemyAndDistanceDict.TryGetValue(this, out var ditToEnemy))
                        {
                            // 目視で発見できる距離の最大値
                            var detectionMaxDistance = generalParameters.DetectionDistanceCurve.GetLastTime();
                            if (ditToEnemy < detectionMaxDistance)
                            {
                                // 攻撃された方向に敵がいる
                                aiController.SetForceFindOut(from, true);
                            }
                            else
                            {
                                // 攻撃された方向は分かるが距離で敵を発見できない
                                aiController.OnAttackedFromUnfindedEnemyKnowDirection(from);
                            }
                        }
                        else
                        {
                            // 遮蔽物で遮られた敵から攻撃を受けた
                            aiController.OnAttackedFromUnfindedEnemyKnowDirection(from);
                        }
                    }
                    else
                    {
                        // 攻撃された方向が分からない
                        // 設置型爆弾など
                        aiController.OnAttackedFromUnfindedEnemy(from);
                    }
                }

            }

            // Write Some thing of damage animation
            yield return new WaitForSeconds(generalParameters.tacticsTurnEndInterval);

            IsDamageAnimating = false;
            WorkState = previousState;
        }


        /// <summary>
        /// 攻撃を試みられたもののハズレた場合
        /// </summary>
        /// <returns></returns>
        internal IEnumerator MissDamage()
        {
            // Send Event
            if (damageEventArgs == null)
                damageEventArgs = new DamageEventArgs();
            damageEventArgs.point = 0;
            damageEventArgs.transform = ThisObject.transform;
            damageHandler(this, damageEventArgs);

            // Flag of damage animation
            IsDamageAnimating = true;

            yield return new WaitForSeconds(generalParameters.tacticsTurnEndInterval);

            IsDamageAnimating = false;
        }

        /// <summary>
        /// 使用中のギミックが破壊された場合沿っているGimmickにより呼び出される
        /// </summary>
        public void UsingGimmickIsDestroied(GimmickObject gimmickObject)
        {
            if (IsUsingGimmickObject && GimmickObject == gimmickObject)
                TpsController.UsingGimmickIsDestroied(gimmickObject);
            else if (TpsController.FollowingGimmickObject == gimmickObject)
                TpsController.FollowingGimmickIsDestroied(gimmickObject);

            GimmickObject = null;
        }

        #endregion

        // Collisionの衝突の際に呼び出される関数群
        #region CollisionDetector Events
        private float previousTriggerAngle = float.MinValue;
        private void TriggerEnter(Collider obj)
        {

            if (obj.transform.localScale.y < 0.5)
            {
                // 土のうなどの低い物に隣接
                //Print("TriggerEnter", obj);
            }
            else
            {
                // 壁等の高いものに隣接
                //Print("TriggerEnter: HighObject", obj);
            }
        }

        /// <summary>
        /// ギミックオブジェクトに接触している場合呼び出され続ける
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="position"></param>
        private void TriggerStay(GameObject obj, Vector3 position)
        {
            if (!IsActive)
                return;

            const float TargetLocation = 0.57f;
            var angle = Angle_bug(position, transform.position) - 180;

            if (angle < 0)
                angle = Math.Abs(angle);
            else if (angle < 180)
                angle = 180 + (180 - angle);

            // Angleの推移の平滑化
            if (previousTriggerAngle != float.MinValue)
            {
                if (previousTriggerAngle - angle < -350)
                {
                    // angle=360 && previousTriggerAngle=0　付近のとき 左回転中
                    var _previous = previousTriggerAngle + 360;
                    angle = 0.7f * _previous + 0.3f * angle;
                    if (angle > 360)
                        angle -= 360;

                }
                else if (previousTriggerAngle - angle > 350)
                {
                    // angle=0 && previousTriggerAngle=360 ふきんのとき 右回転中
                    var _angle = angle + 360;
                    angle = 0.7f * previousTriggerAngle + 0.3f * _angle;
                    if (angle > 360)
                        angle -= 360;

                }
                else
                {
                    angle = 0.7f * previousTriggerAngle + 0.3f * angle;
                }
            }
            previousTriggerAngle = angle;

            var rad = (angle) * Mathf.Deg2Rad;
            var pos = transform.position;
            pos.x += TargetLocation * Mathf.Cos(rad);
            pos.z += -TargetLocation * Mathf.Sin(rad);

            if (float.IsNaN(pos.x))
            {
                print("pos.x is Nan");
                Print($"Angle: {angle}, OriginPos: {position}, : {transform.position}");
            }

            RaderTargetPosition = pos;
        }

        private void TriggerExit(Collider obj)
        {
            previousTriggerAngle = float.MinValue;
            RaderTargetPosition = null;
        }

        #endregion


        // 様々な状況判断に使用される関数群
        #region Calclation

        /// <summary>
        /// 対象のユニットが敵か味方か判断する
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool IsEnemyFromMe(UnitController unit)
        {
            var targetAttribute = unit.Attribute;
            targetAttribute = targetAttribute == UnitAttribute.NPC ? UnitAttribute.PLAYER : targetAttribute;
            if (targetAttribute.Equals(Attribute))
                return false;
            else
                return true;
        }

        /// <summary>
        ///  ターゲットに対する命中率とダメージを計算する Rayを同一Objectに複数回飛ばしているため処理は重め
        ///  厳密な当たり判定以外は軽い<c>GetRayDistanceTo(target,location</c>を使う
        /// </summary>
        /// <param name="target"></param>
        /// <returns>(部位, (ダメージ量, 0~1の命中率))</returns>
        public Dictionary<TargetPartType, (int damage, float rate)> GetAttackPoint(UnitController target, Vector3? location = null)
        {
            float SphereCastRadius = generalParameters.BulletCastRadius / 10;

            static float Minus(float origin, float minus)
            {
                var result = origin - minus;
                return result < 0 ? 0 : result;
            }

            float distance;
            if (location == null)
                distance = Vector3.Distance(transform.position, target.transform.position);
            else
                distance = Vector3.Distance(location.GetValueOrDefault(), target.transform.position);



            var origin = myEyesLocationObject.transform.position;
            if (location != null)
            {
                var _location = location.GetValueOrDefault();
                origin = new Vector3(_location.x, origin.y, _location.z);
            }

            var activeTarges = new List<TargetPartType>();
            var mask = LayerMask.GetMask(new string[] { "Object", "ShootTarget" });
            foreach(var part in target.bodyParts)
            {
                foreach(var raycastPart in part.partObjects)
                {
                    var direction = (raycastPart.transform.position - origin);
                    if (Physics.SphereCast(origin, SphereCastRadius, direction, out var hit, 100, mask))
                    {
                        if (!raycastPart.Equals(hit.collider.gameObject))
                            continue;
                        activeTarges.Add(part.partType);
                        break;
                    }
                }
            }

            var hitRate = itemController.HitRate(distance, target);

            // しゃがんでいるかの判定
            if (target.TpsController.IsCrouching)
            {
                if (target.IsCoveredFromEnemy(transform.position))
                {
                    Print($"GetAttackPoint: {target} is covered from {this} with {target.TpsController.FollowingGimmickObject}");
                    // TargetはUnitから見てカバーされた位置にある
                    hitRate -= generalParameters.CoverCloakingRate;
                }
            }

            var body = activeTarges.Contains(TargetPartType.Body) ? Minus(hitRate, 0) : 0;
            var head = activeTarges.Contains(TargetPartType.Head) ? Minus(hitRate, 0.3f) : 0;
            var arm = activeTarges.Contains(TargetPartType.Arm) ? Minus(hitRate, 0.2f) : 0;
            var leg = activeTarges.Contains(TargetPartType.Leg) ? Minus(hitRate, 0.2f) : 0;

            if (itemController.CurrentItemHolder.Data.TargetType == TargetType.Object)
            {
                // Human系に対物系の武器で攻撃 命中率にデバフをかける
                body = Minus(body, ItemData.DecreaseHitToMan);
                head = Minus(head, ItemData.DecreaseHitToMan);
                arm = Minus(arm, ItemData.DecreaseHitToMan);
                leg = Minus(leg, ItemData.DecreaseHitToMan);
            }

            var bodyDm = (float)itemController.CurrentItemHolder.Data.Attack;
            var headDm = bodyDm * ItemData.HeadShotBonus;
            var armDm = bodyDm * ItemData.LimbsShotBonus;
            var legDm = armDm;

            var output = new Dictionary<TargetPartType, (int damage, float rate)>();
            output[TargetPartType.Body] = ((int)bodyDm, body);
            output[TargetPartType.Head] = ((int)headDm, head);
            output[TargetPartType.Arm] = ((int)armDm, arm);
            output[TargetPartType.Leg] = ((int)legDm, leg);

            return output;
        }


        /// <summary>
        /// AIが攻撃する際の命中確率を取得
        /// </summary>
        /// <param name="target"></param>
        /// <param name="location"></param>
        /// <returns>命中確率 100分率</returns>
        public float GetAIAttackRate(UnitController target, Vector3? location = null)
        {
            var pts = GetAttackPoint(target, location);

            var count = 0;
            var hitRate = pts.ToList().Sum(t =>
            {
                if (t.Value.rate != 0)
                    count++;
                return t.Value.rate;
            });
            if (count == 0)
                return 0;

            return hitRate / count;
        }

        /// <summary>
        /// AIが攻撃する際の Enemy, ActiveUnitの両方の位置がForcastの場合の命中確率
        /// </summary>
        /// <param name="targetLocation"></param>
        /// <param name="origin"></param>
        /// <returns>0~1</returns>
        public float GetAIForcastAttackRate(UnitController target, Vector3 targetLocation, Vector3 origin)
        {
            var ray = new Ray(origin, targetLocation - origin);
            var mask = LayerMask.GetMask(new string[] { "Object" });

            var distance = Vector3.Distance(targetLocation, origin);
            if (Physics.Raycast(ray, out var hit, 100, mask, QueryTriggerInteraction.UseGlobal))
            {
                if (hit.distance < distance)
                    distance = float.MaxValue;
            }
            else
            {
                distance = float.MaxValue;
            }
            return itemController.HitRate(distance, target);
        }

        /// <summary>
        /// Grenadeで攻撃した際の命中率とダメージを取得する
        /// </summary>
        /// <param name="position"></param>
        public List<(UnitController target, float rate, float damage)> GetGrenadeAttackPoint(Vector3 position, List<UnitController> allUnits)
        {
            var output = new List<(UnitController target, float rate, float damage)>();
            var hitRange = itemController.CurrentItemHolder.Data.Range;
            var layer = LayerMask.GetMask("Object", "ShootTarget");
            
            foreach(var unit in allUnits)
            {
                var dist = Vector3.Distance(unit.transform.position, position);
                if (dist > hitRange) continue;

                var totalCount = 0;
                var hitCount = 0;
                if (!unit.IsCoveredFromEnemy(position))
                {
                    hitCount = unit.bodyParts.Sum(p => p.partObjects.Sum(o =>
                    {
                        totalCount++;
                        Ray ray = new Ray(position, o.transform.position - position);
                        if (Physics.Raycast(ray, out var hit, 100, layer))
                        {
                            if (hit.collider.gameObject == o)
                                return 1;
                        }
                        return 0;
                    }));
                }
                else
                {
                    Print($"{unit} is covered from grenede {position}");
                }

                var _output = (unit, 0f, 0f);

                if (hitCount != 0 || totalCount != 0)
                {
                    _output.Item2 = generalParameters.grenadeHitRate.Evaluate(dist / (float)hitRange);
                    var partRate = (float)hitCount * 1.5f / (float)totalCount;
                    _output.Item3 = itemController.CurrentItemHolder.Data.Attack* partRate;
                }

                output.Add(_output);
            }
            Print("Grenade Target:\n", string.Join("\n", output.ConvertAll(o => $"{o.target}, Dist{Vector3.Distance(o.target.transform.position, position)}, Rate{o.rate}, Dam{o.damage}")));

            return output;
        }

        /// <summary>
        /// GetRayDistanceToで処理を軽くするためすでに射線が計算されている移動していないtargetの位置
        /// </summary>
        private readonly Dictionary<UnitController, (Vector3 loc, float dist)> RayTargetAndLocationPair =  new Dictionary<UnitController, (Vector3, float)>();
        /// <summary>
        /// GetRayDistanceToで処理を軽くするためのすでに射線が計算されている移動してないmyunitの位置
        /// </summary>
        private Vector3 PreviousRayShootLocation;
        /// <summary>
        /// ターゲットまでの距離を取得 ShootTargetLayerMaskに属するオブジェクトで射線が通るかどうかで判断
        /// </summary>
        /// <param name="target"></param>
        /// <returns>射線が通らない場合は0</returns>
        public float GetRayDistanceTo(UnitController target, Vector3? location = null)
        {
            float SphereCastRadius = generalParameters.BulletCastRadius  / 10;
            var origin = myEyesLocationObject.transform.position;

            static bool IsNear(Vector3 a, Vector3 b)
            {
                const float nearDistance = 1;
                var dist = Vector3.Distance(a, b);
                return dist < nearDistance;
            }

            // MyUnitの位置とTargetの位置が同じであるとき計算を省略して過去のものを返す
            if (PreviousRayShootLocation != null &&
                IsNear(PreviousRayShootLocation, origin) &&
                RayTargetAndLocationPair.TryGetValue(target, out var previous))
            {
                if (IsNear(previous.loc, target.transform.position))
                {
                    return previous.dist;
                }
            }

            if (location != null)
            {
                var _location = location.GetValueOrDefault();
                origin = new Vector3(_location.x, origin.y, _location.z);
            }

            PreviousRayShootLocation = transform.position;
            RaycastHit hit;
            foreach (var part in target.bodyParts)
            {

                if (part.partObjects == null)
                    continue;
                if (!part.partObjects.IndexAt(0, out var raycastPart))
                    continue;
                var direction = (raycastPart.transform.position - origin);
                if (Physics.SphereCast(origin, SphereCastRadius, direction, out hit, 100, shootTargetLayerMask))
                {
                    //　var ray = new Ray(origin, direction);
                    //　Debug.DrawRay(ray.origin, direction * 100, Color.green, 10);
                    if (raycastPart.Equals(hit.collider.gameObject))
                    {
                        RayTargetAndLocationPair[target] = (target.transform.position, hit.distance);
                        return hit.distance;
                    }
                }
            }

            RayTargetAndLocationPair[target] = (target.transform.position, 0);
            return 0;
        }

        /// <summary>
        /// Unitが<c>enemyPosからカバーされた位置にあるかどうか</c>
        /// </summary>
        /// <param name="enemyPos"></param>
        /// <returns></returns>
        internal bool IsCoveredFromEnemy(Vector3 enemyPos)
        {

            if (TpsController.FollowingGimmickObject == null)
                return false;

            var posC = new Vector2(TpsController.FollowingWallTouchPosition.x,
                                   TpsController.FollowingWallTouchPosition.z);
            var posE = new Vector2(enemyPos.x, enemyPos.z);
            var posG = new Vector2(transform.position.x, transform.position.z);
            var rad = Utility.RadianOfTwoVector(posE - posC, posG - posC);
            var deg = rad / (Mathf.PI / 180);

            return deg > 100;
        }



        /// <summary>
        /// AIが攻撃した際の部位で統一のダメージ量を出す
        /// </summary>
        /// <returns></returns>
        public int GetAIAttackDamage(UnitController target)
        {
            return itemController.CurrentItemHolder.Data.Attack;
        }

        /// <summary>
        /// positionがNavmeshObstacleに入っているかどうか
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool IsInMyArea(Vector3 position)
        {
            var obstaclePos = gameObject.transform.position + NavMeshObstacle.center;
            if (NavMeshObstacle.shape == NavMeshObstacleShape.Capsule)
            {
                var distance = Vector3.Distance(obstaclePos, position);
                return distance < NavMeshObstacle.radius * 1.6;
            }
            else
            {
                var min = obstaclePos - (NavMeshObstacle.size / 2);
                var max = obstaclePos + (NavMeshObstacle.size / 2);
                var biggerThanMin = min.x < position.x && min.z < position.z;
                var smallerThanMax = position.x < max.x && position.z < max.z;

                return biggerThanMin && smallerThanMax;
            }
        }

        /// <summary>
        /// <c>target</c>がunitの前方にいるかどうか
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsTargetFrontOfUnit(UnitController target)
        {
            if (target.ThisObject == null)
                return false;
            var angle = Angle(ThisObject.transform.position, target.transform.position);
            angle = angle + 360 - this.transform.rotation.eulerAngles.y;
            if (angle >= 360)
                angle -= 360;

            var limitAngle = generalParameters.DetectionViewAngle / 2;
            var limitAngle2 = 360 - limitAngle;

            return angle < limitAngle || limitAngle2 < angle;
        }

        #endregion


        /// <summary>
        /// <c>PauseAnimation</c>に対応したWaitForSeconds
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        private IEnumerator WaitForSeconds(float duration)
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < duration * 1000)
            {
                if (PauseAnimation)
                {
                    var startStopping = DateTime.Now;
                    while (PauseAnimation)
                        yield return null;
                    var stopTime = (DateTime.Now - startStopping).TotalMilliseconds;
                    start.AddMilliseconds(stopTime);
                }
                else
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// UnitControllerのbodyPartsをすべてアクティブにする
        /// </summary>
        /// <param name="active"></param>
        internal void SetTargetPartsActive(bool active)
        {
            bodyParts.ForEach(p => p.partObjects.ForEach(p => p.SetActive(active)));
        }

        public override string ToString()
        {
            return $"{Attribute}: {CurrentParameter.Data.Name}";
        }

        /// <summary>
        /// Unitの詳細情報
        /// </summary>
        /// <returns></returns>
        public string GetInfo()
        {
            var txt = $"Name: {CurrentParameter.Data.Name}, Attribute: {Attribute}, ";
            txt += $"Lv: {CurrentParameter.Data.Level}, Exp: {CurrentParameter.Data.Exp}\n";
            txt += $"Is Commander: {CurrentParameter.Data.IsCommander}\n";
            txt += $"HP: {CurrentParameter.HealthPoint}/{CurrentParameter.Data.HealthPoint + CurrentParameter.Data.AdditionalHealthPoint}, ";
            txt += $"Attack: { itemController.BaseAttackPoint}, Weight: {CurrentParameter.Data.Weight}, ";
            return txt;
        }
    }

    #region Event arguments
    internal class DamageEventArgs : EventArgs
    {
        /// <summary>
        /// ダメージ量
        /// </summary>
        public int point;
        /// <summary>
        /// ダメージを受けたUnitのtransform
        /// </summary>
        public Transform transform;
        /// <summary>
        /// 誰からダメージを受けたか nullの場合不明な敵
        /// </summary>
        public UnitController damageFrom;

        /// <summary>
        /// どのようなダメージを受けたか
        /// </summary>
        public AttackType attackType;

    }
    #endregion

    #region UnitParameterのStorategy用ラッパー
    /// <summary>
    /// UnitParameterから作られる各種パラメーターの現在値
    /// </summary>
    [Serializable]
    public class CurrentUnitParameter
    {
        public UnitData Data;
        /// <summary>
        /// 現在のHP
        /// </summary>
        public int HealthPoint;

        /// <summary>
        /// 最大HP
        /// </summary>
        public int TotalHealthPoint
        {
            get => Data.HealthPoint + Data.AdditionalHealthPoint;
        }

        public float Speed;

        /// <summary>
        /// HPが残り少ない状態であるか
        /// </summary>
        public bool IsLowHPMode
        {
            get
            {
                return ((float)HealthPoint / (float)TotalHealthPoint) < Data.Data.LowHPValue;
            }
        }

        /// <summary>
        /// 敵ユニットから見た脅威度行動で自動で加算減算される 0~1
        /// </summary>
        /// メアリーの情報妨害によって味方のmenaceを操作することが可能。その場合敵NPCが勘違いして逃げたり集まったりする
        /// 通常開始時の場合はUnitのweightと同等
        public float menace;

        public CurrentUnitParameter(UnitData unitParameter)
        {
            Data = unitParameter;
            HealthPoint = unitParameter.HealthPoint + unitParameter.AdditionalHealthPoint;
            Speed = unitParameter.Speed + unitParameter.AdditionalSpeed;
        }

        public override string ToString()
        {
            return $"CurrentUnitParameter: {Data.Name}, {Data.MyItems.Count} Items, Health {HealthPoint}/{Data.HealthPoint}";
        }

    }
    #endregion

    [Serializable] public class RaycastPart
    {
        public TargetPartType partType;
        public List<GameObject> partObjects;
    }

    /// <summary>
    /// Unitの当たり判定となる場所のType
    /// </summary>
    [Serializable]
    public enum TargetPartType
    {
        Head,
        Arm,
        Body,
        Leg
    }

    /// <summary>
    /// Unitの行動状況
    /// </summary>
    public enum WorkState
    {
        /// <summary>
        /// 待機中
        /// </summary>
        Wait,
        /// <summary>
        /// 歩行中
        /// </summary>
        Walk,
        /// <summary>
        /// Forcusモードを使用している
        /// </summary>
        Focus,
        /// <summary>
        /// 攻撃やダメージなどのアニメーション中
        /// </summary>
        Animating,
        /// <summary>
        /// Mortarなどのギミックを使用中
        /// </summary>
        Gimmick
    }
}

