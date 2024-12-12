using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// 各種ボタンのイベントをActionDelegateで受け取るための拡張 
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action<PointerEventData> onPointerEnter;
    public Action<PointerEventData> onPointerExit;

    public Button Button;

    private void Awake()
    {
        Button = GetComponent<Button>();
    }

    // this method called by mouse-pointer enter the object.
    public void OnPointerEnter(PointerEventData eventData)
    {
        onPointerEnter?.Invoke(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onPointerExit?.Invoke(eventData);
    }
}