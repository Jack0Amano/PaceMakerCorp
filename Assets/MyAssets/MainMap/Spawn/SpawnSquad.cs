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
        /// <summary>
        /// 所属する基地
        /// </summary>
        internal MainMap.MapLocation baseLocation;
        /// <summary>
        /// 移動用Sequence
        /// </summary>
        internal Sequence sequence;
        /// <summary>
        /// NPCの行動タイプ
        /// </summary>
        internal NPCType moveType;
        /// <summary>
        /// 最初にSquadがスポーンした座標
        /// </summary>
        internal Vector3 spawnPosition
        {
            set 
            {
                if (_spawnPosition == null) _spawnPosition = value;
            }
            get => _spawnPosition;
        }
        private Vector3 _spawnPosition;

        /// <summary>
        /// Squadの移動を一時停止する
        /// </summary>
        internal bool stopAnimation
        {
            set
            {
                _stopAnimation = value;
                if (sequence != null && sequence.IsActive())
                {
                    if (value)
                        sequence.Pause();
                    else
                        sequence.Play();
                }
            }
            get => _stopAnimation;
        }
        private bool _stopAnimation = false;

        public override string ToString()
        {
            return $"SpawnSquad: [ {data} TYPE: {moveType}]";
        }
    }

    enum NPCType
    {
        /// <summary>
        /// 一箇所にとどまる防衛隊
        /// </summary>
        Defence,
        /// <summary>
        /// 救援部隊
        /// </summary>
        Rescue
    }
}