using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Tactics.Character;
using DG.Tweening;
using System;
using UnityEngine.UI;
using Tactics.Object;
using static Utility;

namespace Tactics.UI.Overlay
{
    /// <summary>
    /// OverlayCanvasでUnitがアイテムを使用中なのを表すためのPanel
    /// </summary>
    public class SelectItemPanel : MonoBehaviour
    {
        [SerializeField] RectTransform listParent;

        const float duration = 0.5f;

        Dictionary<ItemType, SelectItemCell> typeToCell = new Dictionary<ItemType, SelectItemCell>();

        private List<SelectItemCell> cells = new List<SelectItemCell>();

        // Start is called before the first frame update
        void Start()
        {
            foreach (var item in GetComponentsInChildren<SelectItemCell>().ToList())
            {
                typeToCell[item.itemType] = item;
            }

        }

        /// <summary>
        /// アイテムが使用できないなどの理由でアイテムを振動させる
        /// </summary>
        /// <param name="holder"></param>
        public void ShakeItemHolder(ItemHolder holder)
        {
            var cell = cells.Find(cells => cells.Holder == holder);
            if (cell != null)
            {
                StartCoroutine(cell.CantUseItem());
            }
        }

        /// <summary>
        /// 指定したアイテムを使用しているという状態の表示にする
        /// </summary>
        /// <param name="holder"></param>
        public void SetItemToUse(ItemHolder holder)
        {
            var selected = cells.Find(cells => cells.Holder == holder);
            if (selected != null)
            {
                cells.ForEach(c => c.IsSelected = false);
                selected.IsSelected = true;
            }
        }

        /// <summary>
        /// Unitの装備している物をすべて表示する
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public IEnumerator SetItems(UnitController unit)
        {
            listParent.DOAnchorPosY(0, duration);
            yield return new WaitForSecondsRealtime(duration);

            listParent.RemoveAllChildren();
            cells.Clear();

            var items = unit.itemController.GetAllItemHolders();
            var count = 1;
            foreach(var item in items)
            {
                SelectItemCell selectItemCell;
                if (!typeToCell.TryGetValue(item.Data.ItemType, out selectItemCell))
                {
                    selectItemCell = typeToCell[ItemType.None];
                }
                var newCell = Instantiate(selectItemCell, listParent);
                newCell.SetItem(item, count);

                newCell.IsSelected = item == unit.itemController.CurrentItemHolder;
                cells.Add(newCell);

                count++;
            }

            yield return null;
            listParent.DOAnchorPosY(listParent.rect.height, duration);
            yield return new WaitForSecondsRealtime(duration);

            if (unit.itemController.CurrentItemHolder != null && unit.itemController.CurrentItemHolder.Data != null)
            {
                SetItemToUse(unit.itemController.CurrentItemHolder);
            }
        }

        /// <summary>
        /// MortarGimmickのアイテムを表示する
        /// </summary>
        /// <param name="mortarGimmick"></param>
        /// <returns></returns>
        public IEnumerator SetItem(MortarGimmick mortarGimmick)
        {
            listParent.DOAnchorPosY(0, duration);
            yield return new WaitForSecondsRealtime(duration);

            listParent.RemoveAllChildren();
            cells.Clear();
            var mortarCell = Instantiate(typeToCell[ItemType.Mortar], listParent);
            cells.Add(mortarCell);
            mortarCell.SetGimmick(mortarGimmick);
            mortarCell.IsSelected = true;

            yield return null;
            listParent.DOAnchorPosY(listParent.rect.height, duration);
            yield return new WaitForSecondsRealtime(duration);
        }

        /// <summary>
        /// アイテムの残弾数を更新する
        /// </summary>
        public void UpdateItemsState()
        {
            cells.ForEach(cells => cells.UpdateItemState());
        }
    }
}