using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EventScene.Dialog
{
    public class SelectMapWindow : SolidWindow
    {
        [SerializeField] RawImage mapImage;
        [SerializeField] GameObject buttons;
        [SerializeField] TextMeshProUGUI locationLabel;

        private Button currentSelectedButton;

        private protected override void Awake()
        {
            base.Awake();

            locationLabel.SetText("");
            foreach(Transform c in buttons.transform)
            {
                var b = c.gameObject.GetComponent<Button>();
                var e = c.gameObject.GetComponent<ButtonEvents>();
                if (b != null && e != null)
                {
                    e.onPointerEnter = ((e) =>
                    {
                        OnPointerEnter(e, b);
                    });

                    b.onClick.AddListener(() =>
                    {
                        input.onClick.Invoke(this, Result.Yes, c.gameObject.name);

                        WindowResultCallback?.Invoke(Result.Yes);
                        Hide(() =>
                        {
                            windowClosedCallback?.Invoke();
                            input.onHidden?.Invoke(this, Result.Yes, c.gameObject.name);
                        });
                    });
                }
            }
        }

        private void OnPointerEnter(PointerEventData eventData, Button b)
        {
            if (b.Equals(currentSelectedButton)) return;
            currentSelectedButton = b;
            locationLabel.SetText(b.gameObject.name);
        }
    }
}