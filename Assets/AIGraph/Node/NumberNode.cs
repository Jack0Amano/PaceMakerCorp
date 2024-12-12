using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using AIGraph.Nodes.Parts;
using AIGraph.Editor;
using AIGraph.InOut;
using UnityEditor;
using UnityEditor.UIElements;
using Tactics.Character;
using Tactics.Map;
using static Utility;

namespace AIGraph.Nodes
{
    public class NumberNode : SampleNode
    {
        private readonly List<(string name, Type type)> SwitchChoices = new List<(string, Type)>
        {
            ("Int", typeof(int)),
            ("Float", typeof(float))
        };

        public CustomPort OutputPort;
        readonly DropdownField DropdownField;
        readonly IntegerField IntField;
        readonly FloatField FloatField;

        const string SwitchChoiceIndexKey = "ValueChoiceIndexKey";
        const string IntValueKey = "ValueIntKey";
        const string FloatValueKey = "FloatValueKey";

        public NumberNode() : base()
        {
            title = "Number";
            NodePath = "Value/Number";

            DropdownField = new DropdownField();
            DropdownField.choices = SwitchChoices.ConvertAll(c => c.name);
            DropdownField.RegisterValueChangedCallback(e => DropdownFieldValueChanged());
            DropdownField.index = 0;
            mainContainer.Add(DropdownField);

            IntField = new IntegerField();
            FloatField = new FloatField();

            OutputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(int));
            outputContainer.Add(OutputPort);

            SetMode(DropdownField.index);
        }

        void DropdownFieldValueChanged()
        {
            SetMode(DropdownField.index);
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(SwitchChoiceIndexKey, DropdownField.index);
            data.raw.SetToPairs(IntValueKey, IntField.value);
            data.raw.SetToPairs(FloatValueKey, FloatField.value);
            return data;
        }

        public override void Load(NodeData data)
        {
            if (data.raw.GetFromPairs(SwitchChoiceIndexKey, out int id)) 
                DropdownField.index = id;
                
            if (data.raw.GetFromPairs(IntValueKey, out int intValue))
                IntField.value = intValue;
            
            if (data.raw.GetFromPairs(FloatValueKey, out float floatValue))
                FloatField.value = floatValue;

            Print(DropdownField.index);
            SetMode(DropdownField.index);

            base.Load(data);
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            DropdownField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            IntField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            FloatField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            base.RegisterAnyValueChanged(action);
        }

        private void SetMode(int index)
        {
            if (!SwitchChoices.IndexAt_Bug(index, out var selected))
                return;
            if (selected.name == "Int")
            {
                FloatField.RemoveFromHierarchy();
                mainContainer.Add(IntField);
            }
            else if (selected.name == "Float")
            {
                IntField.RemoveFromHierarchy();
                mainContainer.Add(FloatField);
            }
            OutputPort.portType = selected.type;
            OutputPort.portName = selected.name;
        }
    }
}