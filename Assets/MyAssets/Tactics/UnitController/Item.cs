using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Animations;
using UnityEngine.VFX;
using static Utility;

namespace Tactics.Items
{
    public class Item : MonoBehaviour
    {
        /// <summary>
        /// アイテムを手に所持している際のアニメーションクリップ
        /// </summary>
        [SerializeField] public AnimationClip havingItemAnimationClip;
        /// <summary>
        /// 使用時のアニメーションのタイプ
        /// </summary>
        [SerializeField] public UseItemType useItemType;
        [Tooltip("Itemを使用するDuration")]
        [SerializeField] public float UseItemDuration;

        [SerializeField] public Rigidbody mainRigidbody;
        [Tooltip("func Purge 時にmainRigidbodyから離れるObject\n" +
                 "ObjectにRidigBodyを持っている必要がある")]
        [SerializeField] public List<GameObject> purgeObjects;
        [Tooltip("func Disapear時に消されるObject")]
        [SerializeField] public List<GameObject> disappearObjects;
        [Tooltip("Collisionの接触をNortifyするLayerMask")]
        [SerializeField] LayerMask nortifyLayerMasks;
        [Tooltip("Explosion時に消えるMeshRenderer")]
        [SerializeField] List<MeshRenderer> explosionRenderers;

        [Tooltip("Effect")]
        [SerializeField] public VisualEffect explosionEffect;

        /// <summary>
        /// <c>nortifyLayerMask</c>のLayerObjectにCollisionが接触したときの呼び出し
        /// </summary>
        public Action<Item, GameObject> OnCollisionEnterAtLayer;

        private List<(GameObject obj, Rigidbody rigi)> _purgeObjects;

        /// <summary>
        /// Unitの手を離れる瞬間などにColliderのついたObjectがあると運動がおかしくなるため
        /// 一時的にこれらのObjectのLayerをThroughUnit layerに変更する
        /// </summary>
        private List<Collider> colliderObjects;

        private LayerMask throughUnitLayer;
        private LayerMask defaultLayer;

        private NortifyTrigger nortifyTrigger;

        private bool _throughUnit = false;
        public bool ThroughUnit
        {
            get => _throughUnit;
            set
            {
                _throughUnit = value;
                if (value)
                    colliderObjects.ForEach(c => c.gameObject.layer = throughUnitLayer.value);
                else
                    colliderObjects.ForEach(c => c.gameObject.layer = defaultLayer.value);
            }
        }

        public bool ActiveGravity
        {
            get 
            {
                if (mainRigidbody == null)
                    return false;
                return !mainRigidbody.isKinematic; 
            }
            set
            {
                if(mainRigidbody != null)
                    mainRigidbody.isKinematic = !value;
            }
        }

        /// <summary>
        /// グレネードが投げられて爆発するまでの状態であるか
        /// </summary>
        public bool grenadeIsMoving = false;

        private void Awake()
        {
            throughUnitLayer = LayerMask.NameToLayer("ThroughUnit");
            defaultLayer = LayerMask.NameToLayer("Default");

            ActiveGravity = false;
            _purgeObjects = purgeObjects.ConvertAll((o) => (o, o.GetComponent<Rigidbody>()));

            colliderObjects = GetComponentsInChildren<Collider>().ToList();
            var mainCollider = GetComponent<Collider>();
            if (mainCollider != null)
                colliderObjects.Add(mainCollider);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (grenadeIsMoving)
            {
                if (((1 << collision.gameObject.layer) & nortifyLayerMasks) != 0)
                    OnCollisionEnterAtLayer?.Invoke(this, collision.gameObject);
            }

        }

        /// <summary>
        /// すべてのコライダーの有効化を管理 (ItemがRigidBodyの子オブジェクトに存在するときはfalseを設定)
        /// </summary>
        /// <param name="a"></param>
        public void ActivateColliders(bool a)
        {
            colliderObjects.ForEach(c => c.enabled = a);
        }

        /// <summary>
        /// SubObjectsをmainから切り離して別演算で計算する
        /// </summary>
        public List<Rigidbody> PurgeParts()
        {
            var parent = gameObject.transform.parent;
            _purgeObjects.ForEach(o =>
            {
                o.obj.transform.parent = parent;
                if (o.rigi != null)
                    o.rigi.isKinematic = false;
            });

            return _purgeObjects.ConvertAll(p => p.rigi);
        }

        /// <summary>
        /// DisappearParts
        /// </summary>
        public void DisappearParts()
        {
            disappearObjects.ForEach(o =>
            {
                o.SetActive(false);
            });
        }

        /// <summary>
        /// 爆発エフェクトと共にItemを消す
        /// </summary>
        internal IEnumerator Explosion()
        {
            if (explosionEffect != null)
            {
                explosionRenderers.ForEach(r => r.enabled = false);

                if (explosionEffect.GetFloat("time", out var time))
                {
                    explosionEffect.Play();
                    yield return new WaitForSeconds(time);
                }
                else
                {
                    PrintWarning("Explosion Effectにtimeのパラメータが設定されていません");
                    yield return new WaitForSeconds(1f);
                }
            }
            
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// AnimationのClipを管理するためのTypeだが、廃止予定
    /// </summary>
    [Serializable]
    public enum UseItemType : int
    {
        Rifle = 1
    }
}