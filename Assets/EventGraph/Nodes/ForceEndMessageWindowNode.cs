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
    /// <summary>
    /// メッセージウィンドウを明示的に閉じる
    /// </summary>
    public class ForceEndMessageWindowNode : ProcessNode
    {

        public ForceEndMessageWindowNode() : base()
        {
            title = "Force End Message Window";
            NodePath = "Process/Force End Message Window";
        }

        public override void Load(NodeData data)
        {
            base.Load(data);
        }

        public override NodeData Save()
        {
            var data = base.Save();
            return data;
        }

        public override EventOutput Execute(EventInput eventInput)
        {
            var output = new MessageEventOutput(this, Guid, null);
            output.ForceEndMessageWindow = true;
            return output;
        }
    }
}