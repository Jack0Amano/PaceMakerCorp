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
        public string EventID { get => eventIdField.value; }

        private readonly TextField eventIdField;

        private readonly string eventIDKey = "EventID";
        public const string REPEATABLE_KEY = "Repeatable";

        /// <summary>
        /// �J��Ԃ��\��Event����Toggle
        /// </summary>
        private readonly Toggle repeatableToggle;

        public RootNode() : base()
        {
            
            title = "Root";
            IsEntryPoint = true;

            capabilities -= Capabilities.Deletable;

            eventIdField = new TextField();
            eventIdField.style.minWidth = 100;
            Add(eventIdField);

            repeatableToggle = new Toggle("Repeatable");
            repeatableToggle.value = false;
            Add(repeatableToggle);

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
            if (data.Raw.GetFromPairs(eventIDKey, out string id))
                eventIdField.value = id;
            if (data.Raw.GetFromPairs(REPEATABLE_KEY, out float toggle))
                repeatableToggle.value = toggle == 1;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.Raw.SetToPairs(eventIDKey, eventIdField.value);
            data.Raw.SetToPairs(REPEATABLE_KEY, repeatableToggle.value ? 1f : 0f);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            eventIdField.RegisterValueChangedCallback(e => action?.Invoke(this));
            repeatableToggle.RegisterValueChangedCallback(e => action?.Invoke(this));
        }
    }
}