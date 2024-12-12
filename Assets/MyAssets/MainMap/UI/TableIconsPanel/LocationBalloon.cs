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
    /// <summary>
    /// Squadの上に出る位置しているlocationのバルーン squadの場所に移動などのときに使用
    /// </summary>
    public class LocationBalloon : Balloon
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
        /// LocationBalloonをSquadの上に表示する
        /// </summary>
        /// <param name="squadImage"></param>
        public void Show(SquadImage squadImage)
        {
            RectTransform.SetParent(squadImage.hudPosition2);
            RectTransform.localPosition = Vector3.zero;
            RectTransform.localRotation = Quaternion.AngleAxis(1, Vector3.zero);
            SquadImage = squadImage;
            Show();
        }

        /// <summary>
        /// LocationBalloonを非表示にする
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