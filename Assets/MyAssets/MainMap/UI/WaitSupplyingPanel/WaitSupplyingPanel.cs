using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static Utility;
using System;

namespace MainMap.UI.Wait{ 
    /// <summary>
    /// Wait中で時間を早く進行する際に表示するパネル
    /// </summary>
    public class WaitSupplyingPanel : MonoBehaviour
    {
        [SerializeField] MainUIController MainUIController;
        [SerializeField] Image Background;
        [SerializeField] UI.Squads.SquadCard SquadCardTemplate;
        [SerializeField] RectTransform CardHidePosition;
        [SerializeField] RectTransform CardShowPosition;

        private Squads.SquadCard SquadCard;
        private CanvasGroup CanvasGroup;

        /// <summary>
        /// Panelが表示状態であるか
        /// </summary>
        public bool isActive { private set; get; } = false;

        // Start is called before the first frame update
        void Start()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            SquadCard = Instantiate(SquadCardTemplate, transform);
            SquadCard.RectTransform.position = CardHidePosition.position;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 倍速を途中で停止する
        /// </summary>
        public void Stop()
        {
            isActive = false;
        }

        /// <summary>
        /// 条件にTrueを返すまでMainWindowを非表示にし倍速シミュレーションをする
        /// </summary>
        /// <param name="squad"></param>
        /// <param name="speed"></param>
        /// <param name="cardStartPosition"></param>
        public IEnumerator WaitUntil(Squad squad, Squads.SquadCard card, float speed, Predicate<object> until)
        {
            gameObject.SetActive(true);
            isActive = true;

            SquadCard.SetSquad(squad);
            SquadCard.baseTransform.position = card.baseTransform.position;
            SquadCard.CanvasGroup.alpha = 1;
            SquadCard.gameObject.SetActive(true);
            card.CanvasGroup.alpha = 0;
            var seq = DOTween.Sequence();
            seq.Append(SquadCard.baseTransform.DOMove(CardShowPosition.position, 0.8f));
            seq.Join(MainUIController.CanvasGroup.DOFade(0, 0.4f));
            seq.Join(Background.DOColor(Color.clear, 0.4f));
            seq.Play();

            GameManager.Instance.Speed = speed;
            while(!until.Invoke(null))
            {
                if (!isActive) break;
                yield return null;
            }
            GameManager.Instance.Speed = 1;

            seq = DOTween.Sequence();
            seq.Append(SquadCard.baseTransform.DOMove(card.baseTransform.position, 0.8f).OnComplete(() =>
            {
                card.CanvasGroup.alpha = 1;
            }));
            seq.Append(MainUIController.CanvasGroup.DOFade(1, 0.4f));
            seq.Join(Background.DOColor(MainUIController.BackgroundColor, 0.4f));

            yield return seq.Play().WaitForCompletion();
            SquadCard.CanvasGroup.alpha = 0;
            SquadCard.gameObject.SetActive(false);
            SquadCard.baseTransform.position = CardHidePosition.position;
            gameObject.SetActive(false);
            isActive = false;
        }

        /// <summary>
        /// 条件にTrueを返すまでSpeedNumberのLabelを表示し倍速シミュレーションをする
        /// </summary>
        /// <returns></returns>
        public IEnumerator WaitUntil(Squad squad, float speed, Predicate<object> until)
        {
            gameObject.SetActive(true);
            isActive = true;


            GameManager.Instance.Speed = speed;
            while (!until.Invoke(null))
            {
                if (!isActive) break;
                yield return null;
            }
            GameManager.Instance.Speed = 1;

            gameObject.SetActive(false);
            isActive = false;

        }
    }
}