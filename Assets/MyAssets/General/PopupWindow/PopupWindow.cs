using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;


public class PopupWindow : MonoBehaviour
{
    [SerializeField] RectTransform windowRectTransform;
    [SerializeField] CanvasGroup windowCanvasGroup;
    [SerializeField] Button backButton;
    [SerializeField] Button closeButton;

    private float animationTime = 0.5f;

    /// <summary>
    /// ユーザーからのUI操作によってWindowが閉じられたときの呼び出し
    /// </summary>
    public Action HideWithUserControl;

    protected virtual void Awake()
    {
        windowCanvasGroup.alpha = 0;
        closeButton.onClick.AddListener(() => 
        {
            Hide(() => HideWithUserControl?.Invoke()) ;
        });
        backButton.onClick.AddListener(() => Hide(() => HideWithUserControl?.Invoke()));
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        windowRectTransform.DOPivotY(1.6f, 0).OnComplete(() => gameObject.SetActive(false));
    }

    /// <summary>
    /// Windowを表示する
    /// </summary>
    public void Show(Action OnComplete = null)
    {
        windowRectTransform.pivot = new Vector2(0.5f, 1.6f);
        gameObject.SetActive(true);
        windowCanvasGroup.alpha = 1;
        windowRectTransform.DOPivotY(0.5f, animationTime).OnComplete(() => OnComplete?.Invoke());
    }

    /// <summary>
    /// Windowを閉じる
    /// </summary>
    public void Hide(Action completion = null)
    {
        if (!gameObject.activeSelf)
            return;

        var seq = DOTween.Sequence()
            .Append(windowRectTransform.DOPivotY(1.6f, animationTime))
            .OnComplete(() =>
            {
                completion?.Invoke();
                windowCanvasGroup.alpha = 0;
                gameObject.SetActive(false);
            });
        seq.SetUpdate(true);
        seq.Play();
    }
}
