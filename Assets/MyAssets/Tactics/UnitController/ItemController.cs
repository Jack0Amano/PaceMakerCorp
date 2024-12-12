using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Tactics.Character;
using static Utility;
using DG.Tweening;
using UnityEngine.ResourceManagement;

namespace Tactics.Items
{
    /// <summary>
    /// Unitの所持するアイテム関連を統合的に操作
    /// </summary>
    public class ItemController : MonoBehaviour
    {

        [SerializeField] Transform handTransform;

        internal Control.ThirdPersonUserControl tpsController;
        /// <summary>
        /// 現在セレクトされているアイテムのホルダー
        /// </summary>
        public ItemHolder CurrentItemHolder { private set; get; }
        /// <summary>
        /// 現在保持しているGameObjectのItem
        /// </summary>
        public Item CurrentItem { private set; get; }
        /// <summary>
        /// Worldにアイテムが設置されたときにItemが置かれるParent
        /// </summary>
        [NonSerialized] public GameObject WorldItemParent;
        /// <summary>
        /// UnitのItemHolder
        /// </summary>
        internal List<ItemHolder> ItemHolders { private set; get; }

        /// <summary>
        /// アニメーションの停止
        /// </summary>
        public bool PauseAnimation
        {
            get => pauseAnimation;
            set
            {
                pauseAnimation = value;
            }
        }
        private bool pauseAnimation;

        /// <summary>
        /// カウンター攻撃可能か
        /// </summary>
        [SerializeField, ReadOnly] public bool CanCounterAttack = false;

        /// <summary>
        /// Unitの装備品データを登録する + currentItemHolderを設定する
        /// </summary>
        /// <param name="holders"></param>
        public void Initialize(List<ItemHolder> holders)
        {
            holders.ForEach(h => h.Initialize());
            ItemHolders = holders;
            var primary = holders.Find(h => h.Type == HolderType.Primary);
            // TODO primaryが存在しない場合はsecondaryに持ち替える、Secondaryも存在しない場合はDefaultSecondaryに変更
            CurrentItemHolder = primary;
            
            UpdateCounterattackData();
        }

        /// <summary>
        /// 現在持っているアイテムから変更して指定されたものを持つ
        /// </summary>
        /// <param name="holder"></param>
        public IEnumerator SetItem(ItemHolder holder)
        {
            if (CurrentItem != null)
                Destroy(CurrentItem.gameObject);
            CurrentItem = null;

            CurrentItemHolder = holder;

            yield return StartCoroutine(ResetItem());
        }

        /// <summary>
        /// 現在ホルダーに存在するアイテムを再び設置する
        /// </summary>
        public IEnumerator ResetItem()
        {
            if (CurrentItemHolder == null)
                PrintError("CurrentItemHolder is null");
            if (CurrentItemHolder.Data == null)
                PrintError($"{CurrentItemHolder.Id} is missing data.");
            if (CurrentItemHolder.Data.Prefab == null)
                PrintError($"{CurrentItemHolder}, {CurrentItemHolder.Data}\nPrefab of Data is missing");
            if (CurrentItemHolder.Data.TacticsItem == null)
                PrintError($"{CurrentItemHolder}, {CurrentItemHolder.Data}\nTactics.Items.Item is not set on prefab");
            if (handTransform == null)
                PrintError("handTransform is null");
            CurrentItem = Instantiate(CurrentItemHolder.Data.TacticsItem, handTransform);
            CurrentItem.ActivateColliders(false);
            yield return StartCoroutine(tpsController.HaveItem(CurrentItem));
        }

        /// <summary>
        /// アイテムを所持しているホルダーをすべて取得する
        /// </summary>
        /// <returns></returns>
        public List<ItemHolder> GetAllItemHolders()
        {
            return ItemHolders.FindAll(h => h.Data != null);
        }


        /// アイテムの利用をControlする
        #region Use Item

        /// <summary>
        /// <c>arc</c>の地点を通る投射運動をアニメーションベースで再現する
        /// </summary>
        /// <param name="arc"></param>
        /// <returns></returns>
        public IEnumerator ThrowAction(List<Vector3> arc, Vector3 initialVelocity)
        {
            CurrentItemHolder.RemainingActionCount--;

            CurrentItem.gameObject.transform.SetParent(WorldItemParent.transform);
            CurrentItem.ActivateColliders(true);
            var item = CurrentItem;
            item.ThroughUnit = true;
            item.transform.position = arc[0];

            item.grenadeIsMoving = true;

            // 最終ArcのstartPhisicalIndex以降はAnimationを使った擬似的な運動ではなく物理ベースで行う
            var startPhysicalIndex = 3;

            var seq = DOTween.Sequence();
            var horizontalVelocity = Vector2.Distance(initialVelocity.xz(), Vector2.zero);

            const int deletePartsIndex = 3;
            const int purgePartsIndex = 5;

            for (var i = 1; i < arc.Count - startPhysicalIndex; i++)
            {
                var p1 = arc[i - 1];
                var p2 = arc[i];
                var horizontalDist = Vector2.Distance(p1.xz(), p2.xz());
                var t = horizontalDist / horizontalVelocity;

                // Time 時間で p1 から p2へと移動すればほぼ投射運動と同じ
                var action = item.transform.DOMove(p2, t);
                if (deletePartsIndex == i)
                {
                    action.OnComplete(() => item.DisappearParts());
                }
                else if (purgePartsIndex == i)
                {
                    action.OnComplete(() => 
                    { 
                        var purged = item.PurgeParts();
                        purged.ForEach(p => p.AddForce((p2 - p1) * 10, ForceMode.Impulse));
                    });
                }
                    
                seq.Append(action);
            }

            // 最終の物理ベースにわたす際にitemがどの程度加速しているか
            var lastArc = arc.Slice(arc.Count - startPhysicalIndex - 1, arc.Count - 1);
            var lastHorizontalDist = Vector2.Distance(lastArc[0].xz(), lastArc[1].xz());
            var lastTime = lastHorizontalDist / horizontalVelocity;
            var lastAbsVelocity = Vector3.Distance(lastArc[0], lastArc[1]) / lastTime;
            var lastVelocity = (lastArc[1] - lastArc[0]) * lastAbsVelocity * 2;

            seq.OnComplete(() =>
            {
                item.ThroughUnit = false;
                item.ActiveGravity = true;
                item.mainRigidbody.AddForce(lastVelocity, ForceMode.Impulse);
            });
            seq.SetEase(Ease.Linear);
            seq.Play();

            // ===>
            Vector3 itemHitPosition = Vector3.positiveInfinity;
            bool itemHit = false;
            item.OnCollisionEnterAtLayer += ((Items.Item item, GameObject hit) =>
            {
                itemHitPosition = item.transform.position;
                itemHit = true;
            });

            // Unit or Groundに接触する もしくはexplosionTimeに達するまで待つ
            const float explosionTime = 20000f;
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).Milliseconds < explosionTime)
            {
                if (itemHit)
                    break;

                if (PauseAnimation)
                {
                    var startPause = DateTime.Now;
                    seq.Pause();
                    while (PauseAnimation)
                        yield return null;
                    seq.Play();
                    startTime.Add(DateTime.Now - startPause);
                }

                yield return null;
            }
            if (!itemHit)
                itemHitPosition = item.transform.position;

            item.mainRigidbody.velocity = Vector3.zero;
            item.ActiveGravity = false;
            StartCoroutine(item.Explosion());

            yield return itemHitPosition;
        }

        /// <summary>
        /// Rifleの弾を発射する
        /// </summary>
        public void ShootAction()
        {
            CurrentItemHolder.RemainingActionCount--;
            PlayShootEffects();
            UpdateCounterattackData();
        }


        /// <summary>
        /// カウンター攻撃可能かどうかをRemainingActionCountから判断する 各Actionの最後に呼び出す
        /// </summary>
        private void UpdateCounterattackData()
        {
            CanCounterAttack = GetCounterAttackWeapon() != null;
        }

        #endregion

        #region Parameters
        /// <summary>
        /// 現在持っている武器の基礎攻撃力 Unitごとのを算出する場合CalcDamageを使用
        /// </summary>
        public int BaseAttackPoint
        {
            get
            {
                if (CurrentItemHolder == null || CurrentItemHolder == null)
                    return 0;
                return CurrentItemHolder.Data.Attack;
            }
        }

        /// <summary>
        /// 現在持っている武器のタイプ
        /// </summary>
        public TargetType TargetType
        {
            get
            {
                if (CurrentItemHolder == null || CurrentItemHolder == null)
                    return TargetType.None;
                return CurrentItemHolder.Data.TargetType;

            }
        }

        /// <summary>
        /// カウンター攻撃可能な武器を取得する
        /// </summary>
        public ItemHolder GetCounterAttackWeapon()
        {
            if (CurrentItemHolder.CanCounterAttack)
                return CurrentItemHolder;
            else
            {
                var itemsHolders = ItemHolders.FindAll(h => h.Data != null);
                if (itemsHolders.Count == 0)
                    return null;
                return ItemHolders.OrderByDescending(h => h.Data.Attack).First();
            }
        }
        #endregion

        #region Calcuration

        /// <summary>
        /// 当たる確率 0_1
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public float HitRate(float distance, UnitController target)
        {
            var parameters = GameManager.Instance.GeneralParameter;
            var range = distance / (CurrentItemHolder.Data.Range * parameters.weaponRangeCoefficient);
            var rate = parameters.weaponReductionCurve.Evaluate(range);
            if (float.IsNaN(rate) && rate < 0)
                rate = 0;
            // 対物武器でHuman型のTargetを攻撃したため命中率を低下させる
            if (target.CurrentParameter.Data.UnitType != UnitType.Type.Tank && CurrentItemHolder.Data.TargetType == TargetType.Object)
                rate *= parameters.ReduceRateUsingAntiObjectWeapon;
            return rate;
        }

        /// <summary>
        /// Targetに攻撃した際のダメージ量を計算
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public int CalcDamage(UnitController target)
        {
            if (CurrentItemHolder == null || CurrentItemHolder.Data == null)
                return 0;
            if (CurrentItemHolder.Data.TargetType == TargetType.Debug)
            {
                // TargetType.Debugはすべての敵に有効なデバック用
                return CurrentItemHolder.Data.Attack;
            }

            if (CurrentItemHolder.Data.TargetType == TargetType.Human)
            {
                // 対人武器でTankに攻撃している状態 ダメージデバフを掛ける
                if (target.CurrentParameter.Data.UnitType == UnitType.Type.Tank)
                    return CurrentItemHolder.Data.Attack / 5;
                // 対人武器でHuman型に攻撃している状態
                return CurrentItemHolder.Data.Attack;
            }

            if (CurrentItemHolder.Data.TargetType == TargetType.Object)
            {
                // 対物武器でHuman型に攻撃している状態
                // Damageは変わらないが命中率が下がる
                // 対物武器でTank型に攻撃している状態
                return CurrentItemHolder.Data.Attack;
            }

            PrintWarning($"Try attack to {target} using {CurrentItemHolder.Data} of {CurrentItemHolder.Data.TargetType}. Damage is None");
            return 0;
        }
        #endregion

        #region Effects
        /// <summary>
        /// 武器や現在装備中のアイテムが使用された際のエフェクト等を実行する
        /// </summary>
        /// <param name="isHit"></param>
        /// <returns></returns>
        private void PlayShootEffects()
        {
            // StartCoroutine(CommonMuzzleFlash());
        }
        

        /// <summary>
        /// マズルフラッシュを表示する
        /// </summary>
        /// <returns></returns>
        //private IEnumerator CommonMuzzleFlash()
        //{
        //    //マズルフラッシュON
        //    if (muzzleFlashPrefab != null)
        //    {
        //        if (muzzleFlash != null)
        //        {
        //            muzzleFlash.SetActive(true);
        //        }
        //        else
        //        {
        //            muzzleFlash = Instantiate(muzzleFlashPrefab, transform.position, transform.rotation);
        //            muzzleFlash.transform.SetParent(weaponTop.gameObject.transform);
        //            muzzleFlash.transform.localScale = muzzleFlashScale;
        //        }
        //    }

        //    yield return new WaitForSeconds(0.2f);
        //    //マズルフラッシュOFF
        //    if (muzzleFlash != null)
        //    {
        //        muzzleFlash.SetActive(false);
        //    }
        //    //ヒットエフェクトOFF
        //    if (hitEffect != null)
        //    {
        //        if (hitEffect.activeSelf)
        //        {
        //            hitEffect.SetActive(false);
        //        }
        //    }
        //}
       
        #endregion
    }


}