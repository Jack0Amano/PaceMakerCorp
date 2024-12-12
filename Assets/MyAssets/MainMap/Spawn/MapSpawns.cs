using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AddressableAssets;
using static Utility;
using Parameters.SpawnSquad;
using MainMap.Roads;
using Parameters.Units;

namespace MainMap.Spawn
{
    /// <summary>
    /// この中に敵ユニットがスポーンする
    /// </summary>
    public class MapSpawns : MonoBehaviour
    {

        [SerializeField] MapLocations mapLocations;

        /// <summary>
        /// 実際にスポーンしているオブジェクトのコンポーネントclass
        /// </summary>
        private readonly List<SpawnSquad> squads = new List<SpawnSquad>();

        private SpawnSquad encountSquad;

        private readonly float distanceToNearSquad = 2;
        /// <summary>
        /// 高速エンカウント等を防ぐためのエンカウントロック 主にTactics画面から戻ってきた際に未だ撃破した敵が存在するためそれを避ける
        /// </summary>
        private bool encountLock = false;
        /// <summary>
        /// Savedデータの敵の内容はリアルタイム（スポーン、デスポーン）と同時に更新されている
        /// </summary>
        private DataSavingController DataSavingController;
        /// <summary>
        /// すべてのSpawnsが止まっているか
        /// </summary>
        public bool StopAll
        {
            get => _StopAll;
            set
            {
                _StopAll = value;
                squads.ForEach(s =>
                {
                    if (!s.stopAnimation)
                        s.stopAnimation = value;
                });
            }
        }
        private bool _StopAll = false;

        protected void Awake()
        {
            DataSavingController = GameManager.Instance.DataSavingController;
            GameManager.Instance.EventSceneController.SpawnSquadRequest += ((s) =>
            {
                if (!GameManager.Instance.StaticData.AllSpawnSquads.GetSpawnSquadFromID(s, 3, out var data))
                {
                    return;
                }
                SpawnRequest(data, true);
            }) ;
            GameManager.Instance.AddTimeEventHandlerAsync += CalledWhenAddTime;
        }

        protected void Start()
        {

        }

        /// <summary>
        /// データの読み出しが完了した際にMainMapControllerから呼び出し
        /// </summary>
        internal void CompleteToLoadData()
        {
            SetSquadsFromSaveData();
        }

        /// <summary>
        /// セーブデータのSpawnSquadをすべてスポーンさせる
        /// </summary>
        public void SetSquadsFromSaveData()
        {
            if (DataSavingController.SaveData.SpawnData == null) return;

            DataSavingController.SaveData.SpawnData.ForEach(squad =>
            {
                if (squad.tacticsSceneID == null || squad.tacticsSceneID.Length == 0)
                {
                    PrintWarning($"TacticsScene id is not set at {squad}");
                }
                else if (squad.AllUnitsData.Count == 0)
                {
                    PrintWarning($"No units in squad: {squad}");
                }
                else if (squad.playerTileIDs.Count == 0)
                {
                    PrintWarning($"Not start position for player: {squad}");
                }
                else
                {
                    SpawnRequest(squad, false);
                }
            });
        }

        /// <summary>
        /// EventからEnemyをスポーンさせるとき呼び出し
        /// </summary>
        /// <param name="data"></param>
        private void SpawnRequest(SpawnSquadData data, bool saveSquad)
        {
            var location = mapLocations.locations.Find(l => l.id == data.location);
            if (location == null)
            {
                PrintWarning($"SpawnRequest of ({data}) is not located");
                return;
            }
            else if (data.CommanderData == null)
            {
                PrintWarning($"CommanderData is null at {data}");
                return;
            }
            
            if (location.SpawnSquadOnLocation != null)
            {
                // DataがSpawnする場所に既にSquadがいる
                if (location.SpawnSquadOnLocation.data.IsNecessaryForEvent)
                {
                    PrintWarning($"NecessarySquad is still exist \nold {location.SpawnSquadOnLocation.data} \n{data}");
                    return;
                }
                if (data.IsNecessaryForEvent || data.sort > location.SpawnSquadOnLocation.data.sort)
                {
                    DespawnSquad(location);
                    StartCoroutine( SetSquad(data, location, saveSquad));
                    return;
                }
            }
            else
            {
                StartCoroutine(SetSquad(data, location, saveSquad));
            }
        }

        /// <summary>
        /// Hierarchy上にSquadのObjectを作成し、その部隊のParameterをsetする
        /// </summary>
        /// <returns></returns>
        private IEnumerator SetSquad(SpawnSquadData squadData, MapLocation location, bool saveSquad = true)
        {
            var obj = Instantiate(squadData.CommanderData.MiniPrefab, location.transform.position, transform.rotation, transform);
            var mapSquad = obj.AddComponent<SpawnSquad>();
            mapSquad.data = squadData;
            mapSquad.spawnPosition = location.transform.position;
            mapSquad.baseLocation = location.BaseLocation;
            mapSquad.baseLocation.Data.defencePoint -= squadData.cost;
            squads.Add(mapSquad);

            location.SpawnSquadOnLocation = mapSquad;
            location.Data.PreviousRecoveryDate = GameManager.Instance.GameTime;

            if (saveSquad)
                DataSavingController.SaveData.SpawnData.Add(squadData);

            yield break;
        }

        /// <summary>
        /// デスポーンさせる
        /// </summary>
        /// <param name="location"></param>
        private void DespawnSquad(MapLocation location)
        {
            Destroy(location.SpawnSquadOnLocation.gameObject);
            squads.Remove(location.SpawnSquadOnLocation);
            DataSavingController.SaveData.SpawnData.Remove(location.SpawnSquadOnLocation.data);
            location.BaseLocation.Data.defencePoint += location.SpawnSquadOnLocation.data.cost;
        }
        
        /// <summary>
        /// encountSquadsに入っているSquadsをデスポーンさせる
        /// </summary>
        /// <param name="reduceBaseCampEnemyPower">敵が撃破されたためこのEnemyの拠点としている部隊のenemyLevelを下げる</param>
        public MapLocation DespawnEncountSquads(bool reduceBaseCampEnemyPower)
        {
            var loc = mapLocations.locations.Find(l => l.SpawnSquadOnLocation == encountSquad);

            if(reduceBaseCampEnemyPower && loc.BaseLocation != null && loc.BaseLocation.SpawnSquadOnLocation != null)
            {
                ReduceSquadPower(loc.BaseLocation.SpawnSquadOnLocation);
            }

            if (loc != null)
                loc.SpawnSquadOnLocation = null;
            DataSavingController.SaveData.SpawnData.Remove(encountSquad.data);
            squads.Remove(encountSquad);
            Destroy(encountSquad.gameObject);
            encountSquad = null;
            encountLock = false;

            return loc;
        }

        /// <summary>
        /// エンカウントした際に呼び出される
        /// </summary>
        /// <param name="spawnSquad"></param>
        /// <param name="mapSquad"></param>
        internal void Encount(SpawnSquad spawnSquad, MapSquad mapSquad)
        {
            encountSquad = spawnSquad;
        }

        /// <summary>
        /// 時間が進んだときの呼び出し
        /// </summary>
        /// <param name="o"></param>
        private void CalledWhenAddTime(object o, EventArgs e)
        {
            var current = GameManager.Instance.GameTime;
            foreach(var l in mapLocations.locations)
            {
                if (l.SpawnSquadOnLocation == null) continue;
                if (l.Data.HourToRecoverPower == 0) continue;
                if ((current - l.Data.PreviousRecoveryDate).TotalHours > l.Data.HourToRecoverPower)
                {
                    // 一定時間経ったためSpawnSquadのlevelを上げる
                    IncreaseSquadPower(l.SpawnSquadOnLocation);
                    l.Data.PreviousRecoveryDate = current;  
                }
            }
        }

        /// <summary>
        /// 敵の質を低下させる
        /// </summary>
        internal void ReduceSquadPower(SpawnSquad spawnSquad)
        {
            var spawns = GameManager.Instance.StaticData.AllSpawnSquads.GetSpawnSquadsFromID(spawnSquad.data.ID);
            if (spawns.Count == 0) return;
            spawns.RemoveAll(s => s.EnemyLevel >= spawnSquad.data.EnemyLevel);
            if (spawns.Count == 0) return;
            spawns.Sort((a, b) => b.EnemyLevel - a.EnemyLevel);
            spawns.IndexAt_Bug(0, out spawnSquad.data);
        }

        /// <summary>
        /// 敵の質を増加させる
        /// </summary>
        internal void IncreaseSquadPower(SpawnSquad spawnSquad)
        {
            if (!spawnSquad.data.AutoRecoveryLevel)
                return;
            var spawns = GameManager.Instance.StaticData.AllSpawnSquads.GetSpawnSquadsFromID(spawnSquad.data.ID);
            if (spawns.Count == 0) return;
            spawns.RemoveAll(s => s.EnemyLevel <= spawnSquad.data.EnemyLevel);
            if (spawns.Count == 0) return;
            spawns.Sort((a, b) => b.EnemyLevel - a.EnemyLevel);
            spawns.IndexAt_Bug(0, out spawnSquad.data);
        }

    }
}
