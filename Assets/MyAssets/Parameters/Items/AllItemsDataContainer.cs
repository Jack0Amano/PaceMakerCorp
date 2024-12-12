using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEditor;
using static Utility;


namespace Parameters.Items
{
    /// <summary>
    /// すべての装備の実データを保存しておく 静的データ
    /// </summary>
    [Serializable]
    public class AllItemsDataContainer : ScriptableObject
    {
        public List<ItemData> Items = new List<ItemData>();

        /// <summary>
        /// IDから装備の実Classを取得する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ItemData GetItemFromID(string id)
        {
            var equip = Items.Find(e => e.ID == id);
            if (equip == null)
                return ItemData.Default();

            return Items.Find(e => e.ID == id);
        }

        /// <summary>
        /// 与えられたcontainerの内容を統合する
        /// </summary>
        /// <param name="container"></param>
        public void Combine(AllItemsDataContainer container)
        {
            Items.AddRange(container.Items);
        }

        /// <summary>
        /// 空のDataContainerを作成する
        /// </summary>
        /// <returns></returns>
        public static AllItemsDataContainer CreateEmptyData()
        {
            return ScriptableObject.CreateInstance<AllItemsDataContainer>();
        }

        public override string ToString()
        {
            return $"EquipmentList: {Items.Count} items is loaded.";
        }

        #region Shop関連
        /// <summary>
        /// 店に並べるアイテム
        /// </summary>
        public List<ItemData> itemsInShop
        {
            get
            {
                var currentLevel = GameManager.Instance.DataSavingController.SaveDataInfo.shopLevel;
                var refresh = !currentLevel.Equals(_shopLevel);
                if (refresh)
                {
                    _itemsInShop = GetItemsUnderLevel(currentLevel);
                    _shopLevel = currentLevel;
                }
                return _itemsInShop;
            }
        }
        private ShopLevel _shopLevel;
        private List<ItemData> _itemsInShop;

        /// <summary>
        /// 自軍のアイテム出現レベル以下のアイテムを取得する
        /// </summary>
        /// <param name="myLevel"></param>
        /// <returns></returns>
        private List<ItemData> GetItemsUnderLevel(ShopLevel myLevel)
        {
            return Items.FindAll(item =>
            {
                return item.ShopLevel.IsLowerThan(myLevel);
            });
        }
        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AllItemsDataContainer))]
    public class AllItemsDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AllItemsDataContainer myTarget = (AllItemsDataContainer)target;
            DrawDefaultInspector();
        }
    }
#endif
}