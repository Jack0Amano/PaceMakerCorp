using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using static Utility;
using UnityEditor;
using Parameters.SpawnSquad;
using UnityEditor.VersionControl;
using MainMap;

[Serializable]
public class SaveData
{
    [Header("セーブデータの概要")]
    /// <summary>
    /// セーブデータの概要
    /// </summary>
    public SaveDataInfo DataInfo;
    [Header("自軍のデータ")]
    /// <summary>
    /// 自軍のデータ
    /// </summary>
    public MyArmyData MyArmyData;
    [Header("スポーンしている敵Squadの情報")]
    /// <summary>
    /// スポーンしている敵Squadの情報
    /// </summary>
    public List<SpawnRequestArgs> SpawnData = new List<SpawnRequestArgs>();
    [Header("エンカウント時のセーブデータ")]
    [Tooltip("エンカウント時のセーブデータ")]
    public ReachedEventArgs ReachedEventArgs;

    /// <summary>
    /// Tactics状態でセーブしたデータが存在するか
    /// </summary>
    public bool HasTacticsData
    {
        get
        {
            return ReachedEventArgs != null && ReachedEventArgs.Player.ID != "";
        }
    }

    #region Load
    /// <summary>
    /// デフォルトデータをDefault.(mapSceneID).Default.jsonから読み込む
    /// </summary>
    /// <param name="mapSceneID"></param>
    public static SaveData LoadDefaultFromJson(string mapSceneID)
    {
        var path = Path.Combine(GameManager.StaticDataRootPath, mapSceneID,"Default", "Default.json");
        var jsonStr = File.ReadAllText(path);
        var output = JsonUtility.FromJson<SaveData>(jsonStr);

        // output.CheckData();

        return output;
    }

    /// <summary>
    /// デフォルトデータをDefault.(mapSceneID).Default.assetから読み込む
    /// </summary>
    /// <param name="mapSceneID"></param>
    /// <returns></returns>
    public static SaveData LoadDefaultFromAsset(string mapSceneID)
    {
        var path = Path.Combine(GameManager.StaticDataRootPath, mapSceneID, "Default", "Default.asset");
        var assetData = AssetDatabase.LoadAssetAtPath<SaveDataContainer>(path).ObjectDeepCopy();
        var output = assetData.SaveData;
        output.DataInfo.shopLevel ??= new ShopLevel();
        output.DataInfo.Deserialize();

        return output;
    }

    /// <summary>
    /// DirectoryからSaveData.binを読み出す
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    public static SaveData Load(string path)
    {
        // path = AssetDatabase.GenerateUniqueAssetPath(path);
        var extension = Path.GetExtension(path).ToLower();
        var output = extension == ".asset" ? LoadAsset(path) : LoadBinary(path);

        // output.CheckData();

        return output;
    }

    /// <summary>
    /// バイナリ型のセーブデータを読み込む
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static SaveData LoadBinary(string path)
    {
        var output = Binary.LoadFrom<SaveData>(path);
        output.DataInfo.shopLevel ??= new ShopLevel();
        output.DataInfo.Deserialize();
        return output;
    }

    ///<summary>
    /// シリアライズされたassetデータ型のセーブデータを読み込む
    /// </summary>
    private static SaveData LoadAsset(string path)
    {
        var assetData = AssetDatabase.LoadAssetAtPath<SaveDataContainer>(path);
        var output = assetData.SaveData;
        output.DataInfo.shopLevel ??= new ShopLevel();
        output.DataInfo.Deserialize();
        return output;
    }
    #endregion

    #region Save
    /// <summary>
    /// Pathにセーブする
    /// </summary>
    /// <param name="path"></param>
    /// <exception cref="Exception"></exception>
    public void Save(string path, bool updateDataInfo=true, ReachedEventArgs reachedEventArg=null)
    {
        this.CheckData();
        ReachedEventArgs = reachedEventArg;
        if (updateDataInfo)
            WriteInfo();

        var extension = Path.GetExtension(path).ToLower();
        if (extension == ".asset")
            SaveToAsset(path);
        else
            SaveToBinary(path);
    }

    /// <summary>
    /// DataInfoに現在のゲームの状態をセットする
    /// </summary>
    public void WriteInfo()
    {
        // DataInfoにゲームの各種状態をセット
        DataInfo.GameTime = GameManager.Instance.GameTime.DeepCopy();
        Print(DataInfo.GameTime);
        DataInfo.SaveTime = DateTime.Now;
        DataInfo.UnitsCount = MyArmyData.Units.Count;
        DataInfo.SquadsCount = MyArmyData.Squads.Count;
        DataInfo.Serialize();
    }

    /// <summary>
    /// バイナリファイルにデータをセーブする
    /// </summary>
    /// <param name="path"></param>
    private void SaveToBinary(string path)
    {
        Binary.SaveTo(this, path);
    }

    /// <summary>
    /// Assetにデータをセーブする
    /// </summary>
    /// <param name="path"></param>
    private void SaveToAsset(string exportPath)
    {
        Print("save to asset");
        var asset = ScriptableObject.CreateInstance<SaveDataContainer>();
        asset.SaveData = this;

        // アセットが存在しない場合はそのまま作成(metaファイルも新規作成).
        if (!File.Exists(exportPath))
        {
            AssetDatabase.CreateAsset(asset, exportPath);
            return;
        }

        // 仮ファイルを作るためのディレクトリを作成.
        var fileName = Path.GetFileName(exportPath);
        var tmpDirectoryPath = Path.Combine(exportPath.Replace(fileName, ""), "tmp");
        Directory.CreateDirectory(tmpDirectoryPath);

        // 仮ファイルを保存.
        var tmpFilePath = Path.Combine(tmpDirectoryPath, fileName);
        AssetDatabase.CreateAsset(asset, tmpFilePath);

        // 仮ファイルを既存のファイルに上書き(metaデータはそのまま).
        FileUtil.ReplaceFile(tmpFilePath, exportPath);

        // 仮ディレクトリとファイルを削除.
        AssetDatabase.DeleteAsset(tmpDirectoryPath);

        // データ変更をUnityに伝えるためインポートしなおし.
        AssetDatabase.ImportAsset(exportPath);
    }
    #endregion

    /// <summary>
    /// Pathに新たなEmptyのSaveDataを作成する
    /// </summary>
    /// <param name="path"></param>
    public static SaveData CreateEmptyData()
    {
        var data = new SaveData();
        data.DataInfo = new SaveDataInfo();
        data.MyArmyData = new MyArmyData();
        return data;
    }

    /// <summary>
    /// セーブデータの整合性をチェックする
    /// </summary>
    public void CheckData()
    {
        DataInfo.CheckData();
        if (DataInfo == null)
            throw new Exception("DataInfo is null. It hasn't loaded yet.");
        MyArmyData.CheckData();
    }
}
