using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;
using EventGraph.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using System.Linq;

namespace EventGraph.Nodes
{
    public class TextNode : SampleNode
    {
        public string Text { get { return TextField.value; } }

        private readonly TextField TextField;
        private readonly EnumField EnumField;

        private readonly string TextAccessKey = "TextNodeTextKey";
        private readonly string TextTypeKey = "TextTypeKey";

        public TextNode() : base()
        {
            title = "Text";
            NodePath = "Value/Text";

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/TextNode.uxml");
            var content = asset.Instantiate();
            mainContainer.Add(content);

            var outputPort = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(string));
            outputContainer.Add(outputPort);

            TextField = content.Q<TextField>();
            TextField.multiline = true;
            TextField.RegisterCallback<FocusInEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.On; });
            TextField.RegisterCallback<FocusOutEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.Auto; });
            TextField.style.whiteSpace = WhiteSpace.Normal;
            TextField.style.textOverflow = TextOverflow.Ellipsis;
            TextField.style.unityTextOverflowPosition = TextOverflowPosition.End;
            TextField.style.overflow = Overflow.Hidden;

            

            EnumField = content.Q<EnumField>();
            EnumField.Init(TextType.Debug);
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            TextField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback?.Invoke(this));

            if (data.raw.GetFromPairs(TextAccessKey, out string value))
                TextField.value = value;
            data.raw.GetFromPairs(TextTypeKey, out string typeStr);
            EnumField.value = ((TextType[])Enum.GetValues(typeof(TextType))).ToList().FirstOrDefault(t => t.ToString() == typeStr);
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(TextAccessKey, TextField.value);
            data.raw.SetToPairs(TextTypeKey, EnumField.value);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            TextField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback?.Invoke(this));
            EnumField.RegisterValueChangedCallback(e => RegisterAnyValueChangedCallback?.Invoke(this));
        }
    }

    /// <summary>
    /// RootNodeÇ∆Ç©Ç≈égÇ§EventÇÃê‡ñæÇ∆Ç©
    /// </summary>
    enum TextType
    {
        Debug,
    }
}