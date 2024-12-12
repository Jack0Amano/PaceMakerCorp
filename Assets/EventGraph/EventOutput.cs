using EventGraph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Linq;


namespace EventGraph.InOut
{
    #region Output
    /// <summary>
    /// イベントの基底クラス
    /// </summary>
    public class EventOutput
    {
        /// <summary>
        /// 次にExecuteするProcessNodeにつながるNode
        /// </summary>
        public Port NextPort;

        /// <summary>
        /// NodeのID
        /// </summary>
        public string NodeID;
        /// <summary>
        /// Event全体のID
        /// </summary>
        public string EventID;
        /// <summary>
        /// イベントが終了した際の固有の名称
        /// </summary>
        public string EndEventName = "";
        /// <summary>
        /// Nodeの名前
        /// </summary>
        public string NodeName;
        /// <summary>
        /// Nodeが終了したか
        /// </summary>
        public bool IsNodeCompleted
        {
            get => NextPort != null;
        }
        /// <summary>
        /// Eventが終了したか
        /// </summary>
        public bool IsEventCompleted
        {
            get => EndEventName.Length > 0;
        }

        public EventOutput()
        {
        }

        public EventOutput(EventGraph.Nodes.SampleNode node, string id, Port outputPort)
        {
            NodeID = node.Guid;
            NodeName = node.title;
            NodeID = id;
            NextPort = outputPort;

            if (node is Nodes.EndEventNode n)
                EndEventName = n.EventReturnName;
        }

        public override string ToString()
        {
            return $"EventOutput: {NodeName} {NodeID}";
        }
    }

    /// <summary>
    /// スポーンするSquadとその位置情報等 を出すイベント
    /// </summary>
    public class SpawnEventOutput : EventOutput
    {
        /// <summary>
        /// Spawnさせる部隊のID (spawnDataの中から選ばれる)
        /// </summary>
        public string squadID;
        /// <summary>
        /// 出現確率 1ならイベントで必須の部隊
        /// </summary>
        public float spawnRate;

        public SpawnEventOutput(Nodes.SampleNode node,  string id, Port outputPort): base(node, id, outputPort)
        {
        }

        public override string ToString()
        {
            return $"SpawnEvent: {squadID}";
        }
    }

    /// <summary>
    /// Location関連のEventの出力
    /// </summary>
    public class LocationEventOutput: EventOutput
    {
        /// <summary>
        /// 対象のLocationのID
        /// </summary>
        public string LocationID;
        /// <summary>
        /// Enemyのちからが回復していく時間
        /// </summary>
        public float HourToRecoverPower = float.MinValue;

        public LocationEventOutput(Nodes.SampleNode node, string id, Port outputPort) : base(node, id, outputPort)
        {
        }
    }


    /// <summary>
    /// メッセージボックスをポップアップさせるタイプのイベント
    /// </summary>
    public class MessageEventOutput : EventOutput
    {
        /*
         * {Stc0, Stc1, Cho1, Cho2, Cho3}
         * という風に並べられた場合 Stc0が最初に出る ページ送りボタンでStc1に
         * Stc1が出たら Cho1~Cho3
         * 選択肢の条件をGraphに返して結果を待つ
         */
        /// <summary>
        /// 会話の流れ
        /// </summary>
        public List<Sentence> sentences = new List<Sentence>();

        /// <summary>
        /// 有効化するイメージ
        /// </summary>
        public List<(ImageAlignment alignment, Sprite image)> ShowImages = new List<(ImageAlignment alignment, Sprite image)>();

        /// <summary>
        /// 有効化するイメージ 
        /// </summary>
        public List<ImageAlignment> ActivateImages = new List<ImageAlignment>();

        /// <summary>
        /// 非有効化するイメージ 色が黒くなる
        /// </summary>
        public List<ImageAlignment> DisactivateImages = new List<ImageAlignment>();

        /// <summary>
        /// 削除するイメージ
        /// </summary>
        public List<ImageAlignment> HideImage = new List<ImageAlignment>();
        /// <summary>
        /// メッセージウィンドウを終了し閉じるトリガー
        /// </summary>
        public bool ForceEndMessageWindow = false;

        public MessageEventOutput(Nodes.SampleNode node, string id, Port outputPort): base(node, id, outputPort)
        {
        }


        public class Sentence
        {
            public string id;
            /// <summary>
            /// メッセージの本文
            /// </summary>
            public string text;
            /// <summary>
            /// メッセージが誰から送られたか
            /// </summary>
            public string messageFrom;
            /// <summary>
            /// 返答用の選択肢
            /// </summary>
            public bool isChoice;

            public override string ToString()
            {
                if (isChoice)
                    return text;
                else
                    return $"{messageFrom}< {text}";
            }
        }

        public override string ToString()
        {
            var output = "";
            var index = 0;
            var choiceIndex = 0;
            sentences.ForEach(s =>
            {
                if (!s.isChoice)
                {
                    output += $"{index}. {s}\n";
                    index++;
                }
                else
                {
                    output += $"-{choiceIndex}. {s}\n";
                    choiceIndex++;
                }
            });

            return $"MessageEvent\n{output}";
        }
    }

    /// <summary>
    /// MessageでImageを表示する際の位置
    /// </summary>
    [Serializable]
    public enum ImageAlignment
    {
        Center,
        Right,
        Left
    }
    #endregion

    #region Input
    /// <summary>
    /// ゲームプログラム本体からEventGraphに返すclass
    /// </summary>
    public class EventInput
    {
        /// <summary>
        /// 途中から始めるためのNodeのID
        /// </summary>
        public string StartAtID = "";

        /// <summary>
        /// 選択肢式のトリガーを保つ場合のその選択したIndex
        /// </summary>
        public int SelectTriggerIndex;

        /// <summary>
        /// 現在時刻
        /// </summary>
        public DateTime DateTime;
        /// <summary>
        /// どのスポーンIDを持つ的にエンカウントしたのか OnBattleTriggerNode等に使う
        /// </summary>
        public string encountSpawnID;
        /// <summary>
        /// トリガーとなるSceneのタイミング
        /// </summary>
        public TriggerTiming triggerTiming;
        /// <summary>
        /// 結果によるトリガー
        /// </summary>
        public Tactics.VictoryConditions.GameResult gameResultTrigger;

        /// <summary>
        /// 所持しているアイテムのID
        /// </summary>
        public List<string> ItemsID;
    }

    /// <summary>
    /// トリガーとなるSceneのタイミング
    /// </summary>
    public enum TriggerTiming
    {
        None,
        BeforeBattle,
        AfterBattle,
        BeforePrepare,
        OnPrepare,
        AfterResultScene,
        PassTime,
        GameStart
    }
    #endregion


}