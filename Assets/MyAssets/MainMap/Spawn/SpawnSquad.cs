using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DG.Tweening;
using System;
using static Utility;
using Parameters.SpawnSquad;

namespace MainMap.Spawn
{
    /// <summary>
    /// SpawnさせたNPCの実Objectデータ
    /// </summary>
    public class SpawnSquad : MonoBehaviour
    {
        /// <summary>
        /// スポーンしたSquadの元データ
        /// </summary>
        [SerializeField] internal SpawnSquadData data;
        public SpawnRequestArgs SpawnRequestData { get; set; }
        /// <summary>
        /// SpawnSquadDataのID
        /// </summary>
        public string SquadID;
        /// <summary>
        /// 移動用Sequence
        /// </summary>
        internal Sequence sequence;
        /// <summary>
        /// BaseとなるSquadのID
        /// </summary>
        internal string BaseSquadID { get => SpawnRequestData.BaseSquadID; }
        /// <summary>
        /// スポーンした時間
        /// </summary>
        internal DateTime spawnTime;
        /// <summary>
        /// Squadの位置しているMapLocation
        /// </summary>
        internal MapLocation mapLocation;

        /// <summary>
        /// Squadがbaseの隷下部隊であるか
        /// </summary>
        public bool IsFollower { get => BaseSquadID != null; }

        /// <summary>
        /// 最初にSquadがスポーンした座標
        /// </summary>
        internal Vector3 SpawnPosition
        {
            set 
            {
                if (spawnPosition == null) spawnPosition = value;
            }
            get => spawnPosition;
        }
        private Vector3 spawnPosition;

        /// <summary>
        /// Squadの移動を一時停止する
        /// </summary>
        internal bool StopAnimation
        {
            set
            {
                stopAnimation = value;
                if (sequence != null && sequence.IsActive())
                {
                    if (value)
                        sequence.Pause();
                    else
                        sequence.Play();
                }
            }
            get => stopAnimation;
        }
        private bool stopAnimation = false;

        public override string ToString()
        {
            return $"SpawnSquad: [ {SpawnRequestData}]";
        }
    }

}