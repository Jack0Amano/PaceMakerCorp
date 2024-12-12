using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;


namespace Parameters.MapData
{
    /// <summary>
    /// MapData(SelectMap���̃f�[�^��ێ�����)�̃��X�g��ێ�����ScriptableObject
    /// </summary>
    [Serializable]
    public class MapData
    {
        /// <summary>
        /// �}�b�v��ID Addresable��DataDirectory�Ɠ���
        /// </summary>
        public string ID;

        /// <summary>
        /// Map�̖��O ��Translation��Key
        /// </summary>
        [SerializeField] string nameAddress;

        /// <summary>
        /// Map�̖��O
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
        /// Map�̐��� ��Translation��Key
        /// </summary>
        [SerializeField] string descriptionAddress;

        /// <summary>
        /// Map�̐���
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
        /// Map�̉摜
        /// </summary>
        public Sprite Image;

        /// <summary>
        /// ����ς݂�Map���ǂ���
        /// </summary>
        public bool IsUnlocked;
    }
}
