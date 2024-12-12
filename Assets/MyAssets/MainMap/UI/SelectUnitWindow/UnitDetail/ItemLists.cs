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
        [SerializeField] ItemBox ItemBox;

        /// <summary>
        /// リスト表示するもののGameObject関連
        /// </summary>
        readonly private List<ItemBox> Boxes = new List<ItemBox>();

        private UnitData UnitData;

        /// <summary>
        /// SelectItemWindowが開かれたときの呼び出し
        /// </summary>
        internal Action OpenSelectItemWindowAction;
        /// <summary>
        /// SelectItemWindowが閉じられたときの呼び出し
        /// </summary>
        internal Action<UnitData> CloseSelectItemWindowAction;

        /// <summary>
        /// ゲームデータのセーブを行う
        /// </summary>
        DataSavingController DataSavingController
        {
            get => GameManager.Instance.DataSavingController;
        }


        /// <summary>
        /// 何も装備していないときに装備されるデフォルトの武器のID
        /// </summary>
        string DefaultWeaponID
        {
            get => GameManager.Instance.DataSavingController.SaveData.DataInfo.DefaultWeaponID;
        }

        protected void Awake()
        {
            Boxes.Add(ItemBox);
            ItemBox.Button.onClick.AddListener(() => ClickItemBox(ItemBox));
            ItemBox.RemoveButton.onClick.AddListener(() => RemoveItem(ItemBox));
        }

        /// <summary>
        /// ItemBoxをクリックしたときに
        /// </summary>
        /// <param name="box"></param>
        private void ClickItemBox(ItemBox box)
        {
            OpenSelectItemWindowAction?.Invoke();
            selectItemWindow.Show(box.ItemHolder.Type, (itemId) =>
            {
                // 表示が終わった際のコールバック
                if (itemId.Length != 0)
                {
                    DataSavingController.MyArmyData.ItemController.SetItem(UnitData, box.ItemHolder, itemId);
                    box.UpdateItemWithAnimation();
                    Boxes.FindAll(b => b.IsDefaultMode).ForEach(b => b.IsDefaultMode = false);
                }
                CloseSelectItemWindowAction?.Invoke(UnitData);
            });
        }

        /// <summary>
        /// ItemBoxを削除する
        /// </summary>
        /// <param name="box"></param>
        private void RemoveItem(ItemBox box)
        {

            DataSavingController.MyArmyData.ItemController.RemoveItemFromHolder(UnitData, box.ItemHolder);

            var weapons = Boxes.FindAll(b =>
            {
                return (b.ItemHolder.Type == HolderType.Primary && b.ItemHolder.Data != null) ||
                       (b.ItemHolder.Type == HolderType.Secondary && b.ItemHolder.Data != null);
            });
            var myArmyData = GameManager.Instance.DataSavingController.MyArmyData;
            UnitData.SetDefaultItemIfNeeded(myArmyData.ItemController.DefaultWeapon.ItemData, myArmyData);

            box.UpdateItemWithAnimation();
        }

        /// <summary>
        /// EquipmentBoxを指定された数表示し、typeに合う適当なアイコンを表示
        /// </summary>
        /// <param name="holders"></param>
        public void SetEquipments(UnitData unitData)
        {
            this.UnitData = unitData;
            var holders = unitData.MyItems;

            var addCount = holders.Count - Boxes.Count;
            for (var i = 0; i < addCount; i++)
            {
                if (addCount > 0)
                {
                    var box = Instantiate(ItemBox, transform);
                    Boxes.Add(box);
                    box.Button.onClick.AddListener(() => ClickItemBox(box));
                    box.RemoveButton.onClick.AddListener(() => RemoveItem(box));
                }
                else if (addCount < 0)
                {
                    var last = Boxes.Last();
                    Destroy(last);
                    Boxes.RemoveAt(Boxes.Count);
                }
            }

            for(var i =0; i<Boxes.Count; i++)
            {
                var box = Boxes[i];
                box.ItemHolder = holders[i];
            }

            if (!unitData.DoesUnitHaveWeapon)
            {
                
            }
            else if (DoesOnlyHaveDefault)
            {
                var box = Boxes.FindAll(b =>
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
                var weapons = Boxes.FindAll(b =>
                {
                    return (b.ItemHolder.Type == HolderType.Primary && b.ItemHolder.Data != null) ||
                           (b.ItemHolder.Type == HolderType.Secondary && b.ItemHolder.Data != null);
                });
                if (weapons.Count == 0)
                    return true;
                if (weapons.Count == 1)
                    return weapons.Find(w => w.ItemHolder.Data.ID == DefaultWeaponID);
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