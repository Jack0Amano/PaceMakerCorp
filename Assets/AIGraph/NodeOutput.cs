using AIGraph.Editor;
using AIGraph.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Linq;
using Tactics.Character;
using Tactics.Map;
using AIGraph.Nodes.Parts;

namespace AIGraph.InOut
{
    #region Output
    /// <summary>
    /// イベントの基底クラス
    /// </summary>
    public class NodeOutput
    {
        /// <summary>
        /// 次にExecuteするProcessNodeにつながるNode
        /// </summary>
        public Port NextPort;

        /// <summary>
        /// NodeのID
        /// </summary>
        public string NodeID;
        /// <summary>
        /// Event全体のID
        /// </summary>
        public string AIID;
        /// <summary>
        /// Nodeの名前
        /// </summary>
        public string NodeName;
        /// <summary>
        /// Nodeが終了したか
        /// </summary>
        public bool IsNodeCompleted
        {
            get => NextPort != null;
        }

        public NodeOutput()
        {
        }

        public NodeOutput(AIGraph.Nodes.SampleNode node, string id, Port outputPort)
        {
            NodeID = node.Guid;
            NodeName = node.title;
            NodeID = id;
            NextPort = outputPort;
        }

        public override string ToString()
        {
            return $"EventOutput: {NodeName} {NodeID}";
        }
    }

    #endregion

    #region Input
    /// <summary>
    /// ゲームプログラム本体からEventGraphに返すclass
    /// </summary>
    public class NodeInput
    {
        /// <summary>
        /// 途中から始めるためのNodeのID
        /// </summary>
        public string StartAtID = "";

        /// <summary>
        /// 選択肢式のトリガーを保つ場合のその選択したIndex
        /// </summary>
        public int SelectTriggerIndex;

        /// <summary>
        /// 現在時刻
        /// </summary>
        public DateTime DateTime;
        /// <summary>
        /// どのスポーンIDを持つ的にエンカウントしたのか OnBattleTriggerNode等に使う
        /// </summary>
        public string encountSpawnID;
        /// <summary>
        /// トリガーとなるSceneのタイミング
        /// </summary>
        public TriggerTiming triggerTiming;
        /// <summary>
        /// 結果によるトリガー
        /// </summary>
        public Tactics.VictoryConditions.GameResult gameResultTrigger;

        /// <summary>
        /// 所持しているアイテムのID
        /// </summary>
        public List<string> ItemsID;
    }

    /// <summary>
    /// トリガーとなるSceneのタイミング
    /// </summary>
    public enum TriggerTiming
    {
        None,
        BeforeBattle,
        AfterBattle,
        BeforePrepare,
        OnPrepare,
        AfterResultScene,
        PassTime,
        GameStart
    }
    #endregion

    #region Result
    /// <summary>
    /// 実際にAIが行動するAction
    /// </summary>
    public class AIAction
    {

        /// <summary>
        /// 行動の順番
        /// </summary>
        public OrderOfAction OrderOfAction = OrderOfAction.None;

        /// <summary>
        /// 行動をするUnit
        /// </summary>
        public UnitController unit;

        /// <summary>
        /// 攻撃対象
        /// </summary>
        public UnitController Target;

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
        public float Rate = 0;

        /// <summary>
        /// このActionでどのようなItemを使って行動したか
        /// </summary>
        public FocusModeType UseItemType = FocusModeType.None;

        /// <summary>
        /// <c>unit</c>が<c>target</c>を攻撃した際のダメージ量
        /// </summary>
        public int Damage
        {
            get => unit.GetAIAttackDamage(Target);
        }

        /// <summary>
        /// Default Init スコアは-1
        /// </summary>
        public AIAction()
        {
            OrderOfAction = OrderOfAction.Skip;
            locationToMove = Vector3.zero;
            score = -1;
        }

        /// <summary>
        /// Default Init
        /// </summary>
        /// <param name="unit"></param>
        public AIAction(UnitController unit)
        {
            this.unit = unit;
        }

        public override string ToString()
        {
            return $"Unit {unit}, Order {OrderOfAction}, Move {locationToMove}, Target {Target}, Rate {Rate}%";
        }

        public AIAction clone()
        {
            return new AIAction()
            {
                OrderOfAction = OrderOfAction,
                unit = unit,
                Target = Target,
                locationToMove = locationToMove,
                score = score,
                Rate = Rate
            };
        }
    }
    #endregion

    public enum OrderOfAction
    {
        MoveToAction,
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
    /// Graphを動かす際の環境入力データ
    /// </summary>
    public class EnvironmentData
    {
        /// <summary>
        /// 次にExecuteするProcessNodeにつながるNode
        /// </summary>
        internal CustomPort OutPort;

        /// <summary>
        /// AIが動かすUnitのController
        /// </summary>
        public UnitController MyUnitController;

        /// <summary>
        /// タイルのコントロール
        /// </summary>
        public TilesController TilesController;

        public UnitsController UnitsController;

        /// <summary>
        /// 移動ルーチンのPassPoints
        /// </summary>
        public List<(Transform point, TileCell tile)> WayPassPoints;
        /// <summary>
        /// Unitが現在いるTile
        /// </summary>
        public TileCell CurrentTile
        {
            get => MyUnitController.tileCell;
            
        }
        /// <summary>
        /// 付近のタイルの状況 tmpファイルでProcessNode間で伝えられる
        /// </summary>
        public List<Situation> NearTileAndSituations;

        /// <summary>
        /// AfterActionAIで使われるActionを受けたtargetのUnitController
        /// </summary>
        public UnitController TargetedUnitController;

        public EnvironmentData()
        {
        }

        public EnvironmentData(UnitController unitController, 
                               TilesController tilesController, 
                               UnitsController unitsController,
                               List<(Transform point, TileCell tile)> wayPassPoints)
        {
            MyUnitController = unitController;
            TilesController = tilesController;
            UnitsController = unitsController;
            WayPassPoints = wayPassPoints;
        }

        public override string ToString()
        {
            return $"EnvironmentData(Out:{OutPort.ConnectedNodes.Count}, Node: {OutPort.ConnectedNodes.FirstOrDefault()}";
        }
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
        internal TileCell Tile;
        /// <summary>
        /// このTileに入る際に反撃してくる敵の数
        /// </summary>
        internal int EnemiesCounterattackCountInTile
        {
            get => enemies.FindAll(e => e.unit.tileCell == Tile).Count;
        }
        /// <summary>
        /// UnitがTileに立ち入る場合counterattackによって受けるダメージの総量
        /// </summary>
        internal float ForcastDamageToCounterattack
        {
            get => enemies.Sum(e => e.unit.tileCell.Equals(Tile) ? e.DamageFromThis : 0);
        }

        internal Situation(UnitController active, TileCell tile, PointInTile pointInTile)
        {
            this.active = active;
            this.Tile = tile;
            this.pointInTile = pointInTile;
            this.enemies = new List<Enemy>();
        }

        public override string ToString()
        {
            return $"{Tile} {pointInTile.location}: {enemies.Count} enemies";
        }

        /// <summary>
        /// <c>locationAndScore</c>から狙えるEnemy
        /// </summary>
        public class Enemy
        {
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
            internal bool CanKillThis = false;
            /// <summary>
            /// <c>active</c>がこのEnemyからの攻撃に耐えれるか
            /// </summary>
            internal bool CanDefenceFromThis = false;
            /// <summary>
            /// <c>active</c>の予想移動位置がCoverObjectによってこのEnemyから守られているか
            /// </summary>
            internal bool IsCoveredFromThis = false;
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
            /// このEnemyから
            /// </summary>
            internal bool CounterattackFromThis;

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
                // このEnemyへのActiveUnitの移動予想ポイントでの命中確率
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