using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using DG.Tweening;

public class FadeInOutCanvas: MonoBehaviour
{
    private CanvasGroup CanvasGroup;
    private Sequence Sequence;

    private void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();
        CanvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// ロード中画面を表示する
    /// </summary>
    /// <param name="onComplete"></param>
    public IEnumerator Show(float duration = 0.5f)
    {
        gameObject.SetActive(true);
        if (Sequence != null && Sequence.IsActive())
            Sequence.Kill();
        Sequence = DOTween.Sequence();
        Sequence.Append(CanvasGroup.DOFade(1, duration));
        Sequence.OnComplete(() =>
        {

        });
        yield return new WaitForSeconds(duration);
    }

    /// <summary>
    /// ロード中の画面を閉じる
    /// </summary>
    /// <param name="onComplete"></param>
    public IEnumerator Hide(float duration = 0.5f, float delay = 0)
    {
        if (Sequence != null && Sequence.IsActive())
            Sequence.Kill();
        Sequence = DOTween.Sequence();
        Sequence.Append(CanvasGroup.DOFade(0, duration));
        Sequence.SetDelay(delay);
        Sequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
        yield return new WaitForSeconds(duration);
    }
}
