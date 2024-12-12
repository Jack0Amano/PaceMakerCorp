using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Parameters.MapData 
{
    /// <summary>
    /// MapDataのリストを保持するScriptableObjectのをEditorで生成するためのクラス
    /// </summary>
    public class MapDataMenuUtility : MonoBehaviour
    {
        static readonly string fileName = "AllMapData.asset";

        [MenuItem("Assets/Create/Parameters/MapData")]
        private static void GenerateSampleScript()
        {
            int instanceID = Selection.activeInstanceID;
            string path = AssetDatabase.GetAssetPath(instanceID);
            path = Path.Combine(path, fileName);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            
            MapDataSaveUtility.SaveNew(path);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// MapDataのリストを保持するScriptableObjectのContainerをLoad&Saveするためのクラス
    /// </summary>
    public class MapDataSaveUtility
    {
        static readonly string fileName = "AllMapData.asset";

        /// <summary>
        /// 新たなMapDataContainerを作成する
        /// </summary>
        /// <param name="path"></param>
        public static void SaveNew(string path)
        {
            var graphData = ScriptableObject.CreateInstance<MapDataContainer>();
            AssetDatabase.CreateAsset(graphData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// MapDataContainerをロードする
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static MapDataContainer Load()
        {

            var path = Path.Combine(GameManager.StaticDataRootPath, fileName);
            try
            {
                return AssetDatabase.LoadAssetAtPath<MapDataContainer>(path);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            
            throw new Exception("No MapDataContainer found");
        }
    }
}

