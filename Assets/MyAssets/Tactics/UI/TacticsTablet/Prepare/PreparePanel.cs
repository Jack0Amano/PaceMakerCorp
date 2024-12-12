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
using Tactics.Map;
using Tactics;

namespace TacticsTablet.Prepare
{
    /// <summary>
    /// Tactics準備画面のMainClass
    /// </summary>
    public class PreparePanel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] PrepareListAdapter listAdapter;
        [SerializeField] MainMap.UI.SelectItem.SelectItemWindow SelectItemWindow;
        [SerializeField] PrepareMap prepareMap;
        [Header("UIs")]
        [SerializeField] Button DebugReloadUnits;
        [SerializeField] TextMeshProUGUI squadNameLabel;
        [SerializeField] internal Button startBattleButton;
        [SerializeField] TextMeshProUGUI sortieCostLabel;
        [SerializeField] TextMeshProUGUI supplyLabel;
        [SerializeField] GameObject supplyCostValue;

        internal CanvasGroup canvasGroup;
        internal TilesController tilesController;
        GameManager gameManager;
        GeneralParameter generalParameter;
        private MainMap.ReachedEventArgs encounter;

        /// <summary>
        /// Playerの準備の結果の情報
        /// </summary>
        public List<PreparesResult> PlayerResults { private set; get; }
        /// <summary>
        /// Enemyの準備の結果の情報
        /// </summary>
        public List<PreparesResult> EnemyResults { private set; get; }

        /// <summary>
        /// 戦闘開始ボタンが押された際のイベント
        /// </summary>
        public Action OnStartBattle;

        /// <summary>
        /// デバッグモード
        /// </summary>
        public bool IsDebugMode
        {
            get => encounter == null;
        }

        /// <summary>
        /// 出撃コスト
        /// </summary>
        private float SortieCostValue
        {
            get => sortieCostValue;
            set
            {
                if (generalParameter == null)
                    return;
                sortieCostValue = value;
                sortieCostLabel.text = "-"+ generalParameter.DaysOfRemainingSupply(SortieCostValue).ToString();
                if (!IsDebugMode)
                    supplyLabel.text = generalParameter.DaysOfRemainingSupply(encounter.Player.supplyLevel - SortieCostValue).ToString();
            }
        }
        float sortieCostValue;

        protected private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            listAdapter.ChangeItemAction += ChangeEquipAction;
            listAdapter.selectUnitAction += SelectUnitAction;
            prepareMap.removeUnitAction += DeleteUnitFromSpawnPoint;
            DebugReloadUnits.onClick.AddListener(() => ShowAllUnits(true));
            startBattleButton.onClick.AddListener(() => StartBattleButton());
            gameManager = GameManager.Instance;
            encounter = gameManager.ReachedEventArgs;
            generalParameter = gameManager.GeneralParameter;

            sortieCostLabel.text = "0";
        }

        // Start is called before the first frame update
        protected private void Start()
        {
        }

        protected private void Update()
        {
        }

        /// <summary>
        /// PreparePanelを表示にする
        /// </summary>
        public void Show()
        {
            IEnumerator ShowUnits()
            {
                canvasGroup.DOFade(1, 0.5f);
                yield return new WaitForSeconds(0.5f);

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
                    squadNameLabel.SetText(encounter.Player.name);
                    supplyLabel.text = encounter.Player.DaysOfRemainingSupply.ToString();

                }
            }

            gameObject.SetActive(true);
            StartCoroutine(ShowUnits());

            UserController.enableCursor = true;
        }

        /// <summary>
        /// PreparePanelを非表示にする
        /// </summary>
        public void Hide(bool animation=true)
        {
            if (canvasGroup.alpha == 0)
                return;
            if ( !animation)
            {
                canvasGroup.alpha = 0;
                gameObject.SetActive(false);
            }
            else
            {
                canvasGroup.DOFade(0, 0.5f).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
        }

        /// <summary>
        /// Listに自軍のデータを表示する
        /// </summary>
        private List<PrepareItemModel> ShowMyUnits()
        {
            var units = new List<UnitData>() { encounter.Player.commander };
            units.AddRange(encounter.Player.member);
            var models = units.ConvertAll(u =>
            {
                return new PrepareItemModel(u);
            });
            listAdapter.SetItems(models);
            var startTilesID = tilesController.GetStartTiles(encounter.SpawnRequestData.StartPosition, false).ConvertAll(t => t.id);
            prepareMap.SetStartTileIDs(startTilesID);

            return models;
        }

        /// <summary>
        /// ListにすべてのUnitのデータを表示する (デバッグ用)
        /// </summary>
        private List<PrepareItemModel> ShowAllUnits(bool reload = false)
        {
            if (reload)
            {
                gameManager.StaticData.ReloadStaticSceneData();
                prepareMap.RemoveAllUnits();
            }
                
            var models = gameManager.StaticData.AllUnitsData.units.ConvertAll((u) =>
            {
                var data = new UnitData(u);
                data.MyItems = data.Data.BaseItems;
                return new PrepareItemModel(data);
            });
            listAdapter.SetItems(models);
            
            prepareMap.SetStartTileIDs(tilesController.Tiles.ConvertAll(t => t.id));
            prepareMap.SetWaysDropdown(tilesController.waysPassPoints.ConvertAll(w => w.parent.name));

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
            canvasGroup.DOFade(0.3f, 0.3f);
            SelectItemWindow.ShowAllItems(pair.holder.Type, (item) =>
            {
                pair.holder.Id = item;
                listAdapter.ForceUpdateViewsHolderIfVisible(pair.modelIndex);
                canvasGroup.DOFade(1f, 0.3f);
            });
        }

        /// <summary>
        /// Unitを選択した時に呼び出し
        /// </summary>
        /// <param name="model"></param>
        private void SelectUnitAction(PrepareItemModel model)
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
                    EnemyLevel = 0,
                    cost = 0,
                    exp = 0,
                    tileAndUnits = tileAndUnits
                };
                encounter = encount;
            }
            else
            {
                encounter.Player.supplyLevel -= SortieCostValue;
            }
                

            PlayerResults = new List<PreparesResult>();
            foreach (var t in prepareMap.PlayerLists)
            {
                if (t.UnitDatas.Count == 0) continue;
                var result = new PreparesResult()
                {
                    IsPlayer = t.UnitAttribute == UnitAttribute.PLAYER,
                    TileID = t.TileID,
                    Units = t.UnitDatas
                };
                PlayerResults.Add(result);
            }

            OnStartBattle?.Invoke();
            Hide();

        }

    }

    /// <summary>
    /// PreparePanelで選択されたUnitの出撃情報を保持する
    /// </summary>
    [SerializeField]
    public class PreparesResult
    {
        public bool IsPlayer;
        public string TileID;
        public List<UnitData> Units;

        public override string ToString()
        {
            return $"PreapareResult: Tile.{TileID}, Player.{IsPlayer}, Units.{string.Join(",", Units)}";
        }
    }
}