using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tactics.Object
{
    public class CoverGimmick: GimmickObject
    {
        [Header("For AI")]
        [Tooltip("Coverに隠れる際のNPCのいち")]
        [SerializeField] public List<Transform> safePositions;

        [Header("Along wall panel")]
        [Tooltip("UnitがCoverに隠れる際に、Sandbagの周りに置くパネル")]
        [SerializeField] public List<GameObject> AlongWallPanels;

        /// <summary>
        /// Coverの破壊アニメーション
        /// </summary>
        /// <returns></returns>
        override public IEnumerator DestroyAnimation()
        {
            yield return new WaitForSeconds(0.5f);
            IsDestroyed = true;
        }
    }
}