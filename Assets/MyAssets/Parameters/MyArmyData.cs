using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static Utility;

/**************************************************************************
 * MainSquadData
 */
/// <summary>
/// 現在使用可能なUnitと部隊のリスト
/// </summary>
[Serializable]
public class MyArmyData
{

    /// <summary>
    /// 所属している隊長クラスの中でFreeな者  操作はarmyControllerから行う
    /// </summary>
    public List<UnitData> FreeCommanders = new List<UnitData>();

    /// <summary>
    /// 所属している隊員の中でFreeな者  操作はarmyControllerから行う
    /// </summary>
    public List<UnitData> FreeUnits = new List<UnitData>();

    /// <summary>
    /// 所属している全ての部隊  操作はarmyControllerから行う
    /// </summary>
    public List<Squad> Squads = new List<Squad>();

    /// <summary>
    /// 所持しているアイテム  受け渡し等はequipControllerから行う
    /// </summary>
    public List<OwnItem> OwnItems = new List<OwnItem>();

    /// <summary>
    /// 部隊員の操作を行う全ての関数群
    /// </summary>
    public ArmyController ArmyController
    {
        get => armyController ??= new ArmyController(this);
    }
    [NonSerialized] private ArmyController armyController;

    /// <summary>
    /// 装備の操作を行う全ての関数群
    /// </summary>
    public ItemController ItemController
    {
        get => itemController ??= new ItemController(this);
    }
    [NonSerialized] private ItemController itemController;

    /// <summary>
    /// 現在所持している全てのUnitData
    /// </summary>
    public List<UnitData> Units
    {
        get
        {
            var units = new List<UnitData>();
            units.AddRange(FreeCommanders);
            units.AddRange(FreeUnits);
            foreach (var squad in Squads)
            {
                units.Add(squad.commander);
                units.AddRange(squad.member);
            }
            return units;
        }
    }

    public MyArmyData()
    {
    }

    /// <summary>
    /// セーブデータの整合性を確認する
    /// </summary>  
    public void CheckData(SaveDataInfo saveDataInfo)
    {
        if( ItemController.FindItemFromID(saveDataInfo.DefaultWeaponID, out ItemData defaultItemData))
        {
            ItemController.AddDefaultItemIfNeeded(defaultItemData);
            ItemController.SetDefaultItemToAllUnitIfNeeded(defaultItemData);
        }
    }
}

// *****************************************************************************
/// <summary>
/// 部隊の操作関数群 部隊のデータ操作は全てこの関数から行う
/// </summary>
public class ArmyController
{
    private MyArmyData data;

    internal ArmyController(MyArmyData data)
    {
        this.data = data;
    }

    /// <summary>
    /// Squadを作成する
    /// </summary>
    /// <param name="commander"></param>
    public Squad MakeSquad(UnitData commander)
    {
        var squad = new Squad(commander);
        squad.supplyLevel = squad.MaxSupply;
        data.FreeCommanders.Remove(commander);

        data.Squads.Add(squad);

        return squad;
    }

    /// <summary>
    /// Squadを削除する
    /// </summary>
    /// <param name="squad"></param>
    public void DeleteSquad(Squad squad)
    {
        if (!data.Squads.Remove(squad))
            PrintError($"UnitParameter.DeleteSquad: {squad} is not exist in List<Squad>");

        data.FreeUnits.AddRange(squad.member);
        data.FreeCommanders.Add(squad.commander);
    }

    /// <summary>
    /// SquadにUnitを追加する
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="squad"></param>
    public void AddUnit(UnitData unit, Squad squad)
    {
        if (!data.FreeUnits.Remove(unit))
            PrintError($"UnitParameter.AddUnit: {unit} is not exist in freeUnits");
        squad.member.Add(unit);
        squad.supplyLevel = squad.MaxSupply;
    }

    /// <summary>
    /// UnitをSquadから削除する
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="squad"></param>
    public void RemoveUnit(UnitData unit, Squad squad)
    {
        if (!squad.member.Remove(unit))
            PrintError($"UnitParameter.RemoveUnit: {unit} is not exist in member of {squad}");
        data.FreeUnits.Add(unit);
        squad.supplyLevel = squad.MaxSupply;
    }

    /// <summary>
    /// FreeUnitとSquad中のUnitを変更する
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="squad"></param>
    public void ChangeUnit(UnitData from, UnitData to, Squad squad)
    {
        if (!squad.member.Remove(from))
            PrintError($"UnitParameter.RemoveUnit: ({from}) is not exist in member of ({squad})");
        data.FreeUnits.Add(from);
        if (!data.FreeUnits.Remove(to))
            PrintError($"UnitParameter.AddUnit: ({to}) is not exist in freeUnits");
        squad.member.Add(to);
        squad.supplyLevel = squad.MaxSupply;
    }
}

// *****************************************************************************
/// <summary>
/// 装備の操作関数　装備品のデータ操作はすべてここの関数から行う
/// </summary>
public class ItemController
{

    readonly private MyArmyData myArmyData;

    public readonly OwnItem DefaultWeapon;

    internal ItemController(MyArmyData data)
    {
        this.myArmyData = data;
    }

    /// <summary>
    /// Defaultの装備を所持していない場合は追加する
    /// </summary>
    public void AddDefaultItemIfNeeded(ItemData defaultItemData)
    {
        if (!FindOwnItemWithID(defaultItemData.ID, out OwnItem own))
        {
            AddItem(defaultItemData);
        }
    }

    /// <summary>
    /// 装備がない場合はDefaultの装備を追加する
    /// </summary>  
    public void SetDefaultItemToAllUnitIfNeeded(ItemData defaultItemData)
    {
        // Unitの装備がない場合はDefaultの装備を追加する
        myArmyData.Units.ForEach(u =>
        {
            u.SetDefaultItemIfNeeded(defaultItemData, myArmyData);
        });
    }

    /// <summary>
    /// すべてのEquipmentとOwnEquipmentのセット
    /// </summary>
    public List<ItemInList> ItemSet
    {
        get
        {
            if (items == null)
            {
                var equipController = GameManager.Instance.DataSavingController.MyArmyData.ItemController;
                Print(GameManager.Instance.StaticData.AllItemsList);
                items = GameManager.Instance.StaticData.AllItemsList.Items.ConvertAll(i =>
                {
                    var itemData = new ItemInList();
                    itemData.data = i;
                    if (equipController.FindOwnItemWithID(i.ID, out OwnItem own))
                        itemData.own = own;
                    return itemData;
                });
            }
            return items;
        }
    }
    private List<ItemInList> items;

    /// <summary>
    /// 所持しているEquipmentの検索
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool FindOwnItemWithID(string id, out OwnItem own)
    {
        own = myArmyData.OwnItems.Find(e => e.Id == id);
        return own != null;
    }

    /// <summary>
    ///     IDからEquipmentDataを検索
    /// </summary>
    /// <param name="id"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool FindItemFromID(string id, out ItemData item)
    {
        if (GameManager.Instance.StaticData == null)
        {
            PrintError("FindItemFromID: StaticData is null. It's not loaded yet.");
            item = null;
            return false;
        }
            
        item = GameManager.Instance.StaticData.AllItemsList.GetItemFromID(id);
        return item != null;
    }

    /// <summary>
    /// OwnEquipmentが新たに追加または削除されたときにitemsの紐付けを更新する
    /// </summary>
    public void UpdateItemsIfNeeded(string equipmentID)
    {
        var item = GameManager.Instance.DataSavingController.MyArmyData.ItemController.ItemSet.Find(i => i.data.ID.Equals(equipmentID));
        if (item == null)
        {
            PrintError($"UpdateItemsIfNeeded: No exist equepment {equipmentID} is updated.");
            return;
        }
            

        var equipController = GameManager.Instance.DataSavingController.MyArmyData.ItemController;
        if (equipController.FindOwnItemWithID(item.data.ID, out OwnItem own))
        {
            // OwnEquipmentとEquipmentDataの紐付けを新たに行う; 新たなOwnEquipmentが追加された
            item.own = own;

        }
        else
        {
            // OwnEquipmentとEquipmentDataの紐付けを解除する; OwnEquipmentの所持数が0になった
            item.own = null;
        }
    }

    /// <summary>
    /// 手持ちにEquipmentを新たに追加する
    /// </summary>
    /// <param name="itemID"></param>
    public void AddItem(string itemID)
    {
        if (FindOwnItemWithID(itemID, out OwnItem own))
        {
            //own.freeCount += count;
            //own.totalCount += count;
        }
        else
        {
            myArmyData.OwnItems.Add(new OwnItem(itemID));

            // GameControllerのUI共通のEquipmentDataとOwnEquipmentの紐付けClassをアップデートする
            UpdateItemsIfNeeded(itemID);
        }
    }

    /// <summary>
    /// 手持ちにEquipmentを新たに追加する
    /// </summary>
    /// <param name="itemID"></param>
    public void AddItem(ItemData itemData)
    {
        if (FindOwnItemWithID(itemData.ID, out OwnItem own))
        {
            //own.freeCount += count;
            //own.totalCount += count;
        }
        else
        {
            myArmyData.OwnItems.Add(new OwnItem(itemData));

            // GameControllerのUI共通のEquipmentDataとOwnEquipmentの紐付けClassをアップデートする
            UpdateItemsIfNeeded(itemData.ID);
        }
    }

    /// <summary>
    /// EquipmentをListから削除する
    /// </summary>
    /// <param name="equipmentID"></param>
    /// <param name="count"></param>
    public void DeleteItem(string equipmentID, int count)
    {
        if (FindOwnItemWithID(equipmentID, out OwnItem own))
        {
            if (own.FreeCount >= count)
            {
                own.FreeCount -= count;
                own.TotalCount -= count;

                if (own.TotalCount == 0)
                {
                    Print(equipmentID, own.TotalCount);
                    // 所持数が0になった
                    myArmyData.OwnItems.Remove(own);
                    // GameControllerのUI共通のEquipmentDataとOwnEquipmentの紐付けClassをアップデートする
                    UpdateItemsIfNeeded(equipmentID);
                }
            }
            else
            {
                PrintError($"DeleteEquipment: Try to reomve {count} {equipmentID}, but count of free item is {own.FreeCount}");
            }
        }
        else
        {
            PrintError($"DeleteEquipment: {equipmentID} is not exist in OwnEquipments");
        }
    }

    /// <summary>
    /// 装備をUnitに追加する
    /// </summary>
    /// <param name="unit">TargetのUnit</param>
    /// <param name="index">変更するEquipmentスロットのindex</param>
    /// <param name="equipmentID">追加するEquipment</param>
    public void SetItem(UnitData unit, int index, string equipmentID)
    {
        // 装備したいEquipmentがOwnEquipmentに存在するか
        if (!FindOwnItemWithID(equipmentID, out OwnItem equipment))
        {
            PrintError($"SetEquipment: {equipmentID} is not exist in OwnEquipments");
            return;
        }

        // 装備したいEquipmentの数が余っているか ---- Unlock型にしたため削除---
        //if (equipment.freeCount == 0)
        //{
        //    PrintError($"SetEquipment: {equipmentID}: {equipment.data.name} Not much equipment");
        //    return;
        //}

        // equipment.freeCount--;

        // 装備 or 既にHolderに入っている装備を取り外してから装備
        var targetHolder = unit.MyItems[index];
        if (targetHolder.Id != null)
            RemoveItemFromHolder(unit, index);
        targetHolder.Id = equipment.Id;
    }

    /// <summary>
    /// 装備をUnitにつける
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="holder"></param>
    /// <param name="itemID"></param>
    public void SetItem(UnitData unit, ItemHolder holder, string itemID)
    {
        if (!FindOwnItemWithID(itemID, out OwnItem item))
        {
            PrintError($"SetEquipment: Try to set item {unit}, but {itemID} is not exist in OwnEquipments");
            return;
        }

        if (holder.Id != null)
            RemoveItemFromHolder(unit, holder);
        holder.Id = item.Id;
    }

    /// <summary>
    /// 装備をHolderから取り外す
    /// </summary>
    /// <param name="unit">対象のUnit</param>
    /// <param name="index">取り外すHolderのindex</param>
    /// <returns>取り外されたアイテムのID  ItemがOwnEquipmentsに存在しない場合Null</returns>
    public string RemoveItemFromHolder(UnitData unit, int index)
    {
        var holder = unit.MyItems[index];
        var itemID = holder.Id;
        // Holderに入っている装備をOwnEquipmentの所持数に戻す
        if (FindOwnItemWithID(holder.Id, out OwnItem own))
        {
            // freeCountを1増加
            //own.freeCount++;
        }
        else
        {
            // OwnEquipmentに存在しないがUnitのHolderに入っている装備は削除される
            itemID = null;
            Print($"RemoveItemFromHolder: {holder.Id} is not exist in OwnEquipments");
        }

        holder.Id = null;

        return itemID;
    }

    /// <summary>
    /// 装備をHolderから取り外す
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="holder"></param>
    /// <returns></returns>
    public string RemoveItemFromHolder(UnitData unit, ItemHolder holder)
    {
        var itemID = holder.Id;
        //// Holderに入っている装備をOwnEquipmentの所持数に戻す
        //if (FindOwnItemWithID(holder.id, out OwnItem own))
        //{
        //    // freeCountを1増加
        //    // own.freeCount++;
        //}
        //else
        //{
        //    // OwnEquipmentに存在しないがUnitのHolderに入っている装備は削除される
        //    itemID = null;
        //    Print($"RemoveItemFromHolder: {holder.id} is not exist in OwnEquipments");
        //}

        holder.Id = null;

        return itemID;
    }
}

/// <summary>
/// List表示用のItemData
/// </summary>
public class ItemInList
{
    public ItemInList(OwnItem own)
    {
        this.own = own;
        this.data = own.ItemData;
    }

    public ItemInList()
    {
    }

    /// <summary>
    /// 所持しているItem
    /// </summary>
    public OwnItem own;
    /// <summary>
    /// itemの元データ
    /// </summary>
    public ItemData data;

    public string cost;

    public override string ToString()
    {
        return $"OwnEquipID: {own.Id}, EquipData: ({data})";
    }
}


/***********************************************************************************
 *  SquadData
 */
/// <summary>
/// Squadの実データ
/// </summary>
[Serializable]
public class Squad
{
    #region Parameter
    /// <summary>
    /// Squadの名前
    /// </summary>
    public string name = "Default Squad";
    /// <summary>
    /// SquadのID
    /// </summary>
    public string ID = "";
    /// <summary>
    /// SquadがMap上に存在しているか
    /// </summary>
    public bool isOnMap = false;
    /// <summary>
    /// Squadが位置しているLocationのID
    /// </summary>
    public string LocationID = "";
    /// <summary>
    /// Squadが退くための退避エリアにいるか
    /// </summary>
    public bool IsOnTurnout = false;
    /// <summary>
    /// Squadが位置しているRoadのID
    /// </summary>
    public string RoadID = "";
    /// <summary>
    /// 部隊長
    /// </summary>
    public UnitData commander;
    /// <summary>
    /// 隊員
    /// </summary>
    public List<UnitData> member = new List<UnitData>();
    /// <summary>
    /// 隊員の最大値
    /// </summary>
    public int maxMemberCount = 3;
    /// <summary>
    /// 気力の現在値
    /// </summary>
    public float supplyLevel = 10;
    #endregion

    #region GetterSetter
    /// <summary>
    /// Supplyの最大値 (カバーパラメーター 表示ではDaysを置く)
    /// </summary>
    public int MaxSupply
    {
        get
        {
            return BaseSupply + AdditionalSupply;
        }
    }

    /// <summary>
    /// 装備品を除いたUnitの基本SupplyValue
    /// </summary>
    public int BaseSupply
    {
        get
        {
            int v;
            if (member.Count != 0)
                v = member.Sum(m => m.BaseSupply) + (int)(commander.BaseSupply * 1.1f);
            else
                v = commander.BaseSupply;
            return v;
        }
    }

    /// <summary>
    /// 装備品によるSupplyの追加値
    /// </summary>
    public int AdditionalSupply
    {
        get
        {
            if (member.Count != 0)
            {
                return member.Sum(m => m.AdditionalSupply) + (int)(commander.AdditionalSupply * 1.1f);
            }
            else
            {
                return commander.AdditionalSupply;
            }
        }
    }

    /// <summary>
    /// 待機する場合SquadがSupplyで何日待機できるか
    /// </summary>
    public float MaxSupplyDaysWhenWaiting
    {
        get => BaseSupplyDaysWhenWaiting + AdditionalSupplyDaysWhenWaiting;
    }

    /// <summary>
    /// UnitのbaseSupplyで 待機する場合SquadがSupplyで何日待機できるか
    /// </summary>
    public float BaseSupplyDaysWhenWaiting
    {
        get => (float)Math.Round(BaseSupply / GameManager.Instance.GeneralParameter.SupplyCostOnMap, 1);
    }

    /// <summary>
    /// 装備による追加のsupplyで 待機する場合SquadがSupplyで何日待機できるか
    /// </summary>
    public float AdditionalSupplyDaysWhenWaiting
    {
        get => (float)Math.Round(AdditionalSupply / GameManager.Instance.GeneralParameter.SupplyCostOnMap, 1);
    }

    /// <summary>
    /// 現時点のSupplyで活動できる日数
    /// </summary>
    public float DaysOfRemainingSupply
    {
        get
        {
            return GameManager.Instance.GeneralParameter.DaysOfRemainingSupply(supplyLevel);
        }
    }



    /// <summary>
    /// ランタイム時にSquadの位置しているMapLocationをおいておく
    /// </summary>
    public MainMap.MapLocation MapLocation
    {
        set
        {
            _mapLocation = value;
            LocationID = value == null ? "" : value.id;
        }
        get => _mapLocation;
    }
    [NonSerialized] MainMap.MapLocation _mapLocation;
    #endregion

    public Squad(UnitData commander)
    {
        this.commander = commander;
        ID = GetUUID();
        maxMemberCount = commander.CommanderParameter.MemberCount;

        if (commander.CommanderParameter.DefaultSquadName != null)
            name = commander.CommanderParameter.DefaultSquadName;
    }

    public override string ToString()
    {
        return $"SquadName: {name}, ID: {ID}";
    }
}