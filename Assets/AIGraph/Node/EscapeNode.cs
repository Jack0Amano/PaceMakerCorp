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
    /// <summary>
    /// 逃走をするためのAI FirendCountは2以上
    /// </summary>
    public class EscapeNode : ProcessNode
    {
        readonly CustomPort SafePointsPort;
        readonly Toggle CanAttackToggle;
        const string CanAttackToggleKey = "CanAttackToggleKey";

        public EscapeNode(): base()
        {
            title = "Escape";
            NodePath = "AI/Escape";

            OutputPort.RemoveFromHierarchy();
            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(AIAction));
            OutputPort.portName = "AI Action";
            outputContainer.Add(OutputPort);

            CanAttackToggle = new Toggle("Can attack");
            mainContainer.Add(CanAttackToggle);

            SafePointsPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Situation));
            SafePointsPort.portName = "SafePoints";
            inputContainer.Add(SafePointsPort);
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            base.Execute(input);

            var unit = MyUnitController;
            var current = CurrentTile;
            var getSafePointsNode = (GetSafePointsNode)SafePointsPort.ConnectedNodes[0];

            // TODO 隣接Cellに敵がいないかつ当該セルに敵がいない条件を付加
            var cells = current.borderOnTiles.FindAll(t => t.CanEnterUnitInThis(unit));
            cells.Add(current);

            // 隣接Tileに敵がいないかつ当該セルに敵がいない
            cells.RemoveAll(t =>
            {
                if (t.UnitsInCell.Find(u => u.IsEnemyFromMe(unit)))
                    return true;
                if (t.borderOnTiles.Find(bt => bt.UnitsInCell.Find(u => u.IsEnemyFromMe(unit))))
                    return true;
                return false;
            });
            if (cells.Count == 0)
            {
                // 囲まれているため現時点のTileから移動しない
                cells.Add(current);
            }

            var cellAndSituation = new List<(TileCell tile, (Situation situation, float point) value)>();
            void _GetSituations(TileCell c)
            {
                var situations = GetSituations(c);
                cellAndSituation.Add((c, getSafePointsNode.GetSafePoint(EnvironmentData, situations)));
            }
            cells.ForEach(c => _GetSituations(c));
            // situationsCoroutines.ForEach(c => StartCoroutine(c));

            // 各Friendsまでの最短距離を取得
            var friends = UnitsController.GetFriendsOf(unit);
            var wayToFriends = new List<(UnitController friend, List<TileCell> way)>();
            void _ShrotestWaies()
            {
                wayToFriends = friends.ConvertAll(f =>
                {
                    return (f, TilesController.GetShortestWay(current, f.tileCell));
                });
            }
            _ShrotestWaies();

            // 各Coroutineを待つ
            //while (shortestWaiesCoroutine.IsNotCompleted(true))
            //    yield return null;

            //while (situationsCoroutines.AreNotCompleted(true))
            //    yield return null;

            Vector3 nextPoint = unit.transform.position;
            if (wayToFriends.Count != 0)
            {
                // Friendのいる方向に逃げる
                // 道順までのスコアが同じならHPの多い方に逃げる
                var max = wayToFriends.Max(a => a.way.Count * 100 + ((a.friend.CurrentParameter.HealthPoint * 100) / (a.friend.CurrentParameter.Data.HealthPoint * 100)));
                var _max = (float)max / 100;

                wayToFriends.Sort((a, b) =>
                {
                    var wayA = a.way.Count * 100 + ((a.friend.CurrentParameter.HealthPoint * 100) / (a.friend.CurrentParameter.Data.HealthPoint * 100));
                    var rateA = ((float)wayA / 100) / _max;
                    var wayB = b.way.Count * 100 + ((b.friend.CurrentParameter.HealthPoint * 100) / (b.friend.CurrentParameter.Data.HealthPoint * 100));
                    var rateB = ((float)wayB / 100) / _max;

                    var situA = cellAndSituation.Find(x => x.tile == a.way[1]);
                    var situB = cellAndSituation.Find(x => x.tile == b.way[1]);

                    var A = CalcRate(rateA, situA.value.point, 0.3f, 0.7f);
                    var B = CalcRate(rateB, situB.value.point, 0.3f, 0.7f);

                    var _A = (int)(A * 100);
                    var _B = (int)(B * 100);

                    return _A - _B;
                });

                var nextTile = wayToFriends[0].way[1];
                nextPoint = cellAndSituation.Find(cs => cs.tile == nextTile).value.situation.pointInTile.location;
            }
            else
            {
                // 仲間への道が存在しない場合
                // 隣接するCellで最も安全な箇所
                if (cellAndSituation.Count != 0)
                {
                    var safeSituation = cellAndSituation.FindMin(cs => cs.value.point).value.situation;
                    nextPoint = safeSituation.pointInTile.location;
                }
                else
                {
                    PrintError("EscapeAI: No situation");
                }
            }

            var action = new AIAction(unit);
            action.locationToMove = nextPoint;

            // 現在位置からActionで狙える敵が存在する場合に攻撃しておいてから逃げる
            
            if (CanAttackToggle.value)
            {
                var loc = new PointInTile(unit);
                var currentSitu = GetSituation( loc);
                var currentEnemies = currentSitu.enemies;
                if (currentEnemies.Count != 0)
                {
                    currentEnemies.Sort((a, b) => (int)((a.DamageRateToThis - b.DamageRateToThis) * 100));
                    action.Target = currentEnemies[0].unit;
                    action.Rate = MyUnitController.GetAIAttackRate(currentEnemies[0].unit);
                    action.OrderOfAction = OrderOfAction.ActionTo_;
                }
                else
                {
                    action.OrderOfAction = OrderOfAction.MoveToSkip;
                }
            }
            else
            {
                action.OrderOfAction = OrderOfAction.MoveToSkip;
            }

            AIAction = action;

            input.OutPort = OutputPort;
            return input;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            CanAttackToggle.RegisterValueChangedCallback(evt => action?.Invoke(this));
            base.RegisterAnyValueChanged(action);
        }

        public override void Load(NodeData data)
        {
            if (data.raw.GetFromPairs(CanAttackToggleKey, out bool canAttack))
                CanAttackToggle.value = canAttack;
            base.Load(data);
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(CanAttackToggleKey, CanAttackToggle.value);
            return data;
        }
    }
}