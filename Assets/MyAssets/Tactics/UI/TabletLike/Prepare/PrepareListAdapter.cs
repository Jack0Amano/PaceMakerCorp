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
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using frame8.Logic.Misc.Other.Extensions;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine.AddressableAssets;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.Animations;
using TMPro;
using static Utility;
using DG.Tweening;
using UnityEngine.ResourceManagement.AsyncOperations;

// You should modify the namespace to your own or - if you're sure there won't ever be conflicts - remove it altogether
namespace Tactics.Prepare.Lists
{
    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    [System.Runtime.InteropServices.Guid("817DE949-07A9-4D5D-A5A5-C02E29060A17")]
    public class PrepareListAdapter : OSA<MyParams, MyListItemViewsHolder>
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        // public SimpleDataHelper<MyListItemModel> Data { get; private set; }
        List<MyListItemModel> RawData;
        LazyDataHelper<MyListItemModel> LazyData;
        ExpandCollapseAnimationState _ExpandCollapseAnimation;
        const float EXPAND_COLLAPSE_ANIM_DURATION = .2f;

        public Action<(int modelIndex, ItemHolder holder)> ChangeItemAction;
        public Action<MyListItemModel> selectUnitAction;
        /// <summary>
        /// 現在Model内のAsyncで読込中のModel
        /// </summary>
        private readonly List<MyListItemModel> LoadingModels = new List<MyListItemModel>();

        /// <summary>
        /// LazyDataの準備が整っているか
        /// </summary>
        public bool IsDataPrepared
        {
            get => LazyData != null;
        }

        /// <summary>
        /// Itemの選択をPrepare画面でできるようにする debug用
        /// </summary>
        public bool CanSelectItems
        {
            get => _CanSelectItems;
            set
            {
                _CanSelectItems = value;
            }
        }
        private bool _CanSelectItems;

        #region OSA implementation
        protected override void Start()
        {
            // Data = new SimpleDataHelper<MyListItemModel>(this);
            LazyData = new LazyDataHelper<MyListItemModel>(this, CreateNewModel);

            // Needed so that CancelUserAnimations() won't be called when sizes change (which happens during our animation itself)
            var cancel = _Params.Animation.Cancel;
            cancel.UserAnimations.OnSizeChanges = false;

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
        protected override MyListItemViewsHolder CreateViewsHolder(int itemIndex)
        {
            var instance = new MyListItemViewsHolder();

            // Using this shortcut spares you from:
            // - instantiating the prefab yourself
            // - enabling the instance game object
            // - setting its index 
            // - calling its CollectViews()
            instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
            var model = LazyData.GetOrCreate(itemIndex);
            instance.button.onClick.AddListener(() => ListOnClick(model));
            instance.infoButton.onClick.AddListener(() => OnExpandCollapseButtonClicked(instance));
            if (CanSelectItems)
            {
                for (var i = 0; i < instance.equipLabels.Count; i++)
                {
                    var index = i;
                    instance.equipLabels[i].button.onClick.AddListener(() => SelectItemInUnit(model, itemIndex, index));
                }
            }

            return instance;
        }

        /// <summary>
        /// Unitを選択する
        /// </summary>
        /// <param name="model"></param>
        private void ListOnClick(MyListItemModel model)
        {
            selectUnitAction?.Invoke(model);
        }

        /// <summary>
        /// アイテムを変えるパネルを開くボタン
        /// </summary>
        /// <param name="model"></param>
        /// <param name="itemIndex"></param>
        /// <param name="holderIndex"></param>
        private void SelectItemInUnit(MyListItemModel model, int itemIndex, int holderIndex)
        {
            ChangeItemAction?.Invoke((itemIndex, model.itemHolders[holderIndex]));
        }

        /// <summary>
        /// Unitの選択が無効などのアニメーションを再生する
        /// </summary>
        /// <param name="model"></param>
        public void NotAcceptAnimation(MyListItemModel model)
        {
            var index = RawData.FindIndex(d => d.Equals(model));
            if (index != -1)
            {
                var _vh = GetBaseItemViewsHolderIfVisible(index);
                if (_vh != null)
                {
                    var vh = (MyListItemViewsHolder)_vh;
                    vh.ShakeFaceImage();
                }
            }
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
            
            MyListItemModel model = LazyData.GetOrCreate(newOrRecycled.ItemIndex);
            newOrRecycled.Update(model);
            
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();

            if (!IsInitialized)
                return;

            if (_ExpandCollapseAnimation != null)
                AdvanceExpandCollapseAnimation();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            //if (loadingModels.Count != 0)
            //{
            //    var loadedModels = loadingModels.FindAll(m => m.faceImageHandle.IsDone);
            //    loadedModels.ForEach(m =>
            //    {
            //        var index = rawData.FindIndex(m2 => m2 == m);
            //        if (index != -1)
            //        {
            //            ForceUpdateViewsHolderIfVisible(index);
            //            loadingModels.Remove(m);
            //        }
            //    });
            //}
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
            RawData.InsertRange(index, items);
            LazyData.InsertItems(index, items.Count);
            
        }

        public void RemoveItemsFrom(int index, int count)
        {
            // Commented: the below 2 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.RemoveRange(index, count);
            //RemoveItems(index, count);

            RawData.RemoveRange(index, count);
            LazyData.RemoveItems(index, count);
        }

        public void SetItems(IList<MyListItemModel> items)
        {
            // Commented: the below 3 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.Clear();
            //YourList.AddRange(items);
            //ResetItems(YourList.Count);
            RawData = items.ToList();
            if (LazyData.Count != 0)
                LazyData.RemoveItemsFromStart(LazyData.Count);
            LazyData.InsertItemsAtStart(items.Count);
        }

        /// <summary>
        /// Modelからlistのindexを取得
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public int GetIndexFrom(MyListItemModel model)
        {
            return RawData.FindIndex(m => m.Equals(model));
        }

        /// <summary>
        /// UnitDataからIndexを取得
        /// </summary>
        /// <param name="unitData"></param>
        /// <returns></returns>
        public int GetIndexFrom(UnitData unitData)
        {
            return RawData.FindIndex(m => m.unitData == unitData);
        }
        #endregion

        #region ExpandAnimation
        /// <summary>
        /// CellサイズのExpandの開始時に呼び出す
        /// </summary>
        /// <param name="vh"></param>
        void OnExpandCollapseButtonClicked(MyListItemViewsHolder vh)
        {
            // Force finish previous animation
            if (_ExpandCollapseAnimation != null)
            {
                int oldItemIndex = _ExpandCollapseAnimation.itemIndex;
                var oldModel = LazyData.GetOrCreate(oldItemIndex);
                _ExpandCollapseAnimation.ForceFinish();
                oldModel.ExpandedAmount = _ExpandCollapseAnimation.CurrentExpandedAmount;
                ResizeViewsHolderIfVisible(oldItemIndex, oldModel);
                _ExpandCollapseAnimation = null;
            }


            var model = LazyData.GetOrCreate(vh.ItemIndex);
            var anim = new ExpandCollapseAnimationState(_Params.UseUnscaledTime);
            anim.initialExpandedAmount = model.ExpandedAmount;
            anim.duration = EXPAND_COLLAPSE_ANIM_DURATION;
            if (model.ExpandedAmount == 1f) // fully expanded
                anim.targetExpandedAmount = 0f;
            else
                anim.targetExpandedAmount = 1f;

            anim.itemIndex = vh.ItemIndex;

            _ExpandCollapseAnimation = anim;
        }

        private void AdvanceExpandCollapseAnimation()
        {
            var itemIndex = _ExpandCollapseAnimation.itemIndex;
            var model = LazyData.GetOrCreate(itemIndex);
            model.ExpandedAmount = _ExpandCollapseAnimation.CurrentExpandedAmount;
            ResizeViewsHolderIfVisible(itemIndex, model);

            if (_ExpandCollapseAnimation != null && _ExpandCollapseAnimation.IsDone)
                _ExpandCollapseAnimation = null;
        }

        void ResizeViewsHolderIfVisible(int itemIndex, MyListItemModel model)
        {
            float newSize = GetModelCurrentSize(model);

            // Set to true if positions aren't corrected; this happens if you don't position the pivot exactly at the stationary edge
            bool correctPositions = false;

            RequestChangeItemSizeAndUpdateLayout(itemIndex, newSize, false, true, correctPositions);

            var vh = GetItemViewsHolderIfVisible(itemIndex);
            if (vh != null)
            {
                // Fixing Unity bug: https://issuetracker.unity3d.com/issues/rectmask2d-doesnt-update-when-the-parent-size-is-changed
                // Changing the transform's scale and restoring it back. This trigggers the update of the RectMask2D. 
                // Tried RectMask2D.PerformClipping(), tried setting m_ForceClipping and other params through reflection with no success.
                // This workaround remains the only one that works.
                // This is not needed in case galleryEffect is bigger than 0, since that already changes the items' scale periodically, but we included it to cover all cases
                var localScale = vh.rectMask2DRectTransform.localScale;
                vh.rectMask2DRectTransform.localScale = localScale * .99f;
                vh.rectMask2DRectTransform.localScale = localScale;
            }
        }

        float GetModelCurrentSize(MyListItemModel model)
        {
            float expandedSize = GetModelExpandedSize();

            return Mathf.Lerp(_Params.DefaultItemSize, expandedSize, model.ExpandedAmount);
        }

        float GetModelExpandedSize()
        {
            return _Params.expandFactor * _Params.ItemPrefabSize;
        }

        /// <inheritdoc/>
        protected override void CancelUserAnimations()
        {
            // Correctly handling OSA's request to stop user's (our) animations
            _ExpandCollapseAnimation = null;

            base.CancelUserAnimations();
        }
        #endregion

        /// <summary>
        /// DataからModelを新たに作成する (LazyDataの非同期呼び出しで自動的に呼び出される)
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        MyListItemModel CreateNewModel(int index)
        {
            var model = RawData[index];
            LoadingModels.Add(model);
            return model;
        }

        /// <summary>
        /// 指定したModelのViewHolderのCheckImageを更新
        /// </summary>
        /// <param name="check"></param>
        /// <param name="model"></param>
        public void CheckViewHolders(string tileID, int index)
        {
            if (RawData.IndexAt_Bug(index, out var model))
            {
                var vh = GetItemViewsHolderIfVisible(index);
                model.tileID = tileID;
                if (vh != null)
                {
                    vh.checkImage.DOColor(tileID.Length != 0 ? Color.white : Color.clear, 0.5f);
                }
            }
        }
    }

    // Class containing the data associated with an item
    public class MyListItemModel
    {
        public MyListItemModel() { }

        public MyListItemModel(UnitData parameter)
        {
            unitData = parameter;
            name = parameter.Name;
            level = parameter.Level.ToString();

            var antiTankWeapon = parameter.AntiTankWeapon;
            if (antiTankWeapon != null)
                tankAttack = antiTankWeapon.Attack.ToString();
            var antiManWeapon = parameter.AntiManWeapon;
            if (antiManWeapon != null)
                manAttack = antiManWeapon.Attack.ToString();
            
            health = parameter.HealthPoint.ToString();
            cost = parameter.SortieCostOfDay.ToString();
            itemHolders = parameter.MyItems;
            faceImage = parameter.FaceImage;
        }

        private Sprite _faceImage;
        public string name;
        public string level;
        public string tankAttack;
        public string manAttack;
        public string health;
        public string cost;
        public UnitData unitData;
        internal Sprite faceImage;
        public List<ItemHolder> itemHolders;
        public bool isChecked = false;
        /// <summary>
        /// Unitの位置のTileID
        /// </summary>
        public string tileID = "";

        // View size related
        public float ExpandedAmount { get; set; }
    }


    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    public class MyListItemViewsHolder : BaseItemViewsHolder
    {

        public Image faceImage;
        public TextMeshProUGUI nameLabel;
        public TextMeshProUGUI levelLabel;
        public TextMeshProUGUI tankAttackLabel;
        public TextMeshProUGUI manAttackLabel;
        public TextMeshProUGUI healthLabel;
        public TextMeshProUGUI costLabel;
        public RectTransform tankAttackObject;
        public RectTransform manAttackObject;
        public Button button;
        public Button infoButton;
        public RectTransform rectMask2DRectTransform;
        public Image checkImage;

        public List<(TextMeshProUGUI label, Button button)> equipLabels;
        public Button setItemSetButton;


        // Retrieving the views from the item's root GameObject
        public override void CollectViews()
        {
            base.CollectViews();
            // GetComponentAtPath is a handy extension method from frame8.Logic.Misc.Other.Extensions
            // which infers the variable's component from its type, so you won't need to specify it yourself
            rectMask2DRectTransform = root;

            root.GetComponentAtPath("CheckImage", out checkImage);
            var front = root.GetComponentAtPath<RectTransform>("Front");
            front.GetComponentAtPath("FaceImage", out faceImage);
            front.GetComponentAtPath("NameLabel", out nameLabel);
            front.GetComponentAtPath("InformationButton", out infoButton);
            front.GetComponentAtPath("Parameters/Level/Value", out levelLabel);
            front.GetComponentAtPath("Parameters/TankAttack/Value", out tankAttackLabel);
            front.GetComponentAtPath("Parameters/ManAttack/Value", out manAttackLabel);
            front.GetComponentAtPath("Parameters/TankAttack", out tankAttackObject);
            front.GetComponentAtPath("Parameters/ManAttack", out manAttackObject);
            front.GetComponentAtPath("Parameters/Health/Value", out healthLabel);
            front.GetComponentAtPath("Parameters/Cost/Value", out costLabel);

            button = root.GetComponent<Button>();

            var expandParams = root.GetComponentAtPath<RectTransform>("Expand/Parameters");
            var buttons = expandParams.GetComponentsInChildren<Button>();
            equipLabels = new List<(TextMeshProUGUI, Button)>();
            foreach(var b in buttons)
            {
                var label = b.GetComponentInChildren<TextMeshProUGUI>();
                equipLabels.Add((label, b));
            }
        }

        /// <summary>
        /// 表示内容をアップデートする
        /// </summary>
        /// <param name="model"></param>
        internal void Update(MyListItemModel model)
        {
            healthLabel.SetText(model.health);
            costLabel.SetText(model.cost);
            levelLabel.SetText(model.level);
            nameLabel.SetText(model.name);
            faceImage.sprite = model.faceImage;
            faceImage.color = Color.white;
            checkImage.color = model.tileID.Length != 0 ? Color.white : Color.clear;

            if (model.tankAttack != null)
            {
                tankAttackObject.gameObject.SetActive(true);
                tankAttackLabel.text = model.tankAttack;
            }
            else
                tankAttackObject.gameObject.SetActive(false);

            if (model.manAttack != null)
            {
                manAttackObject.gameObject.SetActive(true);
                manAttackLabel.text = model.manAttack;
            }
            else
                manAttackObject.gameObject.SetActive(false);
            
            for(int i=0; i<equipLabels.Count; i++)
            {
                if(model.itemHolders.IndexAt_Bug(i, out var item))
                {
                    equipLabels[i].button.interactable = true;
                    if (item.Data == null)
                    {
                        equipLabels[i].label.SetText("EMPTY");
                    }else
                    {
                        equipLabels[i].label.SetText(item.Data.Name);
                    }
                }
                else
                {
                    equipLabels[i].label.SetText("");
                    equipLabels[i].button.interactable = false;
                }
            }
        }

        /// <summary>
        /// FaceImageをX軸に揺らす
        /// </summary>
        internal void ShakeFaceImage()
        {

        }
    }

    [Serializable]
    public class MyParams: BaseParamsWithPrefab
    {
        public float expandFactor = 2f;

        [NonSerialized]
        public bool freezeItemEndEdgeWhenResizing;
    }
}
