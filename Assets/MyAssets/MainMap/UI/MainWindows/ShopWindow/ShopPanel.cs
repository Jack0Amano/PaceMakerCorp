using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using static Utility;

namespace MainMap.UI.Shop
{

    public class ShopPanel : MainMapUIPanel
    {
        [SerializeField] TextMeshProUGUI capitalFundLabel;
        [SerializeField] ItemsBasicListAdapter itemsBasicListAdapter;
        [SerializeField] Tabbar tabbar;
        [SerializeField] ShopItemDetail itemDetail;
        [SerializeField] Button buyTabButton;
        [SerializeField] Button sellTabButton;

        private Image buyTabImage;
        private Image sellTabImage;
        private Sprite selectImage;
        private Sprite deselectImage;
        private TextMeshProUGUI buyTabLabel;
        private TextMeshProUGUI sellTabLabel;
        private Color selectedLabelColor;
        private Color deselectedLabelColor;

        private List<ItemType> tabTypeList;
        private ItemType currentTabType = ItemType.Rifle;
        private List<ItemInList> showItemsInList;
        /// <summary>
        /// 現在のトレードのタイプ Buy or Sell
        /// </summary>
        private TradeType currentTradeType = TradeType.Buy;

        private GameManager GameManager;

        override protected void Awake()
        {
            GameManager = GameManager.Instance;
            base.Awake();
            // 各ラベルの内容を初期設定

            // BuySEllラベルの変更  (武器をアンロックする形にしたため削除・１つ買えば部隊全体に適用できる)
            //buyTabImage = buyTabButton.GetComponent<Image>();
            //sellTabImage = sellTabButton.GetComponent<Image>();
            //buyTabLabel = buyTabButton.GetComponentInChildren<TextMeshProUGUI>();
            //sellTabLabel = sellTabButton.GetComponentInChildren<TextMeshProUGUI>();
            //selectedLabelColor = buyTabLabel.color;
            //deselectedLabelColor = sellTabLabel.color;
            //selectImage = buyTabImage.sprite;
            //deselectImage = sellTabImage.sprite;
            //buyTabButton.onClick.AddListener(() =>
            //{
            //    if (!currentTabType.Equals(TradeType.Buy))
            //        ChangeTradeType(TradeType.Buy);
            //});
            //sellTabButton.onClick.AddListener(() =>
            //{
            //    if (!currentTabType.Equals(TradeType.Sell))
            //        ChangeTradeType(TradeType.Sell);
            //});

            // 各パネルにDelegateを設置
            itemDetail.tradeIsCompleted += TradeIsCompleted;
            itemsBasicListAdapter.viewsHolderOnClick += SelectViewsHolder;

            itemDetail.parentWindowCanvasGroup = parentWindowCanvasGroup;

            // タブ表示の初期化
            tabTypeList = Enum.GetValues(typeof(ItemType)).Cast<ItemType>().ToList();
            tabbar.tabButtonClicked += TabButtonClicked;
        }

        protected private void Start()
        {
            // タブ表示の初期化
            var typeValues = Enum.GetNames(typeof(ItemType)).OfType<string>().ToList();
            var tabs = typeValues.ConvertAll(t =>
            {
                return GameManager.Translation.CommonUserInterfaceIni.ReadValue("EquipmentType", t, t);
            }); ;
            tabbar.SetTabs(tabs);
        }

        internal override void Show()
        {
            base.Show();
            // Listの表示
            ShowShopItems(ItemType.None);
            capitalFundLabel.SetText("$" + GameManager.DataSavingController.SaveDataInfo.capitalFund.ToString("#,0"));
        }

        /// <summary>
        /// リストにEquipmentTypeに合致するものをセットする
        /// </summary>
        private void ShowShopItems(ItemType equipmentType, bool forceUpdate = false)
        {
            if (currentTabType.Equals(equipmentType) && forceUpdate == false)
                return;
            currentTabType = equipmentType;
            if (equipmentType == ItemType.None)
            {
                // すべてのショップのアイテムを表示
                showItemsInList = GameManager.DataSavingController.MyArmyData.ItemController.ItemSet;
            }
            else
            {
                showItemsInList = GameManager.DataSavingController.MyArmyData.ItemController.ItemSet.FindAll(i => i.data.ItemType == equipmentType);
            }
            itemsBasicListAdapter.RetrieveDataAndUpdate(showItemsInList);
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

            if(equipmentType == ItemType.None)
            {
                showItemsInList = GameManager.DataSavingController.MyArmyData.OwnItems.ConvertAll(e => new ItemInList(e));

            }else
            {
                var all = GameManager.DataSavingController.MyArmyData.OwnItems.ConvertAll(e => new ItemInList(e));
                showItemsInList = all.FindAll(i => i.data.ItemType.Equals(equipmentType));
            }

            var nullItems = showItemsInList.FindAll(i => i.data == null);
            if (nullItems.Count != 0)
                PrintWarning($"{nullItems.Count} items data is null.\n", string.Join("\n", nullItems));
            nullItems.ForEach(i => showItemsInList.Remove(i));

            itemsBasicListAdapter.RetrieveDataAndUpdate(showItemsInList);
        }

        /// <summary>
        /// タブボタンによるアイテムの変更
        /// </summary>
        /// <param name="o"></param>
        /// <param name="index"></param>
        private void TabButtonClicked(object o, int index)
        {
            tabbar.ableToChangeTab = !itemsBasicListAdapter.isDrawing;
            if (itemsBasicListAdapter.isDrawing)
                return;
            itemDetail.Hide();
            if (currentTradeType == TradeType.Buy)
                ShowShopItems(tabTypeList[index]);
            else if (currentTradeType == TradeType.Sell)
                ShowOwnItems(tabTypeList[index]);
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
            {
                if (currentTradeType == TradeType.Buy)
                    itemDetail.ShowBuyItem(showItemsInList[index]);
                else
                    itemDetail.ShowSellItem(showItemsInList[index]);
            }
        }

        /// <summary>
        /// ボタンタイプから取引内容を変更
        /// </summary>
        /// <param name="type"></param>
        private void ChangeTradeType(TradeType type)
        {
            if (itemsBasicListAdapter.isDrawing)
                return;

            if (currentTradeType.Equals(type))
                return;
            currentTradeType = type;

            if (type.Equals(TradeType.Buy)){
                // 購入モード
                buyTabImage.sprite = selectImage;
                buyTabLabel.color = selectedLabelColor;
                sellTabImage.sprite = deselectImage;
                sellTabLabel.color = deselectedLabelColor;

                ShowShopItems(ItemType.None, forceUpdate: true);
            }
            else
            {
                // 売却モード
                buyTabImage.sprite = deselectImage;
                buyTabLabel.color = deselectedLabelColor;
                sellTabImage.sprite = selectImage;
                sellTabLabel.color = selectedLabelColor;

                ShowOwnItems(ItemType.None, forceUpdate: true) ;
            }
            itemDetail.Hide();
        }

        /// <summary>
        /// 品物が売買が行われたときに呼び出される
        /// </summary>
        private void TradeIsCompleted()
        {
            if (currentTradeType == TradeType.Buy)
            {
                // 購入が行われた
                var selectIndex = itemsBasicListAdapter.selectIndex;
                if (selectIndex != -1)
                {
                    showItemsInList.RemoveAt(selectIndex);
                    itemsBasicListAdapter.RemoveItemsFrom(selectIndex, 1);
                    // TODO: Thank画面とかなんかリアクション
                    itemDetail.Hide();
                }
                    
            }
            else
            {
                // アイテムの売却が行われた
                var selectIndex = itemsBasicListAdapter.selectIndex;
                if (itemsBasicListAdapter.items[selectIndex].own.TotalCount == 0)
                {
                    // OwnItemの数が0になった場合 Listから削除し Detailも非表示にする
                    print(selectIndex);
                    showItemsInList.RemoveAt(selectIndex);
                    itemsBasicListAdapter.RemoveItemsFrom(selectIndex, 1);
                    itemDetail.Hide();
                }
                else
                {
                    // アイテム所持数が減少したためListの中の数字も少なくする
                    itemsBasicListAdapter.UpdateItem(selectIndex);
                }
            }

            capitalFundLabel.SetText(GameManager.DataSavingController.SaveDataInfo.capitalFund.ToString("C"));
        }

        internal override void Hide(Action onCompletion = null)
        {
            itemsBasicListAdapter.Deselect();
            itemDetail.Hide();
            base.Hide(onCompletion);
        }
    }

    internal enum TradeType
    {
        Buy,
        Sell
    }
}