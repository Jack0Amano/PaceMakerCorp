using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MainMap.Spawn;
using System;
using Parameters.SpawnSquad;

namespace MainMap.Roads
{
    /// <summary>
    /// 敵Unitがスポーンする可能性のある場所
    /// </summary>
    public class MapSpawnLocation : MonoBehaviour
    {
        [Tooltip("スポーンする場所のID")]
        [SerializeField] public string SpawnID;

        [Tooltip("通常エンカウント時のTacticsSceneのID")]
        [SerializeField] public string tacticsSceneID;

        [Tooltip("Spawnする際にどこから出撃したていにするか costが影響")]
        [SerializeField] public MapLocation baseLocation;
        /// <summary>
        /// SpawnSquadがLocationに存在しているか
        /// </summary>
        [NonSerialized] public SpawnSquad SpawnSquadOnLocation;
        /// <summary>
        /// SpawnLocationがMapLocationの上に存在しているかどうか
        /// </summary>
        public bool LocationOnBase { get; private set; } = false;

        private void Awake()
        {
            LocationOnBase = GetComponent<MapLocation>() != null;
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}