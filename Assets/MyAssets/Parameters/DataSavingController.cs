using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using static Utility;
using UnityEditor;
using System.Linq;
using Unity.VisualScripting;
using MainMap;

/// <summary>
/// データSave関係のを全てこの関数内で行う
/// </summary>
public class DataSavingController
{

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
    /// SaveDataの一時保存
    /// </summary>
    internal SaveData TempSaveData { private set; get; }

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
    /// File名からSaveDataのPathを作成する
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string MakeSavePath(string fileName)
    {
        var extension = SaveDataExtension.Remove(0, 1);
        return Path.Combine(GameManager.SaveDataRootPath, fileName + extension);
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
        var path = MakeSavePath(SaveDataName);
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
    public void Save(string path, ReachedEventArgs reachedEventArgs=null)
    {
        if (!Directory.Exists(GameManager.SaveDataRootPath))
            Directory.CreateDirectory(GameManager.SaveDataRootPath);

        SaveDataInfo.SaveTime = DateTime.Now;
        //SaveDataInfo.ID = Path.GetFileName(path);
        var datetime = DateTime.Now;
        SaveData.Save(path, true, reachedEventArgs);
        // 処理にかかった時間
        var span = DateTime.Now - datetime;
        Print($"Save data to {path} in {span.TotalMilliseconds}ms {SaveData.DataInfo.GameTime}");

        // セーブ数が上限を超えている場合は古いものを削除する
        if (GameManager.Instance.StaticData.CommonSetting.SaveLimit > 0)
        {
            var infos = GetAllSavedData();
            if (infos.Count > GameManager.Instance.StaticData.CommonSetting.SaveLimit)
            {
                infos.Sort((a, b) => DateTime.Compare(a.data.DataInfo.SaveTime, b.data.DataInfo.SaveTime));
                RemoveSave(infos[0].path);
            }
        }
    }

    /// <summary>
    /// 現在の進行状態を新しいセーブとして保存する
    /// </summary>
    public string NewSave()
    {
        var path = MakeSavePath(SaveDataName);
        Save(path);
        return path;
    }

    /// <summary>
    /// TempSaveからSaveDataを作成して書き込む
    /// </summary>
    /// <returns></returns>
    public bool SaveFromTemp(string fileName="")
    {
        if (TempSaveData == null)
            return false;
        if (fileName == "")
            fileName = SaveDataName;
        var path = MakeSavePath(fileName);
        TempSaveData.Save(path);
        return true;
    }

    /// <summary>
    /// SaveDataをメモリ上に一時保存する
    /// </summary>  
    public void WriteAsTempData()
    {
        Print("Write as temp data at", GameManager.Instance.GameTime);
        SaveData.WriteInfo();
        TempSaveData = SaveData.DeepCopy();
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


