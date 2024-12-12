using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EventScene.Message
{
    /// <summary>
    /// レスポンスボタンをのためのObject
    /// </summary>
    public class MessageResponse : MonoBehaviour
    {
        [SerializeField] internal Button button;
        [SerializeField] internal TextMeshProUGUI message;
        [SerializeField] CanvasGroup canvasGroup;
        /// <summary>
        /// ボタンの識別Index
        /// </summary>
        internal int index = -1;

        private float duration = 0.3f;

        public bool IsActive { private set; get; } = false;


        protected private void Awake()
        {
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        internal void Show(string text = "")
        {
            IsActive = true;
            gameObject.SetActive(true);
            canvasGroup.DOFade(1, duration);
            if (text.Length != 0 || message != null)
                message.SetText(text);
        }

        internal void Hide()
        {
            if (!gameObject.activeSelf)
                return;
            IsActive = false;
            canvasGroup.DOFade(0, duration).OnComplete(() => gameObject.SetActive(false));
        }
    }
}