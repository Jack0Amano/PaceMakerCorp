using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System;

public class EasyCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rightLabel;
    [SerializeField] private TextMeshProUGUI leftLabel;
    [SerializeField] private TextMeshProUGUI topLabel;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button rightDoubleButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button leftDoubleButton;
    /// <summary>
    /// DoubleButtonを押した際の値の変化値
    /// </summary>
    [SerializeField] public int doubleButtonStep = 10;
    /// <summary>
    /// CounterのMaxとMin
    /// </summary>
    [SerializeField] public RangeAttribute counterRange = new RangeAttribute(1, 100);
    /// <summary>
    /// Counterの現在値
    /// </summary>
    [SerializeField] public int count { private set; get; } = 1;
    public Action<int> CountIsChagned;

    protected private void Awake()
    {
        rightButton.onClick.AddListener(() => OnClick(1));
        rightDoubleButton.onClick.AddListener(() => OnClick(doubleButtonStep));
        leftButton.onClick.AddListener(() => OnClick(-1));
        leftDoubleButton.onClick.AddListener(() => OnClick(-doubleButtonStep));
    }

    protected private void Start()
    {
        if (!count.In(counterRange))
            count = (int)counterRange.min;
        UpdateLabels();
    }

    /// <summary>
    /// クリックした際の値の増減を行う
    /// </summary>
    /// <param name="i">上昇下降値</param>
    private void OnClick(int i)
    {
        var _count = count;
        if (_count == counterRange.min && i == doubleButtonStep)
            _count = doubleButtonStep;
        else if ((_count + i) < counterRange.min)
            _count = (int)counterRange.min;
        else if ((_count + i) > counterRange.max)
            _count = (int)counterRange.max;
        else
            _count += i;

        if (_count == count) return;

        count = _count;
        UpdateLabels();
        CountIsChagned?.Invoke(count);
    }

    /// <summary>
    /// Labelsを現在のcountの値にアップデートする
    /// </summary>
    private void UpdateLabels()
    {
        rightLabel.SetText((count + 1).In(counterRange) ? (count + 1).ToString() : "");
        leftLabel.SetText((count - 1).In(counterRange) ? (count - 1).ToString() : "");
        topLabel.SetText(count);
    }

    /// <summary>
    /// 通知なしでcountを設定する
    /// </summary>
    /// <param name="count"></param>
    public void SetCountWithoutNotifiy(int count)
    {
        this.count = count;
        UpdateLabels();
    }
}
