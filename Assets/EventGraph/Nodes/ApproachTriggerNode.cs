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
    public class ApproachTriggerNode : SampleTriggerNode
    {

        readonly ObjectField GameObjectField;
        readonly TextField FriendTextField;

        private readonly string FriendIDFieldKey = "FriendUnitIDKey";

        public ApproachTriggerNode() : base()
        {
            title = "Approach Trigger";
            NodePath = "Trigger/Approach";

            var outputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputContainer.Add(outputPort);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/ApproachTriggerNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            GameObjectField = container.Q<ObjectField>();
            FriendTextField = container.Q<TextField>();
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(FriendIDFieldKey, out string value))
                FriendTextField.value = value;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(FriendIDFieldKey, FriendTextField.value);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            FriendTextField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }

        public override bool Check(EventInput input)
        {
            return false;
        }
    }
}