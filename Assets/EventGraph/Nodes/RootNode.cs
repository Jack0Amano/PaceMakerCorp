using EventGraph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using EventGraph.InOut;

namespace EventGraph.Nodes
{
    public class RootNode : ProcessNode
    {
        /// <summary>
        /// Event�����ʂ���ID
        /// </summary>
        public string EventID { get => EventIdField.value; }

        private readonly TextField EventIdField;

        private readonly string EventIDKey = "EventID";

        public RootNode() : base()
        {
            
            title = "Root";
            IsEntryPoint = true;

            capabilities -= Capabilities.Deletable;

            EventIdField = new TextField();
            EventIdField.style.minWidth = 100;
            Add(EventIdField);

            inputContainer.Remove(InputPort);
        }

        public override InOut.EventOutput Execute(EventInput eventInput)
        {
            return new InOut.EventOutput(this, Guid, OutputPort);
        }

        // Cast���Ă����g�͎����Ȃ� ������Init����Simple���̎g�p����UIElement��\�ߏ��������Ēu���Ȃ���΂Ȃ�Ȃ�
        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(EventIDKey, out string id))
                EventIdField.value = id;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(EventIDKey, EventIdField.value);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            EventIdField.RegisterValueChangedCallback(e => action?.Invoke(this));
        }
    }
}