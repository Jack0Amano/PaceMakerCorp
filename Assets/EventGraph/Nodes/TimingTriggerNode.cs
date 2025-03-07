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

namespace EventGraph.Nodes.Trigger
{
    public class TimingTriggerNode : SampleTriggerNode
    {

        readonly EnumField EnumField;
        /// <summary>
        /// どのSpawnIDを持つenemyとencountしたときに発動するTriggerか
        /// </summary>
        readonly TextField SpawnIDField;

        private readonly string TimingTriggerKey = "TimingTriggerKey";
        private readonly string SpawnIDKey = "EnemyUnitIDKey";

        public TimingTriggerNode() : base()
        {
            title = "Timing Trigger";
            NodePath = "Trigger/Timing Trigger";

            var outputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputContainer.Add(outputPort);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/TimingTriggerNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            EnumField = container.Q<EnumField>();
            EnumField.Init(TriggerTiming.BeforeBattle);

            SpawnIDField = container.Q<TextField>();
        }

        public override void Load(NodeData data)
        {
            data.Raw.GetFromPairs<string>(TimingTriggerKey, out var trigger);
            if (((TriggerTiming[])Enum.GetValues(typeof(TriggerTiming))).ToList().TryFindFirst(t => t.ToString() == trigger, out var type))
                EnumField.value = type;

            if(data.Raw.GetFromPairs<string>(SpawnIDKey, out var id))
                SpawnIDField.value = id;

            base.Load(data);
        }

        public override NodeData Save()
        {
            var node = base.Save();
            var trigger = (TriggerTiming)EnumField.value;
            node.Raw.SetToPairs(TimingTriggerKey, trigger.ToString());

            node.Raw.SetToPairs(SpawnIDKey, SpawnIDField.value);
            
            return node;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            EnumField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            SpawnIDField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }

        public override bool Check(EventInput input)
        {
            if (SpawnIDField.value.Length == 0 && input.TriggerTiming == (TriggerTiming)EnumField.value)
                return true;
            if (SpawnIDField.value == input.EncountSpawnID && input.TriggerTiming == (TriggerTiming)EnumField.value)
                return true;
            return false;
        }
    }
}