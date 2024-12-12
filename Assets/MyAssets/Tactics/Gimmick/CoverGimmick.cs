using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tactics.Object
{
    public class CoverGimmick: GimmickObject
    {
        [Header("For AI")]
        [Tooltip("Cover�ɉB���ۂ�NPC�̂���")]
        [SerializeField] public List<Transform> safePositions;

        [Header("Along wall panel")]
        [Tooltip("Unit��Cover�ɉB���ۂɁASandbag�̎���ɒu���p�l��")]
        [SerializeField] public List<GameObject> AlongWallPanels;

        /// <summary>
        /// Cover�̔j��A�j���[�V����
        /// </summary>
        /// <returns></returns>
        override public IEnumerator DestroyAnimation()
        {
            yield return new WaitForSeconds(0.5f);
            IsDestroyed = true;
        }
    }
}