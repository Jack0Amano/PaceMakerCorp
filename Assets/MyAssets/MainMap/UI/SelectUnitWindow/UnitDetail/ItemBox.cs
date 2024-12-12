using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace UnitDetail
{
    /// <summary>
    /// EquipmentBoxをリスト化してまとめておくためのField用
    /// </summary>
    class ItemBox: MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("HolderのIconImage")]
        [SerializeField] Image IconImage;
        [Tooltip("Holderに表示するLabel")]
        [SerializeField] TextMeshProUGUI Label;
        [Tooltip("Boxのボタン")]
        [SerializeField] internal Button Button;
        [Tooltip("Itemの削除ボタン")]
        [SerializeField] internal Button RemoveButton;

        [Header("Defaults")]
        [Tooltip("BoxがEmptyの場合表示するlabel")]
        [SerializeField] string EmptyItemBoxText;
        [Tooltip("BoxがEmptyの場合のLabelの色")]
        [SerializeField] Color EmptyItemBoxColor = Color.white;
        [Tooltip("BoxがDefault武器の場合の色")]
        [SerializeField] Color DefaultItemBoxColor = Color.white;
        [Tooltip("装備ホルダーとアイコンアドレスの紐付け")]
        [SerializeField] List<HolderIcon> HolderIcons = new List<HolderIcon>();

        /// <summary>
        /// Boxに表示するItemHolder
        /// </summary>
        public ItemHolder ItemHolder
        {
            get => _ItemHolder;
            set
            {
                _ItemHolder = value;
                IconImage.sprite = HolderIcons.Find(h => h.type == value.Type).icon;
                if (value.Data != null)
                {
                    Label.text = value.Data.Name;
                    Label.color = Color.white;
                    RemoveButton.interactable = true;
                    RemoveButton.targetGraphic.color = Color.white;
                }
                else
                {
                    Label.text = EmptyItemBoxText;
                    Label.color = EmptyItemBoxColor;
                    RemoveButton.interactable = false;
                    RemoveButton.targetGraphic.color = Color.clear;
                }
            }
        }
        private ItemHolder _ItemHolder;

        /// <summary>
        /// BoxがdefaultItemが置かれている状態か
        /// </summary>
        public bool IsDefaultMode
        {
            get
            {
                return ItemHolder != null && _isDefaultMode;
            }
            set
            {
                if (ItemHolder.Data == null || _isDefaultMode == value)
                    return;

                _isDefaultMode = value;
                RemoveButton.targetGraphic.DOColor(IsDefaultMode ? DefaultItemBoxColor : Color.white, 0.3f).OnComplete(() =>
                {
                    RemoveButton.interactable = !value;
                });
            }
        }
        private bool _isDefaultMode = false;

        private protected void Awake()
        {
        }

        /// <summary>
        /// BoxにItemを表示する
        /// </summary>
        internal void UpdateItemWithAnimation(bool showAsDefault = false)
        {
            var seq = DOTween.Sequence();
            if (ItemHolder.Data != null)
            {
                if (showAsDefault)
                {
                    RemoveButton.interactable = false;
                    seq.Append(RemoveButton.targetGraphic.DOColor(Color.clear, 0.3f));
                    Label.DOColor(DefaultItemBoxColor, 0.3f);
                    _isDefaultMode = true;
                }
                else
                {
                    seq.Append(RemoveButton.targetGraphic.DOColor(Color.white, 0.3f));
                    seq.Join(Label.DOColor(Color.white, 0.3f));
                    seq.OnComplete(() => RemoveButton.interactable = true);
                    _isDefaultMode = false;
                }
                Label.text = ItemHolder.Data.Name;
            }
            else
            {
                RemoveButton.interactable = false;
                seq.Append(RemoveButton.targetGraphic.DOColor(Color.clear, 0.3f));
                seq.Join(Label.DOColor(EmptyItemBoxColor, 0.3f));
                Label.text = EmptyItemBoxText;
                _isDefaultMode = false;
            }
            seq.Play();
        }

        /// <summary>
        /// アニメーション無しでItemBoxの描写内容をUpdateする
        /// </summary>
        /// <param name="showAsDefault">デフォルトの武器として表示するか</param>
        internal void UpdateItemWithoutAnimation(bool showAsDefault = false)
        {
            if(ItemHolder.Data != null)
            {
                if (showAsDefault)
                {
                    RemoveButton.interactable = false;
                    RemoveButton.targetGraphic.color = Color.clear;
                    Label.color = DefaultItemBoxColor;
                    _isDefaultMode = true;
                }
                else
                {
                    RemoveButton.interactable = true;
                    RemoveButton.targetGraphic.color = Color.white;
                    Label.color = Color.white;
                    _isDefaultMode = false;
                }
                Label.text = ItemHolder.Data.Name;
            }
            else
            {
                RemoveButton.interactable = false;
                RemoveButton.targetGraphic.color = Color.clear;
                Label.color = EmptyItemBoxColor;
                Label.text = EmptyItemBoxText;
                _isDefaultMode = false;
            }
        }
    }
}