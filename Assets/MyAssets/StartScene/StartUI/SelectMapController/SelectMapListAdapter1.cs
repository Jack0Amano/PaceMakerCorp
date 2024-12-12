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
using Parameters.MapData;

// You should modify the namespace to your own or - if you're sure there won't ever be conflicts - remove it altogether
namespace SelectMapUI
{
	// There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
	// See explanations below
	public class SelectMapListAdapter1 : OSA<BaseParamsWithPrefab, MapListItemViewsHolder1>
	{
		// Helper that stores data and notifies the adapter when items count changes
		// Can be iterated and can also have its elements accessed by the [] operator
		public SimpleDataHelper<MapListItemModel1> Data { get; private set; }

		/// <summary>
		/// MapDataのリストの元データ
		/// </summary>
		private List<MapData> mapDataList = new List<MapData>();

		/// <summary>
		/// ScrollViewのItemViewHolderが選択された際に呼ばれる
		/// </summary>
		internal Action<MapData> onSelectMap;

		#region OSA implementation
		protected override void Start()
		{
			Data = new SimpleDataHelper<MapListItemModel1>(this);

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
		protected override MapListItemViewsHolder1 CreateViewsHolder(int itemIndex)
		{
			var instance = new MapListItemViewsHolder1();

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
		protected override void UpdateViewsHolder(MapListItemViewsHolder1 newOrRecycled)
		{
			// In this callback, "newOrRecycled.ItemIndex" is guaranteed to always reflect the
			// index of item that should be represented by this views holder. You'll use this index
			// to retrieve the model from your data set
			/*
			MyListItemModel model = Data[newOrRecycled.ItemIndex];

			newOrRecycled.backgroundImage.color = model.color;
			newOrRecycled.titleText.text = model.title + " #" + newOrRecycled.ItemIndex;
			*/

			// Set the data of the cell
			newOrRecycled.mapListItemCell.SetData(Data[newOrRecycled.ItemIndex].MapData, onSelectMap);
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
		public void AddItemsAt(int index, IList<MapListItemModel1> items)
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

		public void SetItems(IList<MapListItemModel1> items)
		{
			// Commented: the below 3 lines exemplify how you can use a plain list to manage the data, instead of a DataHelper, in case you need full control
			//YourList.Clear();
			//YourList.AddRange(items);
			//ResetItems(YourList.Count);

			Data.ResetItems(items);
		}
		#endregion


		// Here, we're requesting <count> items from the data source
		/// <summary>
		/// データを取得して更新する
		/// </summary>
		/// <param name="count"></param>
		internal void RetrieveDataAndUpdate()
		{
			if (Data.Count != 0)
                Data.RemoveItemsFromStart(Data.Count);
			// MapDataを取得
			mapDataList = MapDataSaveUtility.Load().MapDataList;
			StartCoroutine(FetchMoreItemsFromDataSourceAndUpdate(mapDataList.Count));
		}

		// Retrieving <count> models from the data source and calling OnDataRetrieved after.
		// In a real case scenario, you'd query your server, your database or whatever is your data source and call OnDataRetrieved after
		IEnumerator FetchMoreItemsFromDataSourceAndUpdate(int count)
		{
			// Simulating data retrieving delay
			yield return new WaitForSeconds(.5f);
			
			var newItems = new MapListItemModel1[count];

			for (int i = 0; i < count; ++i)
			{
				var model = MapListItemModel1.Create(mapDataList[i]);
				newItems[i] = model;
			}

			OnDataRetrieved(newItems);
		}

		void OnDataRetrieved(MapListItemModel1[] newItems)
		{
			Data.InsertItemsAtEnd(newItems);
		}

	}

	// Class containing the data associated with an item
	public class MapListItemModel1
	{
		/// <summary>
		/// MapData
		/// </summary>
		public MapData MapData;

		/// <summary>
		/// MapのID (AddressやMapDataのFolder名等と同じ)
		/// </summary>
		public string MapID;
		/// <summary>
		/// Mapの名前
		/// </summary>
		public string Name;
		/// <summary>
		///	Mapの説明
		/// </summary>
		public string Description;
		/// <summary>
		/// Mapの画像
		/// </summary>
		public Sprite Image;
		/// <summary>
		/// Mapが解放されているか
		/// </summary>
		public bool IsUnlocked;
		
		/// <summary>
		/// Create a new instance of MyListItemModel
		/// </summary>
		/// <param name="mapData"></param>
		/// <returns></returns>
		public static MapListItemModel1 Create(MapData mapData)
		{
			MapListItemModel1 mapListItemModel = new MapListItemModel1();
			mapListItemModel.SetData(mapData);
			return mapListItemModel;
        }

		/// <summary>
		/// Set the data of this instance
		/// </summary>
		/// <param name="mapData"></param>
		public void SetData(MapData mapData)
		{
			MapData = mapData;
            MapID = mapData.ID;
            Name = mapData.Name;
            Description = mapData.Description;
            Image = mapData.Image;
            IsUnlocked = mapData.IsUnlocked;
        }
	}


	// This class keeps references to an item's views.
	// Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
	public class MapListItemViewsHolder1 : BaseItemViewsHolder
	{
		internal MapListItemCell mapListItemCell;

		// Retrieving the views from the item's root GameObject
		public override void CollectViews()
		{
			base.CollectViews();
			mapListItemCell = root.GetComponent<MapListItemCell>();

			// GetComponentAtPath is a handy extension method from frame8.Logic.Misc.Other.Extensions
			// which infers the variable's component from its type, so you won't need to specify it yourself
			/*
			root.GetComponentAtPath("TitleText", out titleText);
			root.GetComponentAtPath("BackgroundImage", out backgroundImage);
			*/
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
}
