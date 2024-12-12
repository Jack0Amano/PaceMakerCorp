using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;
using EventGraph.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace EventGraph.Nodes
{
    public class TextArrayNode : SampleNode
    {
        /// <summary>
        /// “ü—Í‚³‚ê‚½value‚ðŽæ“¾
        /// </summary>
        public List<(string from, string main)> Texts
        {
            get
            {
                var outputs = textFields.ConvertAll(f => (f.from.value, f.main.value));
                if ((outputs.Last().Item2 == null || outputs.Last().Item2.Length == 0) &&
                    (outputs.Last().Item1 == null || outputs.Last().Item1.Length == 0))
                {
                    outputs.RemoveAt(outputs.Count - 1);
                }
                return outputs;
            } 
        }

        private List<(TextField from, TextField main, GroupBox box)> textFields = new List<(TextField, TextField, GroupBox)>();
        private readonly VisualTreeAsset textFieldAsset;

        private readonly string TextArrayKey = "TextArrayKey";
        private readonly string TextArrayFromKey = "TextArratFromKey";

        public TextArrayNode() : base()
        {
            title = "Text Array";
            NodePath = "Value/Text Array";

            var outputPort = Parts.CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(List<(string, string)>));
            outputPort.portName = "Out";
            outputContainer.Add(outputPort);

            textFieldAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/TextArrayNode.uxml");
           

            if (textFields.Count == 0)
                MakeTextField();
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
                if (last.from.value == null || last.main.value == null)
                    return;

                if (last.from.value.Length != 0 || last.main.value.Length != 0)
                {
                    MakeTextField();
                }
                else
                {
                    var index = textFields.Count - 2;
                    if (index >= 0 && textFields[index].from.value.Length == 0 && textFields[index].main.value.Length == 0)
                    {
                        mainContainer.Remove(last.box);
                        textFields.Remove(last);
                    }
                }
            }
        }

        private (TextField from, TextField main) MakeTextField()
        {
            var container = textFieldAsset.Instantiate();
            var box = container.Q<GroupBox>("SentenceBox");

            var fromField = container.Q<TextField>("FromField");
            fromField.multiline = false;
            fromField.RegisterCallback<FocusInEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.On; });
            fromField.RegisterCallback<FocusOutEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.Auto; });

            var mainField = container.Q<TextField>("MainField");
            mainField.multiline = true;

            mainField.style.whiteSpace = WhiteSpace.Normal;
            mainField.style.textOverflow = TextOverflow.Ellipsis;
            mainField.style.unityTextOverflowPosition = TextOverflowPosition.End;
            mainField.style.overflow = Overflow.Hidden;


            mainField.RegisterCallback<FocusInEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.On; });
            mainField.RegisterCallback<FocusOutEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.Auto; });

            textFields.Add((fromField, mainField, box));

            fromField.RegisterValueChangedCallback((e) => RegisterAnyValueChangedCallback?.Invoke(this));
            mainField.RegisterValueChangedCallback((e) => RegisterAnyValueChangedCallback?.Invoke(this));

            mainContainer.Add(box);

            return (fromField, mainField) ;
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            textFields.ForEach(f => 
            {
                mainContainer.Remove(f.box);
            });
            textFields.Clear();

            for(var i=0; i<data.raw.Count; i++)
            {
                data.raw.GetFromPairs($"{TextArrayFromKey}{i}", out string fromValue);
                if ( data.raw.GetFromPairs($"{TextArrayKey}{i}", out string value))
                {
                    var fields = MakeTextField();
                    fields.main.value = value;
                    fields.from.value = fromValue;
                }
            }


            if (textFields.Count == 0)
                MakeTextField();
        }

        public override NodeData Save()
        {
            var node = base.Save();

            var fromDic = Texts.Select((text, index) => new { index, text }).ToDictionary(n => $"{TextArrayFromKey}{n.index}", n => n.text.from);

            var mainDic = Texts.Select((text, index) => new { index, text }).ToDictionary(n => $"{TextArrayKey}{n.index}", n => n.text.main);
            fromDic.ToList().ForEach(f => mainDic[f.Key] = f.Value);

            node.raw.SetDictionary(mainDic);

            return node;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            textFields.ForEach(f =>
            {
                f.main.RegisterValueChangedCallback((e) => RegisterAnyValueChangedCallback?.Invoke(this));
                f.from.RegisterValueChangedCallback((e) => RegisterAnyValueChangedCallback?.Invoke(this));
            });
        }
    }
}