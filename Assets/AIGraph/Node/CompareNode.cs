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
    public class CompareNode : ProcessNode
    {
        readonly List<string> OperatorDropdownItems = new List<string>()
        {
            "==",
            "<=",
            ">=",
            "<",
            ">",
            "!="
        };
        readonly List<string> ValueDropdownItems = new List<string>
        {
            "Level",
        };
        readonly DropdownField OperatorDropdownField;
        readonly DropdownField ValueDropdownField;
        readonly FloatField FloatField;

        public readonly CustomPort TruePort;
        public readonly CustomPort FalsePort;

        public CompareNode(): base()
        {
            title = "Compare";
            NodePath = "Logic/Compare";

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AIGraph/Node/Style/CompareNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            OperatorDropdownField = container.Q<DropdownField>("OperatorDropdownField");
            OperatorDropdownField.choices = OperatorDropdownItems;

            ValueDropdownField = container.Q<DropdownField>("ValueDropdownField");
            ValueDropdownField.choices = ValueDropdownItems;

            FloatField = container.Q<FloatField>();

            OutputPort.RemoveFromHierarchy();
            TruePort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(EnvironmentData));
            TruePort.portName = "True";
            outputContainer.Add(TruePort);
            FalsePort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(EnvironmentData));
            FalsePort.portName = "False";
            outputContainer.Add(FalsePort);
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            OperatorDropdownField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            ValueDropdownField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            FloatField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(nameof(OperatorDropdownField), OperatorDropdownField.index);
            data.raw.SetToPairs(nameof(ValueDropdownField), ValueDropdownField.index);
            data.raw.SetToPairs(nameof(FloatField), FloatField.value);
            return data;
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(nameof(OperatorDropdownField), out int index))
                OperatorDropdownField.index = index;
            if (data.raw.GetFromPairs(nameof(ValueDropdownField), out int index2))
                ValueDropdownField.index = index2;
            if (data.raw.GetFromPairs(nameof(FloatField), out float value))
                FloatField.value = value;
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            input.OutPort = FalsePort;
            var testValue = 0f;
            if (ValueDropdownField.index == 0)
                testValue = (float)MyUnitController.CurrentParameter.Data.Data.Level;

            if (ValueDropdownField.index == 0)
            {
                if (OperatorDropdownField.index == 0)
                    input.OutPort = FloatField.value == testValue ? TruePort : FalsePort;
                else if (OperatorDropdownField.index == 1)
                    // value <= testValue
                    input.OutPort = FloatField.value <= testValue ? TruePort : FalsePort;
                else if (OperatorDropdownField.index == 2)
                    // value >= testValue
                    input.OutPort = FloatField.value >= testValue ? TruePort : FalsePort;
                else if (OperatorDropdownField.index == 3)
                    // value < testValue
                    input.OutPort = FloatField.value < testValue ? TruePort : FalsePort;
                else if (OperatorDropdownField.index == 4)
                    // value > testValue
                    input.OutPort = FloatField.value > testValue ? TruePort : FalsePort;
                else if (OperatorDropdownField.index == 5)
                    // value != testValue
                    input.OutPort = FloatField.value != testValue ? TruePort : FalsePort;
            }

            return input;
        }
    }
}