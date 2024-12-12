using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;
using TMPro;

public class PopupSelectionWindow : MonoBehaviour
{
    [SerializeField] GameObject tableContents;

    [SerializeField] RectTransform tabCursor;

    [SerializeField] GameObject tabBar;

    [SerializeField] public List<Tab> tabs;

    [SerializeField] public string cellAddress;

    [SerializeField] float animationTime = 0.5f;

    [SerializeField] RectTransform window;

    [SerializeField] BackPanel backPanel;

    public List<GameObject> Cells { private set; get; } = new List<GameObject>();

    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;

        set
        {
            _selectedTabIndex = value;
            tabCursor.transform.position = tabs[value].rectTransform.transform.position;
        }
    }

    public event EventHandler<Tab> TabEventHandler;

    // Called when awake the class
    private void Awake()
    {
        ReloadTabs();
    }

    /// <summary>
    /// タブを作成
    /// </summary>
    public void ReloadTabs()
    {
        // Tabを最初の１つのみ残してすべて消す
        for (var i = 1; i < tabBar.transform.childCount; i++)
        {
            var child = tabBar.transform.GetChild(i);
            DestroyImmediate(child.gameObject);
        }

        if (tabs.Count != 0)
        {
            tabBar.SetActive(true);

            var originTab = tabBar.transform.GetChild(0);
            var _tab = tabs[0];
            _tab.SetObject(originTab.gameObject, 0);
            _tab.button.onClick.AddListener(() => SelectTab(_tab));

            for (int i = 1; i < tabs.Count; i++)
            {
                var objTransform = Instantiate(originTab, tabBar.transform);
                var tab = tabs[i];
                tab.SetObject(objTransform.gameObject, i);
                tab.button.onClick.AddListener(() => SelectTab(tab));
            }
        }
        else
        {
            tabBar.SetActive(false);
        }
    }

    /// <summary>
    /// セルを指定された数量作成する
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public IEnumerator MakeUnitTableCells<T>(int count, Action<List<T>> end = null)
    {
        // TableにCellが一つもない場合は1つめを作る
        if (Cells.Count == 0)
        {
            var handler = Addressables.InstantiateAsync(cellAddress, tableContents.transform);
            while (!handler.IsDone)
                yield return null;

            var cell = handler.Result;
            Cells.Add(cell);

        }

        if (Cells.Count < count)
        {
            // Cellを足していく
            while (Cells.Count != count)
            {
                var clone = Instantiate(Cells[0].gameObject, tableContents.transform);
                Cells.Add(clone);
            }
        }
        else
        {
            // Cellを引いて行く
            while (Cells.Count != count)
            {
                DestroyImmediate(Cells[Cells.Count - 1].gameObject);
                Cells.RemoveAt(Cells.Count - 1);
            }
        }
        var cellsClass = Cells.ConvertAll((c) => c.GetComponent<T>());

        end?.Invoke(cellsClass);
    }

    /// <summary>
    /// 兵種変更タブの呼び出し
    /// </summary>
    /// <param name="tab"></param>
    private void SelectTab(Tab tab)
    {
        SelectedTabIndex = tab.index;
        TabEventHandler?.Invoke(this, tab);
    }

    public void Show()
    {
        window.pivot = new Vector2(0.5f, 1.51f);
        gameObject.SetActive(true);
        backPanel.Fadein(animationTime);
        window.DOPivotY(0.5f, animationTime);
    }

    /// <summary>
    /// Windowを閉じる
    /// </summary>
    public void Hide(Action completion = null, bool animation = true)
    {
        if (!gameObject.activeSelf)
            return;
        backPanel.Fadeout(animation ? animationTime : 0);

        var seq = DOTween.Sequence()
            .Append(window.DOPivotY(1.51f, animation ? animationTime : 0))
            .OnComplete(() =>
            {
                completion?.Invoke();
                gameObject.SetActive(false);
            });

        seq.Play();
    }

    /// <summary>
    /// Inspectorからの呼び出し用
    /// </summary>
    [SerializeField] public void Hide()
    {
        Hide(null);
    }
}

/// <summary>
///  Inspector表示用のシリアライズ可能なclass
/// </summary>
[Serializable]
public class Tab: EventArgs
{ 
    public Tab(string name)
    {
        this.name = name;
    }

    public string name;

    public int index { private set; get; }

    public GameObject tab { private set; get; }

    public Button button { private set; get; }

    public TextMeshProUGUI label { private set; get; }


    private RectTransform _rectTransform;

    internal RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null)
                _rectTransform = tab.GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

    public void SetObject(GameObject obj, int index)
    {
        this.index = index;
        tab = obj;
        button = obj.GetComponent<Button>();
        label = obj.GetComponent<TextMeshProUGUI>();
        label.text = name;
    }

}