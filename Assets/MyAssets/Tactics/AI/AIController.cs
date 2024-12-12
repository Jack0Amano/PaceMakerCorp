using AIGraph.InOut;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tactics.Character;
using Tactics.Map;
using UnityEngine;
using static AIGraph.InOut.AIAction;
using AIGraph.Editor;
using Unity.Logging;
using static Utility;
using System.Net;
using Unity.VisualScripting;

namespace Tactics.AI
{

    /// <summary>
    /// Map.Navigationで決定された行動を実際に動かすためのClass
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [SerializeField] internal AICore aiCore;
        [SerializeField] UI.Overlay.FindOutUI.HeadUP headUpIcon;

        // Base properties
        internal UnitsController UnitsController;
        internal TilesController TilesController;

        // Sate
        internal bool IsRunning { private set; get; } = false;

        /// <summary>
        /// AIのControlが終了したことを伝えるためのAction
        /// </summary>
        public Action<FocusModeType> EndAIControllingAction;

        private DebugController DebugController;

        private GeneralParameter parameters;

        private UnitController UnitController;
        /// <summary>
        /// 発見ゲージのパネル
        /// </summary>
        internal UI.Overlay.FindOutUI.FindOutPanel FindOutPanel;
        /// <summary>
        /// 現在FindOutRoutineが動いているか
        /// </summary>
        public bool IsFindOutRoutineActive { private set; get; } = false;
        /// <summary>
        /// FindOutRoutineを一時停止
        /// </summary>
        internal bool PauseFindOutRoutine = false;
        /// <summary>
        /// 敵の発見度合いに関するList
        /// </summary>
        internal List<FindOutLevel> FindOutLevels = new List<FindOutLevel>();
        /// <summary>
        ///  Unitが発見しているenemies
        /// </summary>
        [SerializeField] internal List<FindOutLevel> FindedEnemies
        {
            get
            {
                return FindOutLevels.FindAll(el =>
                {
                    return (el.FindOutType == FindOutType.Exculamation ||
                            el.FindOutType == FindOutType.AlreadyFinded) ||
                            el.FindOutType == FindOutType.PlayerMightFindEnemy;
                });
            }
        }

        /// <summary>
        /// 移動ルーチンのPassPoints
        /// </summary>
        public List<(Transform point, TileCell tile)> WayPassPoints;

        internal AIGraph.AIGraphView MainAIGraphView;
        internal AIGraph.AIGraphView AfterActionAIGraphView;
        internal AIGraph.AIGraphView WhileMovingAIGraphView;

        // Start is called before the first frame update
        void Start()
        {
            // Set base properties
            DebugController = GameManager.Instance.debugController;
            parameters = GameManager.Instance.GeneralParameter;
            UnitController = GetComponent<UnitController>();
        }

        private void OnDestroy()
        {
            MainAIGraphView?.DestroyDebugGraphWindow();
            AfterActionAIGraphView?.DestroyDebugGraphWindow();
            WhileMovingAIGraphView?.DestroyDebugGraphWindow ();
        }

        #region FindEnemy functions for NPC
        /// <summary>
        /// watcherからtargetへの発見レベルを取得する
        /// </summary>
        /// <param name="watcher">観測するUnit</param>
        /// <param name="target">観測されるUnit</param>
        /// <param name="distance">距離</param>
        /// <returns></returns>
        float GetDeltaLevelToFindOut(UnitController watcher, UnitController target, float distance)
        {
            if (target.ThisObject == null)
                return 0;
            var isTargetFrontOfWatcher = watcher.IsTargetFrontOfUnit(target);

            // 発見状態から何秒で見失うか
            const float ForgetTime = 5;
            var forgetTick = ForgetTime / parameters.DetectUnitTick;
            var forgetValueEachTick = -1 / forgetTick;

            if (distance < parameters.ForceFindEnemyDistance && distance != 0)
            {
                // ForceFindEnemyDistanceの範囲では角度に関係なく気配で発見する
                return 1;
            }
            else if (isTargetFrontOfWatcher)
            {
                if (distance != 0)
                {
                    //　視界内に存在している
                    return parameters.DetectionDistanceCurve.Evaluate(distance);
                }
                else
                {
                    // 前方にはいるが物陰に隠れている
                    return forgetValueEachTick;
                }
            }
            else
            {
                // 視界内の前方におらず、またForceFindEnemyDistanceにも入っていない
                return forgetValueEachTick;
            }
        }

        /// <summary>
        /// FindOutのルーチン　Playerのみが回しEnemyにそのたびに挿入
        /// </summary>
        public void FindOutRoutine()
        {
            IsFindOutRoutineActive = true;

            Print("StartFindoutRuntine", UnitController);

            // 対象となるActiveUnitの敵一覧
            var enemies = UnitsController.GetEnemiesFrom(UnitController);
            enemies = enemies.FindAll(e => e.CurrentParameter.HealthPoint > 0);
            if (enemies.Count == 0)
                return;
            var tmp = new List<FindOutLevel>();
            // Meに対してenemyなunitがmeに対してのFindoutlevel
            var enemiesFindoutMe = new List<FindOutLevel>();
            enemies.ForEach(e =>
            {
                var existFindOutLevel = FindOutLevels.Find(f => f.Enemy == e);
                if (existFindOutLevel != null)
                    tmp.Add(existFindOutLevel);
                else
                    tmp.Add(new FindOutLevel(UnitController, e));
                var enemyFindoutMe = e.aiController.FindOutLevels.Find(f => f.Enemy == UnitController);
                if (enemyFindoutMe == null)
                {
                    enemyFindoutMe = new FindOutLevel(e, UnitController);
                    e.aiController.FindOutLevels.Add(enemyFindoutMe);
                }
                enemiesFindoutMe.Add(enemyFindoutMe);
            });
            FindOutLevels = tmp;
            // 上の処理でenemiesFindoutMeと FindOutLevelsの並びは同じ
            // enemiesFindoutMe = List<FindOutLevel>{(enemy1, me),    (enemy2, me)}
            // FIndOutLevel =     List<FindOutLevel>{(me    ,enemy1), (me    , enemy2){
            
            // 敵の探索を行うインターバル
            var interval = parameters.DetectUnitTick / enemies.Count;

            IEnumerator _FindOutRoutine()
            {
                while (IsFindOutRoutineActive)
                {
                    while(PauseFindOutRoutine)
                        yield return null;

                    while (FindOutLevels.Count == 0)
                        yield return null;

                    for(var i = 0; i< FindOutLevels.Count; i++)
                    {
                        // 自身が敵を見ているFindoutlevel
                        var myFindoutLevel = FindOutLevels[i];
                        // 敵が自身を見ているFindOutlevel
                        var enemyFindoutLevel = enemiesFindoutMe[i];

                        if (myFindoutLevel.Enemy.gameObject.IsDestroyed() || myFindoutLevel.Enemy.IsDead)
                        {
                            FindOutLevels.RemoveAt(i);
                            enemiesFindoutMe.RemoveAt(i);
                            continue;
                        }

                        if (!UnitController.EnemyAndDistanceDict.TryGetValue(myFindoutLevel.Enemy, out var distance))
                        {
                            yield return new WaitForSeconds(interval);
                            continue;
                        }
                        var valueEnemyFindOutMe = GetDeltaLevelToFindOut(myFindoutLevel.Enemy, UnitController, distance);
                        var valueThisUnitFindOutEnemy = GetDeltaLevelToFindOut(UnitController, myFindoutLevel.Enemy, distance);
                        var thisUnitFindoutType = myFindoutLevel.AddLevel(valueThisUnitFindOutEnemy, UnitsController.AttributeTurnCount );
                        var enemyFindoutType = enemyFindoutLevel.AddLevel(valueEnemyFindOutMe, UnitsController.AttributeTurnCount );

                        if (enemyFindoutType == FindOutType.Exculamation)
                        {
                            if (myFindoutLevel.Enemy.aiController.headUpIcon.ShowExculamation())
                                Log.Info($"{myFindoutLevel.Enemy} finds {UnitController}");
                        }
                        else if (enemyFindoutType == FindOutType.Question)
                            myFindoutLevel.Enemy.aiController.headUpIcon.ShowQuestion(myFindoutLevel.Level);
                        else
                            myFindoutLevel.Enemy.aiController.headUpIcon.Hide();

                        yield return new WaitForSeconds(interval);
                    }

                    FindOutPanel.UpdateTargets(UnitController, FindOutLevels);
                    FindOutPanel.UpdateLevels(FindOutLevels);
                }
                Log.Info($"End findout routine of {UnitController}");
            }

            IEnumerator _FindOutPanelRoutine()
            {
                while (IsFindOutRoutineActive)
                {
                    FindOutPanel.UpdateTargets(UnitController, FindOutLevels);
                    FindOutPanel.UpdateLevels(FindOutLevels);

                    yield return new WaitForSeconds(parameters.DetectUnitTick) ;
                }
                Log.Info($"End findout routine of {UnitController}");
            }

            if (UnitController.Attribute == UnitAttribute.ENEMY)
            {
                StartCoroutine(_FindOutPanelRoutine());
            }
            else if (UnitController.Attribute == UnitAttribute.PLAYER)
            {
                StartCoroutine(_FindOutRoutine());
            }
        }

        /// <summary>
        /// FindOutRoutineを停止する
        /// </summary>
        public void EndFindOutRoutine()
        {
            IsFindOutRoutineActive = false;
        }

        /// <summary>
        /// Turnを終了し 探索ルーチンでそのターン宙に発見した敵を取得
        /// </summary>
        /// <returns>ルーチン中に更新し発見状態になったNPCのFindOutLevel</returns>
        internal List<FindOutLevel> EndUnitTurn()
        {
            var output = new List<FindOutLevel>();
            // FindOutLevelをターン終了状態に移行する
            FindOutLevels.ForEach(tmp =>
            {
                Print(tmp.Enemy, tmp.FindOutType) ;
                var alert = tmp.TurnChanged(UnitsController.AttributeTurnCount);
                if (alert)
                {
                    output.Add(tmp);
                }
            });

            return output;
        }

        /// <summary>
        /// Unitに<param>enemy</param>を強制的に発見させる
        /// </summary>
        internal void SetForceFindOut(UnitController enemy, bool animation = false)
        {
            var findoutLevel = FindOutLevels.Find(f => f.Enemy == enemy);
            var type = findoutLevel.AddLevel(1, UnitsController.AttributeTurnCount);
            if (animation && type == FindOutType.Exculamation)
                headUpIcon.ShowExculamation();
        }

        /// <summary>
        /// Unitから見えないが方向は推測できる敵から攻撃を受けた際の処理
        /// </summary>
        internal void OnAttackedFromUnfindedEnemyKnowDirection(UnitController enemy)
        {
            // TODO 未知の敵から攻撃された場合の処理

        }

        /// <summary>
        /// Unitから見えない敵から攻撃を受けた際の処理
        /// </summary>
        internal void OnAttackedFromUnfindedEnemy(UnitController enemy)
        {
            // TODO 未知の敵から攻撃された場合の処理

        }

        #endregion

        #region AIAction

        /// <summary>
        /// AIActionを取得し動かす
        /// </summary>
        /// <returns></returns>
        public IEnumerator Run()
        {
            IsRunning = true;

            AIAction aIAction = null;
            if (MainAIGraphView != null &&
                WhileMovingAIGraphView != null &&
                AfterActionAIGraphView != null)
            {
                aIAction = MainAIGraphView.Execute(new EnvironmentData(UnitController, TilesController, UnitsController, WayPassPoints), true);
                yield return StartCoroutine(PlayAction(aIAction));
            }
            else
            {
                Log.Error($"AIDataContainer is missing: Main.{MainAIGraphView}, While.{WhileMovingAIGraphView}, After.{AfterActionAIGraphView}");
            }

            // AIControlの終了を告げる
            EndAIControllingAction?.Invoke(aIAction == null ? FocusModeType.None : aIAction.UseItemType);

            IsRunning = false;

            yield break;
        }

        /// <summary>
        /// 指定したActionを再生する
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private IEnumerator PlayAction(AIAction action)
        {
            // ===========================
            // スキップ
            // ===========================
            if (action.OrderOfAction == OrderOfAction.Skip)
            {
                yield return new WaitForSeconds(2);
            }

            // ============================
            // 移動した後に行動
            // ============================
            if (action.OrderOfAction == OrderOfAction.MoveToAction || action.OrderOfAction == OrderOfAction.MoveToSkip)
            {
                yield return action.unit.MoveTo(action.locationToMove);
                yield return new WaitForSeconds(1);
                if (action.OrderOfAction == OrderOfAction.MoveToAction)
                {
                    var hit = GameManager.Instance.RandomController.Probability(action.Rate);
                    yield return action.unit.AIRifleAttack(action.Target, hit);
                    yield return new WaitForSeconds(1);
                }
            }

            // =============================
            // 行動してそのあと移動とか
            // =============================
            if (action.OrderOfAction == OrderOfAction.ActionTo_)
            {
                var hit = GameManager.Instance.RandomController.Probability(action.Rate);
                var deadUnits = new List<UnitController>();
                if (hit && action.Target.CurrentParameter.HealthPoint <= action.Damage)
                    deadUnits.Add(action.Target);
                Print("==> After ai action");
                //aiCore.ClearAllDebugPoint();

                AIAction afterActionAIResult = null;
                IEnumerator AfterActionAI()
                {
                    var environment = new EnvironmentData(UnitController, TilesController, UnitsController, WayPassPoints);
                    afterActionAIResult = AfterActionAIGraphView.Execute(environment);
                    yield break;
                }

                StartCoroutine(AfterActionAI());

                //aiCore.PrintDebugText();

                yield return action.unit.AIRifleAttack(action.Target, hit);
                yield return new WaitForSeconds(1);

                while (afterActionAIResult == null)
                    yield return null;

                if (afterActionAIResult.OrderOfAction != OrderOfAction.Skip)
                {
                    yield return StartCoroutine(action.unit.MoveTo(afterActionAIResult.locationToMove));
                }
            }

            AIAction whileMovingActionResult = null;
            IEnumerator WhileMovingActionAI()
            {
                var environment = new EnvironmentData(UnitController, TilesController, UnitsController, WayPassPoints);
                whileMovingActionResult = WhileMovingAIGraphView.Execute(environment);
                yield break;
            }

            bool IsNewEnemyActionCompleted = false;
            // MoveAndFind中に敵を新たに発見したときの行動
            IEnumerator FindNewEnemyWhenMoving()
            {
                Print("==> Find enemy action ai");
                StartCoroutine(WhileMovingActionAI());

                // TODO AI移動するUnitが敵を新たに発見したときのアニメーション
                UnitController.CancelAIWalking();
                yield return new WaitForSeconds(1);

                while (whileMovingActionResult == null)
                    yield return null;

                Print(whileMovingActionResult);
                if (whileMovingActionResult.OrderOfAction == OrderOfAction.Skip)
                {
                    IsNewEnemyActionCompleted = false;
                    yield break;
                }
                yield return whileMovingActionResult.unit.MoveTo(whileMovingActionResult.locationToMove);
                
                if (whileMovingActionResult.OrderOfAction == OrderOfAction.ActionToSkip)
                {
                    var hit = GameManager.Instance.RandomController.Probability(whileMovingActionResult.Rate);
                    yield return whileMovingActionResult.unit.AIRifleAttack(whileMovingActionResult.Target, hit);
                    yield return new WaitForSeconds(1);
                }
                IsNewEnemyActionCompleted = true;
                Print("FindNewEnemy is completed");
            }

            if (action.OrderOfAction == OrderOfAction.MoveAndFind)
            {
                // Move中に発見した敵が攻撃可能であれば攻撃する
                // 遭遇戦
                
                var moveCoroutine = action.unit.MoveTo(action.locationToMove);
                
                StartCoroutine(moveCoroutine);

                // 移動中の敵発見を監視
                while (moveCoroutine.IsNotCompleted(true))
                {
                    // Attribute == EnemyのFindOutLevelはそのUnitのAIController.に保存されている
                    yield return new WaitForSeconds(0.3f);
                    if (FindOutLevels.Exists(d => d.FindOutType == FindOutType.Exculamation))
                    {
                        yield return StartCoroutine(FindNewEnemyWhenMoving());
                        if (IsNewEnemyActionCompleted)
                            yield break;
                    }
                    
                }

                var rotationCoroutine = action.unit.RotateTo(action.rotation);
                StartCoroutine(rotationCoroutine);
                // 移動先で回転している状態での敵発見を監視
                while (rotationCoroutine.IsNotCompleted(true))
                {
                    yield return new WaitForSeconds(0.3f);
                    if (FindOutLevels.Exists(d => d.FindOutType == FindOutType.Exculamation))
                    {
                        yield return StartCoroutine(FindNewEnemyWhenMoving());
                        if (IsNewEnemyActionCompleted)
                            yield break;
                    }
                }

                // 敵発見が移動後しばらくして発生する場合それを待つ
                var time = Time.time;
                while(Time.time - time < 1)
                {
                    yield return new WaitForSeconds(0.2f);
                    if (FindOutLevels.Exists(d => d.FindOutType == FindOutType.Exculamation))
                    {
                        yield return StartCoroutine(FindNewEnemyWhenMoving());
                        if(IsNewEnemyActionCompleted)
                            yield break;
                    }
                }

            }
        }
        
        #endregion
    }

    /// <summary>
    /// Unitが警戒もしくは発見したEnemy
    /// </summary>
    [Serializable]
    class FindOutLevel
    {
        /// <summary>
        /// Unitが発見もしくは警戒中の敵
        /// </summary>
        internal UnitController Enemy;
        /// <summary>
        /// 現在のEnemyの発見状態のレベル
        /// </summary>
        internal float Level = 0;
        /// <summary>
        /// 見つかったターン非発見なら-1
        /// </summary>
        internal int AttributeTurn = -1;
        /// <summary>
        /// 発見状況
        /// </summary>
        internal FindOutType FindOutType { private set; get; } = FindOutType.None;
        /// <summary>
        /// 発見した敵までの距離 見えていない状態でdist=0
        /// </summary>
        internal float distance;
        /// <summary>
        /// Turnが終わった際にdistance!=0で強制的に見つかるFindOutLevel
        /// </summary>
        const float ForceFindLevel = 0.6f;
        /// <summary>
        /// FindOutLevelを持っているUnir (通常NPCのみ)
        /// </summary>
        internal UnitController thisUnit;

        internal FindOutLevel(UnitController thisUnit, UnitController enemy)
        {
            this.Enemy = enemy;
            this.thisUnit = thisUnit;
        }

        /// <summary>
        /// FindOutLevelにDeltaLevelを追加する
        /// </summary>
        /// <param name="deltaLevel"></param>
        /// <param name="attributeTurn"></param>
        /// <returns>ターンで新規に敵を発見した際にtrue</returns>
        internal FindOutType AddLevel(float deltaLevel, int attributeTurn)
        {
            if (FindOutType == FindOutType.Exculamation)
            {
                return FindOutType;
            }
            else if (FindOutType == FindOutType.AlreadyFinded)
            {
                return FindOutType;
            }

            Level += deltaLevel;

            if (Level >= 1)
            {
                Level = 1;
                AttributeTurn = attributeTurn;
                FindOutType = FindOutType.Exculamation;

            }
            else if (Level > 0)
            {
                FindOutType = FindOutType.Question;
            }
            else
            {
                Level = 0;
                AttributeTurn = -1;
                FindOutType = FindOutType.None;
            }
            return FindOutType;
        }

        /// <summary>
        /// 発見状態を解除して初期状態にに戻す
        /// </summary>
        internal void ClearDetection()
        {
            AttributeTurn = -1;
            Level = 0;
        }

        /// <summary>
        /// AttributeTurnが進んだときに呼び出し 
        /// </summary>
        /// <returns>AttributeTurnが変わった際に発見された場合True</returns>
        internal bool TurnChanged(int lastAttributeTurn)
        {
            var output = false;
            if (FindOutType == FindOutType.Exculamation || 
                (distance != 0 && Level >= ForceFindLevel))
            {
                FindOutType = FindOutType.AlreadyFinded;
                AttributeTurn = lastAttributeTurn;
                output = true;
            }
                
            else if (FindOutType == FindOutType.Question || 
                     FindOutType == FindOutType.None)
            {
                FindOutType = FindOutType.None;
                AttributeTurn = -1;
            }

            Level = 0;
            distance = 0;
            return output;
        }

        internal FindOutLevel Clone()
        {
            return new FindOutLevel(thisUnit, Enemy)
            {
                Level = this.Level,
                AttributeTurn = this.AttributeTurn,
                FindOutType = this.FindOutType
            };
        }

        public override string ToString()
        {
            return $"My:{thisUnit}, Enemy:{Enemy}, Type:{FindOutType}, Dist:{distance}";
        }
    }

    /// <summary>
    /// 実際にAIが行動するAction
    /// </summary>
    public class AIAction_x
    {
        public enum OrderOfAction_x
        {
            MoveToAction,
            ActionToMove,
            MoveToSkip,
            ActionToSkip,
            Skip,
            /// <summary>
            /// Actionした後にそのAction後の状況を見てもう一度判断
            /// </summary>
            ActionTo_,
            /// <summary>
            /// Move中に発見した敵に攻撃する
            /// </summary>
            MoveAndFind,
            None
        }

        /// <summary>
        /// Default Init スコアは-1
        /// </summary>
        public AIAction_x()
        {
            orderOfAction = OrderOfAction_x.Skip;
            locationToMove = Vector3.zero;
            score = -1;
        }

        /// <summary>
        /// Default Init
        /// </summary>
        /// <param name="unit"></param>
        public AIAction_x(UnitController unit)
        {
            this.unit = unit;
        }

        /// <summary>
        /// 行動の順番
        /// </summary>
        public OrderOfAction_x orderOfAction = OrderOfAction_x.None;

        /// <summary>
        /// 行動をするUnit
        /// </summary>
        public UnitController unit;

        /// <summary>
        /// 攻撃対象
        /// </summary>
        public UnitController target;

        /// <summary>
        /// 移動する場所
        /// </summary>
        public Vector3 locationToMove;

        /// <summary>
        /// (任意）移動後に向く方向
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// 攻撃対象の査定値 0~1
        /// </summary>
        public float score = 0;

        /// <summary>
        /// 攻撃が当たる確率 0~1
        /// </summary>
        public float rate = 0;

        /// <summary>
        /// <c>unit</c>が<c>target</c>を攻撃した際のダメージ量
        /// </summary>
        public int Damage
        {
            get => unit.GetAIAttackDamage(target);
        }

        public override string ToString()
        {
            return $"Unit {unit}, Order {orderOfAction}, Move {locationToMove}, Target {target}, Rate {rate}%";
        }

        public AIAction_x clone()
        {
            return new AIAction_x()
            {
                orderOfAction = orderOfAction,
                unit = unit,
                target = target,
                locationToMove = locationToMove,
                score = score,
                rate = rate
            };
        }
    }

    /// <summary>
    /// AIのBaseClass
    /// </summary>
    public abstract class AICore : MonoBehaviour
    {
        internal TilesController tilesController;
        internal UnitsController unitsController;
        internal GeneralParameter parameters;
        internal AIController aiController;

        /// <summary>
        /// AIControllerの制御するunit
        /// </summary>
        internal UnitController unit;

        protected List<Vector3> DebugPositions = new List<Vector3>();

        /// <summary>
        /// AIのAction結果
        /// </summary>
        public AIAction_x Result { internal set; get; }

        ///// <summary>
        ///// AIがForcastModeで計算される Forcastの場合他EnemyのForcastを参照しない
        ///// </summary>
        //protected bool ForcastMode = false;

        /// <summary>
        /// デバッグ用のメッセージ
        /// </summary>
        public List<AIDebugMsg> debugTexts = new List<AIDebugMsg>();

        /// <summary>
        /// 移動ルーチンのPassPoints
        /// </summary>
        public List<(Transform point, TileCell tile)> WayPassPoints;

        /// <summary>
        /// AIActionを<c>Result</c>に取得する
        /// </summary>
        /// <param name="forcast"></param>
        /// <returns></returns>
        public virtual IEnumerator GetAIAction()
        {
            // ForcastMode = isForcast;
            yield break;
        }

        /// <summary>
        /// 行動後に動ける場合のActionを取得する
        /// </summary>
        /// <param name="removedUnits"></param>
        /// <returns></returns>
        public abstract IEnumerator AfterActionAIAction(List<UnitController> removedUnits);

        // 遭遇戦なので少々ガバガバに作っても問題ない
        /// <summary>
        /// Unitが現在のTileでかつ現在発見している敵の中で最適な行動を取るようなAI
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerator FindEnemyAction();

        private void OnDrawGizmos()
        {
            DebugPositions.ForEach(p => Gizmos.DrawCube(p, new Vector3(0.1f, 0.1f, 0.1f)));
        }

        internal void PrintDebugText()
        {
            print($"AI routine of {unit}\n" + string.Join("\n", debugTexts) + "\n===================");
            debugTexts.Clear();
        }

        /// <summary>
        /// DebugScoreを0にする
        /// </summary>
        public void ClearAllDebugPoint()
        {
            tilesController.Tiles.ForEach(t =>
            {
                t.pointsInTile.ForEach(p =>
                {
                    p.DebugScore = 0;
                });
            });
        }

        /// <summary>
        /// 合計を<c>a</c>を<c>rateA</c>倍の値に、<c>b</c>を<c>rateB</c>倍にしその合計値を返す
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="rateA"></param>
        /// <param name="rateB"></param>
        /// <returns></returns>
        protected float CalcRate(float a, float b, float rateA, float rateB)
        {
            return a * rateA + b * rateB;
        }

        /// <summary>
        /// <c>CalcSaftyScoreOnLoc</c>で使われる評価用のTempClass
        /// </summary>
        [SerializeField]
        public class Situation
        {
            /// <summary>
            /// AIを動かすUnit
            /// </summary>
            internal UnitController active;
            /// <summary>
            /// <c>locationAndScore</c>に関係してくるenemyとその情報
            /// </summary>
            internal List<Enemy> enemies;
            /// <summary>
            /// Activeが移動するか検討中のlocation
            /// </summary>
            internal PointInTile pointInTile;
            /// <summary>
            /// <c>locationAndScore</c>の位置しているtileCell
            /// </summary>
            internal TileCell tileCell;
            /// <summary>
            /// このTileに入る際に反撃してくる敵の数
            /// </summary>
            internal int EnemiesCounterattackCountInTile
            {
                get => enemies.FindAll(e => e.unit.tileCell == tileCell).Count;
            }
            /// <summary>
            /// UnitがTileに立ち入る場合counterattackによって受けるダメージの総量
            /// </summary>
            internal float forcastDamageToCounterattack
            {
                get => enemies.Sum(e => e.unit.tileCell.Equals(tileCell) ? e.DamageFromThis : 0);
            }

            internal Situation(UnitController active, TileCell tile, PointInTile pointInTile)
            {
                this.active = active;
                this.tileCell = tile;
                this.pointInTile = pointInTile;
                this.enemies = new List<Enemy>();
            }

            public override string ToString()
            {
                return $"{tileCell} {pointInTile.location}: {enemies.Count} enemies";
            }

            /// <summary>
            /// <c>locationAndScore</c>から狙えるEnemy
            /// </summary>
            public class Enemy
            {
                /// <summary>
                /// この情報がForcastの物か
                /// </summary>
                internal bool isForcastInfo;
                /// <summary>
                /// EnemyのUnitController
                /// </summary>
                internal UnitController unit;
                /// <summary>
                /// ActiveとEnemyまでの射線距離
                /// </summary>
                internal float distance;
                /// <summary>
                /// その地点から射線が通っているかどうか
                /// </summary>
                internal bool hit => distance > 0;
                /// <summary>
                /// このEnemyをActiveUnitが倒しきれるか
                /// </summary>
                internal bool canKillThis = false;
                /// <summary>
                /// <c>active</c>がこのEnemyからの攻撃に耐えれるか
                /// </summary>
                internal bool canDefenceFromThis = false;
                /// <summary>
                /// <c>active</c>の予想移動位置がCoverObjectによってこのEnemyから守られているか
                /// </summary>
                internal bool isCoveredFromThis = false;
                /// <summary>
                /// このEnemyへのこのポイントでの命中確率 0~1
                /// </summary>
                /// <returns></returns>
                internal float HitRateToThis = 0;
                /// <summary>
                /// このEnemyに攻撃した際のDamage量
                /// </summary>
                /// <returns></returns>
                internal float DamageToThis;
                /// <summary>
                /// Enemyに攻撃して成功した場合どの程度HPを削れるか
                /// </summary>
                /// <returns></returns>
                internal float DamageRateToThis;
                /// <summary>
                /// このEnemyに攻撃された場合どの程度の割合のダメージを受けるか
                /// </summary>
                /// <returns></returns>
                internal float DamageRateFromThis;
                /// <summary>
                /// このEnemyからActiveUnitが攻撃された際のダメージ量
                /// </summary>
                /// <returns></returns>
                internal float DamageFromThis;

                /// <summary>
                /// <c>ActiveLocation</c>の移動位置
                /// </summary>
                private readonly PointInTile ActiveLocation;
                /// <summary>
                /// AIを動かしているAcitveなUnit このclassのUnitの敵になる
                /// </summary>
                private readonly UnitController ActiveUnit;

                /// <summary>
                /// Enemyの位置
                /// </summary>
                internal Vector3 Position
                {
                    get
                    {
                        return unit.gameObject.transform.position;
                        //if (isForcastInfo && unit.aiController.actionForcast != null)
                        //{
                        //    return unit.aiController.actionForcast.locationToMove;
                        //}
                        //else
                        //{
                        //    return unit.gameObject.transform.position;
                        //}
                    }
                }

                /// <summary>
                /// SituationのEnemyに関するclass
                /// </summary>
                /// <param name="thisUnit">このEnemy</param>
                /// <param name="active">AIを動かしているAcitveなUnit</param>
                /// <param name="activeLocation"><c>active</c>の予想移動位置</param>
                internal Enemy(UnitController thisUnit, UnitController active, PointInTile activeLocation)
                {
                    ActiveUnit = active;
                    ActiveLocation = activeLocation;

                    unit = thisUnit;
                    // このEnemyにActiveUnitが攻撃した際のDamage量
                    DamageToThis = active.GetAIAttackDamage(unit);
                    // このEnemyにActiveUnitが攻撃して成功した場合どの程度HPを削れるか
                    if (unit.CurrentParameter.HealthPoint <= DamageToThis)
                        DamageRateToThis = 1;
                    else
                        DamageRateToThis = DamageToThis / unit.CurrentParameter.HealthPoint;
                    // このEnemyからActiveunitが攻撃された際のダメージ量
                    DamageFromThis = unit.GetAIAttackDamage(active);
                    // このEnemyからActiveUnitが攻撃された際どの程度HPを削れるか
                    if (active.CurrentParameter.HealthPoint <= DamageFromThis)
                        DamageRateFromThis = 1;
                    else
                        DamageRateFromThis = DamageFromThis / active.CurrentParameter.HealthPoint;
                }

                /// <summary>
                /// <c>HitRateToThis</c>を計算する
                /// </summary>
                internal void CalcHitRate()
                {
                    // ! enemyがforcast地点である場合AttackRateToThisが機能しない
                    // このEnemyへのActiveUnitの移動予想ポイントでの命中確率
                    if (isForcastInfo)
                        HitRateToThis = ActiveUnit.GetAIForcastAttackRate(unit, Position, ActiveLocation.location);
                    else
                        HitRateToThis = ActiveUnit.GetAIAttackRate(unit, ActiveLocation.location);
                }

                public string ShortInfo()
                {
                    return $"{unit.CurrentParameter.Data.Name},d{(int)distance}";
                }

                public override string ToString()
                {
                    return $"class Enemy {unit}";
                }
            }
        }
    }

    public class AIDebugMsg
    {
        public AIDebugMsg(string f, string m)
        {
            function = f;
            message = m;
        }

        public string function;
        public string message;

        public override string ToString()
        {
            return $"{function}: {message}";
        }
    }

    [Serializable]
    public enum FindOutType
    {
        Question,
        /// <summary>
        /// NPCが発見して!マークが出ている状況
        /// </summary>
        Exculamation,
        /// <summary>
        /// NPCが既に発見している状況
        /// </summary>
        AlreadyFinded,
        /// <summary>
        /// Playerがおそらく発見している状況である
        /// </summary>
        PlayerMightFindEnemy,
        None
    }
}