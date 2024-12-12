using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace EventGraph.Nodes.Parts
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
        public Action<Port> OnConnect;
        /// <summary>
        /// Portの接続が切れたとき呼び出し
        /// </summary>
        public Action<Port> OnDisconnect;

        public static Color BoolColor = Hex("#E74C3C");
        public static Color PortColor = Color.white;
        public static Color TextArrayColor = Rgb(96, 204, 186);
        public static Color ImageColor = Hex("#F1C40F");
        public static Color TextColor = Rgb(159, 229, 139);
        public static Color SelectiveColor = Rgb(148, 148, 191);

        protected CustomPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
            if (type == typeof(CustomPort) || type == typeof(Port))
                portColor = Color.white;
            else if (type == typeof(bool))
                portColor = BoolColor;
            else if (type == typeof((Sprite, InOut.ImageAlignment)))
                portColor = ImageColor;
            else if (type == typeof(string))
                portColor = TextColor;
            else if (type == typeof(List<(string, string)>))
                portColor = TextArrayColor;
            else if (type == typeof(List<string>))
                portColor = SelectiveColor;
        }

        public override void Connect(Edge edge)
        {
            base.Connect(edge);
            OnConnect?.Invoke(this);
        }

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);
            OnDisconnect?.Invoke(this);
        }

        public override void DisconnectAll()
        {
            base.DisconnectAll();
            OnDisconnect?.Invoke(this);
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
            ele.AddManipulator((IManipulator)ele.m_EdgeConnector);
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