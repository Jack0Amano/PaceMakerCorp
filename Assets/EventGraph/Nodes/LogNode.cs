using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using System;
using EventGraph.InOut;

namespace EventGraph.Nodes
{
    public class LogNode : ProcessNode
    {

        private Parts.CustomPort inputString;

        public LogNode() : base()
        {
            title = "Log";

            inputString = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(object));
            inputContainer.Add(inputString);
        }

        public override InOut.EventOutput Execute(EventInput eventInput)
        {
            var edge = inputString.connections.FirstOrDefault();
            var node = edge.output.node as TextNode;

            if (node == null) return null;

            Debug.Log(node.DebugText);

            return null;
        }

    }
}