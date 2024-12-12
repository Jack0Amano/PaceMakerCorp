using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace Tactics.UI.Overlay
{
    public class HUDWindow : MonoBehaviour
    {
        [SerializeField] GameObject DamageLabel;
        [SerializeField] AnimationCurve eraseCurve;
        [SerializeField] AnimationCurve moveCurve;

        public void ShowDamage(Vector3 position, int score)
        {
            var screenPos = Camera.main.WorldToScreenPoint(position);
            var newLabel = Instantiate(DamageLabel, transform);
            var rect = newLabel.GetComponent<RectTransform>();
            rect.position = screenPos;
            var label = newLabel.GetComponentInChildren<TextMeshProUGUI>();
            var labelTrans = label.GetComponent<RectTransform>();
            if (score == 0)
                label.SetText("Miss");
            else
                label.SetText(score);

            var seq = DOTween.Sequence();
            seq.Append(label.DOColor(Color.clear, 2).SetEase(eraseCurve));
            seq.Join(labelTrans.DOAnchorPosY(90, 2).SetEase(moveCurve));
            seq.OnComplete(() => Destroy(newLabel));
            seq.Play();
            
        }
    }
}