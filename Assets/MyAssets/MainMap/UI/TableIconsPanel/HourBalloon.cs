using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MainMap.UI.TableIcons
{

    public class HourBalloon : Balloon
    {
        [SerializeField] internal TextMeshProUGUI dayLabel;

        // Start is called before the first frame update
        void Start()
        {
            dayLabel.text = "";
        }

        /// <summary>
        /// 移動にどれだけ時間がかかるかのballoonをsquadの上に表示する
        /// </summary>
        /// <param name="squadImage"></param>
        /// <param name="hour"></param>
        public void Show(SquadImage squadImage, float hour)
        {
            RectTransform.SetParent(squadImage.hudPosition);
            RectTransform.localPosition = Vector3.zero;
            RectTransform.localRotation = Quaternion.AngleAxis(1, Vector3.zero);
            dayLabel.SetText(hour);
            Show();
        }

        /// <summary>
        /// 移動にどれだけ時間がかかるかのballoonをlocationの上に表示する
        /// </summary>
        public void Show(LocationImage locationImage, float hour)
        {
            RectTransform.SetParent(locationImage.hudPosition);
            RectTransform.localPosition = Vector3.zero;
            RectTransform.localRotation = Quaternion.AngleAxis(1, Vector3.zero);
            dayLabel.SetText(hour);
            Show();
        }

        /// <summary>
        /// 時間のBalloonを非表示にする
        /// </summary>
        /// <param name="delay"></param>
        public void Hide(float delay = 0)
        {
            RectTransform.SetParent(TableParentTransform);
            Hide(delay, null);
        }
    }
}