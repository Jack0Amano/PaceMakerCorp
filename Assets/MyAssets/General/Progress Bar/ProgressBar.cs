using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using static Utility;

public class ProgressBar : MonoBehaviour
{

    [SerializeField] RectTransform innerTransform;
    [SerializeField] RectTransform outerTransform;

    private int boarderWidth = 1;
    private float _rate;

    /// <summary>
    /// １0~1の００分率でプログレスバーのパーセンテージ指定
    /// </summary>
    public float rate
    {
        set
        {
            _rate = Mathf.Floor(value * 100f) / 100;
            var maxWidth = outerTransform.rect.width - boarderWidth * 2;
            innerTransform.sizeDelta = new Vector2(maxWidth * _rate, 0);
        }
        get
        {
            return _rate;
        }
    }

    /// <summary>
    /// アニメーション付きでプログレスバーを表示する
    /// </summary>
    public void SetRateWithAnimation(float value, float duration = 1f)
    {
        _rate = Mathf.Floor(value * 100f) / 100;
        var maxWidth = outerTransform.rect.width - boarderWidth * 2;
        var deltaX = (maxWidth * _rate);
        innerTransform.DOSizeDelta(new Vector2(deltaX, 0), duration);
    }
}
