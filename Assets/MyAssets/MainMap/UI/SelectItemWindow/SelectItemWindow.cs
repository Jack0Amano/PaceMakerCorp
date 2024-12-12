using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Utility;
using TMPro;

namespace MainMap.UI.SelectItem
{
    /// <summary>
    /// Tactics画面のアイテム選択など
    /// </summary>
    public class SelectItemWindow : MonoBehaviour
    {
        
        [SerializeField] SelectItemListAdapter ListAdapter;
        [SerializeField] Item.ItemDetail ItemDetail;
        [SerializeField] TextMeshProUGUI NoItemsLabel;
        [Header("Window")]
        [SerializeField] Tabbar tabbar;
        [SerializeField] Button backPanelButton;
        [SerializeField] Button closeButton;

        public HolderType holderType { private get; set; }
        /// <summary>
        /// アイテム選択時にアイテムIDを渡して呼出す 選択されなかったときはcount==0
        /// </summary>
        private Action<string> setItemAction;
        private List<ItemData> listRawData;
        private bool showAllItems = false;
        /// <summary>
        /// Windowが表示などのアニメーション中
        /// </summary>
        public bool IsAnimating { private set; get; } = false;

        private CanvasGroup canvasGroup;

        private Color NoItemsLabelColor;

        protected private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            tabbar.tabButtonClicked += ChangeTab;
            ListAdapter.viewsHolderOnClick += SelectTableCell;
            ListAdapter.setItemAction += SetItem;
            backPanelButton.onClick.AddListener(() => Hide());
            closeButton.onClick.AddListener(() => Hide());
            NoItemsLabelColor = NoItemsLabel.color;
        }

        protected private void Start()
        {
            canvasGroup.DOFade(0f, 0.05f).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }

        /// <summary>
        /// HolderTypeに装備可能な所持Itemを表示する
        /// </summary>
        /// <param name="holderType"></param>
        /// <param name="completion"></param>
        public void Show(HolderType holderType, Action<string> completion)
        {
            if (IsAnimating || gameObject.activeSelf) return;

            showAllItems = false;
            this.setItemAction = completion;
            this.holderType = holderType;

            IsAnimating = true;
            gameObject.SetActive(true);
            canvasGroup.DOFade(1, 0.3f).OnComplete(() => IsAnimating = false);

            // タブの作成
            var itemTypes = HolderAndItem.Match[holderType];
            tabbar.SetTabs(itemTypes.ConvertAll(t => t.ToString()));

            // TableCellのセット
            ShowItems(itemTypes[tabbar.Index]);
        }

        /// <summary>
        /// HolderTypeに装備可能なすべてのItemを表示する Debug用
        /// </summary>
        /// <param name="holderType"></param>
        /// <param name="completion"></param>
        public void ShowAllItems(HolderType holderType, Action<string> completion)
        {
            Print(FuncName(), holderType, completion);
            if (IsAnimating || gameObject.activeSelf) return;

            showAllItems = true;
            this.setItemAction = completion;
            this.holderType = holderType;

            IsAnimating = true;
            gameObject.SetActive(true);
            canvasGroup.DOFade(1, 0.3f).OnComplete(() => IsAnimating = false);

            // タブの作成
            var itemTypes = HolderAndItem.Match[holderType];
            tabbar.SetTabs(itemTypes.ConvertAll(t => t.ToString()));
            ShowAllItems(itemTypes[tabbar.Index]);
        }

        /// <summary>
        /// タブボタンで装備の種類変更する際の呼び出し
        /// </summary>
        /// <param name="index"></param>
        private void ChangeTab(object o, int index)
        {
            var equipType = HolderAndItem.Match[holderType][index];
            if (showAllItems)
                ShowAllItems(equipType);
            else 
                ShowItems(equipType);
        }

        /// <summary>
        /// 装備可能なアイテムを表示する
        /// </summary>
        /// <param name="type"></param>
        private void ShowItems(ItemType type)
        {
            var ownItems = GameManager.Instance.DataSavingController.MyArmyData.OwnItems;
            var data = ownItems.FindAll(e => e.ItemData != null && e.ItemData.ItemType == type);
            if (data.Count != 0)
            {
                ListAdapter.IsShown = true;

                if (NoItemsLabel.color != Color.clear)
                    NoItemsLabel.DOColor(Color.clear, 0.3f);

                listRawData = new List<ItemData>();
                var models = data.ConvertAll((d) =>
                {
                    listRawData.Add(d.ItemData);
                    return new MyListItemModel()
                    {
                        itemName = d.ItemData.Name,
                        attackValue = d.ItemData.Attack.ToString(),
                        defenceValue = d.ItemData.Defence.ToString(),
                        rangeValue = d.ItemData.Range.ToString(),
                        supplyValue = d.ItemData.Supply.ToString(),
                        //ownCount = $"{d.freeCount}/{d.totalCount}",
                        targetValue = d.ItemData.TargetType.ToString(),
                    };
                });
                ListAdapter.SetItems(models);
            }
            else
            {
                ListAdapter.IsShown = false;
                if (NoItemsLabel.color == Color.clear)
                    NoItemsLabel.DOColor(NoItemsLabelColor, 0.3f);
            }
        }

        /// <summary>
        /// すべてのEquipmentを表示する Debug用
        /// </summary>
        /// <param name="type"></param>
        private void ShowAllItems(ItemType type)
        {
            listRawData = GameManager.Instance.StaticData.AllItemsList.Items.FindAll(e => e.ItemType == type);
            var models = listRawData.ConvertAll((d) =>
            {
                return new MyListItemModel()
                {
                    itemName = d.Name,
                    attackValue = d.Attack.ToString(),
                    defenceValue = d.Defence.ToString(),
                    rangeValue = d.Range.ToString(),
                    supplyValue = d.Supply.ToString(),
                    //ownCount = "xxx/xxx",
                    targetValue = d.TargetType.ToString(),
                };
            });
            ListAdapter.SetItems(models); 
        }

        /// <summary>
        /// ListのItemが選択されたとき呼び出し
        /// </summary>
        /// <param name="o"></param>
        /// <param name="index"></param>
        private void SelectTableCell(object o, int index)
        {
            if (index != -1)
                ItemDetail.Show(listRawData[index]);
            else
                ItemDetail.Hide();
        }

        /// <summary>
        /// アイテムを装備する
        /// </summary>
        /// <param name="index"></param>
        private void SetItem(int index)
        {
            if (!Hide()) return;
            setItemAction?.Invoke(listRawData[index].ID);
        }

        /// <summary>
        /// Windowを閉じる
        /// </summary>
        /// <returns>正常に閉じられた場合true</returns>
        public bool Hide()
        {
            if (IsAnimating || !gameObject.activeSelf) return false;

            IsAnimating = true;
            ListAdapter.Deselect();
            ItemDetail.Hide();
            canvasGroup.DOFade(0, 0.3f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                IsAnimating = false;
            });
            setItemAction?.Invoke("");
            return true;
        }
    }
}