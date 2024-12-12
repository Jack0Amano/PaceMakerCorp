using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace StartWindow
{
    public class LoadCell : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI dateTimeLabel;

        public Button button { private set; get; }

        public string path { private set; get; }

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void SetData(SaveDataInfo info, string path)
        {
            this.path = path;
            dateTimeLabel.SetText(info.SaveTime.ToString());
        }
    }
}