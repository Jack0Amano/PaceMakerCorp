using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using UnityEngine.UI;

public class BackPanel : MonoBehaviour
{

    [SerializeField] float blur = 35;

    [SerializeField] Material material;

    [SerializeField, Range(0, 1)]
    private float cutoutRange;

    [SerializeField] public Button backpanelButton;

    protected private void Awake()
    {
        //material.SetFloat("_Blur", cutoutRange * blur);
    }

    private IEnumerator FadeoutCoroutine(float time, Action action)
    {
        float endTime = Time.unscaledTime + time * (cutoutRange);

        var endFrame = new WaitForEndOfFrame();

        while (Time.unscaledTime <= endTime)
        {
            cutoutRange = (endTime - Time.unscaledTime) / time;
            //material.SetFloat("_Blur", cutoutRange * blur);

            yield return endFrame;
        }
        cutoutRange = 0;
        //material.SetFloat("_Blur", cutoutRange);

        

        action?.Invoke();
    }

    private IEnumerator FadeinCoroutine(float time, Action action)
    {
        float endTime = Time.unscaledTime + time * (1 - cutoutRange);

        var endFrame = new WaitForEndOfFrame();

        while (Time.unscaledTime <= endTime)
        {
            cutoutRange = 1 - ((endTime - Time.unscaledTime) / time);
            //material.SetFloat("_Blur", cutoutRange * blur);
            yield return endFrame;
        }
        cutoutRange = 1;
        //material.SetFloat("_Blur", cutoutRange * blur);

        action?.Invoke();
    }

    /// <summary>
    /// Blurエフェクトをフェードアウトする
    /// </summary>
    /// <param name="time"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public Coroutine Fadeout(float time, Action action = null)
    {
        if (action != null)
            StopAllCoroutines();
        return StartCoroutine(FadeoutCoroutine(time, action));
    }

    /// <summary>
    /// Blurエフェクトをフェードインする
    /// </summary>
    /// <param name="time"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public Coroutine Fadein(float time, Action action = null)
    {
        if (action != null)
            StopAllCoroutines();
        return StartCoroutine(FadeinCoroutine(time, action));
    }
} 
