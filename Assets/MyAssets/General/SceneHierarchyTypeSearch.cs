
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using System.Reflection;
using System;

public class SceneHierarchyTypeSearch
{
    [MenuItem("GameObject/Search Type ...", false, 0)]
    static void SearchByType()
    {
        // 現在開いているウィンドウからヒエラルキーを探します。
        var hierarchyWindow = Resources.FindObjectsOfTypeAll<EditorWindow>()
        .FirstOrDefault(window => window.GetType().Name == "SceneHierarchyWindow");

        // コンポーネントタイプのセレクターを呼び出します。
        var searchWindowProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
        searchWindowProvider.Initialize(hierarchyWindow, (selectedTypeName) =>
        {
            SetSearchFilter(hierarchyWindow, selectedTypeName);
            ScriptableObject.DestroyImmediate(searchWindowProvider);
        });
        SearchWindow.Open(new SearchWindowContext(
            new UnityEngine.Vector2(hierarchyWindow.position.x + hierarchyWindow.position.width / 2,
            hierarchyWindow.position.y + 50), hierarchyWindow.position.width), searchWindowProvider);
    }

    /// <summary>
    /// リフレクションでインターナルなメソッドSetSearchFilterを呼び出し、ヒエラルキーの検索窓に文字列を指定します。
    /// </summary>
    static void SetSearchFilter(EditorWindow window, string typeName)
    {
        var method = typeof(SearchableEditorWindow).GetMethod("SetSearchFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            Debug.LogError("method not found. not available with this Unity version.");
            return;
        }
        method.Invoke(window, new object[] { $"t:{typeName}", SearchableEditorWindow.SearchMode.All, true, false });
    }

    public class SearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        struct TypeEntry
        {
            public string[] title;
            public string name;
        }

        Texture2D icon;
        Action<string> onSelectEntry;

        void OnDestroy()
        {
            if (icon != null)
            {
                DestroyImmediate(icon);
                icon = null;
            }
        }

        public void Initialize(EditorWindow editorWindow, Action<string> onSelectEntry)
        {
            this.onSelectEntry = onSelectEntry;
            icon = new Texture2D(1, 1);
            icon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            icon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var nodeEntries = new List<TypeEntry>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Component)))
                    {
                        var titles = type.FullName.Split('.');
                        titles = titles.Length == 1 ? new string[] { "Scripts", titles.First() } : titles;
                        nodeEntries.Add(new TypeEntry
                        {
                            name = type.Name,
                            title = titles,
                        });
                    }
                }
            }

            nodeEntries.Sort((entry1, entry2) =>
            {
                for (var i = 0; i < entry1.title.Length; i++)
                {
                    if (i >= entry2.title.Length)
                        return 1;
                    var value = entry1.title[i].CompareTo(entry2.title[i]);
                    if (value != 0)
                    {
                        if (entry1.title.Length != entry2.title.Length && (i == entry1.title.Length - 1 || i == entry2.title.Length - 1))
                            return entry1.title.Length < entry2.title.Length ? -1 : 1;
                        return value;
                    }
                }
                return 0;
            });

            var groups = new List<string>();
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Search Component"), 0),
            };

            foreach (var nodeEntry in nodeEntries)
            {
                var createIndex = int.MaxValue;
                for (var i = 0; i < nodeEntry.title.Length - 1; i++)
                {
                    var group = nodeEntry.title[i];
                    if (i >= groups.Count)
                    {
                        createIndex = i;
                        break;
                    }
                    if (groups[i] != group)
                    {
                        groups.RemoveRange(i, groups.Count - i);
                        createIndex = i;
                        break;
                    }
                }

                for (var i = createIndex; i < nodeEntry.title.Length - 1; i++)
                {
                    var group = nodeEntry.title[i];
                    groups.Add(group);
                    tree.Add(new SearchTreeGroupEntry(new GUIContent(group)) { level = i + 1 });
                }
                tree.Add(new SearchTreeEntry(new GUIContent(nodeEntry.title.Last(), icon)) { level = nodeEntry.title.Length, userData = nodeEntry });
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var nodeEntry = (TypeEntry)entry.userData;
            onSelectEntry?.Invoke(nodeEntry.name);
            return true;
        }

        void AddEntries(string name, string[] title, List<TypeEntry> nodeEntries)
        {
            nodeEntries.Add(new TypeEntry
            {
                name = name,
                title = title,
            });
        }
    }
}


