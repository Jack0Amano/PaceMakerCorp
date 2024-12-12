using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Febucci.UI;
using UnityEditor.XR;
using DG.Tweening;

namespace GameSetting.SaveLoadData
{
    /// <summary>
    /// Save&LoadDataのWindowのTableに表示されるCell
    /// </summary>
    public class SaveLoadDataCell : MonoBehaviour
    {
        [Header("Save&LoadData Cell")]
        [Tooltip("SaveLoadDataCellの情報を表示する大本のパネル")]
        [SerializeField] GameObject saveLoadDataPanel;

        [Tooltip("保存日")]
        [SerializeField] TextMeshProUGUI saveDateLabel;

        [Tooltip("保存時間")]
        [SerializeField] TextMeshProUGUI saveTimeLabel;

        [Tooltip("作戦地域")]
        [SerializeField] TextMeshProUGUI operationAreaLabel;

        [Tooltip("作戦地域での時間")]
        [SerializeField] TextMeshProUGUI localTimeLabel;

        [Tooltip("所持しているUnitの数")]
        [SerializeField] TextMeshProUGUI unitsCountLabel;

        [Tooltip("組んでいるSquadの数")]
        [SerializeField] TextMeshProUGUI squadsCountLabel;

        [Tooltip("データを削除するボタン")]
        [SerializeField] internal Button deleteButton;

        [Tooltip("データを選択するボタン")]
        [SerializeField] internal Button selectButton;

        TypewriterByCharacter saveDataLabelAnimator;
        TypewriterByCharacter saveTimeLabelAnimator;
        TypewriterByCharacter operationAreaLabelAnimator;
        TypewriterByCharacter localTimeLabelAnimator;
        TypewriterByCharacter unitsCountLabelAnimator;
        TypewriterByCharacter squadsCountLabelAnimator;

        CanvasGroup canvasGroup;

        private void Awake()
        {
            saveDataLabelAnimator = saveDateLabel.GetComponent<TypewriterByCharacter>();
            saveTimeLabelAnimator = saveTimeLabel.GetComponent<TypewriterByCharacter>();
            operationAreaLabelAnimator = operationAreaLabel.GetComponent<TypewriterByCharacter>();
            localTimeLabelAnimator = localTimeLabel.GetComponent<TypewriterByCharacter>();
            unitsCountLabelAnimator = unitsCountLabel.GetComponent<TypewriterByCharacter>();
            squadsCountLabelAnimator = squadsCountLabel.GetComponent<TypewriterByCharacter>();

            canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// セーブデータの新規追加モード
        /// </summary>
        public bool IsNewDataMode
        {
            get => isNewDataMode;
            set
            {
                isNewDataMode = value;
                saveLoadDataPanel.SetActive(!value);
            }
        }
        private bool isNewDataMode = false;

        /// <summary>
        /// セーブデータの情報をセットする
        /// </summary>
        /// <param name="model"></param>
        public void SetInfo(DataListItemModel model, bool animation)
        {
            saveDataLabelAnimator.useTypeWriter = animation;
            saveTimeLabelAnimator.useTypeWriter = animation;
            operationAreaLabelAnimator.useTypeWriter = animation;
            localTimeLabelAnimator.useTypeWriter = animation;
            unitsCountLabelAnimator.useTypeWriter = animation;
            squadsCountLabelAnimator.useTypeWriter = animation;

            if (animation)
            {
                saveDataLabelAnimator.ShowText(model.date);
                saveTimeLabelAnimator.ShowText(model.time);
                operationAreaLabelAnimator.ShowText(model.operatingArea);
                localTimeLabelAnimator.ShowText(model.localTime);
                unitsCountLabelAnimator.ShowText(model.unitsValue);
                squadsCountLabelAnimator.ShowText(model.squadsValue);
            }
            else
            {
                saveDateLabel.text = model.date;
                saveTimeLabel.text = model.time;
                operationAreaLabel.text = model.operatingArea;
                localTimeLabel.text = model.localTime;
                unitsCountLabel.text = model.unitsValue;
                squadsCountLabel.text = model.squadsValue;
                
            }
        }

        /// <summary>
        /// Cellの情報を削除するアニメーション
        /// </summary>
        public void DeleteInfoWithAnimation()
        {
            saveDataLabelAnimator.StartDisappearingText();
            saveTimeLabelAnimator.StartDisappearingText();
            operationAreaLabelAnimator.StartDisappearingText();
            localTimeLabelAnimator.StartDisappearingText();
            unitsCountLabelAnimator.StartDisappearingText();
            squadsCountLabelAnimator.StartDisappearingText();
        }

        /// <summary>
        /// Cellを表示してまた消す
        /// </summary>
        public IEnumerator ShowAndHide(DataListItemModel dataListItemModel)
        {
            canvasGroup.alpha = 0.01f;
            SetInfo(dataListItemModel, true);
            canvasGroup.DOFade(1, 0.5f).OnComplete(() =>
            {
                canvasGroup.DOFade(0, 0.2f).SetDelay(2);
            });
            // alphaが0になるまで待つ
            while (true)
            {
                yield return null;
                if (canvasGroup.alpha == 0)
                    break;
            }
        }
    }
}