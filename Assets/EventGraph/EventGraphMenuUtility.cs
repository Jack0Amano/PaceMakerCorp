using UnityEngine;
using UnityEditor;
using System.IO;

namespace EventGraph.Editor
{
    public class EventGraphMenuUtility : MonoBehaviour
    {

        static string fileName = "SampleEvent.asset";

        [MenuItem("Assets/Create/Story/Event Graph")]
        private static void GenerateSampleScript()
        {
            int instanceID = Selection.activeInstanceID;
            string path = AssetDatabase.GetAssetPath(instanceID);
            path = Path.Combine(path, fileName);
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            EventGraphSaveUtility.SaveNew(path);
            AssetDatabase.Refresh();
        }
    }
}