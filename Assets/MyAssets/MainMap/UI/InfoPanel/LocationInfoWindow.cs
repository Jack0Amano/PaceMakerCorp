using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

namespace MainMap.UI.InfoPanel
{
    public class LocationInfoWindow : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI locationName;
        [SerializeField] TextMeshProUGUI locationType;
        [SerializeField] Image flagImage;

        RectTransform rectTransform;
        private float shownXPosition;
        private float duration = 0.3f;
        public bool isShown { private set; get; } = false;
        public LocationParamter location { private set; get; }

        protected private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            shownXPosition = rectTransform.anchoredPosition.x;

            var position = rectTransform.anchoredPosition;
            position.x = rectTransform.rect.width;
            rectTransform.anchoredPosition = position;
        }

        internal IEnumerator Show()
        {
            isShown = true;
            yield return rectTransform.DOAnchorPosX(shownXPosition, duration).WaitForCompletion();
        }

        internal IEnumerator Hide()
        {
            isShown = false;
            yield return rectTransform.DOAnchorPosX(rectTransform.rect.width, duration).WaitForCompletion();
        }

        internal void SetInfomation(LocationParamter location)
        {
            this.location = location;
            locationName.SetText(location.Name);
        }
    }
}
