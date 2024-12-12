using DG.Tweening;
using Parameters.SpawnSquad;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Utility;
using MainMap.UI.TableIcons;
using Unity.VisualScripting;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace MainMap
{
    /// <summary>
    /// 操作可能な自軍のController
    /// </summary>
    public class MapSquads : MonoBehaviour
    {
        [SerializeField] Roads.MapRoads mapRoads;
        
        [SerializeField] private GUIStyle passGuiStyle;

        internal TableIconsPanel tableOverlayPanel;

        /// <summary>
        /// Mapの各地点の情報  MainMapControllerより
        /// </summary>
        internal MapLocations MapLocations;
        /// <summary>
        /// スポーンしている敵部隊の情報
        /// </summary>
        internal Spawn.MapSpawns MapSpawns;
        /// <summary>
        /// 情報を表示するためのUIパネル MainMapControllerより
        /// </summary>
        internal MainMap.UI.InfoPanel.SquadsInfoPanel infoPanel;
        /// <summary>
        /// 現在選択中のSquad
        /// </summary>
        internal MapSquad SelectedSquad
        {
            set
            {
                if (selectedSquad != null)
                    selectedSquad.DeSelectSquad();

                if (value != null)
                    value.SelectSquad();

                selectedSquad = value;
            }

            get => selectedSquad;
        }
        private MapSquad selectedSquad;
        /// <summary>
        /// 現在Squadが移動中であるならばどこに向かっているか
        /// </summary>
        public MapLocation SquadMoveTo { private set; get; }
        /// <summary>
        /// 現在移動中のSquadがどこから出発したか
        /// </summary>
        public MapLocation SquadMoveFrom { private set; get; }
        /// <summary>
        /// Map上に存在する全ての部隊
        /// </summary>
        internal List<MapSquad> Squads = new List<MapSquad>();
        /// <summary>
        /// 一時停止中のSquad
        /// </summary>
        private List<MapSquad> pausedSquads = new List<MapSquad>();
        /// <summary>
        /// 出撃地点を決定するためにカーソルに追従するSquad
        /// </summary>
        internal MapSquad readySquad;
        /// <summary>
        /// Squadが基地に戻るアニメーションを再生中かどうか
        /// </summary>
        public bool ReturnAnimPlaying { private set; get; } = false;
        /// <summary>
        /// Squadが現在移動中かどうか
        /// </summary>
        public bool IsSquadMoving
        {
            get
            {
                return Squads.Find(s => s.SquadState == MapSquad.State.Walking) != null;
            }
        }
        /// <summary>
        /// 何らかのSquadが出撃準備でLocationの選択中になっているか
        /// </summary>
        public bool IsSquadReadyToGo
        {
            get
            {
                return readySquad != null;
            }
        }

        /// <summary>
        /// すべてのSquadの動作の再生が一時停止しているか
        /// </summary>
        public bool StopAllSquads
        {
            get => stopAllSquads;
            set
            {
                if (stopAllSquads == value)
                    return;
                stopAllSquads = value;
                if (stopAllSquads)
                {
                    pausedSquads = Squads.FindAll((s) =>
                    {
                        if (!s.moveSequence.IsActive())
                            return false;
                        return s.moveSequence != null && s.moveSequence.IsPlaying();
                    });
                    pausedSquads.ForEach(s => {
                        if (s.moveSequence != null)
                            s.moveSequence.Pause();
                    });
                }
                else
                {
                    pausedSquads.ForEach(s => s.moveSequence.Play());
                    pausedSquads.Clear();
                }
            }
        }
        bool stopAllSquads = false;

        /// <summary>
        /// Squadがlocationに到着したときの呼び出し エンカウントした場合の敵の情報も含む
        /// </summary>
        public EventHandler<ReachedEventArgs> ReachedEventHandler;

        /// <summary>
        /// Supplyを移動させる元のSquad
        /// </summary>
        private MapSquad squadOfMoveSupplyFrom;
        /// <summary>
        /// Supplyを移動させる先のSquad
        /// </summary>
        private MapSquad squadOfMoveSupplyTo;
        /// <summary>
        /// 現在SupplyをMap上で移動させている状態であるか
        /// </summary>
        public bool IsMovingSupply { private set; get; }
        

        GeneralParameter parameter;
        GameManager gameManager;

        protected private void Awake()
        {
            gameManager = GameManager.Instance;
            parameter = gameManager.GeneralParameter;
            gameManager.PassTimeEventHandlerAsync += TimerNortification;
            gameManager.AddTimeEventHandlerAsync += AddTimeNortificationAsync;
            gameManager.AddTimeEventHandlerSync += AddTimeNortificationSync;

            Initialize();
        }

        protected void Update()
        {
            if (readySquad != null)
                tableOverlayPanel.UpdateSquadPosition(readySquad.SquadImage);
        }

        /// <summary>
        /// DataControllerの中身をロードしてMapSquadsに反映させる
        /// </summary>
        internal IEnumerator LoadData()
        {
            tableOverlayPanel.MapSquads = this;

            // 一旦全てのSquadとObjectを消す
            Squads.ForEach((s) => Destroy(s.gameObject));
            Squads.Clear();
            pausedSquads.Clear();
            infoPanel.Clear();

            var coroutines = new List<Coroutine>();
            foreach (var squad in gameManager.DataSavingController.MyArmyData.Squads)
            {
                if (!squad.isOnMap) continue;
                // 現在出撃中のSquad
                var coroutine = StartCoroutine(PutSquadOnMap(squad));
                coroutines.Add(coroutine);
            }

            // 全てのcoroutineが終了するまで待つ
            foreach (var coroutine in coroutines)
                yield return coroutine;

            Squads.ForEach(s => infoPanel.Add(s));
        }

        /// <summary>
        /// MapSquadsの初期化
        /// </summary>
        internal void Initialize()
        {
            IsMovingSupply = false;
            squadOfMoveSupplyFrom = squadOfMoveSupplyTo = null;
            ReturnAnimPlaying   = false;
            readySquad = null;
            pausedSquads = new List<MapSquad>();
            Squads = new List<MapSquad>();
            SquadMoveFrom = SquadMoveTo = null;
            selectedSquad = null;
        }

        /// <summary>
        /// <c>timerCallbackSeconds</c>ごとに呼び出されるゲーム内時間のタイマー
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void TimerNortification(object o, EventArgs args)
        {
        }

        /// <summary>
        /// <c>ゲーム内時間で1分経過したときに呼び出し</c>
        /// </summary>
        /// <param name="o"></param>
        /// <param name="minute"></param>
        private void AddTimeNortificationSync(object o,int deltaMinute)
        {
            UpdateSupplyLevel();
        }

        /// <summary>
        /// ゲーム内で1分経過したときに呼び出し 非同期
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void AddTimeNortificationAsync(object o, EventArgs args)
        {
            infoPanel.UpdateCards();
        }

        /// <summary>
        /// SupplyLevelの行動や時間経過に対する増減
        /// </summary>
        private void UpdateSupplyLevel()
        {
            // Supplyの補給
            if (squadOfMoveSupplyFrom != null && squadOfMoveSupplyTo != null)
            {
                if (!IsMovingSupply)
                {
                    // Supplyの移動を開始する
                    IsMovingSupply = true;
                    squadOfMoveSupplyTo.SupplyLevel += parameter.MoveSupplyingAmountPerHour / 60;
                    squadOfMoveSupplyFrom.SupplyLevel -= parameter.MoveSupplyingAmountPerHour / 60;
                    GameManager.Instance.Speed = 4;
                    tableOverlayPanel.FastForwardIcon.Show();
                }
                else
                {

                    if (squadOfMoveSupplyFrom.SupplyLevel == 0)
                    {
                        // Supplyの元が0になったためSupplyの移動を中止させる
                        GameManager.Instance.Speed = 1;
                        squadOfMoveSupplyFrom = squadOfMoveSupplyTo = null;
                        IsMovingSupply = false;
                        squadOfMoveSupplyFrom.CancelMoveAnimation();
                        StartCoroutine(squadOfMoveSupplyFrom.AnimationReturnToBaseForced());
                        StartCoroutine(SquadReturnToBase(squadOfMoveSupplyFrom, true));
                        tableOverlayPanel.FastForwardIcon.Hide();

                    }
                    else if (squadOfMoveSupplyTo.IsSupplyFull)
                    {
                        GameManager.Instance.Speed = 1;
                        // Supplyの補充が完了した
                        squadOfMoveSupplyFrom = squadOfMoveSupplyTo = null;
                        IsMovingSupply = false;
                        tableOverlayPanel.FastForwardIcon.Hide();
                    }
                    else
                    {
                        squadOfMoveSupplyTo.SupplyLevel += parameter.MoveSupplyingAmountPerHour / 60;
                        squadOfMoveSupplyFrom.SupplyLevel -= parameter.MoveSupplyingAmountPerHour / 60;
                    }
                }
            }

            // Supplyの増減をコントロール
            if (Squads.Count != 0)
            {
                // SquadのSupplyタイマー
                var _squads = new List<MapSquad>(Squads);
                _squads.ForEach(s =>
                {
                    if (!s.IsOnLocation || !s.Location.IsFriend)
                    {
                        //if (lockReducingSupply) return;
                        var cost = parameter.SupplyCostOnMap / gameManager.CallAddTimeEventHandlerCountInDay;
                        s.SupplyLevel -= cost;
                        if (s.SupplyLevel == 0 && !s.Location.IsFriend)
                        {
                            // TODO: SupplyLevelが0以下になったため帰還させる
                            if (GameManager.Instance.Speed != 1)
                            {
                                GameManager.Instance.Speed = 1;
                                tableOverlayPanel.FastForwardIcon.Hide();
                            }
                            s.CancelMoveAnimation();
                            StartCoroutine(s.AnimationReturnToBaseForced());
                            StartCoroutine(SquadReturnToBase(s, true));
                        }
                        else
                        {
                            // TODO: SupplyLevelが3割を切ったため腹ペコモードにアニメーションを切り替える
                        }
                    }
                });

            }

            // SquadがBase内にいる場合1日でSupplyがすべて回復する
            var tick = parameter.DayLengthSeconds / gameManager.AddTimeSeconds;
            gameManager.DataSavingController.MyArmyData.Squads.ForEach(s =>
            {
                if (!s.isOnMap)
                {
                    // Squadが基地内にいる場合
                    s.supplyLevel += s.MaxSupply / tick;
                }
                else if (s.isOnMap && s.MapLocation != null && s.MapLocation.Data.type == LocationParamter.Type.friend)
                {
                    // SquadはMap上だが友好的拠点の上にいる場合 基地内にいる場合より通常ゆっくり回復
                    s.supplyLevel += (s.MaxSupply / tick) * parameter.RecoveringSupplyRateWhenStayOnBase;
                }
                if (s.supplyLevel > s.MaxSupply)
                    s.supplyLevel = s.MaxSupply;
                //-45.583
            });
        }

        /// <summary>
        /// SquadからMapSquadを取得する
        /// </summary>
        /// <param name="squad"></param>
        /// <returns></returns>
        internal MapSquad GetMapSquadFromSquadData(Squad squad)
        {
            return Squads.Find(s => s.data == squad);
        }

        #region Squadの行動
        /// <summary>
        /// fromからtoへと補給を行う
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void StartMoveSupply(MapSquad from, MapSquad to)
        {
            if (from.Location != to.Location)
                return;
            squadOfMoveSupplyTo = to;
            squadOfMoveSupplyFrom = from;
        }

        /// <summary>
        /// Turnoutとlocationにいるsquadの位置を入れ替える
        /// </summary>
        /// <param name="squad"></param>
        /// <param name="onLocation"></param>
        internal void ChangeSquads(MapSquad toTurnout, MapSquad toLocation)
        {
            if (!toTurnout.Location.Equals(toLocation.Location))
                return;
            if (toTurnout.IsOnTurnout)
                return;
            
            toLocation.ReturnFromTurnout(true);
            toTurnout.MoveToTurnout(this);
        }
        #endregion

        #region Squad and location
        /// <summary>
        /// Squadの内容に準じて出撃準備状態のSquadをmap上に配置する
        /// </summary>
        /// <param name="squad"></param>
        internal MapSquad SetSquadToReadyToGo(Squad squad)
        {
            var obj = Instantiate(squad.commander.MiniPrefab, Vector3.zero, transform.rotation, transform);
            readySquad = obj.AddComponent<MapSquad>();
            readySquad.data = squad;
            readySquad.IsSpawnMode = true;
            readySquad.tableOverlayPanel = tableOverlayPanel;
            readySquad.SquadImage = tableOverlayPanel.AddSquadImage(readySquad, true);
            infoPanel.AddWithAnimation(readySquad);
            return readySquad;
        }

        /// <summary>
        /// Ready状態になっているSquadをlocationにスポーンさせる
        /// </summary>
        internal MapSquad PutReadySquadOnLocation(MapLocation location)
        {
            if (!IsSquadReadyToGo) return null;

            var newSquad = readySquad;

            newSquad.transform.position = location.transform.position;
            newSquad.squadReachedOnLocation = SquadReachedOnSpawnPoint;
            newSquad.squadGetsNearToSpawnPoint = SquadGetsNearToSpawnPoint;
            newSquad.mapLocations = MapLocations;
            newSquad.Location = location;
            Squads.Add(readySquad);
            newSquad.IsSpawnMode = false;
            newSquad.IsOnMap = true;
            readySquad.tableOverlayPanel = tableOverlayPanel;
            readySquad.SquadImage.PutSquadOnMap();
            readySquad = null;

            infoPanel.SetInteractive(newSquad.data, true);

            SelectedSquad = newSquad;

            return newSquad;
        }

        /// <summary>
        /// Hierarchy上にSquadのObjectを作成し、その部隊のParameterをsetする
        /// </summary>
        /// <param name="squad">Squadの情報 nullの場合テスト用の部隊が作成される</param>
        /// <param name="isControllable">Squadが操作可能なSquadかどうか</param>
        /// <returns></returns>
        internal IEnumerator PutSquadOnMap(Squad squad, MapLocation location)
        {
            yield return StartCoroutine(_PutSquadOnMap(squad, location.transform.position));
        }

        /// <summary>
        /// Hierarchy上にSquadのObjectを作成し、その部隊のParameterをsetする
        /// </summary>
        /// <param name="squad">Squadの情報 nullの場合テスト用の部隊が作成される</param>
        /// <param name="isControllable">Squadが操作可能なSquadかどうか</param>
        /// <returns></returns>
        internal IEnumerator PutSquadOnMap(Squad squad, string locationId = null)
        {
            if (locationId != null)
            {
                var location = MapLocations.GetLocationFromID(locationId);
                if (location == null)
                    location = MapLocations.locations.First();
                yield return StartCoroutine(_PutSquadOnMap(squad, location.transform.position));
                
            }
            else
            {
                yield return StartCoroutine(_PutSquadOnMap(squad));
            }
        }

        /// <summary>
        /// Hierarchy上にSquadのObjectを作成し、その部隊のParameterをsetする
        /// </summary>
        /// <param name="squad">Squadの情報 nullの場合テスト用の部隊が作成される</param>
        /// <param name="isControllable">Squadが操作可能なSquadかどうか</param>
        /// <returns></returns>
        private IEnumerator _PutSquadOnMap(Squad squad, Vector3? location = null)
        {
            squad.isOnMap = true;

            var turnoutPosition = MapLocations.GetLocationFromID(this, squad.LocationID, squad.IsOnTurnout);
            var squadLocation = location.GetValueOrDefault(turnoutPosition.position);
            var obj = Instantiate(squad.commander.MiniPrefab, squadLocation, transform.rotation, transform);

            obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, 0, obj.transform.localPosition.z);
            
            var mapSquad = obj.AddComponent<MapSquad>();
            mapSquad.tableOverlayPanel = tableOverlayPanel;
            mapSquad.SquadImage = tableOverlayPanel.AddSquadImage(mapSquad);
            mapSquad.squadReachedOnLocation = SquadReachedOnSpawnPoint;
            mapSquad.mapLocations = MapLocations;
            mapSquad.data = squad;
            obj.name = squad.name;
            mapSquad.Location = MapLocations.GetLocationFromID(mapSquad.data.LocationID);
            if (mapSquad.IsOnTurnout)
                mapSquad.turnoutPosition = turnoutPosition;

            Squads.Add(mapSquad);
            yield return null;
        }

        /// <summary>
        /// SquadがSpawnLocationに到達したときの呼び出し
        /// </summary>
        /// <param name="spawnLocationID"></param>
        /// <param name="squad"></param>
        private void SquadReachedOnSpawnPoint(MapLocation location, MapSquad squad)
        {

            var args = new ReachedEventArgs
            {
                Player = squad.data,
                MapLocationID = location.id,
                ReachedAtLocation = location == SquadMoveTo,
                ReachedDateTime = gameManager.GameTime
            };

            if (location == SquadMoveTo)
            {
                // 目的地に到着
                SquadMoveFrom = SquadMoveTo = null;
            }
            else
            {
                // 経由地のMapLocationに到着
            }

            // 的にエンカウントした場合の処理
            if (location.SpawnSquadOnLocation != null)
            {
                // Squadの移動を停止する
                squad.CancelMoveAnimation();

                args.SpawnRequestData = location.SpawnSquadOnLocation.SpawnRequestData;
                MapSpawns.Encount(location.SpawnSquadOnLocation, squad);
                SquadMoveFrom = SquadMoveTo = null;
            }

            ReachedEventHandler?.Invoke(this, args);
        }

        /// <summary>
        /// SquadがSpawnPointに近づくまたは遠ざかる際に呼び出し
        /// </summary>
        /// <param name="location"></param>
        /// <param name="squad"></param>
        /// <param name="getNear">近づいているか</param>
        private void SquadGetsNearToSpawnPoint(MapLocation location, MapSquad squad, bool getNear)
        {
            var squadsOnLocation = Squads.FindAll(s => s.Location == location);
            squadsOnLocation.ForEach(l =>
            {
                if (getNear)
                {
                    l.MoveToTurnout(this);
                }
                else
                {
                    l.ReturnFromTurnout();
                }
            });
        }

        #endregion

        #region Squad moving

        /// <summary>
        /// SquadをアニメーションなしにLocationに移動させる
        /// </summary>
        internal void SquadMoveWithoutAnimation(Squad squad, LocationParamter location)
        {
            var mapSquad = Squads.Find((s) => s.data == squad);
            if (mapSquad == null)
                PrintError($"Request {squad.name} but it doesn't exist on MapSquads");
            var mapLocation = MapLocations.locations.Find(l => l.Data.Equals(location));
            if (mapLocation == null)
                PrintError($"Request {location.Name} but it doesn't exits on MapLocations");

            mapSquad.MoveToWithoutAnimation(mapLocation.gameObject.transform.position);
        }

        /// <summary>
        /// Squadを位置しているBaseに帰還させる
        /// </summary>
        /// <param name="forced">Supply切れや敗北などで強制的に戻すアニメーションの場合</param>
        /// <param name="squad">帰還させる対象のSquad</param>
        public IEnumerator SquadReturnToBase(MapSquad squad, bool forced)
        {
            // TODO SquadをBaseに帰還させる

            if (selectedSquad == squad)
            {
                selectedSquad = null;
                SquadMoveFrom = SquadMoveTo = null;
            }
                

            ReturnAnimPlaying = true;

            infoPanel.Remove(squad.data);

            squad.IsOnMap = false;
            squad.Location = null;
            Squads.Remove(squad);
            pausedSquads.Remove(squad);
            // 帰還時アニメーションとか
            if (forced)
                yield return StartCoroutine(squad.AnimationReturnToBaseForced());
            else
                yield return StartCoroutine( squad.AnimationReturnToBase() );

            Destroy(squad.gameObject);

            ReturnAnimPlaying = false;
        }

        /// <summary>
        /// 指定したmapSquadをMap上から消す
        /// </summary>
        /// <param name="mapSquad"></param>
        internal void DespawnSquad(MapSquad mapSquad)
        {
            Squads.Remove(mapSquad);
            infoPanel.Remove(mapSquad.data);
            if (readySquad == mapSquad)
                readySquad = null;
            if (selectedSquad == mapSquad)
                selectedSquad = null;
            mapSquad.IsOnMap = false;
            Destroy(mapSquad.gameObject);
        }

        /// <summary>
        /// 全てのSquadをMap上から消す
        /// </summary>
        internal void DespawnAllSquads()
        {
            foreach (var squad in Squads)
            {
                DespawnSquad(squad);
            }
        }

        /// <summary>
        /// SquadをmapSpawnLocation上に設置する
        /// </summary>
        /// <param name="location"></param>
        /// <param name="squad"></param>
        public void PutSquadOnSpawnLocation(MapLocation location, MapSquad squad)
        {
            squad.transform.position = new Vector3(location.transform.position.x,
                                                   squad.transform.position.y,
                                                   location.transform.position.z);
            
        }

        private void OnDrawGizmos()
        {
            //Gizmos.color = Color.red;
            //if (testRouteCheckPoints != null)
            //{
            //    for (var i = 0; i < testRouteCheckPoints.Count; i++)
            //    {
            //        //Handles.Label(testRouteCheckPoints[i], $"{i}", passGuiStyle);
            //        Debug.DrawLine(testRouteCheckPoints[i], testRouteCheckPoints[(i + 1) % testRouteCheckPoints.Count], Color.red, 1f);
            //    }
            //}
        }

        //List<Vector3> testRouteCheckPoints;

        /// <summary>
        /// <c>squad</c>をMapLocationに移動させる
        /// </summary>
        /// <param name="squad"></param>
        /// <param name="to"></param>
        public void MoveSquad(MapSquad squad, MapLocation to, Action onComplete=null)
        {
            squad.CancelMoveAnimation();

            // SquadはLocationの上に位置している
            var routeCheckPoints = mapRoads.GetBetterCheckPointsOfRoute(squad.Location, to).ConvertAll(t => t.position);
            //print(string.Join(",", routeCheckPoints));
            //testRouteCheckPoints = routeCheckPoints;
            SquadMoveFrom = squad.Location;
            SquadMoveTo = to;
            gameManager.Speed = 30;
            var startTime = GameManager.Instance.GameTime;
            var startRealTime = DateTime.Now;
            StartCoroutine(SelectedSquad.MoveAlong(routeCheckPoints, SquadMoveTo, (isCompleteToMove) =>
            {
                if (isCompleteToMove)
                {
                    var endTimeText = GameManager.Instance.GameTime.ToString("yyyy/MM/dd HH:mm:ss");
                    var timeDurationText = (GameManager.Instance.GameTime - startTime).TotalMinutes;
                    print($"Squad {squad.data.name} moved from {squad.Location.Data} to {to.Data} at {startTime.ToString("yyyy/MM/dd HH:mm:ss")} to {endTimeText}, duration {timeDurationText} min\n" +
                        $"RealTime: {(DateTime.Now - startRealTime).TotalSeconds}");
                    squad.Location = to;
                }
                else
                {
                    print(squad.data.name + " is not complete to move");
                }
                gameManager.Speed = 1;
                
                onComplete?.Invoke();
            }));
            
        }

        #endregion
    }

    /// <summary>
    /// エンカウント内容をEventArgsとして送るための
    /// </summary>
    [Serializable]
    public class ReachedEventArgs : EventArgs
    {
        /// <summary>
        /// エンカウントした場所のID ここからTactics画面のIDへと行く
        /// </summary>
        public string TacticsSceneID;
        /// <summary>
        /// エンカウントしたときのPlayerのSquadID
        /// </summary>
        public string PlayerSquadID;
        /// <summary>
        /// エンカウント下のMapLocationのID
        /// </summary>
        public string MapLocationID;
        /// <summary>
        /// エンカウントした日時
        /// </summary>
        public SerializableDateTime ReachedDateTime;
        /// <summary>
        /// エンカウントした場所のSpawnRequestData、エンカウントしなかった場合null
        /// </summary>
        public SpawnRequestArgs SpawnRequestData;
        /// <summary>
        /// 指定した場所に到着した場合 true 経由地ならfalse
        /// </summary>
        public bool ReachedAtLocation = false;

        /// <summary>
        /// エンカウントが発生した場合true
        /// </summary>
        public bool Encountered { get => SpawnRequestData != null; }
        /// <summary>
        /// エンカウントした敵軍のID
        /// </summary>
        public string SpawnSquadID { get => Enemy.ID; }


        /// <summary>
        /// エンカウントした自軍
        /// </summary>
        public Squad Player
        {
            get
            {
                if (player == null)
                    if (!GameManager.Instance.DataSavingController.MyArmyData.GetSquadFromID(PlayerSquadID, out player))
                        PrintError($"Can't find PlayerSquad from ID {PlayerSquadID}");
                return player;
            }
            set
            {
                player = value;
                PlayerSquadID = value.ID;
            }
        }
        [NonSerialized] private Squad player;

        /// <summary>
        /// エンカウントした敵軍
        /// </summary>
        public SpawnSquadData Enemy
        {
            get
            {
                // ゲームの難易度設定によって出る敵のレベルが変わる
                var enemyLevel = SpawnRequestData.Level + GameManager.Instance.GameDifficulty.GetHashCode();
                if (enemyLevel < 0)
                    enemyLevel = 0;

                if (enemy == null)
                     GameManager.Instance.StaticData.AllSpawnSquads.GetSpawnSquadFromID(SpawnSquadID, enemyLevel, out enemy);
                if (enemy == null)
                    PrintError($"Can't find EnemySquad from ID {SpawnSquadID}");
                return enemy;

            }
            set { Enemy = value; }
        }
        [NonSerialized] private SpawnSquadData enemy;

        public ReachedEventArgs()
        {
        }

        public ReachedEventArgs(Squad player, SpawnRequestArgs spawnRequestData, string locationID, DateTime reachedTime, string defaultTacticsSceneID)
        {
            Player = player;
            SpawnRequestData = spawnRequestData;
            ReachedDateTime = reachedTime;
            MapLocationID = locationID;
            if (spawnRequestData != null)
                TacticsSceneID = spawnRequestData.SpecificTacticsSceneID.Length != 0 ? spawnRequestData.SpecificTacticsSceneID : defaultTacticsSceneID;
        }

        public override string ToString()
        {
            return Enemy == null ? $"ReachedEventArgs: ({Player}) reaches at ({MapLocationID})" : 
                                   $"ReachedEventArgs: ({Player}) reaches at ({MapLocationID}) \n,and encounts ({Enemy})";
        }
    }
}