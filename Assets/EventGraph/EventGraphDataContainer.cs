using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using EventGraph.Nodes;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections;

namespace EventGraph.Editor
{
    // NOTE ScriptableObjectは同名のファイルに書かなければならない
    /// <summary>
    /// 実際に保存されるSerialiableなObject
    /// </summary>
    [Serializable]
    public class EventGraphDataContainer : ScriptableObject
    {
        /// <summary>
        /// EventのRootNodeで指定するEventに固有のID
        /// </summary>
        public string EventID = "";

        public List<NodeData> Nodes = new List<NodeData>();
        public List<EdgeData> Edges = new List<EdgeData>();

        public Vector3 viewPosition;
        public Vector3 viewScale = new Vector3(1, 1, 1);
        
        /// <summary>
        /// ScriptableObjectから作ったファイルをダブルクリックしてGrafViewを開くための
        /// </summary>
        /// <param name="instanceID"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var target = EditorUtility.InstanceIDToObject(instanceID);

            if (target is EventGraphDataContainer)
            {
                var path = AssetDatabase.GetAssetPath(instanceID);

                if (Path.GetExtension(path) != ".asset") return false;
                var name = Path.GetFileNameWithoutExtension(path);

                bool windowIsOpen = EditorWindow.HasOpenInstances<EventGraphWindow>();
                EventGraphWindow window;
                if (!windowIsOpen)
                {
                    window = EditorWindow.CreateWindow<EventGraphWindow>();
                }
                else
                {
                    EditorWindow.FocusWindowIfItsOpen<EventGraphWindow>();
                    window = (EventGraphWindow)EditorWindow.focusedWindow;
                }
                window.titleContent.text = name;
                window.Path = path;
                window.Load();

                Selection.activeObject = target;
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            var path = AssetDatabase.GetAssetPath(this);
            return $"DataContainer: {Nodes.Count} nodes, {Edges.Count} edges, {path}";
        }
    }

    #region グラフの保存用実データ
    /// <summary>
    /// Nodeの接続？ Edgeと呼んでいる?
    /// </summary>
    [Serializable]
    public class EdgeData
    {
        public string BaseNodeGuid;
        public string BasePortName;
        public string TargetNodeGuid;
        public string TargetPortName;

        public override string ToString()
        {
            return $"EdgeData: ({BasePortName}, {BaseNodeGuid}) -- ({TargetPortName}, {TargetNodeGuid}";
        }
    }

    /// <summary>
    /// ノードの保存用データ
    /// </summary>
    [Serializable]
    public class NodeData
    {
        [Header("Basic info for node")]
        public string Keyword = "Test NodeName";
        public string Guid = "Test NodeGUID";
        public bool IsEntryPoint = false;
        public Vector2 Position = Vector2.zero;
        public Vector2 Size = Vector2.zero;
        public bool Expanded = true;

        [Header("Raw data in Node")]
        public RawData Raw;

        /// <summary>
        /// Nodeの基本情報をNodeDataに移す
        /// </summary>
        public NodeData()
        {
            Raw = new RawData();
        }

        /// <summary>
        /// ノード内に実際に登録されたデータ
        /// </summary>
        [Serializable]
        public class RawData
        {
            [SerializeField] public List<SerializableKeyValuePair> KeyValuePairs = new List<SerializableKeyValuePair>();

            /// <summary>
            /// Pairsからkeyを使ってvalueを取得する
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public bool GetFromPairs<T>(string key, out T value)
            {
                var searched = KeyValuePairs.Find(p => p.key == key);
                if (searched != null)
                {
                    value = searched.GetValue<T>();
                    return true;
                }
                value = default;
                return false;
            }

            /// <summary>
            /// Pairsにvalueを設置する
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public void SetToPairs(string key, object value)
            {
                var searched = KeyValuePairs.Find(p => p.key == key);
                if (searched == null)
                    KeyValuePairs.Add(new SerializableKeyValuePair(key, value));
                else
                    searched.SetValue(value);
            }

            /// <summary>
            /// PairsにDictionaryの形でvalueを入れる
            /// </summary>
            /// <param name="value"></param>
            public void SetDictionary(Dictionary<string, string> value)
            {
                KeyValuePairs.Clear();
                value.ToList().ForEach(d =>
                {
                    KeyValuePairs.Add(new SerializableKeyValuePair(d.Key, d.Value));
                });
            }

            /// <summary>
            /// Valueの要素数
            /// </summary>
            public int Count
            {
                get => KeyValuePairs.Count;
            }

            /// <summary>
            /// 実データを保存するclass
            /// </summary>
            [Serializable]
            public class SerializableKeyValuePair
            {
                public readonly static string DefaultStringValue = "[\\Empty]";

                public string key;
                [SerializeField] private string stringValue;
                [SerializeField] private Vector3 locationValue = Vector3.positiveInfinity;
                [SerializeField] private Sprite imageValue;
                [SerializeField] private UnityEngine.Object rawObject;
                [SerializeField] private float floatValue;

                public SerializableKeyValuePair(string key, object value)
                {
                    this.key = key;
                    SetValue(value);
                }

                /// <summary>
                /// Pairに値をセットする 他のvalueは消える
                /// </summary>
                /// <param name="value"></param>
                public void SetValue(object value)
                {
                    ClearAllValue();
                    if (value is string s)
                        stringValue = s;
                    else if (value is Vector3 v)
                        locationValue = v;
                    else if (value is Sprite i)
                        imageValue = i;
                    else if (value is UnityEngine.Object o)
                        rawObject = o;
                    else if (value is float f)
                        floatValue = f;
                }

                /// <summary>
                /// すべてのvalueを空にする
                /// </summary>
                private void ClearAllValue()
                {
                    stringValue = DefaultStringValue;
                    locationValue = Vector3.positiveInfinity;
                    imageValue = null;
                    rawObject = null;
                    floatValue = float.MinValue;
                }

                /// <summary>
                /// Pairから値を取得する
                /// </summary>
                public T GetValue<T>()
                {

                    // stringValueはnullにしておいてもassetから読み出すとlength=0のstringになるため検索は最後
                    if (!Vector3.positiveInfinity.Equals(locationValue))
                        return (T)(object)locationValue;
                    else if (imageValue != null)
                        return (T)(object)imageValue;
                    else if (rawObject != null)
                        return (T)(object)rawObject;
                    else if (floatValue != float.MinValue)
                        return (T)(object)floatValue;
                    else if (stringValue != DefaultStringValue)
                        return (T)(object)stringValue;

                    return default;
                }
            }
        }

        public override string ToString()
        {
            return $"{Keyword}: {Guid}";
        }

    }
    #endregion
}