using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System;
using Cinemachine;
using MainMap.UI.TableIcons;
using static Utility;
using EventGraph.InOut;
using AmplifyShaderEditor;

namespace MainMap
{
    /// <summary>
    /// マップのコントローラー 主にMapLocationsで場所 MapSquadsで部隊の管理を行う
    /// </summary>
    public class MainMapController : MonoBehaviour
    {
        [Header("Map Controlling")]
        [SerializeField] float zoomMax;
        [SerializeField] float zoomMin;
        [SerializeField] float mouseWheelSencitive = 0.5f;
        [SerializeField] float keyMoveSencitive = 0.1f;
        [SerializeField] AnimationCurve mapMoveCurve;
        [Tooltip("マップ固有のSceneID")]
        [SerializeField] internal string mapSceneID;

        [Header("Map limits")]
        [SerializeField] Transform mapLeftUp;
        [SerializeField] Transform mapLeftDown;
        [SerializeField] Transform mapRightUp;
        [SerializeField] Transform mapRightDown; 

        [Header("Map Objects")]
        [SerializeField] GameObject mapObject;

        [Header("Table limits")]
        [SerializeField] Transform tableLeftUp;
        [SerializeField] Transform tableLeftDown;
        [SerializeField] Transform tableRightUp;
        [SerializeField] Transform tableRightDown;

        [Header("Child Components")]
        [SerializeField] internal MapSquads MapSquads;
        [SerializeField] internal Spawn.MapSpawns MapSpawns;
        [SerializeField] internal MapLocations MapLocations;

        [NonSerialized] public TableIconsPanel TableIconsPanel;
        [NonSerialized] public UI.InfoPanel.SquadsInfoPanel SquadsInfoPanel;
        [NonSerialized] public UI.MainUIController MainUIController;

        private new Camera camera;

        /// <summary>
        /// Mapをユーザー入力でコントロール可能か
        /// </summary>
        internal bool AbleToControl { private set; get; } = false;

        private Sequence mapMoveSequence;

        private Vector2 cursorPosition;
        // private int MapRayLayer;

        /// <summary>
        /// 現在Mapで何かしらの動作が行われて移動中である
        /// </summary>
        public bool IsControlling { private set; get; } = false;

        /// <summary>
        /// 変化しないマップサイズの厚み
        /// </summary>
        static private float MapSizeY;
        /// <summary>
        /// 現在のカーソルに近い道路上の地点
        /// </summary>
        // private Vector3? CursorPositionOnRoad;

        public static float MapScale { private set; get; }
        public static Vector3 MapPosition { private set; get; }

        internal MainMapScene mainMapScene;

        private GameManager gameManager;

        internal CinemachineVirtualCamera uiVirtualCamera;
        internal CinemachineVirtualCamera tableVirtualCamera;

        /// <summary>
        /// カメラ位置
        /// </summary>
        public MainMapCameraMode CameraMode
        {
            get => cameraMode;
            set
            {
                cameraMode = value;
                if (value == MainMapCameraMode.Table)
                {
                    uiVirtualCamera.Priority = 0;
                    tableVirtualCamera.Priority = 1;
                }
                else if (value == MainMapCameraMode.UI)
                {
                    uiVirtualCamera.Priority = 1;
                    tableVirtualCamera.Priority = 0;
                }
            }
        }
        private MainMapCameraMode cameraMode = MainMapCameraMode.Table;
        /// <summary>
        /// MainMapControllerからGameSettingCanvasが有効化されているかどうか。Squadの停止と再開に必要なパラメーター
        /// </summary>
        private bool isGameSettingCanvasActive = false;

        protected private void Awake()
        {
            gameManager = GameManager.Instance;

            MapSizeY = mapObject.transform.localScale.y;

            MapSquads.MapLocations = MapLocations;
            MapSquads.MapSpawns = MapSpawns;

            gameManager.SceneParameter = GetComponent<SceneParameter>();

            // MapRayLayer = LayerMask.GetMask("TableMapRay");

            // UIControllerからMapのSquad等をコントロールする際のリクエスト
            //if (UIController.SquadsPanel.squadDetail.fastTravelRequest == null)
            //    UIController.SquadsPanel.squadDetail.fastTravelRequest = FastTravel;

            if (MapSquads.ReachedEventHandler == null)
                MapSquads.ReachedEventHandler = ReachedAtLocation;

            gameManager.PassTimeEventHandlerAsync += PassTimeNortificationAsync;
            gameManager.EventSceneController.BeginEventHandler += BeginEventAction;
            gameManager.EventSceneController.EndEventHandler += EndEventAction;

            gameManager.EventSceneController.AddUnitRequest += AddUnitRequest;
        }



        // Start is called before the first frame update
        protected void Start()
        {
            camera = Camera.main;
            CameraMode = MainMapCameraMode.Table;

            if (TableIconsPanel)
                InitParameters();
        }

        private void OnDestroy()
        {
            gameManager.PassTimeEventHandlerAsync -= PassTimeNortificationAsync;
            gameManager.EventSceneController.BeginEventHandler -= BeginEventAction;
            gameManager.EventSceneController.EndEventHandler -= EndEventAction;
            TableIconsPanel.ClearSquadImages();
            TableIconsPanel.ClearLocationImages();
        }

        /// <summary>
        /// MainMapSceneでセットされたパラメーターを各種に振り分ける
        /// </summary>
        private void InitParameters()
        {
            TableIconsPanel.SelectSquadAction = (s => ClickSquad(s.MapSquad));
            TableIconsPanel.SelectLocationAction = (l => ClickLocation(l.MapLocation));

            TableIconsPanel.OnPointerExitLocationAction = (l => CursorExitFromLocation(l));
            TableIconsPanel.OnPointerEnterLocationAction = (l => CursorEnterOnLocation(l));

            TableIconsPanel.OnPointerEnterSquadAction = (s => CursorEnterOnSquad(s));
            TableIconsPanel.OnPointerExitSquadAction = (s => CursorExitFromSquad(s));

            TableIconsPanel.SupplyBalloon.OnPointerEnterAction = (s => CursorEnterOnSupplyOrLocation(s));
            TableIconsPanel.SupplyBalloon.OnPointerExitAction = (s => CursorExitFromSupplyOrLocation(s));
            TableIconsPanel.SupplyBalloon.OnPointerClickAction = (s => CursorOnClickSupplyOrLocation(s, true));

            TableIconsPanel.LocationBalloon.OnPointerEnterAction = (s => CursorEnterOnSupplyOrLocation(s));
            TableIconsPanel.LocationBalloon.OnPointerExitAction = (s => CursorExitFromSupplyOrLocation(s));
            TableIconsPanel.LocationBalloon.OnPointerClickAction = (s => CursorOnClickSupplyOrLocation(s, false));

            TableIconsPanel.ReturnBaseBalloon.OnPointerClickAction = (s => ReturnSquadAction(s.MapSquad));

            MapSquads.tableOverlayPanel = TableIconsPanel;

            MapSquads.infoPanel = SquadsInfoPanel;
            SquadsInfoPanel.cardIsSelectedAction = InfoPanelCardOnClick;
            SquadsInfoPanel.returnSquadAction = ReturnSquadAction;
        }

        /// <summary>
        /// DataControllerの中身をロードしてMapに反映させる
        /// </summary>
        public IEnumerator LoadData()
        {
            InitParameters();

            var locationCoroutine = StartCoroutine(MapLocations.LoadData());
            var squadsCoroutine = StartCoroutine(MapSquads.LoadData());

            yield return locationCoroutine;
            yield return squadsCoroutine;

            MapLocations.locations.ForEach(l =>
            {
                l.LocationImage = TableIconsPanel.AddLocationImage(l);
            });
            TableIconsPanel.UpdateLocationsPosition();

            // TODO: すべてのロードが終了した状態

            // var gameTime = GameManager.SavedData.saveDataInfo.gameTime;
            //timeController.SetDate(gameTime.Year, gameTime.Month, gameTime.Day);
            // var timeline = (float)gameTime.Hour + ((float)gameTime.Minute / 60);
            //timeController.SetTimeline(timeline);

            MapSpawns.CompleteToLoadData();

            var squadDetail = MainUIController.SquadsPanel.squadDetail;
            squadDetail.activateButton.onClick.AddListener(() => SquadReadyToGo(squadDetail.Squad));

            TableIconsPanel.UpdateSquadPosition();

            yield return new WaitForSeconds(1);
            AbleToControl = true;
        }

        /// <summary>
        /// ロード後の初めてのイベントチェック
        /// </summary>
        public IEnumerator CheckEventAtFirstTime()
        {
            //var currentEventPacks = GameManager.EventController.enabledEventPacks.FindAll(p =>
            //{
            //    return p.nextEvent.startTrigger.IsToggledWhenFreeTiming();
            //});
            //if (currentEventPacks.Count == 0) return;
            //GameManager.eventSceneController.messageEvent.AddEvents(currentEventPacks) ;
            yield return StartCoroutine(gameManager.EventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.GameStart));
            if (gameManager.EventSceneController.messageEvent.IsEventSceneActive)
            {
                while (gameManager.EventSceneController.messageEvent.IsEventSceneActive)
                    yield return null;
            }

        }

        // Update is called once per frame
        protected void Update()
        {
            IsControlling = false;

            if (gameManager.StartCanvasController.IsEnable && !isGameSettingCanvasActive)
                MapSquads.StopAllSquads = isGameSettingCanvasActive = true;
            else if (!gameManager.StartCanvasController.IsEnable && isGameSettingCanvasActive)
                MapSquads.StopAllSquads = isGameSettingCanvasActive = false;

            KeyShowInfo();


            if (!AbleToControl || !MainUIController.AbleToShow)
            {
                // イベント中ではMapコントロールは行わない
                return;
            }
            //else if (!UIController.AbleToShow && UserController.KeyShowInfo)
            //{
            //    // UI表示によるポーズを解除
            //    RestartTimer();
            //    UIController.Hide();
            //    mapSquads.StopAllSquads = false;
            //}
            //else if (!UIController.AbleToShow)
            //{
            //    return;
            //}

            //_MouseWheelControl();

            if (UserController.MouseMoving)
            {
                var rayDistance = 600;
                if (camera == null)
                    return;
                var ray = camera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
                {
                    var hitObject = hit.collider.gameObject;
                    cursorPosition = hit.point;
                    cursorPosition.y = mapObject.transform.position.y;
                }
            }

            if (UserController.MouseGrab)
            {

            }

            // _MouseClickDown();
            // _MouseClickUp();
            //ShootRayToMap();
            // TODO Table一枚にマップを収めたためとりあえずZoomしない
            // MapZoom();
            // MapKeyControl();

            // Squadを可能ならばLocationに戻す
            if (UserController.KeyReturnToBase && !MapSquads.ReturnAnimPlaying && MapSquads.SelectedSquad != null)
            {
                StartCoroutine(MapSquads.SquadReturnToBase(MapSquads.SelectedSquad, false));
                return;
            }

            // ClickOnMap();

            //if (Input.GetKeyDown(KeyCode.F1))
            //{
            //    // Debug用にSquadを設置
            //    StartCoroutine(MapSquads.PutSquadOnMap(null, "Location.SaiGon"));
            //}
            //if (UserController.KeyCodeSpace)
            //    MapSquads.Squads.ForEach(s => s.CancelMoveAnimation());

            //GameController.Instance.mainMapScale = mapObject.transform.localScale.x;
            //GameController.Instance.mainMapPosition = mapObject.transform.position;
            MapScale = mapObject.transform.localScale.x;
            MapPosition = mapObject.transform.position;
        }

        #region マップ操作
        void MapKeyControl()
        {
            if (mapMoveSequence.IsActive() && mapMoveSequence.IsPlaying())
            {
                // 現在Mapのアニメーション中
                mapMoveSequence.Kill();
                mapMoveSequence = DOTween.Sequence();
                mapMoveSequence.SetEase(Ease.Linear);
            }
            else
            {
                mapMoveSequence = DOTween.Sequence();
                mapMoveSequence.SetEase(Ease.InQuad);
            }

            AddMoveMapHorizontal(UserController.KeyHorizontal);
            AddMoveMapVertical(UserController.KeyVertical);
            mapMoveSequence.Play();
        }

        bool isOnHorizontalLeftEdge = false;
        bool isOnHorizontalRightEdge = false;
        /// <summary>
        /// Mapの水平方向への移動を行う
        /// </summary>
        void AddMoveMapHorizontal(float value, float duration=1f/60f)
        {
            if (value == 0)
                return;

            var deltaPos = new Vector3(value * keyMoveSencitive, 0, 0);
            var left = mapLeftUp.position + deltaPos;
            var right = mapRightUp.position + deltaPos;

            if (tableLeftUp.position.x > left.x && tableRightUp.position.x < right.x && 
                !isOnHorizontalRightEdge && 
                !isOnHorizontalLeftEdge)
            {
                isOnHorizontalLeftEdge = false;
                isOnHorizontalRightEdge = false;

                mapMoveSequence.Join(mapObject.transform.DOMoveX(mapObject.transform.position.x + deltaPos.x, duration));
                IsControlling = true;
            }
            else if (tableLeftUp.position.x <= left.x)
            {
                // Leftの端に位置している
                isOnHorizontalLeftEdge = true;
                isOnHorizontalRightEdge = false;
            }
            else if (tableRightUp.position.x >= right.x)
            {
                // Rightの端に位置している
                isOnHorizontalLeftEdge = false;
                isOnHorizontalRightEdge = true;
            }
            else if (isOnHorizontalLeftEdge && !isOnHorizontalRightEdge)
            {
                if (deltaPos.x < 0)
                {
                    // Leftの端から移動中
                    isOnHorizontalRightEdge = false;
                    isOnHorizontalLeftEdge = false;

                    mapMoveSequence.Join(mapObject.transform.DOMoveX(mapObject.transform.position.x + deltaPos.x, duration));
                    IsControlling = true;
                }
            }
            else if (!isOnHorizontalLeftEdge && isOnHorizontalRightEdge)
            {
                if (deltaPos.x > 0)
                {
                    isOnHorizontalRightEdge = false;
                    isOnHorizontalLeftEdge = false;
                    mapMoveSequence.Join(mapObject.transform.DOMoveX(mapObject.transform.position.x + deltaPos.x, duration));
                    IsControlling = true;
                }
                    
            }
        }

        bool isOnVerticalDownEdge = false;
        bool isOnVerticalUpEdge = false;
        /// <summary>
        /// Mapの水平方向への移動を行う
        /// </summary>
        void AddMoveMapVertical(float value, float duration=1f/60f)
        {
            if (value == 0) return;

            var deltaPos = new Vector3(0, 0, value * keyMoveSencitive);
            var up = mapLeftUp.position + deltaPos;
            var down = mapLeftDown.position + deltaPos;

            if (tableLeftUp.position.z < up.z && tableLeftDown.position.z > down.z &&
                !isOnVerticalUpEdge &&
                !isOnVerticalDownEdge)
            {
                isOnVerticalUpEdge = false;
                isOnVerticalDownEdge = false;

                mapMoveSequence.Join(mapObject.transform.DOMoveZ(mapObject.transform.position.z + deltaPos.z, duration));
                IsControlling = true;
            }
            else if (tableLeftUp.position.z >= up.z)
            {
                // Upの端に位置している
                isOnVerticalUpEdge = true;
                isOnVerticalDownEdge = false;
            }
            else if (tableLeftDown.position.z <= down.z)
            {
                // Downの端に位置している
                isOnVerticalUpEdge = false;
                isOnVerticalDownEdge = true;
            }
            else if (isOnVerticalUpEdge && !isOnVerticalDownEdge)
            {
                if (deltaPos.z > 0)
                {
                    // Upの端から移動中
                    isOnVerticalUpEdge = false;
                    isOnVerticalDownEdge = false;

                    mapMoveSequence.Join(mapObject.transform.DOMoveZ(mapObject.transform.position.z + deltaPos.z, duration));
                    IsControlling = true;
                }
            }
            else if (!isOnVerticalUpEdge && isOnVerticalDownEdge)
            {
                if (deltaPos.z < 0)
                {
                    // Downの端から移動中
                    isOnVerticalUpEdge = false;
                    isOnVerticalDownEdge = false;

                    mapMoveSequence.Join(mapObject.transform.DOMoveZ(mapObject.transform.position.z + deltaPos.z, duration));
                    IsControlling = true;
                }
            }
        }

        void MapZoom()
        {
            // TODO Squadが移動中の場合Zoomは不可、Squadに追従する形でのZoomがあってもいいかも
            if (MapSquads.IsSquadMoving)
                return;

            if (mapObject.transform.localScale.x > zoomMax)
                mapObject.transform.localScale = new Vector3(zoomMax, MapSizeY, zoomMax);
            else if (mapObject.transform.localScale.x < zoomMin)
                mapObject.transform.localScale = new Vector3(zoomMin, MapSizeY, zoomMin);

            var deltaValue = UserController.MouseWheel * mouseWheelSencitive;
            var size = mapObject.transform.localScale;
            size.x += deltaValue;
            size.y += deltaValue;
            size.z += deltaValue;
            if (deltaValue == 0) return;

            if (mapObject.transform.localScale.x == zoomMax)
            {
                // 拡大の最大値に達している deltaValueがマイナス値のみ受け付ける
                if (deltaValue >= 0)
                    return;
            }
            else if (mapObject.transform.localScale.x == zoomMin)
            {
                // 拡大の最小値に達している deltaValueがプラス値のみ受け付ける
                if (deltaValue <= 0)
                    return;
            }
            else
            {
                IsControlling = true;

                // 拡大縮小範囲内に存在する
                if (size.x > zoomMax)
                    size = new Vector3(zoomMax, zoomMax, zoomMax);
                else if (size.x < zoomMin)
                    size = new Vector3(zoomMin, zoomMin, zoomMin);
            }

            mapObject.transform.DOScale(size, 0.2f);
        }

        /// <summary>
        /// Map全体を見渡せる位置とZoomに変更
        /// </summary>
        internal void ShowWholeMap()
        {

        }

        #endregion

        #region Mapをクリックした時

        /// <summary>
        /// Mapの上のSquadでクリックした
        /// </summary>
        /// <param name="mapSquad"></param>
        void ClickSquad(MapSquad mapSquad)
        {
            if (!MapSquads.IsSquadReadyToGo)
            {
                if (mapSquad.Equals(MapSquads.SelectedSquad))
                {
                    // 同じSquadを選択した
                    // 選択解除
                    SetSquadAsDeselected(mapSquad);
                    // StartCoroutine(infoPanel.Hide());
                }
                else
                {
                    // Squadにハイライトを当てる
                    // 新規選択 or 別Squad選択
                    CursorClickNewSquad(mapSquad);
                    // StartCoroutine(infoPanel.Show(mapSquads.selectedSquad.info));
                }
            }
        }

        /// <summary>
        /// Mapの上のlocationでクリックした
        /// </summary>
        /// <param name="location"></param>
        void ClickLocation(MapLocation location)
        {
            if (!MapSquads.IsSquadReadyToGo)
            {
                if (MapSquads.SelectedSquad != null)
                {
                    // Squadを選択しておいてからLocationを選択した
                    // Print("Select location", location, "After", MapSquads.SelectedSquad);
                    // StartCoroutine(mapSquads.selectedSquad.MoveTo(hitObject.transform.position));
                    //mapSquads.selectedSquad.MoveTo
                    CursorClickLocationWithSquad(location, MapSquads.SelectedSquad);

                }
                else if (MapLocations.selectedLocation != null && location.Equals(MapLocations.selectedLocation))
                {
                    // Print("Deselect location", location);
                    // 同じLocationを選択した
                    MapSquads.SelectedSquad = null;
                    MapLocations.selectedLocation = null;
                    //StartCoroutine(infoPanel.Hide());
                }
                else
                {
                    // Print("Select location", location);
                    // Locationにハイライトを当てる
                    MapSquads.SelectedSquad = null;
                    MapLocations.selectedLocation = location;
                    // StartCoroutine(infoPanel.Show(location: mapLocations.selectedLocation.info));
                }
            }
            else
            {
                // Print("Spawn squad", MapSquads.SelectedSquad);
                // mapSquads.ReadySquadをlocationにスポーンさせる
                if (location.SpawnSquadOnLocation == null)
                {
                    MapLocations.locations.ForEach(l => l.EndFlashingTowerLight());

                    var newSquad = MapSquads.PutReadySquadOnLocation(location);
                    TableIconsPanel.UpdateSquadPosition(newSquad.SquadImage);
                    MapSquads.SelectedSquad = newSquad;
                }
            }
            
        }

        /// <summary>
        /// SquadからMapSquadを選択状態にする
        /// </summary>
        /// <param name="squad"></param>
        internal void SetSquadAsSelected(Squad squad)
        {
            var selectedSquad = MapSquads.Squads.Find(s => s.data == squad);
            if (selectedSquad == null) return;

            if (selectedSquad.Equals(MapSquads.SelectedSquad))
            {
                // 既に選択済みのSquadを再選択しようとしているためアニメーションで通知する
                selectedSquad.SquadImage.AnimationSquadIsAlreadySelected();
                return;
            }

            if (MapSquads.SelectedSquad != null)
                SetSquadAsDeselected(MapSquads.SelectedSquad);

            MapSquads.SelectedSquad = selectedSquad;
            MapLocations.selectedLocation = null;
            SquadsInfoPanel.SetInteractive(selectedSquad.data, true);
        }

        /// <summary>
        /// Squadを選択状態にする
        /// </summary>
        /// <param name="mapSquad"></param>
        internal void SetSquadAsSelected(MapSquad mapSquad)
        {
            if (MapSquads.SelectedSquad != null)
                SetSquadAsDeselected(MapSquads.SelectedSquad);

            MapLocations.selectedLocation = null;
            MapSquads.SelectedSquad = mapSquad;
            // Print("Select squad", mapSquad);
            SquadsInfoPanel.SetInteractive(mapSquad.data, true);
        }

        /// <summary>
        /// MapのSquadの選択状況を解除する
        /// </summary>
        /// <param name="mapSquad"></param>
        internal void SetSquadAsDeselected(MapSquad mapSquad)
        {
            MapLocations.selectedLocation = null;
            MapSquads.SelectedSquad = null;
            // Print("Deselect squad", mapSquad);
            SquadsInfoPanel.SetInteractive(mapSquad.data, false);
            TableIconsPanel.ReturnBaseBalloon.Hide();
        }

        // (table上のraycastによる位置が必要だがshootrayは重い処理なため、thisの_GetCursorPositionに相乗りする)
        // 本来であればMapSquadsのみで完結したい処理だが
        /// <summary>
        /// Squadの出撃地点選択状態に入る
        /// </summary>
        /// <param name="squad"></param>
        internal void SquadReadyToGo(Squad squad)
        {
            MainUIController.Hide();
            if (squad.isOnMap)
            {
                // 既にSquadがMap上に存在するためこれを選択した状態にする
                SetSquadAsSelected(squad);
                CameraMode = MainMapCameraMode.Table;
            }
            else
            {
                if (MapSquads.SelectedSquad != null)
                    SetSquadAsDeselected(MapSquads.SelectedSquad);
                ShowWholeMap();
                MapSquads.SetSquadToReadyToGo(squad);
                CameraMode = MainMapCameraMode.Table;
            }
        }

        /// <summary>
        /// MapSquadがgoalまで行くためにかかる時間を計算する
        /// </summary>
        /// <param name="mapSquad"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        private float CalcTime(MapSquad mapSquad, MapLocation goal)
        {
            var points = MapLocations.mapRoads.GetBetterCheckPointsOfRoute(start: MapSquads.SelectedSquad.Location, goal);
            var realSecond = mapSquad.CalcTime(points.ConvertAll(p => p.position));
            var minute = gameManager.CalcDayTimeFromRealSeconds(realSecond, false);
            int hour = (int)(minute / 60);
            var days = Mathf.Ceil(hour / 24 * 10) / 10;
            return hour;
        }

        /// <summary>
        /// CursorがSquadの上に存在するとき
        /// </summary>
        private void CursorEnterOnSquad(SquadImage squadImage)
        {
            var selected = MapSquads.SelectedSquad;
            if (selected != null && !MapSquads.IsSquadMoving && !squadImage.MapSquad.Equals(selected))
            {
                TableIconsPanel.SupplyBalloon.Show(squadImage);
                if (selected.Location != squadImage.MapSquad.Location)
                {
                    TableIconsPanel.HourBalloon.Show(squadImage, CalcTime(selected, squadImage.MapSquad.Location));
                    TableIconsPanel.LocationBalloon.Show (squadImage);
                }
            }
            else if (squadImage.MapSquad.Equals(selected))
            {
                TableIconsPanel.ReturnBaseBalloon.Show(selected.SquadImage);
            }
        }

        /// <summary>
        /// CursorがSquadから出たとき
        /// </summary>
        private void CursorExitFromSquad(SquadImage squadImage)
        {
            var selected = MapSquads.SelectedSquad;
            if (selected != null && !MapSquads.IsSquadMoving && !squadImage.MapSquad.Equals(selected))
            {

                float delay = TableIconsPanel.HourBalloon.IsAnimating || TableIconsPanel.SupplyBalloon.IsAnimating ? 0.4f : 2f;
                TableIconsPanel.HourBalloon.Hide(delay);
                TableIconsPanel.SupplyBalloon.Hide(delay);
                TableIconsPanel.LocationBalloon.Hide(delay);
            }else if (squadImage.MapSquad.Equals(selected))
            {
                TableIconsPanel.ReturnBaseBalloon.Hide(1);
            }
        }

        /// <summary>
        /// 新たなSquadに対してクリックした
        /// </summary>
        /// <param name="squad"></param>
        private void CursorClickNewSquad(MapSquad squad)
        {
            if (squad.SquadState != MapSquad.State.Walking)
            {
                if (MapSquads.SelectedSquad != null && squad.Location.Equals(MapSquads.SelectedSquad.Location))
                {
                    MapSquads.ChangeSquads(MapSquads.SelectedSquad, squad);
                }

                SetSquadAsSelected(squad);
                TableIconsPanel.HourBalloon.Hide();
                TableIconsPanel.SupplyBalloon.Hide();
                TableIconsPanel.LocationBalloon.Hide ();
                TableIconsPanel.ReturnBaseBalloon.Show(squad.SquadImage);
            }
        }

        /// <summary>
        /// CursorがLocation上に入ったとき
        /// </summary>
        private void CursorEnterOnLocation(LocationImage locationImage)
        {
            var selected = MapSquads.SelectedSquad;
            // Squadが選択されている場合 MapLocationsに SquadからLocationへの線描写を投げる
            if (selected != null && !MapSquads.IsSquadMoving)
            {
                if (selected.Location != locationImage.MapLocation)
                {
                    StartCoroutine(MapLocations.DrawTrajectory(selected, locationImage.MapLocation));
                    TableIconsPanel.HourBalloon.Show(locationImage, CalcTime(selected, locationImage.MapLocation));
                }
            }
            if (MapSquads.IsSquadReadyToGo)
            {
                // MapのIconをXにする
                locationImage.MapLocation.StartFlashingTowerLight();
            }
        }

        /// <summary>
        /// CUrsorがlocation上から出たとき
        /// </summary>
        private void CursorExitFromLocation(LocationImage locationImage)
        {
            // 　Squadが移動中を除いてDrawされている軌跡を消す
            if (!MapSquads.IsSquadMoving)
            {
                StartCoroutine(MapLocations.HideTrajectory());
            }

            if (MapSquads.SelectedSquad != null)
            {
                TableIconsPanel.HourBalloon.Hide();
            }

            if (MapSquads.IsSquadReadyToGo)
            {
                locationImage.MapLocation.EndFlashingTowerLight();
            }
        }

        /// <summary>
        /// Squadが選択されている状態でLocationがクリックされた
        /// </summary>
        private void CursorClickLocationWithSquad(MapLocation location, MapSquad squad)
        {
            if (!MapSquads.IsSquadMoving)
            {
                // 移動前のデータを一時保存
                gameManager.DataSavingController.WriteAsTempData();

                MapSquads.MoveSquad(squad, location);
                TableIconsPanel.HourBalloon.Hide(0);
            }
        }

        /// <summary>
        /// 別SquadへのSupplyの補給ボタンにカーソルが入った
        /// </summary>
        /// <param name="squadImage"></param>
        private void CursorEnterOnSupplyOrLocation(SquadImage squadImage)
        {
            var selected = MapSquads.SelectedSquad;
            // Squadが選択されており、EnterSquadと異なる場合 SquadからEnterSquadへの線描写を投げる
            if (!MapSquads.IsSquadMoving && !squadImage.MapSquad.Equals(selected))
            {
                var location = squadImage.MapSquad.Location;
                if (squadImage.MapSquad.Location != selected.Location)
                    StartCoroutine(MapLocations.DrawTrajectory(selected, location));
                TableIconsPanel.HourBalloon.CancelAnimation();
                TableIconsPanel.SupplyBalloon.CancelAnimation();
                TableIconsPanel.LocationBalloon.CancelAnimation();
            }
        }

        /// <summary>
        /// 別SquadへのSupplyの補給ボタンにカーソルが出た
        /// </summary>
        /// <param name="squadImage"></param>
        private void CursorExitFromSupplyOrLocation(SquadImage squadImage)
        {
            var selected = MapSquads.SelectedSquad;
            // Squadが選択されており、EnterSquadと異なる場合 SquadからEnterSquadへの線描写を投げる
            if (!MapSquads.IsSquadMoving && !squadImage.MapSquad.Equals(selected))
            {
                TableIconsPanel.SupplyBalloon.Hide(1);
                TableIconsPanel.HourBalloon.Hide(1);
                TableIconsPanel.LocationBalloon.Hide(1);
                StartCoroutine(MapLocations.HideTrajectory(false));
            }
        }

        /// <summary>
        /// 別SquadへのSupplyの補給ボタンをクリックした
        /// </summary>
        /// <param name="squadImage"></param>
        private void CursorOnClickSupplyOrLocation(SquadImage squadImage, bool supplyAfterMove)
        {
            var selected = MapSquads.SelectedSquad;
            if (!MapSquads.IsSquadMoving && !squadImage.MapSquad.Equals(selected))
            {
                TableIconsPanel.SupplyBalloon.Hide();
                TableIconsPanel.HourBalloon.Hide(0);
                TableIconsPanel.LocationBalloon.Hide(0);
                if (squadImage.MapSquad.Location != selected.Location)
                {
                    MapSquads.MoveSquad(selected, squadImage.MapSquad.Location, () =>
                    {
                        if (supplyAfterMove)
                            MapSquads.StartMoveSupply(squadImage.MapSquad, selected);
                    });
                }
                else
                {
                    // その場で補給
                    if (supplyAfterMove)
                        MapSquads.StartMoveSupply(squadImage.MapSquad, selected);
                }
            }
        }

        /// <summary>
        /// SidePanelのInfoPanelがクリックされたときの呼び出し
        /// </summary>
        /// <param name="squad"></param>
        private void InfoPanelCardOnClick(MapSquad squad)
        {
            if (MapSquads.SelectedSquad != squad)
            {
                SetSquadAsSelected(squad);
            }
        }

        /// <summary>
        /// Squadをbaseに戻すのを開始する
        /// </summary>
        /// <param name="squad"></param>
        private void ReturnSquadAction(MapSquad squad)
        {
            if (!squad.IsOnMap) return;
            IEnumerator Return()
            {
                yield return StartCoroutine(MapSquads.SquadReturnToBase(squad, true));
                TableIconsPanel.UpdateSquadPosition(squad.SquadImage);
            }
            StartCoroutine(Return());
        }


        #endregion

        /// <summary>
        /// Tabを押してUIを表示
        /// </summary>
        private void KeyShowInfo()
        {

            if (!UserController.KeyShowInfo || gameManager.StartCanvasController.IsEnable) return;

            if (!MainUIController.AbleToShow)
            {
                if (MainUIController.Hide())
                {
                    CameraMode = MainMapCameraMode.Table;
                    // TODO ゲームバランスでUI表示中のタイマーストップやSquadの移動一時停止等を考慮する
                    // RestartTimer();
                    // mapSquads.StopAllSquads = false;
                }
                    
            }
            else
            {

                if (MapSquads.SelectedSquad != null ? MainUIController.Show(MapSquads.SelectedSquad.data) : MainUIController.Show())
                {
                    if (MapSquads.IsSquadReadyToGo)
                        MapSquads.DespawnSquad(MapSquads.readySquad);
                    CameraMode = MainMapCameraMode.UI;
                    // StopTimer();
                    // mapSquads.StopAllSquads = true;
                }
            }
        }

        /// <summary>
        /// Map上でのタイマーの進行を止める
        /// </summary>
        private void StopTimer()
        {
            if (!gameObject.activeSelf)
                return;

            AbleToControl = false;
            gameManager.IsTimerStopping = true;
            MapSpawns.StopAll = true;
            MapSquads.StopAllSquads = true;
            //timeController.PauseTime();
        }

        /// <summary>
        /// Map上でのタイマーの進行を再開する
        /// </summary>
        private void RestartTimer()
        {
            if (!gameObject.activeSelf)
                return;

            AbleToControl = true;
            MapSpawns.StopAll = false;
            MapSquads.StopAllSquads = false;
            gameManager.IsTimerStopping = false;
            //timeController.PlayTimeAgain();
        }

        /// <summary>
        /// Map上にいるSquadがエンカウント状態にある場合これを手動でTacitcs画面に遷移させるか判断する
        /// </summary>
        public void CheckEncountedSquad()
        {
            foreach (var squad in MapSquads.Squads)
            {
                foreach (var spawn in MapSpawns.squads)
                {
                    if (spawn.SpawnRequestData.LocationID == squad.Location.id)
                    {
                        // SpawnRequestData.specificTacticticsSceneIDが設定されている場合これを使い、ない場合はlocationのdefaultを使う
                        ReachedAtLocation(this, new ReachedEventArgs(squad.data, spawn.SpawnRequestData, squad.Location.id, gameManager.GameTime, squad.Location.DefaultTacticsSceneID));
                        return;
                    }
                }
            }
        }

        #region Called
        /// <summary>
        /// Squadがlocationに到着した際に呼び出し
        /// </summary>
        private void ReachedAtLocation(object o, ReachedEventArgs args)
        {
            if (args.ReachedAtLocation)
            {
                print($"{args.Player.commander.Data.ID} reached at {args.MapLocationID}, Encounted: {args.Encountered} ");
            }
            else
            {
                print($"{args.Player.commander.Data.ID} passed through {args.MapLocationID}, Encounted: {args.Encountered} ");
            }

            if (args.Encountered)
            {
                MapLocations.HideTrajectoryWithoutAnimation();
                StartCoroutine(ShowTacticsScene(args));
            }
            else if (args.ReachedAtLocation)
            {
                StartCoroutine( MapLocations.HideTrajectory(true));
            }
                
        }

        /// <summary>
        /// イベントシーンが開始されたときにGameManagerより呼び出し
        /// </summary>
        /// <param name="eventPack"></param>
        private void BeginEventAction(object o, EventArgs args)
        {
            StopTimer();
        }

        /// <summary>
        /// イベントシーンが終了した際にGameManagerより呼び出し
        /// </summary>
        private void EndEventAction(object o, EventArgs args)
        {
            // TODO: spawns.chatMessagesをSideMessageに送る
            // mapSpawns.SpawnFromEvent(asyncEvents.spawns);
            // TODO: SideMessageを廃止してミニフィグが喋っているように
            // sideMessages.SetMessages(asyncEvents.chatMessages);
            RestartTimer();
        }

        /// <summary>
        /// UIからSquadをLocationにFastTravelした際の呼び出し
        /// </summary>
        /// <param name="o"></param>
        /// <param name="squad"></param>
        /// <param name="moveTo"></param>
        //private void FastTravel(object o, Squad squad, LocationParamter moveTo)
        //{
        //    mapSquads.SquadMoveWithoutAnimation(squad, moveTo);
        //}

        /// <summary>
        ///　Tactics画面に遷移する エンカウント時にMapSpawnsからのイベントで呼び出される
        /// </summary>
        /// <param name="args"></param>
        private IEnumerator ShowTacticsScene(ReachedEventArgs args)
        {
            //Print("Encount", args.locationID, args.player.commander.name, args.commander.name);
            //sideMessages.pauseToPlayMessages.pause = true;
            //StopCoroutine(UpdateCursorPositionEnumerator);
            StopTimer();
            gameManager.Speed = 1;
            yield return StartCoroutine(gameManager.EventSceneController.PlayEventIfNeeded(EventGraph.InOut.TriggerTiming.BeforePrepare, 
                                                                                           args.Enemy.ID));
            while (gameManager.EventSceneController.IsEventWindowActive)
                yield return null;
            yield return new WaitForSeconds(0.5f);

            StartCoroutine(mainMapScene.TransitToTacticsScene(args));
        }

        /// <summary>
        /// TTactics画面から遷移してきたときの呼び出し MainMapScene空の呼び出し
        /// </summary>
        /// <param name="despawnEnemy">戦闘に勝利等で敵が排除された場合</param>
        /// <param name="reachedEventArgs">戦闘を行った相手の情報</param>
        /// <param name="returnPlayer">戦闘後Player部隊が帰還するか</param>
        internal void CalledWhenReturnFromTactics(BackToMainMapHandlerArgs arg)
        {
            var selectedSquad = MapSquads.GetMapSquadFromSquadData(arg.ReachedEventArgs.Player);
            MapSquads.SelectedSquad = selectedSquad;
            if (arg.DespawnEnemy)
            {
                var location = MapSpawns.DespawnEncountSquads(arg.ReachedEventArgs);
                MapSquads.PutSquadOnSpawnLocation(location, MapSquads.SelectedSquad);
            }
                
            if (arg.ReturnPlayer)
                StartCoroutine(MapSquads.SquadReturnToBase(MapSquads.SelectedSquad, true));
            //sideMessages.pauseToPlayMessages.pause = false;
            RestartTimer();
            //UpdateCursorPositionEnumerator = UpdateCursorPosition();
            //StartCoroutine(UpdateCursorPositionEnumerator);

            AbleToControl = true;
        }

        /// <summary>
        /// GameManagerから一定時間経過で呼び出される
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void PassTimeNortificationAsync(object o, EventArgs args)
        {
            //var gameTime = GameManager.SavedData.saveDataInfo.gameTime;
            //var currentEventPacks = GameManager.EventController.enabledEventPacks.FindAll(p =>
            //{
            //    return p.nextEvent.startTrigger.IsToggledOnTime(gameTime);
            //});
            //if (currentEventPacks.Count == 0) return;
            //var storyAndMessages = currentEventPacks.FindAll(p => p.nextEvent is Mission.Message || p.nextEvent is Mission.Story);
            //var spawns = currentEventPacks.FindAll(p => p.nextEvent is Mission.Spawn);
            //GameManager.eventSceneController.messageEvent.AddEvents(currentEventPacks);
        }
        
        /// <summary>
        /// EventSceneControllerからAddUnitRequestが呼び出された際に呼び出される
        /// </summary>
        private void AddUnitRequest(AddUnitEventOutput addUnitEventOutput)
        {
            if (gameManager.StaticData.AllUnitsData.GetUnitFromID(addUnitEventOutput.UnitID, out var baseData))
            {
                var data = new UnitData(baseData);
                data.MyItems = GameManager.Instance.SceneParameter.DefaultItemHolders.ConvertAll(i => new ItemHolder(i));
                gameManager.DataSavingController.MyArmyData.AddNewUnit(new UnitData(baseData));
            }
        }

        #endregion
    }

    /// <summary>
    /// MainMap画面でのCamera位置
    /// </summary>
    public enum MainMapCameraMode
    {
        UI,
        Table
    }
}
