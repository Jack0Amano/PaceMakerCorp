using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using static Utility;
using UnityEditor;
using System.IO;
using EventGraph;
using StoryGraph.Editor;
using EventGraph.InOut;
using static Tactics.VictoryConditions;
using MainMap;
using Parameters.SpawnSquad;

namespace EventScene
{
    /// <summary>
    /// 割り込み型のイベントシーンを管理する
    /// </summary>
    public class EventSceneController : MonoBehaviour
    {
        [SerializeField] public Message.MessageEvent messageEvent;
        [SerializeField] public Dialog.DialogEvent dialogEvent;

        public readonly List<StoryGraph.StoryGraphView> AllStories = new List<StoryGraph.StoryGraphView>();

        DataSavingController DataSavingController;

        /// <summary>
        /// 何らかのEventSceneが表示中か
        /// </summary>
        public bool IsEventWindowActive
        {
            get
            {
                return dialogEvent.IsDialogEventActive || messageEvent.IsEventSceneActive;
            }
        }

        /// <summary>
        /// イベントシーンが開始される時呼び出し
        /// </summary>
        public EventHandler<EventArgs> BeginEventHandler;
        /// <summary>
        /// すべてのイベントシーンが終了した際の呼び出し
        /// </summary>
        public EventHandler<EventArgs> EndEventHandler;
        /// <summary>
        /// 敵がスポーンするRequest SpawnするID
        /// </summary>
        public EventHandler<SpawnRequestArgs> SpawnSquadRequest;
        /// <summary>
        /// MapLocations上で動くEventのRequest
        /// </summary>
        public Action<LocationEventOutput> LocationEventRequest;
        /// <summary>
        /// AddUnitEventOutputを受け取るRequest
        /// </summary>
        public Action<AddUnitEventOutput> AddUnitRequest;

        /// <summary>
        /// MainMapScene
        /// </summary>
        public MainMapScene MainMapScene;

        /// <summary>
        /// 直前に実行したイベント
        /// </summary>
        StoryGraph.Nodes.EventNode previousEvent;
        /// <summary>
        /// 現在時点でのEventInputを取得する
        /// </summary>
        EventInput EventInput
        {
            get
            {
                return new EventInput()
                {
                    DateTime = GameManager.Instance.GameTime,
                    ItemsID = GameManager.Instance.DataSavingController.MyArmyData.OwnItems.ConvertAll(e => e.Id),
                    MainMapController = MainMapScene.MainMapController
                };
            }
        }

        private readonly List<StoryGraph.Nodes.EventNode> runableEvents = new List<StoryGraph.Nodes.EventNode>();

        private void Awake()
        {
            DataSavingController = GameManager.Instance.DataSavingController;
        }

        protected void Start()
        {
        }

        #region Save & Load
        /// <summary>
        /// Pathに存在するすべてのストーリーを読み込む
        /// </summary>
        /// <param name="path"></param>
        public void LoadStories(string path)
        {
            if (DataSavingController == null)
                DataSavingController = GameManager.Instance.DataSavingController;

            AllStories.Clear();
            var files = Directory.GetFiles(path, "*.asset");
            foreach(var file in files)
            {
                try
                {
                    var data = AssetDatabase.LoadAssetAtPath<StoryGraphDataContainer>(file);
                    var view = StoryGraphSaveUtility.LoadGraph(data);
                    AllStories.Add(view);
                }
                catch (Exception e)
                {
                    PrintWarning($"File type is miss match: {path}\n{e}");
                }  
            }

            Print($"Loaded {AllStories.Count} Stories from {files.Length} files in {path}");

            foreach(var story in AllStories)
            {
                if (DataSavingController.SaveDataInfo.storiesSaveData != null)
                {
                    var storyData = DataSavingController.SaveDataInfo.storiesSaveData.Find(data => data.StoryID == story.dataContainer.StoryID);
                    if (storyData != null)
                        story.SetSaveData(storyData.EventsData);
                    else
                        story.SetSaveData(new List<SaveDataInfo.StorySaveData.EventSaveData>());
                        
                }
                else
                {
                    story.SetSaveData(new List<SaveDataInfo.StorySaveData.EventSaveData>());
                }
            }

            previousEvent = null;
        }

        /// <summary>
        /// <c>DataSavingController.saveDataInfo.storiesSaveData</c>に現在のストーリーの進行度合いを書き込む
        /// </summary>
        private void SaveStories()
        {
            DataSavingController.SaveDataInfo.storiesSaveData.Clear();
            AllStories.ForEach(s =>
            {
                DataSavingController.SaveDataInfo.storiesSaveData.Add(s.GetSaveData());
            });
        }
        #endregion

        #region PlayEvent
        /*
         * まずこの関数の動きは AllStoriesからアクセス可能なEventを取得 (StoriesのExecuteでNodeを取得) 
         * もし前回実行したEventが未完了の場合はこれを優先してExecute
         * アクセス可能なNodeでのExecuteを行う
         * NodeのTriggerで必要な情報をその際に提供 Inputで必須な情報をinitに入れておく
         * 
         */
        /// <summary>
        /// 実行可能なEventGraphを探しこれを実行する
        /// </summary>
        /// <param name="encountSpawnID">どのSpawnIDを持った敵にエンカウントしたか</param>
        /// <param name="triggerTiming">呼び出したタイミング</param>
        public IEnumerator PlayEventIfNeeded(TriggerTiming triggerTiming, string encountSpawnID = "", GameResult resultType = GameResult.None)
        {
            gameObject.SetActive(true);

            // 他のTimingでEventが実行中であるためこれをストップ
            while (messageEvent.IsEventSceneActive || dialogEvent.IsDialogEventActive)
                yield return null;

            // 前回の実行がないためStoriesから探す
            runableEvents.Clear();
            AllStories.ForEach(s =>
            {
                runableEvents.AddRange(s.GetRunableNodes()); ;
                    
            });
            runableEvents.Sort((a, b) => a.SortValue - b.SortValue);

            //　直線に実行したEventがある場合は優先してこれを実行
            if (previousEvent != null && !previousEvent.IsCompleted)
            {
                runableEvents.Remove(previousEvent);
                runableEvents.Insert(0, previousEvent);
            }

            if (runableEvents.Count == 0)
            {
                yield break;
            }   

            var eventOut = new List<EventOutput>();
            var eventInput = EventInput;
            eventInput.TriggerTiming = triggerTiming;
            eventInput.EncountSpawnID = encountSpawnID;
            eventInput.GameResultTrigger = resultType;
            foreach (var e in runableEvents)
            {
                //e.SaveData
                //GameManager.Instance.SavedData.SaveDataInfo.storiesSaveData.Find(d => d.EventsData.sto)
                eventOut = e.ExecuteEventView(eventInput);
                if (eventOut.Count > 0)
                {
                    previousEvent = e;
                    break;
                }
            }
            if (eventOut.Count > 0)
            {
                BeginEventHandler?.Invoke(this, new EventArgs());
                var previousTimer = GameManager.Instance.IsTimerStopping;
                GameManager.Instance.IsTimerStopping = true;
                // 各EventOutの内容をControllerに振り分ける
                UserController.enableCursor = true;
                yield return StartCoroutine(PlayEventOutputs(eventOut));
                GameManager.Instance.IsTimerStopping = previousTimer;
                EndEventHandler?.Invoke(this, new EventArgs());
                SaveStories();
            }
        }

        /// <summary>
        /// EventGraphを特定してこれを実行する
        /// </summary>
        /// <param name="input">EventGraphのNodeにて使用するInput</param>
        public void Play(string storyID, string eventID, EventInput input)
        {
            var story = AllStories.Find(s => s.dataContainer.StoryID == storyID);
            if (story == null) return;
            var eventNode = story.GetEventNode(eventID);
            if (eventNode == null) return;
            StartCoroutine( Play(eventNode, input));
        }
        
        /// <summary>
        /// <c>eventNode</c>を<c>input</c>を入力して実行する
        /// </summary>
        /// <param name="eventNode"></param>
        /// <param name="input"></param>
        private IEnumerator Play(StoryGraph.Nodes.EventNode eventNode, EventInput input)
        {
            if (eventNode.IsCompleted)
            {
                PrintWarning($"{eventNode} is Already completed");
                previousEvent = null;
                yield break ;
            }
            previousEvent = eventNode;
            var eventOut = eventNode.ExecuteEventView(input);
            if (eventOut.Count > 0)
            {

                // 各EventOutの内容をControllerに振り分ける
                yield return StartCoroutine( PlayEventOutputs(eventOut));
            }
        }

        /// <summary>
        /// イベントの内容の再生
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        private IEnumerator PlayEventOutputs(List<EventOutput> events)
        {
            // TODO　例えばMessage系から動画系に進むときはmessage.hide()を実行してWindowを消しておく
            foreach(var eventOut in events)
            {
                if (eventOut is MessageEventOutput message)
                {
                    print("MessageEventOutput");
                    yield return StartCoroutine(messageEvent.ShowMessageEvent(message));

                }
                else if (eventOut is SpawnEventOutput spawn)
                {
                    print("SpawnEventOutput");
                    SpawnSquadRequest?.Invoke(this,new SpawnRequestArgs( spawn, GameManager.Instance.GameTime));
                    
                }
                else if (eventOut is LocationEventOutput location)
                {

                }
                else if (eventOut is AddUnitEventOutput addUnit)
                {
                    print("AddUnitEventOutput");
                    AddUnitRequest?.Invoke(addUnit);
                }
            }

            // メッセージタイプの返答がされたため
            if (messageEvent.ResponseButtonIndex != -1)
            {
                // 返答ボタンが返されている場合
                var input = EventInput;
                input.StartAtID = events.FindLast(e => e is MessageEventOutput).NodeID;
                input.SelectTriggerIndex = messageEvent.ResponseButtonIndex;
                yield return StartCoroutine( Play(previousEvent, input));
                yield break;
            }
            else
            {
                messageEvent.Hide();
            }
        }
        #endregion

        /// <summary>
        /// 何らかのイベントメッセージが表示中
        /// </summary>
        public bool IsEventActive
        {
            get
            {
                return messageEvent.IsEventSceneActive || dialogEvent.IsDialogEventActive;
            }
        }
    }
}