using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System;
using JetBrains.Annotations;
using Tactics.Character;

namespace Tactics.Object
{
    public class MortarGimmick : GimmickObject
    {
        [Header("Objects")]
        [SerializeField] public CinemachineVirtualCamera MortarVirtualCamera;
        [Tooltip("FollowCamera����������Object")]
        [SerializeField] public GameObject FollowTarget;
        [Tooltip("Camera�̒��S�ƂȂ�Object")]
        [SerializeField] public GameObject FollowObject;
        [Tooltip("���˂̌ʂ��X�^�[�g����ʒu")]
        [SerializeField] public GameObject ArcStartFromObject;
        [Tooltip("Gimmick�g�p����Unit���ʒu����ꏊ")]
        [SerializeField] public Transform UnitPosition;
        [Tooltip("Mortar�̒e")]
        [SerializeField] public GameObject MortarAmmo;
        [Tooltip("Mortar�̒e�̔��ˈʒu")]
        [SerializeField] public Transform MortarAmmoStartPosition;

        [SerializeField] public AttackType AttackType;


        [Tooltip("MortarAmmo�̔����̔��a")]
        [SerializeField] public float DamageCircleRadius = 2f;

        [Header("Aiming")]
        [Tooltip("Unit��Item�𓊂��鎞�̊p�x�ɑ΂���Force�̑����O���t\n" +
    "Angle�̂Ƃ�l��Unit���ǂꂾ����p�����邩�Ō��܂邪�����悻-1~1�̒l���Ƃ�")]
        [SerializeField] public AnimationCurve ThrowItemAngleAndForceCurve;

        [Header("Shoot ammo")]
        [Tooltip("Ammo���ڕW�ɂǂ̒��x�߂Â����甚�����邩")]
        [SerializeField] public float ExplosionDistance = 5f;
        [Tooltip("Ammo�̔��ˑ��x Ammo�̉^���͌����ڏd���̋[���I�Ȃ��̂Ȃ̂Ŏ��ۂ̑��x�Ƃ͈قȂ�")]
        [SerializeField] public float AmmoSpeed = 0.1f;

        [Tooltip("MortarAmmo�ˏo���̑��x�̕ω� x��0~1�̒�����1�l���傫���قǑ��x�������Ȃ� y��Arc�̐�����ʉ߂��鎞��")]
        [SerializeField] public AnimationCurve AmmoSpeedCurve;

        /// <summary>
        /// �e��̔����M�~�b�N
        /// </summary>
        public IEnumerator Explosion(GameObject mortarAmmo)
        {
            var rotation = mortarAmmo.transform.rotation;
            Destroy(mortarAmmo);
            yield return new WaitForSeconds(0.5f);
        }

        /// TODO: �e�X�g�p�Ƀ_���[�W�͈�� �ΐ�Ԟ֒e��ΐl�֒e�Ȃǂ����ꍇ�͂�����ύX����
        /// <summary>
        /// �֒e�̔����ʒu�����Hit�Ń_���[�W���v�Z����
        /// </summary>
        /// <param name="target">target��Unit</param>
        /// <param name="hit">hit����ꍇ�͂���raycast�̕Ԃ�l</param>
        /// <returns>�_���[�W��</returns>
        public int GetDamage(UnitController target, RaycastHit? hit)
        {
            if (hit == null) return 0;
            var distance = hit.GetValueOrDefault().distance;
            return 15;
        }

        /// <summary>
        /// Mortar���j�󂳂ꂽ���̃A�j���[�V����
        /// </summary>
        /// <returns></returns>
        override public IEnumerator DestroyAnimation()
        {
            StartCoroutine(base.DestroyAnimation());
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }
    }
}