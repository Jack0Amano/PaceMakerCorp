using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using DG.Tweening;
using System;
using static Utility;

namespace MainMap.UI.Squads.Detail
{
    public class SquadDetail : MonoBehaviour
    {
        [Header("DetailPanel")]
        [SerializeField] TextMeshProUGUI squadNameLabel;
        [Tooltip("Squadが移動できる現在の日数")]
        [SerializeField] TextMeshProUGUI supplyDaysLabel;
        [Tooltip("Squadが移動できる最大日数")]
        [SerializeField] TextMeshProUGUI maxSupplyDaysLabel;
        [Tooltip("Unitの基本Supply")]
        [SerializeField] TextMeshProUGUI baseSupplyLabel;
        [Tooltip("装備品により増加したSupply")]
        [SerializeField] TextMeshProUGUI additionalSupplyLabel;
        [SerializeField] TextMeshProUGUI sortieCostLabel;
        [Tooltip("Supplyを0-1まで入れたときに消費するSupply")]
        [SerializeField] TextMeshProUGUI supplyCostLabel;
        [SerializeField] TextMeshProUGUI memberLabel;
        [SerializeField] TextMeshProUGUI locationLabel;
        [Tooltip("Squadを出撃もしくは")]
        [SerializeField] internal Button activateButton;
        [Tooltip("Squadの耐久を回復させる")]
        [SerializeField] Button waitSupplyButton;

        [Header("Components")]
        [Tooltip("SquadにいるUnitのCardList")]
        [SerializeField] internal UnitCardList unitCardList;
        [Tooltip("Supply待ちの場合に表示するもの")]
        [SerializeField] internal Wait.WaitSupplyingPanel WaitSupplyingPanel;

        // [SerializeField] TMP_Dropdown locationsDropdown;

        private CanvasGroup canvasGroup;

        /// <summary>
        /// 現在SquadDetailで表示中のSquad
        /// </summary>
        public Squad Squad { private set; get; }
        public SquadCard SelectedCard { private set; get; }

        readonly private float animationTime = 0.5f;

        public EventHandler squadDetailUpdatedHandler;

        //public Action<object, Squad, LocationParamter> fastTravelRequest;

        private TextMeshProUGUI activateLabel;

        GeneralParameter parameter;
        GameManager gameManager;

        protected private void Awake()
        {
            unitCardList.AddUnitCallback = AddUnitCallBack;
            unitCardList.RemoveUnitCallback = RemoveUnitCallBack;
            unitCardList.ChangeUnitCallback = ChangeUnitCallBack;
            activateLabel = activateButton.GetComponentInChildren<TextMeshProUGUI>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            gameManager = GameManager.Instance;
            parameter = gameManager.GeneralParameter;
            waitSupplyButton.onClick.AddListener(() => StartCoroutine( WaitToSupplyAction()));
        }



        /// <summary>
        /// SquadDetailを表示する
        /// </summary>
        internal void Show(SquadCard card)
        {
            if (card == null)
                return;
            SelectedCard = card;
            this.Squad = card.Squad;

            canvasGroup.alpha = 0;
            gameObject.SetActive(true);

            canvasGroup.DOFade(1, animationTime).Play();

            SetInfo(Squad);
            StartCoroutine(unitCardList.SetSquadCard(Squad));

            // LoadLocationDropdown(squad.locationBaseName);
        }

        /*******************************************
         * 実際のSaveデータの書き換えはUnitCardListではなくこちらで行う
         * UnitDetailのValueにも結果を反映させるため
         ******************************************/
        /// <summary>
        /// SquadDetailを非表示にする
        /// </summary>
        internal void Hide()
        {
            if (!gameObject.activeSelf)
                return;
            unitCardList.SelectUnitWindow.Hide();
            canvasGroup.DOFade(0, animationTime).OnComplete(() =>
            {
                this.Squad = null;
                gameObject.SetActive(false);
            }).Play();
        }

        /// <summary>
        /// 既に表示されているDetailWindowの中身のSquadを変更する
        /// </summary>
        /// <param name="squad"></param>
        public void Change(SquadCard card)
        {
            this.Squad = card.Squad;
            this.SelectedCard = card;
            var seq = DOTween.Sequence();
            seq.Append(canvasGroup.DOFade(0, animationTime / 2).OnComplete(() =>
              {
                  SetInfo(Squad);
                  StartCoroutine(unitCardList.SetSquadCard(Squad));
              }));
            seq.Append(canvasGroup.DOFade(1, animationTime / 2));
            seq.Play();
        }
        
        /// <summary>
        /// Detailの内容を再表示
        /// </summary>
        public void UpdateInfo()
        {
            if (Squad != null)
                SetInfo(this.Squad);
        }

        /// <summary>
        /// 情報をLabelに設定する
        /// </summary>
        /// <param name="squad"></param>
        private void SetInfo(Squad squad)
        {
            if (squad.supplyLevel == squad.MaxSupply || squad.isOnMap)
                waitSupplyButton.interactable = false;
            else
                waitSupplyButton.interactable = true;

            if (squad.isOnMap)
            {
                activateButton.interactable = true;
                activateLabel.SetText("Select Squad");
            }
            else if (squad.supplyLevel > (float)squad.MaxSupply * parameter.CanActivateSupplyRate)
            {
                // SupplyLevelが十分なため出撃可能
                activateButton.interactable = true;
                activateLabel.SetText("Activate Squad");
            }
            else
            {
                activateButton.interactable = false;
            }

            squadNameLabel.SetText(squad.name);
            supplyDaysLabel.SetText(squad.DaysOfRemainingSupply);
            maxSupplyDaysLabel.SetText(squad.MaxSupplyDaysWhenWaiting);
            baseSupplyLabel.SetText(squad.BaseSupplyDaysWhenWaiting);
            additionalSupplyLabel.SetText(squad.AdditionalSupplyDaysWhenWaiting);
            //sortieCostLabel.SetText("");
            memberLabel.SetText($"{1+squad.member.Count} / {1+squad.maxMemberCount}");
            string location = "On Load";
            if (!squad.isOnMap)
                location = "In Base";
            else if (squad.MapLocation != null)
                location = squad.MapLocation.Data.Name;
            locationLabel.SetText(location);
        }

        /// <summary>
        /// UnitCardListからのUnit追加コールバック
        /// </summary>
        /// <param name="unit"></param>
        private void AddUnitCallBack(UnitData unit)
        {
            gameManager.DataSavingController.MyArmyData.ArmyController.AddUnit(unit, Squad);
            squadDetailUpdatedHandler?.Invoke(this, null);
        }

        /// <summary>
        /// UnitCardListからのUnit削除コールバック
        /// </summary>
        /// <param name="unit"></param>
        private void RemoveUnitCallBack(UnitData unit)
        {
            gameManager.DataSavingController.MyArmyData.ArmyController.RemoveUnit(unit, Squad);
            squadDetailUpdatedHandler?.Invoke(this, null);
        }

        /// <summary>
        /// UnitCardListからのUnit変更コールバック
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        private void ChangeUnitCallBack(UnitData from, UnitData to)
        {
            gameManager.DataSavingController.MyArmyData.ArmyController.ChangeUnit(from, to, Squad);
        }

        /// <summary>
        /// Supplyの完了を待つボタン
        /// </summary>
        private IEnumerator WaitToSupplyAction()
        {
            const float MaxSeconds = 10;
            var daySec = parameter.dayLengthMinute * 60;
            var speed = daySec / MaxSeconds;
            // 待機時間が4秒以上の場合WaitSupplyingPanelを表示する
            var duration = ((float)Squad.MaxSupply - Squad.supplyLevel) * daySec / (float)Squad.MaxSupply;
            duration /= speed;
            Print("Supplying duration is", duration, speed, daySec, Squad.supplyLevel, (float)Squad.MaxSupply);
            if (duration >= 4)
            {
                yield return StartCoroutine( WaitSupplyingPanel.WaitUntil(Squad, SelectedCard, speed, (o) => Math.Abs(Squad.supplyLevel - Squad.MaxSupply) < 0.0001));
            }
            else
            {
                yield return StartCoroutine(WaitSupplyingPanel.WaitUntil(Squad, speed, (o) => Math.Abs(Squad.supplyLevel - Squad.MaxSupply) < 0.0001));
            }

        }

        ///// <summary>
        ///// LocationDropdownのデータを用意する
        ///// </summary>
        //private void LoadLocationDropdown(string currentLocationID)
        //{
        //    locationsDropdown.ClearOptions();
        //    var rawLocs = GameController.Instance.data.locationsData.locationParamters.FindAll(p => p.type == LocationParamter.Type.friend);
        //    locations = rawLocs.ConvertAll(l => (l.id, l.name));

        //    print(currentLocationID);
        //    var currentLoc = GameController.Instance.data.locationsData.FindWithID(currentLocationID);

        //    if (currentLocationID.Length == 0)
        //        locations.Insert(0, ("", "On Load"));
        //    else if (currentLoc.type != LocationParamter.Type.friend)
        //        locations.Insert(0, (currentLoc.id, currentLoc.name));
        //    locationsDropdown.AddOptions(locations.ConvertAll(l => l.name));

        //    var squadLocationIndex = locations.FindIndex((l) => l.id.Equals(squad.locationBaseName)).Default(0, i => i != -1);
        //    locationsDropdown.SetValueWithoutNotify(squadLocationIndex);
        //    locationsDropdownPreviousIndex = squadLocationIndex;
        //}

        //private void LocationDropdownChanged(int index)
        //{
        //    var fastTravel = locations[index];
        //    var moveTo = GameController.Instance.data.locationsData.FindWithID(fastTravel.id);

        //    Action<MessageBox.SolidWindow, MessageBox.Result> onClick = ( (win, r) =>
        //    {
        //        if (r == MessageBox.Result.Yes)
        //        {
        //            locationsDropdownPreviousIndex = index;
        //            fastTravelRequest?.Invoke(this, squad, moveTo);
        //            LoadLocationDropdown(moveTo.id);
        //        }else
        //        {
        //            locationsDropdown.SetValueWithoutNotify(locationsDropdownPreviousIndex);
        //        }
        //    });

        //    var panelInfo = new MessageBox.WindowInput()
        //    {
        //        onClick = onClick,
        //        message = $"Move to {moveTo.name}",
        //        title = "Fast Tavel"
        //    };
        //    MessageBox.MessagePanel.Instance.ShowYesNoWindow(panelInfo);    
        //}
    }
}