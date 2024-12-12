using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using UnityEngine.UI;

namespace EventScene.Dialog.Help
{
    public class HelpDialog : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI TitleLabel;
        [SerializeField] TextMeshProUGUI DetailLabel;
        [SerializeField] CanvasGroup CanvasGroup;
        [SerializeField] RectTransform RectTransform;
        [SerializeField] float Duration = 0.3f;
        [SerializeField] float Delay = 4;

        /// <summary>
        /// Dialogを表示する位置のObject
        /// </summary>
        private HelpDialogTag ParentTag;
        /// <summary>
        /// Dialogの中身の文章
        /// </summary>
        INIParser UserInterfaceIni;
        /// <summary>
        /// HelpDialogWindowが現在表示中であるか
        /// </summary>
        public bool IsActive { private set; get; } = false;

        private bool IsAnimating = false;

        Sequence Animation;
        /// <summary>
        /// 現在Show待ちであるか
        /// </summary>
        bool ReservedToShow = false;

        internal DialogEvent DialogEvent;

        /// <summary>
        /// Buttonの位置にHelpDialogを表示する Delay後に表示
        /// </summary>
        /// <param name="button"></param>
        public IEnumerator Show(HelpDialogTag helpDialogTag)
        {
            if (ReservedToShow && helpDialogTag == ParentTag)
                yield break;

            ReservedToShow = true;
            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < Delay)
            {
                yield return null;
                if (!helpDialogTag.gameObject.activeSelf || !ReservedToShow)
                    yield break;
            }
            ReservedToShow = false;

            if (UserInterfaceIni == null)
                UserInterfaceIni = GameManager.Instance.Translation.CommonUserInterfaceIni;
            var detail = UserInterfaceIni.ReadValue("Help", helpDialogTag.Key, "");
            if (detail.Length == 0) yield break;

            DialogEvent.Show(WindowType.Help, false);

            ParentTag = helpDialogTag;
            RectTransform.position = ParentTag.DialogPosition;
            

            if (Animation != null && Animation.IsActive())
                Animation.Kill();
            Animation = DOTween.Sequence();

            TitleLabel.text = "Help";
            DetailLabel.text = detail;

            CanvasGroup.alpha = 0;
            gameObject.SetActive(true);
            Animation.Append(CanvasGroup.DOFade(1, DialogEvent.Duration));
            IsActive = true;
            Animation.Play();
        }



        /// <summary>
        /// Dialogを非表示にする
        /// </summary>
        /// <param name="onComplete"></param>
        public void Hide(Action onComplete = null)
        {
            if (!IsActive)
                return;
            
            if (ReservedToShow)
            {
                ReservedToShow = false;
                CanvasGroup.alpha = 0;
                gameObject.SetActive(false);
                DialogEvent.Hide();
            }
            else
            {
                Animation?.Kill();
                Animation = DOTween.Sequence();
                Animation.Append(CanvasGroup.DOFade(0, DialogEvent.Duration));
                Animation.OnComplete(() =>
                {
                    onComplete?.Invoke();
                    gameObject.SetActive(false);
                    DialogEvent.Hide();
                });
                Animation.Play();
            }

        }

        protected private void FixedUpdate()
        {
            if (IsActive && ParentTag != null && !ParentTag.gameObject.activeSelf)
            {
                CanvasGroup.alpha = 0;
                gameObject.SetActive(false);
                ReservedToShow = false;
                IsActive = false;
            }
        }
    }
}