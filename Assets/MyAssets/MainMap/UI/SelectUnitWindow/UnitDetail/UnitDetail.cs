using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using static Utility;
using MainMap.UI.SelectItem;

namespace UnitDetail
{
    public class UnitDetail : MonoBehaviour
    {
        

        [Header("Properties")]
        [SerializeField] TextMeshProUGUI unitName;
        [SerializeField] TextMeshProUGUI className;
        [SerializeField] Image classImage;
        [SerializeField] TextMeshProUGUI levelText;
        [SerializeField] ProgressBar levelProgressBar;
        [SerializeField] TextMeshProUGUI expText;
        [SerializeField] TextMeshProUGUI hpText;
        [SerializeField] TextMeshProUGUI additionalHPText;
        [SerializeField] TextMeshProUGUI energyText;
        [SerializeField] TextMeshProUGUI additionalEnergyText;
        [SerializeField] TextMeshProUGUI attackText;
        [SerializeField] TextMeshProUGUI additionalAttackText;
        [Header("Items")]
        [SerializeField] public ItemLists itemList;
        

        internal CanvasGroup canvasGroup;

        public UnitData unitData { private get; set; }

        private bool IsAnimating = false;
        GeneralParameter GeneralParameter;
        SceneParameter SceneParameter;


        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// UnitDetailWindowを表示する
        /// </summary>
        /// <param name="p"></param>
        public void Show(UnitData p)
        {
            canvasGroup.alpha = 0;
            gameObject.SetActive(true);
            SetUnitParameters(p);
            IsAnimating = true;
            canvasGroup.DOFade(1, 0.3f).OnComplete(() => IsAnimating = false);
        }

        /// <summary>
        /// 詳細表示パネルにUnitの情報を登録する
        /// </summary>
        /// <param name="data"></param>
        public void SetUnitParameters(UnitData data)
        {
            if (GeneralParameter == null)
            {
                GeneralParameter = GameManager.Instance.GeneralParameter;
                SceneParameter = GameManager.Instance.SceneParameter;
            }

            unitName.SetText(data.Name);
            className.SetText(data.UnitTypeStr);

            levelText.SetText(data.Level);
            var requiredExp = data.RequiredExp(data.Level);
            levelProgressBar.rate = (float)data.Exp / (float)requiredExp;

            expText.SetText($"{data.Exp}/{requiredExp}");

            hpText.SetText(data.HealthPoint);
            additionalHPText.SetText(data.AdditionalHealthPoint == 0 ? "" : data.AdditionalHealthPoint.ToString());

            energyText.SetText(data.BaseSupply);
            additionalEnergyText.SetText(data.AdditionalSupply == 0 ? "" : data.AdditionalSupply.ToString());

            // TODO AttackPointの設定は武器によって異なるためそのUIを
            // attackText.SetText(p.attackPoint);
            additionalAttackText.SetText("");

            unitData = data;

            var itemHolderLevel =  SceneParameter.GetItemHolderLevel(data);
            if (!itemHolderLevel.Match(data))
            {
                // Holderの数が0の場合おかしいので GeneralParameterから正常なItemHolderLevelを取得して適用
                data.SetItemHolderLevel(itemHolderLevel);

            }

            itemList.SetEquipments(data);
        }

        /// <summary>
        /// UnitDetailWindowを非表示にする
        /// </summary>
        public void Hide(bool animation = true)
        {
            if (!gameObject.activeSelf) return;
            if (IsAnimating) return;
            // selectItemWindow.Hide(null, animation);
            IsAnimating = true;
            canvasGroup.DOFade(0, animation ? 0.3f : 0).OnComplete(() =>
            {
                IsAnimating = false;
                gameObject.SetActive(false);
            }).Play();
        }

        /// <summary>
        /// 装備の変更ボタン Inspectorから呼び出し
        /// </summary>
        /// <param name="index">呼び出しボタンのINDEX</param>
        private void OpenSelectItemWindow(HolderType type, int index)
        {
        }
    }
}