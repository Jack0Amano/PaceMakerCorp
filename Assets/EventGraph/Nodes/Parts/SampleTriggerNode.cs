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
    /// <summary>
    /// Triggerの条件付を行うノード
    /// </summary>
    public abstract class SampleTriggerNode : SampleNode
    {
        /// <summary>
        /// 条件をInputから検証する
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public abstract bool Check(InOut.EventInput input);
    }
}