using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Utility;

namespace Parameters.Items
{
    public class AllItemsMenuUtility : MonoBehaviour
    {
        static string fileName = "SampleItems.asset";

        [MenuItem("Assets/Create/Parameters/Items")]
        private static void GenerateSampleScript()
        {
            int instanceID = Selection.activeInstanceID;
            string path = AssetDatabase.GetAssetPath(instanceID);
            path = Path.Combine(path, fileName);
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AllItemsSaveUtility.SaveNew(path);
            AssetDatabase.Refresh();
        }
    }

    public class AllItemsSaveUtility
    {
        private static readonly string directoryName = "AllItems";

        public static void SaveNew(string path)
        {
            var graphData = ScriptableObject.CreateInstance<AllItemsDataContainer>();
            AssetDatabase.CreateAsset(graphData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static AllItemsDataContainer Load(string mapName)
        {
            var output = ScriptableObject.CreateInstance<AllItemsDataContainer>();
            var path = Path.Combine(GameManager.StaticDataRootPath, mapName, directoryName);
            var _files = Directory.GetFiles(path, "*.asset");
            if (_files.Count() == 0)
                return output;
            var files = _files.ToList();

            foreach (var file in files)
            {
                try
                {
                    var container = AssetDatabase.LoadAssetAtPath<AllItemsDataContainer>(file);
                    output.Combine(container);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Can't load {file}\n{e}");
                }
            }

            return output;
        }

        public static AllItemsDataContainer LoadAll()
        {
            var sceneDirs = Directory.GetDirectories(GameManager.StaticDataRootPath);
            var output = ScriptableObject.CreateInstance<AllItemsDataContainer>();
            foreach(var sceneDir in sceneDirs)
            {
                var dirName = Path.GetFileName(sceneDir);
                var data = Load(dirName);
                output.Combine(data);
            }
            return output;
        }
    }
}