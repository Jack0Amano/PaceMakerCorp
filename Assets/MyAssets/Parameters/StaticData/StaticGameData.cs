using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using Parameters.Units;
using Parameters.Items;
using Parameters.SpawnSquad;
using static Utility;
using JetBrains.Annotations;

/// <summary>
/// Staticなゲームデータを一括管理
/// </summary>
public class StaticGameData
{
    public string MapName { get; private set;}

    /// <summary>
    /// すべてのEquipment
    /// </summary>
    public AllItemsDataContainer AllItemsList { private set; get; }
    /// <summary>
    /// すべてのUnitの元データ  (MyArmyDataの中のUnitParameterと重複箇所ありだが、こちらは初期データのみで変更されない)
    /// </summary>
    public AllUnitsDataContainer AllUnitsData { private set; get; }
    /// <summary>
    /// すべてのSpawnする可能性のある部隊
    /// </summary>
    public SpawnSquadDataContainer AllSpawnSquads { private set; get; }
    /// <summary>
    /// 設定や言語などゲーム内で統一のデータ
    /// </summary>
    public CommonSetting CommonSetting { private set; get; }
    
    /// <summary>
    /// すべてのStaticなゲームデータをロードする
    /// </summary>
    /// <returns></returns>
    public static StaticGameData Load()
    {
        var output = new StaticGameData();
        output.CommonSetting = CommonSetting.Load();

        return output;
    }

    /// <summary>
    /// MapScene固有のデータのロード
    /// </summary>
    public void LoadStaticSceneData(string mapName)
    {
        MapName = mapName;
        AllItemsList = AllItemsSaveUtility.Load(mapName);
        AllUnitsData = AllUnitsSaveUtility.Load(mapName);
        AllSpawnSquads = SpawnSquadSaveUtility.Load(mapName);
    }

    /// <summary>
    /// MapScene固有データの再ロード
    /// </summary>
    public void ReloadStaticSceneData()
    {
        LoadStaticSceneData(MapName);
    }
    
    /// <summary>
    /// ディレクトリに存在するすべての固有データのロード
    /// </summary>
    public void LoadAllStaticSceneData()
    {
        AllItemsList = AllItemsSaveUtility.LoadAll();
        AllUnitsData = AllUnitsSaveUtility.LoadAll();
        AllSpawnSquads = SpawnSquadSaveUtility.LoadAll();
    }
}


/// <summary>
/// ゲームの操作や言語など全てで共通のパラメーター
/// </summary>
[Serializable]
public class CommonSetting
{

    public static CommonSetting Load()
    {
        var path = Path.Combine(GameManager.SaveDataRootPath, "Setting.json");
        string jsonStr;
        if (!File.Exists(path))
        {
            var common = new CommonSetting();
            jsonStr = JsonUtility.ToJson(common);
            File.WriteAllText(path, jsonStr);
            return common;
        }
        jsonStr = File.ReadAllText(path);
        return JsonUtility.FromJson<CommonSetting>(jsonStr);
    }

    public void Save()
    {
        var path = Path.Combine(GameManager.SaveDataRootPath, "Setting.json");
        var jsonStr = JsonUtility.ToJson(this);
        File.WriteAllText(path, jsonStr);
    }

    public string language = "English";
    /// <summary>
    /// followCameraの位置がRightかLeftを中心にしているか
    /// </summary>
    public bool IsFollowCameraCenterRight = true;
}