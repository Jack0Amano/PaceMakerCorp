using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Tactics.Character;
using DG.Tweening;
using UnityEngine.UI;
using System.Net;
using System;
using static Utility;
using Unity.VisualScripting;
using Sequence = DG.Tweening.Sequence;

namespace Tactics.UI.Overlay
{
    public class MyInfoWindow : MonoBehaviour
    {
        [Header("Unit Info")]
        [SerializeField] TextMeshProUGUI nameLabel;
        [SerializeField] ProgressBar hpProgressBar;
        [SerializeField] TextMeshProUGUI hpValue;
        [Tooltip("カウンター攻撃可能かどうかを表示するアイコン")]
        [SerializeField] Image counterAttackIcon;
        [Tooltip("カバー状態を表示するアイコン")]
        [SerializeField] Image onCoverIcon;

        /// <summary>
        /// OnCoverIconの表示状態を管理するクラス
        /// </summary>
        public AutoShowHideIcon OnCoverAutoShowHideIcon;
        /// <summary>
        /// CounterAttackIconの表示状態を管理するクラス
        /// </summary>
        public AutoShowHideIcon CounterAttackAutoShowHideIcon;

        private void Awake()
        {
            var notCounterAttackIcon = counterAttackIcon.transform.GetChild(0).GetComponent<Image>();
            CounterAttackAutoShowHideIcon = new AutoShowHideIcon(counterAttackIcon, notCounterAttackIcon);

            var notOnCoverIcon = onCoverIcon.transform.GetChild(0).GetComponent<Image>();
            OnCoverAutoShowHideIcon = new AutoShowHideIcon(onCoverIcon, notOnCoverIcon);
        }

        private void Update()
        {
            CounterAttackAutoShowHideIcon.Update();
            OnCoverAutoShowHideIcon.Update();
        }

        #region Show unit info
        /// <summary>
        /// 操作中のUnitの情報を表示する
        /// </summary>
        /// <param name="unit"></param>
        public void SetParameter(UnitController unit)
        {
            nameLabel.SetText(unit.CurrentParameter.Data.Name);
            hpValue.SetText(unit.CurrentParameter.HealthPoint);
            hpProgressBar.SetRateWithAnimation(unit.CurrentParameter.HealthPoint/unit.CurrentParameter.TotalHealthPoint);
        }
        #endregion

    }

    /// <summary>
    /// 呼び出し続けないと自動で非表示になるアイコン
    /// </summary>
    public class AutoShowHideIcon
    {
        /// <summary>
        /// Iconがアクティブな状態であることを示すアイコン
        /// </summary>
        public Image ActiveIcon { get; private set; }
        /// <summary>
        /// Iconが非アクティブな状態であることを示すアイコン (default)
        /// </summary>
        public Image DisactiveIcon { get; private set; }

        /// <summary>
        /// アイコンの表示状態
        /// </summary>
        private bool isActive = false;
        /// <summary>
        /// Activeアイコンの表示を開始したフレーム
        /// </summary>
        private int lastFrameOfShowActiveIcon = 0;
        /// <summary>
        /// 最後にShowActiveIconが呼び出されたフレーム　このフレームが更新されない状態が5フレーム続いたらIcon非表示の時間のカウントを行い始める
        /// </summary>
        private int lastFrameOfCallShowActiveIcon = 0;
        private Sequence showHideIconSequence;
        /// <summary>
        /// アイコンの表示・非表示のアニメーション時間
        /// </summary>
        public float IconAnimationDuration = 0.5f;
        /// <summary>
        /// ActiveIconの表示状態でのアルファ値
        /// </summary>
        public float AppearActiveIconAlpha = 1;
        /// <summary>
        /// DisactiveIconの表示状態でのアルファ値
        /// </summary>
        public float AppearDisactiveIconAlpha = 0.6f;

        public AutoShowHideIcon(Image activeIcon, Image disactiveIcon)
        {
            lastFrameOfShowActiveIcon = Time.frameCount;
            lastFrameOfCallShowActiveIcon = Time.frameCount;
            ActiveIcon = activeIcon;
            DisactiveIcon = disactiveIcon;
        }

        //<summary>
        // カウンター攻撃可能な場合はActiveIconをフェードインし負荷の場合はDisactiveIconをフェードインする
        //</summary>
        public bool IsActive
        {
            get => isActive;
            private set
            {
                if (isActive == value &&
                   (ActiveIcon.color.a != 0 ||
                    DisactiveIcon.color.a != 0))
                     return;
                isActive = value;

                // ShowHideCounteAttackIconSequenceが再生中であればキャンセルする
                if (showHideIconSequence != null && showHideIconSequence.IsActive())
                {
                    showHideIconSequence.Kill();
                }

                showHideIconSequence = DOTween.Sequence();
                if (value)
                {
                    showHideIconSequence.Append(ActiveIcon.DOFade(AppearActiveIconAlpha, IconAnimationDuration));
                    showHideIconSequence.Join(DisactiveIcon.DOFade(0.0f, IconAnimationDuration));
                }
                else
                {
                    showHideIconSequence.Append(ActiveIcon.DOFade(0.0f, IconAnimationDuration));
                    showHideIconSequence.Join(DisactiveIcon.DOFade(AppearDisactiveIconAlpha, IconAnimationDuration));
                }
                showHideIconSequence.Play();
            }
        }

        /// <summary>
        /// この関数をfadeDuration秒以上呼び出し続けたときにカウンター攻撃可能な場合はActiveIconをフェードインし負荷の場合はDisactiveIconをフェードインする
        /// </summary>
        public void ShowActiveIcon(bool isShow)
        {
            if (Time.frameCount - lastFrameOfCallShowActiveIcon > 5)
            {
                lastFrameOfCallShowActiveIcon = Time.frameCount;
                lastFrameOfShowActiveIcon = Time.frameCount;
                return;
            }
            lastFrameOfCallShowActiveIcon = Time.frameCount;

            if ((Time.frameCount - lastFrameOfShowActiveIcon) * Time.deltaTime > IconAnimationDuration)
            {
                //Print("ShowCounterAttackIcon", isShow);
                lastFrameOfShowActiveIcon = Time.frameCount;
                IsActive = isShow;
            }
        }

        /// <summary>
        /// Updateで呼び出すこと
        /// </summary>
        public void Update()
        {
            if (IsActive == false)
                return;

              // ShowCouterAttackIconの呼び出しがfadeDuration秒以上続いていなければCounterAttackIconとNotCounterAttackIconをフェードアウトする
            if ((Time.frameCount - lastFrameOfCallShowActiveIcon) * Time.deltaTime > IconAnimationDuration * 2)
            {
                IsActive = false;
            }
        }
    }

}