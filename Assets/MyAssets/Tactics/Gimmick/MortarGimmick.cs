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
        [Tooltip("FollowCameraが周りを回るObject")]
        [SerializeField] public GameObject FollowTarget;
        [Tooltip("Cameraの中心となるObject")]
        [SerializeField] public GameObject FollowObject;
        [Tooltip("投射の弧がスタートする位置")]
        [SerializeField] public GameObject ArcStartFromObject;
        [Tooltip("Gimmick使用時にUnitが位置する場所")]
        [SerializeField] public Transform UnitPosition;
        [Tooltip("Mortarの弾")]
        [SerializeField] public GameObject MortarAmmo;
        [Tooltip("Mortarの弾の発射位置")]
        [SerializeField] public Transform MortarAmmoStartPosition;

        [SerializeField] public AttackType AttackType;


        [Tooltip("MortarAmmoの爆発の半径")]
        [SerializeField] public float DamageCircleRadius = 2f;

        [Header("Aiming")]
        [Tooltip("UnitがItemを投げる時の角度に対するForceの増加グラフ\n" +
    "Angleのとる値はUnitがどれだけ俯角を取れるかで決まるがおおよそ-1~1の値をとる")]
        [SerializeField] public AnimationCurve ThrowItemAngleAndForceCurve;

        [Header("Shoot ammo")]
        [Tooltip("Ammoが目標にどの程度近づいたら爆発するか")]
        [SerializeField] public float ExplosionDistance = 5f;
        [Tooltip("Ammoの発射速度 Ammoの運動は見た目重視の擬似的なものなので実際の速度とは異なる")]
        [SerializeField] public float AmmoSpeed = 0.1f;

        [Tooltip("MortarAmmo射出時の速度の変化 xは0~1の長さで1値が大きいほど速度が速くなる yはArcの線分を通過する時間")]
        [SerializeField] public AnimationCurve AmmoSpeedCurve;

        /// <summary>
        /// 弾薬の爆発ギミック
        /// </summary>
        public IEnumerator Explosion(GameObject mortarAmmo)
        {
            var rotation = mortarAmmo.transform.rotation;
            Destroy(mortarAmmo);
            yield return new WaitForSeconds(0.5f);
        }

        /// TODO: テスト用にダメージは一定 対戦車榴弾や対人榴弾などを作る場合はここを変更する
        /// <summary>
        /// 榴弾の爆発位置からのHitでダメージを計算する
        /// </summary>
        /// <param name="target">targetのUnit</param>
        /// <param name="hit">hitする場合はそのraycastの返り値</param>
        /// <returns>ダメージ量</returns>
        public int GetDamage(UnitController target, RaycastHit? hit)
        {
            if (hit == null) return 0;
            var distance = hit.GetValueOrDefault().distance;
            return 15;
        }

        /// <summary>
        /// Mortarが破壊された時のアニメーション
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