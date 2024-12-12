using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using UnityEngine.UI;

namespace EventScene.Dialog
{
    public class BuySellWindow : SolidWindow
    {
        [SerializeField] private TextMeshProUGUI totalCountLabel;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI yesLabel;
        [SerializeField] private TextMeshProUGUI cancelLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] public EasyCounter counter;

        [SerializeField] int _itemValue = 0;
        public int itemValue
        {
            set
            {
                _itemValue = value;
                totalCountLabel.SetText("$" + (counter.count * value).ToString("#,0"));
            }
            get => _itemValue;
        }

        override protected private void Awake()
        {
            base.Awake();

            yesButton.onClick.AddListener(() =>
            {
                if (buttonLock) return;
                WindowResultCallback?.Invoke(Result.Yes);
                input.onClick?.Invoke(this, Result.Yes, null);
                Hide(() =>
                {
                    windowClosedCallback?.Invoke();
                    input.onHidden?.Invoke(this, Result.Yes, null);
                });
            });

            cancelButton.onClick.AddListener(() =>
            {
                if (buttonLock) return;
                WindowResultCallback?.Invoke(Result.No);
                input.onClick?.Invoke(this, Result.No, null);
                Hide(() =>
                {
                    windowClosedCallback?.Invoke();
                    input.onHidden?.Invoke(this, Result.No, null);
                });
            });

            closeButton.onClick.AddListener(() =>
            {
                if (buttonLock) return;
                WindowResultCallback?.Invoke(Result.None);
                input.onClick?.Invoke(this, Result.None, null);
                Hide(() =>
                {
                    windowClosedCallback?.Invoke();
                    input.onHidden?.Invoke(this, Result.None, null);
                });

            });

            counter.CountIsChagned += ItemCountIsChanged;
        }

        protected void Start()
        {
            totalCountLabel.SetText("$" + (counter.count * itemValue).ToString("#,0"));
        }

        /// <summary>
        /// Counterの値が変更された時の呼び出し
        /// </summary>
        /// <param name="count"></param>
        private void ItemCountIsChanged(int count)
        {
            totalCountLabel.SetText("$" + (count * itemValue).ToString("#,0"));
        }

    }
}