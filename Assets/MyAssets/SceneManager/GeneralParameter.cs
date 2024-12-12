using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tactics.Character;
using Tactics.Map;
using Tactics.AI;
using System;
using static Utility;

/// <summary>
/// 一般的なパラメーター
/// </summary>
public class GeneralParameter : MonoBehaviour
{

    [Header("Tacticsの一般的なパラメーター")]
        
    [Tooltip("カバーに隠れその位置が敵から隠れている場合の命中率の補正")]
    [SerializeField] public float CoverCloakingRate = 0.1f;

    [Tooltip("Unitが動いていないとして扱われる距離")]
    [SerializeField] public float NotMoveDistance = 1.0f;

    [Tooltip("Tacticsのターン終了時の待ち時間")]
    [SerializeField] public float tacticsTurnEndInterval = 1.5f;


    [Header("TacticsのWeaponのパラメーター")]
    [Tooltip("射程距離までの距離による命中率低下のグラフ")]
    [SerializeField] public AnimationCurve weaponReductionCurve;

    [Tooltip("敵がTileに侵入時のカウンターアタック命中率の係数")]
    [SerializeField] public float counterattackHitRate = 0.4f;

    [Tooltip("対物武器でHuman型を狙った際の命中率低下率")]
    [SerializeField] public float ReduceRateUsingAntiObjectWeapon;

    [Tooltip("Grenadeの命中率")]
    [SerializeField] public AnimationCurve grenadeHitRate;

    [Tooltip("Unitの敵発見視野角")]
    [SerializeField] public float DetectionViewAngle = 100;

    [Tooltip("Unitの敵発見CurveのValueが加算されていく時間 1になると発見状態")]
    [SerializeField] public float DetectUnitTick = 0.3f;

    [Tooltip("Unitの敵発見距離と発見Tickが貯まるまでの時間Curve")]
    [SerializeField] public AnimationCurve DetectionDistanceCurve;

    [Tooltip("Unitが角度に関係なく敵を一瞬で見つける距離\n真後ろに立って気づかないとかの防止")]
    [SerializeField] public float ForceFindEnemyDistance = 5;

    [Tooltip("Unitが発見したとかの時の頭上アイコンの距離によるサイズ変更の倍率")]
    [SerializeField] public float HeadUpIconSizeRate = 1;

    [Tooltip("UnitがItemを投げる時の角度に対するForceの増加グラフ\n" +
"Angleのとる値はUnitがどれだけ俯角を取れるかで決まるがおおよそ-1~1の値をとる")]
    [SerializeField] public AnimationCurve ThrowItemAngleAndForceCurve;

    [Header("Tactics AI関連のパラメーター")]

    [Tooltip("Shpear raycastで発射するShpearの大きさ")] 
    [SerializeField] public float BulletCastRadius = 0.1f;

    [Tooltip("MoveToActionを選択する場合のHealthスコアの最低値")]
    [SerializeField] public float ThresholdOfHealthForMoveToAction = 0.6f;

    [Tooltip("UnitがActiveな行動を取るHealthの最低値")]
    [SerializeField] public float ThresholdOfWarningHealth = 0.3f;

    [Tooltip("Weaponの射程距離につけられる係数")]
    [SerializeField] public float weaponRangeCoefficient = 1f;

    [Tooltip("AIが行動中にPlayerを発見しやすくなる係数")]
    [SerializeField, Range(1, 3)] public float easyFindOutWhileNPCMoving = 1;

    [Tooltip("TacticsSceneでEnemyのAIを有効化する")]
    [SerializeField] public bool enemyAiEnable = true;
    
    [Tooltip("TacticsSceneでPlayerのAIを有効化する")]
    [SerializeField] public bool playerAiEnable = false;


    [Header("TacticsのTileCell")]
    [Tooltip("現在位置しているTileのColor")]
    [SerializeField] public Color CurrentTileColor;

    [Tooltip("移動可能なTileの色")]
    [SerializeField] public Color CanEnterTileColor;

    [Tooltip("移動不可能なTileの色")]
    [SerializeField] public Color NotEnterTileColor;

    [Tooltip("TileDecalの色変化アニメーション")]
    [SerializeField] public AnimationCurve TileColorAnimationCurve;


    [Header("MainMap")]
    [Tooltip("一日を何分で終了するか")]
    [SerializeField] public float dayLengthMinute = 24;

    [Tooltip("一日を何秒で終了するか")]
    public float DayLengthSeconds => dayLengthMinute * 60;

    [Tooltip("ReduceSupplyなどのUpdateを呼び出す秒数")]
    [SerializeField] public float timerCallbackSeconds = 5;

    [Tooltip("Squadの移動速度")]
    [SerializeField] public float squadSpeedOnMainMap = 0.1f;

    [Tooltip("Unit一人あたりのBaseのSupply量")]
    [SerializeField] public int BaseSupplyAtUnit = 25;

    [Tooltip("Map上にいる場合の1日で消費するsupplyの量")]
    [SerializeField] public float SupplyCostOnMap;

    [Tooltip("Squadが基地に入った時瞬時に回復するSupplyの割合")]
    [SerializeField] public float RecoveringSupplyWhenEnterBase;

    [Tooltip("Squadが基地の上に入った瞬間に回復するSupplyの割合")]
    [SerializeField] public float RevcoveringSupplyWhenOnBase;

    [Tooltip("Squadが基地の上にいる場合に回復する係数 *基地内だと1日でSupplyは回復する")]
    [SerializeField] public float RecoveringSupplyRateWhenStayOnBase;

    [Tooltip("SquadからSquadへのSupplyの移動が行われた時1時間で移動できるSupplyの量")]
    [SerializeField] public float MoveSupplyingAmountPerHour = 1;

    [Tooltip("Squadが出撃可能なSupplyの最低レベル")]
    [SerializeField] public float CanActivateSupplyRate;

    [Tooltip("SpawnPointに到着したと判断する最短の距離")]
    [SerializeField] public float DistanceOfLocatedOnSpawnPoint = 0.005f;
    
    [Tooltip("SpawnPointの付近に到着もしくは離れた場合の距離")]
    [SerializeField] public float NearDistanceOfLocatedOnSpawnPoint = 0.05f;


    [Header("Data系")]
    [Tooltip("SaveDataの概要を記したSaveDataInfoをJsonとして出力する")]
    [SerializeField] public bool outputSaveDataInfoAsJson = false;

    [Tooltip("バイナリデータにセーブされたMyArmyDataの内容をJsonに出力する")]
    [SerializeField] public bool outputSavedMyArmyDataAsJson = false;

    [Tooltip("Event系のDebugJsonを出力する")]
    [SerializeField] public bool outputEventDebugLog = false;


    /// <summary>
    /// 与えられたSupplyでどの程度の日数活動できるかの計算
    /// </summary>
    /// <param name="supply"></param>
    /// <returns></returns>
    public float DaysOfRemainingSupply(float supply)
    {
        var value = supply / SupplyCostOnMap;
        return (float)Math.Round(value, 1);
    }
}
