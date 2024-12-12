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
                return new DateTime(int.Parse(calenderFields["Year"].value),
                                    int.Parse(calenderFields["Month"].value),
                                    int.Parse(calenderFields["Day"].value),
                                    int.Parse(calenderFields["Hour"].value),
                                    int.Parse(calenderFields["Minute"].value),
                                    int.Parse(calenderFields["Second"].value));
            } 
        }

        readonly EnumField timingField;

        private readonly string timingKey = "OwnItemIDKey";
        private readonly string timeKey = "TimeKey";
        private readonly List<(string name, int max)> calenderFieldNames = new List<(string, int)>()
        {
            ( "Year", 9999 ), ("Month", 12), ("Day", 31 ), ("Hour", 23 ), ("Minute", 59 ), ("Second", 59 )
        };
        private readonly Dictionary<string, TextField> calenderFields = new Dictionary<string, TextField>();

        //<summary>
        // Spanの場合TimingとNowDateTimeが同じであるとする誤差の許容値の秒数
        //</summary>
        private const int SPAN_TOLERANCE_SECONDS = 5;

        /// <summary>
        /// TimingTypeがSpanの場合前回Checkしてtrueを返した時間
        /// </summary>
        private DateTime lastSpanTime;

        public TimeTriggerNode() : base()
        {
            title = "Time Trigger";
            NodePath = "Trigger/Time Trigger";
            Description = "時間に達した場合Trigger\n" +
                "Before enum: Time以前の場合\n" +
                "After enum : Time移行の場合\n" +
                "Span enum  : *を入力した場合その数値は任意";

            var outputPort = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputContainer.Add(outputPort);

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/EventGraph/Nodes/Style/TimeTriggerNode.uxml");
            var container = asset.Instantiate();
            mainContainer.Add(container);

            timingField = container.Q<EnumField>();
            timingField.Init(TimingType.After);
            timingField.RegisterCallback<ChangeEvent<Enum>>(evt =>
            {
            });

            calenderFieldNames.ForEach(n =>
            {
                var field = container.Q<TextField>(n.name);
                field.RegisterValueChangedCallback(evt =>
                {
                    // fieldに数字以外が入力された場合1にする
                    if (!int.TryParse(field.value, out int result) && field.value != "*")
                        field.value = "1";
                    // filedに入力された値がn.maxより大きい場合n.maxにする
                    else if (int.TryParse(field.value, out int value) && value > n.max)
                        field.value = n.max.ToString();
                    // fieldに入力された値が0より小さい場合1にする
                    else if (int.TryParse(field.value, out int value2) && value2 <= 0)
                        field.value = "1";

                });
                calenderFields[n.name] = field;
            });
        }

        public override void Load(NodeData data)
        {
            if (data.Raw.GetFromPairs(timingKey, out string strType))
                timingField.value = ((TimingType[])Enum.GetValues(typeof(TimingType))).ToList().FirstOrDefault(t => t.ToString() == strType);

            calenderFields.ToList().ForEach(p =>
            {
                if (data.Raw.GetFromPairs($"{p.Key}{timeKey}", out float value))
                    p.Value.value = ((int)value).ToString();
            });

            base.Load(data);
        }

        public override NodeData Save()
        {
            var node = base.Save();
            node.Raw.SetToPairs(timingKey, timingField.value.ToString());

            calenderFields.ToList().ForEach(p =>
            {
                if (!float.TryParse(p.Value.value, out float input))
                    input = 0f;
                node.Raw.SetToPairs($"{p.Key}{timeKey}", input);
            });

            return node;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            timingField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            calenderFields.ToList().ForEach(p => p.Value.RegisterValueChangedCallback(evt => action?.Invoke(this)));
        }

        public override bool Check(InOut.EventInput input)
        {
            if ((TimingType)timingField.value == TimingType.After)
                return DateTime < input.DateTime;
            else if ((TimingType)timingField.value == TimingType.Before)
                return input.DateTime < DateTime;
            else if ((TimingType)timingField.value == TimingType.JustOnTime)
            {
                var year = calenderFields["Year"].value == "*" ? input.DateTime.Year : int.Parse(calenderFields["Year"].value);
                var month = calenderFields["Month"].value == "*" ? input.DateTime.Month : int.Parse(calenderFields["Month"].value);
                var day = calenderFields["Day"].value == "*" ? input.DateTime.Day : int.Parse(calenderFields["Day"].value);
                var hour = calenderFields["Hour"].value == "*" ? input.DateTime.Hour : int.Parse(calenderFields["Hour"].value);
                var minute = calenderFields["Minute"].value == "*" ? input.DateTime.Minute : int.Parse(calenderFields["Minute"].value);
                var second = calenderFields["Second"].value == "*" ? input.DateTime.Second : int.Parse(calenderFields["Second"].value);
                var timing = new DateTime(input.DateTime.Year, input.DateTime.Month, day, hour, minute, second);

                // input.DateTimeがtimingの時間に近づいた場合true
                var result = Math.Abs((timing - input.DateTime).TotalSeconds) < SPAN_TOLERANCE_SECONDS;
                // resultがtrueのとき、lastSpanTimeがnullの場合はtrueを返す
                if (result && lastSpanTime == null)
                {
                    lastSpanTime = input.DateTime;
                    return true;
                }
                // resultがtrueのとき、lastSpanTimeとinput.DateTimeがSPAN_TOLEARANCE_SECONDSの2倍以下の場合falseを返す
                // あまりにも短い間隔でtrueを返すのを防ぐ
                if (result && Math.Abs((lastSpanTime - input.DateTime).TotalSeconds) < SPAN_TOLERANCE_SECONDS*2)
                    return false;

                lastSpanTime = input.DateTime;
                return result;
            }
            return false;
        }

        /// <summary>
        /// 時間のトリガーを判別するタイプ
        /// </summary>
        enum TimingType
        {
            After,
            Before,
            JustOnTime
        }
    }
}