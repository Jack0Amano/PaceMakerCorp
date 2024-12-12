using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using static Utility;

namespace MapUI.UI.InfoPanel
{
    public class Calender : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI monthLabel;
        [SerializeField] TextMeshProUGUI dayLabel;
        [SerializeField] TextMeshProUGUI yearLabel;
        [SerializeField] TextMeshProUGUI hourLabel;
        [SerializeField] TextMeshProUGUI minuteLabel;

        protected private void Awake()
        {
            GameManager.Instance.AddTimeEventHandlerAsync += (o,a) => UpdateTime();
        }

        /// <summary>
        /// 一定時間経過したらGameManagerのHandlerから呼び出し
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        public void UpdateTime()
        {
            var gameTime = GameManager.Instance.GameTime;
            monthLabel.SetText(gameTime.MonthEN());
            dayLabel.SetText(gameTime.Day.ToString("D2"));
            yearLabel.SetText(gameTime.Year.ToString("D4"));
            hourLabel.SetText(gameTime.Hour.ToString("D2"));
            if (GameManager.Instance.IsHighSpeedMode)
                StartCoroutine(SetHighSpeedMinute(gameTime.Minute));
            else
                minuteLabel.SetText(gameTime.Minute.ToString("D2"));
        }

        /// <summary>
        /// GameSpeedが30倍以上の場合時間の更新が10分おきになるため分の1の位だけ関係なく早送りアニメーション風にする
        /// </summary>
        private IEnumerator SetHighSpeedMinute(int minute)
        {
            var minute1 = minute%10;
            var minute2 = minute - minute1;
            for(var i=0; i<10; i++)
            {
                minute1++;
                if (minute1 == 9)
                    minute1 = 0;
                minuteLabel.SetText((minute1 + minute2).ToString("D2"));
                yield return null;
            }
        }
    }
}