﻿using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using AIGraph.Nodes;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using static Utility;

namespace AIGraph.Editor
{
    // NOTE ScriptableObjectのclass名はファイル名と同名出なければならない
    /// <summary>
    /// 実際に保存されるSerialiableなObject
    /// </summary>
    [Serializable]
    public class AIGraphDataContainer : ScriptableObject
    {
        /// <summary>
        /// EventのRootNodeで指定するEventに固有のID
        /// </summary>
        public string AIID = "";

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

            if (target is AIGraphDataContainer)
            {
                var path = AssetDatabase.GetAssetPath(instanceID);

                if (Path.GetExtension(path) != ".asset") return false;
                var name = Path.GetFileNameWithoutExtension(path);

                AIGraphWindow window;
                window = AIGraphWindow.GetAllOpenEditorWindows().Find(w => w.path == path);
                if (window != null)
                {
                    // Fileは既に開かれている
                    window.Focus();
                }
                else
                {
                    window = EditorWindow.CreateWindow<AIGraphWindow>();
                }

                window.titleContent.text = name;
                window.path = path;
                window.IsDebugMode = false;
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
        public RawData raw;

        /// <summary>
        /// Nodeの基本情報をNodeDataに移す
        /// </summary>
        public NodeData()
        {
            raw = new RawData();
        }

        /// <summary>
        /// ノード内に実際に登録されたデータ
        /// </summary>
        [Serializable]
        public class RawData
        {
            [SerializeField] public List<SerializableKeyValuePair> keyValuePairs = new List<SerializableKeyValuePair>();

            /// <summary>
            /// Pairsからkeyを使ってvalueを取得する
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public bool GetFromPairs<T>(string key, out T value)
            {
                var searched = keyValuePairs.Find(p => p.key == key);
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
                var searched = keyValuePairs.Find(p => p.key == key);
                if (searched == null)
                    keyValuePairs.Add(new SerializableKeyValuePair(key, value));
                else
                    searched.SetValue(value);
            }

            /// <summary>
            /// PairsにDictionaryの形でvalueを入れる
            /// </summary>
            /// <param name="value"></param>
            public void SetDictionary(Dictionary<string, string> value)
            {
                keyValuePairs.Clear();
                value.ToList().ForEach(d =>
                {
                    keyValuePairs.Add(new SerializableKeyValuePair(d.Key, d.Value));
                });
            }

            /// <summary>
            /// Valueの要素数
            /// </summary>
            public int Count
            {
                get => keyValuePairs.Count;
            }

            /// <summary>
            /// 実データを保存するclass
            /// </summary>
            [Serializable]
            public class SerializableKeyValuePair
            {
                public readonly static string DefaultStringValue = "[\\Empty]";

                public string key;
                [NonSerialized] private bool boolean;
                [SerializeField] private string stringValue;
                [SerializeField] private Vector3 locationValue = Vector3.positiveInfinity;
                [SerializeField] private Sprite imageValue;
                [SerializeField] private UnityEngine.Object rawObject;
                [SerializeField] private float floatValue;
                [SerializeField] private int intValue;
                [SerializeField] private AnimationCurve animationCurve;

                const string BooleanPattern = "^(\\\\Boolean):\\((True|False)\\)";
                const string BooleanPatternTrue = "\\Boolean:(True)";
                const string BooleanPatternFalse = "\\Boolean:(False)";

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
                    if (value is bool b)
                    {
                        if (b)
                            stringValue = BooleanPatternTrue;
                        else
                            stringValue = BooleanPatternFalse;
                    }
                    else if (value is string s)
                        stringValue = s;
                    else if (value is Vector3 v)
                        locationValue = v;
                    else if (value is Sprite i)
                        imageValue = i;
                    else if (value is UnityEngine.Object o)
                        rawObject = o;
                    else if (value is float f)
                        floatValue = f;
                    else if (value is int num)
                        intValue = num;
                    else if (value is AnimationCurve curve)
                        animationCurve = curve;
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
                    intValue = int.MinValue;
                    animationCurve = null;
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
                    {
                        // Boolean用の
                        if (Regex.IsMatch(stringValue, BooleanPattern))
                        {
                            var match = Regex.Match(stringValue, "(True|False)");
                            if (match.Success && bool.TryParse(match.Value, out var boolean))
                                return (T)(object)boolean;
                            return (T)(object)false;
                        }
                        return (T)(object)stringValue;
                    }
                    else if (intValue != int.MinValue)
                        return (T)(object)intValue;
                    else if (animationCurve != null)
                        return (T)(object)animationCurve;

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