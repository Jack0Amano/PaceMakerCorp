using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using static Utility;
using System;
using System.Linq;
using static Parameters.SpawnSquad.SpawnSquadData;

namespace Tactics.Prepare
{
    /// <summary>
    /// Tactics準備画面のMainClass
    /// </summary>
    public class PreparePanel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] Lists.PrepareListAdapter listAdapter;
        [SerializeField] MainMap.UI.SelectItem.SelectItemWindow SelectItemWindow;
        [SerializeField] PrepareMap prepareMap;
        [Header("UIs")]
        [SerializeField] Button DebugReloadUnits;
        [SerializeField] TextMeshProUGUI squadNameLabel;
        [SerializeField] internal Button startBattleButton;
        [SerializeField] TextMeshProUGUI sortieCostLabel;
        [SerializeField] TextMeshProUGUI supplyLabel;
        [SerializeField] GameObject supplyCostValue;

        public MainMap.ReachedEventArgs Encount { private set; get; }
        internal CanvasGroup CanvasGroup;
        internal Map.TilesController TilesController;

        /// <summary>
        /// Playerの準備の結果の情報
        /// </summary>
        public List<PreparesResult> PlayerResults { private set; get; }
        /// <summary>
        /// Enemyの準備の結果の情報
        /// </summary>
        public List<PreparesResult> EnemyResults { private set; get; }


        GameManager GameManager;
        GeneralParameter GeneralParameter;

        /// <summary>
        /// デバッグモード
        /// </summary>
        public bool IsDebugMode
        {
            get => Encount == null;
        }

        /// <summary>
        /// 出撃コスト
        /// </summary>
        private float SortieCostValue
        {
            get => _sortieCostValue;
            set
            {
                if (GeneralParameter == null)
                    return;
                _sortieCostValue = value;
                sortieCostLabel.text = "-"+ GeneralParameter.DaysOfRemainingSupply(SortieCostValue).ToString();
                if (!IsDebugMode)
                    supplyLabel.text = GeneralParameter.DaysOfRemainingSupply(Encount.Player.supplyLevel - SortieCostValue).ToString();
            }
        }
        float _sortieCostValue;

        protected private void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            listAdapter.ChangeItemAction += ChangeEquipAction;
            listAdapter.selectUnitAction += SelectUnitAction;
            prepareMap.removeUnitAction += DeleteUnitFromSpawnPoint;
            DebugReloadUnits.onClick.AddListener(() => ShowAllUnits(true));
            startBattleButton.onClick.AddListener(() => StartBattleButton());
            GameManager = GameManager.Instance;
            Encount = GameManager.Encounter;
            GeneralParameter = GameManager.GeneralParameter;

            sortieCostLabel.text = "0";
        }

        // Start is called before the first frame update
        protected private void Start()
        {
            IEnumerator ShowUnits()
            {
                while (!listAdapter.IsDataPrepared)
                    yield return null;

                prepareMap.IsDebugMode = IsDebugMode;
                if (IsDebugMode)
                {
                    // encountが存在しないためDebug用のデータを引っ張ってくる
                    // playersModels = ShowAllUnits();
                    listAdapter.CanSelectItems = true;
                    ShowAllUnits();
                    squadNameLabel.SetText("All Units (Debug)");
                    supplyLabel.text = "999";
                }
                else
                {
                    listAdapter.CanSelectItems = false;
                    ShowMyUnits();
                    squadNameLabel.SetText(Encount.Player.name);
                    supplyLabel.text = Encount.Player.DaysOfRemainingSupply.ToString();
                    
                }
            }

            StartCoroutine(ShowUnits());

            UserController.enableCursor = true;
        }

        protected private void Update()
        {
        }

        /// <summary>
        /// Listに自軍のデータを表示する
        /// </summary>
        private List<Lists.MyListItemModel> ShowMyUnits()
        {
            var units = new List<UnitData>() { Encount.Player.commander };
            units.AddRange(Encount.Player.member);
            var models = units.ConvertAll(u =>
            {
                return new Lists.MyListItemModel(u);
            });
            listAdapter.SetItems(models);
            prepareMap.SetStartTileIDs(Encount.PlayerPositions);

            return models;
        }

        /// <summary>
        /// ListにすべてのUnitのデータを表示する (デバッグ用)
        /// </summary>
        private List<Lists.MyListItemModel> ShowAllUnits(bool reload = false)
        {
            if (reload)
            {
                GameManager.StaticData.ReloadStaticSceneData();
                prepareMap.RemoveAllUnits();
            }
                
            var models = GameManager.StaticData.AllUnitsData.units.ConvertAll((u) =>
            {
                var data = new UnitData(u);
                data.MyItems = data.Data.BaseItems;
                return new Lists.MyListItemModel(data);
            });
            listAdapter.SetItems(models);
            
            prepareMap.SetStartTileIDs(TilesController.Tiles.ConvertAll(t => t.id));
            prepareMap.SetWaysDropdown(TilesController.WaysPassPoints.ConvertAll(w => w.parent.name));

            if (reload)
                Print("Static data is reloaded");

            return models;
        }

        /// <summary>
        /// Unitのアイテムを変更する際に呼び出される (Debug用)
        /// </summary>
        /// <param name="pair"></param>
        private void ChangeEquipAction((int modelIndex, ItemHolder holder) pair)
        {
            // TODO: EquipSelection.MakeCellsFromType(EquipmentType type)でownItemsがnullになってるためすべて読み込みに変える
            CanvasGroup.DOFade(0.3f, 0.3f);
            SelectItemWindow.ShowAllItems(pair.holder.Type, (item) =>
            {
                pair.holder.Id = item;
                listAdapter.ForceUpdateViewsHolderIfVisible(pair.modelIndex);
                CanvasGroup.DOFade(1f, 0.3f);
            });
        }

        /// <summary>
        /// Unitを選択した時に呼び出し
        /// </summary>
        /// <param name="model"></param>
        private void SelectUnitAction(Lists.MyListItemModel model)
        {

            if (model.tileID.Length != 0 && model.tileID != prepareMap.ActiveNameList.TileID)
            {
                // 既にチェック済みのUnitが別のTileに出撃要請された
                prepareMap.AddUnitOnActiveList(model.unitData);
            }
            else if (model.tileID.Length != 0)
            {
                // 既にチェック済みのUnitを下げる
                listAdapter.NotAcceptAnimation(model);
                prepareMap.RemoveUnit(model.unitData);
                var index = listAdapter.GetIndexFrom(model);
                listAdapter.CheckViewHolders("", index);
                SortieCostValue -= model.unitData.SortieCost;
            }
            else
            {
                // 新規出撃準備にUnitを配置する
                prepareMap.AddUnitOnActiveList(model.unitData);
                var index = listAdapter.GetIndexFrom(model);
                listAdapter.CheckViewHolders(prepareMap.ActiveNameList.TileID, index);
                SortieCostValue += model.unitData.SortieCost;
            }

            startBattleButton.interactable = prepareMap.IsEnoughtUnitsInList;
        }

        /// <summary>
        /// PrepareMapで出撃予定に入っていたUnitがそこから取り除かれたときの呼び出し
        /// </summary>
        /// <param name="data"></param>
        private void DeleteUnitFromSpawnPoint(NameList nameList, UnitData data)
        {
            SortieCostValue -= data.SortieCost;
            var index = listAdapter.GetIndexFrom(data);
            listAdapter.CheckViewHolders(nameList.TileID, index);
        }
    
        /// <summary>
        /// 戦闘を開始してTactics画面に移るときのボタンの呼び出し
        /// </summary>
        private void StartBattleButton()
        {
            if (!prepareMap.IsEnoughtUnitsInList)
                return;
            if (IsDebugMode)
            {
                // Debugmodeのためencountを設置
                var encount = new MainMap.ReachedEventArgs();
                var tileAndUnits = new List<TileAndEnemiesPair>();
                foreach(var list in prepareMap.EnemiesLists)
                {
                    if (list.UnitDatas.Count == 0)
                        continue;
                    var ways = list.UnitDatas.ConvertAll(d =>
                    {
                        return new UnitAndMovePair(d.ID, list.GetIndexOfWaysDropdown(d));
                    });
                    tileAndUnits.Add(new TileAndEnemiesPair()
                    {
                        tileID = list.TileID,
                        UnitAndMovePairs = ways
                    });
                }

                encount.Enemy = new Parameters.SpawnSquad.SpawnSquadData()
                {
                    ID = "Debug",
                    location = "",
                    EnemyLevel = 0,
                    IsNecessaryForEvent = false,
                    sort = 0,
                    cost = 0,
                    tacticsSceneID = "",
                    encounmtSpawnID = "Debug",
                    playerTileIDs = prepareMap.PlayerLists.FindAll(l => l.UnitDatas.Count!=0).ConvertAll(l => l.TileID),
                    exp = 0,
                    tileAndUnits = tileAndUnits
                };
                Encount = encount;
            }
            else
            {
                Encount.Player.supplyLevel -= SortieCostValue;
            }
                

            PlayerResults = new List<PreparesResult>();
            foreach (var t in prepareMap.PlayerLists)
            {
                if (t.UnitDatas.Count == 0) continue;
                var result = new PreparesResult()
                {
                    isPlayer = t.UnitAttribute == UnitAttribute.PLAYER,
                    tileID = t.TileID,
                    units = t.UnitDatas
                };
                PlayerResults.Add(result);
            }

            CanvasGroup.DOFade(0, 0.5f).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });

        }

    }

    [SerializeField]
    public class PreparesResult
    {
        public bool isPlayer;
        public string tileID;
        public List<UnitData> units;

        public override string ToString()
        {
            return $"PreapareResult: Tile.{tileID}, Player.{isPlayer}, Units.{string.Join(",", units)}";
        }
    }
}