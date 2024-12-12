using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using static Utility;
using MainMap.UI.Shop;

namespace MainMap.UI.Item
{
    public class ItemPanel : MainMapUIPanel
    {
        [SerializeField] ItemsBasicListAdapter itemsBasicListAdapter;
        [SerializeField] Tabbar tabbar;
        [SerializeField] ItemDetail itemDetail;

        private List<ItemInList> showItems;
        private List<ItemType> tabTypeList;
        private ItemType currentTabType = ItemType.Rifle;
        private GameManager GameManager;

        protected override void Awake()
        {
            base.Awake();
            // タブ表示の初期化
            tabTypeList = Enum.GetValues(typeof(ItemType)).Cast<ItemType>().ToList();
            tabbar.tabButtonClicked += TabButtonClicked;

            itemsBasicListAdapter.viewsHolderOnClick += SelectViewsHolder;
            GameManager = GameManager.Instance;
        }

        protected private void Start()
        {
            // タブ表示の初期化
            var typeValues = Enum.GetNames(typeof(ItemType)).OfType<string>().ToList();
            var tabs = typeValues.ConvertAll(t =>
            {
                var name = GameManager.Translation.CommonUserInterfaceIni.ReadValue("EquipmentType", t, t);
                return name;
            }); 
            tabbar.SetTabs(tabs);
        }

        internal override void Show()
        {
            base.Show();

            ShowOwnItems(ItemType.None);
        }

        /// <summary>
        /// ListViewのViewHolderが選択された時の呼び出し
        /// </summary>
        /// <param name="o"></param>
        /// <param name="index"></param>
        private void SelectViewsHolder(object o, int index)
        {
            if (index == -1)
                itemDetail.Hide();
            else
                itemDetail.Show(showItems[index].data);
        }

        /// <summary>
        /// タブボタンが変更された時の
        /// </summary>
        /// <param name="o"></param>
        /// <param name="index"></param>
        private void TabButtonClicked(object o, int index)
        {
            tabbar.ableToChangeTab = !itemsBasicListAdapter.isDrawing;
            if (itemsBasicListAdapter.isDrawing)
                return;

            itemDetail.Hide();
            ShowOwnItems(tabTypeList[index]);
        }

        /// <summary>
        /// リストにEquipmentTypeに合致するものをセットする
        /// </summary>
        private void ShowAllItems(ItemType equipmentType)
        {
            if (currentTabType.Equals(equipmentType))
                return;
            currentTabType = equipmentType;
            if (equipmentType == ItemType.None)
            {
                // TODO　すべてのアイテムを持っていることになっている -> OwnItemのみに絞る
                // すべてののアイテムを表示
                showItems = new List<ItemInList>(GameManager.Instance.DataSavingController.MyArmyData.ItemController.ItemSet);
                
            }
            else
            {
                showItems = GameManager.Instance.DataSavingController.MyArmyData.ItemController.ItemSet.FindAll(i => i.data.ItemType == equipmentType);
            }

            var nullItems = showItems.FindAll(i => i.data == null);
            if (nullItems.Count != 0)
                PrintWarning($"{nullItems.Count} items data is null.\n", string.Join("\n", nullItems));
            nullItems.ForEach(i => showItems.Remove(i));

            itemsBasicListAdapter.RetrieveDataAndUpdate(showItems);
        }

        /// <summary>
        /// リストに所持しているEquipmentを表示する
        /// </summary>
        /// <param name="equipmentType"></param>
        private void ShowOwnItems(ItemType equipmentType, bool forceUpdate = false)
        {
            if (currentTabType.Equals(equipmentType) && forceUpdate == false)
                return;
            currentTabType = equipmentType;

            showItems = GameManager.Instance.DataSavingController.MyArmyData.OwnItems.ConvertAll(e => new ItemInList(e));

            var nullItems = showItems.FindAll(i => i.data == null);
            if (nullItems.Count != 0)
                PrintWarning($"{nullItems.Count} items data is null.\n", string.Join("\n", nullItems));
            nullItems.ForEach(i => showItems.Remove(i));

            if (equipmentType != ItemType.None)
                showItems = showItems.FindAll(i => i.data.ItemType.Equals(equipmentType));

            itemsBasicListAdapter.RetrieveDataAndUpdate(showItems);
        }

        internal override void Hide(Action onCompletion = null)
        {
            base.Hide(onCompletion);
            itemsBasicListAdapter.Deselect();
            itemDetail.Hide();
        }
    }
}