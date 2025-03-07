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
        /// Nodeを再生
        /// </summary>
        /// <returns></returns>
        public abstract InOut.EventOutput Execute(InOut.EventInput eventInput);

        /// <summary>
        /// NodeがTriggerで止まっている状態からその条件を与える形で途中から再生する 停止するNodeのみで実装 (WaitEvent, ImageWindowとか...) 
        /// </summary>
        /// <param name="eventInput">Nodeを再生する際の入力</param>
        /// <returns>Nodeが実行されても侵攻しなかった場合Null</returns>
        public virtual InOut.EventOutput ExecuteFromMiddle(InOut.EventInput eventInput)
        {
            return null;
        }
    }
}