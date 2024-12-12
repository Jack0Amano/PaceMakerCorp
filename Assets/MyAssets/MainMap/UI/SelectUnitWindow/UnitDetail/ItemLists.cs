using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using TMPro;
using UnityEngine.AddressableAssets;
using MainMap.UI.SelectItem;
using DG.Tweening;

namespace UnitDetail
{
    /// <summary>
    /// EquipListの外観上の変更を反映するためのClass
    /// </summary>
    public class ItemLists : MonoBehaviour
    {
        [SerializeField] internal SelectItemWindow selectItemWindow;
        [SerializeField] ItemBox itemBox;

        /// <summary>
        /// リスト表示するもののGameObject関連
        /// </summary>
        readonly private List<ItemBox> boxes = new List<ItemBox>();

        private UnitData unitData;

        /// <summary>
        /// SelectItemWindowが開かれたときの呼び出し
        /// </summary>
        internal Action openSelectItemWindowAction;
        /// <summary>
        /// SelectItemWindowが閉じられたときの呼び出し
        /// </summary>
        internal Action<UnitData> closeSelectItemWindowAction;

        /// <summary>
        /// ゲームデータのセーブを行う
        /// </summary>
        DataSavingController DataSavingController
        {
            get => GameManager.Instance.DataSavingController;
        }

        protected void Awake()
        {
            boxes.Add(itemBox);
            itemBox.Button.onClick.AddListener(() => ClickItemBox(itemBox));
            itemBox.RemoveButton.onClick.AddListener(() => RemoveItem(itemBox));
        }

        /// <summary>
        /// ItemBoxをクリックしたときに
        /// </summary>
        /// <param name="box"></param>
        private void ClickItemBox(ItemBox box)
        {
            openSelectItemWindowAction?.Invoke();
            selectItemWindow.Show(box.ItemHolder.Type, (itemId) =>
            {
                // 表示が終わった際のコールバック
                if (itemId.Length != 0)
                {
                    DataSavingController.MyArmyData.SetItem(unitData, box.ItemHolder, itemId);
                    box.UpdateItemWithAnimation();
                    boxes.FindAll(b => b.IsDefaultMode).ForEach(b => b.IsDefaultMode = false);
                }
                closeSelectItemWindowAction?.Invoke(unitData);
            });
        }

        /// <summary>
        /// ItemBoxを削除する
        /// </summary>
        /// <param name="box"></param>
        private void RemoveItem(ItemBox box)
        {

            DataSavingController.MyArmyData.RemoveItemFromHolder(unitData, box.ItemHolder);

            var weapons = boxes.FindAll(b =>
            {
                return (b.ItemHolder.Type == HolderType.Primary && b.ItemHolder.Data != null) ||
                       (b.ItemHolder.Type == HolderType.Secondary && b.ItemHolder.Data != null);
            });
            var myArmyData = GameManager.Instance.DataSavingController.MyArmyData;
            unitData.SetDefaultItemIfNeeded(myArmyData);

            box.UpdateItemWithAnimation();
        }

        /// <summary>
        /// EquipmentBoxを指定された数表示し、typeに合う適当なアイコンを表示
        /// </summary>
        /// <param name="holders"></param>
        public void SetEquipments(UnitData unitData)
        {
            this.unitData = unitData;
            var holders = unitData.MyItems;

            var addCount = holders.Count - boxes.Count;
            for (var i = 0; i < addCount; i++)
            {
                if (addCount > 0)
                {
                    var box = Instantiate(itemBox, transform);
                    boxes.Add(box);
                    box.Button.onClick.AddListener(() => ClickItemBox(box));
                    box.RemoveButton.onClick.AddListener(() => RemoveItem(box));
                }
                else if (addCount < 0)
                {
                    var last = boxes.Last();
                    Destroy(last);
                    boxes.RemoveAt(boxes.Count);
                }
            }

            for(var i =0; i<boxes.Count; i++)
            {
                var box = boxes[i];
                box.ItemHolder = holders[i];
            }

            if (!unitData.DoesUnitHaveWeapon)
            {
                
            }
            else if (DoesOnlyHaveDefault)
            {
                var box = boxes.FindAll(b =>
                {
                    return (b.ItemHolder.Type == HolderType.Primary && b.ItemHolder.Data != null) ||
                           (b.ItemHolder.Type == HolderType.Secondary && b.ItemHolder.Data != null);
                }).First();
                box.UpdateItemWithoutAnimation(true);
            }
        }

        /// <summary>
        /// 現在UnitがDefaultしか持っていない場合
        /// </summary>
        private bool DoesOnlyHaveDefault
        {
            get
            {
                var weapons = boxes.FindAll(b =>
                {
                    return (b.ItemHolder.Type == HolderType.Primary && b.ItemHolder.Data != null) ||
                           (b.ItemHolder.Type == HolderType.Secondary && b.ItemHolder.Data != null);
                });
                if (weapons.Count == 0)
                    return true;
                if (weapons.Count == 1)
                    return weapons.Find(w => w.ItemHolder.Data.ID == GameManager.Instance.SceneParameter.DefaultWeaponID);
                return false;
            }
        }
    }

    /// <summary>
    /// ホルダーとアイコンイメージのアドレスの紐付け
    /// </summary>
    [Serializable]
    class HolderIcon
    {
        // internal string address;
        [SerializeField] internal HolderType type;
        [SerializeField] internal Sprite icon;
    }
}