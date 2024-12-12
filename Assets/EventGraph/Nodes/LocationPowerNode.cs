using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;
using EventGraph.Editor;
using UnityEditor.UIElements;
using UnityEditor;
using EventGraph.InOut;
using System.Linq;
using System.Collections.Generic;

namespace EventGraph.Nodes
{
    /// <summary>
    /// LocationのSpawnSquadの自動回復の頻度を調整するNode
    /// </summary>
    public class LocationPowerNode : ProcessNode
    {

        private readonly Slider HourSlider;
        private readonly TextField LocationIDField;

        private readonly string HourToRecoverPowerKey = "OutputportsCountKey";
        private readonly string LocationIDKey = "LocationIDofRecoverPowerKey";

        public LocationPowerNode(): base()
        {
            title = "Location Power";
            NodePath = "Process/Location Power";

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/LocationPowerNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            LocationIDField = container.Q<TextField>();
            HourSlider = container.Q<Slider>();
        }

        public override EventOutput Execute(EventInput eventInput)
        {
            return new InOut.LocationEventOutput(this, Guid, OutputPort)
            {
                LocationID = LocationIDField.value,
                HourToRecoverPower = HourSlider.value
            };
        }

        public override void Load(NodeData data)
        {
            base.Load(data);

            if (data.raw.GetFromPairs(HourToRecoverPowerKey, out float value))
                HourSlider.value = value;
            if (data.raw.GetFromPairs(LocationIDKey, out string locationID))
                LocationIDField.value = locationID;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(HourToRecoverPowerKey, (float)HourSlider.value);
            data.raw.SetToPairs(LocationIDKey, (string)LocationIDField.value);
            return data;
        }
    }
}