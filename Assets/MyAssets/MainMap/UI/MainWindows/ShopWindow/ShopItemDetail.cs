using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System;

namespace MainMap.UI.Shop
{
    public class ShopItemDetail : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI itemName;
        [SerializeField] ItemDetailValues ItemDetailValues;

        [SerializeField] GameObject buyTagObj;
        [SerializeField] TextMeshProUGUI originalBuyCostLabel;
        [SerializeField] TextMeshProUGUI buyCostLabel;
        [SerializeField] TextMeshProUGUI saveCostLabel;
        [SerializeField] Button buyButton;

        [SerializeField] GameObject sellTagObj;
        [SerializeField] TextMeshProUGUI originalSellCostLabel;
        [SerializeField] TextMeshProUGUI sellCostLabel;
        [SerializeField] TextMeshProUGUI addCostLabel;
        [SerializeField] Button sellButton;

        private CanvasGroup canvasGroup;
        /// <summary>
        /// アイテムの実売価格
        /// </summary>
        private int itemBuyValue;
        /// <summary>
        /// アイテムの実際の買取価格
        /// </summary>
        private int itemSellValue;
        internal ItemInList item { private set; get; }
        /// <summary>
        /// Tradeによって変更されたItem所持数や所持金の変化を更新するためShopWindowにActionを送る
        /// </summary>
        internal Action tradeIsCompleted;
        /// <summary>
        /// 大本のWindowのCanvasGroup (ポップアップ等を出したときに薄くするため)
        /// </summary>
        internal CanvasGroup parentWindowCanvasGroup;

        // Start is called before the first frame update
        protected private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            buyButton.onClick.AddListener(() => BuyAction());
            // sellButton.onClick.AddListener(() => SellAction());
        }

        /// <summary>
        /// アイテムの売却画面を表示する
        /// </summary>
        /// <param name="item"></param>
        internal void ShowSellItem(ItemInList item)
        {
            this.item = item;
            itemName.text = item.data.Name;
            ItemDetailValues.SetItemData(item.data);

            sellTagObj.SetActive(true);
            buyTagObj.SetActive(false);
            originalSellCostLabel.SetText($"${item.data.ResellCost}");
            // TODO: 買取の際の追加の価格UPを計算
            itemSellValue = item.data.ResellCost;
            sellCostLabel.SetText($"${item.data.ResellCost}");

            canvasGroup.DOFade(1, 0.5f);
        }

        /// <summary>
        ///  アイテムの購入画面を表示する
        /// </summary>
        /// <param name="item"></param>
        internal void ShowBuyItem(ItemInList item)
        {
            this.item = item;
            itemName.text = item.data.Name;
            ItemDetailValues.SetItemData(item.data);

            buyTagObj.SetActive(true);
            sellTagObj.SetActive(false);
            originalBuyCostLabel.SetText($"${item.cost}");
            var discount = item.data.DiscountCost;
            saveCostLabel.SetText($"Save: ${discount}");
            itemBuyValue = item.data.BuyCost - discount;
            buyCostLabel.SetText($"${itemBuyValue}");

            canvasGroup.DOFade(1, 0.5f);
        }

        private void SetTextIfNeeded(object text, TextMeshProUGUI label, GameObject labelObject, string easeValue = "0")
        {
            if (text == null)
            {
                labelObject.SetActive(false);
                return;
            }
            var txt = text.ToString();
            if (txt.Equals(easeValue) )
            {
                labelObject.SetActive(false);
            }
            else
            {
                labelObject.SetActive(true);
                label.text = txt;
            }
        }

        internal void Hide()
        {
            if (canvasGroup.alpha == 0) return;
            canvasGroup.DOFade(0, 0.5f);
        }

        /// <summary>
        /// 購入ボダンを押した際の処理
        /// </summary>
        private void BuyAction()
        {
            GameManager.Instance.DataSavingController.SaveDataInfo.capitalFund -= itemBuyValue;
            GameManager.Instance.DataSavingController.MyArmyData.ItemController.AddItem(item.data.ID);
            tradeIsCompleted?.Invoke();
        }

        ///// <summary>
        ///// 購入ボタンを押した際の処理 (メッセージで購入数を聞くタイプ、廃止済み)
        ///// </summary>
        //private void BuyActionWithMessage()
        //{
        //    var ableToBuyCount = GameController.Instance.data.saveDataInfo.capitalFund / itemBuyValue;

        //    parentWindowCanvasGroup.DOFade(0.3f, 0.3f);

        //    var input = new MessageBox.WindowInput();
        //    input.onClick += _BuyAction;
        //    input.message = "Buy";
        //    var message = MessageBox.DialogEvent.Instance.ShowBuySellWindow(input);
        //    message.itemValue = itemBuyValue;
        //    message.counter.counterRange = new RangeAttribute(1, ableToBuyCount);
        //}

        ///// <summary>
        ///// 購入ボタンを決定した時の処理
        ///// </summary>
        ///// <param name="win"></param>
        ///// <param name="result"></param>
        //private void _BuyAction(MessageBox.SolidWindow win, MessageBox.Result result, object o)
        //{
        //    parentWindowCanvasGroup.DOFade(1f, 0.3f);

        //    if (result != MessageBox.Result.Yes)
        //        return;
        //    var window = (MessageBox.BuySellWindow)win;

        //    var itemCount = window.counter.count;
        //    GameController.Instance.data.saveDataInfo.capitalFund -= itemCount * itemBuyValue;
        //    GameController.Instance.data.myArmyData.equipController.AddEquipment(item.data.ID, itemCount);
        //    tradeIsCompleted?.Invoke();
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        //private void SellAction()
        //{
        //    var ableToSellCount = item.own.freeCount;

        //    parentWindowCanvasGroup.DOFade(0.3f, 0.3f);

        //    var input = new MessageBox.WindowInput();
        //    input.onClick += _SellAction;
        //    input.message = "Sell";
        //    var message = MessageBox.DialogEvent.Instance.ShowBuySellWindow(input);
        //    message.itemValue = itemSellValue;
        //    message.counter.counterRange = new RangeAttribute(1, ableToSellCount);
            
        //}

        //private void _SellAction(MessageBox.SolidWindow win, MessageBox.Result result, object o)
        //{
        //    parentWindowCanvasGroup.DOFade(1f, 0.3f);

        //    if (result != MessageBox.Result.Yes)
        //        return;
        //    var window = (MessageBox.BuySellWindow)win;

        //    var itemCount = window.counter.count;
        //    GameController.Instance.data.saveDataInfo.capitalFund += itemCount * itemSellValue;
        //    GameController.Instance.data.myArmyData.equipController.DeleteEquipment(item.data.ID, itemCount);
        //    tradeIsCompleted?.Invoke();
        //}
    }
}