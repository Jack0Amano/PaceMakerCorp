using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

namespace MainMap.UI.TableIcons
{
    public class FastForwardIcon : MonoBehaviour
    {
        [SerializeField] Image IconImage;
        [SerializeField] TextMeshProUGUI SpeedLabel;

        CanvasGroup CanvasGroup;

        // Start is called before the first frame update
        void Start()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            CanvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// FastForwardIconを表示する
        /// </summary>
        public void Show()
        {
            SpeedLabel.text = Math.Round(GameManager.Instance.Speed, 1).ToString();
            gameObject.SetActive(true);
            CanvasGroup.DOFade(1, 0.3f);
        }

        /// <summary>
        /// FastForwardIconを非表示にする
        /// </summary>
        public void Hide()
        {
            CanvasGroup.DOFade(0, 0.3f).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        
    }
}