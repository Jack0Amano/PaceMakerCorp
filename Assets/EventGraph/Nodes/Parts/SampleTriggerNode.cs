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
    /// Triggerฬ๐t๐sคm[h
    /// </summary>
    public abstract class SampleTriggerNode : SampleNode
    {
        /// <summary>
        /// ๐๐Inputฉ็ุท้
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public abstract bool Check(InOut.EventInput input);
    }
}