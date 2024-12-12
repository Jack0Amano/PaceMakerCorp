using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using EventGraph.Nodes.Parts;
using EventGraph.Editor;
using EventGraph.InOut;

namespace EventGraph.Nodes
{
    public class ImageWindowNode : ProcessNode
    {

        private CustomPort inputString;
        private CustomPort inputImages;
        private CustomPort inputSelective;

        private List<CustomPort> outputPorts = new List<CustomPort>();

        private readonly string OutputPortsCountKey = "OutputportsCountKey";

        public ImageWindowNode() : base()
        {
            title = "Image Window";
            NodePath = "Process/Image Window";

            inputImages = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof((Sprite, InOut.ImageAlignment)));
            inputImages.portName = "Images";
            inputContainer.Add(inputImages);

            inputString = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(List<(string, string)>));
            inputString.portName = "Text Array";
            inputContainer.Add(inputString);

            inputSelective = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(List<string>));
            inputSelective.portName = "Choice";
            inputContainer.Add(inputSelective);
            inputSelective.OnConnect += ((Port port) =>
            {
                var edge = port.connections.ToList().FirstOrDefault();
                if (edge == null) return;
                if (edge.output != null && edge.output.node is ChoiceNode node)
                {
                    ChoiceCountIsChanged(node);
                    node.OnCountOfChoicesIsChaged = ChoiceCountIsChanged;
                }
            });
            inputSelective.OnDisconnect += ((Port port) =>
            {

                ChoiceCountIsChanged(null);
            });

            OutputPort.RemoveFromHierarchy();

            ChoiceCountIsChanged(null);
        }

        /// <summary>
        /// 接続されたChoiceNodeの選択肢の数が変更された場合呼び出される
        /// </summary>
        /// <param name="node"></param>
        private void ChoiceCountIsChanged(ChoiceNode node)
        {
            var count = 1;
            if (node != null)
            {
                var texts = node.Texts;
                count = texts.Count == 0 ? 1 : texts.Count;
            }
            SetOutputPorts(count);
        }

        /// <summary>
        /// OutputPortをCountの数用意する
        /// </summary>
        /// <param name="count"></param>
        private void SetOutputPorts(int count)
        {
            if (count != outputPorts.Count)
            {
                var reducePorts = outputPorts.Count > count;
                var diffCount = Math.Abs(outputPorts.Count - count);
                for (var i = 0; i < diffCount; i++)
                {
                    if (reducePorts)
                    {
                        outputPorts.Last().DisconnectAll();
                        outputContainer.Remove(outputPorts.Last());
                        outputPorts.RemoveAt(outputPorts.Count - 1);
                    }
                    else
                    {
                        var port = CustomPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(CustomPort));
                        outputContainer.Add(port);
                        outputPorts.Add(port);
                        port.portName = $"No.{outputPorts.Count-1}";
                    }
                }
            }
        }

        public override void Load(NodeData data)
        {
            base.Load(data);

            if (data.raw.GetFromPairs(OutputPortsCountKey, out float value))
            {
                SetOutputPorts((int)value);
            }
        }

        public override NodeData Save()
        {
            var data = base.Save();
            data.raw.SetToPairs(OutputPortsCountKey, (float)outputPorts.Count);
            return data;
        }

        public override EventOutput Execute(EventInput eventInput)
        {

            var output = new MessageEventOutput(this, Guid, null);

            if (outputPorts.Count == 1)
                output.NextPort = outputPorts.First();

            var strEdge = inputString.connections.FirstOrDefault();
            if (strEdge != null)
            {
                var strNode = strEdge.output.node as TextArrayNode;
                if (strNode != null)
                {
                    strNode.Texts.ForEach(t =>
                    {
                        var s = new MessageEventOutput.Sentence()
                        {
                            text = t.main,
                            messageFrom = t.from,
                            isChoice = false,
                        };
                        output.sentences.Add(s);
                    });
                }
            }

            var imgNodes = inputImages.connections.ToList().ConvertAll(e => e.output.node as ImageNode);
            foreach(var imgNode in imgNodes)
            {
                if (imgNode.SetType == ImageNode.SetImageType.Show)
                    output.ShowImages.Add((imgNode.Alignment, imgNode.Image));
                else if (imgNode.SetType == ImageNode.SetImageType.Hide)
                    output.HideImage.Add(imgNode.Alignment);
                else if (imgNode.SetType == ImageNode.SetImageType.Active)
                    output.ActivateImages.Add(imgNode.Alignment);
                else if (imgNode.SetType == ImageNode.SetImageType.Disactive)
                    output.DisactivateImages.Add(imgNode.Alignment);
            }

            var choiceEdge = inputSelective.connections.FirstOrDefault();
            if (choiceEdge != null)
            {
                var choiceNode = choiceEdge.output.node as ChoiceNode;
                if (choiceNode != null)
                {
                    choiceNode.Texts.ForEach(t =>
                    {
                        var s = new MessageEventOutput.Sentence()
                        {
                            text = t,
                            isChoice = true,
                        };
                        output.sentences.Add(s);
                    });
                }
            }

            return output;
        }

        public override EventOutput ExecuteFromMiddle(EventInput eventInput)
        {
            var selectedPort = outputPorts.ElementAtOrDefault(eventInput.SelectTriggerIndex);
            return new MessageEventOutput(this, Guid, selectedPort);
        }
    }
}