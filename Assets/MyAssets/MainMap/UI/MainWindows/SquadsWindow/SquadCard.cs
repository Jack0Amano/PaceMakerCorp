using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static Utility;
using TMPro;

namespace MainMap.UI.Squads
{
    /// <summary>
    /// SquadListで表示するSquadのリストカード
    /// </summary>
    public class SquadCard : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI squadName;

        [SerializeField] TextMeshProUGUI commanderName;

        [SerializeField] Image commanderImage;

        [SerializeField] TextMeshProUGUI unitCount;

        [SerializeField] internal RectTransform baseTransform;

        [SerializeField] GameObject statusBar;

        [SerializeField] internal Button actionButton;

        [SerializeField] TextMeshProUGUI statusBarLabel;
        
        public Button SelectSquadButton { private set; get; }

        [SerializeField] public Button removeSquadButton;

        private float animationTime = 0.5f;

        private float normalPosition = -8.79f;
        private float selectedPosition = 19f;

        public CanvasGroup CanvasGroup { private set; get; }

        public RectTransform RectTransform { private set; get; }


        public Squad Squad { private set; get; }

        private bool _isSelected = false;

        /// <summary>
        /// カードを選択し移動アニメーションが再生される
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;

            set
            {
                if (IsSelected == value) return;

                _isSelected = value;

                baseTransform
                    .DOLocalMoveX(value ? selectedPosition : normalPosition, animationTime/2)
                    .SetEase(Ease.InOutCubic);
            }
        }

        private DataSavingController gameData;

        private void Awake()
        {
            SelectSquadButton = GetComponent<Button>();
            CanvasGroup = GetComponent<CanvasGroup>();
            RectTransform = GetComponent<RectTransform>();
            gameData = GameManager.Instance.DataSavingController;
        }

        public void ShakeCard()
        {
            var seq = DOTween.Sequence()
                .SetRelative()
                .Append(baseTransform.DOLocalMoveX(-2, animationTime / 4))
                .Append(baseTransform.DOLocalMoveX(2, animationTime / 4))
                .Append(baseTransform.DOLocalMoveX(-2, animationTime / 4)).
                Append(baseTransform.DOLocalMoveX(2, animationTime / 4));

            seq.SetEase(Ease.InOutCubic);
            seq.Play();
        }


        /// <summary>
        /// Squadの情報を描写
        /// </summary>
        /// <param name="squad"></param>
        public void SetSquad(Squad squad)
        {
            this.Squad = squad;
            squadName.SetText(squad.name);
            commanderName.SetText(squad.commander.Name);
            unitCount.SetText($"{1 + squad.member.Count}/{squad.maxMemberCount}");
        }

        /// <summary>
        /// SquadCardの情報を更新
        /// </summary>
        public void UpdateInfo()
        {
            squadName.SetText(Squad.name);
            unitCount.SetText($"{1 + Squad.member.Count}/{Squad.maxMemberCount + 1}");
        }
    }
}