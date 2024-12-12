using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using AIGraph.Nodes;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;

namespace AIGraph
{
    /// <summary>
    /// 右クリックメニューからNodeを追加するときの動作
    /// </summary>
    public class AIGraphSearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private AIGraphView graphView;
        /// <summary>
        /// ElementをProviderから追加したときに呼び出し
        /// </summary>
        internal Action<SampleNode> AddElementAction;

        /// <summary>
        /// 右クリックでメニューを出した際のポジション
        /// </summary>
        public Vector2 rightClickPosition = Vector2.zero;

        private List<SearchTreeEntry> entries;

        public void Initialize(AIGraphView graphView)
        {
            this.graphView = graphView;

            // SearchTreeを作成
            entries = new List<SearchTreeEntry>();
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Create Node")));

            var nodePathList = new List<(Type type, List<string> path)>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(SampleNode)))
                        && type != typeof(RootNode))
                    {
                        var sampleNode = (SampleNode)Activator.CreateInstance(type);
                        var NodePath = sampleNode.NodePath;
                        
                        if (NodePath == null || NodePath.Length == 0)
                        {
                            NodePath = type.Name.Replace("Node", "");

                            NodePath = Regex.Replace(NodePath, @"[A-Z]", match => " " + match.Value);
                            NodePath = NodePath.Remove(0, 1);
                        }
                        nodePathList.Add((type, NodePath.Split('/').ToList()));
                    }
                }
            }

            nodePathList.Sort((a, b) => b.path.Count - a.path.Count);
            while (nodePathList.Count != 0)
            {
                var node = nodePathList.First();
                if (node.path.Count == 1)
                {
                    // Pathの深さが0のNode
                    entries.Add(new SearchTreeEntry(new GUIContent(node.path[0])) { level = 1, userData = node.type });
                    nodePathList.Remove(node);
                    continue;
                }
                else
                {
                    // Nodeのpathの深さが1のNode
                    entries.Add(new SearchTreeGroupEntry(new GUIContent(node.path[0])) { level = 1 });
                    var addNodes = nodePathList.FindAll(l => l.path[0] == node.path[0]);
                    addNodes.ForEach(n => entries.Add(new SearchTreeEntry(new GUIContent(n.path[1])) { level = 2, userData = n.type }));
                    nodePathList.RemoveAll(p => addNodes.Contains(p));
                }
            }
        }

        List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context)
        {

            return entries;
        }

        bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {

            var type = searchTreeEntry.userData as System.Type;
            var node = Activator.CreateInstance(type) as SampleNode;
            graphView.AddElement(node);
            var rect = node.GetPosition();
            rect.position = rightClickPosition;
            node.SetPosition(rect);
            return true;
        }
    }
}