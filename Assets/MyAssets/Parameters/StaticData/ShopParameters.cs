//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using UnityEngine;
//using System.Linq;
//using static Utility;


///// <summary>
///// すべてのShopのアイテム販売データ  静的データ
///// </summary>
//[Serializable] public class ShopParameters
//{
//    public List<ShopItem> shopItems;

//    /// <summary>
//    /// 店に並べるアイテム
//    /// </summary>
//    public List<ShopItem> itemsInShop
//    {
//        get
//        {
//            var currentLevel = GameController.Instance.data.saveDataInfo.shopLevel;
//            var refresh = !currentLevel.Equals(_shopLevel);
//            if (refresh)
//                return _itemsInShop = GetItemsUnderLevel(currentLevel);
//            return _itemsInShop;
//        }
//    }
//    private ShopLevel _shopLevel;
//    private List<ShopItem> _itemsInShop;

//    /// <summary>
//    /// Assets/Data/Shopディレクトリに存在するShopのJsonFileを読み込みShopParametersをロードする
//    /// </summary>
//    /// <returns></returns>
//    public static ShopParameters LoadFromJson()
//    {
//        var path = "Assets/Data/Shop";
//        var files = Directory.GetFiles(path, "*.json");
//        var output = new ShopParameters();

//        if (!File.Exists(path))
//        {
//            output.shopItems = new List<ShopItem>() { ShopItem.Default() };
//            var jsonStr = JsonUtility.ToJson(output);
//            File.WriteAllText(Path.Combine(path, "Default.json"), jsonStr);
//            return output;
//        }
//        else
//        {
//            foreach (var f in files)
//            {
//                var jsonStr = File.ReadAllText(f);
//                var shopFile = JsonUtility.FromJson<ShopParameters>(jsonStr);
//                output.shopItems.AddRange(shopFile.shopItems);
//            }
//            return output;
//        }
//    }

//    /// <summary>
//    /// 自軍のアイテム出現レベル以下のアイテムを取得する
//    /// </summary>
//    /// <param name="myLevel"></param>
//    /// <returns></returns>
//    private List<ShopItem> GetItemsUnderLevel(ShopItem.ShopLevel myLevel)
//    {
//        return shopItems.FindAll(item =>
//        {
//            return item.shopLevel.IsLowerThan(myLevel);
//        });
//    }
//}