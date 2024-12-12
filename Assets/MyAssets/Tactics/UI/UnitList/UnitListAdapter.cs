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
using System.Linq;
using UnityEngine.AddressableAssets;
using frame8.Logic.Misc.Other.Extensions;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine.ResourceManagement.AsyncOperations;

// You should modify the namespace to your own or - if you're sure there won't ever be conflicts - remove it altogether
namespace Tactics.UI.Lists
{
    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    public class UnitListAdapter : OSA<BaseParamsWithPrefab, MyListItemViewsHolder>
    {

        public List<MyListItemModel> rawData;
        public LazyDataHelper<MyListItemModel> LazyData { get; private set; }

        /// <summary>
        /// 現在Model内のAsyncで読込中のModel
        /// </summary>
        private List<MyListItemModel> loadingModels = new List<MyListItemModel>();


        #region OSA implementation
        protected override void Start()
        {

            LazyData = new LazyDataHelper<MyListItemModel>(this, CreateNewModel);

            base.Start();
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

            MyListItemModel model = LazyData.GetOrCreate(newOrRecycled.ItemIndex);
            newOrRecycled.Update(model);
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
        public void AddItemsAt(int index, IList<MyListItemModel> items)
        {
            // Commented: the below 2 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.InsertRange(index, items);
            //InsertItems(index, items.Length);

            rawData.InsertRange(index, items);
            LazyData.InsertItems(index, items.Count);
        }

        public void RemoveItemsFrom(int index, int count)
        {
            // Commented: the below 2 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.RemoveRange(index, count);
            //RemoveItems(index, count);

            rawData.RemoveRange(index, count);
            LazyData.RemoveItems(index, count);
        }

        public void SetItems(IList<MyListItemModel> items)
        {
            // Commented: the below 3 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
            //YourList.Clear();
            //YourList.AddRange(items);
            //ResetItems(YourList.Count);

            rawData = items.ToList();
            if (LazyData.Count != 0)
                LazyData.RemoveItemsFromStart(LazyData.Count);
            LazyData.InsertItemsAtStart(items.Count);

        }
        #endregion

        /// <summary>
        /// DataからModelを新たに作成する (LazyDataの非同期呼び出しで自動的に呼び出される)
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        MyListItemModel CreateNewModel(int index)
        {
            var model = rawData[index];
            //model.StartLoading();
            loadingModels.Add(model);
            return model;
        }
    }

    // Class containing the data associated with an item
    public class MyListItemModel
    {
        public MyListItemModel(Character.UnitController unitController)
        {
            this.unitController = unitController;
            this.parameter = unitController.CurrentParameter.Data;
        }

        public Character.UnitController unitController;
        public UnitData parameter;
    }


    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    public class MyListItemViewsHolder : BaseItemViewsHolder
    {
        //public SelectUnit.UnitTableCell unitTableCell;


        // Retrieving the views from the item's root GameObject
        public override void CollectViews()
        {
            base.CollectViews();

            // GetComponentAtPath is a handy extension method from frame8.Logic.Misc.Other.Extensions
            // which infers the variable's component from its type, so you won't need to specify it yourself
            //unitTableCell = root.GetComponent<SelectUnit.UnitTableCell>();
        }

        internal void Update(MyListItemModel model)
        {
            //unitTableCell.faceImage.sprite = model.parameter.FaceImage;
            //unitTableCell.SetParameter(model.parameter);
        }
    }
}
