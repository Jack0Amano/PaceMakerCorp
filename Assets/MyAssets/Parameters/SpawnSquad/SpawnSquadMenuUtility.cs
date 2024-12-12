using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Parameters.SpawnSquad
{
    public class SpawnSquadMenuUtility : MonoBehaviour
    {
        static string fileName = "SampleSpawnSquad.asset";

        [MenuItem("Assets/Create/Parameters/Spawn Squad")]
        private static void GenerateSampleScript()
        {
            int instanceID = Selection.activeInstanceID;
            string path = AssetDatabase.GetAssetPath(instanceID);
            path = Path.Combine(path, fileName);
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            SpawnSquadSaveUtility.SaveNew(path);
            AssetDatabase.Refresh();
        }
    }

    public class SpawnSquadSaveUtility
    {
        private static readonly string directoryName = "Spawn";

        public static void SaveNew(string path)
        {
            var graphData = ScriptableObject.CreateInstance<SpawnSquadDataContainer>();
            AssetDatabase.CreateAsset(graphData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static SpawnSquadDataContainer Load(string mapName)
        {
            var output = ScriptableObject.CreateInstance<SpawnSquadDataContainer>();
            var path = Path.Combine(GameManager.StaticDataRootPath, mapName, directoryName);
            var _files = Directory.GetFiles(path, "*.asset");
            if (_files.Count() == 0)
                return output;
            var files = _files.ToList();

            foreach (var file in files)
            {
                try
                {
                    var container = AssetDatabase.LoadAssetAtPath<SpawnSquadDataContainer>(file);
                    output.Combine(container);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Can't load {file}\n{e}");
                }
            }

            return output;
        }

        public static SpawnSquadDataContainer LoadAll()
        {
            var sceneDirs = Directory.GetDirectories(GameManager.StaticDataRootPath);
            var output = ScriptableObject.CreateInstance<SpawnSquadDataContainer>();
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