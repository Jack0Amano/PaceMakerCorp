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

namespace AIGraph.Nodes
{
    /// <summary>
    /// 指定されたgraphを動かしreturn のaiactionを返す
    /// </summary>
    public class RunGraphNode : ProcessNode
    {
        ObjectField FileField;

        public AIGraph.Editor.AIGraphDataContainer DataContainer { get => (AIGraph.Editor.AIGraphDataContainer)FileField.value; }
        /// <summary>
        /// FileFieldに設定されたAIGraphのview
        /// </summary>
        public AIGraph.AIGraphView AIGraphView
        {
            get
            {
                if (_AIGraphView == null)
                    _AIGraphView = AIGraph.Editor.AIGraphSaveUtility.LoadGraph(DataContainer);
                return _AIGraphView;
            }
        }
        private AIGraph.AIGraphView _AIGraphView;

        readonly static public string AIGraphFileKey = "AIGraphFileKey";
        /// <summary>
        /// 実行したAIGRaphの結果
        /// </summary>
        public AIAction ResultOfAIGraph;

        public RunGraphNode(): base()
        {
            NodePath = "AI/Run Graph";
            title = "Run Graph";

            FileField = new ObjectField();
            FileField.objectType = typeof(AIGraphDataContainer);
            mainContainer.Add(FileField);

            OutputPort.RemoveFromHierarchy();
            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(AIAction));
            OutputPort.portName = "AI Action";
            outputContainer.Add(OutputPort);
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            FileField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }

        public override NodeData Save()
        {
            var data = base.Save();
            if (FileField.value != null)
            {
                data.raw.SetToPairs(AIGraphFileKey, FileField.value);
            }
            return data;
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(AIGraphFileKey, out UnityEngine.Object file))
            {
                FileField.value = file;
            }
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            base.Execute(input);
            ResultOfAIGraph = AIGraphView.Execute(input);
            input.OutPort = OutputPort;
            return input;
        }
    }
}