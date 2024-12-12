using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using DG.Tweening;
using static Utility;

namespace MainMap.UI.Squads
{
    /// <summary>
    /// UIのSquadを選択する画面
    /// </summary>
    public class SquadsPanel : MainMapUIPanel
    {
        
        [SerializeField] ScrollRect scrollRect;

        [SerializeField] internal Detail.SquadDetail squadDetail;
        [SerializeField] Button addSquadButton;
        [SerializeField] GameObject squadCardTemplate;
        [SerializeField] public SelectUnit.SelectUnitWindow selectCommWindow;

        // [SerializeField] TMP_Dropdown baseDropdown;

        private GameObject squadContentsObject;

        private readonly List<SquadCard> squadCards = new List<SquadCard>();

        /// <summary>
        /// Squadが出撃した際のHandler  MainMapControllerにフロントのプロパティが存在する
        /// </summary>
        public EventHandler<SquadActionArgs> squadActionHandler;


        /// <summary>
        /// Commanderになれる兵種の種別
        /// </summary>
        public static readonly HashSet<UnitType.Type> CommanderType = new HashSet<UnitType.Type> 
        { 
            UnitType.Type.Infantry, 
            UnitType.Type.AntiTank, 
            UnitType.Type.Medical, 
            UnitType.Type.Mortar 
        };

        /// <summary>
        /// 味方陣地のList
        /// </summary>
        private readonly List<(string id, string name)> locations = new List<(string id, string name)>();

        /// <summary>
        /// 現在選択中のSquad
        /// </summary>
        public Squad selectedSquad;


        protected override void Awake()
        {
            base.Awake();

            squadContentsObject = scrollRect.content.gameObject;
            addSquadButton.onClick.AddListener(() => AddSquad());
        }

        protected void Start()
        {
            squadDetail.unitCardList.ParentWindowCanvasGroup = parentWindowCanvasGroup;
        }

        internal override void Show()
        {
            base.Show();
            SetSquadCardsFromGameController(selectedSquad);
            squadDetail.squadDetailUpdatedHandler = SquadDetailUpdated;

            if (selectedSquad != null)
                squadDetail.Show(squadCards.Find(c => c.Squad == selectedSquad));
            selectCommWindow.calledWhenHide = (() =>
            {
                parentWindowCanvasGroup.DOFade(1, duration);
            });
        }


        internal override void Hide(Action onCompletion = null)
        {
            base.Hide(onCompletion);
            squadCards.ForEach(c => c.IsSelected = false);
            selectCommWindow.Hide();
            squadDetail.Hide();
        }

        #region Squadを追加
        /// <summary>
        /// SquadデータからSquadCardを作成
        /// </summary>
        /// <param name="squad"></param>
        private void AddSquadCard(Squad squad, bool selectSquad = false)
        {
            var clone = Instantiate(squadCardTemplate, squadContentsObject.transform);
            SetObjectAsSquadCard(clone, squad, selectSquad);
        }

        /// <summary>
        /// gameControllerのSquadsから必要枚数分cardを作成し描写する
        /// </summary>
        /// <returns></returns>
        private void SetSquadCardsFromGameController(Squad selected = null)
        {
            // locationIDが空の場合はAll Squadsが表示される
            List<Squad> squads = GameManager.Instance.DataSavingController.MyArmyData.Squads;

            // Squadが空なら全て削除する
            if (squads.Count == 0)
            {
                squadCards.ForEach(c => DestroyImmediate(c.gameObject));
                squadCards.Clear();
            }
            else if (squadCards.Count == 0)
            {
                // Squadが空ではなく、かつsquadCardsが一枚もない場合はfirstCardをaddressから読み込み
                var cardObject = Instantiate(squadCardTemplate, squadContentsObject.transform);
                SetObjectAsSquadCard(cardObject, squads[0], false);
            }

            var index = 1;
            while (true)
            {
                if (squadCards.IndexAt_Bug(index, out SquadCard card))
                {
                    if (squads.IndexAt_Bug(index, out Squad squad))
                    {
                        // SquadCards[index]とsquads[index]の両方とも存在する場合
                        card.SetSquad(squad);
                        index++;
                        continue;
                    }
                    else
                    {
                        // SquadCards[index]が存在するがsquads[index]がない場合; 余剰なCard
                        // index - maxIndexまで余剰
                        squadCards.GetRange(index, squadCards.Count - index).ForEach(c => DestroyImmediate(c.gameObject));
                        squadCards.RemoveRange(index, squadCards.Count - index);
                        break;
                    }
                }
                else
                {
                    if (squads.IndexAt_Bug(index, out Squad squad))
                    {
                        // SquadCards[index]は存在しないが、squads[index]は存在する場合 cardが不足
                        // 枚数分cardをindex-1からコピーして作成
                        var count = squads.Count - index;
                        for(int i=index; i<index + count; i++)
                        {
                            var cardObj = Instantiate(squadCards[index - 1].gameObject, squadContentsObject.transform);
                            SetObjectAsSquadCard(cardObj, squads[i], false);
                        }
                        break;
                    }
                    else
                    {
                        // SquadCards[index]とsquads[index]ともに存在しない場合
                        // 両者の数が一致
                        break;
                    }
                }
            }

            // SelectedされているSquadが存在する場合 これのSelectSquadを実行
            // この時点でカードは全て完成している状態
            if (selected != null)
            {
                var selectedCard = squadCards.Find(c => c.Squad.Equals(selected));
                if (selectedCard != null)
                    SelectSquad(selectedCard);
            }

        }

        /// <summary>
        /// 新たに作ったCardを新規SquadCardとして登録
        /// </summary>
        /// <param name="cardObject"></param>
        /// <param name="data"></param>
        /// <param name="selectSquad">追加後にSquadを選択するか</param>
        private void SetObjectAsSquadCard(GameObject cardObject, Squad data, bool selectSquad)
        {
            var squadCard = cardObject.GetComponent<SquadCard>();
            squadCard.SetSquad(data);
            squadCards.Add(squadCard);
            squadCard.SelectSquadButton.onClick.AddListener(() => SelectSquad(squadCard));
            squadCard.removeSquadButton.onClick.AddListener(() => RemoveSquad(cardObject));

            if (selectSquad)
                SelectSquad(squadCard);
        }

        
        #endregion

        /// <summary>
        /// SquadCard の Prefabから参照 Squad選択時の
        /// </summary>
        /// <param name="card"></param>
        private void SelectSquad(SquadCard card)
        {

            if (squadDetail.Squad  == card.Squad && squadDetail.gameObject.activeSelf)
            {
                card.ShakeCard();
                return;
            }
            if (squadDetail.gameObject.activeSelf)
            {
                squadCards.ForEach(c => c.IsSelected = false);
                card.IsSelected = true;
                squadDetail.Change(card);
                return;
            }

            // 通常の新規読み込み
            card.IsSelected = true;
            squadDetail.Show(card);
        }

        /// <summary>
        /// Squadをボタン操作から追加する
        /// </summary>
        public void AddSquad()
        {
            Action<UnitData> callback = ((param) =>
            {
                if (param == null) return;
                // Squadを新規作成
                var squad = GameManager.Instance.DataSavingController.MyArmyData.ArmyController.MakeSquad(param);
                selectedSquad = squad;
                AddSquadCard(squad, true);
            });
            selectCommWindow.Show(CommanderType, UnitType.Type.Infantry, callback, true);
            parentWindowCanvasGroup.DOFade(0.3f, duration);
        }

        /// <summary>
        /// Squadを削除
        /// </summary>
        /// <param name="cardObject"></param>
        private void RemoveSquad(GameObject cardObject)
        {
            var squadCard = cardObject.GetComponent<SquadCard>();
            if (squadDetail.Squad == squadCard.Squad)
            {
                squadDetail.Hide();
            }
            GameManager.Instance.DataSavingController.MyArmyData.ArmyController.DeleteSquad(squadCard.Squad);

            squadCards.Remove(squadCard);
            DestroyImmediate(cardObject);
        }

        /// <summary>
        /// Squadの情報を更新する
        /// </summary>
        private void SquadDetailUpdated(object o, EventArgs args)
        {
            var card = squadCards.Find(c => c.IsSelected);
            if (card == null)
                return;

            card.UpdateInfo();
        }
    }

    /// <summary>
    /// Squadが出撃した際のEventArgs
    /// </summary>
    public class SquadActionArgs: EventArgs
    {
        public Squad squad;
        public string spawnLocationID;
    }
}