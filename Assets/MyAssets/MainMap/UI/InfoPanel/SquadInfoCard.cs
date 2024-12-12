using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using static Utility;
using System;

namespace MainMap.UI.InfoPanel
{
    /// <summary>
    /// Tableの上側に表示するInfoCard
    /// </summary>
    internal class SquadInfoCard : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI squadName;
        [SerializeField] TextMeshProUGUI commanderLabel;
        [SerializeField] TextMeshProUGUI energyScore;
        [SerializeField] ProgressBar energyProgressBar;
        [SerializeField] TextMeshProUGUI memberCount;
        [SerializeField] Image StateIconImage;
        [SerializeField] internal Button ReturnSquadButton;
        [Tooltip("状態とアイコンの紐づけ")]
        [SerializeField] List<StateAndIcon> StateAndIcons;

        internal RectTransform RectTransform;
        private float shownXPosition;
        private float duration = 0.3f;
        /// <summary>
        /// Cardの表示対象となるSquad
        /// </summary>
        public MapSquad Squad { private set; get; }
        public bool IsShown { private set; get; } = false;

        internal CanvasGroup CanvasGroup;
        /// <summary>
        /// Interactive=faleのときのalpha
        /// </summary>
        internal readonly float disabledAlpha = 0.2f;
        /// <summary>
        /// Interactive系のアニメーションを行っている場合のseqence
        /// </summary>
        private Tween interactiveAnimation;

        private bool IsSquadMoveing = false;

        private bool endSupplyCoroutine = false;
        /// <summary>
        /// Cardの選択ボタン
        /// </summary>
        internal Button Button;
        /// <summary>
        /// Cardの表示している現在のState
        /// </summary>
        private MapSquad.State CurrentState = MapSquad.State.Waiting;
        /// <summary>
        /// Iconのアニメーション
        /// </summary>
        private Sequence IconAnimation;

        /// <summary>
        /// グレイアウト状態を管理
        /// </summary>
        public bool Interactive
        {
            get => _interactive;
            set
            {
                _interactive = value;
                CanvasGroup.alpha = value ? 1 : disabledAlpha;
            }
        }
        private bool _interactive;

        protected private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            shownXPosition = RectTransform.anchoredPosition.x;

            var position = RectTransform.anchoredPosition;
            position.x = RectTransform.rect.width;
            RectTransform.anchoredPosition = position;

            CanvasGroup = GetComponent<CanvasGroup>();
            CanvasGroup.alpha = 0;
            Button = GetComponent<Button>();
        }

        protected private void Start()
        {
            StateIconImage.color = Color.clear;
        }

        /// <summary>
        /// SquadInfoCardにSquadの情報をセットする
        /// </summary>
        /// <param name="squad"></param>
        internal void SetInfomation(MapSquad squad)
        {

            this.Squad = squad;
            UpdateInfo();
        }

        /// <summary>
        /// Cardの内容を更新する
        /// </summary>
        internal void UpdateInfo()
        {
            squadName.SetText(Squad.TeamName);
            commanderLabel.SetText(Squad.Commander.Name);

            IEnumerator SetSupplyValue(float newValue, float oldValue, float duration)
            {
                if (newValue == oldValue)
                    yield break;
                if (endSupplyCoroutine)
                {
                    endSupplyCoroutine = true;
                    while (endSupplyCoroutine)
                        yield return null;
                }
                endSupplyCoroutine = false;
                newValue = (float)Math.Round(newValue, 1);
                var tick = 0.1f;
                var length = Mathf.Abs(oldValue - newValue) / tick;
                var count = 0;
                var test = new List<float>();
                while (count <= length)
                {
                    if (endSupplyCoroutine)
                    {
                        endSupplyCoroutine = false;
                        break;
                    }
                    var v = 0f;
                    if (oldValue < newValue)
                        v = oldValue + (count * tick);
                    else if (oldValue > newValue)
                        v = oldValue - (count * tick);
                    else
                        break;
                    test.Add(v);
                    energyScore.SetText(v);
                    count++;
                    yield return new WaitForSeconds(duration / length);
                    var d = duration / length * 1000;
                    var time = DateTime.Now;
                    while((DateTime.Now - time).Milliseconds < d)
                    {
                        if (endSupplyCoroutine)
                            break;
                        yield return null;
                    }
                }
                endSupplyCoroutine = false;
            }

            energyScore.SetText(Squad.data.DaysOfRemainingSupply);
            //if (isSquadMoveing != squad.IsMoving)
            //{
            //    var oldValue = float.Parse(energyScore.text);
            //    StartCoroutine(SetSupplyValue(supply, oldValue, 1f));
            //}
            //else
            //{
            //    energyScore.SetText(supply);
            //}
            if (CurrentState != Squad.SquadState)
            {
                IsSquadMoveing = Squad.SquadState == MapSquad.State.Walking;
                CurrentState = Squad.SquadState;
                if (StateAndIcons.TryFindFirst(i => i.State == Squad.SquadState, out var stateAndIcon))
                {
                    if (IconAnimation != null && IconAnimation.IsActive())
                        IconAnimation.Kill();
                    IconAnimation = DOTween.Sequence();
                    if (StateIconImage.sprite != null)
                    {
                        IconAnimation.Append(StateIconImage.DOColor(Color.clear, 0.3f));
                    }
                    IconAnimation.Append(StateIconImage.DOColor(stateAndIcon.color, 0.3f).OnStart(() =>
                    {
                        StateIconImage.sprite = stateAndIcon.Icon;
                    }));
                    IconAnimation.Play();
                }
            }

            energyProgressBar.rate = Squad.data.supplyLevel / (float)Squad.data.MaxSupply;
            memberCount.SetText($"{Squad.Members.Count + 1}/{Squad.data.maxMemberCount + 1}");
        }



        /// <summary>
        /// InteractiveModeの状態にするアニメーションを返す
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        public Tween GetInteractiveAnimation(bool active)
        {
            if (interactiveAnimation != null && interactiveAnimation.IsActive())
                interactiveAnimation.Kill();
            if (_interactive != active)
                interactiveAnimation = CanvasGroup.DOFade(active ? 1 : disabledAlpha, 0.3f);
            else
                interactiveAnimation = RectTransform.DOShakeX().SetDelay(1f);
            _interactive = active;
            return interactiveAnimation;
        }

        /// <summary>
        /// Squadの状態とIconの画像のセット
        /// </summary>
        [Serializable] public class StateAndIcon
        {
            public MapSquad.State State;
            public Sprite Icon;
            public Color color;
        }

    }
}
