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

namespace EventGraph.Nodes.Trigger
{
    public class OwnItemTriggerNode : SampleTriggerNode
    {
        readonly TextField ItemIDTextField;

        private readonly string OwnItemIDKey = "OwnItemIDKey";

        public OwnItemTriggerNode() : base()
        {
            title = "Own Item Trigger";
            NodePath = "Trigger/Own Item Trigger";

            var outputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputContainer.Add(outputPort);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/OwnItemTriggerNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            ItemIDTextField = container.Q<TextField>();
        }

        public override void Load(NodeData data)
        {
            if (data.raw.GetFromPairs<string>(OwnItemIDKey, out var friend))
                ItemIDTextField.value = friend;

            base.Load(data);
        }

        public override NodeData Save()
        {
            var node = base.Save();
            node.raw.SetToPairs(OwnItemIDKey, ItemIDTextField.value);

            return node;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            ItemIDTextField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }

        public override bool Check(InOut.EventInput input)
        {
            if (input == null || input.ItemsID ==null) return false;
            return input.ItemsID.Contains(ItemIDTextField.value);
        }
    }
}