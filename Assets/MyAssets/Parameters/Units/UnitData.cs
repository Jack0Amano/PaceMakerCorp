using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static Utility;
using AIGraph.Editor;

/// <summary>
/// UnitDataのパラメーターはレベルアップ等によって変動する値 変動しないものはBaseUnitDatに
/// </summary>
[Serializable]
public class UnitData
{
    #region Parameters
    [Tooltip("Unitの管理ID")]
    public string ID;
    
    [Tooltip("Unitの経験値　レベルが上がると0から (敵Unitの場合は撃破時の獲得経験値)")]
    public int Exp = 0;
    
    [Tooltip("Unitのレベル")]
    public int Level = 1;
    
    [Tooltip("HPポイント")]
    public int HealthPoint = 20;

    [Tooltip("UnitがPlayer操作の場合に保持しているItem")]
    public List<ItemHolder> MyItems = new List<ItemHolder>();

    [Tooltip("隊長特有のパラメーター")]
    public CommanderParameter CommanderParameter;

    /// <summary>
    /// すべてのGameで共通のデータ
    /// </summary>
    GeneralParameter GeneralParameter
    {
        get => _GeneralParameter == null ? _GeneralParameter = GameManager.Instance.GeneralParameter : _GeneralParameter;
    }
    [NonSerialized] GeneralParameter _GeneralParameter;


    /// <summary>
    /// Unitの基本
    /// </summary>
    public int BaseSupply { get => GeneralParameter.BaseSupplyAtUnit; }

    /// <summary>
    /// 静的なUnitの基本パラメーター
    /// </summary>
    public BaseUnitData Data
    {
        get
        {
            if (_data == null)
            {
                if (!GameManager.Instance.StaticData.AllUnitsData.GetUnitFromID(ID, out var baseData))
                {
                    PrintWarning($"Fail to load unit prefab of {ID}. Set data and the prefab on asset.");
                    baseData = GameManager.Instance.StaticData.AllUnitsData.units.First();
                }
                _data = baseData;
            }
            return _data;
        }
    }
    [NonSerialized] private BaseUnitData _data;
    #endregion

    #region GetterSetter
    public bool IsCommander { get => Data.IsCommander; }

    /// <summary>
    /// Unitの名前
    /// </summary>
    public string Name { get => Data.Name; }
    /// <summary>
    ///  Unitの種別
    /// </summary>
    public UnitType.Type UnitType { get => Data.UnitType; }
    /// <summary>
    /// Unitのプレファブ
    /// </summary>
    public GameObject Prefab { get => Data.Prefab; }
    /// <summary>
    /// Unitのミニフィグプレファブ
    /// </summary>
    public GameObject MiniPrefab { get => Data.MiniPrefab; }
    /// <summary>
    /// ユニットの行動順を司る基本速度
    /// </summary>
    public float Speed { get => Data.Speed; }
    /// <summary>
    /// 勝敗を決定するフラグ管理を担うユニット e.g. 敵のFlagを倒せばWin 
    /// </summary>
    public bool IsFlag { get => Data.IsFlag; }
    /// <summary>
    /// レベルから次のレベルになるために必要なExpを計算
    /// </summary>
    /// <param name="level">現在のレベル</param>
    /// <returns></returns>
    public int RequiredExp(int level)
    {
        return 200 * level;
    }

    /// <summary>
    /// Unitの顔写真
    /// </summary>
    public Sprite FaceImage { get => Data.FaceImage; }

    /// <summary>
    /// UnitTypeの文字を取得
    /// </summary>
    public string UnitTypeStr
    {
        get
        {
            if (_unitTypeStr == null)
                _unitTypeStr = GameManager.Instance.Translation.CommonUserInterfaceIni.ReadValue("UnitType", UnitType.ToString(), "NoneIni");
            return _unitTypeStr;
        }
    }
    [NonSerialized] private string _unitTypeStr;

    /// <summary>
    /// ユニットの装備による速度変化の追加値
    /// </summary>
    public float AdditionalSpeed
    {
        get
        {
            return MyItems.Sum(e => e.Data == null ? 0 : ItemData.WeightAndValues[e.Data.Weight]);
        }
    }

    /// <summary>
    /// 装備によるHPの追加値
    /// </summary>
    public int AdditionalHealthPoint
    {
        get
        {
            if (MyItems == null) return 0;
            return MyItems.Sum(e => e.Data == null ? 0 : e.Data.Defence);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public int Supply
    {
        get
        {
            return BaseSupply + AdditionalSupply;
        }
    }

    /// <summary>
    /// 装備による追加の補給物資
    /// </summary>
    public int AdditionalSupply
    {
        get
        {
            if (MyItems == null) return 0;
            return MyItems.Sum(e => e.Data == null ? 0 : e.Data.Supply);
        }
    }

    /// <summary>
    /// ユニットを出撃させる際に消費する物資のコスト
    /// </summary>
    public int SortieCost
    {
        get
        {
            var cost = global::UnitType.GetSortieCost(UnitType);
            MyItems.ForEach(e =>
            {
                if (e.Data != null)
                    cost += e.Data.SortieCost;
            });
            return (int)Math.Round(cost);
        }
    }

    /// <summary>
    /// 出撃コストで何日Dayが減少するか
    /// </summary>
    public float SortieCostOfDay
    {
        get
        {
            return GeneralParameter.DaysOfRemainingSupply(SortieCost);
        }
    }

    /// <summary>
    /// Unitの重さ (Tileに一度に入れる重量制限がある)
    /// </summary>
    public int Weight
    {
        get => UnitType == global::UnitType.Type.Tank ? 5 : 2;
    }

    /// <summary>
    /// Unitの主武器となる武装
    /// </summary>
    public ItemData MainWeapon
    {
        get
        {
            if (MyItems.TryFindFirst(i => i.Type == HolderType.Primary &&
                                          i.Data != null, out var item))
                return item.Data;
            if (MyItems.TryFindFirst(i => i.Type == HolderType.Secondary &&
                                          i.Data != null, out item))
                return item.Data;
            return null;
        }
    }

    /// <summary>
    /// 主武器として装備しているもの
    /// </summary>
    public ItemData PrimaryWeapon
    {
        get
        {
            if (MyItems.TryFindFirst(i => i.Type == HolderType.Primary &&
                                          i.Data != null, out var item))
                return item.Data;
            if (Data.BaseItems.TryFindFirst(i => i.Type == HolderType.Primary &&
                                                  i.Data != null, out item))
                return item.Data;

            return null;
        }
    }

    /// <summary>
    /// Secndaryとして装備しているもの
    /// </summary>
    public ItemData SecondaryWeapon
    {
        get
        {
            if (MyItems.TryFindFirst(i => i.Type == HolderType.Secondary &&
                                          i.Data != null, out var item))
                return item.Data;
            if (Data.BaseItems.TryFindFirst(i => i.Type == HolderType.Secondary &&
                                                  i.Data != null, out item))
                return item.Data;

            return null;
        }
    }

    /// <summary>
    /// 所持している場合は最も攻撃力の高い対戦車兵器を返す
    /// </summary>
    public ItemData AntiTankWeapon
    {
        get
        {
            var items = MyItems.FindAll(i =>
            {
                if (i.Data == null)
                    return false;
                return i.Data.TargetType == TargetType.Object &&
                       i.Data.ItemType != ItemType.Grenade &&
                       i.Data.ItemType != ItemType.Landmine;
            });
            if (items.Count == 0)
                return null;
            return items.FindMax(i => i.Data.Attack).Data;
        }
    }

    /// <summary>
    /// 所持している場合は最も攻撃力の高い対人兵器を返す
    /// </summary>
    public ItemData AntiManWeapon
    {
        get
        {
            var items = MyItems.FindAll(i => 
            {
                if (i.Data == null)
                    return false;
                return i.Data.TargetType == TargetType.Human &&
                       i.Data.ItemType != ItemType.Landmine &&
                       i.Data.ItemType != ItemType.Grenade;
            });
            if (items.Count == 0)
                return null;
            return items.FindMax(i => i.Data.Attack).Data;
        }
    }

    /// <summary>
    /// Unitがcounterattackを行えるかどうか
    /// </summary>
    public bool CanCounterAttack
    {
        get
        {
            if (MyItems.Count != 0)
                return MyItems.Find(i => i.Data.Counterattack) != null;
            else
                return Data.BaseItems.Find(i => i.Data.Counterattack) != null;
        }
    }

    /// <summary>
    /// 何らかのWeaponを持っているか
    /// </summary>
    public bool DoesUnitHaveWeapon
    {
        get
        {
            var weapons = MyItems.FindAll(h =>
            {
                return (h.Type == HolderType.Primary && h.Data != null) ||
                       (h.Type == HolderType.Secondary && h.Data != null);
            });
            return weapons.Count != 0;
        }
    }
    #endregion

    #region Temp
    /// <summary>
    /// Unitが敵非発見状態で周回するWayのIndex
    /// </summary>
    [NonSerialized] public int RoutineWayIndex = -1;
    #endregion

    /// <summary>
    /// ItemHolderをLevelの通りに適用する
    /// </summary>
    public void SetItemHolderLevel(ItemHolderLevel itemHolderLevel)
    {

        // 既にnewTypeとの一致が確認されたholder
        var stillActiveHolder = new List<ItemHolder>(MyItems);
        var remainNewHolderTypes = new List<HolderType>(itemHolderLevel.HolderTypes);
        // 1回目のループNewholderTypeとOldHolerTypeが同じ場合
        foreach(var newType in itemHolderLevel.HolderTypes)
        {
            if (!remainNewHolderTypes.Contains(newType))
                continue;
            var activeHolder = stillActiveHolder.Find(h => h.Type == newType);
            if (activeHolder != null && activeHolder.Data != null)
            {
                remainNewHolderTypes.Remove(newType);
                stillActiveHolder.Remove(activeHolder);
            }
        }
        //  2回目のループ newHolderTypeとOldが異なるが NewTypeがDataのItemtypeを許容する場合
        foreach(var newType in itemHolderLevel.HolderTypes)
        {
            if (!remainNewHolderTypes.Contains(newType))
                continue;
            var activeHolder = stillActiveHolder.Find(h => {
                return h.Data != null && HolderAndItem.Match[newType].Contains(h.Data.ItemType);
            });
            if (activeHolder != null)
            {
                remainNewHolderTypes.Remove(newType);
                stillActiveHolder.Remove(activeHolder);
            }

        }
        // この時点でstillActiveHolderに残されているのは ItemがセットされていないHOlderか、更新によってsetできなくなったitem入holder
        // RemainNewHolderTypesに入っているのは残りの新しいHolder
        var removedItemCount = stillActiveHolder.Count(h => h.Data != null);
        stillActiveHolder.ForEach(h => MyItems.Remove(h));

        foreach(var newType in remainNewHolderTypes)
        {
            MyItems.Add(new ItemHolder(newType));
        }
        // ここまでで順番はともかくMyItemsに新しいHolderに適用されたlistがある
        // 順番をnewに整える typeが同じであればDataが入っているのを上に来る用に配置する
        var newMyItems = new List<ItemHolder>();
        foreach(var newType in itemHolderLevel.HolderTypes)
        {
            var holder = MyItems.Find(i => i.Type == newType && i.Data != null);
            if (holder != null)
            {
                MyItems.Remove(holder);
                newMyItems.Add(holder);
                continue;
            }
            holder = MyItems.Find(i => i.Type == newType);
            if (holder != null)
            {
                MyItems.Remove(holder);
                newMyItems.Add(holder);
                continue;
            }
            PrintWarning("Throughout in SetItemHolderLevel");
        }
        MyItems = newMyItems;
    }

    public UnitData()
    {
    }

    public UnitData(BaseUnitData baseUnitData)
    {
        ID = baseUnitData.ID;
        _data = baseUnitData;
    }

    /// <summary>
    /// 装備がない場合はDefaultの装備を追加する
    /// </summary>  
    public void SetDefaultItemIfNeeded(ItemData defaultWeapon, MyArmyData myArmyData)
    {
        // Unitの装備がない場合はDefaultの装備を追加する
        var ownItemCount = MyItems.FindAll(h => h.IsEquipped).Count;
        if (ownItemCount == 0 && defaultWeapon != null)
        {
            var holderToDefaultWeapon = MyItems.Find(h => HolderAndItem.MatchHolderTypes(defaultWeapon.ItemType).Contains(h.Type));
            PrintWarning($"ItemController: ({this}) has no item. Add default item ({defaultWeapon.ID}) to ({holderToDefaultWeapon})");
            myArmyData.ItemController.SetItem(this, holderToDefaultWeapon, defaultWeapon.ID);
        }
    }

    public static UnitData Default()
    {
        var p = new UnitData();
        p.ID = "DefaultID";
        p.MyItems.Add(new ItemHolder(HolderType.Primary, "er0003"));
        p.HealthPoint = 10;
        //p.equipments.Add(new EquipmentHolder(HolderType.Pouch, "test"));
        //p.equipments.Add(new EquipmentHolder(HolderType.Backpack, "test"));
        return p;
    }

    public override string ToString()
    {
        if (Data != null)
            return $"UnitData: name_{Name}, id_{ID}";
        else
            return $"UnitData: id_{ID}";
    }
}

/// <summary>
/// Unitの静的な元データ　敵としてスポーンする際はこれが参照される
/// </summary>
[Serializable]
public class BaseUnitData
{
    [Tooltip("Unitの管理ID UnitDataが読み込みのときに参照する")]
    public string ID;
    /// <summary>
    /// Unitの名前
    /// </summary>
    public string Name = "Default";
    /// <summary>
    /// 隊長ユニットかどうか
    /// </summary>
    public bool IsCommander = false;
    /// <summary>
    ///  Unitの種別
    /// </summary>
    public UnitType.Type UnitType = global::UnitType.Type.Infantry;
    /// <summary>
    /// Unitのプレファブ
    /// </summary>
    public GameObject Prefab;
    /// <summary>
    /// Unitのミニフィグプレファブ
    /// </summary>
    public GameObject MiniPrefab;
    /// <summary>
    /// HPポイント
    /// </summary>
    public int HealthPoint = 20;
    /// <summary>
    /// ユニットの行動順を司る基本速度
    /// </summary>
    public float Speed;
    /// <summary>
    /// 勝敗を決定するフラグ管理を担うユニット e.g. 敵のFlagを倒せばWin 
    /// </summary>
    public bool IsFlag = false;
    /// <summary>
    /// 体力が少なくなっているかの割合
    /// </summary>
    public float LowHPValue = 0.3f;
    /// <summary>
    /// 顔写真のイメージ
    /// </summary>
    public Sprite FaceImage;

    [Header("AI")]
    [Tooltip("AI等に使用するレベル")]
    public int Level = 0;

    /// <summary>
    /// 最初に回すAI
    /// </summary>
    public AIGraphDataContainer MainAIGraphDataContainer;
    /// <summary>
    /// Action後のAI
    /// </summary>
    public AIGraphDataContainer AfterActionAIGraphDataContainer;
    /// <summary>
    /// 移動中に敵を発見した場合のAI
    /// </summary>
    public AIGraphDataContainer WhileMovingAIGraphDataContainer;

    /// <summary>
    /// UnitがEnemyとして登場する際、もしくは何も設定されていない場合
    /// </summary>
    public List<ItemHolder> BaseItems = new List<ItemHolder>();
}

/// <summary>
/// UnitのItemを持つ場合のホルダー
/// </summary>
[Serializable]
public class ItemHolder
{

    /// <summary>
    /// Holderの持てる武器のタイプ
    /// </summary>
    public HolderType Type;
    /// <summary>
    /// Holderに装備しているItemのID
    /// </summary>
    public string Id;
    /// <summary>
    /// idに基づいたItemの実データ
    /// </summary>
    public ItemData Data
    {
        get
        {
            if (Id == null || Id.Length == 0)
            {
                return null;
            }
                
            data ??= GameManager.Instance.StaticData.AllItemsList.GetItemFromID(Id);
            if (data == null)
            {
                  PrintWarning($"Fail to load item ID of {Id}. Check it.");
                return null;
            }
            if (data.ID != Id)
            {
                PrintWarning($"ID is not match, ID is changed from {data.ID} to {Id}");
                data = GameManager.Instance.StaticData.AllItemsList.GetItemFromID(Id);
            }
                
            return data;
        }
    }
    [NonSerialized] private ItemData data;

    /// <summary>
    /// ItemがHolderに装備されているか
    /// </summary>
    public bool IsEquipped
    {
        get
        {
            return Data != null;
        }
    }

    /// <summary>
    /// Itemの残り残弾数
    /// </summary>
    [NonSerialized] public int RemainingActionCount = 0;

    /// <summary>
    /// カウンター攻撃可能なItemが装備されているか
    /// </summary>
    public bool CanCounterAttack
    {
        get
        {
            if (Data == null)
                return false;
            return Data.Counterattack && RemainingActionCount != 0;
        }
    }

    /// <summary>
    /// ItemHolderの初期化
    /// </summary>
    public void Initialize()
    {

        RemainingActionCount = Data == null ? 0 : Data.LimitActionCount;
    }

    public ItemHolder(HolderType type, string id = null)
    {
        this.Type = type;
        this.Id = id;

        Initialize();
    }

    public ItemHolder(ItemHolder itemHolder)
    {
        Type = itemHolder.Type;
        Id = itemHolder.Id;

        Initialize();
    }

    public override string ToString()
    {
        return $"ItemData: Type.{Type}, ID.{Id}";
    }
}

/// <summary>
/// どのレベルでどのホルダーが開放されるか
/// </summary>
[Serializable]
public class ItemHolderLevel
{
    /// <summary>
    /// 開放されるレベルの下限値
    /// </summary>
    public int FromValue;
    /// <summary>
    /// 開放されるレベルの上限値
    /// </summary>
    public int ToValue;
    /// <summary>
    /// どのHolderが開放されるか
    /// </summary>
    public List<HolderType> HolderTypes;

    /// <summary>
    /// Unitの現在登録されているHolderとこのLevelの内容がMatchしているか
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool Match(UnitData data)
    {
        if (!(FromValue <= data.Level && data.Level <= ToValue))
            return false;
        if (HolderTypes.Count != data.MyItems.Count)
            return false;
        for (var i=0; i<HolderTypes.Count; i++)
        {
            if (HolderTypes[i] != data.MyItems[i].Type)
                return false;
        }
        return true;
    }
}

/// <summary>
/// どの兵種の何レベルでホルダーが開放されるか
/// </summary>
[Serializable]
public class UnitTypeAndHolderLevel
{
    /// <summary>
    /// 対象の兵種
    /// </summary>
    public UnitType.Type UnitType;
    /// <summary>
    /// どのレベルでどのホルダーが開放されるか
    /// </summary>
    public List<ItemHolderLevel> ItemHolderLevels;
}

/// <summary>
/// Unitが隊長クラスの場合使用
/// </summary>
[Serializable]
public class CommanderParameter
{
    /// <summary>
    /// 指揮できるMemberの数
    /// </summary>
    public int MemberCount = 3;
    /// <summary>
    /// Squadの予め設定されている名前
    /// </summary>
    public string DefaultSquadName;

    public CommanderParameter()
    {
    }

    public CommanderParameter(CommanderParameter c)
    {
        this.MemberCount = c.MemberCount;
        this.DefaultSquadName = c.DefaultSquadName;
    }
}


/// <summary>
/// Unitの兵種別
/// </summary>
[Serializable] public class UnitType
{
    [Serializable] public enum Type
    {
        // 歩兵
        Infantry,
        // 砲兵
        Mortar,
        // 工兵
        Engineer,
        // 対戦車兵
        AntiTank,
        // 衛生兵
        Medical,
        // 情報科
        Intelligence,
        // 戦車
        Tank,
    }

    /// <summary>
    /// 出撃コスtと兵種のペア
    /// </summary>
    private static readonly Dictionary<Type, float> costPair = new Dictionary<Type, float>
    {
        {Type.Infantry, 5 },
        {Type.Mortar, 5 },
        {Type.Engineer, 5 },
        {Type.AntiTank, 8 },
        {Type.Medical, 5 },
        {Type.Intelligence, 5 },
        {Type.Tank, 12 }
    };

    /// <summary>
    /// UnitTypeから出撃コストを取得する
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static float GetSortieCost(Type type)
    {
        return costPair[type];
    }
}