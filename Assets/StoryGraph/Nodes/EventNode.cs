using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using StoryGraph.Nodes.Parts;
using StoryGraph.Editor;
using StoryGraph.InOut;
using UnityEditor.UIElements;
using UnityEditor;
using System.IO;
using static Utility;

namespace StoryGraph.Nodes
{
    /// <summary>
    /// 実行するイベントのノード
    /// </summary>
    public class EventNode : ProcessNode
    {
        readonly ObjectField FileField;
        readonly Toggle RepeatableToggle;
        readonly Button ReloadButton;
        readonly IntegerField SortValueField;

        readonly static public string EventNodeFileKey = "EventNodeFileKey";
        readonly string EventNodeToggleKey = "EventNodeToggleKey"; 
        readonly static public string EventSortKey = "EventSortKey";

        public EventGraph.Editor.EventGraphDataContainer DataContainer { get => (EventGraph.Editor.EventGraphDataContainer)FileField.value; }
        /// <summary>
        /// Nodeに登録されているEventが繰り返し可能なEventか
        /// </summary>
        public bool Repeatable { get => RepeatableToggle.value; }
        /// <summary>
        /// Eventの優先順位
        /// </summary>
        public int SortValue { get => SortValueField.value; }
        /// <summary>
        /// EventNodeに保持されているEventDataContainerからEventViewを取得する
        /// </summary>
        public EventGraph.EventGraphView EventGraphView
        {
            get
            {
                if (_EventGraphView == null)
                    _EventGraphView = EventGraph.Editor.EventGraphSaveUtility.LoadGraph(DataContainer);
                return _EventGraphView;
            }
        }
        private EventGraph.EventGraphView _EventGraphView;
        /// <summary>
        /// DataControllerにて保存されたEventの進行内容
        /// </summary>
        public SaveDataInfo.StorySaveData.EventSaveData SaveData;
        /// <summary>
        /// 既にEventが終了しているか
        /// </summary>
        public bool IsCompleted
        {
            get => SaveData.state == SaveDataInfo.StorySaveData.EventSaveData.State.Completed;
        }

        public EventNode(): base()
        {
            title = "Event Node";
            NodePath = "Event Node";

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/StoryGraph/Nodes/EventNode.uxml");

            var content = asset.Instantiate();
            mainContainer.Add(content);

            FileField = content.Q<ObjectField>();
            FileField.objectType = typeof(object);
            FileField.RegisterValueChangedCallback(evt =>
            {
                if (!(FileField.value is EventGraph.Editor.EventGraphDataContainer))
                    FileField.value = null;
                ReloadEventData();
            });

            ReloadButton = content.Q<Button>();
            ReloadButton.clicked += (() =>
            {
                ReloadEventData();
            });

            RepeatableToggle = content.Q<Toggle>();
            SortValueField = content.Q<IntegerField>();
        }

        /// <summary>
        /// 接続されたChoiceNodeの選択肢の数が変更された場合呼び出される
        /// </summary>
        /// <param name="node"></param>
        private void ReloadEventData()
        {
            if (FileField.value == null || !(FileField.value is EventGraph.Editor.EventGraphDataContainer data))
                return;

            title = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(FileField.value));
            var endEventNodes = data.Nodes.FindAll(n => n.Keyword == nameof(EventGraph.Nodes.EndEventNode)).ConvertAll(n => n);
            SetOutputPorts(endEventNodes.Count);

            var portAndTargetDict = new Dictionary<CustomPort, List<(Edge edge, Node target)>>();
            OutputPorts.ForEach(p =>
            {
                portAndTargetDict[p] = p.connections.ToList().ConvertAll(e => (e, e.input.node));
            });

            var valueChanged = false;
            for(var i=0; i<endEventNodes.Count; i++)
            {
                endEventNodes[i].raw.GetFromPairs(EventGraph.Nodes.EndEventNode.EndEventNameKey, out string name);
                var portName = $"No.{i}: {name}";
                if (OutputPorts[i].portName != portName)
                {
                    var targetPorts = OutputPorts[i].connections.ToList().ConvertAll(e => e.input);
                    OutputPorts[i].portName = portName;

                    targetPorts.ForEach(p =>
                    {
                        var tempEdge = new Edge
                        {
                            output = OutputPorts[i],
                            input = p
                        };
                        tempEdge.output.Connect(tempEdge);
                        tempEdge.input.Connect(tempEdge);
                    });

                    valueChanged = true;
                }
            }

            if (valueChanged)
                RegisterAnyValueChangedCallback?.Invoke(this);
        }

        /// <summary>
        /// OutputPortをCountの数用意する
        /// </summary>
        /// <param name="count"></param>
        private void SetOutputPorts(int count)
        {
            if (count != OutputPorts.Count)
            {
                var reducePorts = OutputPorts.Count > count;
                var diffCount = Math.Abs(OutputPorts.Count - count);
                for (var i = 0; i < diffCount; i++)
                {
                    if (reducePorts)
                    {
                        OutputPorts.Last().DisconnectAll();
                        outputContainer.Remove(OutputPorts.Last());
                        OutputPorts.RemoveAt(OutputPorts.Count - 1);
                    }
                    else
                    {
                        var port = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(CustomPort));
                        outputContainer.Add(port);
                        OutputPorts.Add(port);
                    }
                }
            }
        }

        public override CustomPort FindOutputPortFromEndEventName(string endEventName)
        {
            if (endEventName.Length == 0)
            {
                if (OutputPorts.Count != 1)
                    Debug.LogWarning($"{this}: Node is out more than 2, but endEventName is empty");
                return OutputPorts.FirstOrDefault();
            }
            return OutputPorts.Find(p => p.portName == endEventName);
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
            if (data.raw.GetFromPairs(EventNodeFileKey, out UnityEngine.Object file))
            {
                FileField.value = file;
                ReloadEventData();
            }
            data.raw.GetFromPairs(EventNodeToggleKey, out float toggleF);
            var toggle = toggleF > 0;
            RepeatableToggle.value = toggle;

            data.raw.GetFromPairs(EventSortKey, out float sortValue);
            SortValueField.value = (int)sortValue;
        }

        public override NodeData Save()
        {
            var data = base.Save();
            if (FileField.value != null)
            {
                data.raw.SetToPairs(EventNodeFileKey, FileField.value);
            }
            data.raw.SetToPairs(EventNodeToggleKey, RepeatableToggle.value ? 1f : -1f);
            data.raw.SetToPairs(EventSortKey, (float)SortValueField.value);
            return data;
        }

        public override void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            base.RegisterAnyValueChanged(action);
            FileField.RegisterValueChangedCallback(evt => action?.Invoke(this));
            RepeatableToggle.RegisterValueChangedCallback(evt => action?.Invoke(this));
            SortValueField.RegisterValueChangedCallback(evt => action?.Invoke(this));
        }

        /// <summary>
        /// Nodeに置かれるEventGraphを実行する
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public List<EventGraph.InOut.EventOutput> ExecuteEventView(EventGraph.InOut.EventInput input)
        {
            var output = new List<EventGraph.InOut.EventOutput>();
            if (IsCompleted)
                return output;
            if (input.StartAtID == null || input.StartAtID.Length == 0)
                input.StartAtID = SaveData.WaitingNodeID;
            output = EventGraphView.Execute(input);
            if (output.Count == 0)
                return output;

            if (output.Last().IsEventCompleted)
            {
                SaveData.state = SaveDataInfo.StorySaveData.EventSaveData.State.Completed;
                SaveData.WaitingNodeID = "";
            }
            else
            {
                SaveData.state = SaveDataInfo.StorySaveData.EventSaveData.State.Running;
                SaveData.WaitingNodeID = output.Last().NodeID;
            }
            

            return output;
        }

        public override string ToString()
        {
            return $"StoryGraph.EventNode: event of {DataContainer}";
        }
    }
}