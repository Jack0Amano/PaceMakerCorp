using EventGraph.Editor;
using UnityEditor.Experimental.GraphView;
using StoryGraph.Nodes.Parts;
using System.Collections.Generic;

namespace StoryGraph.Nodes
{

    public abstract class ProcessNode : SampleNode
    {
        public Parts.CustomPort InputPort;
        public List<CustomPort> OutputPorts = new List<CustomPort>();

        public ProcessNode()
        {
            InputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(CustomPort));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);
        }

        /// <summary>
        /// Nodeを再生
        /// </summary>
        /// <param name="endNodeName">Eventが終了した際に返されるEndEventNodeに登録する固有の名称</param>
        /// <returns></returns>
        public abstract CustomPort FindOutputPortFromEndEventName(string endNodeName);

    }
}