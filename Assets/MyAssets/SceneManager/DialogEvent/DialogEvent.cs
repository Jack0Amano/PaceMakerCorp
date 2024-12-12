using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using static Utility;

namespace EventScene.Dialog
{
    public class DialogEvent : MonoBehaviour
    {
        [Header("Dialog Windows")]
        [SerializeField] internal SolidWindow solidWindow;
        [SerializeField] public YesNoWindow YesNoWindow;
        [SerializeField] internal SaveDataWindow saveDataWindow;
        [SerializeField] internal BuySellWindow buySellWindow;
        [SerializeField] internal SelectMapWindow selectMapWindow;
        [SerializeField] internal Help.HelpDialog HelpDialog;

        [Header("Background panel")]
        [SerializeField] Image backgroundImage;
        [SerializeField] Button backgroundCloseButton;

        private EaseType currentEaseType;

        /// <summary>
        /// 現在表示中のWindowのタイプ
        /// </summary>
        public WindowType WindowType { internal set; get; } = WindowType.None;

        private Color backgroundColor;

        internal readonly float Duration = 0.3f;

        /// <summary>
        /// ダイアログでのイベント中か
        /// </summary>
        public bool IsDialogEventActive { private set; get; } = false;

        protected private void Awake()
        {
            backgroundCloseButton.onClick.AddListener(() => BackPanelButtonAction());

            solidWindow.DialogEvent = this;
            YesNoWindow.DialogEvent = this;
            saveDataWindow.DialogEvent = this;
            buySellWindow.DialogEvent = this;
            selectMapWindow.DialogEvent = this;
            HelpDialog.DialogEvent = this;

            gameObject.SetActive(false);
            backgroundColor = backgroundImage.color;
            backgroundImage.color = Color.clear;
        }

        private void Start()
        {
        }

        /// <summary>
        /// SolidWindowを表示する
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="easeType"></param>
        //public SolidWindow ShowSolidWindow(WindowInput input)
        //{
        //    WindowType = WindowType.Solid;
        //    currentEaseType = input.easeType;

        //    gameObject.SetActive(true);
        //    backgroundImage.DOColor(backgroundColor, animationDuration);
        //    solidWindow.input = input;
        //    solidWindow.Show();
        //    IsDialogEventActive = true;

        //    solidWindow.windowResultCallback = ((r) =>
        //    {
        //        IsDialogEventActive = false;
        //    });

        //    return solidWindow;
        //}

        //public SelectMapWindow ShowSelectMapWindow(WindowInput input)
        //{
        //    WindowType = WindowType.Map;
        //    currentEaseType = input.easeType;

        //    gameObject.SetActive(true);
        //    backgroundImage.DOColor(backgroundColor, animationDuration);
        //    selectMapWindow.input = input;
        //    selectMapWindow.Show();
        //    IsDialogEventActive = true;

        //    selectMapWindow.windowResultCallback = ((r) =>
        //    {
        //        backgroundImage.DOColor(Color.clear, animationDuration);
        //    });
        //    selectMapWindow.windowClosedCallback = (() =>
        //    {
        //        gameObject.SetActive(false);
        //        IsDialogEventActive = false;
        //    });


        //    return selectMapWindow;
        //}

        /// <summary>
        /// Save.Load.Deleteボタンを持つWindowを表示する
        /// </summary>
        //public SaveDataWindow ShowSaveDataWindow(WindowInput input)
        //{
        //    WindowType = WindowType.SaveData;
        //    currentEaseType = input.easeType;

        //    gameObject.SetActive(true);
        //    backgroundImage.DOColor(backgroundColor, animationDuration);
        //    saveDataWindow.input = input;
        //    saveDataWindow.Show();
        //    IsDialogEventActive = true;

        //    saveDataWindow.windowResultCallback = ((r) =>
        //    {
        //        backgroundImage.DOColor(Color.clear, animationDuration);
        //    });

        //    saveDataWindow.windowClosedCallback = (() =>
        //    {
        //        gameObject.SetActive(false);
        //        IsDialogEventActive = false;
        //    });

        //    return saveDataWindow;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        //public BuySellWindow ShowBuySellWindow(WindowInput input)
        //{
        //    WindowType = WindowType.BuySell;
        //    currentEaseType = input.easeType;

        //    gameObject.SetActive(true);
        //    backgroundImage.DOColor(backgroundColor, animationDuration);
        //    buySellWindow.input = input;
        //    buySellWindow.Show();
        //    IsDialogEventActive = true;

        //    buySellWindow.windowResultCallback = ((r) =>
        //    {
        //        backgroundImage.DOColor(Color.clear, animationDuration);
        //    });

        //    buySellWindow.windowClosedCallback = (() =>
        //    {
        //        gameObject.SetActive(false);
        //        IsDialogEventActive = false;
        //    });

        //    return buySellWindow;
        //}

        /// <summary>
        /// Dialogの親を表示状態にする
        /// </summary>
        internal void Show(WindowType windowType, bool isBackgroundActive = true)
        {
            gameObject.SetActive(true);
            if (isBackgroundActive)
            {
                backgroundImage.gameObject.SetActive(true);
                backgroundImage.DOColor(backgroundColor, Duration);
            }
            else
            {
                backgroundImage.gameObject.SetActive(false);
            }
            
            WindowType = windowType;
            IsDialogEventActive = true;
        }

        /// <summary>
        /// Dialogを非表示にする
        /// </summary>
        internal void Hide()
        {
            if (backgroundImage.gameObject.activeSelf)
            {
                backgroundImage.DOColor(Color.clear, Duration).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    IsDialogEventActive = false;
                    WindowType = WindowType.None;
                });
            }
            else
            {
                gameObject.SetActive(false);
                IsDialogEventActive = false;
                WindowType = WindowType.None;
            }
        }

        /// <summary>
        /// バックパネルをクリックした際の処理
        /// </summary>
        private void BackPanelButtonAction()
        {
            if (currentEaseType != EaseType.Click) return;

            if (WindowType == WindowType.Solid)
            {
                if (solidWindow.buttonLock) return;
                backgroundImage.DOColor(Color.clear, Duration);
                solidWindow.input.onClick(solidWindow, Result.None, null);
                solidWindow.Hide(() => {
                    solidWindow.input.onHidden?.Invoke(solidWindow, Result.None, null);
                    gameObject.SetActive(false);
                    IsDialogEventActive = false;
                });
            }
            else if (WindowType == WindowType.YesNo)
            {
                if (YesNoWindow.buttonLock) return;
                backgroundImage.DOColor(Color.clear, Duration);
                YesNoWindow.input.onClick?.Invoke(YesNoWindow, Result.None, null);
                YesNoWindow.Hide(() => {
                    solidWindow.input.onHidden?.Invoke(YesNoWindow, Result.None, null);
                    gameObject.SetActive(false);
                    IsDialogEventActive = false;
                });
            }

            WindowType = WindowType.None;
        }
    }

    public enum EaseType
    {
        None,
        Move,
        Click
    }

    public enum PositionType
    {
        Under,
        Center
    }

    /// <summary>
    /// 現在表示中のWIndowのタイプ
    /// </summary>
    public enum WindowType
    {
        None,
        Solid,
        YesNo,
        SaveData,
        BuySell,
        Map,
        Help
    }
}
