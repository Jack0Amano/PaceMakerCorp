using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using static Utility;
using UnityEditor;
using System.Linq;

/// <summary>
/// データSave関係のを全てこの関数内で行う
/// </summary>
public class DataSavingController
{

    public static readonly string AutoSaveDirectory = "AutoSave";
    public static readonly string SaveDataName = "SaveData";

    /// <summary>
    /// セーブファイルの拡張子
    /// </summary>
    public static readonly string SaveDataExtension = "*.save";

    internal SaveData SaveData { private set; get; }
    /// <summary>
    /// 自軍のデータの実データ
    /// </summary>
    internal MyArmyData MyArmyData { get => SaveData.MyArmyData; }
    /// <summary>
    /// セーブデータの諸々を入れる実データ
    /// </summary>
    internal SaveDataInfo SaveDataInfo { get => SaveData.DataInfo; }
    /// <summary>
    /// データが既に読み込まれているか
    /// </summary>
    public bool HasDataLoaded
    {
        get
        {
            if (SaveData == null)
                return false;
            var info = SaveDataInfo != null;
            var army = MyArmyData != null;
            return info && army;
        }
    }

    /// <summary>
    /// 最新のデータをロードする
    /// </summary>
    public void LoadNewerData()
    {
        var files = Directory.GetFiles(GameManager.SaveDataRootPath, SaveDataExtension);
        var infos = new List<(SaveData data, string path)>();
        Print($"{files.Count()} save data files exist");
        foreach (var path in files)
        {
            //try
            //{
            //    var saveDataInfo = SaveData.Load(path);
            //    infos.Add((saveDataInfo, path));
            //}
            //catch (Exception e)
            //{
            //    Print(e);
            //    continue;
            //}
            var saveData = SaveData.Load(path);
            infos.Add((saveData, path));
        }
        infos.Sort((a, b) => DateTime.Compare(b.data.DataInfo.SaveTime, a.data.DataInfo.SaveTime));
        Print($"Load new data: {infos[0].data.DataInfo.ID}, {infos[0].path}");
        SaveData = infos[0].data;
        SaveData.DataInfo.Deserialize();
    }

    /// <summary>
    /// 指定したディレクトリからロードする
    /// </summary>
    /// <param name="path"></param>
    public void Load(string path)
    {
        SaveData = SaveData.Load(path);
        SaveData.DataInfo.Deserialize();
    }

    /// <summary>
    /// 指定したデータをロードする
    /// </summary>
    /// <param name="data"></param>
    public void Load(SaveData data)
    {
        SaveData = data;
        SaveData.DataInfo.Deserialize();
    }

    /// <summary>
    /// Defaultから新規データを作成する (新しいデータを作るだけでセーブはされない)
    /// </summary>
    public string MakeNewGameData(string mapSceneID, GameDifficulty gameDifficulty)
    {
        Print($"Make new data of {mapSceneID}, difficulty: {gameDifficulty}");
        var extension = SaveDataExtension.Remove(0, 1);
        var path = Path.Combine(GameManager.SaveDataRootPath, SaveDataName + extension);
        Directory.CreateDirectory(GameManager.SaveDataRootPath);

        SaveData = SaveData.LoadDefaultFromAsset(mapSceneID);

        // セーブの概要を示したファイル
        SaveDataInfo.SaveTime = DateTime.Now;
        SaveDataInfo.MainMapSceneName = mapSceneID;
        SaveDataInfo.ID = GetUUID();
        SaveDataInfo.GameDifficulty = gameDifficulty;

        return path;
    }

    /// <summary>
    /// Pathに上書きセーブ
    /// </summary>
    public void Save(string path)
    {
        if (!Directory.Exists(GameManager.SaveDataRootPath))
            Directory.CreateDirectory(GameManager.SaveDataRootPath);

        SaveDataInfo.SaveTime = DateTime.Now;
        //SaveDataInfo.ID = Path.GetFileName(path);

        SaveData.Save(path);
    }

    /// <summary>
    /// 現在の進行状態を新しいセーブとして保存する
    /// </summary>
    public string NewSave()
    {
        var extension = SaveDataExtension.Remove(0, 1);
        var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(GameManager.SaveDataRootPath, SaveDataName + extension));
        Save(path);
        return path;
    }

    /// <summary>
    /// オートセーブディレクトリに保存する
    /// </summary>
    public void AutoSave()
    {
        var directory = Path.Combine(GameManager.SaveDataRootPath, AutoSaveDirectory);
        Save(directory);
    }

    /// <summary>
    /// セーブデータを削除する
    /// </summary>
    public void RemoveSave(string path)
    {
        File.Delete(path);
    }

    /// <summary>
    /// セーブされているすべてのデータを取得する
    /// </summary>
    /// <returns></returns>
    public List<(SaveData data, string path)> GetAllSavedData()
    {
        var pathes = Directory.GetFiles(GameManager.SaveDataRootPath, SaveDataExtension);
        Print($"{pathes.Count()} save data files exist");
        var output = new List<(SaveData, string)>();
        foreach (var path in pathes)
        {
            try
            {
                var saveData = SaveData.Load(path);
                output.Add((saveData, path));
            }
            catch (Exception e)
            {
                Print(e);
                continue;
            }
        }
        return output;
    }
}


