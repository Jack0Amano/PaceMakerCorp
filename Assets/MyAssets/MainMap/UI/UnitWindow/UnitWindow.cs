using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;

namespace MainMap.UI.Squads.Detail
{
    public class UnitWindow : MonoBehaviour
    {

        [SerializeField] private UnitDetail.UnitDetail unitDetail;
        [SerializeField] private Button backgroundButton;
        [SerializeField] private Button closeButton;

        private CanvasGroup windowCanvasGroup;
        private Action callWhenClose;

        protected void Awake()
        {
            Setup();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Windowを開始状態にする
        /// </summary>
        private void Setup()
        {
            windowCanvasGroup = GetComponent<CanvasGroup>();
            windowCanvasGroup.alpha = 0;

            backgroundButton.onClick.AddListener(() => Hide());
            closeButton.onClick.AddListener(() => Hide());
        }

        /// <summary>
        /// Windowを表示状態にする
        /// </summary>
        /// <param name="parameter"></param>
        public void Show(UnitData parameter, Action onComplete)
        {
            unitDetail.SetUnitParameters(parameter);
            if (windowCanvasGroup == null)
                Setup();
            gameObject.SetActive(true);
            windowCanvasGroup.DOFade(1f, 0.3f);
            callWhenClose = onComplete;
        }

        /// <summary>
        /// Windowをボタンから閉じる
        /// </summary>
        private void Hide()
        {
            callWhenClose?.Invoke();
            windowCanvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
    }
}