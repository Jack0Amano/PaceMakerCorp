using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static Utility;
using Unity.VisualScripting;

namespace MainMap.UI.TableIcons
{
    public class LocationImage : MonoBehaviour
    {
        [SerializeField] internal Button button;
        [SerializeField] internal ButtonEvents ButtonEvents;
        [SerializeField] internal RectTransform RectTransform;
        [SerializeField] internal RectTransform hudPosition;
        [SerializeField] Image IconImage;

        internal TableIconsPanel TableIconsPanel;

        public MapLocation MapLocation { internal get; set; }

        Camera Camera;

        /// <summary>
        /// Locationが表示中か
        /// </summary>
        public bool IsEnable
        {
            get => isEnable;
            set
            {
                if (isEnable == value)
                    return;
                isEnable = value;
                if (this.IsDestroyed())
                    return;
                if (isEnable)
                {
                    IconImage.DOFade(1, 0.3f);
                    button.enabled = true;
                }
                else
                {
                    IconImage.DOFade(0, 0.3f);
                    button.enabled = false;
                }
            }
        }
        private bool isEnable = true;

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Canvas上での位置をUpdateする
        /// </summary>
        internal void UpdateLocation()
        {
            if (Camera == null)
                Camera = Camera.main;
            RectTransform.position = MapLocation.transform.position;
        }
    }
}