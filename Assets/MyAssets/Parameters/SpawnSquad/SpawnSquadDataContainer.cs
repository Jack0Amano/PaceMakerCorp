﻿using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using StoryGraph.Nodes;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections;
using Parameters.Units;
using AIGraph.Editor;
using Unity.VisualScripting.FullSerializer;
using static Utility;
using EventGraph.InOut;

namespace Parameters.SpawnSquad
{
    [Serializable]
    public class SpawnSquadDataContainer : ScriptableObject
    {

        public List<SpawnSquadData> squads;

        /// <summary>
        /// ScriptableObjectから作ったファイルをダブルクリックしてGrafViewを開くための
        /// </summary>
        /// <param name="instanceID"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var target = EditorUtility.InstanceIDToObject(instanceID);

            if (target is SpawnSquadDataContainer)
            {
                return true;
            }
            return false;
        }

        public SpawnSquadDataContainer()
        {
            squads = new List<SpawnSquadData>();
        }

        /// <summary>
        /// DataContainer内のSpawnSquadをIDから検索する
        /// </summary>
        /// <param name="id">SpawnSquadのID</param>
        /// <param name="level">Spawnする敵のlevel</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool GetSpawnSquadFromID(string id, int level, out SpawnSquadData data)
        {
            data = null;
            var _data = squads.FindAll(s => s.ID == id);
            if (_data.Count == 0)
                return false;
            if (_data.Count == 1)
            {
                data = _data[0];
                return true;
            }
            data = _data.Find(d => d.EnemyLevel == level);
            if (data == null)
                data = _data.FindMax(d => d.EnemyLevel);
            return data != null;
        }

        /// <summary>
        /// 同一IDを持つSquadをすべて取得
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Levelの異なるSquadが複数返される</returns>
        public List<SpawnSquadData> GetSpawnSquadsFromID(string id)
        {
            return squads.FindAll(s => s.ID == id);
        }

        /// <summary>
        /// IDとLevelからSpawnSquadを取得 (同じLevelがない場合は次に低いLevelのSquadを返す)
        /// </summary>
        /// <param name="id">SpawnSquadDataのID</param>
        /// <param name="level">指定レベル</param>
        /// <returns></returns>
        public SpawnSquadData GetSpawnSquadFromIDAndLevel(string id, int level)
        {
            
            var spawnSquads = GetSpawnSquadsFromID(id);
            spawnSquads.Sort((a, b) => a.EnemyLevel - b.EnemyLevel);
            SpawnSquadData data = null;
            data = spawnSquads.Find(s => s.EnemyLevel == level);
            if (data == null)
            {
                // 指定したLevelがない場合は次に低いLevelのSquadを返す
                spawnSquads.RemoveAll(s => s.EnemyLevel > level);
                if (spawnSquads.Count > 0)
                    data = spawnSquads[spawnSquads.Count - 1];
                else
                    PrintError($"SpawnSquadDataContainer: ID {id} has no level {level} squad");
            }
            return data;
        }

        /// <summary>
        /// 複数のContainerを一つに統合する
        /// </summary>
        /// <param name="container"></param>
        public void Combine(SpawnSquadDataContainer container)
        {
            squads.AddRange(container.squads);
        }

        /// <summary>
        /// 空のDataContainerを作成する
        /// </summary>
        /// <returns></returns>
        public static SpawnSquadDataContainer CreateEmptyData()
        {
            return ScriptableObject.CreateInstance<SpawnSquadDataContainer>();
        }
    }

    /// <summary>
    /// スポーンする敵の部隊データ
    /// </summary>
    [Serializable]
    public class SpawnSquadData
    {
        [SerializeField] public string ID;
        [Tooltip("EnemyのLevel (周りのEnemyがPlayerに撃破されるごとにEnemyLevelの低いEnemyが適応される)")]
        [SerializeField] public int EnemyLevel;
        [Tooltip("Spawnさせるのにかかるコスト")]
        [SerializeField] public int cost;
        [Tooltip("SawnするEnemyのUnits")]
        [SerializeField] public List<TileAndEnemiesPair> tileAndUnits;
        [Tooltip("撃破時の受領BaseExp")]
        [SerializeField] public int exp;

        /// <summary>
        /// Unitの実パラメーター
        /// </summary>
        public List<UnitData> AllUnitsData
        {
            get
            {
                if (allUnitsData == null)
                {
                    allUnitsData = new List<UnitData>();
                    tileAndUnits?.ForEach(tu =>
                    {
                        if (tu != null)
                            allUnitsData.AddRange(tu.UnitsData);
                    });
                }
                return allUnitsData;
            }
        }
        [NonSerialized] private List<UnitData> allUnitsData;

        public UnitData CommanderData
        {
            get => AllUnitsData.Find(p => p.Data.IsCommander);
        }

        public List<UnitData> UnitsData
        {
            get => AllUnitsData.FindAll(p => !p.Data.IsCommander);
        }

        public override string ToString()
        {
            return $"SpawnSquadData: Comm.{CommanderData} {UnitsData.Count} units";
        }

        /// <summary>
        /// TacticsにおいてUnitの初期タイルとその中のUnitのペア
        /// </summary>
        [Serializable] public class TileAndEnemiesPair
        {
            /// <summary>
            /// TacticsのTIleのID
            /// </summary>
            [SerializeField] public string tileID;

            /// <summary>
            /// Tileに入るUnitのID
            /// </summary>
            [SerializeField] public List<UnitAndMovePair> UnitAndMovePairs;

            /// <summary>
            /// Unitの実パラメーター
            /// </summary>
            public List<UnitData> UnitsData
            {
                get
                {
                    if (unitsData == null)
                    {
                        var allUnitsData = GameManager.Instance.StaticData.AllUnitsData;
                        unitsData = new List<UnitData>();
                        UnitAndMovePairs.ForEach(i =>
                        {
                            if (allUnitsData.GetUnitFromID(i.UnitID, out var p))
                            {
                                var unit = new UnitData(p);
                                unit.RoutineWayIndex = i.RoutineWayIndex;
                                unitsData.Add(unit);
                            }
                            else
                                Debug.LogWarning($"Invalid access unitID: {i}");
                        });
                    }

                    return unitsData;
                }
            }
            [NonSerialized] private List<UnitData> unitsData;

        }

        /// <summary>
        /// AIで動かすUnitのIDとその動作
        /// </summary>
        [Serializable] public class UnitAndMovePair
        {
            public UnitAndMovePair()
            {
            }

            public UnitAndMovePair(string unitID, int wayIndex = -1)
            {
                this.UnitID = unitID;
                RoutineWayIndex = wayIndex;
            }

            [Tooltip("AIで動かすSpawnしたUnitのID")]
            public string UnitID;
            [Tooltip("Unitが敵非発見状態で周回するWayのIndex")]
            public int RoutineWayIndex = -1;
        }
    }

    /// <summary>
    /// SpawnするSquadのリクエストデータ SaveDataに保存されるDataでもある
    /// </summary>
    [Serializable]
    public class SpawnRequestArgs: EventArgs
    {
        /// <summary>
        /// SpawnするSquadのBaseからのID
        /// </summary>
        public string SpawnSquadID;
        /// <summary>
        /// SpawnするSquadがFollowerの場合のBaseSquadID
        /// </summary>
        public string BaseSquadID;
        /// <summary>
        /// Spawnする位置のID
        /// </summary>
        public string LocationID;
        /// <summary>
        /// SquadのLevel
        /// </summary>
        public int Level;
        /// <summary>
        /// spawnする時間
        /// </summary>
        public SerializableDateTime SerializableSpawnTime;
        /// <summary>
        /// イベント用にスポーンが必須なSquadか
        /// </summary>
        public bool IsNecessaryForEvent;
        /// <summary>
        /// スポーンするSquadの優先度
        /// </summary>
        public int Priority;
        /// <summary>
        /// TacticsSceneを指定して出す場合のID (無しならLocationのDefaultTacticsSceneIDになる)
        /// </summary>
        public string SpecificTacticsSceneID = "";

        /// <summary>
        /// TacticsSceneで戦闘が始まる位置
        /// </summary>
        public StartPosition StartPosition;

        /// <summary>
        /// SquadDataを検索してminiPrefabを取得 miniPrefabはレベル共通のため最初に取得したものをキャッシュする
        /// </summary>
        public GameObject MiniPrefab
        {
            get
            {
                if (miniPrefab == null)
                {
                    if (GameManager.Instance.StaticData.AllSpawnSquads.GetSpawnSquadFromID(SpawnSquadID, 0, out var data))
                    {
                        miniPrefab = data.CommanderData.Data.MiniPrefab;
                    }
                    else
                    {
                        PrintError($"SpawnRequestArgs: SpawnSquadID {SpawnSquadID} is not found");
                    }
                }
                return miniPrefab;
            }
        }
        private GameObject miniPrefab;

        /// <summary>
        /// Spawnする時間
        /// </summary>
        public DateTime SpawnTime
        {
            get => SerializableSpawnTime.Value;
            set => SerializableSpawnTime = value;
        }

        public SpawnRequestArgs()
        {
            SerializableSpawnTime = new SerializableDateTime();
        }

        public SpawnRequestArgs(SpawnEventOutput spawnEventOutput, DateTime spawnTime)
        {
            SpawnSquadID = spawnEventOutput.SquadID;
            BaseSquadID = spawnEventOutput.BaseSquadID;
            LocationID = spawnEventOutput.LocationID;
            SpecificTacticsSceneID = spawnEventOutput.SpecificTacticsSceneID;
            Level = spawnEventOutput.Level;
            SerializableSpawnTime = new SerializableDateTime(spawnTime);
            IsNecessaryForEvent = spawnEventOutput.IsNecessaryForEvent;
            Priority = spawnEventOutput.Priority;
            StartPosition = spawnEventOutput.StartPosition;
        }
    }
}