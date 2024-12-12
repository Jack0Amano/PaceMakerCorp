using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;
using TMPro;
using static Utility;


namespace MainMap.UI.SelectUnit
{
    public class SelectUnitWindow : MonoBehaviour
    {
        [Header("リスト")]
        [Tooltip("リスト表示用")]
        [SerializeField] SelectUnitListAdapter listAdapter;
        [SerializeField] Tabbar tabbar;
        [SerializeField] Image verticalLine;
        [Header("詳細")]
        [Tooltip("Unitの詳細パネル")]
        [SerializeField] UnitDetail.UnitDetail unitDetail;

        [Header("Window")]
        [SerializeField] RectTransform windowRectTransform;
        [SerializeField] Button backPanelButton;
        [SerializeField] Button closeButton;
        [SerializeField] float MaxWindowWidth = 1770;
        [SerializeField] float MinWindowWidth = 885;

        private Action<UnitData> calledWhenAddUnit;
        /// <summary>
        /// Windowが閉じられる場合呼び出し
        /// </summary>
        internal Action calledWhenHide;

        CanvasGroup canvasGroup;

        CanvasGroup listAdapterCanvasGroup;

        private MyArmyData MyArmyData
        {
            get => GameManager.Instance.DataSavingController.MyArmyData;
        }

        private List<UnitType.Type> typesOnTab;

        /// <summary>
        /// UnitDetailのみ表示するタイプ
        /// </summary>
        public bool IsUnitDetailMode { private set; get; } = false;

        /// <summary>
        /// 表示時に選択されている兵種
        /// </summary>
        public UnitType.Type UnitType { private set; get; } = global::UnitType.Type.Infantry;
        /// <summary>
        /// 現在表示されているUnitsは指揮官のみか
        /// </summary>
        public bool IsCommanderOnly { private set; get; } = false;

        public bool IsAnimating { private set; get; } = false;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            
            backPanelButton.onClick.AddListener(() => Hide());
            closeButton.onClick.AddListener(() => Hide());

            listAdapter.viewsHolderOnClick += SelectTableCell;
            listAdapter.setUnitAction += SetUnit;
            tabbar.tabButtonClicked += SelectClassTab;
            listAdapterCanvasGroup = listAdapter.GetComponent<CanvasGroup>();

            unitDetail.itemList.OpenSelectItemWindowAction = OpenSelectItemWindow;
            unitDetail.itemList.CloseSelectItemWindowAction = CloseSelectItemWindow;
        }

        // Start is called before the first frame update
        void Start()
        {
            listAdapter.Init();
            canvasGroup.alpha = 0;
            listAdapterCanvasGroup.alpha = 0;
            gameObject.SetActive(false);

        }

        /// <summary>
        /// UnitDataのみ表示するWindowタイプ
        /// </summary>
        /// <param name="unitData"></param>
        public void Show(UnitData unitData)
        {
            if (IsAnimating || gameObject.activeSelf) return;

            tabbar.Hide(false);
            listAdapterCanvasGroup.alpha = 0;
            listAdapter.gameObject.SetActive(false);
            verticalLine.color = Color.clear;

            unitDetail.Show(unitData);
            IsUnitDetailMode = true;
            gameObject.SetActive(true);
            IsAnimating = true;
            canvasGroup.DOFade(1, 0.3f).OnComplete(() => IsAnimating = false);
            windowRectTransform.sizeDelta = new Vector2(MinWindowWidth, windowRectTransform.sizeDelta.y);
        }

        /// <summary>
        ///  UnitSelectionWindowを表示する
        /// </summary>
        /// <param name="startType">表示時に表示しておく兵種</param>
        /// <param name="unitTypes">表示するすべてのタブ</param>
        /// <param name="calledWhenAddUnit">UnitSelectionが非表示になる際に呼び出されるラムダ式</param>
        public void Show(HashSet<UnitType.Type> unitTypes, UnitType.Type startType, Action<UnitData> calledWhenAddUnit, bool commanderOnly = false)
        {
            if (IsAnimating || gameObject.activeSelf) return;

            this.IsCommanderOnly = commanderOnly;
            gameObject.SetActive(true);
            IsAnimating = true;
            canvasGroup.DOFade(1, 0.3f).OnComplete(() => IsAnimating = false) ;

            IsUnitDetailMode = false;

            tabbar.Show(false);
            listAdapterCanvasGroup.alpha = 1;
            listAdapterCanvasGroup.gameObject.SetActive(true);
            verticalLine.color = Color.white;
            windowRectTransform.sizeDelta = new Vector2(MaxWindowWidth, windowRectTransform.sizeDelta.y);

            typesOnTab = unitTypes.ToList();
            tabbar.SetTabs(unitTypes.ToList().ConvertAll(t => t.ToString()));
            var firstTabIndex = unitTypes.ToList().FindIndex(t => t == startType);
            tabbar.Index = firstTabIndex;

            this.UnitType = startType;
            this.calledWhenAddUnit = calledWhenAddUnit;

            unitDetail.Hide(false);

            ShowUnits(UnitType, IsCommanderOnly);
        }

        /// <summary>
        /// SelectUnitWindowを閉じる
        /// </summary>
        public void Hide()
        {
            if (IsAnimating || !gameObject.activeSelf) return;

            if (!unitDetail.itemList.selectItemWindow.IsAnimating && unitDetail.itemList.selectItemWindow.gameObject.activeSelf)
            {
                unitDetail.itemList.selectItemWindow.Hide();
            }

            var seq = DOTween.Sequence()
                .Append(canvasGroup.DOFade(0, 0.3f))
                .OnComplete(() =>
                {
                    unitDetail.Hide();
                    gameObject.SetActive(false);
                    IsAnimating = false;
                });

            IsAnimating = true;
            seq.Play();
            calledWhenHide?.Invoke();
        }

        /// <summary>
        /// ListのUnitが選択されたときの呼び出し Detailを表示
        /// </summary>
        /// <param name="cell"></param>
        private void SelectTableCell(object o, int index)
        {
            if (listAdapter.Data.ToList().IndexAt_Bug(index, out var model))
                if (model.IsSelected)
                    unitDetail.Show(model.data);
                else
                    unitDetail.Hide();
        }

        /// <summary>
        /// Unitの追加ボタンが押された際の処理
        /// </summary>
        /// <param name="cell"></param>
        private void SetUnit(int index)
        {
            if (listAdapter.Data.ToList().IndexAt_Bug(index, out var model))
                calledWhenAddUnit(model.data);
            Hide();
        }

        /// <summary>
        /// 兵種変更タブの呼び出し
        /// </summary>
        /// <param name="tab"></param>
        private void SelectClassTab(object o, int index)
        {
            UnitType = typesOnTab[index];
            ShowUnits(UnitType, IsCommanderOnly);
        }

        /// <summary>
        /// 指定兵種のUnitを表示する
        /// </summary>
        /// <param name="type"></param>
        private void ShowUnits(UnitType.Type type, bool isCommanderOnly)
        {
            var data = MyArmyData.FreeCommanders.FindAll(u => u.UnitType == type);
            if (!isCommanderOnly)
                data.AddRange(MyArmyData.FreeUnits.FindAll(u => u.UnitType == type));
            var models = data.ConvertAll(d =>
            {
                print(d);
                return new MyListUnitModel(d);
            });
            listAdapter.SetItems(models);
        }

        /// <summary>
        /// UnitDetailからSelectItemWindowが開かれたときの呼び出し
        /// </summary>
        private void OpenSelectItemWindow()
        {
            canvasGroup.DOFade(0.3f, 0.3f);
        }

        /// <summary>
        /// UnitDetailから開かれているSelectItemWindowが閉じられたときの呼び出し
        /// </summary>
        private void CloseSelectItemWindow(UnitData unitData)
        {
            canvasGroup.DOFade(1, 0.3f);
        }
    }

}
