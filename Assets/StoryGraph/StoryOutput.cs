using StoryGraph.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Linq;


namespace StoryGraph.InOut
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

        public string nodeID;

        public EventOutput()
        {
        }

        public EventOutput(string id, Port outputPort)
        {
            nodeID = id;
            NextPort = outputPort;
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
        /// スポーンする地点のID
        /// </summary>
        public Vector3 location;
        /// <summary>
        /// 出現確率 1ならイベントで必須の部隊
        /// </summary>
        public float spawnRate;

        public SpawnEventOutput(string id, Port outputPort): base(id, outputPort)
        {
        }

        public override string ToString()
        {
            return $"SpawnEvent: {squadID}, {location}";
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

        public MessageEventOutput(string id, Port outputPort): base(id, outputPort)
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
        /// 所持しているアイテムのID
        /// </summary>
        public List<string> ItemsID;
    }
    #endregion


}