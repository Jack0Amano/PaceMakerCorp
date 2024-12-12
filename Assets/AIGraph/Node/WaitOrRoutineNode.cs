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

namespace AIGraph.Nodes
{
    /// <summary>
    /// Unitが敵を発見してないため通常のルーチンを行う際のNode
    /// </summary>
    public class WaitOrRoutineNode : ProcessNode
    {

        public WaitOrRoutineNode(): base()
        {
            NodePath = "AI/Wait or Routine";
            title = "Wait or Routine";

            OutputPort.RemoveFromHierarchy();
            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(AIAction));
            OutputPort.portName = "AI Action";
            outputContainer.Add(OutputPort);
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            base.Execute(input);
            input.OutPort = OutputPort;
            if (WayPassPoints != null && WayPassPoints.Count != 0)
            {

                var currentTile = MyUnitController.tileCell;
                var currentPassPointIndex = WayPassPoints.FindIndex(w => w.tile == currentTile);

                if (currentPassPointIndex == -1)
                {
                    // 現在UnitはwayPassから外れたTileに存在する
                    // 最寄りのTileに戻る
                    AIAction = MoveToNearWayPassTile(currentTile, null);

                }

                if (!WayPassPoints.IndexAt_Bug(currentPassPointIndex + 1, out var nextPass))
                    nextPass = WayPassPoints.First();

                if (currentTile.borderOnTiles.Contains(nextPass.tile))
                {
                    // nextPassのtileがcurrentTileにつながっている
                    var action = new AIAction(MyUnitController);
                    action.OrderOfAction = OrderOfAction.MoveAndFind;
                    action.locationToMove = nextPass.point.position;
                    action.rotation = nextPass.point.rotation;
                    AIAction = action;
                }
                else
                {
                    // nextPasのtileがcurrentTileから離れている
                    AIAction = MoveToNearWayPassTile(currentTile, nextPass.tile);
                }
            }
            else
            {
                var action = new AIAction(MyUnitController);
                action.OrderOfAction = OrderOfAction.Skip;

                AIAction = action;
            }

            return input;
        }

        /// <summary>
        /// 現在UnitがWayPass上に存在しないため最寄りのwaypassTileに移動する
        /// </summary>
        /// <returns></returns>
        private AIAction MoveToNearWayPassTile(TileCell current, TileCell to)
        {
            var action = new AIAction(MyUnitController);
            action.OrderOfAction = OrderOfAction.Skip;

            if (to == null)
            {
                var _nearTile = WayPassPoints.FindMin(pp => TilesController.GetShortestWay(current, pp.tile).Count);
                if (_nearTile == default((Transform, TileCell)))
                    return action;
                to = _nearTile.tile;
            }

            var way = TilesController.GetShortestWay(current, to);
            if (way.Count <= 2)
                return action;

            var nextPos = way[1].pointsInTile.Find(p => p.isNormalPosition);
            action.OrderOfAction = OrderOfAction.MoveAndFind;
            action.locationToMove = nextPos.location;

            return action;
        }

    }
}