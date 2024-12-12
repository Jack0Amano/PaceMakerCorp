using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Tactics.Character;
using DG.Tweening;
using static Utility;
using UnityEditor.Build.Pipeline.Tasks;
using Febucci.UI;

namespace Tactics.UI.Overlay
{
    public class BottomUIPanel : MonoBehaviour
    {
        [SerializeField] GameObject HUDLabelPrehub;
        [SerializeField] float damageShowSecond = 1f;
        [SerializeField] private Vector3 HUDOffset = new Vector3(0, 1.8f, 0);
        [SerializeField] TextMeshProUGUI nameLabel;
        [SerializeField] RectTransform targetUIRect;
        [SerializeField] CanvasGroup manTargetUI;
        [SerializeField] CanvasGroup tankTargetUI;
        [SerializeField] List<TargetPartUI> targetPartUIs;
        [SerializeField] TextMeshProUGUI atkPointLabel;
        [SerializeField] TextMeshProUGUI hpLabel;
        [SerializeField] Button nextTargetButton;
        [SerializeField] Button previousTargetButton;
        [SerializeField] AimLineController aimLineController;
        [SerializeField] TextMeshProUGUI MessageLabel;
        [SerializeField] TextMeshProUGUI SmallMessageLabel;
        [SerializeField] int ShowHideMessageCount = 30;

        private Action<Target> attackAction;
        public UnitController activeUnit { private set; get; }
        private Dictionary<TargetPartType, TargetPartUI> targetPartUIDict;
        private Dictionary<TargetPartType, (int damage, float percentage)> partAndValuesDict;
        private const float HideUITime = 6f;
        private float hideUICurrentTime = 0;
        private Sequence textSequence;
        private ButtonEvents targetUIButton;
        private bool cursorOnTargetUI = false;
        private int currentTargetIndex = 0;
        private List<UnitController> targetedList;

        /// <summary>
        /// Messageを表示するカウント ShowMessageを呼ぶたびにShowMessageCountが増え、これがShowHideMessaeCount以上になれば表示する
        /// </summary>
        int showMessageCount = 0;
        /// <summary>
        /// Messageを表示したFrame 表示されたときにその時のFrameが置かれる
        /// </summary>
        int appearMessageFrame = 0;
        /// <summary>
        /// ShowMessage()を呼んだ最後のFrame Callの連続性がなければShowMessageCountは0になる そのためのFrameCount
        /// </summary>
        int callShowMessageLastFrame = 0;
        /// <summary>
        /// ShowMessage()を呼んだunitController 別のUnitControllerが呼べばMessageは一度引っ込められて再表示される
        /// </summary>
        UnitController showMessageUnitController;
        Sequence showMessageAnimation;

        /// <summary>
        /// TargetUIが表示されているかどうか
        /// </summary>
        public bool IsForcusModeUIActive = false;

        /// <summary>
        /// 下部メッセージが表示されているかどうか
        /// </summary>
        public bool IsBottomMessageActive = false;

        /// <summary>
        /// 下部メッセージの表示・非表示を自動で行う
        /// </summary>
        public AutoShowHideMessage AutoShowHideBottomMessage { get; private set; }
        /// <summary>
        /// メッセージの短期間表示を行うときに使用するコルーチンが呼ばれているかどうか
        /// </summary>
        private bool isOnShowMessageRoutine = false;
        /// <summary>
        /// メッセージの短期間表示を始めたframe
        /// </summary>
        private int startShowMessageFrame;

        protected private void Awake()
        {
            for(var i=0; i<targetPartUIs.Count; i++)
            {
                var ui = targetPartUIs[i];
                ui.labelButton.onClick.AddListener(() => LabelButtonOnClick(ui));
            }

            targetPartUIDict = new Dictionary<TargetPartType, TargetPartUI>();
            partAndValuesDict = new Dictionary<TargetPartType, (int percentage, float damage)>();
            foreach (var t in targetPartUIs)
            {
                targetPartUIDict[t.targetPartType] = t;
                partAndValuesDict[t.targetPartType] = (0,0);
            }

            AutoShowHideBottomMessage = new AutoShowHideMessage(MessageLabel, SmallMessageLabel);
        }

        protected private void Start()
        {
            manTargetUI.DOFade(0, 0);
            tankTargetUI.DOFade(0, 0);
            nameLabel.DOFade(0, 0);
            targetUIRect.DOAnchorPosY(-targetUIRect.rect.height, 0);
            manTargetUI.gameObject.SetActive(false);
            tankTargetUI.gameObject.SetActive(false);
            //gameObject.SetActive(false);
            targetUIButton = targetUIRect.GetComponent<ButtonEvents>();
            targetUIButton.onPointerEnter += ((e) => cursorOnTargetUI = true);
            targetUIButton.onPointerExit += ((e) => cursorOnTargetUI = false);

            nextTargetButton.onClick.AddListener(NextTargetAction);
            previousTargetButton.onClick.AddListener(PreviousTargetAction);

            MessageLabel.gameObject.SetActive(false);
            SmallMessageLabel.gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            AutoShowHideBottomMessage.Update();
        }

        #region TargetUIの表示・非表示
        /// <summary>
        /// TargetUIを表示する
        /// </summary>
        /// <param name="active">TargetUIを使用中のUnit</param>
        /// <param name="targeted">ターゲット可能な敵Unit</param>
        /// <param name="attack">攻撃先の指定を完了したときに呼び出し</param>
        public void Show(UnitController active, List<UnitController> targeted, Action<Target> attack)
        {
            Print("Show target UI", active, $"{targeted.Count} targets");   
            attackAction = attack;
            activeUnit = active;
            nextTargetButton.interactable = targeted.Count > 1;
            previousTargetButton.interactable = targeted.Count > 1;
            currentTargetIndex = 0;

            AutoShowHideBottomMessage.Hide();

            targetedList = targeted;
            if (targeted.Count == 0)
            {
                // 何も表示されない
                return;
            }

            IsForcusModeUIActive = true;

            gameObject.SetActive(true);
            targetUIRect.DOAnchorPosY(0, 0.5f);

            if (targeted[currentTargetIndex].CurrentParameter.Data.UnitType == UnitType.Type.Tank)
            {
                if (manTargetUI.gameObject.activeSelf)
                    manTargetUI.gameObject.SetActive(false);
                DrawTankInfo(targeted[currentTargetIndex]);
            }
            else
            {
                if (tankTargetUI.gameObject.activeSelf)
                    tankTargetUI.gameObject.SetActive(false);
                DrawUnitInfo(targeted[currentTargetIndex]);
            }

            activeUnit.transform.DOLookAt(targeted[currentTargetIndex].transform.position, 0.5f);
            //aimLineController.SetAimLineActive(targeted[currentTargetIndex]);
        }

        /// <summary>
        /// TargetUIを隠す
        /// </summary>
        /// <param name="onComplete">完全に隠し終わった際に呼び出し</param>
        public void Hide(Action onComplete = null)
        {
            if (gameObject.activeSelf == false)
                return;

            IsForcusModeUIActive = false;

            var seq = DG.Tweening.DOTween.Sequence();
            seq.Append(targetUIRect.DOAnchorPosY(-targetUIRect.rect.height, 0.5f));
            seq.OnComplete(() =>
            {
                manTargetUI.DOFade(0, 0);
                tankTargetUI.DOFade(0, 0);
                nameLabel.DOFade(0, 0);
                manTargetUI.gameObject.SetActive(false);
                tankTargetUI.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
            seq.Play();
        }

        /// <summary>
        /// ラベル内のどれかの攻撃方法が選択されたときの呼び出し
        /// </summary>
        /// <param name="targetPartUI"></param>
        private void LabelButtonOnClick(TargetPartUI targetPartUI)
        {
            var target = new Target();
            target.partType = targetPartUI.targetPartType;
            target.targetUnit = targetedList[currentTargetIndex];
            target.percentage = partAndValuesDict[targetPartUI.targetPartType].percentage;
            target.damage = partAndValuesDict[targetPartUI.targetPartType].damage;

            // 攻撃開始
            Hide(() =>
            {
                attackAction?.Invoke(target);
            });
        }


        /// <summary>
        /// TargetUIにUnitの当たり判定を描写する
        /// </summary>
        private void DrawUnitInfo(UnitController target)
        {
            targetUIRect.gameObject.SetActive(true);
            if (!manTargetUI.gameObject.activeSelf)
            {
                manTargetUI.gameObject.SetActive(true);
                if (textSequence != null && textSequence.IsActive())
                    textSequence.Kill();

                textSequence = DOTween.Sequence();
                textSequence.Append(manTargetUI.DOFade(1, 0.5f));
                textSequence.Join(nameLabel.DOFade(1, 0.5f));
                textSequence.Play();
            }

            Print("Focus for Unit", target, "IsCovered", target.IsCoveredFromEnemy(activeUnit.transform.position));

            partAndValuesDict = activeUnit.GetAttackPoint(target);

            targetPartUIDict[TargetPartType.Body].percentageLabel.SetText((int)(partAndValuesDict[TargetPartType.Body].percentage * 100));
            targetPartUIDict[TargetPartType.Head].percentageLabel.SetText((int)(partAndValuesDict[TargetPartType.Head].percentage * 100));
            targetPartUIDict[TargetPartType.Arm].percentageLabel.SetText((int)(partAndValuesDict[TargetPartType.Arm].percentage * 100));
            targetPartUIDict[TargetPartType.Leg].percentageLabel.SetText((int)(partAndValuesDict[TargetPartType.Leg].percentage * 100));
            hpLabel.SetText(target.CurrentParameter.HealthPoint);
            nameLabel.SetText(target.CurrentParameter.Data.Name);
        }

        /// <summary>
        /// TargetUIにTankのあたりを描写する
        /// </summary>
        private void DrawTankInfo(UnitController unitController)
        {

        }
        #endregion

        #region TargetUnitを次のにする
        /// <summary>
        /// Targetの切り替えを行う
        /// </summary>
        private void NextTargetAction()
        {
            currentTargetIndex++;
            if (!targetedList.IndexAt_Bug(currentTargetIndex, out var target))
            {
                currentTargetIndex = 0;
                target = targetedList[currentTargetIndex];
            }
            SetTarget(target);
        }

        /// <summary>
        /// TargetUnitを前のにする
        /// </summary>
        private void PreviousTargetAction()
        {
            currentTargetIndex--;
            if (!targetedList.IndexAt_Bug(currentTargetIndex, out var target))
            {
                currentTargetIndex = targetedList.Count - 1;
                target = targetedList[currentTargetIndex];
            }
            SetTarget(target);
        }

        private void SetTarget(UnitController target)
        {
            if (target.CurrentParameter.Data.UnitType == UnitType.Type.Tank)
            {
                if (manTargetUI.gameObject.activeSelf)
                    manTargetUI.gameObject.SetActive(false);
                tankTargetUI.gameObject.SetActive(true);
                DrawTankInfo(target);
            }
            else
            {
                if (tankTargetUI.gameObject.activeSelf)
                    tankTargetUI.gameObject.SetActive(false);
                manTargetUI.gameObject.SetActive(true);
                DrawUnitInfo(target);
            }
            activeUnit.transform.DOLookAt(target.transform.position, 0.5f);
            //aimLineController.SetAimLineActive(target);
        }

        #endregion

        #region 下部メッセージの表示
        /// <summary>
        /// BottomMessageをDuration秒間表示する
        /// </summary>
        public IEnumerator ShowBottomMessage(string message , string detail, float duration)
        {
            startShowMessageFrame = Time.frameCount;
            if (isOnShowMessageRoutine)
                yield break;
            isOnShowMessageRoutine = true;
            while (true)
            {
                AutoShowHideBottomMessage.Show(true, message, detail);
                // 10フレーム待つ
                yield return null;
                yield return null;
                if ((Time.frameCount - startShowMessageFrame) * Time.deltaTime > duration)
                    break;
            }
            isOnShowMessageRoutine = false;
        }

        /// <summary>
        /// <c>ShowHideMessageCount</c>の間ShowMesasgeを連続して呼び続けているときMessageを表示  
        /// このShowMessageを呼んでいないFrameが続いた場合Messageは自動で取り下げられる
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detail"></param>
        /// <param name="unitController"></param>
        public void ShowBottomMessage(string message, string detail)
        {
            if (isOnShowMessageRoutine || IsForcusModeUIActive)
                return;
            AutoShowHideBottomMessage.Show(true, message, detail);
        }

        #endregion

        /// <summary>
        /// HUDとしてダメージを表示する
        /// </summary>
        /// <param name="target"></param>
        /// <param name="activeCamera"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        internal IEnumerator DamageHUD(Transform target, Camera activeCamera, string text)
        {
            var label = Instantiate(HUDLabelPrehub, Vector3.zero, Quaternion.identity, this.transform);
            var labelText = label.GetComponent<Text>();
            labelText.text = text;

            var rectTrans = label.GetComponent<RectTransform>();
            rectTrans.position = RectTransformUtility.WorldToScreenPoint(activeCamera, target.position + HUDOffset);
            

            yield return new WaitForSeconds(damageShowSecond);

            Destroy(label);
        }

    }

    /// <summary>
    /// ターゲットを指定して攻撃を支持したときのEventのArg
    /// </summary>
    //public class Target: EventArgs
    //{
    //    public UnitController targetUnit;
    //    public TargetPartType partType;
    //    /// <summary>
    //    /// 0~1
    //    /// </summary>
    //    public float percentage;
    //    public int damage = 0;
    //}

    /// <summary>
    /// HUDのターゲットの部位選択UIのパーツ毎のclass
    /// </summary>
    //[Serializable]
    //public class TargetPartUI
    //{
    //    public TargetPartType targetPartType;
    //    public Button labelButton;
    //    public TextMeshProUGUI percentageLabel;
    //}

    /// <summary>
    /// 呼び出し続けないと自動で非表示になるアイコン
    /// </summary>
    public class AutoShowHideMessage
    {
        /// <summary>
        /// Iconがアクティブな状態であることを示すアイコン
        /// </summary>
        private TextMeshProUGUI messageLabel;
        private TypewriterByCharacter messageTextAnimator;
        /// <summary>
        /// Iconが非アクティブな状態であることを示すアイコン (default)
        /// </summary>
        private TextMeshProUGUI detailMessageLabel;
        private TypewriterByCharacter detailMessageTextAnimator;
        /// <summary>
        /// アイコンの表示状態
        /// </summary>
        private bool isActive = false;
        /// <summary>
        /// Activeアイコンの表示を開始したフレーム
        /// </summary>
        private int frameOfStartToShowMessage = 0;
        /// <summary>
        /// 最後にShowActiveIconが呼び出されたフレーム　このフレームが更新されない状態が5フレーム続いたらIcon非表示の時間のカウントを行い始める
        /// </summary>
        private int lastFrameOfCallShowActiveMessage = 0;
        /// <summary>
        /// Messageの表示・非表示のアニメーション
        /// </summary>
        private Sequence showHideMessageSequence;
        /// <summary>
        /// アイコンの表示・非表示のアニメーション時間
        /// </summary>
        public float MessageAnimationDuration = 0.5f;
        /// <summary>
        /// ActiveIconの表示状態でのアルファ値
        /// </summary>
        public float AppearActiveIconAlpha = 1;
        /// <summary>
        /// DisactiveIconの表示状態でのアルファ値
        /// </summary>
        public float AppearDisactiveIconAlpha = 0.6f;

        /// <summary>
        /// メインで表示するメッセージ
        /// </summary>
        public string MainMessageText;
        /// <summary>
        /// サブで表示するメッセージ
        /// </summary>
        public string DetailMessageText;

        public AutoShowHideMessage(TextMeshProUGUI message, TextMeshProUGUI detail)
        {
            frameOfStartToShowMessage = Time.frameCount;
            lastFrameOfCallShowActiveMessage = Time.frameCount;
            this.messageLabel = message;
            detailMessageLabel = detail;
            messageTextAnimator = this.messageLabel.GetComponent<TypewriterByCharacter>();
            detailMessageTextAnimator = detailMessageLabel.GetComponent<TypewriterByCharacter>();
        }

        //<summary>
        // カウンター攻撃可能な場合はActiveIconをフェードインし負荷の場合はDisactiveIconをフェードインする
        //</summary>
        public bool IsActive
        {
            get => isActive;
            private set
            {
                if (isActive == value && messageLabel.color.a != 0 )
                    return;
                isActive = value;

                if (messageTextAnimator && detailMessageTextAnimator)
                {
                    // TextAnimatorがあればそれを使う
                    if (value)
                    {
                        messageLabel.gameObject.SetActive(true);
                        detailMessageLabel.gameObject.SetActive(true);
                        messageTextAnimator.ShowText(MainMessageText);
                        detailMessageTextAnimator.ShowText(DetailMessageText);
                    }
                    else
                    {
                        messageTextAnimator.StartDisappearingText();
                        detailMessageTextAnimator.StartDisappearingText();
                    }
                }
                else
                {
                    // TextAnimatorがなければ普通に表示する
                    // ShowHideCounteAttackIconSequenceが再生中であればキャンセルする
                    if (showHideMessageSequence != null && showHideMessageSequence.IsActive())
                    {
                        showHideMessageSequence.Kill();
                    }

                    showHideMessageSequence = DOTween.Sequence();
                    if (value)
                    {
                        messageLabel.SetText(MainMessageText);
                        detailMessageLabel.SetText(DetailMessageText);
                        messageLabel.gameObject.SetActive(true);
                        detailMessageLabel.gameObject.SetActive(true);
                        showHideMessageSequence.Append(messageLabel.DOFade(AppearActiveIconAlpha, MessageAnimationDuration));
                        showHideMessageSequence.Join(detailMessageLabel.DOFade(AppearActiveIconAlpha, MessageAnimationDuration));
                    }
                    else
                    {
                        showHideMessageSequence.Append(messageLabel.DOFade(0.0f, MessageAnimationDuration));
                        showHideMessageSequence.Join(detailMessageLabel.DOFade(0.0f, MessageAnimationDuration));
                        showHideMessageSequence.OnComplete(() =>
                        {
                            messageLabel.gameObject.SetActive(false);
                            detailMessageLabel.gameObject.SetActive(false);
                        });
                    }
                    showHideMessageSequence.Play();
                }
            }
        }

        /// <summary>
        /// この関数をfadeDuration秒以上呼び出し続けたときにMessageを表示する
        /// </summary>
        public void Show(bool isShow, string main, string detail)
        {
            if (Time.frameCount - lastFrameOfCallShowActiveMessage > 5)
            {
                // 5フレーム以上ShowActiveIconが呼び出されていなければShowHideCounteAttackIconSequenceをキャンセルする
                lastFrameOfCallShowActiveMessage = Time.frameCount;
                frameOfStartToShowMessage = Time.frameCount;
                return;
            }

            lastFrameOfCallShowActiveMessage = Time.frameCount;

            if ((Time.frameCount - frameOfStartToShowMessage) * Time.deltaTime > MessageAnimationDuration)
            {
                if (IsActive != isShow)
                {
                    // 現在の表示状態と異なる場合は表示を切り替える
                    frameOfStartToShowMessage = Time.frameCount;
                    MainMessageText = main;
                    DetailMessageText = detail;
                    IsActive = isShow;
                }
                else if (MainMessageText != main || DetailMessageText != detail)
                {
                    if (IsActive == true)
                    {
                        // 現在の表示メッセージと異なる場合は表示を切り替える
                        MainMessageText = main;
                        DetailMessageText = detail;
                        messageTextAnimator.ShowText(MainMessageText);
                        detailMessageTextAnimator.ShowText(DetailMessageText);
                    }
                }
            }
        }

        public void Hide()
        {
            IsActive = false;
        }

        /// <summary>
        /// Updateで呼び出すこと
        /// </summary>
        public void Update()
        {
            if (isActive == false)
                return;

            // ShowCouterAttackIconの呼び出しがfadeDuration秒以上続いていなければCounterAttackIconとNotCounterAttackIconをフェードアウトする
            if ((Time.frameCount - lastFrameOfCallShowActiveMessage) * Time.deltaTime > MessageAnimationDuration * 2)
            {
                IsActive = false;
            }
        }
    }
}