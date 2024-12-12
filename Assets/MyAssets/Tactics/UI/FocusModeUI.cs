using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Tactics.Character;
using DG.Tweening;
using static Utility;

namespace Tactics.UI.Overlay
{
    public class FocusModeUI : MonoBehaviour
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
        int ShowMessageCount = 0;
        /// <summary>
        /// Messageを表示したFrame 表示されたときにその時のFrameが置かれる
        /// </summary>
        int AppearMessageFrame = 0;
        /// <summary>
        /// ShowMessage()を呼んだ最後のFrame Callの連続性がなければShowMessageCountは0になる そのためのFrameCount
        /// </summary>
        int CallShowMessageLastFrame = 0;
        /// <summary>
        /// ShowMessage()を呼んだunitController 別のUnitControllerが呼べばMessageは一度引っ込められて再表示される
        /// </summary>
        UnitController ShowMessageUnitController;
        Sequence ShowMessageAnimation;

        /// <summary>
        /// TargetUIが表示されているかどうか
        /// </summary>
        public bool IsForcusModeUIActive = false;

        /// <summary>
        /// 下部メッセージが表示されているかどうか
        /// </summary>
        public bool IsBottomMessageActive = false;

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
        }

        protected private void Start()
        {
            manTargetUI.DOFade(0, 0);
            tankTargetUI.DOFade(0, 0);
            nameLabel.DOFade(0, 0);
            targetUIRect.DOAnchorPosY(-targetUIRect.rect.height, 0);
            manTargetUI.gameObject.SetActive(false);
            tankTargetUI.gameObject.SetActive(false);
            gameObject.SetActive(false);
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
            UpdateBottomMessageTimer();
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
            aimLineController.SetAimLineActive(targeted[currentTargetIndex]);
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

            aimLineController.SetAllAimLineActive();
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

        /// <summary>
        /// TargetUI内のテキストの内容を消す
        /// </summary>
        private void HideTexts()
        {
            if (textSequence != null && textSequence.IsActive())
                textSequence.Kill();

            textSequence = DOTween.Sequence();
            textSequence.Append(nameLabel.DOFade(0, 0.5f));

            if (manTargetUI.gameObject.activeSelf)
                textSequence.Join(manTargetUI.DOFade(0, 0.5f));
            else if (tankTargetUI.gameObject.activeSelf)
                textSequence.Join(tankTargetUI.DOFade(0, 0.5f));

            textSequence.OnComplete(() =>
            {
                nameLabel.SetText("");
                tankTargetUI.gameObject.SetActive(false);
                manTargetUI.gameObject.SetActive(false);
            });

            textSequence.Play();
        }
        #endregion

        #region TargetUnitを次のにする
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
            aimLineController.SetAimLineActive(target);
        }

        #endregion

        #region 下部メッセージの表示
        /// <summary>
        /// <c>ShowHideMessageCount</c>の間ShowMesasgeを連続して呼び続けているときMessageを表示  
        /// このShowMessageを呼んでいないFrameが続いた場合Messageは自動で取り下げられる
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detail"></param>
        /// <param name="unitController"></param>
        public void ShowBottomMessage(string message, string detail, UnitController unitController)
        {
            if (IsForcusModeUIActive)
                return;

            if (Time.frameCount - CallShowMessageLastFrame > 10 || ShowMessageUnitController != unitController)
            {
                ShowMessageUnitController = unitController;
                CallShowMessageLastFrame = Time.frameCount;
                ShowMessageCount = 0;
            }
            else
            {
                ShowMessageCount++;
                CallShowMessageLastFrame = Time.frameCount;
            }
            if (ShowMessageAnimation != null && ShowMessageAnimation.IsActive())
                return;

            // 新たなmessageを呼び出しの際
            if (ShowMessageCount > ShowHideMessageCount && !MessageLabel.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
                IsBottomMessageActive = true;

                ShowMessageCount = 0;
                MessageLabel.gameObject.SetActive(true);
                MessageLabel.text = message; SmallMessageLabel.alpha = 0;
                MessageLabel.alpha = 0;
                SmallMessageLabel.gameObject.SetActive(true);
                SmallMessageLabel.text = detail;

                ShowMessageUnitController = unitController;

                if (ShowMessageAnimation != null && ShowMessageAnimation.IsActive())
                    ShowMessageAnimation.Kill();
                ShowMessageAnimation = DOTween.Sequence();

                ShowMessageAnimation.Join(SmallMessageLabel.DOFade(1, 0.25f));
                ShowMessageAnimation.Join(MessageLabel.DOFade(1, 0.25f));
                ShowMessageAnimation.OnComplete(() => AppearMessageFrame = Time.frameCount);
                ShowMessageAnimation.Play();
                return;
            }


            // 呼び出したUnitが違う場合
            if (((ShowMessageUnitController != null && ShowMessageUnitController != unitController) ||
                 MessageLabel.text != message ||
                 SmallMessageLabel.text != detail) &&
                Time.frameCount - AppearMessageFrame > ShowHideMessageCount * 2)
            {
                IsBottomMessageActive = true;

                if (ShowMessageAnimation != null && ShowMessageAnimation.IsActive())
                    ShowMessageAnimation.Kill();
                ShowMessageAnimation = DOTween.Sequence();

                ShowMessageUnitController = unitController;

                ShowMessageAnimation.Append(SmallMessageLabel.DOFade(0, 0.25f));
                ShowMessageAnimation.Join(MessageLabel.DOFade(0, 0.25f).OnComplete(() =>
                {
                    SmallMessageLabel.text = detail;
                    MessageLabel.text = message;
                }));
                ShowMessageAnimation.Append(SmallMessageLabel.DOFade(1, 0.25f));
                ShowMessageAnimation.Join(MessageLabel.DOFade(1, 0.25f).OnComplete(() =>
                {
                    AppearMessageFrame = Time.frameCount;
                }));
                ShowMessageAnimation.Play();
                ShowMessageCount = 0;
                return;
            }
        }

        /// <summary>
        /// Messageの表示タイマー
        /// </summary>
        void UpdateBottomMessageTimer()
        {
            void Hide()
            {
                IsBottomMessageActive = false;
                ShowMessageAnimation = DOTween.Sequence();
                ShowMessageAnimation.Join(SmallMessageLabel.DOFade(0, 0.25f));
                ShowMessageAnimation.Join(MessageLabel.DOFade(0, 0.25f));
                ShowMessageAnimation.OnComplete(() =>
                {
                    SmallMessageLabel.gameObject.SetActive(false);
                    MessageLabel.gameObject.SetActive(false);
                    SmallMessageLabel.text = "";
                    MessageLabel.text = "";
                    ShowMessageUnitController = null;
                    //gameObject.SetActive(false);
                });
                ShowMessageAnimation.Play();
            }


            if (!IsBottomMessageActive)
                return;
            if (ShowMessageAnimation != null && ShowMessageAnimation.IsActive())
                return;

            if (MessageLabel.gameObject.activeSelf && Time.frameCount - CallShowMessageLastFrame > ShowHideMessageCount)
            {
                // ShowMessage()の呼び出しがしばらく行われなかったらHideする
                Hide();
            }
            else if (IsForcusModeUIActive)
            {
                // TargetUIが表示されているときはMessageをHideする
                Hide();
            }
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
    public class Target: EventArgs
    {
        public UnitController targetUnit;
        public TargetPartType partType;
        /// <summary>
        /// 0~1
        /// </summary>
        public float percentage;
        public int damage = 0;
    }

    /// <summary>
    /// HUDのターゲットの部位選択UIのパーツ毎のclass
    /// </summary>
    [Serializable]
    public class TargetPartUI
    {
        public TargetPartType targetPartType;
        public Button labelButton;
        public TextMeshProUGUI percentageLabel;
    }
}