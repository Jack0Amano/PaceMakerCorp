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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using frame8.Logic.Misc.Other.Extensions;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using TMPro;
using static Utility;
using Unity.VisualScripting;
using Com.TheFallenGames.OSA.Util.Animations;

// You should modify the namespace to your own or - if you're sure there won't ever be conflicts - remove it altogether
namespace GameSetting.SaveLoadData
{
    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    public class DataBasicListAdapter : OSA<DataParams, DataListItemViewsHolder>
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public LazyDataHelper<DataListItemModel> Data { get; private set; }

        public List<(SaveData data, string path)> SavedDatas;

        /// <summary>
        /// ScrollViewのItemViewHolderが選択されたときに呼び出す
        /// </summary>
        public Action<SaveData, string> ItemViewHolderSelected;

        /// <summary>
        /// ScrollViewのItemViewHolderの削除ボタンが押された際の呼び出し
        /// </summary>
        public Action<SaveData, string> ItemViewHolderTryToRemove;

        InsertDeleteAnimationState insertDeleteAnimation;

        /// <summary>
        /// 一つのItemのサイズ
        /// </summary>
        const float NON_EXPANDED_SIZE = .1f;

        /// <summary>
        /// 
        /// </summary>
        bool alternatingEndEdgeStationary;

        #region OSA implementation
        protected override void Start()
        {
            Data = new LazyDataHelper<DataListItemModel>(this, CreateModel);

            // Calling this initializes internal data and prepares the adapter to handle item count changes
            base.Start();

            var cancel = _Params.Animation.Cancel;
            // Needed so that CancelUserAnimations() won't be called when sizes change (which happens during our animation itself)
            cancel.UserAnimations.OnCountChanges = false;
            // Needed so that CancelUserAnimations() won't be called on count changes - we're handling these manually by overriding ChangeItemsCount 
            cancel.UserAnimations.OnSizeChanges = false;

            // Retrieve the models from your data source and set the items count
        }

        // This is called initially, as many times as needed to fill the viewport, 
        // and anytime the viewport's size grows, thus allowing more items to be displayed
        // Here you create the "ViewsHolder" instance whose views will be re-used
        // *For the method's full description check the base implementation
        protected override DataListItemViewsHolder CreateViewsHolder(int itemIndex)
        {
            var instance = new DataListItemViewsHolder();

            // Using this shortcut spares you from:
            // - instantiating the prefab yourself
            // - enabling the instance game object
            // - setting its index 
            // - calling its CollectViews()
            instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
            instance.cell.selectButton.onClick.AddListener(() => 
            {
                var selected = SavedDatas[instance.ItemIndex];
                ItemViewHolderSelected?.Invoke(selected.data, selected.path) ;
            });
            instance.cell.deleteButton.onClick.AddListener(() =>
            {
                var selected = SavedDatas[instance.ItemIndex];
                ItemViewHolderTryToRemove?.Invoke(selected.data, selected.path);
            });

            return instance;
        }

        /// <summary>
        /// DataからModelを作成する (Lazyで読み込みを遅延させる場合この中で読み込み等を行う)
        /// 現在はSaveDatasに一括で読み込んでいるため、このメソッドは単純にModelを作成するだけ
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private DataListItemModel CreateModel(int index)
        {
            return DataListItemModel.Create(SavedDatas[index].data);
        }

        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateViewsHolder(DataListItemViewsHolder newOrRecycled)
        {
            // In this callback, "newOrRecycled.ItemIndex" is guaranteed to always reflect the
            // index of item that should be represented by this views holder. You'll use this index
            // to retrieve the model from your data set

            var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
            newOrRecycled.UpdateFromModel(model);
        }

        protected override void Update()
        {
            base.Update();

            if (!IsInitialized)
                return;

            if (insertDeleteAnimation != null)
                AdvanceExpandCollapseAnimation(GetShouldKeepEndEdgeStationary());
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
        public void AddNewSave(SaveData saveDataInfo, string path)
        {
            SavedDatas.Insert(0, (saveDataInfo, path));
            AnimatedInsert(0, CreateModel(0));
        }

        /// <summary>
        /// アニメーション付きの挿入
        /// </summary>
        /// <param name="index"></param>
        /// <param name="model"></param>
        private void AnimatedInsert(int index, DataListItemModel model)
        {
            // Force finish previous animation
            if (insertDeleteAnimation != null)
            {
                ForceFinishCurrentAnimation();

                if (index > Data.Count)
                    // The previous animation was a removal and this index is not valid anymore
                    return;
            }

            insertDeleteAnimation = new InsertDeleteAnimationState(_Params.UseUnscaledTime, index, 0f, 1f);
            Data.InsertOneManuallyCreated(index, model, false);
            
            var viewsHolder = GetItemViewsHolderIfVisible(index);
            viewsHolder?.cell.SetInfo(model, true);
        }

        /// <summary>
        /// ListからSaveDataを削除する
        /// </summary>
        /// <param name="saveDataInfo"></param>
        public void RemoveSave(SaveData saveDataInfo, bool animation)
        {
            var index = SavedDatas.FindIndex((s) => s.data.DataInfo.ID == saveDataInfo.DataInfo.ID);
            print("Remove save data at " + index);
            if (index == -1)
                return;
            SavedDatas.RemoveAt(index);
            AnimatedRemove(index);
        }

        /// <summary>
        /// アニメーション付きの削除
        /// </summary>
        /// <param name="index"></param>
        private void AnimatedRemove(int index)
        {
            // Force finish previous animation
            if (insertDeleteAnimation != null)
            {
                ForceFinishCurrentAnimation();

                if (index >= Data.Count)
                    // The previous animation was a removal and this index is not valid anymore
                    return;
            }

            //var vh = GetItemViewsHolderIfVisible(index);
            //vh?.cell.DeleteInfoWithAnimation();
            var model = Data.GetOrCreate(index);
            Print(model.expandedAmount);
            insertDeleteAnimation = new InsertDeleteAnimationState(_Params.UseUnscaledTime, index, model.expandedAmount, 0f);
        }

        #endregion

        // Here, we're requesting <count> items from the data source
        /// <summary>
        /// データを更新する
        /// </summary>
        public void RetrieveDataAndUpdate()
        {
            if (Data.Count != 0)
                Data.RemoveItemsFromStart(Data.Count);
            UpdateSaveDataInfos();
            StartCoroutine(FetchMoreItemsFromDataSourceAndUpdate(SavedDatas.Count));
        }

        // Retrieving <count> models from the data source and calling OnDataRetrieved after.
        // In a real case scenario, you'd query your server, your database or whatever is your data source and call OnDataRetrieved after
        /// <summary>
        /// データを取得して更新する
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerator FetchMoreItemsFromDataSourceAndUpdate(int count)
        {
            // Simulating data retrieving delay
            yield return null;
            
            var newItems = new DataListItemModel[count];

            // Retrieve your data here
            for (int i = 0; i < count; ++i)
            {
                var model = DataListItemModel.Create(SavedDatas[i].data);
                newItems[i] = model;
            }

            Data.InsertItemsAtEnd(newItems.Length);
        }

        /// <summary>
        /// セーブディレクトリ内の全てのセーブデータを読み込む
        /// </summary>
        /// <returns></returns>
        public void UpdateSaveDataInfos()
        {
            //var files = Directory.GetFiles(GameManager.SaveDataRootPath);
            //var directories = Directory.GetDirectories(GameManager.SaveDataRootPath);
            var output = GameManager.Instance.DataSavingController.GetAllSavedData();
            output.Sort((a, b) => DateTime.Compare(b.data.DataInfo.SaveTime, a.data.DataInfo.SaveTime));
            SavedDatas = output;

        }

        /// <summary>
        /// 指定のIndexのDataを更新する
        /// </summary>
        /// <param name="index"></param>
        public void UpdateOn(int index)
        {
            var saveDataInfo = SavedDatas[index];
            Data.GetOrCreate(index).Set(saveDataInfo.data);
            ForceUpdateViewsHolderIfVisible(index);
        }

        #region Animation   
        /// <summary>
        /// 現在のアニメーションを強制終了する
        /// </summary>
        void ForceFinishCurrentAnimation()
        {
            insertDeleteAnimation.ForceFinish();
            AdvanceExpandCollapseAnimation(false);
        }

        /// <summary>
        /// Modelのサイズを取得する
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        float GetModelCurrentSize(DataListItemModel model)
        {
            float nonExpandedSize = NON_EXPANDED_SIZE;
            float expandedSize = _Params.DefaultItemSize;

            return Mathf.Lerp(nonExpandedSize, expandedSize, model.expandedAmount);
        }

        /// <summary>
        /// 現在のViewsHolderが表示されている場合に呼び出される
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <param name="model"></param>
        /// <param name="endEdgeStationary"></param>
        void ResizeViewsHolderIfVisible(int itemIndex, DataListItemModel model, bool endEdgeStationary)
        {
            float newSize = GetModelCurrentSize(model);

            // Set to true if positions aren't corrected; this happens if you don't position the pivot exactly at the stationary edge
            bool correctPositions = false;

            RequestChangeItemSizeAndUpdateLayout(itemIndex, newSize, endEdgeStationary, true, correctPositions);
        }

        /// <summary>
        /// ViewsHolderのサイズが変更されたときに呼び出される (ViewsHolderが開くようなアニメーション等)
        /// </summary>
        /// <param name="itemEndEdgeStationary"></param>
        void AdvanceExpandCollapseAnimation(bool itemEndEdgeStationary)
        {
            int itemIndex = insertDeleteAnimation.itemIndex;
            var model = Data.GetOrCreate(itemIndex);

            model.expandedAmount = insertDeleteAnimation.CurrentExpandedAmount;

            ResizeViewsHolderIfVisible(itemIndex, model, itemEndEdgeStationary);

            if (insertDeleteAnimation != null && insertDeleteAnimation.IsDone)
                OnCurrentInsertDeleteAnimationFinished();
        }

        /// <summary>
        /// 現在のInsertDeleteAnimationが終了したときに呼び出される
        /// </summary>
        void OnCurrentInsertDeleteAnimationFinished()
        {
            int itemIndex = insertDeleteAnimation.itemIndex;
            if (!insertDeleteAnimation.IsInsert)
            {
                // The animation was a remove animation => The item needs to be removed at the end of it
                Data.RemoveItems(itemIndex, 1, false);
            }

            insertDeleteAnimation = null;
        }

        /// <summary>
        /// If the item needs to expand both upwards and downwards, do it alternatively between the frames.
        /// Otherwise, fix its top edge.
        /// </summary>
        bool GetShouldKeepEndEdgeStationary()
        {
            if (_Params.ItemAnimationPivotMiddle)
            {
                alternatingEndEdgeStationary = !alternatingEndEdgeStationary;
                return alternatingEndEdgeStationary;
            }
            return false;
        }
        #endregion
    }

    // Class containing the data associated with an item
    public class DataListItemModel
    {
        public string date;
        public string time;
        public string unitsValue;
        public string squadsValue;
        public string operatingArea;
        public string localTime;

        internal float expandedAmount;

        public bool isMakeNewDataCell = true;

        public SaveData RawData { private set; get; }

        public static DataListItemModel Create(SaveData data)
        {
            var output = new DataListItemModel();


            output.expandedAmount = 1;

            output.RawData = data;
            output.date = data.DataInfo.SaveTime.ToString("yyyy/MM/dd");
            output.time = data.DataInfo.SaveTime.ToString("HH:mm:ss");
            output.operatingArea = data.DataInfo.MainMapSceneName;
            output.unitsValue = data.DataInfo.UnitsCount.ToString();
            output.squadsValue = data.DataInfo.SquadsCount.ToString();
            output.localTime = data.DataInfo.GameTime.ToString("yyyy/MM/dd HH:mm:ss");

            output.isMakeNewDataCell = false;

            return output;
        }

        public void Set(SaveData data)
        {
            RawData = data;
            date = data.DataInfo.SaveTime.ToString("yyyy/MM/dd");
            time = data.DataInfo.SaveTime.ToString("HH:mm:ss");
            operatingArea = data.DataInfo.MainMapSceneName;
            unitsValue = data.DataInfo.UnitsCount.ToString();
            squadsValue = data.DataInfo.SquadsCount.ToString();
            localTime = data.DataInfo.GameTime.ToString("yyyy/MM/dd HH:mm:ss");
        }
    }

    [Serializable] // serializable, so it can be shown in inspector
    public class DataParams : BaseParamsWithPrefab
    {
        public bool ItemAnimationPivotMiddle;
    }

    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    public class DataListItemViewsHolder : BaseItemViewsHolder
    {
        internal SaveLoadDataCell cell;
        /// <summary>
        /// The root RectTransform of the item prefab. Used for resizing the item when it's expanded
        /// </summary>
        internal RectTransform rootRectTransform;

        // Retrieving the views from the item's root GameObject
        public override void CollectViews()
        {
            base.CollectViews();
            rootRectTransform = root;

            // GetComponentAtPath is a handy extension method from frame8.Logic.Misc.Other.Extensions
            // which infers the variable's component from its type, so you won't need to specify it yourself
            cell = root.GetComponent<SaveLoadDataCell>();
        }

        /// <summary>
        /// ViesHolderが表示されて更新されているときの呼び出し
        /// </summary>
        /// <param name="model"></param>
        internal void UpdateFromModel(DataListItemModel model)
        {
            cell.SetInfo(model, false);
        }

        protected override void OnRootCreated(int itemIndex, bool activateRootGameObject = true, bool callCollectViews = true)
        {
            //Print("OnRootCreated", itemIndex, activateRootGameObject, callCollectViews);
            base.OnRootCreated(itemIndex, activateRootGameObject, callCollectViews);
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
    }

    /// <summary>
    /// Viewsholderの挿入と削除をアニメーションの実装
    /// </summary>
    public class InsertDeleteAnimationState : ExpandCollapseAnimationState
    {
        public bool IsInsert { get { return targetExpandedAmount == 1f; } }

        public const float ANIMATION_DURATION = .5f;

        public InsertDeleteAnimationState(bool useUnscaledTime, int itemIndex, float initialExpandedAmount, float targetExpandedAmount)
            : base(useUnscaledTime)
        {
            this.itemIndex = itemIndex;
            this.initialExpandedAmount = initialExpandedAmount;
            this.targetExpandedAmount = targetExpandedAmount;
            duration = ANIMATION_DURATION;
        }
    }
}
