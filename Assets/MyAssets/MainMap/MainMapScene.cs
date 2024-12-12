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
        [Tooltip("Gameの開始する時間 GameManagerに上げられてCurrentTimeになる")]
        [SerializeField] SerializableDateTime startDateTime;
        [SerializeField] CinemachineVirtualCamera uiVirtualCamera;
        [SerializeField] CinemachineVirtualCamera tableVirtualCamera;
        [Tooltip("Gameの開始時のVirtualCamera")]
        [SerializeField] CinemachineVirtualCamera startVirtualCamera;
        [Tooltip("MapにSaveDataの内容が展開されているか")]
        [SerializeField, ReadOnly] public bool IsMapDataLoaded = false;

        GameManager gameManager;

        /// <summary>
        /// GameのMapに固有なMapObject 違うMapでは別のものが用意されている
        /// </summary>
        MainMapController mainMapController;
        /// <summary>
        /// GameのMapに固有なUI 違うMapでは別のものが用意されている
        /// </summary>
        UI.MainUIController mainUIController;

        [Tooltip("mapを直接起動している場合")]
        [SerializeField, ReadOnly] public bool isDebugMode = false;

        protected private void Awake()
        {
            gameObject.SetActive(true);
            // 同じシーンにMainMapControllerがあるか
            mainMapController = FindObjectOfType<MainMapController>();
            mainUIController = FindObjectOfType<UI.MainUIController>();
            // MainMapControllerがあらかじめある場合はDebugModeになる
            if (mainMapController != null)
            {
                isDebugMode = true;
                mainMapController.tableVirtualCamera = tableVirtualCamera;
                mainMapController.uiVirtualCamera = uiVirtualCamera;
            }

            gameManager = GameManager.Instance;
            // Tacticsから戻ってきた時の処理
            gameManager.BackToMainMapHandler = (o, args) =>
            {
                gameObject.SetActive(true);
                StartCoroutine(CalledWhenReturnFromTactics(o, args));
            };

            gameManager.MainMapScene = this;
        }

        // Start is called before the first frame update
        protected private void Start()
        {
            // デバッグモードならばMainMapControllerがあらかじめあるのでそちらを使う
            if (isDebugMode)
            {
                IsMapDataLoaded = false;
                mapSceneID = mainMapController.mapSceneID;
                gameManager.LoadDebugData(mapSceneID);
                StartCoroutine(SetDataToMap());
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
            if (mainMapController != null)
            {
                Destroy(mainMapController.transform.parent.gameObject);
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
            var mainMapObject = Instantiate(handle.Result);
            mainMapController = mainMapObject.GetComponentInChildren<MainMapController>();
            mainUIController = mainMapController.MainUIController;

            if (mainMapController == null)
            {
                Debug.LogError("MainMapControllerがありません");
                yield break;
            }

            mainMapController.tableVirtualCamera = tableVirtualCamera;
            mainMapController.uiVirtualCamera = uiVirtualCamera;

            yield return StartCoroutine(SetDataToMap());
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
            yield return StartCoroutine(mainMapController.LoadData());
            mainUIController.CompleteToLoad();
            IsMapDataLoaded = true;
            gameManager.HasMainMapLoaded = true;
        }

        /// <summary>
        /// LoadDataおよびCameraの切り替えが完了した際に呼び出される
        /// </summary>
        public void CompleteToLoad()
        {
            if (mainMapController != null)
            {
                mainMapController.CheckEventAtFirstTime();
            }
        }

        /// <summary>
        /// Tactics画面に遷移する際の処理
        /// </summary>
        public void TransitToTacticsScene()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Tactics画面から戻ってきた時呼び出し エンカウント時にsetActive = falseし一時停止したものを再開する
        /// </summary>
        private IEnumerator CalledWhenReturnFromTactics(object o, BackToMainMapHandlerArgs arg)
        {
            GameManager.Instance.NortifyCompleteToLoad();
            Print($"Tactics end: despawnEnemy.{arg.DespawnEnemy}, returnPlayer.{arg.ReturnPlayer}");
            yield return StartCoroutine(GameManager.Instance.EventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.AfterResultScene,
                                                                                                    arg.EncounterEnemyID,
                                                                                                    arg.GameResult));
            mainUIController.CalledWhenReturnFromTactics();
            mainMapController.CalledWhenReturnFromTactics(arg.DespawnEnemy, arg.ReturnPlayer);
        }

    }
}