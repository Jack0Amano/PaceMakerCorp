using UnityEditor.Experimental.GraphView;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using StoryGraph.Editor;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace StoryGraph.Nodes
{

    public abstract class SampleNode : Node
    {
        public string Guid;
        public Color Color;
        public bool IsEntryPoint;

        public string NodePath = "";

        protected Action<SampleNode> RegisterAnyValueChangedCallback;

        public string DebugText;

        public SampleNode()
        {
            // Panel��Enable�ɂȂ����Ƃ�
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                // �e��o�^���ꂽ�|�[�g�̐F������
                outputContainer.Query<Port>().ToList().ForEach(p => SetPortColorFromType(p));
                inputContainer.Query<Port>().ToList().ForEach(p => SetPortColorFromType(p));
            });
        }

        private void SetPortColorFromType(Port port)
        {
        }

        public static string GetNodePath()
        {
            return nameof(SampleNode).Replace("Node", "");
        }

        /// <summary>
        /// Node�̊J�{�^��
        /// </summary>
        protected override void ToggleCollapse()
        {
            base.ToggleCollapse();
        }

        /// <summary>
        /// node����field�ɉ��炩��value�̕ύX���s��ꂽ�ۂɌĂяo�����߂̓o�^ *�ʏ�field��RegisterAnyValueChangedCallback��action��o�^����*
        /// </summary>
        /// <param name="action"></param>
        public virtual void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            RegisterAnyValueChangedCallback = action;
        }

        // NOTE Cast���Ă����g�͎����Ȃ� ������Init����Simple���̎g�p����UIElement��\�ߏ��������Ēu���Ȃ���΂Ȃ�Ȃ�
        /// <summary>
        /// NodeData����f�[�^��ǂݍ��݊eUIElement��value�ɔz�u����
        /// </summary>
        /// <param name="data"></param>
        public virtual void Load(Editor.NodeData data)
        {
            SetPosition(new Rect(data.Position, data.Size));

            Guid = data.Guid;
            IsEntryPoint = data.IsEntryPoint;
            expanded = data.Expanded;
        }

        /// <summary>
        /// NodeData�̎��f�[�^Raw��Node�̕ۑ����ׂ�����Save����
        /// </summary>
        /// <param name="raw"></param>
        public virtual NodeData Save()
        {
            var data = new NodeData();
            data.Guid = Guid;
            data.Keyword = GetType().Name;
            data.IsEntryPoint = IsEntryPoint;
            data.Position = GetPosition().position;
            data.Size = GetPosition().size;
            data.Expanded = expanded;



            return data;
        }
    }
}