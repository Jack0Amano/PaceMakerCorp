using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;
using static Utility;
using System;

namespace MainMap.UI.TableIcons
{
    public class Balloon : MonoBehaviour
    {
        [SerializeField] Color normal;
        [SerializeField] Color warning;

        Sequence Sequence;

        [SerializeField] Image balloon;

        internal RectTransform RectTransform;

        protected Transform TableParentTransform;

        /// <summary>
        /// アニメーション中であるか
        /// </summary>
        internal bool IsAnimating { private set; get; } = false;
        /// <summary>
        /// Showのアニメーションをしている途中
        /// </summary>
        private bool IsOnAnimationToShow = false;

        protected void Awake()
        {
            transform.localScale = Vector3.zero;
            balloon.color = Color.clear;
            RectTransform = GetComponent<RectTransform>();
            TableParentTransform = transform.parent;
        }

        /// <summary>
        /// Balloonを表示する 
        /// </summary>
        protected void Show()
        {
            if (transform.localScale == Vector3.one)
                return;

            if (Sequence != null && Sequence.IsActive())
                Sequence.Kill();
            IsAnimating = true;
            IsOnAnimationToShow = true;
            Sequence = DOTween.Sequence();
            gameObject.SetActive(true);
            Sequence.Append(balloon.DOColor(normal, 0.4f));
            Sequence.Join(this.transform.DOScale(1, 0.4f));
            Sequence.OnComplete(() =>
            {
                IsAnimating = false;
                IsOnAnimationToShow = false;
            });
            Sequence.Play();
        }

        /// <summary>
        /// Balloonを非表示にする 
        /// </summary>
        protected void Hide(float delay = 0, Action OnComplete = null)
        {
            if (transform.localScale == Vector3.zero)
                return;

            if (Sequence != null && Sequence.IsActive())
                Sequence.Kill();
            Sequence = DOTween.Sequence();
            gameObject.SetActive(true);
            if (delay != 0 && !IsOnAnimationToShow)
                Sequence.SetDelay(delay);
            IsAnimating = true;
            Sequence.Append(balloon.DOColor(Color.clear, 0.4f));
            Sequence.Join(this.transform.DOScale(0, 0.4f));
            Sequence.OnComplete(() =>
            {
                OnComplete?.Invoke();
                IsAnimating = false;
            });
            Sequence.Play();
        }

        /// <summary>
        /// Show Hideのアニメーションをキャンセルする
        /// </summary>
        public void CancelAnimation()
        {
            if (IsOnAnimationToShow)
                return;
            if (Sequence != null && Sequence.IsActive())
                Sequence.Kill();
        }
    }
}