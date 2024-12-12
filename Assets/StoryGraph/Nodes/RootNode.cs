using StoryGraph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using StoryGraph.InOut;
using StoryGraph.Nodes.Parts;

namespace StoryGraph.Nodes
{
    public class RootNode : ProcessNode
    {
        /// <summary>
        /// Storyを識別するID
        /// </summary>
        public string StoryID { get => StoryIdField.value; }

        internal CustomPort OutputPort;
        private readonly TextField StoryIdField;

        private readonly string StoryIDKey = "StoryID";

        public RootNode() : base()
        {

            title = "Root";
            IsEntryPoint = true;

            capabilities -= Capabilities.Deletable;

            StoryIdField = new TextField();
            StoryIdField.style.minWidth = 100;
            Add(StoryIdField);

            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(CustomPort));
            OutputPort.portName = "In";
            outputContainer.Add(OutputPort);

            inputContainer.Remove(InputPort);
        }

        public override CustomPort FindOutputPortFromEndEventName(string endPortName)
        {

            return OutputPort;
        }

        // Castしても中身は失われない ただしInit内でSimple内の使用するUIElementを予め初期化して置かなければならない
        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(StoryIDKey, out string id))
                StoryIdField.value = id;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(StoryIDKey, StoryIdField.value);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            StoryIdField.RegisterValueChangedCallback(e => action?.Invoke(this));
        }

    }
}