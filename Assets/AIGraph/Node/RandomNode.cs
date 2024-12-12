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

    public class RandomNode : ProcessNode
    {

        readonly SliderInt SliderInt;
        readonly IntegerField IntegerField;

        public readonly CustomPort TruePort;
        public readonly CustomPort FalsePort;

        public RandomNode() : base()
        {
            title = "Random";
            NodePath = "Logic/Random";

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/AIGraph/Node/Style/RandomNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            IntegerField = mainContainer.Q<IntegerField>();
            IntegerField.RegisterValueChangedCallback(evt =>
            {
                if (IntegerField.value < 0)
                    IntegerField.value = 0;
                else if (IntegerField.value > 100)
                    IntegerField.value = 100;
                SliderInt.value = IntegerField.value;
            });

            SliderInt = mainContainer.Q<SliderInt>();
            SliderInt.RegisterValueChangedCallback(evt =>
            {
                IntegerField.value = SliderInt.value;

            });

            OutputPort.RemoveFromHierarchy();
            TruePort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(EnvironmentData));
            TruePort.portName = "True";
            outputContainer.Add(TruePort);
            FalsePort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(EnvironmentData));
            FalsePort.portName = "False";
            outputContainer.Add(FalsePort);
        }
        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(nameof(SliderInt), out int value))
                SliderInt.value = value;
            if (data.raw.GetFromPairs(nameof(IntegerField), out int fieldValue))
                IntegerField.value = fieldValue;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(nameof(SliderInt), SliderInt.value);
            data.raw.SetToPairs(nameof(IntegerField), IntegerField.value);
            return data;
        }

        public override EnvironmentData Execute(EnvironmentData input)
        {
            base.Execute(input);

            float fPercent = (float)SliderInt.value / 100f;

            float fProbabilityRate = UnityEngine.Random.value;

            if (fPercent == 1f && fProbabilityRate == fPercent)
                input.OutPort = TruePort;
            else if (fProbabilityRate < fPercent)
                input.OutPort = TruePort;
            else
                input.OutPort = FalsePort;

            return input;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            IntegerField.RegisterValueChangedCallback(evt =>
            {
                action?.Invoke(this);
            });
            SliderInt.RegisterValueChangedCallback(evt => 
            {
                action?.Invoke(this); 
            });
        }
    }
}