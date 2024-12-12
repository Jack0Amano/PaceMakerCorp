using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using EventGraph.Nodes.Parts;
using EventGraph.Editor;
using EventGraph.InOut;
using UnityEditor;

namespace EventGraph.Nodes
{
    /// <summary>
    /// EventÇÃèIóπÇStoryGraphÇ…í ímÇ∑ÇÈ
    /// </summary>
    public class EndEventNode : ProcessNode
    {
        public string EventReturnName { get => OutputNameField.value; }

        readonly TextField OutputNameField;
        public readonly static string EndEventNameKey = "EndEventNameKey";
        public readonly static string EndEventKey = "EndEventKey";
        public string EndEventID;

        public EndEventNode() : base()
        {
            title = "End Event";
            NodePath = "Process/End Event";

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/EndEventNode.uxml");
            var content = asset.Instantiate();
            mainContainer.Add(content);
            OutputNameField = content.Q<TextField>();

            Guid guid = System.Guid.NewGuid();
            EndEventID = guid.ToString();

            OutputPort.RemoveFromHierarchy();
        }

        public override EventOutput Execute(EventInput eventInput)
        {
            return CreateOutput();
        }

        private EventOutput CreateOutput()
        {
            var evt = new EventOutput(this, Guid, null);
            return evt;
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            data.raw.GetFromPairs(EndEventKey, out string id);
            if (data.raw.GetFromPairs(EndEventNameKey, out string text))
                OutputNameField.value = text;
            EndEventID = id;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(EndEventKey, EndEventID);
            data.raw.SetToPairs(EndEventNameKey, OutputNameField.value);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            OutputNameField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }
    }
}