using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using static Utility;

namespace MainMap.UI
{
    /// <summary>
    /// MainMapのUIにてMainMapUIWindowの上に表示する情報パネルの表示非表示を動かす基底クラス
    /// </summary>
    public class MainMapUIPanel : MonoBehaviour
    {
        protected CanvasGroup canvasGroup;

        internal readonly float duration = 0.3f;

        protected RectTransform rectTransform;
        /// <summary>
        /// MainUIWindowのCanvasGroup
        /// </summary>
        internal CanvasGroup parentWindowCanvasGroup;

        virtual protected void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
            
        }

        /// <summary>
        /// Windowを表示する
        /// </summary>
        virtual internal void Show()
        {
            gameObject.SetActive(true);

            canvasGroup.DOFade(1, duration).OnComplete(() =>
            {
                
            }).Play();
        }

        /// <summary>
        /// Windowを消す
        /// </summary>
        /// <param name="onCompletion"></param>
        virtual internal void Hide(Action onCompletion = null)
        {
            canvasGroup.DOFade(0, duration).OnComplete(() =>
            {
                onCompletion?.Invoke();
                gameObject.SetActive(false);
            }).Play();
        }
    }
}