/*
 * * * * This bare-bones script was auto-generated * * * *
 * The code commented with "/ * * /" demonstrates how data is retrieved and passed to the adapter, plus other common commands. You can remove/replace it once you've got the idea
 * Complete it according to your specific use-case
 * Consult the Example scripts if you get stuck, as they provide solutions to most common scenarios
 * 
 * Main terms to understand:
 *		Model = class that contains the data associated with an item (title, content, icon etc.)
 *		Views Holder = class that contains references to your views (Text, Image, MonoBehavior, etc.)
 * 
 * Default expected UI hiererchy:
 *	  ...
 *		-Canvas
 *		  ...
 *			-MyScrollViewAdapter
 *				-Viewport
 *					-Content
 *				-Scrollbar (Optional)
 *				-ItemPrefab (Optional)
 * 
 * Note: If using Visual Studio and opening generated scripts for the first time, sometimes Intellisense (autocompletion)
 * won't work. This is a well-known bug and the solution is here: https://developercommunity.visualstudio.com/content/problem/130597/unity-intellisense-not-working-after-creating-new-1.html (or google "unity intellisense not working new script")
 * 
 * 
 * Please read the manual under "/Docs", as it contains everything you need to know in order to get started, including FAQ
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using frame8.Logic.Misc.Other.Extensions;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using TMPro;
using DG.Tweening;

// You should modify the namespace to your own or - if you're sure there won't ever be conflicts - remove it altogether
namespace MainMap.UI.SelectItem
{
    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    public class SelectItemListAdapter : OSA<BaseParamsWithPrefab, MyListItemViewsHolder>
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public SimpleDataHelper<MyListItemModel> Data { get; private set; }
        /// <summary>
        /// 現在選択中のIndex (選択中でない場合は-1）
        /// </summary>
        public int selectIndex { private set; get; } = -1;
        /// <summary>
        /// Diselectされた場合のIndexは-1
        /// </summary>
        public Action<object, int> viewsHolderOnClick;
        /// <summary>
        /// アイテムを装備するボタンを選択したときの呼び出し
        /// </summary>
        public Action<int> setItemAction;

        private CanvasGroup CanvasGroup;
        private Sequence ShowHideAnimation;

        /// <summary>
        /// アニメーション付きで表示非表示を切り替え
        /// </summary>
        public bool IsShown
        {
            get => _IsShown;
            set
            {
                if (_IsShown == value) return;
                _IsShown = value;
                ShowHideAnimation?.Kill(true);
                ShowHideAnimation = DOTween.Sequence();
                if (value)
                {
                    CanvasGroup.alpha = 0;
                    gameObject.SetActive(true);
                    ShowHideAnimation.Append(CanvasGroup.DOFade(1, 0.3f));
                }
                else
                {
                    ShowHideAnimation.Append(CanvasGroup.DOFade(0, 0.3f));
                    ShowHideAnimation.OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                    });
                }
                ShowHideAnimation.Play();
            }
        }
        private bool _IsShown = true;

        #region OSA implementation
        protected override void Awake()
        {
            base.Awake();
        }
        protected override void Start()
        {
            

            // Calling this initializes internal data and prepares the adapter to handle item count changes
            base.Start();
            Data = new SimpleDataHelper<MyListItemModel>(this);
            CanvasGroup = GetComponent<CanvasGroup>();
            // Retrieve the models from your data source and set the items count
            /*
            RetrieveDataAndUpdate(500);
            */
        }

        // This is called initially, as many times as needed to fill the viewport, 
        // and anytime the viewport's size grows, thus allowing more items to be displayed
        // Here you create the "ViewsHolder" instance whose views will be re-used
        // *For the method's full description check the base implementation
        protected override MyListItemViewsHolder CreateViewsHolder(int itemIndex)
        {
            var instance = new MyListItemViewsHolder();

            // Using this shortcut spares you from:
            // - instantiating the prefab yourself
            // - enabling the instance game object
            // - setting its index 
            // - calling its CollectViews()
            instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
            instance.button.onClick.AddListener(() =>
            {
                ViewsHolderOnClick(instance);
            });
            instance.setItemButton.onClick.AddListener(() =>
            {
                setItemAction?.Invoke(instance.ItemIndex);
            });

            return instance;
        }

        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateViewsHolder(MyListItemViewsHolder newOrRecycled)
        {
            // In this callback, "newOrRecycled.ItemIndex" is guaranteed to always reflect the
            // index of item that should be represented by this views holder. You'll use this index
            // to retrieve the model from your data set
            var model = Data[newOrRecycled.ItemIndex];

            newOrRecycled.itemNameLabel.text = model.itemName;

            SetText(newOrRecycled.BulletObject, newOrRecycled.BulletLabel, model.targetValue, "None");
            SetText(newOrRecycled.AntiPersAttackObject, newOrRecycled.AntiPersAttackLabel, model.attackValue, "0");
            SetText(newOrRecycled.defenceObject, newOrRecycled.defenceLabel, model.defenceValue, "0");
            SetText(newOrRecycled.rangeObject, newOrRecycled.rangeLabel, model.rangeValue, "0");
            SetText(newOrRecycled.supplyObject, newOrRecycled.supplyLabel, model.supplyValue, "0");
            //SetText(newOrRecycled.countObject, newOrRecycled.countLabel, model.ownCount, "");
            SetText(newOrRecycled.costObject, newOrRecycled.costLabel, model.cost, "");

            newOrRecycled.SelectViewsHolder(model.isSelected, false);
        }

        /// <summary>
        /// 指定したTextLabelに文字を入れる、valueがnoneValueと同じ場合textLabelは非表示になる
        /// </summary>
        private void SetText(GameObject obj, TextMeshProUGUI label, string value, string noneValue)
        {
            if (noneValue.Equals(value))
            {
                if (obj.activeSelf)
                    obj.SetActive(false);
            }
            else
            {
                if (!obj.activeSelf)
                    obj.SetActive(true);
                label.text = value;
            }
        }
        #endregion

        // These are common data manipulation methods
        // The list containing the models is managed by you. The adapter only manages the items' sizes and the count
        // The adapter needs to be notified of any change that occurs in the data list. Methods for each
        // case are provided: Refresh, ResetItems, InsertItems, RemoveItems
        #region data manipulation
        public void AddItemsAt(int index, IList<MyListItemModel> items)
        {
            // Commented: the below 2 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.InsertRange(index, items);
            //InsertItems(index, items.Length);

            Data.InsertItems(index, items);
        }

        public void RemoveItemsFrom(int index, int count)
        {
            // Commented: the below 2 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.RemoveRange(index, count);
            //RemoveItems(index, count);

            Data.RemoveItems(index, count);
        }

        public void SetItems(IList<MyListItemModel> items)
        {
            // Commented: the below 3 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.Clear();
            //YourList.AddRange(items);
            //ResetItems(YourList.Count);
            Data.ResetItems(items);
        }
        #endregion


        /// <summary>
        /// ViewHolderの選択を解除する
        /// </summary>
        public void Deselect()
        {
            
            var index = Data.List.FindIndex(l => l.isSelected);
            if (index != -1)
            {
                // 以前までセレクトされていたViewsHolderを選択解除する
                // 同じViewHolderを選択した場合を除く
                Data[index].isSelected = false;
                var _oldViewsHolder = GetBaseItemViewsHolderIfVisible(index);
                if (_oldViewsHolder != null && _oldViewsHolder is MyListItemViewsHolder)
                {
                    var oldViewsHolder = (MyListItemViewsHolder)_oldViewsHolder;
                    oldViewsHolder.SelectViewsHolder(false, true);
                }
            }
        }

        /// <summary>
        /// ViewsHolderが選択された時の呼び出し
        /// </summary>
        /// <param name="viewsHolder"></param>
        private void ViewsHolderOnClick(MyListItemViewsHolder viewsHolder)
        {
            var index = Data.List.FindIndex(l => l.isSelected);
            if (index != -1 && index != viewsHolder.ItemIndex)
            {
                // 以前までセレクトされていたViewsHolderを選択解除する
                // 同じViewHolderを選択した場合を除く
                Data[index].isSelected = false;
                var _oldViewsHolder = GetBaseItemViewsHolderIfVisible(index);
                if (_oldViewsHolder != null && _oldViewsHolder is MyListItemViewsHolder)
                {
                    var oldViewsHolder = (MyListItemViewsHolder)_oldViewsHolder;
                    oldViewsHolder.SelectViewsHolder(false, true);
                }
            }

            if (Data[viewsHolder.ItemIndex].isSelected)
            {
                // 既に選択されているViewsHolderを選択したため選択解除
                Data[viewsHolder.ItemIndex].isSelected = false;
                viewsHolder.SelectViewsHolder(false, true);
                selectIndex = -1;
                viewsHolderOnClick?.Invoke(this, -1);
            }
            else
            {
                // 新しいViewHolderを選択した時
                Data[viewsHolder.ItemIndex].isSelected = true;
                viewsHolder.SelectViewsHolder(true, true);
                selectIndex = viewsHolder.ItemIndex;
                viewsHolderOnClick?.Invoke(this, viewsHolder.ItemIndex);
            }
        }
    }

    // Class containing the data associated with an item
    public class MyListItemModel
    {
        public string itemName;
        public string attackValue;
        public string defenceValue;
        public string supplyValue;
        /// <summary>
        /// 武器の射程距離
        /// </summary>
        public string rangeValue;
        /// <summary>
        /// 武器の対応target
        /// </summary>
        public string targetValue;
        /// <summary>
        /// 使用するのに必要なコスト
        /// </summary>
        public string cost;
        // public string ownCount;

        internal bool isSelected = false;
    }


    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    public class MyListItemViewsHolder : BaseItemViewsHolder
    {
        public RectTransform panel;

        public TextMeshProUGUI itemNameLabel;
        public TextMeshProUGUI itemCountLabel;

        public TextMeshProUGUI costLabel;
        public GameObject costObject;

        public GameObject AntiPersAttackObject;
        public TextMeshProUGUI AntiPersAttackLabel;

        public GameObject AntiTankAttackObject;
        public TextMeshProUGUI AntiTankAttackLabel;

        public GameObject defenceObject;
        public TextMeshProUGUI defenceLabel;

        public GameObject supplyObject;
        public TextMeshProUGUI supplyLabel;

        public GameObject rangeObject;
        public TextMeshProUGUI rangeLabel;

        public GameObject BulletObject;
        public TextMeshProUGUI BulletLabel;

        public CanvasGroup selectPanel;

        public Button setItemButton;
        public Button button;

        private readonly float selectViewsHolderX = 15f;
        private readonly float animationDuration = 0.3f;

        private bool isSelected = false;


        // Retrieving the views from the item's root GameObject
        public override void CollectViews()
        {
            base.CollectViews();

            // GetComponentAtPath is a handy extension method from frame8.Logic.Misc.Other.Extensions
            // which infers the variable's component from its type, so you won't need to specify it yourself
            root.GetComponentAtPath("Panel", out panel);

            root.GetComponentAtPath("Panel/Name", out itemNameLabel);
            root.GetComponentAtPath("Panel/Count", out itemCountLabel);

            root.GetComponentAtPath("Panel/Cost", out costLabel);
            costObject = costLabel.gameObject;

            root.GetComponentAtPath("Panel/Values/AntiPersAttack/AttackValue", out AntiPersAttackLabel);
            AntiPersAttackObject = AntiPersAttackLabel.transform.parent.gameObject;

            root.GetComponentAtPath("Panel/Values/AntiTankAttack/AttackValue", out AntiTankAttackLabel);
            AntiTankAttackObject = AntiTankAttackLabel.transform.parent.gameObject;

            root.GetComponentAtPath("Panel/Values/Defence/DefenceValue", out defenceLabel);
            defenceObject = defenceLabel.transform.parent.gameObject;

            root.GetComponentAtPath("Panel/Values/Supply/SupplyValue", out supplyLabel);
            supplyObject = supplyLabel.transform.parent.gameObject;

            root.GetComponentAtPath("Panel/Values/Range/RangeValue", out rangeLabel);
            rangeObject = rangeLabel.transform.parent.gameObject;

            root.GetComponentAtPath("Panel/Values/Bullet/BulletValue", out BulletLabel);
            BulletObject = BulletLabel.transform.parent.gameObject;

            root.GetComponentAtPath("Selection", out selectPanel);
            selectPanel.alpha = 0;

            root.GetComponentAtPath("SetItemButton", out setItemButton);

            button = root.GetComponent<Button>();
        }

        /// <summary>
        /// ViewsHolderが選択された時のアニメーション
        /// </summary>
        /// <param name="select"></param>
        /// <param name="animation"></param>
        public void SelectViewsHolder(bool select, bool animation)
        {
            if (isSelected != select)
            {
                if (animation)
                {
                    var seq = DOTween.Sequence();
                    seq.Append(panel.DOAnchorPosX(select ? selectViewsHolderX : 0, animationDuration));
                    seq.Join(selectPanel.DOFade(select ? 1 : 0, animationDuration));
                    seq.Play();
                    isSelected = select;
                }
                else
                {
                    panel.anchoredPosition = new Vector2(select ? selectViewsHolderX : 0,
                                                         panel.anchoredPosition.y);
                    selectPanel.alpha = select ? 1 : 0;
                }
            }

        }
    }
}
