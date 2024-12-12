using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using static Utility;

namespace MainMap.UI.InfoPanel
{
    /// <summary>
    /// SideCanvasに表示するSquadのcardの内容
    /// </summary>
    public class SquadsInfoPanel : MonoBehaviour
    {
        [SerializeField] SquadInfoCard SquadInfoCardTemplate;

        List<GameObject> cardHolders;
        RectTransform startPosition;
        readonly List<SquadInfoCard> squadInfoCards = new List<SquadInfoCard>();

        CanvasGroup CanvasGroup;

        /// <summary>
        /// Cardが選択されたときの呼び出し
        /// </summary>
        internal Action<MapSquad> CardIsSelectedAction;
        /// <summary>
        /// CardからSquadを帰還させる
        /// </summary>
        internal Action<MapSquad> ReturnSquadAction;

        // Start is called before the first frame update
        protected void Start()
        {
            cardHolders = transform.GetChildren();
            startPosition = cardHolders.Last().GetComponent<RectTransform>();
            cardHolders.RemoveAt(cardHolders.LastIndex());
            SquadInfoCardTemplate.CanvasGroup.alpha = 0;
            CanvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Panelを表示する
        /// </summary>
        public void Show()
        {
            CanvasGroup.DOFade(1, 0.5f);
        }

        /// <summary>
        /// Panelを非表示状態にする
        /// </summary>
        public void Hide()
        {
            CanvasGroup.DOFade(0, 0.5f);
        }

        /// <summary>
        /// すべてのCardを削除する
        /// </summary>
        public void Clear()
        {
            squadInfoCards.ForEach(c => Destroy(c.gameObject));
            squadInfoCards.Clear();
        }

        /// <summary>
        /// InfoPanelにSquadのcardを表示する
        /// </summary>
        /// <param name="squad"></param>
        public void AddWithAnimation(MapSquad squad)
        {
            var holder = cardHolders[squadInfoCards.Count];
            var card = Instantiate(SquadInfoCardTemplate, startPosition);
            card.SetInfomation(squad);
            squadInfoCards.Add(card);
            card.RectTransform.anchoredPosition = Vector2.zero;
            card.transform.SetParent(holder.transform);
            var seq = DOTween.Sequence();
            seq.Append(card.CanvasGroup.DOFade(card.disabledAlpha, 0.5f));
            seq.Join(card.RectTransform.DOAnchorPosX(0, 0.5f));
            seq.SetDelay(1f);
            seq.Play();
            card.Button.onClick.AddListener(() => CardIsSelectedAction?.Invoke(squad));
            card.ReturnSquadButton.onClick.AddListener(() => ReturnSquadAction?.Invoke(squad));
        }

        /// <summary>
        /// アニメーションなしでInfopanlにSquadのcardを表示する
        /// </summary>
        /// <param name="squad"></param>
        public void Add(MapSquad squad)
        {
            var holder = cardHolders[squadInfoCards.Count];
            var card = Instantiate(SquadInfoCardTemplate, holder.transform);
            card.CanvasGroup.alpha = card.disabledAlpha;
            squadInfoCards.Add(card);
            card.SetInfomation(squad);
            card.RectTransform.anchoredPosition = Vector2.zero;
            card.Button.onClick.AddListener(() => CardIsSelectedAction?.Invoke(squad));
            card.ReturnSquadButton.onClick.AddListener(() => ReturnSquadAction?.Invoke(squad));
        }

        /// <summary>
        /// 対象のsquadのcardをinfoPanelから削除する
        /// </summary>
        /// <param name="squad"></param>
        public void Remove(Squad squad)
        {
            var removeIndex = squadInfoCards.FindIndex(c => c.Squad.data == squad);
            if (removeIndex == -1) return;
            var seq = DOTween.Sequence();
            var removedCard = squadInfoCards[removeIndex];
            squadInfoCards.RemoveAt(removeIndex);
            seq.Append(removedCard.CanvasGroup.DOFade(0, 0.3f).OnComplete(() => Destroy(removedCard.gameObject)));
            
            squadInfoCards.Select((card, index) => (card, index)).ToList().ForEach(item =>
            {
                if (item.card.transform.parent != cardHolders[item.index].transform)
                {
                    item.card.transform.SetParent(cardHolders[item.index].transform);
                    seq.Join(item.card.RectTransform.DOAnchorPosX(0, 0.3f));
                }
            });
            seq.Play();
        }

        /// <summary>
        /// CardをInteractiveModeにする
        /// </summary>
        /// <param name="squad"></param>
        public void SetInteractive(Squad squad, bool interactive)
        {
            var card = squadInfoCards.Find(c => c.Squad.data == squad);
            if (card == null) return;
            card.GetInteractiveAnimation(interactive).Play();
        }

        /// <summary>
        /// Cardの表示内容をすべてUpdateする
        /// </summary>
        public void UpdateCards()
        {
            squadInfoCards.ForEach(c => c.UpdateInfo());
        }
    }
}