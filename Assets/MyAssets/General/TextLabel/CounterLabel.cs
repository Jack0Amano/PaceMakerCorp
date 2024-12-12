using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CounterLabel : MonoBehaviour
{
    public TextMeshProUGUI textLabel { private set; get; }

    protected void Awake()
    {
        textLabel = GetComponent<TextMeshProUGUI>();
    }

    public IEnumerator SetCount(int from, int to, float timeDelta)
    {
        textLabel.SetText(from.ToString());

        var _skip = Time.deltaTime / (timeDelta / (to - from));
        var skip = 0;
        var interval = 1f;
        if (_skip < 1)
        {
            // Countの上昇が急でないためSkipが必要ない
            skip = 1;
            interval = 1.5f / (to - from);
        }
        else
        {
            // Countの上昇がFPSより上なためSkipして時間通りに表示を終わらせれるようにする
            skip = (int)_skip;
            interval = Time.deltaTime;
        }

        while (from != to)
        {
            yield return new WaitForSeconds(interval);
            from += skip;
            if (from > to)
                from = to;
            textLabel.SetText(from.ToString());
        }
    }
}
