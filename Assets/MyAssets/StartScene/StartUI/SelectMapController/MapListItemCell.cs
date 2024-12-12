using Parameters.MapData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SelectMapUI
{
    /// <summary>
    /// SelectMapListAdapter�̃Z��
    /// </summary>
    public class MapListItemCell : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI mapNameText;
        [SerializeField] private TMPro.TextMeshProUGUI mapDescriptionText;
        
        private UnityEngine.UI.Button selectButton;

        private MapData mapData;
        public System.Action<MapData> OnSelectMap;

        public void Awake()
        {
            selectButton = GetComponent<UnityEngine.UI.Button>();
        }

        /// <summary>
        /// List�̃Z���Ƀf�[�^���Z�b�g����
        /// </summary>
        /// <param name="mapData">Map���n�߂�ۂ̃X�^�[�g�f�[�^</param>
        /// <param name="onSelectMap">Map���I�����ꂽ�ۂ̋���</param>
        public void SetData(MapData mapData, System.Action<MapData> onSelectMap)
        {
            this.mapData = mapData;
            this.OnSelectMap = onSelectMap;

            mapNameText.text = this.mapData.Name;
            mapDescriptionText.text = this.mapData.Description;

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => this.OnSelectMap?.Invoke(this.mapData));
        }
    }

}