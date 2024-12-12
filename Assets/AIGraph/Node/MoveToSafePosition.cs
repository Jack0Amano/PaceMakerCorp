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

    public class MoveToSafePosition : ProcessNode
    {

        readonly CustomPort SafePointsPort;

        readonly Toggle StayOnTileToggleButton;

        public MoveToSafePosition() : base()
        {
            title = "Move to safe pos";
            NodePath = "AI/Move to safe pos";

            OutputPort.RemoveFromHierarchy();
            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(AIAction));
            outputContainer.Add(OutputPort);

            SafePointsPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Situation));
            SafePointsPort.portName = "SafePoints";
            inputContainer.Add(SafePointsPort);

            StayOnTileToggleButton = new Toggle("Stay on tile");
            mainContainer.Add(StayOnTileToggleButton);
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            StayOnTileToggleButton.RegisterValueChangedCallback(evt => action?.Invoke(this));
            base.RegisterAnyValueChanged(action);
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(nameof(StayOnTileToggleButton), StayOnTileToggleButton.value);
            return data;
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(nameof(StayOnTileToggleButton), out bool value))
                StayOnTileToggleButton.value = value;
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            base.Execute(input);

            var current = CurrentTile;
            var getSafePointsNode = (GetSafePointsNode)SafePointsPort.ConnectedNodes[0];

            // CurrentTileのSituationを取得する
            var situations = GetSituations(CurrentTile);

            // 横のSituationを取得する
            if (!StayOnTileToggleButton.value)
            {
                var safeTiles = current.borderOnTiles.FindAll(t =>
                {
                    return !t.UnitsInCell.Exists(u => AIController.FindedEnemies.Exists(e => e.Enemy == u)) &&
                           t.CanEnterUnitInThis(MyUnitController);
                });
                CurrentTile.borderOnTiles.ForEach(t =>
                {
                    if (!t.UnitsInCell.Exists(u => AIController.FindedEnemies.Exists(e => e.Enemy == u)))
                    {
                        situations.AddRange(GetSituations(t));
                    }
                });
            }

            var (betterSituation, point) = getSafePointsNode.GetSafePoint(EnvironmentData, situations);
            var action = new AIAction(MyUnitController);
            action.OrderOfAction = OrderOfAction.MoveToSkip;
            action.locationToMove = betterSituation.pointInTile.location;
            input.OutPort = OutputPort;
            AIAction = action;

            return input;
        }
    }
}