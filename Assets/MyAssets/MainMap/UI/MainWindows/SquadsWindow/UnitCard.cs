using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using TMPro;

namespace MainMap.UI.Squads.Detail
{
    public class UnitCard : MonoBehaviour
    {
        [SerializeField] public Button deleteButton;

        [SerializeField] public Image deleteImage;

        [SerializeField] public Button changeUnitButton;

        [SerializeField] public Image changeImage;

        [SerializeField] public TextMeshProUGUI unitName;

        [SerializeField] public TextMeshProUGUI healthPoint;

        [SerializeField] public TextMeshProUGUI attack;

        [SerializeField] public Image unitImage;

        [SerializeField] List<HolderIconAndType> holderIconAndTypes;

        [SerializeField] List<EquipmentCell> equipmentCells;

        public Dictionary<HolderType, Sprite> icons;

        public Button SelectUnitButton { private set; get; }

        private bool _isLocked = false;

        public UnitData unitData { private set; get; }

        /// <summary>
        /// 削除や変更のできないユニット 主にCommanderやイベント系
        /// </summary>
        public bool isLocked
        {
            get
            {
                return _isLocked;
            }
            set
            {
                _isLocked = value;
                deleteButton.interactable = !value;
                changeUnitButton.interactable = !value;
            }
        }

        private void Awake()
        {
            SelectUnitButton = GetComponent<Button>();
        }

        /// <summary>
        /// Cardに表示するUnitのParameterを設置する
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="icons"></param>
        internal void SetParameter(UnitData parameter)
        {
            this.unitData = parameter;
            unitName.SetText(parameter.Name);
            healthPoint.SetText(parameter.HealthPoint);
            // TODO AttackPointはunit.itemController.attackPointから 取得
            // attack.SetText(parameter.attackPoint);

            for (int i=0; i<equipmentCells.Count; i++)
            {
                if (parameter.MyItems.IndexAt_Bug(i, out ItemHolder holder))
                {
                    equipmentCells[i].gameObject.SetActive(true);
                    var holderIconAndType = holderIconAndTypes.Find(i => i.type == holder.Type);
                    if (holderIconAndType != null)
                        equipmentCells[i].icon.sprite = holderIconAndType.icon;

                    equipmentCells[i].label.SetText(holder.Data != null ? holder.Data.Name : "");

                }
                else
                {
                    // 表示しないCell
                    equipmentCells[i].gameObject.SetActive(false);
                }
            }
        }


        /// <summary>
        /// Inspector用のEquipment class
        /// </summary>
        [Serializable]
        class EquipmentCell
        {

            [SerializeField] internal TextMeshProUGUI label;

            [SerializeField] internal Image icon;

            [NonSerialized] private GameObject _gameObject;

            internal GameObject gameObject
            {
                get
                {
                    if (_gameObject == null)
                        _gameObject = label.gameObject;
                    return _gameObject;
                }
            }
        }

        /// <summary>
        /// UnitCardに表示するHolderのIconとそのTypeの紐づけ
        /// </summary>
        [Serializable]
        class HolderIconAndType
        {
            [SerializeField] internal HolderType type;
            [SerializeField] internal Sprite icon;
        }

    }
}