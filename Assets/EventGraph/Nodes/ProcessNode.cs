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
        /// Node���Đ�
        /// </summary>
        /// <returns></returns>
        public abstract InOut.EventOutput Execute(InOut.EventInput eventInput);

        /// <summary>
        /// Node��Trigger�Ŏ~�܂��Ă����Ԃ��炻�̏�����^����`�œr������Đ����� ��~����Node�݂̂Ŏ��� (WaitEvent, ImageWindow�Ƃ�...) 
        /// </summary>
        /// <param name="eventInput">Node���Đ�����ۂ̓���</param>
        /// <returns>Node�����s����Ă��N�U���Ȃ������ꍇNull</returns>
        public virtual InOut.EventOutput ExecuteFromMiddle(InOut.EventInput eventInput)
        {
            return null;
        }
    }
}