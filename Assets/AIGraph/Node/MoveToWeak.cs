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

    public class MoveToWeak : ProcessNode
    {
        [Tooltip("移動Tileの増加に反比例して減少するScore" +
                 "X軸は10倍され  (0.1, 1) ~ (0.5, 0)")]
        [SerializeField] CurveField WayCountCurveField;
        const string WayCountCurveKey = "WayCountCurveKey";

        readonly CustomPort SafePointsPort;

        public MoveToWeak() : base()
        {
            title = "Move to weak target";
            NodePath = "AI/Move to weak target";

            InputPort.RemoveFromHierarchy();
            InputPort.portType = typeof(List<Situation>);
            InputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(List<Situation>));
            InputPort.portName = "Input";
            inputContainer.Add(InputPort);

            OutputPort.RemoveFromHierarchy();
            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(AIAction));
            outputContainer.Add(OutputPort);

            SafePointsPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Situation));
            SafePointsPort.portName = "SafePoints";
            inputContainer.Add(SafePointsPort);

            WayCountCurveField = new CurveField();
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            WayCountCurveField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            base.RegisterAnyValueChanged(action);
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(WayCountCurveKey, WayCountCurveField.value);
            return data;
        }

        public override void Load(NodeData data)
        {
            if (data.raw.GetFromPairs(WayCountCurveKey, out AnimationCurve curve))
                WayCountCurveField.value = curve;
            base.Load(data);
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            input = base.Execute(input);
            var activeTile = CurrentTile;
            var unit = MyUnitController;
            var situations = input.NearTileAndSituations;
            var getSafePointsNode = (GetSafePointsNode)SafePointsPort.ConnectedNodes[0];
            input.OutPort = OutputPort;

            // 既に見つけているEnemyのScoreを計算
            var enemies = AIController.FindedEnemies.ConvertAll(d =>
            {
                return (d.Enemy, CalcHealthScore(d.Enemy));
            });
            enemies.Sort((a, b) => (int)((b.Item2 - a.Item2) * 1000));
            var targets = enemies.Slice(0, 3).ToDictionary(a => a.Enemy, a => a.Item2);

            // Enemyへの道のりを計算
            var unitCellDict = new Dictionary<UnitController, TileCell>();
            // cellScoreDict.key = 目的地TileCell,  cellScoreDict.value.next = 目的地に向かうための次のTile
            var cellScoreDict = new Dictionary<TileCell, (float score, TileCell next)>();

            var targetScoreAndWaysDict = new Dictionary<TileCell, List<(float score, TileCell next)>>();
            targets.Keys.ToList().ForEach(t =>
            {
                targetScoreAndWaysDict[t.tileCell] = new List<(float score, TileCell next)>();
                unitCellDict[t] = t.tileCell;
            });

            // 最大3つの経路を探索する
            List<List<TileCell>> SearchWays(TileCell end)
            {
                var output = new List<List<TileCell>>();

                var way1 = TilesController.GetShortestWay(activeTile, end);
                output.Add(way1);
                // 隣接Cellの場合 Way探索は1ルートのみ
                if (way1.Count <= 2)
                    return output;

                var exceptTiles = new List<TileCell>();

                exceptTiles.Add(way1[1]);
                var way2 = TilesController.GetShortestWay(activeTile, end, exceptTiles);

                if (way2.Count == 0)
                {
                    // way1[1]を通らないway2が存在しないということ
                    if (way1.Count > 3)
                    {
                        exceptTiles.Clear();
                        exceptTiles.Add(way1[2]);
                        var way3 = TilesController.GetShortestWay(activeTile, end, exceptTiles);
                        if (way3.Count != 0)
                            output.Add(way3);
                    }
                    else
                    {
                        // way1.Count == 3 ということは start - way[1] - end　以外のルートが存在しない
                        return output;
                    }
                }
                else
                {
                    // way1[1]を通らないway2が存在する
                    output.Add(way2);

                    if (way2.Count > 3)
                    {
                        exceptTiles.Add(way2[1]);
                        var way3 = TilesController.GetShortestWay(activeTile, end, exceptTiles);
                        if (way3.Count != 0)
                            output.Add(way3);
                    }

                }

                return output;
            }

            // TODO 現在地点のTileにEnemyが存在する場合にway.count == 1となってerror
            // ActiveCell -> TargetCellへ距離が近いほど優先的
            // Goalに向けてMoveするための初期経路を取得
            void GetWaysForEachTargets(TileCell goalTile)
            {
                if (goalTile == activeTile)
                {
                    // 敵が現在のcellにいる
                    targetScoreAndWaysDict[goalTile] = new List<(float, TileCell)> { (1, goalTile) };
                    return;
                }

                var ways = SearchWays(goalTile);
                foreach (var way in ways)
                {
                    var wayScore = WayCountCurveField.value.Evaluate((float)ways.Count / 10f);

                    if (way.Count > 1)
                    {
                        if (targetScoreAndWaysDict[goalTile] != null)
                            targetScoreAndWaysDict[goalTile].Add((wayScore, way[1]));
                        else
                            targetScoreAndWaysDict[goalTile] = new List<(float, TileCell)> { (wayScore, way[1]) };
                    }

                }
            }

            targetScoreAndWaysDict.ToList().ForEach(pair =>
            {
                GetWaysForEachTargets(pair.Key);
            });

            // すべてのスコアを合算したもの
            var totalScoreAndSituation = new List<(Situation situ, float point)>();
            var tileAndSafeSituationBuffer = new Dictionary<TileCell, (Situation situ, float point)>();
            // WayScoreとHealthScoreを加算
            unitCellDict.ToList().ForEach(pair =>
            {
                // 攻撃に適したEnemyを表すPoint
                // 攻撃に適したEnemyを表すPoint
                // 0~1で高いほど適したUnit
                var enemyScore = targets[pair.Key];

                // pair.Value (TileCell) に行くための道順をGetSafePointする
                targetScoreAndWaysDict[pair.Value].ForEach(scoreAndNext =>
                {
                    // 移動の短くて済む 高いほうが良い経路 
                    var wayScore = scoreAndNext.score;

                    if (!tileAndSafeSituationBuffer.TryGetValue(scoreAndNext.next, out var safe))
                    {
                        // Bufferにない場合は作成
                        safe = getSafePointsNode.GetSafePoint(EnvironmentData, situations.FindAll(s => s.Tile = scoreAndNext.next));
                        tileAndSafeSituationBuffer[scoreAndNext.next] = safe;
                    }
                    // safe.pointは移動地点として安全な場所
                    // safe.pointは 0~1で高いほど安全
                    var safePoint = safe.point;

                    totalScoreAndSituation.Add((safe.situ, safePoint + wayScore + enemyScore));
                });
            });

            // DEBUG Error when run forcast
            var betterSitu = totalScoreAndSituation.FindMax(t => t.point);

            var output = new AIAction(unit);
            output.locationToMove = betterSitu.situ.pointInTile.location;
            output.OrderOfAction = OrderOfAction.MoveToSkip;
            output.Rate = 0;
            output.Target = null;
            AIAction = output;

            return input;
        }
    }
}