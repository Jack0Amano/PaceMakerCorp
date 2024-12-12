using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class CoveredTextMesh : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI textMesh;

    [SerializeField] Image coverImage;

    [SerializeField] Sprite sprite;

    [SerializeField] bool showCover = false;

    [SerializeField] float duration = 1f;

    protected private void Awake()
    {
        coverImage.gameObject.SetActive(true);
        coverImage.sprite = sprite;
        coverImage.DOFade(showCover ? 1 : 0, 0);
        coverImage.DOFade(showCover ? 0 : 1, 0);
    }

    /// <summary>
    /// カバーイメージを表示する
    /// </summary>
    public void HideTextLabel(bool animation = true)
    {
        var seq = DOTween.Sequence();
        seq.Append(coverImage.DOFade(1, animation ? duration : 0));
        seq.Join(textMesh.DOFade(0, animation ? duration : 0));
        seq.SetDelay(0.8f);
        seq.Play();
    }

    /// <summary>
    /// カバーイメージを非表示にする
    /// </summary>
    public void ShowTextLabel(bool animation = true)
    {
        var seq = DOTween.Sequence();
        seq.Append(coverImage.DOFade(0, animation ? duration : 0));
        seq.Join(textMesh.DOFade(1, animation ? duration : 0));
        seq.SetDelay(0.8f);
        seq.Play();
    }
}
