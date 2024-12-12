using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AzureSky;

namespace Tactics
{
    /// <summary>
    /// 時間や天候などの自然環境を管理するクラス
    /// </summary>
    public class NaturalEnvironmentController : MonoBehaviour
    {
        [Tooltip("AzureSkyの時間のコントローラー")]
        [SerializeField] AzureTimeController timeController;

        [Tooltip("1ターンに経過する時間")]
        [SerializeField] float timePerTurn = 1f;
        [Tooltip("AddTurnで時間を経過させる際のduration")]
        [SerializeField] float addTurnDuration = 1f;

        public DateTime CurrentTime { get; private set; }

        AzureWeatherController weatherController;

        // Addhourの処理が終わった時の時間
        private DateTime addHourEndTime;

        private void Awake()
        {
            weatherController = timeController.GetComponent<AzureWeatherController>();
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            timeController.SetTimeline((float)CurrentTime.Hour + (float)CurrentTime.Minute / 60);
            timeController.SetDate(CurrentTime.Year, CurrentTime.Month, CurrentTime.Day);
        }

        /// <summary>
        /// 1ターン分時間を進める
        /// </summary>
        public IEnumerator AddTurn()
        {
            yield return AddHour(timePerTurn, addTurnDuration);
        }

        /// <summary>
        /// durationの時間でhour時間進める
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="duration"></param>
        public IEnumerator AddHour(float hour, float duration)
        {
            // もしaddHourEndTimeが設定されていたら、現在時刻との差分も加算する
            var addHour = addHourEndTime == DateTime.MinValue ? 0 : (float)(addHourEndTime - CurrentTime).TotalHours;
            hour += addHour;

            var addHourEndTimeInCoroutine = CurrentTime.AddHours(hour);
            addHourEndTime = addHourEndTimeInCoroutine;

            var seconds = hour * 3600;
            var addSeconds = seconds / duration;
            // deltaTimeで何回呼び出されるか
            var count = duration / Time.fixedDeltaTime;
            for(int i = 0; i < count; i++)
            {
                // addHourEndTimeが別のcoroutineで変更されていたら終了
                if (!addHourEndTime.Equals(addHourEndTimeInCoroutine)) break;

                CurrentTime = CurrentTime.AddSeconds(addSeconds);
                yield return new WaitForFixedUpdate();
            }
            CurrentTime = addHourEndTime;
            addHourEndTime = DateTime.MinValue;

            StopAllCoroutines();
        }

        /// <summary>
        /// 時刻を設定
        /// </summary>
        /// <param name="dateTime"></param>
        public void SetDateTime(DateTime dateTime)
        {
            CurrentTime = dateTime;
            timeController.SetTimeline((float)CurrentTime.Hour + (float)CurrentTime.Minute / 60);
            timeController.SetDate(CurrentTime.Year, CurrentTime.Month, CurrentTime.Day);
        }
    }

}