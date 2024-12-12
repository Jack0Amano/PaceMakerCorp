using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

namespace EventScene.Dialog
{

     public class SolidWindow : MonoBehaviour
    {
        internal Dictionary<PositionType, Vector2> positionTypes = new Dictionary<PositionType, Vector2>
        {
            { PositionType.Under, new Vector2(0.5f, 1.101785f) },
            { PositionType.Center, new Vector2(0.5f, 0.5f)}
        };

        [SerializeField] internal TextMeshProUGUI title;
        [SerializeField] internal TextMeshProUGUI message;
        [SerializeField] internal RectTransform rectTransform;
        [SerializeField] internal CanvasGroup canvasGroup;

        internal bool buttonLock = false;


        /// <summary>
        /// 各DialogWindowのUser戻り値
        /// </summary>
        internal Action<Result> WindowResultCallback;

        internal Action windowClosedCallback;

        internal DialogEvent DialogEvent;

        virtual internal WindowInput input
        {
            
            set
            {
                if (title != null)
                    title.text = value.title != null ? value.title : "";
                if (message != null)
                    message.text = value.message != null ? value.message : "";
                rectTransform.pivot = positionTypes[value.positionType];
                rectTransform.anchoredPosition = value.position;
                _input = value;
            }
            get => _input;
        }
        private WindowInput _input;

        virtual protected private void Awake()
        {
        }

        virtual internal void Show(WindowInput windowInput)
        {
            input = windowInput;
            canvasGroup.alpha = 0;
            gameObject.SetActive(true);
            buttonLock = true;
            canvasGroup.DOFade(1, DialogEvent.Duration).OnComplete(() =>
            {
                buttonLock = false;
            }).Play();
        }

        /// <summary>
        /// MessageWindowをフェードアウトさせてInactiveにする
        /// </summary>
        /// <param name="onComplete"></param>
        virtual internal void Hide(Action onComplete)
        {
            if (buttonLock) return;
            DialogEvent.Hide();
            canvasGroup.DOFade(0, DialogEvent.Duration).OnComplete(() =>
            {
                input.onHidden?.Invoke(this, Result.None, null);
                onComplete?.Invoke();
                gameObject.SetActive(false);
            }).Play();
        }
    }

    /// <summary>
    /// MessageBoxの戻り値
    /// </summary>
    public enum Result
    {
        None,
        Yes,
        No,
        Save,
        Delete,
        Load,
    }

    /// <summary>
    /// 各MessageWindowのInput classの基底クラス
    /// </summary>
    public class WindowInput
    {
        internal WindowType windowType = WindowType.None;
        public string title = "";
        public string message = "";
        public Vector2 position = Vector2.zero;
        public Action<SolidWindow, Result, object> onClick = null;
        /// <summary>
        /// Dialogが閉じられるときの呼び出し
        /// </summary>
        public Action<SolidWindow, Result, object> onHidden = null;
        public PositionType positionType = PositionType.Center;
        public EaseType easeType = EaseType.None;
        public bool reverseButton = false;
        public float autoEaseDuration = 0f;

        public WindowInput()
        {
        }

        public WindowInput(string title, string message, Vector2 position)
        {
            this.title = title;
            this.message = message;
            this.position = position;
        }
    }
}