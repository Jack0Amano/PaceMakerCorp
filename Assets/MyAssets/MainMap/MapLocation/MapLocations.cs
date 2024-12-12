using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using static Utility;
using MainMap.Roads;
using System.Linq;
using DG.Tweening;

namespace MainMap
{
    public class MapLocations : MonoBehaviour
    {

        [SerializeField] internal List<MapLocation> locations = new List<MapLocation>();

        [SerializeField] internal Roads.MapRoads mapRoads;

        [Header("敵のスポーン表示用のColor")]
        [Tooltip("敵レベル1の場合のColor")]
        [SerializeField] Color WarningColor01;
        [Tooltip("敵レベル2の場合のColor")]
        [SerializeField] Color WarningColor02;
        [Tooltip("敵レベル3の場合のColor")]
        [SerializeField] Color WarningColor03;
        [Tooltip("敵が存在しない場合のColor")]
        [SerializeField] Color SafeColor;
        [Tooltip("色変化のアニメーション")]
        [SerializeField] AnimationCurve DetectSpawningAnimationCurve;

        private MapLocation _selectedLocation;

        LineRenderer LineRenderer;

        internal MapLocation selectedLocation
        {
            get => _selectedLocation;

            set
            {
                if (_selectedLocation != null)
                    _selectedLocation.SelectLocation();

                if (value != null)
                    value.DeSelectLocation();

                _selectedLocation = value;
            }
        }

        /// <summary>
        /// 通行止めの交差点
        /// </summary>
        internal List<MapLocation> exceptCrossing = new List<MapLocation>();

        /// <summary>
        /// 軌跡を現在描写中であるか
        /// </summary>
        bool IsDrawingTrajectory = false;
        /// <summary>
        /// 軌跡描写のトリガー
        /// </summary>
        readonly WaitForSecondsExtensions.Trigger TrajectoryAnimationTrigger = new WaitForSecondsExtensions.Trigger();
        /// <summary>
        /// 軌跡が現在描写中である
        /// </summary>
        public bool IsTrajectoryActive { get => LineRenderer.isVisible; }

        private List<Transform> TrajectoryPositions;

        GeneralParameter parameter;

        protected private void Awake()
        {
            if (mapRoads.MapLocations == null)
                mapRoads.MapLocations = this;

            LineRenderer = GetComponent<LineRenderer>();

            GameManager.Instance.EventSceneController.LocationEventRequest += CalledFromEvent;
            parameter = GameManager.Instance.GeneralParameter;

            locations.ForEach(l =>
            {
                l.SafeColor = SafeColor;
                l.WarningColor01 = WarningColor01;
                l.WarningColor02 = WarningColor02;
                l.WarningColor03 = WarningColor03;
                l.DetectSpawningAnimationCurve = DetectSpawningAnimationCurve;
            });
        }

        /// <summary>
        /// IDからMapLocationを検索する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MapLocation GetLocationFromID(string id)
        {
            var loc = locations.Find(a => a.id == id);
            return loc;
        }

        /// <summary>
        /// IDからMapLocationを検索してそのTransformを返す
        /// </summary>
        /// <param name="mapSquads"></param>
        /// <param name="id"></param>
        /// <param name="isOnTurnout">退避エリアにいるか</param>
        /// <returns></returns>
        public Transform GetLocationFromID(MapSquads mapSquads, string id, bool isOnTurnout)
        {
            var loc = GetLocationFromID(id);
            if (loc == null)
                PrintError($"{id} of MapLocation ID is missing");
            if (!isOnTurnout)
                return loc.transform;
            var samePositionSquads = mapSquads.Squads.FindAll(s => s.Location == loc);
            if (samePositionSquads.Count == 0)
                return loc.TurnoutTransforms.First();
            samePositionSquads.RemoveAll(s => !s.IsOnTurnout);
            if (!loc.TurnoutTransforms.IndexAt_Bug(samePositionSquads.Count-1, out var output))
                PrintError($"Tryed to put {samePositionSquads.Count} squds on same location," +
                    $" but turnout position are not enough.");
            return output;
        }

        /// <summary>
        /// DataControllerの中身をロードしてMapLocationsに反映させる
        /// </summary>
        internal IEnumerator LoadData()
        {
            var saveDataInfo = GameManager.Instance.DataSavingController.SaveDataInfo;
            if (saveDataInfo.locationParameters == null)
            {
                PrintWarning($"Data of locationParameters is null in SaveDataInfo. Set default parameters");
                saveDataInfo.locationParameters = new List<LocationParamter>();
                locations.ForEach(l =>
                {
                    var loc = new LocationParamter() { id = l.id };
                    l.Data = loc;
                    saveDataInfo.locationParameters.Add(loc);
                });
            }
            else
            {
                foreach(var l in locations)
                {
                    if (l.Data != null)
                        continue;
                    var loc = saveDataInfo.locationParameters.Find(p => p.id == l.id);
                    if (loc == null)
                    {
                        PrintWarning($"Data of ({l.id}) is null in SaveDataInfo.locationParameters. Set default parameters");
                        loc = new LocationParamter() { id = l.id };
                        saveDataInfo.locationParameters.Add(loc);
                        l.Data = loc;
                    }
                    else
                    {
                        l.Data = loc;
                    }
                }
            }
            yield break;
        }

        internal void GetWay(MapSquad squad, Vector3 to)
        {
            // TODO squadがどのRoad上に存在しているか、もしくはLocationにいるか判断
            
            // mapRoads.GetBetterRoute()
        }

        /// <summary>
        /// Squadの位置するRoadかLocationのIDを取得する
        /// </summary>
        /// <param name="mapSquad"></param>
        internal string GetRoadOrLocationID(MapSquad mapSquad)
        {
            var locate = locations.Find(l => Vector3.Distance(mapSquad.transform.position, l.transform.position) < parameter.DistanceOfLocatedOnLocation);
            if (locate != null)
            {
                return locate.id;
            }

            const float MaxDistanceToRoadCheckPoint = 3;
            foreach(var road in mapRoads.roads)
            {
                var nearCP = road.checkPoints.FindMin(p => Vector3.Distance(p.position, mapSquad.transform.position));
                if (Vector3.Distance(nearCP.position, mapSquad.transform.position) > MaxDistanceToRoadCheckPoint)
                    continue;

                if (road.TargetOnRoad(mapSquad.transform))
                {
                    // mapSquadがRoad上にいる
                    return road.id;
                }
            }

            PrintWarning($"{mapSquad} is not on any location and roads");
            return "";
        }

        /// <summary>
        /// SquadがMapLocationの上にいるか　いない場合なnull
        /// </summary>
        /// <param name="mapSquad"></param>
        /// <returns></returns>
        internal MapLocation IsSquadOnLocation(MapSquad mapSquad)
        {
            var locate = locations.Find(l => 
            { 
                if (!mapSquad.IsOnTurnout)
                    return Vector3.Distance(mapSquad.transform.localPosition, l.transform.localPosition) < parameter.DistanceOfLocatedOnLocation;
                else
                    return Vector3.Distance(mapSquad.transform.localPosition, l.transform.localPosition) < parameter.NearDistanceOfLocatedOnLocation;
            });

            return locate;
        }

        /// <summary>
        /// SquadがRoadの上にいるか いない場合null
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        internal Roads.MapRoad IsTargetOnRoad(Transform target)
        {
            var test = new List<(MainMap.Roads.MapRoad road, float distance)>();
            const float MaxDistanceToRoadCheckPoint = 0.25f;
            foreach (var road in mapRoads.roads)
            {
                var nearCP = road.checkPoints.FindMin(p => Vector3.Distance(p.position, target.position));
                if (Vector3.Distance(nearCP.position, target.position) > MaxDistanceToRoadCheckPoint)
                    continue;
                test.Add((road, Vector3.Distance(nearCP.position, target.position)));

                if (road.TargetOnRoad(target))
                {
                    // mapSquadがRoad上にいる
                    return road;
                }
            }
            return null;
        }

        Vector3 BasePoint;
        private void OnDrawGizmos()
        {
            if (BasePoint != null && BasePoint != Vector3.zero)
                Gizmos.DrawCube(BasePoint, new Vector3(0.005f, 0.005f, 0.005f));
        }

        /// <summary>
        /// 軌跡を描写する
        /// </summary>
        /// <param name="mapSquad"></param>
        /// <param name="mapLocation"></param>
        internal IEnumerator DrawTrajectory(MapSquad mapSquad, MapLocation mapLocation)
        {

            if (IsDrawingTrajectory)
            {
                TrajectoryAnimationTrigger.cancel = true;
                var start = DateTime.Now;
                while (TrajectoryAnimationTrigger.cancel && (DateTime.Now - start).Milliseconds < 1500)
                    yield return null;
            }

            LineRenderer.positionCount = 0;


            if (mapSquad.IsOnLocation)
            {
                IsDrawingTrajectory = true;
                // MapSquadがLocation上にいる場合は おそらく停止状態
                TrajectoryPositions = mapRoads.GetBetterCheckPointsOfRoute(mapSquad.Location, mapLocation);
                var checkpoints = TrajectoryPositions.ConvertAll(t => t.position);
                LineRenderer.positionCount = checkpoints.Count;
                LineRenderer.endWidth = LineRenderer.startWidth = 0.01f;

                for (var i = 0; i < checkpoints.Count; i++)
                {
                    yield return WaitForSecondsExtensions.WaitForSecondsStopable(0.05f, TrajectoryAnimationTrigger);
                    if (TrajectoryAnimationTrigger.cancel) break;
                    LineRenderer.positionCount = i + 1;
                    LineRenderer.SetPositions(checkpoints.Slice(0, i).ToArray());
                }

                TrajectoryAnimationTrigger.cancel = false;
            }


            IsDrawingTrajectory = false;
        }

        /// <summary>
        /// 軌跡を消す
        /// </summary>
        /// <returns></returns>
        internal IEnumerator HideTrajectory(bool reverse = false)
        {
            if (!LineRenderer.isVisible) yield break ;

            if (IsDrawingTrajectory)
            {
                TrajectoryAnimationTrigger.cancel = true;
                var start = DateTime.Now;
                while (TrajectoryAnimationTrigger.cancel && (DateTime.Now - start).Milliseconds < 1500)
                    yield return null;
            }

            if (reverse)
                LineRenderer.SetPositions(TrajectoryPositions.ConvertAll(t => t.position).Reversed().ToArray());

            IsDrawingTrajectory = false;
            for(var i=LineRenderer.positionCount-1 ; 0<=i; i--)
            {
                yield return null;
                if (TrajectoryAnimationTrigger.cancel) break;
                LineRenderer.positionCount = i;
            }
            LineRenderer.positionCount = 0;
            TrajectoryAnimationTrigger.cancel = false;
        }

        /// <summary>
        /// アニメーション無しで軌跡を消す
        /// </summary>
        internal void HideTrajectoryWithoutAnimation()
        {
            IsDrawingTrajectory = false;
            TrajectoryAnimationTrigger.cancel = false;
            LineRenderer.positionCount = 0;
        }

        /// <summary>
        /// 軌跡の位置を更新する
        /// </summary>
        internal void UpdateTrajectory()
        {
            LineRenderer.SetPositions(TrajectoryPositions.ConvertAll(t=>t.position).ToArray());
        }

        /// <summary>
        /// EventGraphからLocation関連のEventを受け取った際の呼び出し
        /// </summary>
        /// <param name="locationEventOutput"></param>
        private void CalledFromEvent(EventGraph.InOut.LocationEventOutput e)
        {
            if (e.HourToRecoverPower != float.MinValue)
            {
                // 特定地点のHourToRecoverPowerの値を変更する
                if (locations.TryFindFirst(l => l.id == e.LocationID, out var loc))
                {
                    loc.Data.HourToRecoverPower = e.HourToRecoverPower;
                }
            }
        }
    }
}