using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utility;

/// <summary>
/// Sceneのゲームの固有のパラメーター GameManagerからアクセスできるようにMainMapがロードされた時点で渡される
/// </summary>
public class SceneParameter : MonoBehaviour
{
    [Header("UnitのData")]
    [Tooltip("どの兵種のレベルでHolderが開放されるか")]
    [SerializeField] public List<UnitTypeAndHolderLevel> UnitTypeAndHolderLevels;

    /// <summary>
    /// 現在のUnitDataから適当なUnitTypeAndHolderLevelを取得する
    /// </summary>
    /// <param name="unitData"></param>
    /// <returns></returns>
    public ItemHolderLevel GetItemHolderLevel(UnitData unitData)
    {
        var holderLevel = UnitTypeAndHolderLevels.Find(u => u.UnitType == unitData.UnitType);
        if (holderLevel == null)
            PrintError($"Miss to get UnitTypeAndHolderLevel of {unitData.UnitType} of {unitData}");
        var output = holderLevel.ItemHolderLevels.Find(h =>
        {
            return h.FromValue <= unitData.Level && unitData.Level <= h.ToValue;
        });
        if (output == null)
            PrintError($"Miss to get ItemHolderLevel of level {unitData.Level} {unitData}");
        return output;
    }
}
