using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using DG.Tweening;
using System;
using System.IO;
using UnityEngine.UI;
using static Utility;

namespace MainMap
{
    public class MapSquad : MonoBehaviour
    {
        /// <summary>
        /// Squadのパラメーター
        /// </summary>d
        [SerializeField] internal Squad data;

        [Tooltip("移動スピード")]
        /// <summary>
        /// 移動スピード
        /// </summary>
        public float Speed = 0.006f;

        internal UI.TableIcons.TableIconsPanel tableOverlayPanel;

        /// <summary>
        /// Locationデータ
        /// </summary>
        internal MapLocations mapLocations;

        /// <summary>
        /// Squadが移動する際のアニメーションSquad
        /// </summary>
        internal Sequence moveSequence;
        /// <summary>
        /// MoveAnimationが終わったときのSequence
        /// </summary>
        private Action onCompleteMoveSequence;
        /// <summary>
        /// Squadが移動するpath
        /// </summary>
        internal List<Transform> squadMoveCheckPoints;
        /// <summary>
        /// SquadがSpawnpointに到達したときの呼び出し
        /// </summary>
        internal Action<MapLocation, MapSquad> squadReachedOnSpawnPoint;
        /// <summary>
        /// SquadがSpawnPointの付近から入ったり出た場合の呼び出し ＜位置, 対象Squad, 近づいた場合＞
        /// </summary>
        internal Action<MapLocation, MapSquad, bool> squadGetsNearToSpawnPoint;
        /// <summary>
        /// OverlayCanvasに表示するSquadのイメージ
        /// </summary>
        internal MainMap.UI.TableIcons.SquadImage SquadImage
        {
            set
            {
                squadImage = value;
                UpdateCanvasImagePosition();
            }
            get => squadImage;
        }
        private UI.TableIcons.SquadImage squadImage;
        private GameManager gameManager;
        /// <summary>
        /// 以前のGetNearした時間
        /// </summary>
        private DateTime previousGetNearDateTime;
        /// <summary>
        /// 最後の近づいたり離れたりした場合のロック時間
        /// </summary>
        private const float getNearIntervalTime = 0.5f;

        private GeneralParameter parameter;
        /// <summary>
        /// Squadがturnoutにいる場合はそのtransform
        /// </summary>
        internal Transform turnoutPosition;

        #region Squdの状態のためのproperties
        /// <summary>
        /// SpawnPointに接近しているか
        /// </summary>
        public bool IsNearToSpawnPoint { private set; get; } = true;
        /// <summary>
        /// SquadがSpawnPoint上にいるときそのID リアルタイムで更新されていく
        /// </summary>
        public string IdOfLocateOnSpawnPoint { private set; get; } = "";
        /// <summary>
        /// Squadが
        /// </summary>
        public bool IsOnTurnout
        {
            get => data.IsOnTurnout;
            private set
            {
                data.IsOnTurnout = value;
            }
        }
        /// <summary>
        /// Squadが現在カーソルに追従する形のスポーンモードであるか
        /// </summary>
        internal bool IsSpawnMode
        {
            get => isSpawnMode;
            set
            {
                isSpawnMode = value;
            }
        }
        private bool isSpawnMode = false;
        /// <summary>
        /// 操作可能なSquadかどうか アニメーション中などはコントロール不可
        /// </summary>
        public bool IsControllable { private set; get; } = true;
        /// <summary>
        /// SquadのMap上での行動
        /// </summary>
        public MapSquad.State SquadState { private set; get; } = MapSquad.State.Waiting;
        /// <summary>
        /// Squadがロケーションの上にいるかどうか
        /// </summary>
        public bool IsOnLocation { get => data.MapLocation != null; }
        /// <summary>
        /// 部隊の名称
        /// </summary>
        public string TeamName { get => data.name;}
        /// <summary>
        /// SquadがMap上に出撃しているか
        /// </summary>
        public bool IsOnMap
        {
            get => data.isOnMap;
            set
            {
                data.isOnMap = value;
            }
        }
        /// <summary>
        /// Squadの指揮官
        /// </summary>
        public UnitData Commander
        {
            get => data.commander;
        }
        /// <summary>
        /// Squadの構成メンバー
        /// </summary>
        public List<UnitData> Members
        {
            get => data.member;
        }
        /// <summary>
        /// Squadの位置するlocation
        /// </summary>
        public MapLocation Location
        {
            get => data.MapLocation;
            set
            {
                data.MapLocation = value;
            }
        }
        /// <summary>
        /// Squadの物資の量
        /// </summary>
        public float SupplyLevel
        {
            get => data.supplyLevel;
            set
            {
                data.supplyLevel = value;
                if (data.supplyLevel > data.MaxSupply)
                    data.supplyLevel = data.MaxSupply;
                else if (data.supplyLevel < 0)
                    data.supplyLevel = 0;
            }
        }

        /// <summary>
        /// Supplyがfullの状態であるか
        /// </summary>
        public bool IsSupplyFull
        {
            get => ((float)data.MaxSupply - data.supplyLevel) < 0.1f;
        }

        /// <summary>
        /// MapのGameObject
        /// </summary>
        private GameObject mapObject;
        #endregion

        protected private void Awake()
        {
            gameManager = GameManager.Instance;
            parameter = GameManager.Instance.GeneralParameter;
            mapObject = transform.parent.parent.gameObject;
        }

        protected private void Update()
        {

            // 歩行後のSupplyLockインターバル
            //if (!IsMoving && lockReducingSupply)
            //{
            //    if (timeBeforeWalk > intervalLockReducingSupply)
            //        lockReducingSupply = false;
            //}
        }

        /// <summary>
        /// Overlaycanvas上のSquadImageの位置をMap上のSquadに同期するように移動する
        /// </summary>
        private void UpdateCanvasImagePosition()
        {
            tableOverlayPanel.UpdateSquadPosition(SquadImage);
        }

        /// <summary>
        /// Squadを選択したときの動作
        /// </summary>
        internal void SelectSquad()
        {
            // print($"Select: {info.name}");
        }

        /// <summary>
        /// Squadの選択が解除されたときの動作
        /// </summary>
        internal void DeSelectSquad()
        {
            
        }

        /// <summary>
        /// 別のSquadが近づいた時Squadが退避エリアに避ける
        /// </summary>
        internal void MoveToTurnout(MapSquads mapSquads)
        {
            var loc = mapLocations.GetLocationFromID(mapSquads, Location.id, true);
            IsOnTurnout = true;
            turnoutPosition = loc;
            MoveTo(loc, 0.5f);
        }

        /// <summary>
        /// TurnoutからSquadを復帰させる
        /// </summary>
        /// <param name="changeImmidiatly">SquadのTurnoutの位置のisTurnOutを即座に変える (changeとか用)</param>
        internal void ReturnFromTurnout(bool changeImmidiatly = false)
        {
            void ChangeProperties()
            {
                IsOnTurnout = false;
                turnoutPosition = null;
            }
            if (changeImmidiatly) ChangeProperties();
            MoveTo(Location.transform, 0.5f, () => { if (!changeImmidiatly) ChangeProperties(); });
        }

        /// <summary>
        /// 現在のアニメーションを中止しlocationへとdurationで移動する
        /// </summary>
        /// <param name="location"></param>
        /// <param name="duration"></param>
        /// <param name="onComplete"></param>
        private void MoveTo(Transform location, float duration, Action onComplete=null)
        {
            bool isPlaying = true;
            IEnumerator MoveIcon()
            {
                while (isPlaying)
                {
                    tableOverlayPanel.UpdateSquadPosition(SquadImage);
                    yield return null;
                }
            }

            if (moveSequence != null && !moveSequence.IsActive())
                moveSequence.Kill();
            moveSequence = DOTween.Sequence();
            moveSequence.Append(transform.DOMove(location.position, duration));
            moveSequence.OnComplete(() =>
            {
                isPlaying = false;
                onComplete?.Invoke();
            });
            moveSequence.OnKill(() => isPlaying = false);
            moveSequence.Play();
            StartCoroutine(MoveIcon());
        }

        /// <summary>
        /// Squadが基地に戻るときにMapSquadsから呼び出される
        /// </summary>
        internal IEnumerator AnimationReturnToBase()
        {
            IsControllable = false;
            yield return StartCoroutine(SquadImage.ReturnToBaseForced());
        }

        /// <summary>
        /// Squadが強制的に帰還されたときのアニメーション
        /// </summary>
        /// <returns></returns>
        internal IEnumerator AnimationReturnToBaseForced()
        {
            IsControllable = false;
            yield return StartCoroutine(SquadImage.ReturnToBaseForced());
        }

        /// <summary>
        /// Squadをlocationに移動させる
        /// </summary>
        /// <param name="location"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        internal IEnumerator MoveAlong(List<Vector3> checkPoints, MapLocation moveTo, Action onComplete = null)
        {

            bool isMoveCompleted = false;
            if (checkPoints == null || checkPoints.Count == 0)
                yield break;

            var positionOfCheckPoint = checkPoints.Last();
            // Squadの移動アニメーションを作成
            moveSequence = DOTween.Sequence();
            var totalTime = 0f;
            for (var i = 1; i < checkPoints.Count; i++)
            {
                var cp0 = checkPoints[i - 1];
                var cp1 = checkPoints[i];
                float distToCP = Vector3.Distance(cp0, cp1) / mapObject.transform.localScale.x;
                // timeToCP = distToCP / GameController.generalParameters.squadSpeedOnMainMap;
                var timeToCP = distToCP / Speed;
                totalTime += timeToCP;
                var moveAnim = transform.DOMove(cp1, timeToCP);
                moveAnim.SetEase(Ease.Linear);
                moveSequence.Append(moveAnim);
            }

            moveSequence.OnComplete(() => isMoveCompleted = true);
            onCompleteMoveSequence = onComplete;

            SquadState = State.Walking;
            moveSequence.Play();
            
            var isOnSpawnPoint = false;
            while (!isMoveCompleted)
            {
                // 移動中のSquadが敵のスポーン位置にいるかどうか監視
                foreach(var l in mapLocations.locations)
                {
                    var distToSpawnPoint = Vector3.Distance(transform.position, l.transform.position);
                    if (distToSpawnPoint < parameter.DistanceOfLocatedOnSpawnPoint)
                    {
                        isOnSpawnPoint = true;
                        if (IdOfLocateOnSpawnPoint != l.id)
                        {
                            IdOfLocateOnSpawnPoint = l.id;
                            squadReachedOnSpawnPoint?.Invoke(l, this);
                        }
                        break;
                    }
                    var isOnNearPosition = distToSpawnPoint < parameter.NearDistanceOfLocatedOnSpawnPoint;
                    if (isOnNearPosition != IsNearToSpawnPoint && (DateTime.Now - previousGetNearDateTime).TotalSeconds > getNearIntervalTime)
                    {
                        previousGetNearDateTime = DateTime.Now;
                        if (isOnNearPosition && !IsNearToSpawnPoint)
                        {
                            // 近づいていった場合
                            squadGetsNearToSpawnPoint?.Invoke(l, this, true);


                        }
                        else if (!isOnNearPosition && IsNearToSpawnPoint)
                        {
                            // 遠ざかっていった場合
                            squadGetsNearToSpawnPoint?.Invoke(l, this, false);
                        }
                        IsNearToSpawnPoint = isOnNearPosition;
                    }

                }
                if (!isOnSpawnPoint)
                {
                    IdOfLocateOnSpawnPoint = "";
                }
                moveSequence.timeScale = gameManager.Speed;
                UpdateCanvasImagePosition();

                yield return null;
            }

            // SquadReachedOnSpawnPointが呼ばれる前に移動が終了した場合
            if (IdOfLocateOnSpawnPoint != moveTo.id)
            {
                squadReachedOnSpawnPoint?.Invoke(moveTo, this);
            }

            SquadState = State.Waiting;
            onCompleteMoveSequence = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Squadの移動アニメーションを中止する
        /// </summary>
        internal void CancelMoveAnimation()
        {
            if (SquadState == State.Waiting) return;
            moveSequence.Kill();
            onCompleteMoveSequence?.Invoke();
        }

        /// <summary>
        /// checkPointsを移動した際の時間 MapObjectのScaleが1のときの時間を返す
        /// </summary>
        /// <param name="checkPoints"></param>
        /// <returns>実際の移動にかかる現実の秒数</returns>
        internal float CalcTime(List<Vector3> checkPoints)
        {
            var totalTime = 0f;
            for (var i = 1; i < checkPoints.Count; i++)
            {
                var cp0 = checkPoints[i - 1];
                var cp1 = checkPoints[i];
                float distToCP = Vector3.Distance(cp0, cp1) / mapObject.transform.localScale.x;
                totalTime += distToCP / Speed;
            }
            return totalTime;
        }

        /// <summary>
        /// 移動ログを保存する用のclass (Debug用)
        /// </summary>
        [Serializable]
        class MoveLog
        {
            public List<SerializableVector3> data = new List<SerializableVector3>();
        }

        /// <summary>
        /// Squadをアニメーションなしに移動させる 現在の移動Sequenceがある場合はCancelされる
        /// </summary>
        /// <param name="position"></param>
        internal void MoveToWithoutAnimation(Vector3 position)
        {
            if (moveSequence != null && ( moveSequence.IsActive() || moveSequence.IsPlaying()))
                moveSequence.Kill();
            transform.position = position;
            UpdateCanvasImagePosition();
        }


        public override string ToString()
        {
            return $"Squad: {data.name}, locate({(data.LocationID.Length != 0 ? data.LocationID : data.RoadID)}, supply({data.supplyLevel})";
        }


        /// <summary>
        /// SquadのMap上での行動状況
        /// </summary>
        public enum State
        {
            /// <summary>
            /// 歩行中
            /// </summary>
            Waiting,
            /// <summary>
            /// 待機中
            /// </summary>
            Walking,
            /// <summary>
            /// 補給を実行中
            /// </summary>
            Supplying,
            /// <summary>
            /// 補給を受け取り中
            /// </summary>
            ReceavingSupplies
        }
    }
}