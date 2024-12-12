using EventGraph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using EventGraph.Nodes.Parts;
using EventGraph.InOut;

namespace EventGraph.Nodes
{
    public class EnemySpawnNode : ProcessNode
    {
        readonly Label SpawnRateLabel;
        readonly TextField SpawnSquadIDField;
        readonly TextField SpawnIDField;
        readonly Slider SpawnRateSlider;

        private readonly string SquadIDKey = "EnemySpawnIDKey";
        private readonly string SpawnIDKey = "SpawnIDKey";
        private readonly string RateKey = "EnemySpawnRateKey";

        public EnemySpawnNode() : base()
        {

            title = "EnemySpawnNode";
            NodePath = "Process/Enemy Spawn";

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/EnemySpawnNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            SpawnRateLabel = container.Q<Label>("SpawnRate");
            SpawnSquadIDField = container.Q<TextField>("SpawnSquadIDField");
            SpawnIDField = container.Q<TextField>("SpawnIDField");
            SpawnRateSlider = container.Q<Slider>();

            SpawnRateLabel.text = $"Spawn Rate: {SpawnRateSlider.value}";
            SpawnRateSlider.RegisterValueChangedCallback(evt => SpawnRateLabel.text = $"Spawn Rate: {SpawnRateSlider.value}");
        }

        public override InOut.EventOutput Execute(EventInput eventInput)
        {
            return new InOut.SpawnEventOutput(this, Guid, OutputPort)
            {
                spawnRate = SpawnRateSlider.value,
                squadID = SpawnSquadIDField.value,
            };
        }

        // Castしても中身は失われない ただしInit内でSimple内の使用するUIElementを予め初期化して置かなければならない
        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(SquadIDKey, out string id))
                SpawnSquadIDField.value = id;
            if (data.raw.GetFromPairs(RateKey, out float rate))
                SpawnRateSlider.value = rate;
            if (data.raw.GetFromPairs(SpawnIDKey, out string spawnID))
                SpawnIDField.value = spawnID;

            SpawnRateLabel.text = $"Spawn Rate: {SpawnRateSlider.value}";
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(SquadIDKey, SpawnSquadIDField.value);
            data.raw.SetToPairs(RateKey, SpawnRateSlider.value);
            data.raw.SetToPairs(SpawnIDKey, SpawnIDField.value);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            SpawnSquadIDField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            SpawnRateSlider.RegisterValueChangedCallback(evt => action?.Invoke(this));
            SpawnIDField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }
    }
}