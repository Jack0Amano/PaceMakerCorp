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
        /// Node‚ğÄ¶
        /// </summary>
        /// <param name="endNodeName">Event‚ªI—¹‚µ‚½Û‚É•Ô‚³‚ê‚éEndEventNode‚É“o˜^‚·‚éŒÅ—L‚Ì–¼Ì</param>
        /// <returns></returns>
        public abstract CustomPort FindOutputPortFromEndEventName(string endNodeName);

    }
}