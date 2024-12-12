using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using static Utility;
using System.Runtime.Serialization;
using Tactics.Object;

/*******************************************************************
 * 保持しているEquipment
 */
/// <summary>
/// プレイヤーが取得しているアイテム
/// </summary>
[Serializable]
public class OwnItem
{
    /// <summary>
    /// EquipmentのID
    /// </summary>
    public string Id;

    /// <summary>
    /// 使用可能な数
    /// </summary>
    public int FreeCount;

    /// <summary>
    /// 所持しているトータル数
    /// </summary>
    public int TotalCount;

    /// <summary>
    /// Itemの元データ
    /// </summary>
    public ItemData ItemData
    {
        get
        {
            if (Id == null || Id.Length == 0)
                return null;

            itemData ??= GameManager.Instance.StaticData.AllItemsList.GetItemFromID(Id);

            if (itemData.ID != Id)
                itemData = GameManager.Instance.StaticData.AllItemsList.GetItemFromID(Id);

            return itemData;
        }
    }
    /// <summary>
    /// Equipmentの実Class
    /// </summary>
    [NonSerialized]
    private ItemData itemData;


    public OwnItem()
    {
        this.Id = "";
        FreeCount = 1;
        TotalCount = 1;
    }

    public OwnItem(string id, int count)
    {
        this.Id = id;
        FreeCount = count;
        TotalCount = count;
    }

    public OwnItem(string id)
    {
        this.Id = id;
    }

    public OwnItem(ItemData itemData)
    {
        this.Id = itemData.ID;
        this.itemData = itemData;
    }

    public OwnItem(OwnItem ownItem)
    {
        Id = ownItem.Id;
        FreeCount = ownItem.FreeCount;
        TotalCount = ownItem.TotalCount;
    }
}

/********************************************************************************
 * Equipmentの元データ
 */
/// <summary>
/// Equipmentの元データ
/// </summary>
[Serializable]
public class ItemData
{
    [Tooltip("アイテムのID検索用")]
    public string ID = "TestID";
    [Tooltip("アイテムの名前")]
    public string Name;
    [Tooltip("アイテムの種類")]
    public ItemType ItemType;
    [Tooltip("どのようなフォーカスモードで使用できるか")]
    public FocusModeType FocusModeType;
    [Tooltip("武器のターゲットとするタイプ")]
    public TargetType TargetType = TargetType.None;
    [Tooltip("どのようなダメージを与えるか")]
    public AttackType AttackType;
    [Tooltip("AimLineを表示するか")]
    public bool ShowAimLine = false;
    /// <summary>
    /// 攻撃力
    /// </summary>
    public int Attack = 0;
    /// <summary>
    /// 守備力
    /// </summary>
    public int Defence = 0;
    /// <summary>
    /// 有効射程 Grenadeの場合加害半径
    /// </summary>
    public float Range = 0;
    /// <summary>
    /// 装備時の追加物資 
    /// </summary>
    public int Supply = 0;
    /// <summary>
    /// 出撃時の消費コスト
    /// </summary>
    [Tooltip("出撃時の消費コスト")]
    public float SortieCost = 0;

    ///<summary>
    /// Itemの一回アクションで使用する回数
    /// </summary>
    [Tooltip("Itemの一回アクションで使用する回数")]
    public int UseCountPerAction = 1;

    /// <summary>
    /// Itemの使用限度回数
    /// </summary>
    [Tooltip("Itemの使用限度回数")]
    public int LimitActionCount = 1;

    /// <summary>
    /// 敵が侵入してきた時の反撃
    /// </summary>
    public bool Counterattack = false;
    /// <summary>
    /// Itemのオブジェクトデータ
    /// </summary>
    public GameObject Prefab;
    /// <summary>
    /// ショップに出現するレベル
    /// </summary>
    public ShopLevel ShopLevel;
    /// <summary>
    /// 購入時のコスト
    /// </summary>
    public int BuyCost = 1;
    /// <summary>
    /// 売った際のコスト
    /// </summary>
    public int ResellCost = 1;
    /// <summary>
    /// ショップレベルに応じた値引き値
    /// </summary>
    public List<int> Discount;
    public string Company;
    public string SubCompany;
    public string Description;
    public ItemWeight Weight;

    /// <summary>
    /// ManTargetの武器でObject型の敵を攻撃した際の威力減衰
    /// </summary>
    [NonSerialized] public static readonly float DecreaseDamageToObject = 0.2f;
    /// <summary>
    /// ObjectTargetの武器で人形の敵を攻撃した際の命中低下
    /// </summary>
    [NonSerialized] public static readonly float DecreaseHitToMan = 0.4f;
    /// <summary>
    /// ヘッドショットした際のボーナス値
    /// </summary>
    [NonSerialized] public static readonly float HeadShotBonus = 1.5f;
    /// <summary>
    /// 四肢に攻撃した際のボーナス値
    /// </summary>
    [NonSerialized] public static readonly float LimbsShotBonus = 0.7f;

    [NonSerialized] private string targetTypeName;
    public string TargetTypeName
    {
        get
        {
            var code = TargetType.GetHashCode().ToString();
            if (targetTypeName == null || targetTypeName == code)
                targetTypeName = GameManager.Instance.Translation.CommonUserInterfaceIni.ReadValue("TargetType", code, code);
            return targetTypeName;
        }
    }

    /// <summary>
    /// 値引きを計算
    /// </summary>
    public int DiscountCost
    {
        get
        {
            if (Discount.Count() == 0)
                return 0;
            var myLevel = GameManager.Instance.DataSavingController.SaveDataInfo.shopLevel.GetHigherLevel();
            var itemLevel = ShopLevel.GetHigherLevel();
            int index = (myLevel.level + 1) / (itemLevel.level + 1) - 1;
            if (Discount.Count() < index)
                return Discount[Discount.Count() - 1];

            return Discount[index];
        }
    }

    /// <summary>
    /// Tactics中で使用するItemClass
    /// </summary>
    public Tactics.Items.Item TacticsItem
    {
        get
        {
            if (tacticsItem == null)
                tacticsItem = Prefab.GetComponent<Tactics.Items.Item>();
            if (tacticsItem == null)
                Debug.LogError($"ItemData.TacticsItem: {Name}にTactics.Items.Itemがアタッチされていません");
            return tacticsItem;
        }
    }
    [NonSerialized] Tactics.Items.Item tacticsItem;

    public static ItemData Default()
    {
        return new ItemData()
        {
            Name = "Default",
            ItemType = ItemType.Rifle,
            Attack = 0,
            TargetType = TargetType.None,
            Range = 0,
            Supply = 0
        };
    }

    [NonSerialized]
    public static Dictionary<ItemWeight, float> WeightAndValues = new Dictionary<ItemWeight, float>
    {
        {ItemWeight.None, 0 },
        {ItemWeight.Assist, 1.5f },
        {ItemWeight.Light, -1 },
        {ItemWeight.Heavy, -2 }
    };

    public override string ToString()
    {
        return $"Item: ID.{ID}, Name.{Name}, ItemType.{ItemType}, Prefab.{Prefab}";
    }
}

#region ItemDataに必要な各種パラメーター
/// <summary>
/// 装備の収納箇所
/// </summary>
[Serializable]
public enum HolderType
{
    Primary,
    Secondary,
    Pouch,
    Backpack,
    All
}

/// <summary>
/// Tab分けする際の装備の種別
/// </summary>
[Serializable]
public enum ItemType: int
{
    None,
    /// <summary>
    /// ライフル
    /// </summary>
    Rifle,
    /// <summary>
    /// ハンドガン
    /// </summary>
    HandGun,
    /// <summary>
    /// 手榴弾
    /// </summary>
    Grenade,
    /// <summary>
    /// 地雷
    /// </summary>
    Landmine,
    /// <summary>
    /// 防具
    /// </summary>
    Armor,
    /// <summary>
    /// 食料
    /// </summary>
    Ration,
    /// <summary>
    /// 医療品
    /// </summary>
    Medicine,
    /// <summary>
    /// 迫撃砲
    /// </summary>
    Mortar,
}

/// <summary>
/// どのようなタイプの使用を行えるか UnitsControllerの"Unitの行動に関する処理"に対応
/// </summary>
[Serializable]
public enum FocusModeType
{
    None,
    Gun,
    Throw,
    Mortar
}

/// <summary>
/// Equipmentの重量
/// </summary>
[Serializable]
public enum ItemWeight: int
{
    None,
    Assist,
    Light,
    Heavy
}

/// <summary>
/// 武装の有効な相手
/// </summary>
[Serializable]
public enum TargetType
{
    None,
    Human,
    Object,
    Debug
}

/// <summary>
/// どのようなダメージを与えるか
/// </summary>
[Serializable] public class AttackType
{
    /// <summary>
    /// 発射した攻撃元を即座に判別可能か
    /// </summary>
    public bool IsKnownSource;

    /// <summary>
    /// 攻撃した方向が即座に判別可能か
    /// </summary>
    public bool IsKnownDirection;

    /// <summary>
    /// 攻撃した場所で音など的に位置を知らせる物があるか
    /// </summary>
    public bool IsKnownLocation;

    /// <summary>
    /// どのようなダメージを受けるか アニメーションの選別に使用
    /// </summary>
    public DamageType DamageType;

    /// <summary>
    /// 破棄できるObjectの種類
    /// </summary>
    public DestroyableType DestroyableType;

    /// <summary>
    /// 与えられたGimmickObjectがこの攻撃によって破壊可能か
    /// </summary>
    public bool IsDestroyable(GimmickObject gimmickObject)
    {
        if (gimmickObject == null)
            return false;

        if (DestroyableType == DestroyableType.None)
            return false;

        if (DestroyableType == DestroyableType.Soft && gimmickObject.IsSoftyDestructible)
            return true;

        // 硬目標の場合は軟目標も破壊可能
        if (DestroyableType == DestroyableType.Hard)
        {
            if (gimmickObject.IsHardyDestructible || gimmickObject.IsSoftyDestructible)
                return true;
        }

        return false;
    }
}

/// <summary>
/// 破壊できるObjectの種類
/// </summary>
public enum DestroyableType
{
    None,
    [Tooltip("軟目標")]
    Soft,
    [Tooltip("硬目標")]
    Hard,
}

/// <summary>
/// どのようなダメージを受けるか アニメーションの選別に使用
/// </summary>
public enum DamageType
{
    None,
    [Tooltip("爆発")]
    Explosion,
    [Tooltip("銃弾")]
    Shoot,
}


/// <summary>
/// 装備する箇所であるHolderTypeとEquipmentTypeの整合表
/// </summary>
static class HolderAndItem
{
    /// <summary>
    /// Primaryのホルダーの中に入れることのできるItemType
    /// </summary>
    private static readonly List<ItemType> itemTypesInPrimary = new List<ItemType>
    {
        ItemType.Rifle,
        // EquipmentType.ATWarfare
    };

    /// <summary>
    /// Secondaryの中に入れることのできるItem
    /// </summary>
    private static readonly List<ItemType> itemTypesInSecondary = new List<ItemType>
    {
        ItemType.HandGun
    };


    /// <summary>
    /// ポーチのホルダーに入れることのできるItemのType
    /// </summary>
    private static readonly List<ItemType> itemTypesInPouch = new List<ItemType>
    {
        ItemType.Landmine,
        ItemType.Grenade
    };

    /// <summary>
    /// Backpackの中に入れることのできるItemのType
    /// </summary>
    private static readonly List<ItemType> itemTypesInBackpack = new List<ItemType>
    {
        ItemType.Landmine,
        ItemType.Ration,
        ItemType.Armor,
    };

    /// <summary>
    /// すべてのType
    /// </summary>
    private static readonly List<ItemType> allType = Enum.GetValues(typeof(ItemType)).Cast<ItemType>().ToList();

    /// <summary>
    /// ホルダーとその中に入れれるItemTypeのDictionary
    /// </summary>
    public static readonly Dictionary<HolderType, List<ItemType>> Match = new Dictionary<HolderType, List<ItemType>>
    {
        {HolderType.All, allType },
        {HolderType.Primary, itemTypesInPrimary },
        {HolderType.Secondary,  itemTypesInSecondary},
        {HolderType.Pouch, itemTypesInPouch },
        {HolderType.Backpack, itemTypesInBackpack },
    };

    /// <summary>
    /// ItemTypeからどのHolderTypeに入れることができるか
    /// </summary>
    /// <param name="itemType"></param>
    /// <returns></returns>
    public static List<HolderType> MatchHolderTypes(ItemType itemType)
    {
        var holderTypes = new List<HolderType>();
        foreach (var pair in Match)
        {
            if (pair.Value.Contains(itemType))
                holderTypes.Add(pair.Key);
        }
        return holderTypes;
    }
}

/// <summary>
/// ショップとアイテムの出現レベル
/// </summary>
[Serializable]
public class ShopLevel
{
    public int WeaponLevel = 0;
    public int ArmarLevel = 0;
    public int VehicleLevel = 0;


    public Dictionary<Type, int> Levels
    {
        get
        {
            return new Dictionary<Type, int>(){
                    {Type.Weapon,  WeaponLevel },
                    {Type.Armar,   ArmarLevel },
                    {Type.Vehicle, VehicleLevel }
                };
        }
    }

    public enum Type
    {
        Weapon,
        Armar,
        Vehicle,
    }

    /// <summary>
    /// アイテムの出現レベルが与えられたレベル以下の場合True
    /// </summary>
    /// <param name="level"></param>
    public bool IsLowerThan(ShopLevel myLevel)
    {
        foreach (Type type in Enum.GetValues(typeof(Type)))
        {
            var itemLevel = 0;
            Levels.TryGetValue(type, out itemLevel);
            var myItemLevel = 0;
            myLevel.Levels.TryGetValue(type, out myItemLevel);
            if (myItemLevel < itemLevel)
                return false;
        }
        return true;
    }

    public bool Equals(ShopLevel myLevel)
    {
        if (myLevel == null)
            return false;
        foreach (Type type in Enum.GetValues(typeof(Type)))
        {
            var itemLevel = 0;
            Levels.TryGetValue(type, out itemLevel);
            var myItemLevel = 0;
            myLevel.Levels.TryGetValue(type, out myItemLevel);
            if (myItemLevel != itemLevel)
                return false;
        }
        return true;
    }

    /// <summary>
    /// ShopLevelの中で最も値が高い要素のtypeとレベルを取得
    /// </summary>
    /// <returns></returns>
    public (Type type, int level) GetHigherLevel()
    {
        var pair = Levels.FindMax(l => l.Value);
        return (pair.Key, pair.Value);
    }
}



#endregion