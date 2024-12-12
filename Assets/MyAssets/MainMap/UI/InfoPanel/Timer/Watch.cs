using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using static Utility;

namespace MapUI.Calender
{
    public class Watch : MonoBehaviour
    {
        [SerializeField] RectTransform minuteHand;
        [SerializeField] RectTransform hourHand;

        private Sequence sequence;

        /// <summary>
        /// 時計のアニメーションを停止するかどうか
        /// </summary>
        internal bool IsPause
        {
            set
            {
                if (value)
                {
                    if (sequence != null && sequence.IsPlaying())
                        sequence.Pause();
                }
                else
                {
                    if (sequence != null)
                        sequence.Play();
                }
                _IsPause = value;
            }

            get => _IsPause;
        }
        private bool _IsPause = false;

        protected private void Start()
        {
        }

        /// <summary>
        /// タイマーアニメーションを開始する すべてのロードが行われ時間がカウントされ始めた時に呼び出す
        /// </summary>
        internal void TimeInit()
        {
            SetTime(GameManager.Instance.GameTime);
        }

        /// <summary>
        /// 指定した時間に時計をセットする
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="duration"></param>
        private void SetTime(DateTime dateTime, float duration = 0)
        {
            var hourHandDegree = dateTime.Hour * 30f;
            hourHandDegree += dateTime.Minute * 0.5f;
            var minuteHandDegree = dateTime.Minute * 6f;

            hourHand.rotation = Quaternion.Euler(0, 0, -hourHandDegree);
            minuteHand.rotation = Quaternion.Euler(0, 0, -minuteHandDegree);
        }

        /// <summary>
        /// from時間からdurationの秒数を経て一時間経過した状態にする
        /// </summary>
        /// <param name="from"></param>
        /// <param name="duration"></param>
        internal void AddHour(DateTime from, float duration)
        {
            if (sequence != null && sequence.IsActive())
                sequence.Kill();

            var hourHandDegree = 0f;
            if (from.Hour + 1 >= 13)
                hourHandDegree = (from.Hour - 11) * 30f;
            else
                hourHandDegree = (from.Hour + 1) * 30f;

            sequence = DOTween.Sequence();
           
            sequence.Append(hourHand.DORotate(new Vector3(0, 0, -hourHandDegree), duration).SetEase(Ease.Linear));
            sequence.Join(minuteHand.DORotate(new Vector3(0, 0, -360f), duration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
            sequence.OnComplete(() =>
            {
                SetTime(from.AddHours(1), 0);
            });
            sequence.OnKill(() =>
            {
                SetTime(from.AddHours(1), 0);
            });

            if (!IsPause)
                sequence.Play();
            else
                sequence.Pause();
        }
    }
}