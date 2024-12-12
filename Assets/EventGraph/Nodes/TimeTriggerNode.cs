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

namespace EventGraph.Nodes.Trigger
{
    public class TimeTriggerNode : SampleTriggerNode
    {
        public DateTime DateTime
        { 
            get{
                return new DateTime(CalenderFields["Year"].value,
                                    CalenderFields["Month"].value,
                                    CalenderFields["Day"].value,
                                    CalenderFields["Hour"].value,
                                    CalenderFields["Minute"].value,
                                    CalenderFields["Second"].value);
            } 
        }

        readonly EnumField TimingField;

        private readonly string TimingKey = "OwnItemIDKey";
        private readonly string TimeKey = "TimeKey";
        private readonly List<(string name, int max)> CalenderFieldNames = new List<(string, int)>()
        {
            ( "Year", 9999 ), ("Month", 12), ("Day", 31 ), ("Hour", 23 ), ("Minute", 59 ), ("Second", 59 )
        };
        private Dictionary<string, IntegerField> CalenderFields = new Dictionary<string, IntegerField>();

        public TimeTriggerNode() : base()
        {
            title = "Time Trigger";
            NodePath = "Trigger/Time Trigger";

            var outputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputContainer.Add(outputPort);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/TimeTriggerNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            TimingField = container.Q<EnumField>();
            TimingField.Init(TimingType.After);

            CalenderFieldNames.ForEach(n =>
            {
                var field = container.Q<IntegerField>(n.name);
                field.RegisterValueChangedCallback(evt =>
                {
                    if (field.value < 0)
                        field.value = 0;
                    else if (field.value > n.max)
                        field.value = n.max;
                });
                CalenderFields[n.name] = field;
            });
        }

        public override void Load(NodeData data)
        {
            if (data.raw.GetFromPairs(TimingKey, out string strType))
                TimingField.value = ((TimingType[])Enum.GetValues(typeof(TimingType))).ToList().FirstOrDefault(t => t.ToString() == strType);

            CalenderFields.ToList().ForEach(p =>
            {
                if (data.raw.GetFromPairs($"{p.Key}{TimeKey}", out float value))
                    p.Value.value = (int)value;
            });

            base.Load(data);
        }

        public override NodeData Save()
        {
            var node = base.Save();
            node.raw.SetToPairs(TimingKey, TimingField.value.ToString());

            CalenderFields.ToList().ForEach(p =>
            {
                node.raw.SetToPairs($"{p.Key}{TimeKey}", (float)p.Value.value);
            });

            return node;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            TimingField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            CalenderFields.ToList().ForEach(p => p.Value.RegisterValueChangedCallback(evt => action?.Invoke(this)));
        }

        public override bool Check(InOut.EventInput input)
        {
            if ((TimingType)TimingField.value == TimingType.After)
                return DateTime < input.DateTime;
            else
                return input.DateTime < DateTime;
        }

        /// <summary>
        /// 時間のトリガーを判別するタイプ
        /// </summary>
        enum TimingType
        {
            After,
            Before,
        }
    }
}