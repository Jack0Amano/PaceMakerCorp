using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Parameters.Units
{
    /// <summary>
    /// すべてのUnitの元データ  (MyArmyDataの中のUnitParameterと重複箇所ありだが、こちらは初期データのみで変更されない)
    /// </summary>
    [Serializable]
    public class AllUnitsDataContainer : ScriptableObject
    {
        [Header("Base unit data")]
        [Tooltip("Unitの基礎データ")]
        [SerializeField] public List<BaseUnitData> units;

        public AllUnitsDataContainer()
        {
            units = new List<BaseUnitData>();
        }

        /// <summary>
        /// 与えられたcontainerの内容を統合する
        /// </summary>
        /// <param name="container"></param>
        public void Combine(AllUnitsDataContainer container)
        {
            units.AddRange(container.units);
        }

        /// <summary>
        /// アクセスIDからUnitParameterを取得する
        /// </summary>
        /// <param name="id"></param>
        /// <param name="unitParameter"></param>
        /// <returns></returns>
        public bool GetUnitFromID(string id, out BaseUnitData unitParameter)
        {
            unitParameter = default;
            unitParameter = units.Find(u => u.ID == id);
            return unitParameter != null;
        }

        /// <summary>
        /// 空のDataContainerを作成する
        /// </summary>
        /// <returns></returns>
        public static AllUnitsDataContainer CreateEmptyData()
        {
            return ScriptableObject.CreateInstance<AllUnitsDataContainer>();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AllUnitsDataContainer))]
    public class AllUnitsDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AllUnitsDataContainer myTarget = (AllUnitsDataContainer)target;
            DrawDefaultInspector();
        }

        // UI Toolkit のカスタムエディタは CreateInspectorGUI をオーバーライドし、
        // VisualElementを戻り値として渡すことで表示を変更できます
        //public override VisualElement CreateInspectorGUI()
        //{
        // 　 // ここでUIToolBuilderを設定できる
        //    var treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Test.uxml");
        //    var container = treeAsset.Instantiate();
        //    return container;
        //}
    }
#endif
}