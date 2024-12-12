using EventGraph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Linq;
using EventGraph.Nodes.Parts;
using EventGraph.InOut;
using static Tactics.VictoryConditions;

namespace EventGraph.Nodes.Trigger
{
    public class GameResultTriggerNode : SampleTriggerNode
    {

        readonly EnumField EnumField;

        private readonly string ResultTriggerKey = "GameResultTriggerKey";

        public GameResultTriggerNode() : base()
        {
            title = "Game Result";
            NodePath = "Trigger/Game Result";

            var outputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputContainer.Add(outputPort);

            EnumField = new EnumField();
            EnumField.Init(GameResult.Win) ;
            mainContainer.Add(EnumField);
        }

        public override void Load(NodeData data)
        {
            data.raw.GetFromPairs<string>(ResultTriggerKey, out var trigger);
            if (((GameResult[])Enum.GetValues(typeof(GameResult))).ToList().TryFindFirst(t => t.ToString() == trigger, out var type))
                EnumField.value = type;

            base.Load(data);
        }

        public override NodeData Save()
        {
            var node = base.Save();
            var trigger = (GameResult)EnumField.value;
            node.raw.SetToPairs(ResultTriggerKey, trigger.ToString());
            
            return node;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            EnumField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }

        public override bool Check(EventInput input)
        {
            if (input.gameResultTrigger == (GameResult)EnumField.value)
                return true;
            return false;
        }
    }
}