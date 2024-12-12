using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AzureSky;

namespace Tactics
{
    /// <summary>
    /// ���Ԃ�V��Ȃǂ̎��R�����Ǘ�����N���X
    /// </summary>
    public class NaturalEnvironmentController : MonoBehaviour
    {
        [Tooltip("AzureSky�̎��Ԃ̃R���g���[���[")]
        [SerializeField] AzureTimeController timeController;

        [Tooltip("1�^�[���Ɍo�߂��鎞��")]
        [SerializeField] float timePerTurn = 1f;
        [Tooltip("AddTurn�Ŏ��Ԃ��o�߂�����ۂ�duration")]
        [SerializeField] float addTurnDuration = 1f;

        public DateTime CurrentTime { get; private set; }

        AzureWeatherController weatherController;

        // Addhour�̏������I��������̎���
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
        /// 1�^�[�������Ԃ�i�߂�
        /// </summary>
        public IEnumerator AddTurn()
        {
            yield return AddHour(timePerTurn, addTurnDuration);
        }

        /// <summary>
        /// duration�̎��Ԃ�hour���Ԑi�߂�
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="duration"></param>
        public IEnumerator AddHour(float hour, float duration)
        {
            // ����addHourEndTime���ݒ肳��Ă�����A���ݎ����Ƃ̍��������Z����
            var addHour = addHourEndTime == DateTime.MinValue ? 0 : (float)(addHourEndTime - CurrentTime).TotalHours;
            hour += addHour;

            var addHourEndTimeInCoroutine = CurrentTime.AddHours(hour);
            addHourEndTime = addHourEndTimeInCoroutine;

            var seconds = hour * 3600;
            var addSeconds = seconds / duration;
            // deltaTime�ŉ���Ăяo����邩
            var count = duration / Time.fixedDeltaTime;
            for(int i = 0; i < count; i++)
            {
                // addHourEndTime���ʂ�coroutine�ŕύX����Ă�����I��
                if (!addHourEndTime.Equals(addHourEndTimeInCoroutine)) break;

                CurrentTime = CurrentTime.AddSeconds(addSeconds);
                yield return new WaitForFixedUpdate();
            }
            CurrentTime = addHourEndTime;
            addHourEndTime = DateTime.MinValue;

            StopAllCoroutines();
        }

        /// <summary>
        /// ������ݒ�
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