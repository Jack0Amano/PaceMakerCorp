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
using System.Linq;
using TMPro;
using DG.Tweening;
using static Utility;

// You should modify the namespace to your own or - if you're sure there won't ever be conflicts - remove it altogether
namespace MainMap.UI.Shop
{
    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    public class ItemsBasicListAdapter : OSA<BaseParamsWithPrefab, ItemListItemViewsHolder>
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public SimpleDataHelper<ItemListItemModel> Data { get; private set; }

        public List<ItemInList> items { private set; get; }

        /// <summary>
        /// Diselectされた場合のIndexは-1
        /// </summary>
        public Action<object, int> viewsHolderOnClick;
        /// <summary>
        /// 現在選択中のIndex (選択中でない場合は-1）
        /// </summary>
        public int selectIndex { private set; get; } = -1;

        /// <summary>
        /// 描写中かどうか
        /// </summary>
        public bool isDrawing { private set; get; } = false;

        public CanvasGroup canvasGroup { private set; get; }

        protected override void Awake()
        {
            base.Awake();
            canvasGroup = GetComponent<CanvasGroup>();
        }


        #region OSA implementation
        protected override void Start()
        {
            Data = new SimpleDataHelper<ItemListItemModel>(this);

            // Calling this initializes internal data and prepares the adapter to handle item count changes
            base.Start();

            // Retrieve the models from your data source and set the items count
        }

        // This is called initially, as many times as needed to fill the viewport, 
        // and anytime the viewport's size grows, thus allowing more items to be displayed
        // Here you create the "ViewsHolder" instance whose views will be re-used
        // *For the method's full description check the base implementation
    protected override ItemListItemViewsHolder CreateViewsHolder(int itemIndex)
    {
        var instance = new ItemListItemViewsHolder();

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

        return instance;
    }

        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
    protected override void UpdateViewsHolder(ItemListItemViewsHolder newOrRecycled)
    {
        // In this callback, "newOrRecycled.ItemIndex" is guaranteed to always reflect the
        // index of item that should be represented by this views holder. You'll use this index
        // to retrieve the model from your data set

        ItemListItemModel model = Data[newOrRecycled.ItemIndex];

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
            if (noneValue.Equals( value))
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

        // This is the best place to clear an item's views in order to prepare it from being recycled, but this is not always needed, 
        // especially if the views' values are being overwritten anyway. Instead, this can be used to, for example, cancel an image 
        // download request, if it's still in progress when the item goes out of the viewport.
        // <newItemIndex> will be non-negative if this item will be recycled as opposed to just being disabled
        // *For the method's full description check the base implementation
        /*
        protected override void OnBeforeRecycleOrDisableViewsHolder(MyListItemViewsHolder inRecycleBinOrVisible, int newItemIndex)
        {
            base.OnBeforeRecycleOrDisableViewsHolder(inRecycleBinOrVisible, newItemIndex);
        }
        */

        // You only need to care about this if changing the item count by other means than ResetItems, 
        // case in which the existing items will not be re-created, but only their indices will change.
        // Even if you do this, you may still not need it if your item's views don't depend on the physical position 
        // in the content, but they depend exclusively to the data inside the model (this is the most common scenario).
        // In this particular case, we want the item's index to be displayed and also to not be stored inside the model,
        // so we update its title when its index changes. At this point, the Data list is already updated and 
        // shiftedViewsHolder.ItemIndex was correctly shifted so you can use it to retrieve the associated model
        // Also check the base implementation for complementary info
        /*
        protected override void OnItemIndexChangedDueInsertOrRemove(MyListItemViewsHolder shiftedViewsHolder, int oldIndex, bool wasInsert, int removeOrInsertIndex)
        {
            base.OnItemIndexChangedDueInsertOrRemove(shiftedViewsHolder, oldIndex, wasInsert, removeOrInsertIndex);

            shiftedViewsHolder.titleText.text = Data[shiftedViewsHolder.ItemIndex].title + " #" + shiftedViewsHolder.ItemIndex;
        }
        */
        #endregion

        // These are common data manipulation methods
        // The list containing the models is managed by you. The adapter only manages the items' sizes and the count
        // The adapter needs to be notified of any change that occurs in the data list. Methods for each
        // case are provided: Refresh, ResetItems, InsertItems, RemoveItems
        #region data manipulation
        public void AddItemsAt(int index, IList<ItemListItemModel> items)
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

        public void SetItems(IList<ItemListItemModel> items)
        {
            // Commented: the below 3 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.Clear();
            //YourList.AddRange(items);
            //ResetItems(YourList.Count);

            Data.ResetItems(items);
        }

        public void UpdateItem(int index)
        {
            var data = items[index].data;
            var own = items[index].own;
            Data[index].attackValue = data.Attack.ToString();
            Data[index].defenceValue = data.Defence.ToString();
            Data[index].itemName = data.Name;
            Data[index].ownCount = $"{own.FreeCount}/{own.TotalCount}";
            Data[index].rangeValue = data.Range.ToString();
            Data[index].supplyValue = data.Supply.ToString();
            Data[index].targetValue = data.TargetTypeName;

            ForceUpdateViewsHolderIfVisible(index);
        }

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
                if (_oldViewsHolder != null && _oldViewsHolder is ItemListItemViewsHolder)
                {
                    var oldViewsHolder = (ItemListItemViewsHolder)_oldViewsHolder;
                    oldViewsHolder.SelectViewsHolder(false, true);
                }
            }
        }
        #endregion

        // Here, we're requesting <count> items from the data source
        public void RetrieveDataAndUpdate(List<ItemInList> items)
        {
            isDrawing = true;

            if (Data != null && Data.Count != 0)
                Data.RemoveItemsFromStart(Data.Count);
            this.items = items;

            // showEquipments.Sort((a, b) => a.data.equipType.GetHashCode() - b.data.equipType.GetHashCode());

            StartCoroutine(FetchMoreItemsFromDataSourceAndUpdate(this.items.Count));
        }

        // Retrieving <count> models from the data source and calling OnDataRetrieved after.
        // In a real case scenario, you'd query your server, your database or whatever is your data source and call OnDataRetrieved after
        IEnumerator FetchMoreItemsFromDataSourceAndUpdate(int count)
        {
            // Simulating data retrieving delay
            yield return new WaitForSeconds(.5f);
            
            var newItems = new ItemListItemModel[count];

            // Retrieve your data here
            for (int i = 0; i < count; ++i)
            {
                var equip = items[i];
                var model = new ItemListItemModel()
                {
                    itemName = equip.data.Name,
                    targetValue = equip.data.TargetTypeName,
                    attackValue = equip.data.Attack.ToString(),
                    defenceValue = equip.data.Defence.ToString(),
                    rangeValue = equip.data.Range.ToString(),
                    supplyValue = equip.data.Supply.ToString(),
                    ownCount = equip.own != null ? $"{equip.own.FreeCount}/{equip.own.TotalCount}" : "",
                };

                newItems[i] = model;
            }

            OnDataRetrieved(newItems);

            isDrawing = false;
        }

        void OnDataRetrieved(ItemListItemModel[] newItems)
        {
            Data.InsertItemsAtEnd(newItems);
        }

        /// <summary>
        /// ViewsHolderが選択された時の呼び出し
        /// </summary>
        /// <param name="viewsHolder"></param>
        private void ViewsHolderOnClick(ItemListItemViewsHolder viewsHolder)
        {
            var index = Data.List.FindIndex(l => l.isSelected);
            if (index != -1 && index != viewsHolder.ItemIndex)
            {
                // 以前までセレクトされていたViewsHolderを選択解除する
                // 同じViewHolderを選択した場合を除く
                Data[index].isSelected = false;
                var _oldViewsHolder =  GetBaseItemViewsHolderIfVisible(index);
                if (_oldViewsHolder != null && _oldViewsHolder is ItemListItemViewsHolder)
                {
                    var oldViewsHolder = (ItemListItemViewsHolder)_oldViewsHolder;
                    oldViewsHolder.SelectViewsHolder(false, true);
                }
            }
            
            if (Data[viewsHolder.ItemIndex].isSelected )
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
    public class ItemListItemModel
    {
        public string itemName;
        public string attackValue;
        public string defenceValue;
        public string supplyValue;
        public string rangeValue;
        public string targetValue;
        public string cost;
        public string ownCount;

        public bool isSelected;
    }


    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    public class ItemListItemViewsHolder : BaseItemViewsHolder
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

            button = root.GetComponent<Button>();
        }

        // Override this if you have children layout groups or a ContentSizeFitter on root that you'll use. 
        // They need to be marked for rebuild when this callback is fired
        /*
        public override void MarkForRebuild()
        {
            base.MarkForRebuild();

            LayoutRebuilder.MarkLayoutForRebuild(yourChildLayout1);
            LayoutRebuilder.MarkLayoutForRebuild(yourChildLayout2);
            YourSizeFitterOnRoot.enabled = true;
        }
        */

        // Override this if you've also overridden MarkForRebuild() and you have enabled size fitters there (like a ContentSizeFitter)
        /*
        public override void UnmarkForRebuild()
        {
            YourSizeFitterOnRoot.enabled = false;

            base.UnmarkForRebuild();
        }
        */

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
