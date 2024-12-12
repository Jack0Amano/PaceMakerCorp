using EventGraph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Linq;
using EventGraph.Nodes.Parts;
using EventGraph.Nodes.Trigger;

namespace EventGraph.Nodes
{
    /// <summary>
    /// 与えられたレートからランダムにbooleanを出力する
    /// </summary>
    public class RandomNode : SampleTriggerNode
    {
        private const string RATE_KEY = "rate";
        private readonly Slider rateSlider;
        private readonly Label label;

        public RandomNode() : base()
        {
            title = "Random";
            NodePath = "Trigger/Random";
            Description = "与えられたレートから\nランダムにbooleanを出力する";
            label = new Label("Rate: 0%");
            mainContainer.Add(label);
            rateSlider = new Slider(0, 1);
            rateSlider.RegisterValueChangedCallback(evt =>
            {
                label.text = $"Rate: {rateSlider.value * 100}%";
            });
            mainContainer.Add(rateSlider);

            var outputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputContainer.Add(outputPort);
        }

        override public bool Check(InOut.EventInput input)
        {
            return GameManager.Instance.RandomController.Probability(rateSlider.value);
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            rateSlider.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }

        public override NodeData Save()
        {
            var node = base.Save();
            node.raw.SetToPairs("rate", rateSlider.value);
            label.text = $"Rate: {rateSlider.value * 100}%";
            return node;
        }

        public override void Load(NodeData data)
        {
            if (data.raw.GetFromPairs(RATE_KEY, out float value))
                rateSlider.value = value;
            label.text = $"Rate: {rateSlider.value * 100}%";
            base.Load(data);
        }
    }
}
