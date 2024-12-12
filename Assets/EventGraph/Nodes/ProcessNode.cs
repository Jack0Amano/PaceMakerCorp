using EventGraph.Editor;
using UnityEditor.Experimental.GraphView;
using EventGraph.Nodes.Parts;

namespace EventGraph.Nodes
{

    public abstract class ProcessNode : SampleNode
    {
        public Parts.CustomPort InputPort;
        public Parts.CustomPort OutputPort;

        public ProcessNode()
        {
            InputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(CustomPort));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(CustomPort));
            OutputPort.portName = "Out";
            outputContainer.Add(OutputPort);
        }

        /// <summary>
        /// Node‚ğÄ¶
        /// </summary>
        /// <returns></returns>
        public abstract InOut.EventOutput Execute(InOut.EventInput eventInput);

        /// <summary>
        /// Node‚ªTrigger‚Å~‚Ü‚Á‚Ä‚¢‚éó‘Ô‚©‚ç‚»‚ÌğŒ‚ğ—^‚¦‚éŒ`‚Å“r’†‚©‚çÄ¶‚·‚é ’â~‚·‚éNode‚Ì‚İ‚ÅÀ‘• (WaitEvent, ImageWindow‚Æ‚©...) 
        /// </summary>
        /// <param name="eventInput">Node‚ğÄ¶‚·‚éÛ‚Ì“ü—Í</param>
        /// <returns>Node‚ªÀs‚³‚ê‚Ä‚àNU‚µ‚È‚©‚Á‚½ê‡Null</returns>
        public virtual InOut.EventOutput ExecuteFromMiddle(InOut.EventInput eventInput)
        {
            return null;
        }
    }
}