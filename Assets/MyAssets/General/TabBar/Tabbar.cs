using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class Tabbar : MonoBehaviour
{
    private List<Tab> tabButtons;

    [SerializeField] Color normalLabelColor;
    [SerializeField] Color selectedLabelColor;
    [SerializeField] float animationDuration = 0.3f;

    /// <summary>
    /// タブボタンが押された際の呼び出し
    /// </summary>
    public Action<Tab, int> tabButtonClicked;

    /// <summary>
    /// タブボタンを変更できるかどうか
    /// </summary>
    public bool ableToChangeTab = true;

    private CanvasGroup CanvasGroup;

    private int _index = 0;
    public int Index
    {
        get => _index;
        set
        {
            var seq = DOTween.Sequence();
            if (tabButtons.IndexAt_Bug(_index, out Tab old))
            {
                seq.Append(old.hlImage.DOColor(Color.clear, 0.3f));
                seq.Join(old.Label.DOColor(normalLabelColor, 0.3f));
                old.isSelected = false;
            }

            if (tabButtons.IndexAt_Bug(value, out Tab tab))
            {
                seq.Join(tab.hlImage.DOColor(Color.white, 0.3f));
                seq.Join(tab.Label.DOColor(selectedLabelColor, 0.3f));
                tab.isSelected = true;

                _index = value;
            }

            seq.Play();
        }
    }


    protected void Awake()
    {
        tabButtons = new List<Tab>();
        var obj = transform.GetChild(0).gameObject;
        tabButtons.Add(new Tab(obj));
        CanvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 消えているタブを表示
    /// </summary>
    /// <param name="animation"></param>
    public void Show(bool animation)
    {
        if (gameObject.activeSelf)
            return;
        gameObject.SetActive(true);
        if (animation)
        {
            CanvasGroup.alpha = 0;
            CanvasGroup.DOFade(1, animationDuration);
        }
        else
        {
            CanvasGroup.alpha = 1;
        }
    }

    /// <summary>
    /// Tabを消す
    /// </summary>
    /// <param name="animation"></param>
    public void Hide(bool animation)
    {
        if (!gameObject.activeSelf)
            return;
        CanvasGroup.alpha = 1;
        if (animation)
        {
            CanvasGroup.DOFade(0, animationDuration).OnComplete(() => gameObject.SetActive(false));
        }
        else
        {
            CanvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// TabをValuesの数だけ設置
    /// </summary>
    /// <param name="values"></param>
    public void SetTabs(List<string> values)
    {
        while(values.Count != tabButtons.Count)
        {
            if (tabButtons.Count < values.Count)
            {
                if (!tabButtons[0].button.gameObject.activeSelf)
                    tabButtons[0].button.gameObject.SetActive(true);

                // Tabをクローンして増やしていく
                var newTab = Instantiate(tabButtons[0].button.gameObject, transform);
                tabButtons.Add(new Tab(newTab));
            }
            else if (tabButtons.Count == 1)
            {
                tabButtons[0].button.gameObject.SetActive(false);
                break;
            }
            else
            {
                var last = tabButtons[tabButtons.Count - 1].button.gameObject;
                Destroy(last);
                tabButtons.RemoveAt(tabButtons.Count - 1);
            }
        }

        Index = 0;
        for (int i=0; i<values.Count; i++)
        {
            var j = i;
            var tab = tabButtons[i];
            tab.Label.text = values[i];
            tab.button.onClick.AddListener(() => TabButtonClicked(tab, j));

            if (Index != i)
            {
                tab.hlImage.color = Color.clear;
                tab.Label.color = normalLabelColor;
            }
        }

        
    }

    private void TabButtonClicked(Tab tab, int index)
    {
        tabButtonClicked?.Invoke(tab, index);
        // タブボタンの変更が可能でない場合
        if (ableToChangeTab == false)
            return;
        this.Index = index;
    }

    public class Tab
    {
        internal Tab(GameObject obj)
        {
            this.button = obj.GetComponent<Button>();
            var images = obj.GetComponentsInChildren<Image>();
            this.image = images[0];
            this.hlImage = images[1];
            this.Label = obj.GetComponentInChildren<TextMeshProUGUI>();
        }

        internal Button button;
        internal Image image;
        public TextMeshProUGUI Label { internal set; get; }
        internal Image hlImage;

        internal bool isSelected = false;
    }
}
