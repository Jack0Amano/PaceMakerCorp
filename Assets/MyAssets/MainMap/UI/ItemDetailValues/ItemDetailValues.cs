using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MainMap.UI
{
    public class ItemDetailValues : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] public TextMeshProUGUI AntiPersValueLabel;
        [SerializeField] public TextMeshProUGUI AntiTankValueLabel;
        [SerializeField] public TextMeshProUGUI DefenceLabel;
        [SerializeField] public TextMeshProUGUI SupplyCostLabel;
        [SerializeField] public TextMeshProUGUI RangeLabel;

        [SerializeField] public TextMeshProUGUI TotalBulletLabel;
        [SerializeField] public TextMeshProUGUI BulletPerAttackLabel;
        [SerializeField] public TextMeshProUGUI CounterattackLabel;

        [Header("Objects")]
        [SerializeField] public GameObject AntiPersValueObj;
        [SerializeField] public GameObject AntiTankValueObj;
        [SerializeField] public GameObject DefenceObj;
        [SerializeField] public GameObject SupplyCostObj;
        [SerializeField] public GameObject RangeObj;

        [SerializeField] public GameObject TotalBulletObj;
        [SerializeField] public GameObject BulletPerAttackObj;
        [SerializeField] public GameObject CounterattackObj;

        // Start is called before the first frame update
        void Start()
        {

        }

        /// <summary>
        /// Itemの表示データをSetする
        /// </summary>
        /// <param name="item"></param>
        public void SetItemData(ItemData item)
        {
            if (item.TargetType == TargetType.Human)
            {
                SetTextIfNeeded(item.Attack, AntiPersValueLabel, AntiPersValueObj);
                SetTextIfNeeded(null, AntiTankValueLabel, AntiTankValueObj);
            }
            else if (item.TargetType == TargetType.Object)
            {
                SetTextIfNeeded(item.Attack, AntiPersValueLabel, AntiPersValueObj);
                SetTextIfNeeded(null, AntiTankValueLabel, AntiTankValueObj);
            }
            else
            {
                AntiTankValueObj.SetActive(false);
                AntiPersValueObj.SetActive(false);
            }

            SetTextIfNeeded(item.Defence, DefenceLabel, DefenceObj);
            SetTextIfNeeded(item.Supply, SupplyCostLabel, SupplyCostObj);
            SetTextIfNeeded(item.Range, RangeLabel, RangeObj);

            //SetTextIfNeeded(item.totalBullet, TotalBulletLabel, TotalBulletObj);
            // SetTextIfNeeded(item.bulletCountPerAttack, BulletPerAttackLabel, BulletPerAttackObj);
            if (item.ItemType == ItemType.HandGun || item.ItemType == ItemType.Rifle)
                SetTextIfNeeded(item.Counterattack, CounterattackLabel, CounterattackObj);
            else
                CounterattackObj.SetActive(false);
        }

        private void SetTextIfNeeded(object text, TextMeshProUGUI label, GameObject labelObject, string easeValue = "0")
        {
            if (text == null)
            {
                labelObject.SetActive(false);
                return;
            }
            var txt = text.ToString();
            if (txt.Equals(easeValue))
            {
                labelObject.SetActive(false);
            }
            else
            {
                labelObject.SetActive(true);
                label.text = txt;
            }
        }
    }
}