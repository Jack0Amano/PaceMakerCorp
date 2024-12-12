using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using DG.Tweening;
using static Utility;

namespace MainMap.UI.Squads.Detail
{
    /// <summary>
    /// UnitCardをリスト化してまとめておく
    /// </summary>
    public class UnitCardList : MonoBehaviour
    {
        [Tooltip("Cardが入る予定のImageObject")]
        [SerializeField] List<Image> cardBases;
        [Tooltip("CardBasesにUnitCardを追加する際のボタン")]
        [SerializeField] Button addUnitButton;

        [Header("Controllers")]
        [SerializeField] internal SelectUnit.SelectUnitWindow SelectUnitWindow;

        [Header("Images")]
        [SerializeField] Sprite blankCardImage;
        [SerializeField] Sprite closedCardImage;

        internal List<UnitCard> UnitCards = new List<UnitCard>();

        public Squad Squad { private set; get; }

        public Action<UnitData> AddUnitCallback;

        public Action<UnitData> RemoveUnitCallback;

        internal Action<UnitData, UnitData> ChangeUnitCallback;

        internal CanvasGroup ParentWindowCanvasGroup;

        private void Awake()
        {
            addUnitButton.onClick.AddListener(() => AddUnit());
        }

        /// <summary>
        /// AddButtonからUnitをSquadに追加 (Inspectorから呼び出し)
        /// </summary>
        [SerializeField]
        public void AddUnit()
        {
            Action<UnitData> endCallback = ((result) =>
            {
                AddUnitCard(result);
                UpdateCardsLocation();
                AddUnitCallback(result);
            });
            var showType = new HashSet<UnitType.Type> { UnitType.Type.Infantry, UnitType.Type.Medical };
            SelectUnitWindow.Show(showType, UnitType.Type.Infantry, endCallback);
        }

        /// <summary>
        /// 各カードの位置やAddUnitButtonの位置を更新する
        /// </summary>
        /// unitCardsを移動したときに毎回更新する
        private void UpdateCardsLocation()
        {
            for (int i = 0; i < UnitCards.Count; i++)
            {
                var baseObj = cardBases[i];
                var cardObj = UnitCards[i].gameObject;

                var parent = cardObj.transform.parent;

                if (parent == baseObj) continue;

                // CardのparentをbaseObjに変更

                cardObj.transform.SetParent(baseObj.transform);

                cardObj.transform.localScale = Vector3.one;

                // TODO: アニメーション移動
                cardObj.transform.localPosition = Vector3.zero;

            }

            // AddUnitが可能であれば空いた部分にAddUnitButtonを移動
            // 追加できなければ Hideする
            if (UnitCards.Count < cardBases.Count && UnitCards.Count <= Squad.maxMemberCount)
            {
                var emptyBase = cardBases[UnitCards.Count];
                addUnitButton.transform.position = emptyBase.transform.position;
                addUnitButton.gameObject.SetActive(true);
            }
            else
            {
                addUnitButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// SquadのUnitのデータを表示する
        /// </summary>
        internal IEnumerator SetSquadCard(Squad squad)
        {
            this.Squad = squad;

            // CardのClosedとBlankのイメージの読み込みが終わっていない場合は待つ
            while (blankCardImage == null || closedCardImage == null)
                yield return null;

            // Cardの表示のClosedとBlankのImageをset
            for(int i=1; i<cardBases.Count; i++)
            {
                if (i <= squad.maxMemberCount)
                    cardBases[i].sprite = blankCardImage;
                else
                    cardBases[i].sprite = closedCardImage;
            }

            // 部隊長のUnitCardを作成
            var firstUnitCard = cardBases.Find(b => b.transform.childCount != 0).GetComponentInChildren<UnitCard>();
            firstUnitCard.name = "UnitCard";
            SetParameterOnCard(firstUnitCard, squad.commander, true);

            for (var i = 1; i <= squad.member.Count; i++)
            {
                if (UnitCards.IndexAt_Bug(i, out UnitCard card))
                {
                    card.SetParameter(squad.member[i - 1]);
                }
                else
                {
                    AddUnitCard(squad.member[i - 1]);
                }
            }

            if (squad.member.Count + 1 < UnitCards.Count)
            {
                var unitCardsCount = UnitCards.Count;
                for (int i = squad.member.Count + 1; i < unitCardsCount; i++)
                {
                    cardBases[i].transform.RemoveAllChildren();
                    UnitCards.RemoveAt(squad.member.Count + 1);
                }
            }

            UpdateCardsLocation();
        }

        /// <summary>
        /// Card index0をクローンする形でカードを追加する
        /// </summary>
        /// <param name="index"></param>
        private void AddUnitCard(UnitData parameter, bool isLocked = false)
        {
            var clone = Instantiate(UnitCards[0]);
            SetParameterOnCard(clone, parameter, isLocked);
            
        }

        private void SetParameterOnCard(UnitCard card, UnitData parameter, bool isLocked = false)
        {
            //Print("Set parameter on card:", parameter);
            card.isLocked = isLocked;
            card.deleteButton.onClick.AddListener(() => DeleteUnit(card.gameObject));
            card.changeUnitButton.onClick.AddListener(() => ChangeUnit(card.gameObject));
            card.SelectUnitButton.onClick.AddListener(() => SelectUnit(card));
            card.SetParameter(parameter);
            UnitCards.Add(card);
        }

        /// <summary>
        /// UnitをDeleteした際の呼び出し
        /// </summary>
        private void DeleteUnit(GameObject unitCard)
        {
            var index = UnitCards.FindIndex((card) => card.gameObject == unitCard);
            cardBases[index].transform.RemoveAllChildren();
            RemoveUnitCallback(UnitCards[index].unitData);
            UnitCards.RemoveAt(index);

            UpdateCardsLocation();
        }

        /// <summary>
        /// Unitを変更する際の呼び出し
        /// </summary>
        /// <param name="unitCard"></param>
        private void ChangeUnit(GameObject unitCard)
        {
            var index = UnitCards.FindIndex((card) => card.gameObject == unitCard);
            Action<UnitData> endCallback = ((param) =>
            {
                ChangeUnitCallback(UnitCards[index].unitData, param);
                UnitCards[index].SetParameter(param);
            });

            var showType = new HashSet<UnitType.Type> { UnitType.Type.Infantry, UnitType.Type.Medical };
            SelectUnitWindow.Show(showType, UnitType.Type.Infantry, endCallback);
        }

        /// <summary>
        /// UnitCardを選択したときの呼び出し
        /// </summary>
        /// <param name="unitCard"></param>
        private void SelectUnit(UnitCard unitCard)
        {
            ParentWindowCanvasGroup.DOFade(0.3f, 0.3f);
            SelectUnitWindow.Show(unitCard.unitData);
            SelectUnitWindow.calledWhenHide += (() =>
            {
                unitCard.SetParameter(unitCard.unitData);
                ParentWindowCanvasGroup.DOFade(1f, 0.3f);
            });
        }
    }
}