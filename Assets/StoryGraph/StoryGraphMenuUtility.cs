using UnityEngine;
using UnityEditor;
using System.IO;

namespace StoryGraph.Editor
{
    public class EventGraphMenuUtility : MonoBehaviour
    {

        static string fileName = "SampleStory.asset";

        [MenuItem("Assets/Create/Story/Story Graph")]
        private static void GenerateSampleScript()
        {
            int instanceID = Selection.activeInstanceID;
            string path = AssetDatabase.GetAssetPath(instanceID);
            path = Path.Combine(path, fileName);
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            StoryGraphSaveUtility.SaveNew(path);
            AssetDatabase.Refresh();
        }
    }
}