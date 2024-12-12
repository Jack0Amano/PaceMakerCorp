using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;


namespace Parameters.MapData
{
    /// <summary>
    /// MapData(SelectMap等のデータを保持する)のリストを保持するScriptableObject
    /// </summary>
    [Serializable]
    public class MapData
    {
        /// <summary>
        /// マップのID AddresableやDataDirectoryと同じ
        /// </summary>
        public string ID;

        /// <summary>
        /// Mapの名前 のTranslationのKey
        /// </summary>
        [SerializeField] string nameAddress;

        /// <summary>
        /// Mapの名前
        /// </summary>  
        public string Name
        {
            get
            {
                if (name == null || name.Length == 0)
                {
                    name = GameManager.Instance.Translation.CommonUserInterfaceIni.ReadValue("AllMapData", nameAddress, nameAddress);
                }
                return name;
            }
        }
        private string name = null;

        /// <summary>
        /// Mapの説明 のTranslationのKey
        /// </summary>
        [SerializeField] string descriptionAddress;

        /// <summary>
        /// Mapの説明
        /// </summary>
        public string Description
        {
            get
            {
                if (description == null || description.Length == 0)
                {
                    description = GameManager.Instance.Translation.CommonUserInterfaceIni.ReadValue("AllMapData", descriptionAddress, descriptionAddress);
                }
                return description;
            }
        }
        private string description;

        /// <summary>
        /// Mapの画像
        /// </summary>
        public Sprite Image;

        /// <summary>
        /// 解放済みのMapかどうか
        /// </summary>
        public bool IsUnlocked;
    }
}
