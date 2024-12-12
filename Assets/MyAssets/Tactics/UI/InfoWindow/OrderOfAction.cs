using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;
using Tactics.Character;
using static Utility;

namespace Tactics.UI.Overlay
{
    public class OrderOfAction : MonoBehaviour
    {
        [SerializeField] float faceImageSpace;
        [SerializeField] GameObject faceImageTemplate;
        [SerializeField] float animationDuration;
        [SerializeField] Transform layoutGroup;

        private FaceAndName _faceImageTemplate;

        private List<FaceAndName> faceAndNames = new List<FaceAndName>();

        private void Awake()
        {
            _faceImageTemplate = new FaceAndName(faceImageTemplate, null);
            _faceImageTemplate.SetScale(0, 0, null) ;
        }

        public void SetOrder(List<Tactics.Character.UnitController> order)
        {
            faceAndNames.ForEach((f) => Destroy(f.gameObject));
            faceAndNames.Clear();

            foreach(var p in order)
            {
            }

            for (var i= 0; i< order.Count; i++)
            {
                var newObj = Instantiate<GameObject>(faceImageTemplate, layoutGroup);
                var faceAndName = new FaceAndName(newObj, order[i]);
                newObj.transform.localPosition = new Vector3(i * faceImageSpace, 0);
                faceAndName.SetImage();
                faceAndName.SetScale(1, animationDuration, null);
                faceAndNames.Add(faceAndName);
            }
        }

        /// <summary>
        /// FaceImagesを初期化
        /// </summary>
        public void Clear()
        {
            faceAndNames.ForEach(f =>
            {
                Destroy(f.gameObject);
            });
            faceAndNames.Clear();
        }

        /// <summary>
        /// FaceImageを後ろに追加
        /// </summary>
        /// <param name="unitController"></param>
        public void Add(UnitController unitController)
        {
            var newObj = Instantiate<GameObject>(faceImageTemplate, layoutGroup);
            var faceAndName = new FaceAndName(newObj, unitController);
            var lastX = 0f;
            if (faceAndNames.Count != 0)
                lastX = faceAndNames[faceAndNames.Count - 1].gameObject.transform.localPosition.x;

            newObj.transform.localPosition = new Vector3(lastX + faceImageSpace, 0);

            faceAndName.SetImage();
            faceAndName.SetScale(1, animationDuration, null);
            faceAndNames.Add(faceAndName);
        }

        public void RemoveFirst(Action OnComplete)
        {
            if (faceAndNames.IndexAt_Bug(0, out var output))
            {
                faceAndNames.RemoveRange(0, 1);
                output.SetScale(0, animationDuration, () =>
                {
                    Destroy(output.gameObject);
                });

                var seq = DOTween.Sequence();
                for(var i=0; i<faceAndNames.Count; i++)
                {
                    seq.Append(faceAndNames[i].gameObject.transform.DOLocalMoveX(i * faceImageSpace, animationDuration));
                }
                seq.OnComplete(() => OnComplete?.Invoke());
                seq.Play();
            }
        }

        public void RemoveAt(UnitController unitParameter)
        {
            var match = faceAndNames.FindAll(f => f.unit.Equals(unitParameter));
            faceAndNames.RemoveAll(f => f.unit.Equals(unitParameter));
            foreach(var m in match)
            {
                m.SetScale(0, animationDuration, () =>
                {
                    Destroy(m.gameObject);
                });
            }
        }

        class FaceAndName
        {
            internal UnitController unit;
            internal GameObject gameObject;
            internal Image faceImage;
            internal RectTransform faceImageRect;
            internal TextMeshProUGUI nameLabel;
            internal RectTransform nameLabelRect;

            internal FaceAndName(GameObject obj, UnitController unitController)
            {
                unit = unitController;
                gameObject = obj;
                faceImage = obj.GetComponent<Image>();
                faceImageRect = obj.GetComponent<RectTransform>();
                nameLabel = obj.GetComponentInChildren<TextMeshProUGUI>();
                nameLabelRect = nameLabel.GetComponent<RectTransform>();
                if (unit != null)
                {
                    nameLabel.SetText(unit.CurrentParameter.Data.Name);
                }
            }

            internal void SetImage()
            {
                if (unit == null)
                    return;
                faceImage.sprite = unit.CurrentParameter.Data.FaceImage;
            }

            internal void SetScale(float scale, float duration, Action OnComplete)
            {
                var seq = DOTween.Sequence();
                seq.Append(faceImageRect.DOScale(new Vector3(scale, scale, scale), duration));
                seq.Join(nameLabelRect.DOScale(new Vector3(scale, scale, scale), duration));
                seq.OnComplete(() => OnComplete?.Invoke());
                seq.Play();
            }
        }
    }
}
