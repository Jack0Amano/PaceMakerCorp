using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Utility;

public class SaveDataUtility : MonoBehaviour
{

    [MenuItem("Assets/Data Debugger/Parse from data")]
    private static void ParseFromBin()
    {
        int instanceID = Selection.activeInstanceID;
        string path = AssetDatabase.GetAssetPath(instanceID);
        var extensionTemplate = DataSavingController.SaveDataExtension.Remove(0, 1);
        if (Path.GetExtension(path) != extensionTemplate)
        {
            print($"ExtensionError: Can't parse from {path}. Wrong extension '{Path.GetExtension(path)}', must be '{extensionTemplate}'");
            return;
        }

        try
        {
            var data = SaveData.Load(path);
            var container = ScriptableObject.CreateInstance<SaveDataContainer>();
            container.SaveData = data;
            var newPath = Path.ChangeExtension(path, "asset");
            newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);
            AssetDatabase.CreateAsset(container, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch
        {
            print($"Can't parse from bin : {path}");
        }
    }

    [MenuItem("Assets/Data Debugger/Pack to data")]
    public static void PackToBin()
    {
        int instanceID = Selection.activeInstanceID;
        string path = AssetDatabase.GetAssetPath(instanceID);
        if (Path.GetExtension(path) != ".asset")
        {
            print($"ExtensionError: Can't pack asset file to ({DataSavingController.SaveDataExtension})");
            return;
        }

        try
        {
            var container = AssetDatabase.LoadAssetAtPath<SaveDataContainer>(path);
            container.SaveData.DataInfo.ID = GetUUID();
            var newPath = Path.ChangeExtension(path, DataSavingController.SaveDataExtension);
            newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);
            Binary.SaveTo(container.SaveData, newPath);
            AssetDatabase.Refresh();
        }
        catch
        {
            print($"Can't pack asset file to bin");
        }
    }

    /// <summary>
    /// SaveDataのassetファイルを作成する
    /// </summary>
    [MenuItem("Assets/Data Debugger/Create/Default SaveData")]
    private static void CreateEmptySaveData()
    {
        const string fileName = "Default.asset";

        int instanceID = Selection.activeInstanceID;
        string path = AssetDatabase.GetAssetPath(instanceID);
        path = Path.Combine(path, fileName);
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        var data = SaveData.CreateEmptyData();
        var container = ScriptableObject.CreateInstance<SaveDataContainer>();
        container.SaveData = data;
        AssetDatabase.CreateAsset(container, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// ItemsDataのファイルを作成する
    /// </summary>
    [MenuItem("Assets/Data Debugger/Create/ItemsData")]
    private static void CreateEmptyItemsData()
    {
        const string fileName = "ItemsData.asset";

        int instanceID = Selection.activeInstanceID;
        string path = AssetDatabase.GetAssetPath(instanceID);
        path = Path.Combine(path, fileName);
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        var container = Parameters.Items.AllItemsDataContainer.CreateEmptyData();
        AssetDatabase.CreateAsset(container, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// UnitsDataのファイルを作成する
    /// </summary>
    [MenuItem("Assets/Data Debugger/Create/UnitsData")]
    private static void CreateEmptyUnitsData()
    {
        const string fileName = "UnitsData.asset";

        int instanceID = Selection.activeInstanceID;
        string path = AssetDatabase.GetAssetPath(instanceID);
        path = Path.Combine(path, fileName);
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        var container = Parameters.Units.AllUnitsDataContainer.CreateEmptyData();
        AssetDatabase.CreateAsset(container, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// UnitsDataのファイルを作成する
    /// </summary>
    [MenuItem("Assets/Data Debugger/Create/SpawnSquadsData")]
    private static void CreateEmptySpawnSquadsData()
    {
        const string fileName = "SpawnSquadsData.asset";

        int instanceID = Selection.activeInstanceID;
        string path = AssetDatabase.GetAssetPath(instanceID);
        path = Path.Combine(path, fileName);
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        var container = Parameters.SpawnSquad.SpawnSquadDataContainer.CreateEmptyData();
        AssetDatabase.CreateAsset(container, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
