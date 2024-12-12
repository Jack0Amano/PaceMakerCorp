using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using System;
using static Utility;

namespace Tactics.UI
{
    /// <summary>
    /// TacticsでUnitのlistを表示する
    /// </summary>
    public class UnitListWindow : MonoBehaviour
    {
        [SerializeField] Character.UnitsController unitsController;
        [SerializeField] Lists.UnitListAdapter listAdapter;
        [SerializeField] PopupWindow popupWindow;
        [SerializeField] BackPanel backPanel;

        private float animationTime = 0.5f;
        private bool animating = false;

        public Action hideButtonAction;

        private void Start()
        {
            if (gameObject.activeSelf)
                Hide(false);

            popupWindow.HideWithUserControl += (() =>
            {
                hideButtonAction?.Invoke();
            });
        }

        public IEnumerator Show(Action OnComplete)
        {
            if (animating) yield break;

            animating = true;

            popupWindow.Show(() =>
            {
                animating = false;
                OnComplete?.Invoke();
            });

            var models = unitsController.UnitsList.ConvertAll((u) =>
            {
                return new Lists.MyListItemModel(u);
            });

            while (listAdapter.LazyData == null)
                yield return null;

            listAdapter.SetItems(models);
        }

        public void Hide(bool animation)
        {
            if (animating) return;

            animating = true;
            if (!gameObject.activeSelf)
                return;

            popupWindow.Hide(() =>
            {
                animating = false;
            });
        }


    }
}