using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System;
using static Utility;
using Tactics.Object;

namespace Tactics.UI
{
    public class SelectItemCell : MonoBehaviour
    {
        [SerializeField] internal ItemType itemType;
        [SerializeField] TextMeshProUGUI itemLabel;
        [SerializeField] Image typeImage;
        [SerializeField] TextMeshProUGUI numberLabel;
        [Tooltip("残弾数を表示するラベル")]
        [SerializeField] TextMeshProUGUI ammoCountLabel;
        [SerializeField] TextMeshProUGUI subAmmoCountLabel;

        readonly Color activeColor = Color.white;
        readonly Color disactiveColor = new Color(219, 219, 219, 175);
        const float duration = 0.5f;

        internal ItemHolder Holder { private set; get; }

        internal GimmickObject GimmickObject { private set; get; }

        /// <summary>
        /// Itemを選択中かどうか
        /// </summary>
        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;

            set
            {
                isSelected = value;
                var seq = DOTween.Sequence();
                if (value)
                {
                    seq.Append(itemLabel.DOFade(1, duration));
                    seq.Join(typeImage.DOFade(1, duration));
                    seq.Join(numberLabel.DOFade(1, duration));
                }
                else
                {
                    seq.Append( itemLabel.DOFade(0.3f, duration));
                    seq.Join( typeImage.DOFade(0.3f, duration));
                    seq.Join(numberLabel.DOFade(0.3f, duration));
                }
                seq.Play();
            }
        }

        /// <summary>
        /// 点滅アニメーション
        /// </summary>
        Sequence labelSequence;
        /// <summary>
        /// シェイクセクエンス
        /// </summary>
        Sequence shakeSequence;
        /// <summary>
        /// CantuseItemAnimationが再生中かどうか
        /// </summary>
        bool cantUseItemAnimationPlaying = false;

        // Start is called before the first frame update
        void Start()
        {
            //IsSelected = false;
        }

        /// <summary>
        /// CellにEquipmentDataを設定する
        /// </summary>
        /// <param name="data"></param>
        internal void SetItem(ItemHolder holder, int index)
        {
            numberLabel.SetText(index);
            itemLabel.SetText(holder.Data.Name);
            ammoCountLabel.SetText(holder.RemainingActionCount);
            Holder = holder;
            GimmickObject = null;
        }

        internal void SetGimmick(GimmickObject gimmickObject)
        {
            numberLabel.SetText("1");
            itemLabel.SetText(gimmickObject.GimmickName);
            ammoCountLabel.SetText(gimmickObject.RemainingActionCount);
            GimmickObject = gimmickObject;
            Holder = null;
        }

        /// <summary>
        /// アイテムの残弾数を更新する
        /// </summary>
        internal void UpdateItemState()
        {
            if (Holder != null)
                ammoCountLabel.SetText(Holder.RemainingActionCount);
            if (GimmickObject != null)
                ammoCountLabel.SetText(GimmickObject.RemainingActionCount);
        }

        /// <summary>
        /// アイテムを使用不可などの理由で振動させる
        /// </summary>
        internal IEnumerator CantUseItem()
        {
            if (cantUseItemAnimationPlaying)
                yield break;

            cantUseItemAnimationPlaying = true;
            if (labelSequence != null && labelSequence.IsActive())
                labelSequence.Kill();
            if (shakeSequence != null && shakeSequence.IsActive())
                shakeSequence.Kill();

            shakeSequence = transform.DOShakeX();
            shakeSequence.Play();

            // ammoCountLabelを点滅させる
            var duration = 0.3f;
            labelSequence = DOTween.Sequence();
            labelSequence.Append(ammoCountLabel.DOFade(0, duration));
            labelSequence.Join(subAmmoCountLabel.DOFade(0, duration));
            labelSequence.Append(ammoCountLabel.DOFade(1, duration));
            labelSequence.Join(subAmmoCountLabel.DOFade(1, duration));
            labelSequence.SetLoops(4, LoopType.Yoyo);
            labelSequence.Play();

            // shakeSequenceが終わるまで待つ
            yield return new WaitWhile(() => shakeSequence.IsActive());
            // labelSequenceが終わるまで待つ
            yield return new WaitWhile(() => labelSequence.IsActive());
            cantUseItemAnimationPlaying = false;
        }
    }
}