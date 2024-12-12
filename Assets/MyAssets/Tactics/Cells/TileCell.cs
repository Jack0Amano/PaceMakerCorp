using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using Tactics.Character;
using System.Linq;
using static Utility;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using Tactics.Object;
using EventGraph.InOut;

namespace Tactics.Map
{
    /// <summary>
    /// Tactics画面のTileの一つ一つにアタッチされる
    /// </summary>
    [DefaultExecutionOrder(1)]
    public class TileCell : MonoBehaviour
    {
        [Tooltip("TileのID")]
        [SerializeField] public string id;

        [Tooltip("このTileからスタートするときTacticsTabletが準備状態で表示される位置")]
        [SerializeField] public Transform StartTabletPosition;
        
        [Tooltip("Cell内の侵入検知TriggerObject")]
        [SerializeField] public NortifyTrigger NortifyTrigger;

        [Tooltip("Cell内の侵入検知TriggerObjectの追加分 サイズ変化は無いため隙間を埋めるTrigger")]
        [SerializeField] public List<NortifyTrigger> SubNortifyTriggers = new List<NortifyTrigger>();
        
        [Tooltip("移動するのによい場所のGameObjectsを子Objectに含むObject")]
        [SerializeField] public GameObject betterPositions;

        [Tooltip("Square内に存在できるUnitのWeightの総重量\n" +
                 "歩兵 = 2, 戦車 = 5\n" +
                 "Weight e.g. 細道 = 4, 大通り = 7 or 9")]
        [SerializeField] public int limitUnitsWeight = 6;
        
        [Tooltip("Wallのリスト")]
        [SerializeField] internal List<Wall> walls = new List<Wall>();

        [Header("Spawn Point")]
        [Tooltip("Enemyのスポーンの優先順")]
        [SerializeField] public int spawnPriority = 0;

        [Tooltip("StartPositionに対応するTileの位置 StartPositionで指定された場合合致するTileから味方が出撃する")]
        [SerializeField] public StartPosition StartPosition;

        [Tooltip("StartPositionに対応して敵が出撃する場合のスポーンポイント")]
        [SerializeField] public bool IsEnemyStartPosition = false;

        [Tooltip("Tankが含まれている場合のスポーンポイント")]
        [SerializeField] public TileSpawnPoints withTankSpawnPoints;

        [Tooltip("歩兵のみの場合のスポーンポイント")]
        [SerializeField] public TileSpawnPoints onlyManSpawnPoints;

        [Header("Tile")]
        [Tooltip("Cell内のTile")]
        [SerializeField] public GameObject tileNeonObject;

        [Header("Debug GUI")]
        [Tooltip("GUIテキストの状態")]
        [SerializeField] private GUIStyle gUIStyle;

        [Tooltip("GridPointの番号表示のGUIテキスト")]
        [SerializeField] private GUIStyle gridPointGuiStyle;

        [Tooltip("生成したGridPointsをGizmosに表示する")]
        [SerializeField] private bool showGridPointsOnGizmos = false;

        internal List<CoverGimmick> SandbagGimmicks = new List<CoverGimmick>();
        internal List<MortarGimmick> MortorGimmicks = new List<MortarGimmick>();

        /// <summary>
        /// Userによるギミック可能なObjects
        /// </summary>
        private List<GimmickObject> GimmickObjects
        {
            get
            {
                if (_GimmickObjects == null)
                {
                    _GimmickObjects = new List<GimmickObject>();
                    _GimmickObjects.AddRange(SandbagGimmicks);
                    _GimmickObjects.AddRange(MortorGimmicks);
                }
                return _GimmickObjects;
            }
        }
        private List<GimmickObject> _GimmickObjects;

        /// <summary>
        /// このSquareに隣接するSquareのリスト (Unitが移動できるSquare)
        /// </summary>
        internal List<TileCell> borderOnTiles = new List<TileCell>();
        /// <summary>
        /// SquareにUnitが入ってきた場合のEvent
        /// </summary>
        internal event EventHandler<EnterEventArgs> EnterEventHander;
        /// <summary>
        /// UnitsListとactiveUnitにアクセスする用のUnitsController
        /// </summary>
        internal UnitsController unitsController;
        /// <summary>
        /// Cell内に存在するUnits　<c>ReloadUnitsInCell()</c>で更新している
        /// </summary>
        [SerializeField, ReadOnly] public List<UnitController> UnitsInCell = new List<UnitController>();
        /// <summary>
        /// Square内の勢力図
        /// </summary>
        internal (float friend, float enemy) scopeInfluence = (0, 0);
        /// <summary>
        /// 現在のSquareのWeight ReloadUnitsInSquare() で更新
        /// </summary>
        [SerializeField, ReadOnly] public int currentWeight = 0;
        /// <summary>
        /// Square上のGridとその脅威値 このGridの点上が移動候補地点となる
        /// </summary>
        internal List<PointInTile> pointsInTile = new List<PointInTile>();
        /// <summary>
        /// 多少の高速化のためのgameObject保持
        /// </summary>
        internal GameObject thisObject;
        /// <summary>
        /// Cell内のTriggerObjectのColliderを取得
        /// </summary>
        private BoxCollider triggerCollider;
        /// <summary>
        /// TriggerのEnterEventを行うか
        /// </summary>
        [SerializeField, ReadOnly] bool isTriggerActive = false;

        /// <summary>
        /// GridPointsの点の間隔
        /// </summary>
        internal float distanceBetweenGridPoints = 0.5f;

        /// <summary>
        /// TileのNeon表示の発光カラー
        /// </summary>
        private const string EmissionNameOfTileNeonMaterial = "_EmissionColor";

        /// <summary>
        /// TileObjectのMaterial
        /// </summary>
        private Material tileNeonMaterial;

        private GeneralParameter parameters;

        void Awake()
        {
            thisObject = gameObject;

            // TriggerObjectから各Componentを取得
            triggerCollider = NortifyTrigger.GetComponent<BoxCollider>();

            onlyManSpawnPoints.parent.SetActive(false);
            withTankSpawnPoints.parent.SetActive(false);

            withTankSpawnPoints.tankSpawnPoints.ForEach(p => p.gameObject.SetActive(false));
            onlyManSpawnPoints.manSpawnPoints.ForEach(p => p.gameObject.SetActive(false));

            NortifyTrigger.OnTriggerEnterAction += OnUnitEnter;
            NortifyTrigger.OnTriggerStayAction += OnUnitEnter;

            SubNortifyTriggers.ForEach(t => t.OnTriggerEnterAction += OnUnitEnter);
            SubNortifyTriggers.ForEach(t => t.OnTriggerStayAction += OnUnitEnter);

            tileNeonMaterial = tileNeonObject.GetComponent<MeshRenderer>().material;

            if (GameManager.Instance != null)
                parameters = GameManager.Instance.GeneralParameter;
        }


        // Start is called before the first frame update
        void Start()
        {
            foreach (var w in walls)
                w.wallObject.SetActive(false);
        }

        /// <summary>
        /// Unitのロードが終了した際に呼び出す UnitsがどのTileにいるか検索
        /// </summary>
        public IEnumerator UnitLoaded()
        {
            yield return null;
            ReloadUnitsInCell();
            UpdateScopeinfluence();
        }

        /// <summary>
        /// UnitがこのTileに進行できるか計算
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool CanEnterUnitInThis(UnitController unit)
        {
            return limitUnitsWeight - currentWeight >= unit.CurrentParameter.Data.Weight;
        }

        #region Locations and Score
        internal void LoadGridPoints()
        {
            pointsInTile.Clear();
            var pts = GetGridPositions();

            foreach (var pt in pts)
            {
                var locAndScore = new PointInTile(this, pt);
                pointsInTile.Add(locAndScore) ;
            }

            const float nearDistance = 0.5f;
            foreach(Transform t in betterPositions.transform)
            {
                var pos = t.position;

                pointsInTile.ForEach(p =>
                {
                    if (Vector3.Distance(p.location, pos) < nearDistance)
                        p.isActive = false;
                });

                pos.y = gameObject.transform.position.y;
                var locAndScore = new PointInTile(this, pos);
                locAndScore.isNormalPosition = true;
                pointsInTile.Add(locAndScore);
                t.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// グリッド状にPointを作成
        /// </summary>
        /// <returns></returns>
        List<Vector3> GetGridPositions()
        {
            const float difference = 0.5f;
            var scale = transform.localScale;

            var pt1 = transform.position + (scale / 2);
            pt1.y = transform.position.y;
            var pt2 = transform.position - (scale / 2);
            pt2.y = transform.position.y;
            var pt3 = pt1;
            pt3.x -= scale.x;
            var pt4 = pt2;
            pt4.x += scale.x;

            var pts = new List<Vector3>();
            for (var i = 1; i < ((scale.x - difference * 2) / distanceBetweenGridPoints); i++)
            {
                var _pt = pt2;
                _pt.x += difference + distanceBetweenGridPoints * i;
                _pt.z += difference;
                for (var j = 1; j < ((scale.z - difference * 2) / distanceBetweenGridPoints); j++)
                {
                    var pt = _pt;
                    pt.z += distanceBetweenGridPoints * j;
                    pts.Add(pt);
                }
            }

            return pts;
        }

        /// <summary>
        /// Tileに位置しているGimmickObjectsを設定する
        /// </summary>
        /// <param name="objects"></param>
        internal void SetGimmickObjects(List<Object.GimmickObject> objects)
        {
            // GimmickObjctに近いGridPointを非有効化する
            pointsInTile.ForEach(g =>
            {
                var contain = objects.Find(o => o.Contain(g.location));
                if (contain != null)
                    g.isActive = false;
            });

            var allObjectsInTile = GetAllColliderInTile();
            SandbagGimmicks.Clear();
            MortorGimmicks.Clear();

            objects.ForEach((o =>
            {

                if (o is CoverGimmick sandbag)
                {
                    foreach (var pos in sandbag.safePositions)
                    {
                        if (allObjectsInTile.Contains(pos.gameObject))
                        {
                            var _pos = pos.position;
                            _pos.y = transform.position.y;
                            var s = new PointInTile(this, _pos, o.gameObject);
                            pointsInTile.Add((PointInTile)s);

                            pos.gameObject.SetActive(false);
                        }
                    }
                    SandbagGimmicks.Add(sandbag);
                }
                else if (o is MortarGimmick mortor)
                {
                    MortorGimmicks.Add(mortor);
                }
            }));
        }

        /// <summary>
        /// 指定された<c>areas</c>内に存在する<c>LocationAndScore</c>を非有効化する
        /// </summary>
        /// <param name="areas"></param>
        /// <returns></returns>
        internal IEnumerator InactivePointInRemoveArea(List<MeshFilter> areas)
        {
            var pts = pointsInTile.ConvertAll(p => p.location);

            foreach(var area in areas)
            {
                var isInRemoveArea = IsPointInCube(pts, area);
                for (var i = 0; i < isInRemoveArea.Count; i++)
                {
                    if (isInRemoveArea[i])
                        pointsInTile[i].isActive = false;
                }
            }

            yield return true;
        }

        /// <summary>
        /// <c>target</c>が<c>points</c>を繋いだ領域上に存在するかチェック
        /// </summary>
        /// <param name="points"></param>
        /// <param name="target"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public bool Check(Vector3[] points, Vector3 target, Vector3 normal)
        {
            Quaternion rot = Quaternion.FromToRotation(normal, -Vector3.forward);

            Vector3[] rotPoints = new Vector3[points.Length];

            for (int i = 0; i < rotPoints.Length; i++)
            {
                rotPoints[i] = rot * points[i];
            }

            target = rot * target;

            int wn = 0;
            float vt = 0;

            for (int i = 0; i < rotPoints.Length; i++)
            {
                // 上向きの辺、下向きの辺によって処理を分ける

                int cur = i;
                int next = (i + 1) % rotPoints.Length;

                // 上向きの辺。点PがY軸方向について、始点と終点の間にある。（ただし、終点は含まない）
                if ((rotPoints[cur].y <= target.y) && (rotPoints[next].y > target.y))
                {
                    // 辺は点Pよりも右側にある。ただし重ならない
                    // 辺が点Pと同じ高さになる位置を特定し、その時のXの値と点PのXの値を比較する
                    vt = (target.y - rotPoints[cur].y) / (rotPoints[next].y - rotPoints[cur].y);

                    if (target.x < (rotPoints[cur].x + (vt * (rotPoints[next].x - rotPoints[cur].x))))
                    {
                        // 上向きの辺と交差した場合は+1
                        wn++;
                    }
                }
                else if ((rotPoints[cur].y > target.y) && (rotPoints[next].y <= target.y))
                {
                    // 辺は点Pよりも右側にある。ただし重ならない
                    // 辺が点Pと同じ高さになる位置を特定し、その時のXの値と点PのXの値を比較する
                    vt = (target.y - rotPoints[cur].y) / (rotPoints[next].y - rotPoints[cur].y);

                    if (target.x < (rotPoints[cur].x + (vt * (rotPoints[next].x - rotPoints[cur].x))))
                    {
                        // 下向きの辺と交差した場合は-1
                        wn--;
                    }
                }
            }

            return wn != 0;
        }

        /// <summary>
        /// <c>y=transform.position.y</c>で(x,z)が頂点上に存在するverticesを返す
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private List<Vector3> GetPoints(Transform transform, MeshFilter filter)
        {
            var cubePts = new List<Vector3>();
            foreach (var v in filter.mesh.vertices)
            {
                var pt = transform.TransformPoint(v);
                pt.y = transform.position.y;

                if (cubePts.Contains(pt))
                    continue;
                cubePts.Add(pt);
            }

            if (cubePts.Count != 4)
                PrintWarning($"PointOnCubeY: {gameObject}, Rotation:{transform.rotation} Rotation XZ must be 0");

            return new List<Vector3> { cubePts[0], cubePts[1], cubePts[3], cubePts[2] };
        }

        /// <summary>
        /// <c>pts</c>が<c>filter</c>のポリゴン内に存在する場合true、存在しないときfalseになるような配列を返す
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private List<bool> IsPointInCube(List<Vector3> pts, MeshFilter filter)
        {
            var cubePts = GetPoints(filter.transform, filter);

            var output = new List<bool>();
            foreach (var pt in pts)
                output.Add(Check(cubePts.ToArray(), pt, new Vector3(0, 1, 0)));

            return output;
        }

        /// <summary>
        /// Rateに即した割合でLocationAndScoreをランダムに有効化する
        /// </summary>
        /// <param name="rate"></param>
        internal void SetRandomPosition(float rate)
        {
            //var actives = locationsAndScores.FindAll(l => l.isActive && l.coverObject == null);
            //var disabledCount = (int)(actives.Count * (1 - rate));
            //actives = actives.Shuffle();
            //for (var i = 0; i < disabledCount; i++)
            //    actives[i].isActive = false;
        }

        /// <summary>
        /// Unitに最も近い起動可能なGimmickObjectを取得する
        /// </summary>
        /// <param name="unitTransform"></param>
        /// <returns></returns>
        internal GimmickObject GetNearlyGimmickObject(Transform unitTransform)
        {
            var nearGimmicks = GimmickObjects.FindAll(o => {
                var dist = Vector3.Distance(o.transform.position, unitTransform.position);
                return dist < o.DistanceActivateGimmick;
                });
            if (nearGimmicks.Count == 0) return null;
            return nearGimmicks.FindMin(g => Vector3.Distance(g.transform.position, unitTransform.position));
        }

        #endregion

        #region Nortification from Trigger

        /// <summary>
        /// Unitと同期して動くCursorがCellに入ってきた場合の呼び出し
        /// </summary>
        /// <param name="collision"></param>
        private void OnUnitEnter(Collider collision)
        {
            if (!isTriggerActive) return;
            if (collision.gameObject.CompareTag("CellCursor") && 
                unitsController.activeUnit != null && 
                !unitsController.activeUnit.IsAlreadyMovedDifferentTile &&
                unitsController.activeUnit.tileCell != this)
            {

                // Unitの位置を更新
                var fromTile = unitsController.activeUnit.tileCell;
                fromTile.UnitsInCell.Remove(unitsController.activeUnit);
                this.UnitsInCell.Add(unitsController.activeUnit);

                // TileのEnterEventの実行を記録
                isTriggerActive = false;
                fromTile.isTriggerActive = false;

                // Tileの勢力図を更新
                fromTile.UpdateScopeinfluence();
                this.UpdateScopeinfluence();

                // EnterEventHandlerでTilesController.EnterEventを呼び出す
                EnterEventHander(this, new EnterEventArgs()
                {
                    unitEnter = unitsController.activeUnit,
                    toTile = this,
                    fromTile = fromTile
                });

                // CellとTriggerのdifferenceを0に
                SetDifferenceCellAndTrigger(0f);

                // ActiveUnitがCellに入ってきた
                // IsStandAloneModeではEnemyのUnitが侵入してきた場合はWallの遮断を行わない
                if (!unitsController.activeUnit.isAiControlled)
                {
                    CellWallControl(true);
                }
            }
        }
        #endregion

        #region Cellの表示非表示
        /// <summary>
        /// 現在Unitが位置しているCellとして表示する
        /// </summary>
        internal void AppearAsCurrentCell()
        {
            isTriggerActive = false;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            // 自身のwallのcolliderは無くして
            walls.ForEach(elem =>
            {
                elem.collider.enabled = elem.contactCells.Count == 0;
                elem.wallObject.SetActive(true);

                elem.navigationStatic.SetActive(false);
            });

            SetDifferenceCellAndTrigger(0.05f);

            AppearAsCurrentTileDecal();
        }


        /// <summary>
        /// 移動可能セルとして表示する
        /// </summary>
        /// <param name="currentTile">現在Unitがいるcell (thisの横になるタイル)</param>
        /// <param name="cursor">cursorオブジェクト</param>
        /// <param name="noWallMode">wallのcolliderを使用しない (AIの移動を確実に行うためのモード)</param>
        internal void AppearAsBorderCell(TileCell currentTile, CapsuleCollider cursor, bool noWallMode = false)
        {
            isTriggerActive = true;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            var remainWeight = limitUnitsWeight - currentWeight;
            if (remainWeight - unitsController.activeUnit.CurrentParameter.Data.Weight >= 0)
            {
                // Weightに余裕があり移動可能なマス
                walls.ForEach(wall =>
                {
                    wall.wallObject.SetActive(true);
                    wall.collider.enabled = false;

                    if (wall.contactCells.Count == 0)
                    {
                        // 他のCellと接続していないWall  経路探索
                        wall.navigationStatic.SetActive(true);
                    }
                    else if (wall.contactCells.Contains(currentTile))
                    {
                        // ここが横と接続しているwall
                        wall.navigationStatic.SetActive(false);
                    }
                    else
                    {
                        // 他のCellとの接続面かつ移動可能範囲の最大距離
                        wall.navigationStatic.SetActive(true);
                    }
                });
                AppearAsBorderTileDecal();
            }
            else
            {
                // Weightに余裕がなく移動不可能なマス
                walls.ForEach(wall =>
                {
                    wall.wallObject.SetActive(true);
                    wall.collider.enabled = false;
                    // 侵入不可能なマス
                    wall.navigationStatic.SetActive(true);
                });
                AppearAsNotEnterTileDecal();
            }

            NortifyTrigger.transform.localScale = SetDifferenceCellAndTrigger(cursor.radius);
        }


        /// <summary>
        /// Unitの移動不可能Cellとして表示とColliderを消す （移動不可能WallのColliderはActiveに)
        /// </summary>
        internal void DisappearAsNotConnection()
        {
            isTriggerActive = false;
            walls.ForEach(elem =>
            {
                elem.collider.enabled = false;
                elem.wallObject.SetActive(false);
                DisappearTileDecal();
            });
        }

        /// <summary>
        /// CellとTriggerの間の隙間を設定する
        /// </summary>
        /// <param name="difference">隙間の距離</param>
        private Vector3 SetDifferenceCellAndTrigger(float difference)
        {
            var scaleRate = (transform.localScale.Subtraction(difference)).Divide(transform.localScale);
            var triggerScale = Vector3.one.Multiply(scaleRate);
            triggerScale.y = 1;
            return triggerScale;
        }
       
        /// <summary>
        /// TileDecalを非表示状態にする
        /// </summary>
        internal void DisappearTileDecal()
        {
            StartCoroutine(tileNeonMaterial.SetColor(EmissionNameOfTileNeonMaterial,
                                                     Color.clear,
                                                     parameters.TileColorAnimationCurve));
        }

        /// <summary>
        /// TileDecalを現在位置として表示状態にする
        /// </summary>
        internal void AppearAsCurrentTileDecal()
        {
            // 空きwiehgtが2未満の場合（歩兵も侵入できない場合）NotEnterTileDecalを表示する
            if (limitUnitsWeight - currentWeight < 2)
            {
                AppearAsNotEnterTileDecal();
                return;
            }
            StartCoroutine(tileNeonMaterial.SetColor(EmissionNameOfTileNeonMaterial,
                                                     parameters.CurrentTileColor,
                                                     parameters.TileColorAnimationCurve));
        }

        /// <summary>
        /// TileDecalを隣接Tileとして表示状態にする
        /// </summary>
        internal void AppearAsBorderTileDecal()
        {
            StartCoroutine(tileNeonMaterial.SetColor(EmissionNameOfTileNeonMaterial,
                                                     parameters.CanEnterTileColor,
                                                     parameters.TileColorAnimationCurve));
        }

        /// <summary>
        /// TileDecalを移動不可能Tileとして表示状態にする
        /// </summary>
        private void AppearAsNotEnterTileDecal()
        {
            StartCoroutine(tileNeonMaterial.SetColor(EmissionNameOfTileNeonMaterial,
                                                     parameters.NotEnterTileColor,
                                                     parameters.TileColorAnimationCurve));
        }
        #endregion

        #region State Update
        /// <summary>
        /// ユニットがSquareから出た際に呼び出される
        /// </summary>
        internal void UnitExit()
        {

            // Squareの勢力図を更新

            UpdateScopeinfluence();

            // activeUnitはcellから出ていった
            CellWallControl(false);
        }


        /// <summary>
        /// Unit Enter Exitで使用される Wallを動作後数秒遅れて遮断
        /// </summary>
        /// <param name="unitEnter"></param>
        /// <returns></returns>
        private void CellWallControl(bool unitEnter)
        {
            walls.ForEach(elem =>
            {
                elem.collider.enabled = true;
                elem.wallObject.SetActive(unitEnter);
            });
        }


        /// <summary>
        /// Squareに居るUnitsのリストを更新する ColliderからUnitを検知
        /// UnitがTileの端にいる場合検知が行われない可能性があるため初期化時のみ使用
        /// </summary>
        /// <returns>更新した際の差分</returns>
        private List<UnitController> ReloadUnitsInCell()
        {

            // TriggerのScaleを1にする
            // UnitがTriggerとWallの間に入ってしまった場合にUnitの検知が行われないためs
            var oldScale = triggerCollider.transform.localScale;
            triggerCollider.transform.localScale = Vector3.one;

            Collider[] hitColliders = Physics.OverlapBox(thisObject.transform.position, 
                                                         triggerCollider.bounds.extents) ;

            var _unitsInCell = new List<UnitController>();
            currentWeight = 0;
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject.tag != "Unit") continue;

                var _unit = hitCollider.gameObject.GetComponent<UnitController>();
                if (_unit != null)
                {
                    _unitsInCell.Add(_unit);
                    currentWeight += _unit.CurrentParameter.Data.Weight;
                }
            }
            print($"ReloadUnitsInCell: {this}, {_unitsInCell.Count} units in this, Time {Time.frameCount}");

            var difference = _unitsInCell.Except<UnitController>(UnitsInCell).ToList();
            difference.AddRange(UnitsInCell.Except<UnitController>(_unitsInCell).ToList());

            UnitsInCell = _unitsInCell;

            UnitsInCell.ForEach(u => u.tileCell = this);

            // TriggerのScaleを元に戻す
            triggerCollider.transform.localScale = oldScale;

            return difference;
        }


        /// <summary>
        /// Squareの勢力図を更新
        /// </summary>
        private void UpdateScopeinfluence()
        {
            foreach (var unit in UnitsInCell)
            {
                if (unit.Attribute == UnitAttribute.ENEMY)
                    scopeInfluence.enemy += unit.CurrentParameter.menace;
                else
                    scopeInfluence.friend ++;
            }
        }

        /// <summary>
        /// Tile内に存在するすべてのコライダーを持つObjectを取得する
        /// </summary>
        /// <returns></returns>
        internal List<GameObject> GetAllColliderInTile()
        {
            Collider[] hitColliders = Physics.OverlapBox(thisObject.transform.position,
                                                         triggerCollider.bounds.extents);
            return hitColliders.ToList().ConvertAll(c => c.gameObject);
        }
        #endregion


#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            Handles.color = Color.yellow;
            Handles.Label(transform.position, id, gUIStyle);
            
            if (showGridPointsOnGizmos && pointsInTile != null)
            {
                var xx = pointsInTile.FindAll(l => l.DebugScore != 0);

                pointsInTile.ForEach(g => 
                {
                    
                    if (g.isActive)
                    {
                        if (g.DebugScore != 0)
                            Handles.Label(g.location, g.DebugScore.ToString(), gridPointGuiStyle);
                    }
                });
            }

        }
#endif  


        public override string ToString()
        {
            return $"TileCell({id}), {UnitsInCell.Count} units in this";
        }
    }

    internal class EnterEventArgs : EventArgs
    {
        /// <summary>
        /// 侵入してきたUnit
        /// </summary>
        internal UnitController unitEnter;

        /// <summary>
        /// 移動先のCell
        /// </summary>
        internal TileCell toTile;

        /// <summary>
        /// 移動元のTile
        /// </summary>
        internal TileCell fromTile;

        public override string ToString()
        {
            return $"EnterEventArgs(Unit:{unitEnter}, ToTile:{toTile}, FromTile:{fromTile})";
        }
    }

    /// <summary>
    /// 初期配置時の位置情報をバンドルするためのclasss
    /// </summary>
    [Serializable]
    public class TileSpawnPoints
    {
        public GameObject parent;
        public List<Transform> tankSpawnPoints;
        public List<Transform> manSpawnPoints;
    }

    /// <summary>
    /// Tile内のPointとPointをカバーするGameObject
    /// </summary>
    [Serializable]
    class PointInTile
    {
        public PointInTile(TileCell tileCell, Vector3 location)
        {
            this.location = location;
            TileCell = tileCell;
        }

        public PointInTile(TileCell tileCell, Vector3 location, GameObject cover)
        {
            coverObject = cover;
            this.location = location;
            TileCell = tileCell;
        }

        public PointInTile(UnitController unitController)
        {
            location = unitController.transform.position;
            TileCell = unitController.tileCell;
        }

        public TileCell TileCell;

        /// <summary>
        /// Gridに位置したときにカバーとなるポイント
        /// </summary>
        public GameObject coverObject;

        /// <summary>
        /// Gridの位置
        /// </summary>
        public Vector3 location;

        /// <summary>
        /// 使用可能なLocationAndScoreか
        /// </summary>
        public bool isActive = true;

        /// <summary>
        /// Unitが通常状態のときに優先する位置
        /// </summary>
        public bool isNormalPosition = false;

        /// <summary>
        /// デバック用にpositionを表示する場合の表示カラー
        /// </summary>
        public Color DebugColor = Color.white;

        [NonSerialized] public float DebugScore = 0;

        public override string ToString()
        {
            return $"PointInTile(Tile:{TileCell}, Loc:{location}, Cover:{coverObject})";
        }
    }
}
