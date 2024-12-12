using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Tactics.AI;
using static Utility;

namespace Tactics.UI.Overlay.FindOutUI
{
    /// <summary>
    /// 頭上に現れる!とか?とかのclass
    /// </summary>
    public class HeadUP : MonoBehaviour
    {
        [SerializeField] GameObject questionPanel;
        [SerializeField] GameObject questionInnerPanel;
        [SerializeField] GameObject exculamationPanel;
        [Tooltip("Exculamationマークを表示する最小時間")]
        [SerializeField] float minExculamationTime = 2;

        Material questionMaterial;
        Material exculamationMaterial;

        Transform cameraTransform;

        /// <summary>
        /// 頭上アイコンがどのような表示になっているか
        /// </summary>
        public FindOutType type { private set; get; } = FindOutType.None;
        /// <summary>
        /// 頭上アイコンが現在フェードアニメーション中か
        /// </summary>
        bool isFadeAnimating = false;

        GeneralParameter parameter;
        static readonly float distanceStartResize = 8;
        /// <summary>
        /// Exculamationを表示した時の時刻
        /// </summary>
        float ShowExculamationTime = 0;

        float defaultSize;
        float IconSize
        {
            get
            {
                var distFromCam = Vector3.Distance(transform.position, cameraTransform.position);
                if (distFromCam > distanceStartResize)
                    return (distFromCam - distanceStartResize) * parameter.HeadUpIconSizeRate + defaultSize;
                return defaultSize;
            }
        }

        private void Awake()
        {
            parameter = GameManager.Instance.GeneralParameter;
            defaultSize = questionPanel.transform.localScale.y;
        }

        // Start is called before the first frame update
        void Start()
        {
            questionMaterial = questionInnerPanel.GetComponent<MeshRenderer>().material;
            exculamationMaterial = exculamationPanel.GetComponent<MeshRenderer>().material;

            cameraTransform = Camera.main.transform;

            questionPanel.transform.localScale = Vector3.zero;
            questionInnerPanel.transform.localScale = Vector3.zero;
            exculamationPanel.transform.localScale = Vector3.zero;
        }

        private void Update()
        {

            if (type != FindOutType.None)
            {
                transform.LookAt(cameraTransform);
                var size = IconSize;

                if (!isFadeAnimating)
                {
                    if (type == FindOutType.Exculamation && exculamationPanel.transform.localScale.x != size)
                    {
                        exculamationPanel.transform.localScale = new Vector3(size, size, size);
                    }
                    else if (type == FindOutType.Question && questionPanel.transform.localScale.x != size)
                    {
                        var newSize = new Vector3(size, size, size);
                        questionPanel.transform.localScale = newSize;
                        questionInnerPanel.transform.localScale = newSize;
                    }
                        
                }
            }
                
        }

        /// <summary>
        /// Questionマークを指定したレベルで表示
        /// </summary>
        /// <param name="level">0~1で</param>
        public void ShowQuestion(float level)
        {

            exculamationPanel.transform.localScale = Vector3.zero;
            if (type != FindOutType.Question && !isFadeAnimating)
            {
                type = FindOutType.Question;
                isFadeAnimating = true;
                var seq = DOTween.Sequence();
                var size = IconSize;
                seq.Append(questionPanel.transform.DOScale(size, 0.2f));
                seq.Join(questionInnerPanel.transform.DOScale(size, 0.2f));
                seq.OnComplete(() =>
                {
                    questionMaterial.SetFloat("_Level", level);
                    isFadeAnimating = false;
                });
                seq.Play();
            }
            else
            {
                questionMaterial.SetFloat("_Level", level);
            }
        }

        /// <summary>
        /// Questionマークを消してExculamationマークを表示
        /// </summary>
        /// <returns>新たにExculamationマークを出現させる場合true</returns>
        public bool ShowExculamation()
        {
            if (type == FindOutType.Exculamation || type == FindOutType.AlreadyFinded)
                return false;

            type = FindOutType.Exculamation;

            questionMaterial.SetFloat("_Level", 0);
            var seq = DOTween.Sequence();
            seq.Append(questionPanel.transform.DOScale(0, 0.2f));
            seq.Join(questionInnerPanel.transform.DOScale(0, 0.2f));
            seq.Join(exculamationPanel.transform.DOScale(IconSize, 0.2f));
            seq.OnComplete(() => isFadeAnimating = false);
            isFadeAnimating = true;
            seq.Play();

            ShowExculamationTime = Time.time;
            return true;
        }

        /// <summary>
        /// マークを非表示にする
        /// </summary>
        public void Hide()
        {
            if (type == FindOutType.None)
                return;

            type = FindOutType.None;
            questionMaterial.SetFloat("_Level", 0);
            var seq = DOTween.Sequence();
            var showTime = Time.time - ShowExculamationTime;
            if (ShowExculamationTime != 0 && showTime < minExculamationTime)
                seq.SetDelay(minExculamationTime - showTime);
            seq.Append(questionPanel.transform.DOScale(0, 0.2f));
            seq.Join(questionInnerPanel.transform.DOScale(0, 0.2f));
            seq.Join(exculamationPanel.transform.DOScale(0, 0.2f));
            seq.Play();
        }
    }
}