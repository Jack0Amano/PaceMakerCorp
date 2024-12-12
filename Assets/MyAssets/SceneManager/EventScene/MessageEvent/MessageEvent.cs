using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Linq;
using EventGraph.InOut;
using static Utility;


namespace EventScene.Message
{
    public class MessageEvent : MonoBehaviour
    {
        [Tooltip("メッセージを次へ進めるボタン")]
        [SerializeField] MessageResponse nextResponse;
        [Tooltip("回答ボタン1")]
        [SerializeField] MessageResponse messageResponse1;
        [Tooltip("回答ボタン2")]
        [SerializeField] MessageResponse messageResponse2;
        [Tooltip("回答ボタン3")]
        [SerializeField] MessageResponse messageResponse3;
        [SerializeField] CanvasGroup messageEventWindowCanvas;
        [SerializeField] TextMeshProUGUI messageFromLabel;
        [SerializeField] TextMeshProUGUI messageLabel;
        [SerializeField] Image messageImage;

        /// <summary>
        /// すべてのResponse用のButtonPanel
        /// </summary>
        private List<MessageResponse> responsePanels;
        /// <summary>
        /// アニメーションの時間
        /// </summary>
        readonly private float duration = 0.5f;
        /// <summary>
        /// イベントシーンが開始される時呼び出し
        /// </summary>
        public EventHandler<EventArgs> BeginEventHandler;
        /// <summary>
        /// すべてのイベントシーンが終了した際の呼び出し
        /// </summary>
        public EventHandler<EventArgs> EndEventHandler;
        /// <summary>
        /// メッセージタイプのイベントの進行中
        /// </summary>
        public bool IsEventSceneActive { private set; get; } = false;
        /// <summary>
        /// [nextResponse]ボタンの入力待ち
        /// </summary>
        public bool IsWaitingNextButtonAction { private set; get; } = false;
        /// <summary>
        /// 返答のインデックス　ない場合は-1
        /// </summary>
        public int ResponseButtonIndex { private set; get; } = -1;

        protected private void Awake()
        {

            responsePanels = new List<MessageResponse>() {messageResponse1, messageResponse2, messageResponse3 };
            for(var i=0; i<responsePanels.Count; i++)
            {
                var buttonPanel = responsePanels[i];
                buttonPanel.index = i;
                buttonPanel.button.onClick.AddListener(() => OnClickResponseButton(buttonPanel));
            }
            nextResponse.button.onClick.AddListener(() => OnClickNextButton());
        }

        // Start is called before the first frame update
        protected void Start()
        {
            messageEventWindowCanvas.alpha = 0;
            gameObject.SetActive(false);
            messageFromLabel.SetText("");
            messageLabel.SetText("");
        }

        /// <summary>
        /// Windowを閉じる
        /// </summary>
        public void Hide()
        {
            End();
        }

        /// <summary>
        /// メッセージタイプの文字列を表示する
        /// </summary>
        /// <param name="message">表示内容</param>
        /// <returns></returns>
        public IEnumerator ShowMessageEvent(MessageEventOutput message)
        {
            IsEventSceneActive = true;
            IsWaitingNextButtonAction = false;

            ResponseButtonIndex = -1;
            if (!messageEventWindowCanvas.gameObject.activeSelf || !gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
                messageEventWindowCanvas.gameObject.SetActive(true);
                messageEventWindowCanvas.DOFade(1, 1);
            }

            if (message.ForceEndMessageWindow)
            {
                End();
                yield break;
            }

            for(var i=0; i<message.Sentences.Count; i++)
            {
                var sentence = message.Sentences[i];
                var isNextResponseSentence = false;
                if (message.Sentences.IndexAt_Bug(i + 1, out var next))
                    isNextResponseSentence = next.IsChoice;

                if (!sentence.IsChoice)
                {
                    // 前のSentenceが表示中で nextResponseButtonが押されていないため待ち
                    while (IsWaitingNextButtonAction)
                        yield return null;
                    yield return new WaitForSeconds(0.5f);
                    ShowSentence(sentence, !isNextResponseSentence);
                }
                else
                {
                    // レスポンス(選択肢が表示される)
                    if (responsePanels.Exists(r => r.IsActive))
                    {
                        yield return new WaitForSeconds(0.2f);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.3f);
                    }
                    ShowResponseButton(sentence);
                }
            }

            // レスポンスボタンを待つ
            while (IsWaitingNextButtonAction)
                yield return null;
        }

        /// <summary>
        /// MessageEventOutputの内容をMessageに表示する
        /// </summary>
        /// <param name="sentence"></param>
        private void ShowSentence(MessageEventOutput.Sentence sentence, bool showResponse)
        {
            messageLabel.SetText(sentence.Text);
            if (sentence.MessageFrom.Length != 0)
                messageFromLabel.SetText(sentence.MessageFrom);
            if (showResponse)
                nextResponse.Show();
            IsWaitingNextButtonAction = true;
        }

        /// <summary>
        /// MessageEventOutputがResponseのときにこれの選択肢ボタンを表示する
        /// </summary>
        /// <param name="sentence"></param>
        private void ShowResponseButton(MessageEventOutput.Sentence sentence)
        {
            var responseButton = responsePanels.Find(r => !r.IsActive);
            if (responseButton == null)
            {
                PrintWarning("ResponseButtons are Only 3");
                return;
            }
            responseButton.Show(sentence.Text);
            IsWaitingNextButtonAction = true;
        }

        /// <summary>
        /// 次にボタンが押されたときの呼び出し
        /// </summary>
        private void OnClickNextButton()
        {
            IsWaitingNextButtonAction = false;
            nextResponse.Hide();
        }

        /// <summary>
        /// 選択肢ボタンが押された際の呼び出し
        /// </summary>
        private void OnClickResponseButton(MessageResponse button)
        {
            ResponseButtonIndex = button.index;
            IsWaitingNextButtonAction = false;
            responsePanels.ForEach(r => r.Hide());

        }

        /// <summary>
        /// イベントを終了する
        /// </summary>
        private void End()
        {
            Print("EndEvent");
            responsePanels.ForEach(r => r.Hide());
            messageEventWindowCanvas.DOFade(0, duration).OnComplete(() => 
            {
                IsEventSceneActive = false;
                gameObject.SetActive(false);
            });
        }
    }
}