using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Parameters.Units
{
    /// <summary>
    /// Unitsのリストを保持するScriptableObjectのをEditorで生成するためのクラス
    /// </summary>
    public class AllUnitsMenuUtility : MonoBehaviour
    {
        static string fileName = "SampleUnits.asset";

        [MenuItem("Assets/Create/Parameters/Units")]
        private static void GenerateSampleScript()
        {
            int instanceID = Selection.activeInstanceID;
            string path = AssetDatabase.GetAssetPath(instanceID);
            path = Path.Combine(path, fileName);
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AllUnitsSaveUtility.SaveNew(path);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Unitsのリストを保持するScriptableObjectのContainerをLoad&Saveするためのクラス
    /// </summary>
    public class AllUnitsSaveUtility
    {
        private static readonly string directoryName = "AllUnits";

        public static void SaveNew(string path)
        {
            var graphData = ScriptableObject.CreateInstance<AllUnitsDataContainer>();
            AssetDatabase.CreateAsset(graphData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static AllUnitsDataContainer Load(string mapName)
        {
            var output = ScriptableObject.CreateInstance<AllUnitsDataContainer>();
            var path = Path.Combine(GameManager.StaticDataRootPath, mapName, directoryName);
            var _files = Directory.GetFiles(path, "*.asset");
            if (_files.Count() == 0)
                return output;
            var files = _files.ToList();

            foreach (var file in files)
            {
                try
                {
                    var container = AssetDatabase.LoadAssetAtPath<AllUnitsDataContainer>(file);
                    output.Combine(container);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Can't load {file}\n{e}");
                }
            }

            return output;
        }

        public static AllUnitsDataContainer LoadAll()
        {
            var sceneDirs = Directory.GetDirectories(GameManager.StaticDataRootPath);
            var output = ScriptableObject.CreateInstance<AllUnitsDataContainer>();
            foreach (var sceneDir in sceneDirs)
            {
                var dirName = Path.GetFileName(sceneDir);
                var data = Load(dirName);
                output.Combine(data);
            }
            return output;
        }
    }
}