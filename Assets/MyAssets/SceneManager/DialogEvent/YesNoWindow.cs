using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using UnityEngine.UI;

namespace EventScene.Dialog
{
    public class YesNoWindow: SolidWindow
    {
        [SerializeField] Button yesButton;
        [SerializeField] Button noButton;
        [SerializeField] TextMeshProUGUI yesLabel;
        [SerializeField] TextMeshProUGUI noLabel;
        [SerializeField] RectTransform yesRectTransform;
        [SerializeField] RectTransform noRectTransform;
        [SerializeField] Button cancelButton;

        /// <summary>
        /// ボタンの位置を逆にする
        /// </summary>
        internal bool reverseButton = false;


        override protected private void Awake()
        {
            base.Awake();

            yesButton.onClick.AddListener(() =>
            {
                if (buttonLock) return;
                WindowResultCallback?.Invoke(Result.Yes);
                input.onClick?.Invoke(this, Result.Yes, null);
                Hide(() => { 
                    windowClosedCallback?.Invoke();
                    input.onHidden?.Invoke(this, Result.Yes, null);
                });
            });

            noButton.onClick.AddListener(() =>
            {
                if (buttonLock) return;
                WindowResultCallback?.Invoke(Result.No);
                input.onClick?.Invoke(this, Result.No, null);
                Hide(() => {
                    input.onHidden?.Invoke(this, Result.No, null);
                });
            });

            cancelButton.onClick.AddListener(() =>
            {
                if (buttonLock) return;
                WindowResultCallback?.Invoke(Result.None);
                input.onClick?.Invoke(this, Result.None, null);
                Hide(() => {
                    input.onHidden?.Invoke(this, Result.None, null);
                });
            });
        }

        internal override void Show(WindowInput windowInput)
        {
            if (windowInput.reverseButton)
            {
                yesRectTransform.anchoredPosition = new Vector2(-89, 27);
                noRectTransform.anchoredPosition = new Vector2(89, 27);
            }
            else
            {
                yesRectTransform.anchoredPosition = new Vector2(89, 27);
                noRectTransform.anchoredPosition = new Vector2(-89, 27);
            }
            base.Show(windowInput);
            DialogEvent.Show(WindowType.YesNo);
        }
    }
}
