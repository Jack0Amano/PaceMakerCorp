using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using AIGraph.Nodes.Parts;
using AIGraph.Editor;
using AIGraph.InOut;
using UnityEditor;
using UnityEditor.UIElements;
using Tactics.Character;
using Tactics.Map;
using Tactics.AI;
using static Utility;

namespace AIGraph.Nodes
{

    public abstract class ProcessNode : SampleNode
    {
        public Parts.CustomPort InputPort;
        public Parts.CustomPort OutputPort;

        public AIGraph.InOut.AIAction AIAction;

        internal AIGraphView AIGraphView;

        protected Label DebugLabel;
        
        public ProcessNode()
        {
            InputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(EnvironmentData));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(EnvironmentData));
            OutputPort.portName = "Out";
            outputContainer.Add(OutputPort);

            DebugLabel = new Label();
        }

        /// <summary>
        /// Nodeを再生
        /// </summary>
        /// <returns></returns>
        public virtual EnvironmentData Execute(EnvironmentData input)
        {
            EnvironmentData = input;
            return input;
        }

        /// <summary>
        /// Nodeの実行結果を表示するためのfunc
        /// </summary>
        /// <param name="result"></param>
        public void DebugDraw(EnvironmentData result)
        {
            mainContainer.Add(DebugLabel);

            if (result.OutPort != null)
            {
                result.OutPort.portColor = AIGraphView.RunGraphColor;
                result.OutPort.IsHighlighted = true;
                if (result.OutPort.ConnectedPorts.Count > 0)
                {
                    result.OutPort.ConnectedPorts[0].portColor = AIGraphView.RunGraphColor;
                    result.OutPort.IsHighlighted = true;
                }
            }
        }

        /// <summary>
        /// 地点が<c>unit</c>にとってどのような状況になっているか判断する
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="specificPoint">評価する地点</param>
        /// <param name="tileCell">評価するTile pointInTileが指定されている場合はそのpointが含まれているtile</param>
        /// <returns>0_1でより評価の高い地点が安全</returns>
        internal List<Situation> GetSituations(TileCell tileCell, PointInTile specificPoint = null)
        {
            return tileCell.pointsInTile.ConvertAll(p => GetSituation(p));
        }

        /// <summary>
        /// 特定地点のSituationを取得
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        internal Situation GetSituation(PointInTile point)
        {
            if (AIGraphView.SituationsDictionary.TryGetValue(point, out var v))
                return v;

            // List<RatingInfo> => 特定位置で通る射線
            var unit = MyUnitController;

            float MAX_DISTANCE_LIMIT = 100;
            var pos = point.location;

            Situation.Enemy _GetEnemyInfo(UnitController enemy)
            {
                //if (removed != null && removed.Contains(enemy))
                //    return null;
                var enemyPos = enemy.gameObject.transform.position;

                var distance = Vector3.Distance(pos, enemyPos);
                if (distance > MAX_DISTANCE_LIMIT) return null;

                var rayDist = 0f;

                rayDist = unit.GetRayDistanceTo(enemy, pos);
                if (rayDist == 0) return null;

                var enemyInfo = new Situation.Enemy(enemy, unit, point)
                {
                    distance = rayDist,
                    CanKillThis = (enemy.CurrentParameter.HealthPoint - unit.GetAIAttackDamage(enemy)) <= 0,
                    CanDefenceFromThis = (unit.CurrentParameter.HealthPoint - enemy.GetAIAttackDamage(unit)) > 0,
                    IsCoveredFromThis = IsCoveredFromEnemy(point, enemyPos)
                };
                // 複数の敵から射線の通る位置かどうか
                enemyInfo.CalcHitRate();

                return enemyInfo;
            }

            // 敵の現在位置からsituationを計算
            var situation = new Situation(unit, point.TileCell, point);
            foreach (var detected in AIController.FindedEnemies)
            {
                var info = _GetEnemyInfo(detected.Enemy);
                if (info != null)
                    situation.enemies.Add(info);
            }
            return situation;
        }

        /// <summary>
        /// 体力面での敵ユニットの評価値
        /// </summary>
        /// <param name="target">標的となる敵Unit</param>
        /// <param name="orderOfAction">ActiveUnitのIndexを0にした行動順リスト</param>
        /// <returns>0~1で値が高いほど攻撃するに適したTarget</returns>
        /// 味方Unitの連撃を考慮
        public float CalcHealthScore(UnitController target)
        {
            var score = (MyUnitController.itemController.CalcDamage(target) / MyUnitController.CurrentParameter.TotalHealthPoint) * 0.7f;
            score += 0.3f * MyUnitController.CurrentParameter.menace;

            var rand = UnityEngine.Random.Range(0, 1.0001f);

            score += rand;

            return score;
        }
    }
}