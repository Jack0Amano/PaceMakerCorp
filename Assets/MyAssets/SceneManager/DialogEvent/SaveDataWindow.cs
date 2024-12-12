using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using static Utility;

namespace EventScene.Dialog
{
    public class SaveDataWindow : SolidWindow
    {
        [SerializeField] Button saveButton;
        [SerializeField] TextMeshProUGUI saveLabel;

        [SerializeField] Button loadButton;
        [SerializeField] TextMeshProUGUI loadLabel;

        [SerializeField] Button cancelButton;

        override protected private void Awake()
        {
            base.Awake();

            saveButton.onClick.AddListener(() =>
            {
                if (buttonLock) return;
                WindowResultCallback?.Invoke(Result.Save);
                input.onClick?.Invoke(this, Result.Save, null);
                Hide(() => {
                    windowClosedCallback?.Invoke();
                    input.onHidden?.Invoke(this, Result.Save, null);
                });
            });

            loadButton.onClick.AddListener(() =>
            {
                if (buttonLock) return;
                WindowResultCallback?.Invoke(Result.Load);
                input.onClick?.Invoke(this, Result.Load, null);
                Hide(() => {
                    windowClosedCallback?.Invoke();
                    input.onHidden?.Invoke(this, Result.Load, null);
                });
            });

            cancelButton.onClick.AddListener(() =>
            {
                if (buttonLock) return;
                WindowResultCallback?.Invoke(Result.None);
                input.onClick?.Invoke(this, Result.None, null);
                Hide(() => {
                    windowClosedCallback?.Invoke();
                    input.onHidden?.Invoke(this, Result.None, null);
                });
            });
        }
    }
}