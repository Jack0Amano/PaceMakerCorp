using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using static Utility;

namespace MainMap.UI
{
    public class LocationWindow : MonoBehaviour
    {
        
        [SerializeField] TMP_Dropdown locationNameDropDown;

        [SerializeField] Button buttonSelectSquadsWindow;

        [SerializeField] TextMeshProUGUI squadsCount;

        [SerializeField] CoveredTextMesh defenceValue;

        [SerializeField] CoveredTextMesh recoveryValue;

        private readonly float animationTime = 0.5f;

        private CanvasGroup canvasGroup;

        private List<LocationParamter> locations;

        /// <summary>
        /// このHandlerが実行された際にMainControllerでEventArgsから指定されたWindowを開く
        /// </summary>
        internal EventHandler<OpenWindowArgs> requestOpenWindowHandler;

        protected private void Awake()
        {
            return;
            locationNameDropDown.onValueChanged.AddListener((i) => LocationDorpdownChanged(i));
            canvasGroup = GetComponent<CanvasGroup>();
            buttonSelectSquadsWindow.onClick.AddListener(() =>
            {
                // TODO: MainUIControllerにdelegateを送りSquadsWindowを選択する
                requestOpenWindowHandler?.Invoke(this, new OpenWindowArgs()
                {
                    windowType = WindowType.SquadsWindow,
                    tabButtonDisabledManual = true
                });
             });
        }

        protected private void Start()
        {
            return;
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Windowを表示
        /// </summary>
        /// <param name="onComplete"></param>
        public void Show(Action onComplete = null)
        {
            SetInfomation();
            gameObject.SetActive(true);
            canvasGroup.DOFade(1, animationTime).OnComplete(() =>
            {
                onComplete?.Invoke();
            }).Play();
        }

        /// <summary>
        /// WIndowを非表示
        /// </summary>
        /// <param name="onComplete"></param>
        public void Hide(Action onComplete = null)
        {
            canvasGroup.DOFade(0, animationTime).OnComplete(() =>
            {
                onComplete?.Invoke();

            //マスクをし直す
            defenceValue.HideTextLabel(false);
                recoveryValue.HideTextLabel(false);

                gameObject.SetActive(false);
            }).Play();
        }

        /// <summary>
        /// Windowの情報を表示させる
        /// </summary>
        /// <param name="locationParamter"></param>
        private void SetInfomation()
        {

            //var locationParameter = GameManager.Instance._uiGeneralParameters.selectedLocation;
            //if (locationParameter == null)
            //{
            //    // locationName.SetText("");
            //    buttonSelectSquadsWindow.interactable = false;
            //    return;
            //}

            //var selectedLocIndex = locations.FindIndex((l) => l.id.Equals(locationParameter.id)).Default(0, (i)=> i!=-1);
            //locationNameDropDown.SetValueWithoutNotify(selectedLocIndex);

            //buttonSelectSquadsWindow.interactable = locationParameter.type == LocationParamter.Type.friend;
            //if (locationParameter.type == LocationParamter.Type.enemy)
            //{
            //    defenceValue.ShowTextLabel();
            //    defenceValue.textMesh.SetText("XXX");
            //    recoveryValue.ShowTextLabel();
            //    recoveryValue.textMesh.SetText("XXX");
            //}
            //else
            //{
            //    defenceValue.ShowTextLabel();
            //    defenceValue.textMesh.SetText("XXX");
            //    recoveryValue.ShowTextLabel();
            //    recoveryValue.textMesh.SetText("XXX");
            //}
        }

        /// <summary>
        /// LocationDropdownの値が変更された時
        /// </summary>
        /// <param name="index"></param>
        private void LocationDorpdownChanged(int index)
        {
            // TODO: Dropdownも消えると不格好なので、Dropdown以下の情報系だけFadeout in するように
            //GameManager.Instance._uiGeneralParameters.selectedLocation = locations[index];
            //var seq = DOTween.Sequence();
            //seq.Append(canvasGroup.DOFade(0, 0.25f).OnComplete(() => SetInfomation()));
            //seq.Append(canvasGroup.DOFade(1, 0.25f));
            //seq.Play();
        }
    }
}