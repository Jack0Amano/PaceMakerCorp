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
using Tactics.Map;
using static AIGraph.InOut.AIAction;
using static Utility;
using Tactics.Character;
using System.Collections;
using static AIGraph.InOut.Situation;

namespace AIGraph.Nodes
{
    /// <summary>
    /// なるべく動かずに現在のTIleで攻撃する
    /// </summary>
    public class AttackFromNearNode : ProcessNode
    {
        readonly CustomPort ActionPort;
        /// <summary>
        /// カバーに隠れているときの命中カーブ\nX軸 = 0(必ず命中) ~ 1(命中しない)
        /// </summary>
        readonly CurveField CoveredCurveField;
        /// <summary>
        /// なるべく適正距離でAimingするためのカーブ \nX軸 = 命中率  Y軸 = 評価スコア
        /// </summary>
        readonly CurveField NormalAimCurveField;
        /// <summary>
        /// なるべく遠くでエイミングするためのカーブ \nNormalより低い値を通る \nX軸 = 命中率  Y軸 = 評価スコア
        /// </summary>
        readonly CurveField HardAimCurveField;

        readonly CurveField MoveRiskCurveField;


        public AttackFromNearNode() : base()
        {
            title = "Attack from near";
            NodePath = "AI/AttackFromNear";

            OutputPort.RemoveFromHierarchy();

            ActionPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(AIAction));
            ActionPort.portName = "AI Action";
            outputContainer.Add(ActionPort);

            mainContainer.Add(new Label("Normal Aim"));
            NormalAimCurveField = new CurveField();
            mainContainer.Add(NormalAimCurveField);

            mainContainer.Add(new Label("Hard Aim"));
            HardAimCurveField = new CurveField();
            mainContainer.Add(HardAimCurveField);

            mainContainer.Add(new Label("Coverd"));
            CoveredCurveField = new CurveField();
            mainContainer.Add(CoveredCurveField);

            mainContainer.Add(new Label("Move risk"));
            MoveRiskCurveField = new CurveField();
            mainContainer.Add(MoveRiskCurveField);

        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            var output = base.Execute(input);

            var unit = MyUnitController;
            var current = CurrentTile;
            var nearTileAndSituations = new List<Situation>();

            // CurrentのSituationを取得
            (Situation situation, Enemy enemy, float point) currentTileSituation = (null, null, 0);
            void _CurrentTile()
            {
                var tmp = new List<(Situation situation, Enemy enemy, float point)>();
                foreach (var pos in current.pointsInTile)
                {
                    tmp.Add(GetPositionAction(pos, current));
                }
                currentTileSituation = tmp.FindMax(t => t.point);
                nearTileAndSituations.AddRange(tmp.ConvertAll(t => t.situation));
            }

            _CurrentTile();



            // 現在位置のSituation
            var currentLocAndScore = new PointInTile(unit);
            var currentPosSituation = GetPositionAction(currentLocAndScore, current);

            // DEBUG Debug show score
            var _currentPos = current.pointsInTile.FindMin(l => Vector3.Distance(l.location, unit.transform.position));
            _currentPos.DebugScore = currentPosSituation.point;


            // 現在位置からActionToMoveできる場合はそれを優先するように
            if (currentPosSituation.enemy != null)
            {
                var addPos = currentPosSituation.point + 0.2f;
                if (addPos > 1)
                    addPos = 1;
                currentPosSituation.point += CalcRate(addPos, currentPosSituation.point, 0.3f, 0.7f);
            }

            if (currentPosSituation.point > currentTileSituation.point)
            {
                // 現在位置から攻撃するのが最も良い場合 ActionToMove or ActionToSkip
                var action = new AIAction(unit);
                action.OrderOfAction = OrderOfAction.ActionTo_;
                action.Target = currentPosSituation.enemy.unit;
                action.Rate = currentPosSituation.enemy.HitRateToThis;
                AIAction = action;
                output.OutPort = ActionPort;

            }
            else 
            {

                // 現在のTile内で移動してから攻撃するのが最も良い場合 MoveToAction
                var action = new AIAction(unit);
                action.OrderOfAction = OrderOfAction.MoveToAction;
                action.locationToMove = currentTileSituation.situation.pointInTile.location;
                action.Target = currentTileSituation.enemy.unit;
                action.Rate = currentTileSituation.enemy.HitRateToThis;
                AIAction = action;
                output.OutPort = ActionPort;
            }

            if (AIAction.Target == null)
            {
                AIAction.OrderOfAction = OrderOfAction.Skip;
            }

            return output;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(nameof(NormalAimCurveField), NormalAimCurveField.value);
            data.raw.SetToPairs(nameof(CoveredCurveField), CoveredCurveField.value);
            data.raw.SetToPairs(nameof(HardAimCurveField), HardAimCurveField.value);
            data.raw.SetToPairs(nameof(MoveRiskCurveField), MoveRiskCurveField.value);
            return data;
        }

        public override void Load(NodeData data)
        {
            if (data.raw.GetFromPairs(nameof(NormalAimCurveField), out AnimationCurve normalCurve))
                NormalAimCurveField.value = normalCurve;
            if (data.raw.GetFromPairs(nameof(CoveredCurveField), out AnimationCurve coveredCurve))
                CoveredCurveField.value = coveredCurve;
            if (data.raw.GetFromPairs(nameof(HardAimCurveField), out AnimationCurve hardCurve))
                HardAimCurveField.value = hardCurve;
            if (data.raw.GetFromPairs(nameof(MoveRiskCurveField), out AnimationCurve moveCurve))
                MoveRiskCurveField.value = moveCurve;
            base.Load(data);
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            NormalAimCurveField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            CoveredCurveField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            HardAimCurveField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            MoveRiskCurveField.RegisterValueChangedCallback(ent => action?.Invoke(this));
            base.RegisterAnyValueChanged(action);
        }



        /// <summary>
        /// Positionが行動するのに良い地点か評価する
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="pos"></param>
        /// <param name="tile">指定<c>pos</c>の位置しているTile</param>
        /// <returns></returns>
        private (Situation situation, Enemy enemy, float point) GetPositionAction(PointInTile pos, TileCell tile)
        {
            var unit = MyUnitController;
            var situation = GetSituation(pos);

            var currentHits = situation.enemies.FindAll(s => s.hit);
            // pointは高いほど移動するに良い地点
            (Situation situation, Situation.Enemy enemy, float point) output = (new Situation(unit, tile, pos), null, 0);

            if (currentHits.Count == 1)
            {
                var point = SingleEnemy(situation);
                output.situation = situation;
                output.enemy = currentHits[0];
                output.point = point;
            }
            else if (currentHits.Count != 0)
            {
                var (target, point) = MultiEnemies(situation);
                output.situation = situation;
                output.enemy = target;
                output.point = point;
            }
            else
            {
            }

            // Enemyが移動先のTileにいる場合反撃を受けるためスコアを減らす
            var forcastDamageWhenEnter = situation.ForcastDamageToCounterattack;
            if (forcastDamageWhenEnter != 0)
            {
                var enemyScore = forcastDamageWhenEnter / situation.active.CurrentParameter.HealthPoint;
                if (enemyScore > 1) enemyScore = 1;
                enemyScore = 1 - enemyScore;
                if (enemyScore < output.point)
                {
                    output.point = CalcRate(output.point, enemyScore, 0.4f, 0.6f);
                }
            }

            // DEBUG Show debug score
            pos.DebugScore = output.point;

            return output;
        }

        /// <summary>
        /// 単体の敵
        /// </summary>
        /// <param name="sitaution">地点の状態</param>
        /// <returns>高いほど移動するに良い地点</returns>
        private float SingleEnemy(Situation sitaution)
        {
            if (!sitaution.enemies.IndexAt(0, out var enemy))
                return 0;

            var output = 0f;

            if (enemy.DamageRateToThis == 1)
            {
                // 倒し切れる場合
                // カバー分のポイント、（近いほど軽減されなくなる）
                var coverPoint = 0f;
                if (enemy.IsCoveredFromThis)
                    coverPoint = CoveredCurveField.value.Evaluate(1 - enemy.HitRateToThis);
                var aimPoint = NormalAimCurveField.value.Evaluate(enemy.HitRateToThis);
                output = aimPoint == 0 ? 0 : CalcRate(aimPoint, coverPoint, 0.85f, 0.15f);

            }
            else if (enemy.DamageRateToThis >= 0.5)
            {
                // 50%以上体力を削れる場合
                var coverPoint = 0f;
                if (enemy.IsCoveredFromThis)
                    coverPoint = CoveredCurveField.value.Evaluate(1 - enemy.HitRateToThis);

                var nearFriends = sitaution.Tile.UnitsInCell.FindAll(u => !u.IsEnemyFromMe(sitaution.active));
                var firendPoint = nearFriends.Count >= 2 ? 0.9f : 0.5f;
                coverPoint = CalcRate(firendPoint, coverPoint, 0.6f, 0.4f);

                var aimPoint = NormalAimCurveField.value.Evaluate(enemy.HitRateToThis);
                output = aimPoint == 0 ? 0 : CalcRate(aimPoint, coverPoint, 0.9f, 0.1f);
            }
            else if (enemy.DamageRateToThis >= 0.3)
            {
                // 30%以上体力を削れる場合
                var coverPoint = 0f;
                if (enemy.IsCoveredFromThis)
                    coverPoint = CoveredCurveField.value.Evaluate(1 - enemy.HitRateToThis);

                var nearFriends = sitaution.Tile.UnitsInCell.FindAll(u => !u.IsEnemyFromMe(sitaution.active));
                var firendPoint = nearFriends.Count >= 3 ? 0.8f : 0.4f;
                coverPoint = CalcRate(firendPoint, coverPoint, 0.6f, 0.4f);

                var aimPoint = HardAimCurveField.value.Evaluate(enemy.HitRateToThis);
                output = aimPoint == 0 ? 0 : CalcRate(aimPoint, coverPoint, 0.9f, 0.1f);
            }
            else
            {
                // 30%以下でしか体力を削れない場合
                var coverPoint = 0f;
                if (enemy.IsCoveredFromThis)
                    coverPoint = CoveredCurveField.value.Evaluate(1 - enemy.HitRateToThis);

                var nearFriends = sitaution.Tile.UnitsInCell.FindAll(u => !u.IsEnemyFromMe(sitaution.active));
                var firendPoint = nearFriends.Count >= 4 ? 0.6f : 0.1f;
                coverPoint = CalcRate(firendPoint, coverPoint, 0.6f, 0.4f);

                var aimPoint = HardAimCurveField.value.Evaluate(enemy.HitRateToThis);
                output = aimPoint == 0 ? 0 : CalcRate(aimPoint, coverPoint, 0.4f, 0.6f);
            }

            var moveRisk = MoveRiskCurveField.value.Evaluate(Vector3.Distance(MyUnitController.transform.position, sitaution.pointInTile.location));
            
            return output * moveRisk;
        }

        /// <summary>
        /// 複数の敵
        /// </summary>
        /// <param name="situation">状態</param>
        /// <returns>ターゲットと 高いほど移動するに良い地点</returns>
        private (Enemy target, float score) MultiEnemies(Situation situation)
        {

            var active = situation.active;
            var canKillEnemies = situation.enemies.FindAll(e => e.CanKillThis);
            var enemies = situation.enemies;
            enemies.Sort((a, b) => (int)((a.DamageFromThis - b.DamageFromThis) * 100));

            var totalDamageFromEnemies = situation.enemies.Sum(e => e.DamageFromThis);
            // 敵の攻撃で受けるダメージの割合
            var damageRateFromEnemies = totalDamageFromEnemies >= active.CurrentParameter.HealthPoint ? 1 : totalDamageFromEnemies / active.CurrentParameter.HealthPoint;
            // 移動に伴うリスク上昇
            var moveRisk = MoveRiskCurveField.value.Evaluate(Vector3.Distance(MyUnitController.transform.position, situation.pointInTile.location));

            // RatingInfoのtargetからの反撃に対してカバーできる箇所にいるか計算
            float CalcCoverPoint(Enemy target, float hitRate)
            {
                var coverPoint = 0f;
                if (target.IsCoveredFromThis)
                    coverPoint = CoveredCurveField.value.Evaluate(1 - hitRate);

                var nearFriends = situation.Tile.UnitsInCell.FindAll(u => !u.IsEnemyFromMe(active));
                var firendPoint = nearFriends.Count >= 2 ? 0.9f : 0.5f;
                return CalcRate(firendPoint, coverPoint, 0.6f, 0.4f);
            }

            // 次のターンで自身が撃破されないように 6割以下のダメージに抑えられる場合
            if (damageRateFromEnemies < 0.6f)
            {
                // 自身に高いダメージを与え尚且つこのターンの攻撃で撃破可能な敵ユニット
                var hightDamageAndCanKill = enemies.Find(e => canKillEnemies.Contains(e));
                if (hightDamageAndCanKill != null)
                {
                    var aimPoint = NormalAimCurveField.value.Evaluate(hightDamageAndCanKill.HitRateToThis);

                    var coverPoint = CalcCoverPoint(hightDamageAndCanKill, hightDamageAndCanKill.HitRateToThis);
                    var totalPoint = CalcRate(aimPoint, coverPoint, 0.8f, 0.2f);
                    totalPoint *= moveRisk;

                    return (hightDamageAndCanKill, totalPoint);
                }

                enemies.Sort((a, b) => (int)((a.DamageRateToThis - b.DamageRateToThis) * 100));

                if (enemies.IndexAt(0, out var betterDamageTarget))
                {
                    // 敵のHPを60%以上削れる場合
                    var aimPoint = CalcRate(betterDamageTarget.HitRateToThis, betterDamageTarget.DamageRateToThis, 0.6f, 0.4f);
                    aimPoint = NormalAimCurveField.value.Evaluate(aimPoint);

                    var coverPoint = CalcCoverPoint(betterDamageTarget, betterDamageTarget.HitRateToThis);
                    var totalPoint = CalcRate(aimPoint, coverPoint, 0.7f, 0.3f);
                    totalPoint *= moveRisk;

                    return (betterDamageTarget, totalPoint);
                }

            }
            else
            {
                // 上記以外　被撃破の可能性があり　あまり推奨しない

                enemies.Sort((a, b) => (int)((a.DamageRateToThis - b.DamageRateToThis) * 100));
                if (enemies.IndexAt(0, out var betterDamageTarget))
                {
                    var aimPoint = CalcRate(betterDamageTarget.HitRateToThis, betterDamageTarget.DamageRateToThis, 0.6f, 0.4f);
                    aimPoint = HardAimCurveField.value.Evaluate(aimPoint);

                    var coverPoint = CalcCoverPoint(betterDamageTarget, betterDamageTarget.HitRateToThis);
                    var totalPoint = CalcRate(aimPoint, coverPoint, 0.6f, 0.4f);
                    totalPoint *= moveRisk;

                    return (betterDamageTarget, totalPoint);
                }

            }

            return (null, 0);
        }

    }
}