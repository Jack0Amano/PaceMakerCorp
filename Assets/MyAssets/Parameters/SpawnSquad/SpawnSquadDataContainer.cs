using UnityEngine.UIElements;
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
            data = _data.Find(d => d.EnemyLevel == level);
            if (data == null)
                data = _data.FindMax(d => d.EnemyLevel);
            return data != null;
        }

        /// <summary>
        /// 同一IDを持つSquadをすべて取得
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<SpawnSquadData> GetSpawnSquadsFromID(string id)
        {
            return squads.FindAll(s => s.ID == id);
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
    /// すぽーんする敵の部隊データ
    /// </summary>
    [Serializable]
    public class SpawnSquadData
    {
        [SerializeField] public string ID;
        [Tooltip("SquadのスポーンするMapLocation上の位置")]
        [SerializeField] public string location;
        [Tooltip("EnemyのLevel (周りのEnemyがPlayerに撃破されるごとにEnemyLevelの低いEnemyが適応される)")]
        [SerializeField] public int EnemyLevel;
        [Tooltip("周囲の敵を撃破されたときに低下していくenemyLevelが時間とともに回復するか")]
        [SerializeField] public bool AutoRecoveryLevel = false;
        [Tooltip("ストーリー上優先してエンカウントするSquadか")]
        [SerializeField, ReadOnly] public bool IsNecessaryForEvent;
        [Tooltip("スポーンの優先度合いのソート")]
        [SerializeField] public float sort;
        [Tooltip("Spawnさせるのにかかるコスト")]
        [SerializeField] public float cost;
        [Tooltip("Squadの戦うTacticsSceneのID")]
        [SerializeField] public string tacticsSceneID;
        [Tooltip("Spawnする部隊のencountID")]
        [SerializeField] public string encounmtSpawnID;
        [Tooltip("SawnするEnemyのUnits")]
        [SerializeField] public List<TileAndEnemiesPair> tileAndUnits;
        [Tooltip("Playerの出撃地点のTileID")]
        [SerializeField] public List<string> playerTileIDs;
        [Tooltip("撃破時の受領BaseExp")]
        [SerializeField] public int exp;

        /// <summary>
        /// Unitの実パラメーター
        /// </summary>
        public List<UnitData> AllUnitsData
        {
            get
            {
                if (_parameters == null)
                {
                    _parameters = new List<UnitData>();
                    if (tileAndUnits != null)
                    {
                        tileAndUnits.ForEach(tu =>
                        {
                            if (tu != null)
                                _parameters.AddRange(tu.UnitsData);
                        });
                    }
                }
                return _parameters;
            }
        }
        [NonSerialized] private List<UnitData> _parameters;

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
            return $"SpawnSquadData: Comm.{CommanderData} {UnitsData.Count} units on {location}, sort {sort}, tactics scene is {tacticsSceneID}";
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
                    if (_unitsData == null)
                    {
                        var allUnitsData = GameManager.Instance.StaticData.AllUnitsData;
                        _unitsData = new List<UnitData>();
                        UnitAndMovePairs.ForEach(i =>
                        {
                            if (allUnitsData.GetUnitFromID(i.UnitID, out var p))
                            {
                                var unit = new UnitData(p);
                                unit.RoutineWayIndex = i.RoutineWayIndex;
                                _unitsData.Add(unit);
                            }
                            else
                                Debug.LogWarning($"Invalid access unitID: {i}");
                        });
                    }

                    return _unitsData;
                }
            }
            [NonSerialized] private List<UnitData> _unitsData;

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
}