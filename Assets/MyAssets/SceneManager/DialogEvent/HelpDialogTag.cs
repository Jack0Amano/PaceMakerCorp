using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Utility;
using UnityEditor.PackageManager;

namespace EventScene.Dialog.Help
{
    public class HelpDialogTag : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("表示内容のアクセスキー")]
        [SerializeField] internal string Key;
        [Tooltip("Dialogを3D空間に表示する場合の位置")]
        [SerializeField] private Transform WorldPosition;
        [Tooltip("DialogをUIにそって表示する場合の位置")]
        [SerializeField] private RectTransform UIPosition;

        /// <summary>
        /// Dialogのひょうじする場所
        /// </summary>
        internal Vector3 DialogPosition
        {
            get
            {
                if (UIPosition != null)
                    return new Vector3(UIPosition.position.x, UIPosition.position.y, 0);
                else if (WorldPosition != null)
                    return Camera.main.WorldToScreenPoint(WorldPosition.position);
                throw new Exception("Both WorldPosition and UIPosition are not set");
            }
        }

        HelpDialog HelpDialog;

        private void Start()
        {
            HelpDialog = GameManager.Instance.EventSceneController.dialogEvent.HelpDialog;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartCoroutine(HelpDialog.Show(this));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HelpDialog.Hide();
        }

        
    }
}