using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Linq;
using UnityEngine.UI;
using System;

namespace Tactics.UI
{
    public class ResultPanel : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI resultLabel;
        [SerializeField] GameObject units;
        [Tooltip("List表示するCell")]
        [SerializeField] UnitInResult originCell;

        [Header("結果ラベル")]
        [SerializeField] TextMeshProUGUI baseExpLabel;
        [SerializeField] TextMeshProUGUI turnLabel;
        [SerializeField] TextMeshProUGUI turnExpLabel;
        [SerializeField] TextMeshProUGUI killCountLabel;
        [SerializeField] TextMeshProUGUI killCountExpLabel;
        [SerializeField] TextMeshProUGUI deadCountLabel;
        [SerializeField] TextMeshProUGUI deadCountExpLabel;
        [SerializeField] CounterLabel totalExpLabel;

        [Header("次に進むボタン")]
        [Tooltip("Retryするボタン")]
        [SerializeField] Button smallRetryButton;
        [SerializeField] Button bigRetryButton;
        [SerializeField] Button returnButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button returnStartMenuButton;

        private List<UnitInResult> cells;
        private CanvasGroup canvasGroup;

        private BattleResult battleResult;
        /// <summary>
        /// TacticsがPrepare画面に戻るときの呼び出し
        /// </summary>
        internal Action ReturnToPrepare;

        /// <summary>
        /// 結果画面が表示されているか
        /// </summary>
        public bool IsResultActive { private set; get; } = false;

        protected void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            // 負けた場合 (Retry, Return to base)
            // 買った場合(Retry, Return to base, Return to Map)
            smallRetryButton.onClick.AddListener(() =>
            {
                IsResultActive = false;
                ReturnToPrepare?.Invoke();
            });
            bigRetryButton.onClick.AddListener(() =>
            {
                IsResultActive  = false;
                ReturnToPrepare?.Invoke();
            });
            returnButton.onClick.AddListener(() => ReturnButtonAction());
            continueButton.onClick.AddListener(() => ContinueAction());

            if (returnStartMenuButton != null)
                returnStartMenuButton.onClick.AddListener(() => ReturnStartMenuAction());
        }

        protected void Start()
        {
            // ResultPanelが最初非表示で行われている際 Showを呼び出し後にSTARTが呼ばれるとGameObjectが非表示になってしまうため
            if (!IsResultActive)
                gameObject.SetActive(false);
        }

        /// <summary> 
        /// リザルト画面を表示する
        /// </summary>
        /// <param name="result"></param>
        public void Show(BattleResult result)
        {
            IsResultActive = true;
            smallRetryButton.interactable = false;
            bigRetryButton.interactable = false;
            returnButton.interactable = false;
            continueButton.interactable = false;
            returnStartMenuButton.interactable = false;

            battleResult = result;
            UserController.enableCursor = true;
            gameObject.SetActive(true);
            if (result.state == VictoryConditions.GameResult.Win)
                ShowWinWindow(result);
            else if (result.state == VictoryConditions.GameResult.Lose)
                ShowLoseWindow(result);
            else if (result.state == VictoryConditions.GameResult.ForceEnd)
                ShowDebugEndWindow(result);
        }

        /// <summary>
        /// 結果を表示する
        /// </summary>
        /// <param name="result"></param>
        private void ShowWinWindow(BattleResult result)
        {
            IsResultActive = true;

            smallRetryButton.gameObject.SetActive(true);
            bigRetryButton.gameObject.SetActive(false);
            returnButton.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(true);
            gameObject.SetActive(true);

            resultLabel.SetText("You Win");

            // Unitの経験値表示部分
            originCell.Set(result.units[0]);
            cells = new List<UnitInResult> { originCell };
            for (var i = 1; i < result.units.Count; i++)
            {
                print(result.units[i]);
                var newCell = Instantiate(originCell.gameObject, units.transform);
                var cell = newCell.GetComponent<UnitInResult>();
                cell.Set(result.units[i]);
                cells.Add(cell);
            }

            var seq = DOTween.Sequence();
            var interval = 0.5f;
            var fadeTime = 0.5f;

            // 全体を表示
            canvasGroup.alpha = 0;
            seq.Append(canvasGroup.DOFade(1, 0.3f));
            seq.AppendInterval(0.4f);

            // BaseExp
            baseExpLabel.alpha = 0;
            baseExpLabel.SetText($"+{result.baseExp} exp");
            seq.Append(baseExpLabel.DOFade(1, fadeTime));

            seq.AppendInterval(interval);

            // ターン経過
            turnLabel.alpha = 0;
            turnExpLabel.alpha = 0;
            turnLabel.SetText(result.numberOfTurn);
            var turnExp = result.TurnExp;
            turnExpLabel.SetText($"+{turnExp} exp");
            seq.Append(turnLabel.DOFade(1, fadeTime));
            seq.Join(turnExpLabel.DOFade(1, fadeTime));

            seq.AppendInterval(interval);

            // Kill Count
            killCountLabel.alpha = 0;
            killCountExpLabel.alpha = 0;
            killCountLabel.SetText(result.killedEnemies.Count);
            var killCountExp = result.killedEnemies.Sum(e => e.Exp);
            killCountExpLabel.SetText($"+{killCountExp} exp");
            seq.Append(killCountLabel.DOFade(1, fadeTime));
            seq.Join(killCountExpLabel.DOFade(1, fadeTime));

            seq.AppendInterval(interval);

            // DeadCount
            deadCountLabel.alpha = 0;
            deadCountExpLabel.alpha = 0;
            deadCountLabel.SetText(result.DeadCount);
            int deadCountExp;
            if (result.DeadCount == 0)
            {
                deadCountExp = 100;
                deadCountExpLabel.SetText($"+{deadCountExp} exp");
            }
            else
            {
                deadCountExp = -result.DeadCount * 100;
                deadCountExpLabel.SetText($"-{deadCountExp} exp");
            }
            seq.Append(deadCountExpLabel.DOFade(1, fadeTime));
            seq.Join(deadCountLabel.DOFade(1, fadeTime));

            // TotalExp
            var totalExp = result.baseExp + turnExp + killCountExp + deadCountExp;
            totalExpLabel.textLabel.SetText("");
            seq.OnComplete(() =>
            {
                StartCoroutine(totalExpLabel.SetCount(0, totalExp, 0.5f));
                foreach (var c in cells)
                {
                    StartCoroutine(c.AddExp(totalExp / cells.Count));
                }

                StartCoroutine(ActivateButtons());
            });
            seq.Play();
        }

        /// <summary>
        /// 負けREsult
        /// </summary>
        /// <param name="result"></param>
        private void ShowLoseWindow(BattleResult result)
        {
            IsResultActive = true;
            smallRetryButton.gameObject.SetActive(false);
            bigRetryButton.gameObject.SetActive(true);
            returnButton.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(false);
            gameObject.SetActive(true);

            resultLabel.SetText("You Lose");

            var seq = DOTween.Sequence();
            var interval = 0.5f;
            var fadeTime = 0.5f;

            // 全体を表示
            canvasGroup.alpha = 0;
            seq.Append(canvasGroup.DOFade(1, 0.3f));
            seq.AppendInterval(0.4f);

            var label2 = smallRetryButton.GetComponentInChildren<TextMeshProUGUI>();
            label2.SetText("Retry");

            seq.Play();
        }

        private void ShowDebugEndWindow(BattleResult result)
        {
            continueButton.gameObject.SetActive(false);
            var label = smallRetryButton.GetComponentInChildren<TextMeshProUGUI>();
            label.SetText("Back to Menu");
            


        }

        /// <summary>
        /// 結果表示のアニメーションが終わるまでボタンのアクティブ化を待つ
        /// </summary>
        /// <returns></returns>
        private IEnumerator ActivateButtons()
        {

            foreach (var c in cells)
            {
                while (true)
                {
                    yield return null;
                    if (!c.isCounting)
                        break;
                }
            }

            // TODO: ボタンのアクティブ化をアニメーション付きで
            smallRetryButton.interactable = true;
            bigRetryButton.interactable = true;
            returnButton.interactable = true;
            continueButton.interactable = true;
            returnStartMenuButton.interactable = true;
        }

        /// <summary>
        /// 結果画面を非表示にする
        /// </summary>
        public void Hide()
        {
            IsResultActive = false;
            gameObject.SetActive(false);
        }

        #region Buttons
        /// <summary>
        /// MainMapに戻るボタン
        /// </summary>
        private void ContinueAction()
        {
            IsResultActive = false;
            StartCoroutine(GameManager.Instance.BackToMainMap(true, false, battleResult.state));
        }

        /// <summary>
        /// そのまま基地に戻るボタン
        /// </summary>
        private void ReturnButtonAction()
        {
            IsResultActive = false;
            StartCoroutine(GameManager.Instance.BackToMainMap(battleResult.state == VictoryConditions.GameResult.Win, true, battleResult.state));
        }

        /// <summary>
        /// StartMenuに戻るボタン
        /// </summary>
        private void ReturnStartMenuAction()
        {
            IsResultActive = false;
            StartCoroutine(GameManager.Instance.BackToStartScene());
        }
        #endregion
    }

    /// <summary>
    /// 戦闘結果の各種データのためのclass
    /// </summary>
    public class BattleResult
    {

        public List<UnitData> units = new List<UnitData>();

        public List<UnitData> deadUnits = new List<UnitData>();

        public List<UnitData> killedEnemies = new List<UnitData>();

        public VictoryConditions.GameResult state;
        /// <summary>
        /// Sceneに配置できるUnit数の最大数
        /// </summary>
        public int limitUnitsNumber = 0;
        /// <summary>
        /// Sceneの基本経験値
        /// </summary>
        public int baseExp = 0;
        /// <summary>
        /// 何ターンでクリアしたか
        /// </summary>
        public int numberOfTurn = 0;
        /// <summary>
        /// 死亡した自軍の人数
        /// </summary>
        public int DeadCount { get => deadUnits.Count; }

        /// <summary>
        /// ターン経過で獲得した経験値
        /// </summary>
        public int TurnExp
        {
            get
            {
                // 少ない人数でクリアした際のボーナス経験値
                var unitCountExp = (limitUnitsNumber - units.Count) * 50;
                int turn = 1;
                if (units.Count + killedEnemies.Count != 0)
                    turn = numberOfTurn / (units.Count + killedEnemies.Count);
                int turnExp;
                if (turn <= 2)
                    turnExp = 300;
                else
                    turnExp = Mathf.RoundToInt(-0.4f * turn + 1.8f);
                return turnExp + unitCountExp;
            }
        }
    }
}