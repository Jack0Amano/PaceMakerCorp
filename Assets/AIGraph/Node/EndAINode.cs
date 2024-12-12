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

namespace AIGraph.Nodes
{
    /// <summary>
    /// Eventの終了をStoryGraphに通知する
    /// </summary>
    public class EndAINode : ProcessNode
    {

        public readonly static string EndAINameKey = "EndAINameKey";
        public readonly static string EndAIKey = "EndAIKey";
        public string EndEventID;

        public EndAINode() : base()
        {
            title = "End AI";
            NodePath = "End AI";

            Guid guid = System.Guid.NewGuid();
            EndEventID = guid.ToString();

            OutputPort.RemoveFromHierarchy();
            InputPort.RemoveFromHierarchy();
            InputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(AIAction));
            InputPort.portName = "AI Action";
            inputContainer.Add(InputPort);
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            base.Execute(input);
            input.OutPort = null;
            AIAction = ((ProcessNode)InputPort.ConnectedNodes[0]).AIAction;
            return input;
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            data.raw.GetFromPairs(EndAIKey, out string id);
            EndEventID = id;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(EndAIKey, EndEventID);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
        }
    }
}