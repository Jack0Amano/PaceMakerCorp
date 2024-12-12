using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using static Utility;

/// <summary>
/// SaveDataの概要を記したファイル  MyArmyDataより先にloadすること
/// </summary>
[Serializable]
public class SaveDataInfo
{

    [Tooltip("セーブした時間")]
    [NonSerialized] public DateTime SaveTime;

    /// <summary>
    /// シリアライズ可能なセーブ時間
    /// </summary>
    [SerializeField] private SerializableDateTime serializableSaveTime;

    /// <summary>
    /// セーブされたゲーム内時間
    /// </summary>
    [Tooltip("セーブされたゲーム内時間")]
    [NonSerialized] public DateTime GameTime;
    /// <summary>
    /// シリアライズ可能なゲーム内時間
    /// </summary>
    [SerializeField] private SerializableDateTime serializableGameTime;

    [Tooltip("セーブデータの識別ID")]
    public string ID;

    [Tooltip("ゲームの難易度")]
    public GameDifficulty GameDifficulty;
    
    [Tooltip("資本金")]
    public int capitalFund;
    
    /// <summary>
    /// TacticsSceneでセーブした場合そのSceneの名前
    /// </summary>
    public string TacticsSceneName;
    /// <summary>
    /// ゲームタイプとなるメインマップのID NewGame時に指定し以降書き換えなし e.g. サイゴンマップだったら SaiGon
    /// </summary>
    public string MainMapSceneName;
    /// <summary>
    /// Shopの品出しの品目を決定するためのレベル
    /// </summary>
    public ShopLevel shopLevel;
    /// <summary>
    /// 編成している部隊の数
    /// </summary>
    public int SquadsCount;
    /// <summary>
    /// 所属している兵士の数
    /// </summary>
    public int UnitsCount;
    /// <summary>
    /// 各地点のパラメーター
    /// </summary>
    public List<LocationParamter> locationParameters = new List<LocationParamter>();
    /// <summary>
    /// ストーリーの進行度合い
    /// </summary>
    public List<StorySaveData> storiesSaveData = new List<StorySaveData>();

    /// <summary>
    /// Serialize不可なデータをSerializableにするための関数
    /// </summary>
    public void Serialize()
    {
        serializableGameTime = new SerializableDateTime(GameTime);
        serializableSaveTime = new SerializableDateTime(SaveTime);
    }

    /// <summary>
    /// Serializeされたデータを元に戻す
    /// </summary>
    public void Deserialize()
    {
        GameTime = serializableGameTime.Value;
        SaveTime = serializableSaveTime.Value;
    }

    /// <summary>
    /// セーブデータの整合性を確認する
    /// </summary>
    public void CheckData()
    {
        if (shopLevel == null)
            shopLevel = new ShopLevel();
    }


    public override string ToString()
    {
        return $"SaveDataInfo: directory_{ID}, time_{SaveTime}";
    }

    /// <summary>
    /// ストーリーイベントのセーブデータ
    /// </summary>  
    [Serializable] 
    public class StorySaveData
    {
        /// <summary>
        /// StoryのID
        /// </summary>
        public string StoryID;
        /// <summary>
        /// Story内で進行中のEventのData
        /// </summary>
        public List<EventSaveData> EventsData = new List<EventSaveData>();

        public StorySaveData(string id)
        {
            StoryID = id;
        }

        /// <summary>
        /// IDからEventDataを取得
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public EventSaveData GetEventDataFromID(string id)
        {
            var output = EventsData.Find(e => e.EventID == id);
            if (output != null)
                output = new EventSaveData(id);
            return output;
        }

        /// <summary>
        /// Eventのセーブデータ (各EventGraphに一つずつ作成される)
        /// </summary>
        [Serializable]
        public class EventSaveData
        {
            public EventSaveData(string id)
            {
                EventID = id;
            }

            /// <summary>
            /// EventのID
            /// </summary>
            public string EventID;
            /// <summary>
            /// 現在待機中のEventのNodeのID
            /// </summary>
            public string WaitingNodeID;
            /// <summary>
            /// Eventが終了しているか
            /// </summary>
            public State state = State.NotYet;

            /// <summary>
            /// DeepCopyを作成
            /// </summary>
            /// <returns></returns>
            public EventSaveData Clone()
            {
                return new EventSaveData(this.EventID)
                {
                    WaitingNodeID = this.WaitingNodeID,
                    state = this.state
                };
            }

            /// <summary>
            /// Eventの実施状況
            /// </summary>
            public enum State
            {
                NotYet,
                Running,
                Completed
            }

            public override string ToString()
            {
                return $"EventSaveData: EventID.{EventID}, WaitingNodeID.{WaitingNodeID}, State.{state}";
            }
        }
    }

        
}

/// <summary>
/// ゲーム難易度のenum 
/// </summary>
[Serializable]
public enum GameDifficulty
{
    Easy,
    Normal,
    Hard
}