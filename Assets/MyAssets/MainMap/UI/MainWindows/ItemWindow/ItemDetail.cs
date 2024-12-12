
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System;

namespace MainMap.UI.Item
{
    public class ItemDetail : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI itemName;
        [SerializeField] ItemDetailValues ItemDetailValues;

        [SerializeField] TextMeshProUGUI companyHead;
        [SerializeField] TextMeshProUGUI companyLabel;
        [SerializeField] TextMeshProUGUI subCompanyLabel;
        [SerializeField] GameObject subCompanyObj;
        [SerializeField] TextMeshProUGUI descriptionLabel;

        private string manufacturer = "Manufacturer";
        private string designedBy = "Designed by";
        private CanvasGroup canvasGroup;

        protected private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
        }

        internal void Show(ItemData item)
        {
            itemName.text = item.Name;
            ItemDetailValues.SetItemData(item);

            if (item.SubCompany.Length != 0)
            {
                subCompanyObj.SetActive(true);
                subCompanyLabel.text = item.SubCompany;
            }
            else
                subCompanyObj.SetActive(false);

            companyLabel.SetText(item.Company);
            companyHead.SetText(subCompanyObj.activeSelf ? designedBy : manufacturer);
            descriptionLabel.SetText(item.Description);

            canvasGroup.DOFade(1, 0.5f);
        }

        internal void Hide()
        {
            canvasGroup.DOFade(0, 0.5f);
        }
    }
}
