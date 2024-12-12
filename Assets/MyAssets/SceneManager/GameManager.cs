using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using static Utility;
using System.Text;
using UnityEngine.Assertions;
using UnityEngine.AddressableAssets;
using Unity.VisualScripting;
using MainMap;
using System.Net;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Parameters.MapData;
using IngameDebugConsole;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Tooltip("Time of Read Only")]
    [SerializeField] private SerializableDateTime SceneTime;
    [Tooltip("割り込み型のストーリーイベントを管理する")]
    [SerializeField] public EventScene.EventSceneController EventSceneController;
    [SerializeField] public StartUI.StartCanvasController StartCanvasController;
    [Tooltip("ロード中に表示するPanelのcanvas")]
    [SerializeField] public FadeInOutCanvas FadeInOutCanvas;
    [Tooltip("Tactics用のAIの調整用固定パラメーター")]
    [SerializeField] public GeneralParameter GeneralParameter;
    [Tooltip("シミュレーションスピード")]
    [SerializeField] public float Speed = 1;
    [Tooltip("Gameがどのような状態であるか")]
    [SerializeField, ReadOnly] public GameState GameState = GameState.Start;
    [Tooltip("DebugLogManagerを参照するためのフィールド")]
    [SerializeField] public DebugLogManager DebugLogManager;
    [SerializeField, ReadOnly] SimulationMode physicsSimulationMode;

    public const float ANIMATION_DURATION = 0.5f;

    public SceneParameter SceneParameter;

    /// <summary>
    /// シミュレーションスピードが十分早い場合AddTimeは10分ごとになる
    /// </summary>
    public bool IsHighSpeedMode
    {
        get => Speed >= 30;
    }

    public static readonly string SaveDataRootPath = "Assets/Data/MyGame";
    public static readonly string DataRootPath = "Assets/Data";
    public static readonly string StaticDataRootPath = "Assets/Data/Static";

    /// <summary>
    /// ストラテジーシーンを非表示にしてMainMapに戻る際に呼び出し
    /// </summary>
    public EventHandler<BackToMainMapHandlerArgs> BackToMainMapHandler;
    /// <summary>
    /// 現在tacticsシーン中であればそのシーンの名前
    /// </summary>
    public string TacticsSceneID { private set; get; }
    /// <summary>
    /// MapSceneでエンカウントしたときの敵味方の情報 Mapからtacticsに移動する時に使用
    /// </summary>
    public ReachedEventArgs ReachedEventArgs
    {
        get => DataSavingController.SaveData.ReachedEventArgs;
        set => DataSavingController.SaveData.ReachedEventArgs = value;
    }

    private readonly string mapSceneID = "Map";
    private readonly string prepareSceneID = "Prepare";
    private readonly string startSceneID = "StartScene";

    /// <summary>
    /// ゲーム内の現在時刻を保持するフィールド
    /// </summary>
    [SerializeField, ReadOnly] public DateTime GameTime;
    /// <summary>
    /// <c>addTimeEventHandler</c>を呼び出す秒数　IsHighSpeedModeがTrueの場合は10分ごとに呼び出される
    /// (HighspeedModeの場合AddTimeSecondsは10分になる)
    /// </summary>
    public float AddTimeSeconds
    {
        get
        {
            if (IsHighSpeedMode)
                return (GeneralParameter.dayLengthMinute * 60) / 144f;
            else
                return (GeneralParameter.dayLengthMinute * 60) / 1440f;
        }
    }

    /// <summary>
    /// <c>addTimeEventHandler</c>を一日に何回呼び出すか
    /// </summary>
    public float CallAddTimeEventHandlerCountInDay
    {
        get
        {
            var dayLengthSecond = GeneralParameter.dayLengthMinute * 60;
            return dayLengthSecond / AddTimeSeconds;
        }
    }

    /// <summary>
    /// ゲーム内タイマーを最後に呼び出した時間
    /// </summary>
    private DateTime previousTimerCallbackTime;

    /// <summary>
    /// Mainmapが既に配置等が終わっているか MaiMapSceneから置かれる
    /// </summary>
    [SerializeField, ReadOnly] public bool HasMainMapLoaded = false;

    /// <summary>
    /// タイマーをストップしているか
    /// </summary>
    [SerializeField, ReadOnly] public bool IsTimerStopping = false;
    /// <summary>
    /// GameTimeが一定時間経過した際に呼び出される
    /// </summary>
    public EventHandler<EventArgs> PassTimeEventHandlerAsync;
    /// <summary>
    /// GameTimeが加算された時呼び出し 非同期呼び出し
    /// </summary>
    public EventHandler AddTimeEventHandlerAsync;
    /// <summary>
    /// GameTimeが加算されたときに呼び出し  同期呼び出し
    /// </summary>
    public EventHandler<int> AddTimeEventHandlerSync;
    /// <summary>
    /// ゲーム全体のランダム要素をコントロールする 通常のRandomだと大体不満が出るため
    /// </summary>
    public RandomController RandomController = new RandomController();
    /// <summary>
    /// StartSceneが読み込まれていないときに自動で読み込むか (Debug時で特定Sceneを動かすときはFalse)
    /// </summary>
    public bool AutoRunStartScene = false;
    /// <summary>
    /// 翻訳 Commonの翻訳はAwake時に読み込まれるがScene固有の翻訳はLoadData時に読み込まれる
    /// </summary>
    public Translation Translation { private set; get; }
    /// <summary>
    /// ゲーム内で静的なデータ 全Unitアイテムデータや Setting 言語
    /// </summary>
    public StaticGameData StaticData { private set; get; }
    /// <summary>
    /// セーブされたデータ
    /// </summary>
    public DataSavingController DataSavingController { private set; get; } = new DataSavingController();
    /// <summary>
    /// 何らかのロード中であるか
    /// </summary>
    public bool IsLoading { private set; get; } = false;
    /// <summary>
    /// メインマップのScene SceneにしなくてもGameManagerSceneで統一してもいいかも
    /// </summary>
    [NonSerialized] public MainMap.MainMapScene MainMapScene;

    /// <summary>
    /// DataSavingControllerのデータが読み込まれているか (readonly, Inspectorに表示するためのもの)
    /// </summary>
    [SerializeField, ReadOnly] public bool HasDataLoaded = false;

    /// <summary>
    /// 前回AutoSaveした時間
    /// </summary>
    private DateTime previousAutoSaveTime;

    /// <summary>
    /// MainMapのカメラ
    /// </summary>
    public Camera MainMapCamera;

    /// <summary>
    /// MainMapカメラのAudioListener
    /// </summary>
    public AudioListener MainMapAudioListener;

    /// <summary>
    /// Gameの難易度
    /// </summary>
    public GameDifficulty GameDifficulty
    {
        get => DataSavingController.SaveData.DataInfo.GameDifficulty;
    }

    #region Init
    protected override void Awake()
    {
        base.Awake();
        // カルチャをUSに統一
        System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");

        StaticData = StaticGameData.Load();
        Translation = new Translation(StaticData.CommonSetting.language);

        if (!EventSceneController.gameObject.activeSelf)
            EventSceneController.gameObject.SetActive(true);

        previousAutoSaveTime = DateTime.Now;

        MainMapCamera = Camera.main;
        MainMapAudioListener = MainMapCamera.GetComponent<AudioListener>();

        EventSceneController.MainMapScene = MainMapScene;
    }

    // Start is called before the first frame update
    protected void Start()
    {
        if (AutoRunStartScene)
            StartCoroutine(CheckStartScene());
        StartCoroutine(TimeUpdate());
    }

    private void Update()
    {
        physicsSimulationMode = Physics.simulationMode;

        if (UserController.KeyCodeSetting && !IsLoading && !(GameState == GameState.Start || GameState == GameState.Event))
        {
            if (GameState == GameState.MainMap)
            {
                // MainMapの場合のStartUIの表示
                if (!StartCanvasController.IsEnable)
                {
                    StartCanvasController.Show(StartUI.StartPosition.NearTable, true);
                }
                else
                {
                    StartCoroutine(StartCanvasController.Hide());
                }
                    
            }
            else if (GameState == GameState.Tactics)
            {

            }
        }

        // QuickSaveボタンによるセーブ
        if (UserController.QuickSave && GameState != GameState.Start && !IsLoading)
        {
            QuickSave();
        }
    }

    /// <summary>
    /// GameManagerのデータを初期化する
    /// </summary>
    public void ResetMap()
    {
        Speed = 1;
        PassTimeEventHandlerAsync = null;
        AddTimeEventHandlerAsync = null;
        AddTimeEventHandlerSync = null;
        EventSceneController.BeginEventHandler = null;
        EventSceneController.EndEventHandler = null;
    }

    /// <summary>
    /// スタートSceneが読み込まれていなときは読み込む
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckStartScene()
    {
        yield return null;
        var startScene = SceneManager.GetSceneByName(startSceneID);
        if (!startScene.IsValid())
            StartCoroutine(LoadScene(startSceneID));
    }
    #endregion 

    #region Timer on game
    public bool IsTimerStoppingOnGame()
    {
        if (IsTimerStopping)
            return true;
        if (StartCanvasController.IsEnable)
            return true;
        if (EventSceneController.IsEventWindowActive)
            return true;
        if (DataSavingController.SaveData.HasTacticsData)
            return true;
        return false;
    }

    /// <summary>
    /// ゲーム内時間のタイマー MainMap画面でしかカウントされない
    /// </summary>
    /// <returns></returns>
    private IEnumerator TimeUpdate()
    {
        // 他のPanelがStartするまで待つ
        yield return new WaitForSeconds(0.5f);
        previousTimerCallbackTime = DateTime.Now;

        IEnumerator AsyncCall(DateTime now)
        {
            AddTimeEventHandlerAsync?.Invoke(this, null);
            if ((now - previousTimerCallbackTime).TotalMilliseconds * Speed > GeneralParameter.timerCallbackSeconds * 1000)
            {
                previousTimerCallbackTime = now;
                PassTimeEventHandlerAsync?.Invoke(this, null);
            }
            yield break;
        }


        while (true)
        {
            HasDataLoaded = DataSavingController.HasDataLoaded;
            if (!HasDataLoaded ||
                PassTimeEventHandlerAsync == null ||
                !HasMainMapLoaded
                )
            {
                yield return null;
                continue;
            }
            var now = DateTime.Now;

            // Stop機能付きのWaitForSeconds
            var stopTime = 0.0;
            while (((DateTime.Now - now).TotalMilliseconds - stopTime) * Speed < AddTimeSeconds * 1000)
            {
                if (IsTimerStoppingOnGame())
                {
                    var start = DateTime.Now;
                    while (IsTimerStoppingOnGame())
                        yield return null;
                    stopTime += (DateTime.Now - start).TotalMilliseconds;
                }
                yield return null;
            }

            var addTime = Speed >= 30 ? 10 : 1;
            GameTime = GameTime.AddMinutes(addTime);

            SceneTime = GameTime;
            StartCoroutine(EventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.PassTime));
            AutoSave();
            AddTimeEventHandlerSync?.Invoke(this, addTime);
            StartCoroutine(AsyncCall(now));
        }
    }

    /// <summary>
    /// 実際の秒数からゲーム内時間がどの程度進むのか計算する
    /// </summary>
    /// <returns>ゲーム内で経過する時間(分)</returns>
    public float CalcDayTimeFromRealSeconds(float second, bool highSpeedMode)
    {
        var dayLengthSeconds = GeneralParameter.dayLengthMinute * 60;
        if (highSpeedMode)
        {
            // highSpeedModeの場合は時間の増加が10分ごとになるため 1440/10 = 144
            return second * 144 / dayLengthSeconds;
        }
        else
        {
            return second * 1440 / dayLengthSeconds;
        }
    }
    #endregion

    #region Load and save system
    /// <summary>
    /// クイックセーブを行う
    /// </summary>
    /// <param name="fileName">セーブファイルの名前</param>
    /// <param name="reachedEventArgs">保存する場合はエンカウントした敵と味方の内容を保持する</param>
    public void QuickSave(string fileName = "QuickSave", ReachedEventArgs reachedEventArgs=null)
    {

        if (GameState == GameState.MainMap)
        {
            if (MainMapScene.IsTempSave)
            {
                var isSaved = DataSavingController.SaveFromTemp(fileName);
                if (isSaved)
                {
                    Print("QuickSave is saved from temp data at", DataSavingController.TempSaveData.DataInfo.GameTime);
                }
                else
                {
                    PrintError("Failed to save QuickSave");
                }
            }
            else
            {
                DataSavingController.Save(DataSavingController.MakeSavePath(fileName), reachedEventArgs);
            }
        }
        else
        {
            DataSavingController.Save(DataSavingController.MakeSavePath(fileName), reachedEventArgs);
        }
    }

    /// <summary>
    /// 前回のAutoSaveから一定時間経過している場合AutoSaveを行う
    /// </summary>
    private void AutoSave()
    {
        if (StaticData.CommonSetting.AutoSaveIntervalMinutes == 0)
            return;
        var now = DateTime.Now;
        if ((now - previousAutoSaveTime).TotalMinutes > StaticData.CommonSetting.AutoSaveIntervalMinutes)
        {
            QuickSave("AutoSave");
            previousAutoSaveTime = now;
        }
    }

    /// <summary>
    /// 選択したデータを読み込む
    /// </summary>
    public IEnumerator LoadData(SaveData data)
    {
        IsLoading = true;
        MainMapScene.OnDestroyMap();
        ResetMap();

        AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(data.DataInfo.MainMapSceneName);
        yield return handle;
        if (!(handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null && handle.Result.Count > 0))
        {
            PrintError($"Failed to load map scene {data.DataInfo.MainMapSceneName}");
            yield break;
        }

        // loadDataの中身を読み込む
        StaticData.LoadStaticSceneData(data.DataInfo.MainMapSceneName);
        DataSavingController.Load(data);
        var storiesPath = Path.Combine(StaticDataRootPath, data.DataInfo.MainMapSceneName, "Story");
        EventSceneController.LoadStories(storiesPath);
        Translation.LoadFromGameSceneName(data.DataInfo.MainMapSceneName);

        var mainMapSceneIsActive = MainMapScene.gameObject.activeSelf;
        MainMapScene.gameObject.SetActive(true);

        yield return StartCoroutine(MainMapScene.LoadMap(data.DataInfo.MainMapSceneName));

        GameTime = DataSavingController.SaveDataInfo.GameTime;

        data.CheckData();


        if (GameState == GameState.Start)
        {
            // Start画面からMainMapに遷移する場合
            GameState = GameState.MainMap;
        }
        else if (GameState == GameState.MainMap)
        {
            // MainMapのときにLoadを行った場合
            if (DataSavingController.SaveData.HasTacticsData)
            {
                // Tacticsのデータが存在する場合MainMapからTacticsに遷移する
                yield return StartCoroutine(MainMapScene.TransitToTacticsScene(ReachedEventArgs));
            }
            else
            {
                // Tacticsのデータが存在しない場合はMainMapに戻る
                GameState = GameState.MainMap;
            }
        }
        else if (GameState == GameState.Tactics)
        {
            // Tactics画面でLoadを行った場合
            if (DataSavingController.SaveData.HasTacticsData)
            {
                // Tacticsのデータが存在する場合はTacticsを初期化する

            }
            else
            {
                // Tacticsのデータが存在しない場合はMainMapに戻る
            }
        }
    }

    /// <summary>
    /// TacticsSceneからLoadDataを行った時の処理 既存のTacticsSceneを破棄し新しいデータをロードする
    /// </summary>
    public void LoadDataFromTacticsScene(SaveData data)
    {
        if (GameState != GameState.Tactics)
        {
            PrintError("LoadDataFromTacticsScene is called but GameState is not Tactics");
        }

        IEnumerator LoadDataFromTacticsScenAsync()
        {
            IsLoading = true;
            yield return StartCoroutine(FadeInOutCanvas.Show(ANIMATION_DURATION));
            MainMapScene.OnDestroyMap();
            ResetMap();

            StaticData.LoadStaticSceneData(data.DataInfo.MainMapSceneName);
            DataSavingController.Load(data);
            var storiesPath = Path.Combine(StaticDataRootPath, data.DataInfo.MainMapSceneName, "Story");
            EventSceneController.LoadStories(storiesPath);
            Translation.LoadFromGameSceneName(data.DataInfo.MainMapSceneName);

            MainMapScene.gameObject.SetActive(true);

            yield return StartCoroutine(MainMapScene.LoadMap(data.DataInfo.MainMapSceneName));

            GameTime = DataSavingController.SaveDataInfo.GameTime;

            data.CheckData();
            MainMapCamera.enabled = true;
            yield return StartCoroutine(UnloadScene(TacticsSceneID));
            TacticsSceneID = null;

            if (DataSavingController.SaveData.HasTacticsData)
            {
                // Tacticsのデータが存在する場合はTacticsを初期化する
                yield return StartCoroutine(MainMapScene.TransitToTacticsScene(DataSavingController.SaveData.ReachedEventArgs));
                MainMapCamera.enabled = false;
            }
            else
            {
                GameState = GameState.MainMap;
                // Tacticsのデータが存在しない場合はMainMapに戻る
                yield return StartCoroutine(FadeInOutCanvas.Hide());
            }

        }
        StartCoroutine(LoadDataFromTacticsScenAsync());
        
    }


    /// <summary>
    /// 最初の画面から新たなMapを指定してゲームを始める
    /// </summary> 
    /// <param name="from">現在表示中のScene</param>
    /// <param name="mapID"></param>
    public IEnumerator StartNewGame(MapData mapData, GameDifficulty gameDifficulty)
    {
        var gameMapAddress = mapData.ID;
        IsLoading = true;
        // リセット
        MainMapScene.OnDestroyMap();
        ResetMap();
        // 新しいデータを作成してロード
        StaticData.LoadStaticSceneData(gameMapAddress);
        DataSavingController.MakeNewGameData(gameMapAddress, gameDifficulty);
        GameTime = DataSavingController.SaveData.DataInfo.GameTime;
        var storiesPath = Path.Combine(StaticDataRootPath, gameMapAddress, "Story");
        EventSceneController.LoadStories(storiesPath);
        Translation.LoadFromGameSceneName(gameMapAddress);
        // MapにSaveDataを適用
        yield return StartCoroutine(MainMapScene.LoadMap(DataSavingController.SaveData.DataInfo.MainMapSceneName));

        DataSavingController.NewSave();

        GameState = GameState.MainMap;
    }

    /// <summary>
    /// デバック用のデータ読み込み
    /// </summary>
    public void LoadDebugData(string gameSceneName)
    {
        IsLoading = true;

        var files = Directory.GetFiles(SaveDataRootPath, DataSavingController.SaveDataExtension);
        StaticData.LoadStaticSceneData(gameSceneName);

        if (files.Count() > 0)
        {
            // TODO: 最近のデータを読み込み
            DataSavingController.LoadNewerData();
            DataSavingController.SaveDataInfo.capitalFund = 10000000;
            // EventController.Load(recentPath);
        }
        else
        {
            // TODO: 新しくデバッグデータをDefaultから作成 
            var path = DataSavingController.MakeNewGameData(gameSceneName, GameDifficulty.Normal);
            GameTime = DataSavingController.SaveData.DataInfo.GameTime;
            DataSavingController.NewSave();
            DataSavingController.Load(path);
        }

        //gameController.translation = new Translation("English");
        var storiesPath = Path.Combine(StaticDataRootPath, gameSceneName, "Story");
        EventSceneController.LoadStories(storiesPath);
        Translation.LoadFromGameSceneName(gameSceneName);

        GameTime = DataSavingController.SaveData.DataInfo.GameTime;

        GameState = GameState.MainMap;
    }
    #endregion

    #region Scene segue

    /// <summary>
    /// MapとPrepareシーンがHierarchy上に存在するか確認し無いならば読み込む
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckScenesOnHierarchy()
    {
        yield return null;
        var mapScene = SceneManager.GetSceneByName(mapSceneID);
        var prepareScene = SceneManager.GetSceneByName(prepareSceneID);
        if (!mapScene.IsValid())
            StartCoroutine(LoadScene(mapSceneID));
        if (!prepareScene.IsValid())
            StartCoroutine(LoadScene(prepareSceneID));
    }

    /// <summary>
    /// SceneをHierarchy上に読み込む
    /// </summary>
    /// <param name="scene"></param>
    /// <returns></returns>
    private IEnumerator LoadScene(string scene)
    {
        var sync = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        while (!sync.isDone)
            yield return null;
    }

    /// <summary>
    /// SceneをHierarchy上からDeleteする
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private IEnumerator UnloadScene(string name)
    {
        var scene = SceneManager.GetSceneByName(name);
        if (!scene.IsValid() || !scene.isLoaded)
            yield break;

        var sync = SceneManager.UnloadSceneAsync(name);
        while (!sync.isDone)
            yield return null;
    }

    /// <summary>
    /// <c>MonoBehaviour</c>の所属するSceneをUnloadする
    /// </summary>
    /// <param name="monoBehaviour"></param>
    public IEnumerator UnloadScene(MonoBehaviour monoBehaviour)
    {
        var sync = SceneManager.UnloadSceneAsync(monoBehaviour.gameObject.scene);
        while (!sync.isDone)
            yield return null;
    }

    /// <summary>
    /// TacticsSceneをLoadする
    /// </summary>
    public IEnumerator ShowTactics(string tacticsSceneID)
    {
        Speed = 1;
        IsLoading = true;
        yield return StartCoroutine(FadeInOutCanvas.Show());
        this.TacticsSceneID = tacticsSceneID;
        GameState = GameState.Tactics;
        MainMapAudioListener.enabled = false;
        yield return StartCoroutine(LoadScene(tacticsSceneID));
        MainMapCamera.enabled = false;
    }

    /// <summary>
    /// 現在表示中のTacticsSceneを破棄しもう一度同じTacticsSceneをLoadする
    /// </summary>
    public IEnumerator ReloadTacticsScene()
    {
        Speed = 1;
        IsLoading = true;
        yield return StartCoroutine(FadeInOutCanvas.Show());
        MainMapCamera.enabled = true;
        MainMapAudioListener.enabled = false;
        yield return StartCoroutine(UnloadScene(TacticsSceneID));
        yield return StartCoroutine(LoadScene(TacticsSceneID));
        MainMapCamera.enabled = false;
    }

    /// <summary>
    /// ストラテジーシーンを非表示にしてMainMapに戻る
    /// </summary>
    /// <param name="squadIDBackToLoc">拠点に帰還させるSquadが存在する場合はそのIDを入れる</param>
    public IEnumerator BackToMainMap(bool despawnEnemy, bool returnPlayer, Tactics.VictoryConditions.GameResult gameResult)
    {
        Speed = 1;
        IsLoading = true;
        yield return StartCoroutine(FadeInOutCanvas.Show());
        IsTimerStopping = false;
        var _tacticsSceneID = this.TacticsSceneID;
        TacticsSceneID = null;
        BackToMainMapHandler?.Invoke(this, new BackToMainMapHandlerArgs()
        {
            DespawnEnemy = despawnEnemy,
            ReturnPlayer = returnPlayer,
            EnemyID = ReachedEventArgs.Enemy.ID,
            GameResult = gameResult,
            ReachedEventArgs = ReachedEventArgs
        });
        this.ReachedEventArgs = null;
        MainMapCamera.enabled = true;
        yield return StartCoroutine(UnloadScene(_tacticsSceneID));
        MainMapAudioListener.enabled = true;
        GameState = GameState.MainMap;
    }

    /// <summary>
    /// StartSceneに戻る
    /// </summary>
    public IEnumerator BackToStartScene()
    {
        Speed = 1;
        IsLoading = true;
        yield return StartCoroutine(FadeInOutCanvas.Show());
        yield return StartCoroutine(LoadScene("StartScene"));
        yield return StartCoroutine(UnloadScene(TacticsSceneID));
        this.TacticsSceneID = null;
        GameState = GameState.Start;

        if (DataSavingController.SaveDataInfo != null)
            yield return StartCoroutine(UnloadScene(DataSavingController.SaveDataInfo.MainMapSceneName));
    }

    /// <summary>
    /// GameManagerに画面遷移後のシーンのロードが終了したことを伝える
    /// </summary>
    public void NortifyCompleteToLoad(float delay = 0)
    {
        IsLoading = false;
    }
    #endregion

}

//シングルトンなMonoBehaviourの基底クラス
public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    static T instance;
    public static T Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = (T)FindObjectOfType(typeof(T));

            if (instance == null)
            {
                Debug.LogWarning(typeof(T) + " is nothing");
            }

            return instance;
        }
    }

    public static T InstanceNullable
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError(typeof(T) + " is multiple created", this);
            return;
        }

        instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

/// <summary>
/// 翻訳用クラス
/// </summary>
public class Translation
{
    /// <summary>
    /// UIの共通翻訳
    /// </summary>
    public INIParser CommonUserInterfaceIni { private set; get; } = new INIParser();

    /// <summary>
    /// Object用のScene固有の翻訳
    /// </summary>
    public INIParser SceneObjectsIni { private set; get; } = new INIParser();

    /// <summary>
    /// Stroy用のscene固有の翻訳
    /// </summary>
    public INIParser SceneStoryIni { private set; get; } = new INIParser();

    public const string COMMON_USER_INTERFACE_INI_FILE_NAME = "UserInterface.ini";
    public const string SCENE_OBJECTS_INI_FILE_NAME = "objects_*.ini";
    public const string SCENE_STORY_INI_FILE_NAME = "story_*.ini";

    private readonly string language;

    public List<string> AvailableLanguages = new List<string>();

    public Translation(string language)
    {
        this.language = language;
        var path = "Assets/Data/Translation";
        var files = Directory.GetDirectories(path);
        AvailableLanguages = files.ToList().ConvertAll(f => Path.GetDirectoryName(f));
        if (!AvailableLanguages.Contains(language))
            this.language = "English";
        LoadCommonTranslation();
    }

    /// <summary>
    /// 共通の翻訳ファイルを読み込む
    /// </summary>
    public void LoadCommonTranslation()
    {
        ReadUserInterfaceIni();
    }

    /// <summary>
    /// ユーザーインターフェース用のIniを取得する
    /// </summary>
    /// <param name="language"></param>
    private void ReadUserInterfaceIni()
    {
        CommonUserInterfaceIni.Open($"Assets/Data/Translation/{language}/" + COMMON_USER_INTERFACE_INI_FILE_NAME);
    }

    /// <summary>
    /// GameScene固有の翻訳ファイルを読み込む
    /// </summary>
    /// <param name="gameSceneName"></param>
    public void LoadFromGameSceneName(string gameSceneName)
    {
        ReadStoryIni(gameSceneName);
        ReadObjectsIni(gameSceneName);
    }

    /// <summary>
    /// オブジェクト名の翻訳のiniを取得
    /// </summary>
    private void ReadObjectsIni(string gameSceneName)
    {
        var text = ReadFiles(gameSceneName, SCENE_OBJECTS_INI_FILE_NAME);
        SceneObjectsIni.OpenFromString(text);
    }

    /// <summary>
    /// ストーリーのiniを取得
    /// </summary>
    /// <param name="gameSceneName"></param>
    private void ReadStoryIni(string gameSceneName)
    {

        var story = ReadFiles(gameSceneName, SCENE_STORY_INI_FILE_NAME);
        SceneStoryIni.OpenFromString(story);
    }

    /// <summary>
    /// ディレクトリ内にあるfileNameに適合するすべてのファイルを読み込みこれを接続して返す
    /// </summary>
    /// <param name="gameSceneName">シーン名 Directory名に直結</param>
    /// <param name="fileName">ファイル名前の形式</param>
    /// <returns>directory内にある全てのiniファイルと</returns>
    private string ReadFiles(string gameSceneName, string fileName)
    {
        var directoryPath = $"Assets/Data/Static/{gameSceneName}/Translation/{language}";
        string[] filePaths = Directory.GetFiles(directoryPath, fileName, SearchOption.AllDirectories);
        StringBuilder sb = new StringBuilder();
        foreach (string filePath in filePaths)
        {
            sb.Append(File.ReadAllText(filePath));
        }
        return sb.ToString();
    }
}


/// <summary>
/// TacticsからMapに戻るときのイベント
/// </summary>
public class BackToMainMapHandlerArgs : EventArgs
{
    /// <summary>
    /// Tactics終了後EnemyをMapから消すか
    /// </summary>
    public bool DespawnEnemy;
    /// <summary>
    /// Tactics終了後PlayerをMapから基地に戻すか
    /// </summary>
    public bool ReturnPlayer;
    /// <summary>
    /// Tacticsで戦闘を行ったEnemyの部隊ID
    /// </summary>
    public string EnemyID;
    /// <summary>
    /// Tacticsのゲーム結果
    /// </summary>
    public Tactics.VictoryConditions.GameResult GameResult;
    /// <summary>
    /// 戦闘を行った相手の情報
    /// </summary>
    public ReachedEventArgs ReachedEventArgs;
}

/// <summary>
/// Gameがどのような状態であるかを示すenum
/// </summary>
public enum GameState
{
    Start,
    /// <summary>
    /// 設定中
    /// </summary>
    Setting,
    MainMap,
    Tactics,
    Event
}

/// <summary>
/// どの方角からスタートするか
/// </summary>
public enum StartPosition
{
    None,
    North,
    South,
    East,
    West,
}