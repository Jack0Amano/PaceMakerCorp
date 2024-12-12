using EventGraph.InOut;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tactics.Character;
using Tactics.Object;
using Tactics.UI;
using TMPro;
using Unity.Logging;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using static Utility;

namespace Tactics.Map
{
    /// <summary>
    /// Tactics画面のTileの動作を総合的に管理する
    /// </summary>
    public class TilesController : MonoBehaviour
    {
        [SerializeField] Character.UnitsController unitsController;

        [Tooltip("ActiveなUnitに追従するTileCellのTriggerとの当たり判定用のCursorCollider")]
        [SerializeField] internal CapsuleCollider UnitCursor;

        [Tooltip("すべてのTileCellの接続を行うObjectの親Object")]
        [SerializeField] internal GameObject connectionsParent;

        [Tooltip("すべてのTileCellを含む親オブジェクト")]
        [SerializeField] internal GameObject tilesParent;

        [Tooltip("すべてのGimmickを内包する親オブジェクト")]
        [SerializeField] internal GameObject gimmickParent;

        [Tooltip("すべての破壊可能なObjectを内包する親オブジェクト")]
        [SerializeField] internal GameObject DestructibleObjectsParent;

        [Tooltip("TacticsTablet")]
        [SerializeField] TacticsTablet.TacticsTablet tacticsTablet;

        [Tooltip("グリットポイントを生成しないAreaの親Object")]
        [SerializeField] private Transform removeLocationArea;

        [Tooltip("生成された格子状のグリッドポイントからどれだけの割合で有効化するかのレート")]
        [SerializeField] float chooseRate = 0.7f;

        [Tooltip("GridPointsの点の間隔")]
        [SerializeField] private float distanceBetweenGridPoints = 0.5f;

        [Tooltip("NPCの巡回を結んだobjectsの親")]
        [SerializeField] GameObject waysParent;
        /// <summary>
        /// Map全体のSquaresのList
        /// </summary>
        /// <value></value>
        public List<TileCell> Tiles { private set; get; }
        /// <summary>
        /// Unitがターン開始時に位置していたCell
        /// </summary>
        private TileCell startTile;
        /// <summary>
        /// <c>locationsAndScores</c>の地点が削除されるエリアの<c>MeshFilter</c>
        /// </summary>
        private List<MeshFilter> removeLocationMeshes;
        /// <summary>
        /// カバーなどのUnitがギミックを起こすObject
        /// </summary>
        private List<Object.GimmickObject> gimmickObjects;

        private List<GameObject> DestructibleObjects;

        internal List<(GameObject parent, List<(Transform pos, TileCell tile)> values)>  waysPassPoints;

        // StartPositionから対応したCellを取得するためのDictionary
        public Dictionary<StartPosition, List<TileCell>> StartTileDict = new Dictionary<StartPosition, List<TileCell>>();

        protected private void Awake()
        {

            Tiles = tilesParent.GetComponentsInChildren<TileCell>().ToList();

            foreach (var cell in Tiles)
            {
                cell.unitsController = unitsController;
                cell.EnterEventHander += EnterEvent;
                cell.distanceBetweenGridPoints = distanceBetweenGridPoints;
            }

            removeLocationMeshes = new List<MeshFilter>();
            removeLocationArea.GetChildren().ForEach(c =>
            {
                var mesh = c.GetComponent<MeshFilter>();
                if (mesh != null)
                    removeLocationMeshes.Add(mesh);
            });

            // AIの経路ルーチンを指定
            waysPassPoints = new List<(GameObject parent, List<(Transform pos, TileCell tile)> values)>();
            foreach (Transform ways in waysParent.transform)
            {
                var pt = new List<(Transform, TileCell)>();
                foreach (Transform passPoint in ways)
                    pt.Add((passPoint, null));

                waysPassPoints.Add((ways.gameObject, pt));
            }

            StartTileDict = Enum.GetValues(typeof(StartPosition)).Cast<StartPosition>().ToDictionary(s => s, s => {
                var startTiles = Tiles.FindAll(t => t.StartPosition == s);
                // Priorityの高い順にソート
                startTiles.Sort((a, b) => a.spawnPriority.CompareTo(b.spawnPriority));
                return startTiles;
            });
        }

        // Start is called before the first frame update
        protected void Start()
        {
            // Cellの接続を行う
            foreach (Transform connection in connectionsParent.transform)
            {
                var connectorObject = connection.gameObject;
                var collider = connectorObject.GetComponent<BoxCollider>();
                Collider[] hitColliders = Physics.OverlapBox(connection.position, collider.bounds.extents);
                var walls = new List<Wall>();
                foreach (var hit in hitColliders)
                {
                    var hitObject = hit.gameObject;
                    if (hitObject.CompareTag("CellWall"))
                    {
                        var wall = hitObject.GetComponent<Wall>();
                        walls.Add(wall);
                    }
                }
                if (walls.Count == 2)
                {
                    var tile = walls[0].wallObject.transform.parent.gameObject;
                    var tileCon1 = tile.GetComponent<TileCell>();
                    walls[1].contactCells.Add(tileCon1);

                    tile = walls[1].wallObject.transform.parent.gameObject;
                    var tileCon2 = tile.GetComponent<TileCell>();
                    walls[0].contactCells.Add(tileCon2);

                    tileCon1.borderOnTiles.Add(tileCon2);
                    tileCon2.borderOnTiles.Add(tileCon1);

                    connectorObject.SetActive(false);
                }
                else
                {
                    PrintError("Connector", connection.gameObject, "is connected", walls.Count, "objects");
                }
            }

            UnitCursor.gameObject.SetActive(false);
        }

        /// <summary>
        /// TIle系の非同期処理
        /// </summary>
        /// <param name="pathGridPoint"></param>
        /// <returns></returns>
        public IEnumerator AsyncLoad()
        {
            gimmickObjects = gimmickParent.GetComponentsInChildren<Object.GimmickObject>().ToList();

            //DestructibleObjects = DestructibleObjectsParent.GetChildren().ToList();

            // Tile内のObjectの配置を行う
            var coroutines = new List<IEnumerator>();
            foreach (var tile in Tiles)
            {
                tile.LoadGridPoints();

                var objectsInTile = tile.GetAllColliderInTile();
                var gimmicksInTile = gimmickObjects.FindAll(s => objectsInTile.Contains(s.gameObject));


                var coroutine = tile.InactivePointInRemoveArea(removeLocationMeshes);
                StartCoroutine(coroutine);
                tile.SetGimmickObjects(gimmicksInTile);

                // wayPassPointsを登録
                waysPassPoints.ForEach(w =>
                {
                    for(var i=0; i<w.values.Count; i++)
                    {
                        if (objectsInTile.Exists (o => o.transform == w.values[i].pos))
                        {
                            w.values[i] = (w.values[i].pos, tile);
                            break;
                        }
                    }
                });

                coroutines.Add(coroutine);
            }

            // tileの検索が終わったwayspassPointsをUnitsControllerに送る
            waysPassPoints.ForEach(w =>
            {
                w.values.RemoveAll(p => p.tile == null);
            });
            unitsController.waysPassPoints = waysPassPoints.ConvertAll(w => w.values);

            // MeshによるLocationsAndScoresのinactive化が終了するまで待つ
            while (true)
            {
                if (coroutines.Find(c => (bool)c.Current == false)  == null)
                    break;
                yield return null;
            }

            

            Tiles.ForEach(t =>
            {
                t.SetRandomPosition(chooseRate);
            });
        }

        /// <summary>
        /// Unitのロードが終了した際に呼び出す
        /// </summary>
        public IEnumerator UnitLoaded()
        {
            foreach(var c in Tiles)
            {
                yield return StartCoroutine(c.UnitLoaded());
            }
            UnitCursor.gameObject.SetActive(true);
        }

        /// <summary>
        /// TilesControllerを初期化する
        /// </summary>
        public void Clear()
        {
            UnitCursor.gameObject.SetActive(false);
            startTile = null;
        }

        // Update is called once per frame
        void Update()
        {
            if (UnitCursor.gameObject.activeSelf && unitsController.activeUnit != null)
                UnitCursor.transform.position = unitsController.activeUnit.gameObject.transform.position;
        }


        /// <summary>
        /// Unitの開始位置をStartPositionから取得する
        /// </summary>
        public List<TileCell> GetStartTiles(StartPosition startPosition, bool isEnemy)
        {
            var startTiles = StartTileDict.GetValueOrDefault(startPosition);
            if (startTiles == null || startTiles.Count == 0)
            {
                startTiles = StartTileDict[StartPosition.North];
            }
            if (startTiles.Count == 0)
            {
                PrintWarning($"TilesControllerに{startPosition}のStartTabletPositionが設定されていません。\n" +
              $"Tactics開始位置のTileにStartTabletPositionを設定してください。");
            }
            return startTiles.FindAll(t => t.IsEnemyStartPosition == isEnemy);
        }

        /// <summary>
        /// 指定したUnitのターンを開始する
        /// </summary>
        /// <param name="unit">ターン開始するunit</param>
        /// <param name="noWallMode">wallのcolliderを使用しない (AIの移動を確実に行うためのモード)</param>
        /// <param name="updateUnitsInTile">TileのUnitの所属をアップデートする</param>
        public TileCell StartTurn(UnitController unit, bool updateUnitsInTile = false, bool noWallMode = false)
        {
            if (updateUnitsInTile)
            {
                Tiles.ForEach(t => 
                {
                    var isActive = t.gameObject.activeSelf;
                    t.gameObject.SetActive(true);
                    t.gameObject.SetActive(isActive);
                });
            }

            var currentCell = unit.tileCell;
            if (currentCell == null)
            {
                Log.Error($"Current tile of {unit} is missing.");
                return null;
            }
            startTile = currentCell;

            // 現在のcellでも境界でも無いCellを非表示
            var otherCells = Tiles.Removed(currentCell);
            otherCells.Except(currentCell.borderOnTiles);
            otherCells.ForEach(c => c.DisappearAsNotConnection());

            if (unit.NavMeshObstacle.shape == NavMeshObstacleShape.Capsule)
            {
                UnitCursor.height = unit.NavMeshObstacle.height;
                UnitCursor.transform.localScale = Vector3.one * unit.UnitCursorSize;
            }

            // CurrentCellを表示
            currentCell.AppearAsCurrentCell();

            // 境界のCellをborderCellとして表示
            currentCell.borderOnTiles.ForEach(c =>
            {
                c.AppearAsBorderCell(currentCell, UnitCursor);
            });

            return currentCell;
        }


        /// <summary>
        /// Unitの位置するSquareをDisableする; 現在のマスから隣のマスへ移動できるようになる
        /// </summary>
        /// <param name="point"></param>
        internal void DisableSquareCurrentLocation(GameObject unit)
        {
            var targetSquare = GetTileInUnit(unit);
            if (targetSquare != null)
                targetSquare.gameObject.SetActive(false);
        }

        internal void DisableAllTiles()
        {
            foreach(var square in Tiles)
            {
                square.gameObject.SetActive(false);
            }
        }


        /// <summary>
        /// すべてのSquaresをActiveにする; すべてのユニットが現在のマスから出れなくなる
        /// </summary>
        private void EnableAllTiles()
        {
            foreach (var square in Tiles)
            {
                square.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Unitの居る現在のSquareを取得する
        /// </summary>
        /// <param name="unit"></param>
        /// <returns>Unitの居る現在のSquare or Unitが枠外の場合はnull</returns>
        public TileCell GetTileInUnit(GameObject unit)
        {

            foreach (var square in Tiles)
            {
                foreach (var u in square.UnitsInCell)
                {
                    if (u == null)
                        continue;
                    if (u.gameObject == unit)
                        return square;
                }
            }

            var name = "None";
            var unitCon = unit.GetComponent<Character.UnitController>();
            if (unitCon != null)
                name = unitCon.CurrentParameter.Data.Name;
            Print($"Unit {name}: {unit} is out of square");

            return null;
        }

        /// <summary>
        /// ユニットがTileに移動してきた際にEnterEventHandlerから呼び出される
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void EnterEvent(object o, EnterEventArgs args)
        {
            if (startTile == null) return;

            if (!unitsController.activeUnit.Equals(args.unitEnter)) return;

            Print($"Unit {args.unitEnter} is moved to {args.toTile}");

            var erasedCell = startTile.borderOnTiles.Removed(args.toTile);
            erasedCell.Add(startTile);
            erasedCell.ForEach((cell) =>
            {
                cell.UnitExit();
                cell.DisappearTileDecal();
            });

            args.toTile.AppearAsCurrentTileDecal();

            args.unitEnter.MovedDifferentTile(args.toTile);

            AttackEnteringEnemy(args.toTile, args.unitEnter);
        }

        /// <summary>
        /// UnitのTile位置のアップデートとTileの描写を強制的に行う
        /// </summary>
        internal void ForceUpdateUnitsInTile()
        {
            var active = unitsController.activeUnit;

            Tiles.ForEach(t =>
            {
                var isActive = t.gameObject.activeSelf;
                t.gameObject.SetActive(true);
                //t.ReloadUnitsInCell();
                t.gameObject.SetActive(isActive);
            });

            var currentActiveTile = active.tileCell;
            if (startTile != currentActiveTile)
            {
                // activeUnitのstartTileとcurrentActiveTileが異なっている場合activeUnitは既に移動しているということ
                var erasedCell = startTile.borderOnTiles.Removed(currentActiveTile);
                erasedCell.Add(startTile);
                erasedCell.ForEach((cell) =>
                {
                    cell.UnitExit();
                    cell.DisappearTileDecal();
                });
                currentActiveTile.AppearAsCurrentTileDecal();
                active.MovedDifferentTile(currentActiveTile);
                AttackEnteringEnemy(currentActiveTile, active);
            }
            else
            {
                // activeUnitのstartTileとcurrentActiveTileが同じ場合activeUnitはまだ移動していないということ
                var otherCells = Tiles.Removed(currentActiveTile);
                otherCells.Except(currentActiveTile.borderOnTiles);
                otherCells.ForEach(c => c.DisappearAsNotConnection());

                // CurrentCellを表示
                currentActiveTile.AppearAsCurrentCell();

                // 境界のCellをborderCellとして表示
                currentActiveTile.borderOnTiles.ForEach(c =>
                {
                    c.AppearAsBorderCell(currentActiveTile, UnitCursor);
                });

                //UnitIsMoved = false;
            }
        }

        /// <summary>
        /// Tileに侵入してきた敵勢力に対して迎撃を行う
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="unit">Tileに侵入してきたEnemy</param>
        private void AttackEnteringEnemy(TileCell tile, Character.UnitController unit)
        {
            List<UnitController> counterattacks = null;

            if (unit.Attribute == UnitAttribute.ENEMY)
            {
                // enemyが侵入してきた場合playerは発見非発見にかかわらず迎撃
                counterattacks = tile.UnitsInCell.FindAll(u =>
                {
                    var distance = unit.EnemyAndDistanceDict.GetValueOrDefault(u, 0);
                    var isFrontOfPlayer = u.IsTargetFrontOfUnit(unit);
                    var isNotUsingGimmick = u.TpsController.FollowingGimmickObject == null;
                    return u.IsEnemyFromMe(unit) && distance != 0 &&  isNotUsingGimmick;
                });
            }
            else if (unit.Attribute == UnitAttribute.PLAYER)
            {
                // Playerが移動した場合そのUnitが発見されているなら反撃を受ける
                // とりあえず現時点では何らかのGimmick使用中のUnitは反撃しない
                counterattacks = tile.UnitsInCell.FindAll(u => 
                {
                    if (u.TpsController.FollowingGimmickObject != null)
                        return false;
                    else if (u.aiController.FindedEnemies.Find(e => e.Enemy == unit) != null)
                    {
                        var distance = unit.EnemyAndDistanceDict.GetValueOrDefault(u, 0);
                        return distance != 0;
                    }
                    else
                    {
                        return false;
                    }
                });
            }
            Print($"Counterattack: {counterattacks.Count} units try to counterattack to {unit} in {tile}");

            if (counterattacks.Count != 0)
            {
                // カウンター攻撃とダメージアニメーションを実行
                if (unit.Attribute == UnitAttribute.ENEMY)
                    StartCoroutine(unit.WaitUntilCounterAttack());
                foreach (var counterAttackUnit in counterattacks)
                {
                    StartCoroutine(counterAttackUnit.CounterattackWhenEnemyEnter(unit));
                }
            }
        }

        /// <summary>
        /// Radius内にあるGimmickを取得する
        /// </summary>
        /// <param name="hitPosition"></param>
        /// <param name="radius"></param>
        /// <param name="isBomb">迫撃砲などの大型爆発系武器</param>
        /// <returns></returns>
        internal List<GimmickObject> GetGimmicksWithinRadius(Vector3 hitPosition, float radius, bool isBomb)
        {
            print(gimmickObjects.Count);
            var output = new List<GimmickObject>();
            foreach(var gimmick in gimmickObjects)
            {
                if (!gimmick.IsSoftyDestructible && !gimmick.IsHardyDestructible)
                    continue;
                var distance = Vector3.Distance(hitPosition, gimmick.transform.position);
                if(distance > radius)
                    continue;

                if (!isBomb && gimmick.IsSoftyDestructible)
                    output.Add(gimmick);
                else if (isBomb && gimmick.IsHardyDestructible)
                    output.Add(gimmick);
            }
            return output;
        }

        ///<summary>
        /// Radius内にあるDestructibleObjectを取得する
        /// </summary>
        internal List<GameObject> GetDestructibleObjectsWithinRadius(Vector3 hitPosition, float radius, bool isBomb)
        {
            var output = new List<GameObject>();
            foreach (var obj in DestructibleObjects)
            {
                var distance = Vector3.Distance(hitPosition, obj.transform.position);
                if (distance > radius)
                    continue;

                if (!isBomb)
                {
                    // 軽量爆薬で破壊できるオブジェクト
                    if (obj.CompareTag("SoftyDestructible"))
                        output.Add(obj);
                }
                else
                {
                    // 重量爆薬で破壊できるオブジェクト
                    if (obj.CompareTag("HardyDestructible"))
                        output.Add(obj);
                }
            }
            return output;
        }

        /// <summary>
        /// TIleIDからTileCellを取得する
        /// </summary>
        public TileCell GetTileCellWithID(string id)
        {
            return Tiles.Find(t => t.id.Equals(id));
        }

#if UNITY_EDITOR
        /// <summary>
        /// Tabletの位置を指定したTileに移動する Editor内でのみ使用可能
        /// </summary>
        /// <param name="tileID"></param>
        public void DebugSetTabletToStartPosition(string tileID)
        {
            var tiles = tilesParent.GetComponentsInChildren<TileCell>().ToList();
            var targetTile = tiles.Find(t => t.id.Equals(tileID));
            if (targetTile != null)
            {
                if (targetTile.StartTabletPosition != null)
                {
                    Print("TacticsTablet is moved to", targetTile, targetTile.StartTabletPosition.position);
                    tacticsTablet.transform.position = targetTile.StartTabletPosition.position;
                    tacticsTablet.transform.rotation = targetTile.StartTabletPosition.rotation;
                }
                else
                {
                    PrintWarning("StartTabletPosition of", targetTile, "is null");
                }
            }
            else
            {
                PrintWarning($"TileID {tileID} is not match");
            }
        }
#endif


        #region A* Measure
        /// <summary>
        /// 最短距離を取得する
        /// StartとGoalのセルを含む
        /// </summary>
        /// <param name="start">開始Tile</param>
        /// <param name="goal">終了Tile</param>
        /// <param name="exceptTiles">経路探索から除外するTileCells</param>
        public List<TileCell> GetShortestWay(TileCell start, TileCell goal, List<TileCell> exceptTiles = null)
        {
            // startとgoalが横に並んでいる場合
            if (start.borderOnTiles.Contains(goal))
                return new List<TileCell> { start, goal };

            // すべてのCellをStart to Goal順にStepで値付け
            var recentCells = new List<TileCell> { start };
            var _recentCells = new List<TileCell>();
            var dic = new Dictionary<TileCell, APlusInfo>();
            Tiles.ForEach(c => dic.Add(c, new APlusInfo(c, -1)));
            
            if (exceptTiles != null)
            {
                exceptTiles.ForEach(e => dic.Remove(e));
            }

            var step = 1;
            dic[start].step = 0;
            while (true)
            {
                recentCells.ForEach(r =>
                {

                    foreach(var b in r.borderOnTiles)
                    {
                        if (exceptTiles != null && exceptTiles.Contains(b))
                            continue;

                        if (dic[b].step != -1)
                            continue;

                        dic[b].step = step;
                        _recentCells.Add(b);
                    }

                });
                if (_recentCells.Count == 0)
                    break;
                recentCells.Clear();
                recentCells.AddRange(_recentCells);
                _recentCells.Clear();
                step += 1;
            }

            // goalのstepが0ならば到達できなかったということ
            if (dic[goal].step == 0)
                return new List<TileCell>();

            // Goal to Startの順にstep + score = weightから低くなるように移動
            // 移動した順をList化
            var way = new List<TileCell> { goal };
            var currentCell = goal;
            
            while (true)
            {
                var nextCells = currentCell.borderOnTiles.Except(way).ToList();
                if (exceptTiles != null)
                    exceptTiles.ForEach(e => nextCells.Remove(e));

                if (nextCells.Count() == 0)
                {
                    PrintWarning("No way from", goal.id, "to", start.id);
                    return new List<TileCell>();
                }
                var minCell = nextCells.FindMin(b =>
                {
                    return dic[b].weight;
                });

                // currentCellのweightより高いweightのminCellに移動したらループ防止の為終了
                if (dic[minCell].weight > dic[currentCell].weight)
                {
                    way.Reverse();
                    return way;
                }

                way.Add(minCell);
                currentCell = minCell;
                if (currentCell == start)
                    break;
            }

            way.Reverse();

            return way;
        }

        /*
         * すべてのCellをGoalから順にIndexつけ、一つ横に行くたびにIndexは1づつ増えていく 簡略化
         */
        private class APlusInfo
        {
            internal APlusInfo(TileCell cell, int step)
            {
                this.cell = cell;
                this.step = step;
                this.score = 0;
            }

            internal TileCell cell;
            internal int step;
            internal float score;
            internal float weight { get { return step + score; } }

            public override string ToString()
            {
                return $"{cell}: {step}";
            }
        }
        #endregion

    }

    /// <summary>
    /// TilesControllerのInspectorを拡張する
    /// </summary>
    [CustomEditor(typeof(TilesController))]
    public class TilesControllerScriptEditor : Editor
    {

        TilesController tilesController;

        string startTileID = "";    

        /// <summary>
        /// InspectorのGUIを更新
        /// </summary>
        public override void OnInspectorGUI()
        {
            //元のInspector部分を表示
            base.OnInspectorGUI();

            tilesController = target as TilesController;

            GUILayout.Space(10);
            GUILayout.Label("Debug");
            GUILayout.Label("Tableの開始位置のTileIDを指定\nTabletが位置に移動");
            startTileID = GUILayout.TextField(startTileID);

            //ボタンを表示
            if (GUILayout.Button("Change tablet position"))
            {
                if (startTileID.Length != 0)
                    tilesController.DebugSetTabletToStartPosition(startTileID);
            }
        }

    }
}