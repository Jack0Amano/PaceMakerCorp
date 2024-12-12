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
            // PanelがEnableになったとき
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                // 各種登録されたポートの色を決定
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
        /// Nodeの開閉ボタン
        /// </summary>
        protected override void ToggleCollapse()
        {
            base.ToggleCollapse();
        }

        /// <summary>
        /// node内のfieldに何らかのvalueの変更が行われた際に呼び出すための登録 *通常fieldのRegisterAnyValueChangedCallbackにactionを登録する*
        /// </summary>
        /// <param name="action"></param>
        public virtual void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            RegisterAnyValueChangedCallback = action;
        }

        // NOTE Castしても中身は失われない ただしInit内でSimple内の使用するUIElementを予め初期化して置かなければならない
        /// <summary>
        /// NodeDataからデータを読み込み各UIElementのvalueに配置する
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
        /// NodeDataの実データRawにNodeの保存すべき情報をSaveする
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