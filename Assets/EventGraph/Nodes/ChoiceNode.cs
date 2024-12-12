using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;
using EventGraph.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using EventGraph.Nodes.Parts;

namespace EventGraph.Nodes
{
    public class ChoiceNode : SampleNode
    {
        /// <summary>
        /// “ü—Í‚³‚ê‚½value‚ðŽæ“¾
        /// </summary>
        public List<string> Texts
        {
            get
            {
                var outputs = textFields.ConvertAll(f =>f.main.value);
                if ((outputs.Last() == null || outputs.Last().Length == 0))
                {
                    outputs.RemoveAt(outputs.Count - 1);
                }
                return outputs;
            }
        }

        private readonly List<(TextField main, Label label, GroupBox box)> textFields = new List<(TextField, Label, GroupBox)>();
        private readonly VisualTreeAsset textFieldAsset;

        public Action<ChoiceNode> OnCountOfChoicesIsChaged;

        private readonly string SelectiveNodeKey = "ChoiceNodeKey";

        public ChoiceNode() : base()
        {
            title = "Sentence Choices";
            NodePath = "Value/Sentence Choices";

            var outputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(List<string>));
            outputPort.portName = "Out";
            outputContainer.Add(outputPort);

            textFieldAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/SentenceChoiceNode.uxml");


            if (textFields.Count == 0)
                MakeTextField(0);
        }

        public override void HandleEvent(EventBase evt)
        {
            base.HandleEvent(evt);

            // “ü—Í‚µ‚½Û‚ÉV‚½‚ÉtextField‚ðì¬‚·‚é“®ì
            if (textFields.Count == 0)
                return;
            var last = textFields.Last();

            if (last != default)
            {
                if (last.main.value == null)
                    return;

                if (last.main.value.Length != 0)
                {
                    MakeTextField(textFields.Count);
                    OnCountOfChoicesIsChaged?.Invoke(this);
                }
                else
                {
                    var index = textFields.Count - 2;
                    if (index >= 0 && textFields[index].main.value.Length == 0)
                    {
                        mainContainer.Remove(last.box);
                        textFields.Remove(last);
                        OnCountOfChoicesIsChaged?.Invoke(this);
                    }
                }
            }
        }

        private (TextField field, Label label) MakeTextField(int index)
        {
            var container = textFieldAsset.Instantiate();
            var box = container.Q<GroupBox>("SentenceBox");

            var mainField = container.Q<TextField>("MainField");
            mainField.multiline = true;
            

            mainField.style.whiteSpace = WhiteSpace.Normal;
            mainField.style.textOverflow = TextOverflow.Ellipsis;
            mainField.style.unityTextOverflowPosition = TextOverflowPosition.End;
            mainField.style.overflow = Overflow.Hidden;

            var label = container.Q<Label>();
            label.text = $"No.{index}";
            mainField.RegisterCallback<FocusInEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.On; });
            mainField.RegisterCallback<FocusOutEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.Auto; });

            textFields.Add((mainField, label, box));

            mainField.RegisterValueChangedCallback((e) => RegisterAnyValueChangedCallback?.Invoke(this));

            mainContainer.Add(box);

            return (mainField, label);
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            textFields.ForEach(f =>
            {
                mainContainer.Remove(f.box);
            });
            textFields.Clear();

            var index = 0;
            for (var i = 0; i < data.raw.Count; i++)
            {
                if (data.raw.GetFromPairs($"{SelectiveNodeKey}{i}", out string value))
                {
                    var (field, label) = MakeTextField(index);
                    field.value = value;
                    index++;
                }
            }


            if (textFields.Count == 0)
                MakeTextField(0);
        }

        public override NodeData Save()
        {
            var node = base.Save();

            var mainDic = Texts.Select((text, index) => new { index, text }).ToDictionary(n => $"{SelectiveNodeKey}{n.index}", n => n.text);
            node.raw.SetDictionary(mainDic);

            return node;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            textFields.ForEach(f =>
            {
                f.main.RegisterValueChangedCallback((e) => RegisterAnyValueChangedCallback?.Invoke(this));
            });
        }
    }
}
