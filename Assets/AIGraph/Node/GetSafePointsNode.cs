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

namespace AIGraph.Nodes
{

    public class GetSafePointsNode : SampleNode
    {
        /// <summary>
        /// ダメージのリスク値 　通常右肩上がりのグラフで、勾配がきついほどダメージに対するリスク評価が高くなる(ダメージをより恐れる)
        /// </summary>
        readonly CurveField DamageCurveField;

        readonly CustomPort OutputPort;

        public GetSafePointsNode() : base()
        {
            title = "Get Safe Points";
            NodePath = "Functions/Get Safe Points";

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AIGraph/Node/Style/GetSafePointsNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Situation));
            OutputPort.portName = "Situations";
            outputContainer.Add(OutputPort);

            DamageCurveField = mainContainer.Q<CurveField>("DamageCurveField");
        }


        // Escape, ToWeakTarget, SafeHP系のスコアを出す Attack系のGetPositionActionの逆的な
        /// <summary>
        /// <c>situations</c>の中から安全な場所とそのスコアを算出する
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="situations"></param>
        /// <param name="removeUnit">計算から外すUnit</param>
        /// <returns>(安全なsituation, 0~1で低いほど安全)</returns>
        internal (Situation situation, float point) GetSafePoint(EnvironmentData data, List<Situation> situations)
        {
            EnvironmentData = data;
            // TODO Forcastのenemyを考慮していない
            // Situation.Enemy.positionを利用して
            var unit = MyUnitController;

            // Scoreは0~1で高いほど有力地
            var situationAndRiskScore = new Dictionary<Situation, float>();
            foreach (var situation in situations)
            {
                if (UnitsController.UnitsList.FindAll(u => u.IsInMyArea(situation.pointInTile.location)).Count != 0)
                    continue;
                situationAndRiskScore[situation] = 1f;

                if (situation.enemies.Count == 0)
                {
                    var allEnemies = UnitsController.UnitsList.FindAll(u => unit.IsEnemyFromMe(u));
                    allEnemies.Sort((a, b) => {
                        var distA = Vector3.Distance(unit.transform.position, a.transform.position);
                        var distB = Vector3.Distance(unit.transform.position, b.transform.position);
                        return (int)((distA - distB) * 100);
                    });

                    var isCovered = IsCoveredFromEnemy(situation.pointInTile, allEnemies[0].transform.position);
                    situationAndRiskScore[situation] = isCovered ? 1f : 0.96f;
                }
                else
                {
                    foreach (var enemy in situation.enemies)
                    {
                        var damageScore = CalcRate(enemy.DamageRateFromThis, enemy.HitRateToThis, 0.95f, 0.1f);
                        damageScore = DamageCurveField.value.Evaluate(damageScore);

                        var coverScore = enemy.IsCoveredFromThis ? 0.05f : 0f;

                        situationAndRiskScore[situation] += damageScore + coverScore;
                        situationAndRiskScore[situation] /= 2;
                    }
                }

                var enemyInCurrentTile = AIController.FindedEnemies.Find(e => e.thisUnit.tileCell == MyUnitController.tileCell) != null;
                var enemyInSituationTile = AIController.FindedEnemies.Find(e => e.thisUnit.tileCell == situation.Tile) != null;
                if (situation.Tile == MyUnitController.tileCell)
                    enemyInSituationTile = false;

                situationAndRiskScore[situation] += enemyInSituationTile ? 0.05f : 0f;
                
                var distToLoc = Vector3.Distance(situation.pointInTile.location, MyUnitController.transform.position);

                // DEBUG Show debug score
                situation.pointInTile.DebugScore = situationAndRiskScore[situation];
            }

            var _list = situationAndRiskScore.ToList().Shuffle();
            var max = _list.FindMax(s => s.Value);

            return (max.Key, max.Value);
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            DamageCurveField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            base.RegisterAnyValueChanged(action);
        }

        public override NodeData Save()
        {
            var save = base.Save();
            save.raw.SetToPairs(nameof(DamageCurveField), DamageCurveField.value);
            return save;
        }

        public override void Load(NodeData data)
        {
            if (data.raw.GetFromPairs(nameof(DamageCurveField), out AnimationCurve curve))
                DamageCurveField.value = curve;
            base.Load(data);
        }
    }
}