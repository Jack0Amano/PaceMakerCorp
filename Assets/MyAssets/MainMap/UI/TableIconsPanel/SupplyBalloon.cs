using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;
using static Utility;
using System;

namespace MainMap.UI.TableIcons
{
    public class SupplyBalloon : Balloon
    {
        [SerializeField] internal Button Button;
        [SerializeField] internal ButtonEvents ButtonEvents;

        internal SquadImage SquadImage;

        /// <summary>
        /// Iconにcursorがenterしたときの呼び出し
        /// </summary>
        public Action<SquadImage> OnPointerEnterAction;
        /// <summary>
        /// Iconにcursorがexitしたときの呼び出し
        /// </summary>
        public Action<SquadImage> OnPointerExitAction;
        /// <summary>
        /// Iconをcursorがクリックしたときの呼び出し
        /// </summary>
        public Action<SquadImage> OnPointerClickAction;

        private void Start()
        {
            ButtonEvents.onPointerEnter = ((e) => OnPointerEnterAction?.Invoke(SquadImage));
            ButtonEvents.onPointerExit = ((e) => OnPointerExitAction?.Invoke(SquadImage));
            Button.onClick.AddListener(() => OnPointerClickAction?.Invoke(SquadImage));
        }

        /// <summary>
        /// Balloonを表示
        /// </summary>
        /// <param name="squadImage"></param>
        public void Show(SquadImage squadImage)
        {
            RectTransform.SetParent(squadImage.hudPosition1);
            RectTransform.localPosition = Vector3.zero;
            RectTransform.localRotation = Quaternion.AngleAxis(1, Vector3.zero);
            SquadImage = squadImage;
            Show();
        }

        /// <summary>
        /// Supply表示のBalloonを非表示
        /// </summary>
        /// <param name="delay"></param>
        public void Hide(float delay = 0)
        {
            Hide(delay, () =>
            {
                RectTransform.SetParent(TableParentTransform);
                SquadImage = null;
            });
        }
    }
}