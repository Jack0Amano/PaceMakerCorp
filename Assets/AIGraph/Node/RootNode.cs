using AIGraph.Editor;
using AIGraph.InOut;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using Tactics.Character;

namespace AIGraph.Nodes
{
    public class RootNode : ProcessNode
    {
        /// <summary>
        /// Eventを識別するID
        /// </summary>
        public string AIID { get => AIIdField.value; }

        private readonly TextField AIIdField;

        private readonly string AIIDKey = "EventID";

        public RootNode() : base()
        {

            title = "Root";
            IsEntryPoint = true;

            capabilities -= Capabilities.Deletable;

            AIIdField = new TextField();
            AIIdField.style.minWidth = 100;
            Add(AIIdField);

            inputContainer.Remove(InputPort);
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            input.OutPort = OutputPort;
            return input;
        }

        // Castしても中身は失われない ただしInit内でSimple内の使用するUIElementを予め初期化して置かなければならない
        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(AIIDKey, out string id))
                AIIdField.value = id;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(AIIDKey, AIIdField.value);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            AIIdField.RegisterValueChangedCallback(e => action?.Invoke(this));
        }
    }
}