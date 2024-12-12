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
    // NOTE ScriptableObject�͓����̃t�@�C���ɏ����Ȃ���΂Ȃ�Ȃ�
    /// <summary>
    /// ���ۂɕۑ������Serialiable��Object
    /// </summary>
    [Serializable]
    public class EventGraphDataContainer : ScriptableObject
    {
        /// <summary>
        /// Event��RootNode�Ŏw�肷��Event�ɌŗL��ID
        /// </summary>
        public string EventID = "";

        public List<NodeData> Nodes = new List<NodeData>();
        public List<EdgeData> Edges = new List<EdgeData>();

        public Vector3 viewPosition;
        public Vector3 viewScale = new Vector3(1, 1, 1);
        
        /// <summary>
        /// ScriptableObject���������t�@�C�����_�u���N���b�N����GrafView���J�����߂�
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

    #region �O���t�̕ۑ��p���f�[�^
    /// <summary>
    /// Node�̐ڑ��H Edge�ƌĂ�ł���?
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
    /// �m�[�h�̕ۑ��p�f�[�^
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
        /// Node�̊�{����NodeData�Ɉڂ�
        /// </summary>
        public NodeData()
        {
            Raw = new RawData();
        }

        /// <summary>
        /// �m�[�h���Ɏ��ۂɓo�^���ꂽ�f�[�^
        /// </summary>
        [Serializable]
        public class RawData
        {
            [SerializeField] public List<SerializableKeyValuePair> KeyValuePairs = new List<SerializableKeyValuePair>();

            /// <summary>
            /// Pairs����key���g����value���擾����
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
            /// Pairs��value��ݒu����
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
            /// Pairs��Dictionary�̌`��value������
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
            /// Value�̗v�f��
            /// </summary>
            public int Count
            {
                get => KeyValuePairs.Count;
            }

            /// <summary>
            /// ���f�[�^��ۑ�����class
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
                /// Pair�ɒl���Z�b�g���� ����value�͏�����
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
                /// ���ׂĂ�value����ɂ���
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
                /// Pair����l���擾����
                /// </summary>
                public T GetValue<T>()
                {

                    // stringValue��null�ɂ��Ă����Ă�asset����ǂݏo����length=0��string�ɂȂ邽�ߌ����͍Ō�
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