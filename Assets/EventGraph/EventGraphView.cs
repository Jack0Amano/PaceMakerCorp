using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using EventGraph.Nodes;
using UnityEngine;

namespace EventGraph
{
    /// <summary>
    /// GraphWindow�̒���Viewer
    /// </summary>
    public class EventGraphView : GraphView
    {
        public RootNode RootNode;

        public Editor.EventGraphDataContainer dataContainer;

        internal EventGraphWindow graphWindow;

        internal EventGraphSearchWindowProvider searchWindowProvider;

        public Vector2 mousePosition;

        public EventGraphView() : base()
        {
            GridBackground gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();

            // �Y�[���@�\
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            // View���̈ړ�
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            var dragger = new ContentDragger();
            
            // �w�i�F
            Insert(0, new GridBackground());
            // �����m�[�h�ǉ���
            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new RectangleSelector());

            // Node�����@�\ 
            searchWindowProvider = ScriptableObject.CreateInstance<EventGraphSearchWindowProvider>();
            searchWindowProvider.Initialize(this);

            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindowProvider);
            };
        }

       

        /// <summary>
        /// �m�[�h�Ԃ̐ڑ����[��
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            // �m�[�h�Ԃ̐ڑ����[��
            var compatiblePorts = new List<Port>();
            foreach (var port in ports.ToList())
            {
                if (startAnchor.node == port.node ||
                    startAnchor.direction == port.direction ||
                    startAnchor.portType != port.portType)
                {
                    continue;
                }

                compatiblePorts.Add(port);
            }
            return compatiblePorts;
        }

        /// <summary>
        /// RootNode���쐬����
        /// </summary>
        public void CreateRootNode()
        {
            // Root node�������쐬
            RootNode = new RootNode();
            AddElement(RootNode);
        }

        /// <summary>
        /// Node�����s
        /// </summary>
        /// <param name="nodeID">�r������J�n����ꍇnodeID����J�n�����</param>
        /// <returns>Node�����s�����ۂ̌��� (Node���i�܂Ȃ������ꍇEmpty)</returns>
        public List<InOut.EventOutput> Execute(InOut.EventInput input)
        {
            if (RootNode == null)
            {
                Debug.Log("RootNode is null");
                return new List<InOut.EventOutput>();
            }

            var output = new List<InOut.EventOutput>();

            Edge firstEdge = null;
            if (input.StartAtID != null && input.StartAtID.Length != 0)
            {
                var startNode = (ProcessNode)nodes.ToList().Find(n => ((SampleNode)n).Guid == input.StartAtID);
                var _output = startNode.ExecuteFromMiddle(input);
                if (_output != null)
                {
                    _output.EventID = RootNode.EventID;
                    if (_output.NextPort != null)
                        firstEdge = _output.NextPort.connections.FirstOrDefault();
                    //output.Add(_output);
                }
            }
            else
            {
                firstEdge = RootNode.OutputPort.connections.FirstOrDefault();
                var evt = RootNode.Execute(input);
                evt.EventID = RootNode.EventID;
                output.Add(evt);
            }
            

            if (firstEdge == null) return output;

            var currentNode = firstEdge.input.node as ProcessNode;

            while (true)
            {
                var evt = currentNode.Execute(input);
                evt.EventID = RootNode.EventID;
                output.Add(evt);

                if (evt == null || evt.NextPort == null)
                    break;
                var edge = evt.NextPort.connections.FirstOrDefault();
                if (edge == null) break;

                currentNode = edge.input.node as ProcessNode;
            }

            return output;
        }

    }
}