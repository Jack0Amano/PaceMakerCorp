using UnityEngine;
using UnityEditor;
using System.IO;

namespace AIGraph.Editor
{
    public class AUGraphMenuUtility : MonoBehaviour
    {

        static string fileName = "AI.asset";

        [MenuItem("Assets/Create/AI Graph")]
        private static void GenerateSampleScript()
        {
            int instanceID = Selection.activeInstanceID;
            string path = AssetDatabase.GetAssetPath(instanceID);
            path = Path.Combine(path, fileName);
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AIGraphSaveUtility.SaveNew(path);
            AssetDatabase.Refresh();
        }
    }
}