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
namespace MainMap.UI.SelectUnit
{
    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    public class SelectUnitListAdapter : OSA<BaseParamsWithPrefab, MyListUnitViewsHolder>
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public SimpleDataHelper<MyListUnitModel> Data { get; private set; }
        /// <summary>
        /// 現在選択中のIndex (選択中でない場合は-1）
        /// </summary>
        public int SelectIndex { private set; get; } = -1;
        /// <summary>
        /// Diselectされた場合のIndexは-1
        /// </summary>
        public Action<object, int> viewsHolderOnClick;
        /// <summary>
        /// Unitを加えるときの呼び出し
        /// </summary>
        public Action<int> setUnitAction;


        #region OSA implementation
        protected override void Awake()
        {
            Data = new SimpleDataHelper<MyListUnitModel>(this);
            base.Awake();
        }

        protected override void Start()
        {
            // Calling this initializes internal data and prepares the adapter to handle item count changes
            base.Start();

            // Retrieve the models from your data source and set the items count
            /*
            RetrieveDataAndUpdate(500);
            */
        }

        // This is called initially, as many times as needed to fill the viewport, 
        // and anytime the viewport's size grows, thus allowing more items to be displayed
        // Here you create the "ViewsHolder" instance whose views will be re-used
        // *For the method's full description check the base implementation
        protected override MyListUnitViewsHolder CreateViewsHolder(int itemIndex)
        {
            var instance = new MyListUnitViewsHolder();

            // Using this shortcut spares you from:
            // - instantiating the prefab yourself
            // - enabling the instance game object
            // - setting its index 
            // - calling its CollectViews()
            instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
            instance.selectUnitButton.onClick.AddListener(() =>
            {
                ViewsHolderOnClick(instance);
            });
            instance.addUnitButton.onClick.AddListener(() =>
            {
                setUnitAction?.Invoke(instance.ItemIndex);
            });

            return instance;
        }

        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateViewsHolder(MyListUnitViewsHolder newOrRecycled)
        {
            // In this callback, "newOrRecycled.ItemIndex" is guaranteed to always reflect the
            // index of item that should be represented by this views holder. You'll use this index
            // to retrieve the model from your data set
            var model = Data[newOrRecycled.ItemIndex];

            newOrRecycled.nameLabel.SetText(model.name);
            newOrRecycled.levelLabel.SetText(model.level);
            newOrRecycled.healthLabel.SetText(model.health);
            newOrRecycled.primaryWeaponLabel.SetText(model.primaryWeapon);
            newOrRecycled.energyLabel.SetText(model.energy);
            newOrRecycled.SelectViewsHolder(model.IsSelected, false);

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
        public void AddItemsAt(int index, IList<MyListUnitModel> items)
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

        public void SetItems(IList<MyListUnitModel> units)
        {
            // Commented: the below 3 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.Clear();
            //YourList.AddRange(items);
            //ResetItems(YourList.Count);

            Data.ResetItems(units);
        }
        #endregion

        /// <summary>
        /// ViewHolderの選択を解除する
        /// </summary>
        public void Deselect()
        {
            
            var index = Data.List.FindIndex(l => l.IsSelected);
            if (index != -1)
            {
                // 以前までセレクトされていたViewsHolderを選択解除する
                // 同じViewHolderを選択した場合を除く
                Data[index].IsSelected = false;
                var _oldViewsHolder = GetBaseItemViewsHolderIfVisible(index);
                if (_oldViewsHolder != null && _oldViewsHolder is MyListUnitViewsHolder)
                {
                    var oldViewsHolder = (MyListUnitViewsHolder)_oldViewsHolder;
                    oldViewsHolder.SelectViewsHolder(false, true);
                }
            }
        }

        /// <summary>
        /// ViewsHolderが選択された時の呼び出し
        /// </summary>
        /// <param name="viewsHolder"></param>
        private void ViewsHolderOnClick(MyListUnitViewsHolder viewsHolder)
        {
            var index = Data.List.FindIndex(l => l.IsSelected);
            if (index != -1 && index != viewsHolder.ItemIndex)
            {
                // 以前までセレクトされていたViewsHolderを選択解除する
                // 同じViewHolderを選択した場合を除く
                Data[index].IsSelected = false;
                var _oldViewsHolder = GetBaseItemViewsHolderIfVisible(index);
                if (_oldViewsHolder != null && _oldViewsHolder is MyListUnitViewsHolder)
                {
                    var oldViewsHolder = (MyListUnitViewsHolder)_oldViewsHolder;
                    oldViewsHolder.SelectViewsHolder(false, true);
                }
            }

            if (Data[viewsHolder.ItemIndex].IsSelected)
            {
                // 既に選択されているViewsHolderを選択したため選択解除
                Data[viewsHolder.ItemIndex].IsSelected = false;
                viewsHolder.SelectViewsHolder(false, true);
                SelectIndex = -1;
                viewsHolderOnClick?.Invoke(this, -1);
            }
            else
            {
                // 新しいViewHolderを選択した時
                Data[viewsHolder.ItemIndex].IsSelected = true;
                viewsHolder.SelectViewsHolder(true, true);
                SelectIndex = viewsHolder.ItemIndex;
                viewsHolderOnClick?.Invoke(this, viewsHolder.ItemIndex);
            }
        }
    }

    // Class containing the data associated with an item
    public class MyListUnitModel
    {
        public MyListUnitModel()
        {
        }

        public MyListUnitModel(UnitData data)
        {
            this.data = data;
            name = data.Name;
            level = data.Level.ToString();
            health = (data.HealthPoint + data.AdditionalHealthPoint).ToString();
            var primary = data.MainWeapon;
            primaryWeapon = primary != null ? primary.Name : "NONE";
            energy = data.Supply.ToString();
        }

        public UnitData data;
        public string name;
        public string level;
        public string health;
        public string primaryWeapon;
        public string energy;

        public bool IsSelected { internal set; get; } = false;
    }


    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    public class MyListUnitViewsHolder : BaseItemViewsHolder
    {
        public RectTransform panel;

        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI levelLabel;
        public TextMeshProUGUI healthLabel;
        public TextMeshProUGUI primaryWeaponLabel;
        public TextMeshProUGUI energyLabel;
        public Button addUnitButton;
        public Button selectUnitButton;
        public CanvasGroup selectPanel;

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

            root.GetComponentAtPath("Panel/UnitName", out nameLabel);
            root.GetComponentAtPath("Panel/Values/Level/LevelValue", out levelLabel);
            root.GetComponentAtPath("Panel/Values/HP/HPValue", out healthLabel);
            root.GetComponentAtPath("Panel/Values/Weapon/WeaponValue", out primaryWeaponLabel);
            root.GetComponentAtPath("Panel/Values/Energy/EnergyValue", out energyLabel);
            root.GetComponentAtPath("AddUnit", out addUnitButton);
            root.GetComponentAtPath("Selection", out selectPanel);
            selectPanel.alpha = 0;
            selectUnitButton = root.GetComponent<Button>();
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
