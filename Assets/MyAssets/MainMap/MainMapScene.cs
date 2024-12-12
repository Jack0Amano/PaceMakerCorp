using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Utility;

namespace MainMap
{
    /// <summary>
    /// マップの最上位クラス 主に画面遷移を担当
    /// </summary>
    public class MainMapScene : MonoBehaviour
    {
        [Tooltip("Gameの動くシーンの名前")]
        [SerializeField, ReadOnly] string mapSceneID;
        [SerializeField] CinemachineVirtualCamera uiVirtualCamera;
        [SerializeField] CinemachineVirtualCamera tableVirtualCamera;
        [Tooltip("MapにSaveDataの内容が展開されているか")]
        [SerializeField, ReadOnly] public bool IsMapDataLoaded = false;

        /// <summary>
        /// GameのMapに固有なUI 違うMapでは別のものが用意されている
        /// </summary>
        [SerializeField] UI.MainUIController mainUIController;

        [SerializeField] UI.InfoPanel.SquadsInfoPanel squadsInfoPanel;

        [SerializeField] UI.TableIcons.TableIconsPanel tableIconsPanel;

        [SerializeField] MapUI.UI.InfoPanel.Calender calender;

        [Tooltip("mapを直接起動している場合")]
        [SerializeField, ReadOnly] public bool IsDebugMode = false;

        GameManager gameManager;
        /// <summary>
        /// GameのMapに固有なMapObject 違うMapでは別のものが用意されている
        /// </summary>
        public MainMapController MainMapController { private set; get; }
        /// <summary>
        /// MapのGameObject
        /// </summary>
        GameObject mainMapObject;

        /// <summary>
        /// セーブをTempSaveから行うべき状態であるか
        /// </summary>
        public bool IsTempSave
        {
            get
            {
                if (MainMapController == null)
                {
                    if (MainMapController.MapSquads.IsSquadMoving)
                    {
                        // Squadが移動中の場合はセーブは以前位置した場所で行われたTempSaveが選ばれる
                        return true;
                    }
                }
                return false;
            }
        }

        protected private void Awake()
        {
            gameObject.SetActive(true);
            // 同じシーンにMainMapControllerがあるか
            MainMapController = FindObjectOfType<MainMapController>();
            mainUIController = FindObjectOfType<UI.MainUIController>();
            // MainMapControllerがあらかじめある場合はDebugModeになる
            if (MainMapController != null)
            {
                IsDebugMode = true;
                MainMapController.tableVirtualCamera = tableVirtualCamera;
                MainMapController.uiVirtualCamera = uiVirtualCamera;
            }

            gameManager = GameManager.Instance;
            // Tacticsから戻ってきた時の処理
            gameManager.BackToMainMapHandler = (o, args) =>
            {
                // GammeObjectが非アクティブになっている際にCorutineを実行するとエラーが出るため
                gameObject.SetActive(true);

                StartCoroutine(CalledWhenReturnFromTactics(o, args));
            };

            gameManager.MainMapScene = this;
        }

        // Start is called before the first frame update
        protected private void Start()
        {
            // デバッグモードならばMainMapControllerがあらかじめあるのでそちらを使う
            if (IsDebugMode)
            {
                IsMapDataLoaded = false;
                mapSceneID = MainMapController.mapSceneID;
                gameManager.LoadDebugData(mapSceneID);
                StartCoroutine(SetDataToMap());
                SetParameterToMainMapController();
            }
            else
            {
                // 通常の起動
                gameManager.StartCanvasController.Show(StartUI.StartPosition.EnterBuilding);
            }
        }

        /// <summary>
        /// GameManagerからMapの破棄を求められた際の呼び出し
        /// </summary>
        public void OnDestroyMap()
        {
            if (MainMapController != null)
            {
                Destroy(MainMapController.transform.parent.gameObject);
                MainMapController = null;
            }
            
        }

        /// <summary>
        /// GameManagerからMapのロードを求められた際の呼び出し\n
        /// Mapデータをaddressableからロードし、ロードが完了したらデータをセットする
        /// </summary>
        public IEnumerator LoadMap(string mapSceneID)
        {

            IsMapDataLoaded = false;
            var handle = Addressables.LoadAssetAsync<GameObject>(mapSceneID);
            yield return handle;
            mainMapObject = Instantiate(handle.Result);
            MainMapController = mainMapObject.GetComponentInChildren<MainMapController>();

            if (MainMapController == null)
            {
                Debug.LogError("MainMapControllerがありません");
                yield break;
            }

            SetParameterToMainMapController();

            yield return StartCoroutine(SetDataToMap());

            gameManager.MainMapScene.CompleteToLoad();

        }

        /// <summary>
        /// MainMapControllerに必要なパラメータをセットする
        /// </summary>
        private void SetParameterToMainMapController()
        {
            MainMapController.TableIconsPanel = tableIconsPanel;
            MainMapController.MainUIController = mainUIController;
            MainMapController.SquadsInfoPanel = squadsInfoPanel;
            MainMapController.mainMapScene = this;
            MainMapController.tableVirtualCamera = tableVirtualCamera;
            MainMapController.uiVirtualCamera = uiVirtualCamera;
        }

        /// <summary>
        /// データから環境をロードする
        /// </summary>
        private IEnumerator SetDataToMap()
        {
            IsMapDataLoaded = false;
            if (IsMapDataLoaded) yield break;
            while (!GameManager.Instance.DataSavingController.HasDataLoaded)
                yield return null;

            // TODO: 必要であればMainUIConに暗転Requestを送る
            yield return StartCoroutine(MainMapController.LoadData());
            calender.UpdateTime();
            gameManager.AddTimeEventHandlerAsync += (o, a) => calender.UpdateTime();

            mainUIController.CompleteToLoad();
            IsMapDataLoaded = true;
            gameManager.HasMainMapLoaded = true;
        }

        /// <summary>
        /// LoadDataおよびCameraの切り替えが完了した際に呼び出される
        /// </summary>
        public void CompleteToLoad()
        {
            if (MainMapController != null)
            {
                StartCoroutine(MainMapController.CheckEventAtFirstTime());
            }
            gameManager.NortifyCompleteToLoad();
        }

        /// <summary>
        /// SaveDataがTacticsSceneの物である場合の処理
        /// </summary>
        public void CheckLoadDataFromTacticsScene()
        {
            print($"CheckLoadDataFromTacticsScene: {MainMapController}");
            if (MainMapController != null)
            {
                MainMapController.CheckEncountedSquad();
            }
        }

        /// <summary>
        /// Tactics画面に遷移する際の処理
        /// </summary>
        public IEnumerator TransitToTacticsScene(ReachedEventArgs reachedEventArgs)
        {
            GameManager.Instance.ReachedEventArgs = reachedEventArgs;
            yield return StartCoroutine(GameManager.Instance.ShowTactics(reachedEventArgs.TacticsSceneID));
            mainUIController.CalledWhenEncount();
            mainMapObject.SetActive(false);
            //gameObject.SetActive(false);
            tableVirtualCamera.Priority = 0;
            uiVirtualCamera.Priority = 0;
        }

        /// <summary>
        /// Tactics画面から戻ってきた時呼び出し エンカウント時にsetActive = falseし一時停止したものを再開する
        /// </summary>
        private IEnumerator CalledWhenReturnFromTactics(object o, BackToMainMapHandlerArgs arg)
        {
            mainMapObject.SetActive(true);
            gameManager.NortifyCompleteToLoad();
            StartCoroutine(gameManager.FadeInOutCanvas.Hide());
            Print($"Tactics end: despawnEnemy.{arg.DespawnEnemy}, returnPlayer.{arg.ReturnPlayer}");
            yield return StartCoroutine(GameManager.Instance.EventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.AfterResultScene,
                                                                                                    arg.EnemyID,
                                                                                                    arg.GameResult));
            mainUIController.CalledWhenReturnFromTactics();
            MainMapController.CalledWhenReturnFromTactics(arg);
        }

    }
}