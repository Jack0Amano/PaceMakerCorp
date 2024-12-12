using EventGraph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using EventGraph.Nodes.Parts;
using EventGraph.InOut;
using System.Linq;

namespace EventGraph.Nodes
{
    /// <summary>
    /// Trigger1���K�������ꍇOutputPort1��2�̏ꍇOutputPort2���o�͂���� (�K�����Ȃ��ꍇ�ҋ@)
    /// </summary>
    public class WaitEventNode : ProcessNode
    {

        readonly CustomPort TriggerPort1;
        readonly CustomPort TriggerPort2;
        readonly EnumField TypeField;
        readonly CustomPort OutputPort2;

        private readonly string LogicalOperationTypeKey = "LogicalOperationTypeKey";

        public WaitEventNode() : base()
        {
            title = "Wait Event";
            NodePath = "Process/Wait Event";

            OutputPort.portName = "Out 1";

            OutputPort2 = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(CustomPort));
            OutputPort2.portName = "Out 2";
            outputContainer.Add(OutputPort2);

            TriggerPort1 = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            TriggerPort1.portName = "Trigger 1";
            inputContainer.Add(TriggerPort1);

            TriggerPort2 = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            TriggerPort2.portName = "Trigger 2";
            inputContainer.Add(TriggerPort2);

            TypeField = new EnumField();
            TypeField.Init(LogicalOperation.All);
            mainContainer.Add(TypeField);
        }

        public override EventOutput Execute(EventInput eventInput)
        {
            var port = CheckTriggers(eventInput);
            return new EventOutput(this, Guid, port);
        }

        public override EventOutput ExecuteFromMiddle(EventInput eventInput)
        {
            var port = CheckTriggers(eventInput);
            return port == null ? null : new EventOutput(this, Guid, port);
        }

        /// <summary>
        /// �ڑ�����Ă���Node�̏������m�F����
        /// </summary>
        /// <returns></returns>
        private CustomPort CheckTriggers(EventInput eventInput)
        {

            if ((LogicalOperation)TypeField.value == LogicalOperation.All)
            {
                if (TriggerPort1.connections.ToList().Find(e =>
                {
                    if (e.output.node is Trigger.SampleTriggerNode node)
                        return !node.Check(eventInput);
                    return false;
                }) == null)
                    return OutputPort;

                if (TriggerPort2.connections.ToList().Find(e =>
                {
                    if (e.output.node is Trigger.SampleTriggerNode node)
                        return !node.Check(eventInput);
                    return false;
                }) == null)
                    return OutputPort2;
            }
            else
            {
                if (TriggerPort1.connections.ToList().Find(e =>
                {
                    if (e.output.node is Trigger.SampleTriggerNode node)
                        return node.Check(eventInput);
                    return false;
                }) != null)
                    return OutputPort;
                if (TriggerPort2.connections.ToList().Find(e =>
                {
                    if (e.output.node is Trigger.SampleTriggerNode node)
                        return node.Check(eventInput);
                    return false;
                }) != null)
                    return OutputPort;
            }

            return null;
        }

        // Cast���Ă����g�͎����Ȃ� ������Init����Simple���̎g�p����UIElement��\�ߏ��������Ēu���Ȃ���΂Ȃ�Ȃ�
        public override void Load(NodeData data)
        {
            base.Load(data);
            data.Raw.SetToPairs(LogicalOperationTypeKey, TypeField.value.ToString());
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.Raw.GetFromPairs(LogicalOperationTypeKey, out string strType);
            TypeField.value = ((LogicalOperation[])Enum.GetValues(typeof(LogicalOperation))).ToList().FirstOrDefault(t => t.ToString() == strType);

            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            // objectField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback(this));
            TypeField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback(this));
        }

        /// <summary>
        /// Trigger�̏����̘_��
        /// </summary>
        public enum LogicalOperation
        {
            /// <summary>
            /// �ڑ�����Ă��邷�ׂĂ�TriggerNode��True�̏ꍇ
            /// </summary>
            All,
            /// <summary>
            /// �ڑ�����Ă���ǂꂩ��TriggerNode��True�̏ꍇ
            /// </summary>
            Any
        }
    }
}