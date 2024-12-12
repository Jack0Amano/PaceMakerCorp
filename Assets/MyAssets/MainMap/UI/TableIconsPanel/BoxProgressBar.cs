using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;
using static Utility;

namespace MainMap.UI.TableIcons
{
    public class BoxProgressBar : MonoBehaviour
    {
        [Header("Images")]
        [SerializeField] Image frame;
        [SerializeField] Image value01;
        [SerializeField] Image value02;
        [SerializeField] Image value03;
        [SerializeField] Image value04;

        [Header("Color")]
        [SerializeField] Color normalColor;
        [SerializeField] Color warningColor;

        List<(Image image, bool enable)> valueImages;

        public bool IsEnable { private set; get; } = false;

        Sequence sequence;

        public float Value { private set; get; } = 0;

        protected private void Start()
        {
            IsEnable = false;
            valueImages = new List<Image> { value01, value02, value03, value04 }.ConvertAll(i => (i, false));
            valueImages.ForEach(i => i.image.color = Color.clear);
            frame.color = Color.clear;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// ProgressBarに値を表示する
        /// </summary>
        /// <param name="value">0~1の値</param>
        public void SetValue(float value)
        {
            value -= 0.05f;
            if (sequence != null && sequence.IsActive())
                sequence.Kill();
            sequence = DOTween.Sequence();
            var tweens = new List<Tween>();

            if (!IsEnable)
            {
                gameObject.SetActive(true);
                sequence.Append(frame.DOColor(normalColor, 0.2f));
            }


            int activeIndex = (int)(value * 4);
            // ActiveIndexより下のIndexはすべてNormalColor, より上のはClear

            // まず valueがValueより少なくて Progressbarが減少する場合
            // valueのIndexに向けて段々とColor.Clearにしていく
            for(var index=0; index<valueImages.Count; index++)
            {
                var i = index;
                var (image, enable) = valueImages[index];
                if (value * 4 < index)
                {
                    if (enable)
                        tweens.Add(image.DOColor(Color.clear, 0.4f).OnComplete(() => valueImages[i] = (valueImages[i].image, false)));
                    else if (IsEnable)
                        sequence.Join(image.DOColor(Color.clear, 0.2f).OnComplete(() => valueImages[i] = (valueImages[i].image, false)));
                    else
                        image.color = Color.clear;
                }
                else
                {
                    if (!enable)
                        tweens.Add(image.DOColor(normalColor, 0.4f).OnComplete(() => valueImages[i] = (valueImages[i].image, true)));
                    else if (IsEnable)
                        sequence.Join(image.DOColor(normalColor, 0.2f).OnComplete(() => valueImages[i] = (valueImages[i].image, true)));
                    else
                        image.color = normalColor;
                }
            }
            
            if (value < Value)
                tweens.Reverse();
            if (tweens.Count == 1)
                tweens.ForEach(t => sequence.Append(t));
            else if (tweens.Count > 1)
            {
                tweens.Select((t,i)=>(t,i)).ToList().ForEach(x => sequence.Append(x.t.SetDelay(x.i == 0 ? 0 : -0.3f)));
            }
                
            sequence.Play();
            IsEnable = true;
            Value = value;
        }

        /// <summary>
        /// ProgressBarを非表示にする
        /// </summary>
        public void Hide()
        {
            if (sequence != null && sequence.IsActive())
                sequence.Kill();
            sequence.Append(frame.DOColor(Color.clear, 0.4f));
            valueImages.ForEach(i => sequence.Join(i.image.DOColor(Color.clear, 0.4f)));
            sequence.OnComplete(() =>
            {
                IsEnable = false;
                gameObject.SetActive(false);
            });
            sequence.Play();
        }


    }
}