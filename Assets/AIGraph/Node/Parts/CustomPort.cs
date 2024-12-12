using AIGraph.InOut;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static Utility;
using System.Linq;

namespace AIGraph.Nodes.Parts
{

    // https://forum.unity.com/threads/callback-on-edge-connection-in-graphview.796290/


    /// <summary>
    /// 拡張したポート
    /// </summary>
    public class CustomPort : Port
    {
        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            private GraphViewChange m_GraphViewChange;
            private List<Edge> m_EdgesToCreate;
            private List<GraphElement> m_EdgesToDelete;

            public DefaultEdgeConnectorListener()
            {
                this.m_EdgesToCreate = new List<Edge>();
                this.m_EdgesToDelete = new List<GraphElement>();
                this.m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                this.m_EdgesToCreate.Clear();
                this.m_EdgesToCreate.Add(edge);
                this.m_EdgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                {
                    foreach (Edge connection in edge.input.connections)
                    {
                        if (connection != edge)
                            this.m_EdgesToDelete.Add(connection);
                    }
                }
                if (edge.output.capacity == Capacity.Single)
                {
                    foreach (Edge connection in edge.output.connections)
                    {
                        if (connection != edge)
                            this.m_EdgesToDelete.Add(connection);
                    }
                }
                if (this.m_EdgesToDelete.Count > 0)
                    graphView.DeleteElements(this.m_EdgesToDelete);
                List<Edge> edgesToCreate = this.m_EdgesToCreate;
                if (graphView.graphViewChanged != null)
                    edgesToCreate = graphView.graphViewChanged(this.m_GraphViewChange).edgesToCreate;
                foreach (Edge edge1 in edgesToCreate)
                {
                    graphView.AddElement(edge1);
                    edge.input.Connect(edge1);
                    edge.output.Connect(edge1);
                }
            }
        }

        // 普通のPortにはOnConnectが無いため作成したCustomPort
        /// <summary>
        /// Port接続時の呼び出し
        /// </summary>
        public Action<CustomPort> OnConnect;
        /// <summary>
        /// Portの接続が切れたとき呼び出し
        /// </summary>
        public Action<CustomPort> OnDisconnect;

        /// <summary>
        /// https://iro-color.com/colorchart/tone/dull-tone.html
        /// </summary>
        public static Color BoolColor = Hex("#E74C3C");
        public static Color PortColor = Color.white;
        public static Color AIActionColor = Hex("#CB4829");
        public static Color WeaponColor = Hex("#D59533");
        public static Color EnvironmentColor = Color.white;
        public static Color SituationsColor = Hex("#136EAB");
        public static Color SituationColor = Hex("#0099CE");

        public bool IsHighlighted
        {
            get => m_IsHighlighted;
            set
            {
                if (value)
                {
                    m_ConnectorBox.style.borderBottomWidth = 3;
                    m_ConnectorBox.style.borderTopWidth = 3;
                    m_ConnectorBox.style.borderLeftWidth = 3;
                    m_ConnectorBox.style.borderRightWidth   = 3;
                    m_ConnectorBox.style.height = 12;
                    m_ConnectorBox.style.width = 12;
                }
                else
                {
                    m_ConnectorBox.style.borderBottomWidth = 1;
                    m_ConnectorBox.style.borderTopWidth = 1;
                    m_ConnectorBox.style.borderLeftWidth = 1;
                    m_ConnectorBox.style.borderRightWidth = 1;
                    m_ConnectorBox.style.height = 8;
                    m_ConnectorBox.style.width = 8;
                }
                m_IsHighlighted = value;
            }
        }
        private bool m_IsHighlighted = false;

        /// <summary>
        /// Portに接続されているEdge
        /// </summary>
        public new List<Edge> connections { private set; get; } = new List<Edge>();

        /// <summary>
        /// Portが接続されているか
        /// </summary>
        public new bool connected
        {
            get => connections.Count != 0;
        }

        /// <summary>
        /// Portに接続されているPort
        /// </summary>
        public List<Port> ConnectedPorts
        {
            get
            {
                var ports = connections.ToList().ConvertAll(e => e.output == this ? e.input : e.output);
                ports.RemoveAll(p => p == null);
                return ports;
            }
        }

        /// <summary>
        /// Portに接続されているNode
        /// </summary>
        public List<Node> ConnectedNodes
        {
            get => ConnectedPorts.ConvertAll(p => p.node);
        }

        protected CustomPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
            if (type == typeof(CustomPort) || type == typeof(Port) || type == typeof(EnvironmentData))
                portColor = EnvironmentColor;
            else if (type == typeof(bool))
                portColor = BoolColor;
            else if (type == typeof(ItemData))
                portColor = WeaponColor;
            else if (type == typeof(AIAction))
                portColor = AIActionColor;
            else if (type == typeof(List<Situation>))
                portColor = SituationsColor;
            else if (type == typeof(Situation))
                portColor = SituationColor;
        }

        public override void Connect(Edge edge)
        {
            if (!connections.Contains(edge))
                connections.Add(edge);

            base.Connect(edge);
            OnConnect?.Invoke(this);
        }

        public override void Disconnect(Edge edge)
        {
            connections.Remove(edge);

            base.Disconnect(edge);
            OnDisconnect?.Invoke(this);
        }

        public override void DisconnectAll()
        {
            base.DisconnectAll();
            connections.Clear();
            OnDisconnect?.Invoke(this);
        }

        public void DisconnectAll(bool nortification)
        {
            if (nortification)
                DisconnectAll();
            else
            {
                base.DisconnectAll();
                connections.Clear();
            }
        }

        public new static CustomPort Create<TEdge>(
            Orientation orientation,
            Direction direction,
            Port.Capacity capacity,
            System.Type type)
            where TEdge : Edge, new()
        {
            var listener = new DefaultEdgeConnectorListener();
            var ele = new CustomPort(orientation, direction, capacity, type)
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(listener)
            };
            ele.AddManipulator(ele.m_EdgeConnector);
            return ele;
        }

        #region Color用の拡張
        private static Color Rgb(int r, int g, int b)
        {
            return new Color(r / 256f, g / 256f, b / 256f);
        }

        private static Color Hex(string value)
        {
            try
            {
                var c = new System.Drawing.ColorConverter();
                var _color = (System.Drawing.Color)c.ConvertFromString(value);
                var color = new Color(_color.R / 256f, _color.G / .256f, _color.B / 256f, _color.A / 256f);
                return new Color(_color.R / 256f, _color.G / 256f, _color.B / 256f, _color.A / 256f);
            }
            catch (FormatException ex)
            {
                Debug.LogWarning(ex);
                return Color.white;
            }
        }
        #endregion
    }
}