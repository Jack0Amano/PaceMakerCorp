using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tactics.UI
{
    public class TargetPanelController : MonoBehaviour
    {
        [SerializeField] internal GameObject targetNameLabel;
        [SerializeField] internal GameObject hitRateLabel;
        [SerializeField] internal GameObject distanceLabel;
        [SerializeField] internal GameObject healthLabel;

        internal Character.UnitsController unitsController;

        private TextMeshProUGUI targetNameTextComponent;
        private TextMeshProUGUI hitRateTextComponent;
        private TextMeshProUGUI distanceTextComponent;
        private TextMeshProUGUI healthTextComponent;

        #region Texts
        private string HitRate(int rate) { return string.Format("Hit Rate       : {0}%", rate); }
        private string Name(string name) { return string.Format("Unit Name   : {0}", name); }
        private string Distance(int distance) { return string.Format("Distance      : {0}m", distance); }
        private string Health(int point) { return string.Format("Health         : {0}", point); }
        #endregion

        // Start is called before the first frame update
        void Start()
        {
            targetNameTextComponent = targetNameLabel.GetComponent<TextMeshProUGUI>();
            hitRateTextComponent = hitRateLabel.GetComponent<TextMeshProUGUI>();
            distanceTextComponent = distanceLabel.GetComponent<TextMeshProUGUI>();
            healthTextComponent = healthLabel.GetComponent<TextMeshProUGUI>();
        }

        internal void DrawInfo(Character.UnitController? target, float hitRate, int distance)
        {

            if (target != null)
            {
                targetNameTextComponent.text = Name(target.CurrentParameter.Data.Name);
                hitRateTextComponent.text = HitRate((int)hitRate);
                distanceTextComponent.text = Distance(distance);
                healthTextComponent.text = Health(target.CurrentParameter.HealthPoint);
            }
            else
            {
                targetNameTextComponent.text = "";
                hitRateTextComponent.text = "";
                distanceTextComponent.text = "";
                healthTextComponent.text = "";
            }
        }


    }
}