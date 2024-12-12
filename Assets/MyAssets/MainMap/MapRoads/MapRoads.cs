using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using static Utility;

namespace MainMap.Roads
{
    /// <summary>
    /// 交差点を含まない一本の道のリスト
    /// </summary>
    public class MapRoads : MonoBehaviour
    {
        [SerializeField] internal List<MapRoad> roads;

        readonly Vector3 smallPoint = new Vector3(0.005f, 0.005f, 0.005f);
        readonly Vector3 bigPoint = new Vector3(0.008f, 0.008f, 0.008f);
        [SerializeField] private GUIStyle passGuiStyle;

        internal MapLocations MapLocations;

        /// <summary>
        /// 以前検索した経路のBuffer
        /// </summary>
        private List<CheckPointsOfRouteBuffer> checkPointsOfRouteBuffers = new List<CheckPointsOfRouteBuffer>();

        protected void Awake()
        {
        }

        private void Start()
        {
            
        }

        private void Update()
        {
            
        }

        private void OnDrawGizmos()
        {

            var index = 0;
            foreach (var w in roads)
            {
                if (w == null)
                    continue;
                if (w.cross01 == null || w.cross02 == null)
                    continue;

                index++;
                Gizmos.color = w.color;
                var count = 0;
                Transform old = null;
                int centerIndex = w.transform.childCount / 2;

                foreach (Transform pass in w.transform)
                {
                    if (!pass.name.Contains("pass"))
                        continue;

                    if (old != null)
                    {
                        Gizmos.DrawLine(old.position, pass.position);
                        if (count == 1)
                        {
                            var near = new List<MapLocation> { w.cross01, w.cross02 }.FindMin(c => Vector3.Distance(c.Position, old.position));
                            Gizmos.DrawLine(near.Position, old.position);
                            //Gizmos.DrawWireCube(old.position, bigPoint);
                        }
                        else
                        {
                            //Gizmos.DrawWireCube(old.position, smallPoint);
                        }

                        if (count-1 == centerIndex)
                            Handles.Label(old.position, w.id, passGuiStyle);
                    }

                    old = pass;
                    count++;
                }

                if (old != null)
                {
                    var near = new List<MapLocation> { w.cross01, w.cross02 }.FindMin(c => Vector3.Distance(c.Position, old.position));
                    Gizmos.DrawLine(near.Position, old.position);
                    //Gizmos.DrawWireCube(old.position, bigPoint);

                    if (centerIndex == 1)
                        Handles.Label(old.position, w.id, passGuiStyle);
                }

                //if (w.dots != null)
                //{
                //    w.dots.ForEach(d =>
                //    {
                //        Gizmos.DrawCube(d, smallPoint / 6);
                //    });
                //}
            }

            
        }

        /// <summary>
        /// IDからmapRoadを取得
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal MapRoad GetRoadFromID(string id)
        {
            if (id == null || id.Length == 0)
                return null;
            return roads.Find(r => r.id == id);
        }

        #region Sub functions

        /// <summary>
        /// <c>startCross</c>から<c>endCross</c>に行く最小コストの道順を検索する
        /// </summary>
        /// <param name="startCross">出発点となる交差点</param>
        /// <param name="endCross">到着位置となる交差点</param>
        private List<List<MapRoad>> GetBetterWaysBetweenCrosses(MapLocation startCross, MapLocation endCross, List<MapRoad> exceptRoads)
        {
            //Print("GetBetterWaysBetweenCrosses:", "Start.", startCross, ",End.", endCross);

            // crossにつながるRoadとそれに接続する交差点のリストを取得
            // fromRoadはいま来た道 nullの場合return から除外される
            List<(MapLocation cross01, MapRoad road, MapLocation cross02)> SearchRoute(MapRoad fromRoad, MapLocation cross)
            {
                // crossに接続しているroads
                var connectedRoads = roads.FindAll(r => r.IsConnected(cross));
                if (fromRoad != null)
                    connectedRoads.Remove(fromRoad);
                return connectedRoads.ConvertAll(r =>
                {
                    return (cross, r, r.GetOppositeCross(cross));
                });
            }

            if (startCross == endCross)
            {
                return new List<List<MapRoad>>();
            }

            var reachWays = new List<List<(MapLocation cross01, MapRoad road, MapLocation cross02)>>();
            var ways = new List<List<(MapLocation cross01, MapRoad road, MapLocation cross02)>>();

            // 一段目
            var firstStep = SearchRoute(null, startCross);
            firstStep.ForEach(s =>
            {
                if (!exceptRoads.Contains(s.road))
                    ways.Add(new List<(MapLocation cross01, MapRoad road, MapLocation cross02)> { s });
            });

            // n段目
            const int SearchRouteLimit = 10;
            var count = 0;
            while (true)
            {
                var newWays = new List<List<(MapLocation cross01, MapRoad road, MapLocation cross02)>>();
                foreach (var w in ways)
                {
                  
                    var last = w.Last();
                    // 既に到着している
                    if (last.cross02 == endCross)
                    {
                        var newWay = new List<(MapLocation, MapRoad, MapLocation)>(w);
                        reachWays.Add(newWay);
                        continue;
                    }
                    var roadsFromCross02 = SearchRoute(last.road, last.cross02);

                    // roadsFromCross02が空の場合はlast.cross02を出発してlast.roadを通らない交差点に行く道が見つからないということ
                    if (roadsFromCross02.Count == 0)
                        continue;

                    foreach (var r in roadsFromCross02)
                    {
                        // 除外するRoadを通る場合、このルートは除外
                        if (exceptRoads.Contains(r.road))
                            continue;
                        // もしすでに通ったことのある交差点がroadsFromCross02のcross02になった場合このルートは除外
                        if (w.Exists(previousW => previousW.cross01 == r.cross02 || previousW.cross02 == r.cross02))
                            continue;
                        // stopCross（通行不能交差点）がある場合は候補から逃す
                        if (MapLocations.exceptCrossing.Exists(c => c == r.cross02))
                            continue;

                        var newWay = new List<(MapLocation cross01, MapRoad road, MapLocation cross02)>(w);
                        newWay.Add(r);

                        // endCrossに到達
                        if (r.cross02 == endCross)
                            reachWays.Add(newWay);
                        else
                            newWays.Add(newWay);
                    }
                }

                if (newWays.Count == 0 || count == SearchRouteLimit)
                    break;

                count++;

                ways = newWays;
            }

            //float CalcScore(List<(MapLocation cross01, MapRoad road, MapLocation cross02)> route)
            //{
            //    var cost = route.Sum(r => r.road.CostValue);
            //    return cost;
            //}

            //Print("GetBetterWaysBetweenCrosses: to", endCross, ", from", startCross);
            //foreach (var w in reachWays)
            //{
            //    var score = CalcScore(w);
            //    Print(score, string.Join(",", w.ConvertAll(t => t.road)));
            //}

            //var shortestWay = reachWays.FindMin(w => CalcScore(w));
            //var shortestRoads = shortestWay.ConvertAll(r => r.road);
            //Print(string.Join(",", shortestRoads));

            //var text = $"{count} tick\n";
            //foreach (var reach in reachWays)
            //{
            //    var roads = reach.ConvertAll(r => r.road);
            //    text += string.Join(",", roads) + "\n";
            //}
            //Print(text);

            return reachWays.ConvertAll(w => w.ConvertAll(r => r.road));
        }

        /// <summary>
        /// 与えられたルートを通るためのコスト
        /// </summary>
        /// <param name="routes"></param>
        /// <returns></returns>
        private float CalcCost(List<MapRoad> routes)
        {
            return routes.Sum(r => r.CostValue);
        }

        /// <summary>
        /// 与えられたPointがどのRoad上に存在する可能性が高いか
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        internal MapRoad GetRoadFromPosition(Vector3 position)
        {
            
            return roads.FindMin(r =>
            {
                var nearCheckPoint = r.checkPoints.FindMin(p => Vector3.Distance(p.position, position));
                return Vector3.Distance(nearCheckPoint.position, position);
            });
        }
        #endregion

        #region ルート検索

        /// <summary>
        /// <c>start</c>ルート上の<c>startPosition</c>から<c>goal</c>ルート上の<c>goalPosition</c>に行くルートを検索
        /// </summary>
        internal List<MapRoad> GetBetterRoute(MapRoad start, Vector3 startPosition, MapRoad goal, Vector3 goalPosition)
        {
            var costOnStartRoad = start.GetPositionOnRoad(startPosition);
            var cost_sCross01 = costOnStartRoad[start.cross01] * start.CostValue;
            var cost_sCross02 = costOnStartRoad[start.cross02] * start.CostValue;

            var costOnGoalRoad = goal.GetPositionOnRoad(goalPosition);
            var cost_gCross01 = costOnGoalRoad[goal.cross01] * start.CostValue;
            var cost_gCross02 = costOnGoalRoad[goal.cross02] * start.CostValue;

            (List<MapRoad> ways,float cost) GetWaysAndScore(MapLocation startCross, 
                                                            MapLocation goalCross, 
                                                            float costFromStartCross, 
                                                            float costToGoalCross)
            {
                var ways = GetBetterWaysBetweenCrosses(startCross, goalCross, new List<MapRoad>());
                var shortWay = new List<MapRoad>();
                if (ways.Count != 0)
                {
                    shortWay = ways.FindMin(w =>
                    {
                        if (w.Last() == goal)
                            return float.MaxValue;
                        return CalcCost(w);
                    });
                    var cost = CalcCost(shortWay) + costFromStartCross + costToGoalCross;
                    return (shortWay, cost);
                }
                else
                    return (shortWay, costFromStartCross + costToGoalCross);
            }

            // Print("-> GetBetterRoute:", start, startPosition, "|", goal, goalPosition);
            // Startのcross01を通りGoalのcross01を通る最短ルート
            var (sCross01_gCross01, cost_sCross01_gCross01) = GetWaysAndScore(start.cross01, goal.cross01, cost_sCross01, cost_gCross01);

            // Startのcross02を通りGoalのcross01を通る最短ルート
            var (sCross02_gCross01, cost_sCross02_gCross01) = GetWaysAndScore(start.cross02, goal.cross01, cost_sCross02, cost_gCross01);

            // Startのcross02を通りGoalのcross02を通る最短ルート
            var (sCross02_gCross02, cost_sCross02_gCross02) = GetWaysAndScore(start.cross02, goal.cross02, cost_sCross02, cost_gCross02);

            // Startのcross01を通りGoalのcross02を通る最短ルート
            var (sCross01_gCross02, cost_sCross01_gCross02) = GetWaysAndScore(start.cross01, goal.cross02, cost_sCross01, cost_gCross02);

            var betterRoute = new List<(List<MapRoad> roads, float cost)>
            {
                (sCross01_gCross01, cost_sCross01_gCross01 ),
                (sCross02_gCross01, cost_sCross02_gCross01),
                (sCross02_gCross02, cost_sCross02_gCross02),
                (sCross01_gCross02, cost_sCross01_gCross02)
            }.FindMin(r => r.cost);

            // Print("GetBetterRoute <-");

            return betterRoute.roads;
        }

        /// <summary>
        /// <c>start</c>ルート上の<c>startPosition</c>から<c>goal</c>の交差点に行くルートを検索
        /// </summary>
        internal List<MapRoad> GetBetterRoute(MapRoad start, Vector3 startPosition, MapLocation goal)
        {

            var costOnStartRoad = start.GetPositionOnRoad(startPosition);
            var cost_sCross01 = costOnStartRoad[start.cross01] * start.CostValue;
            var cost_sCross02 = costOnStartRoad[start.cross02] * start.CostValue;

            var exceptRoads = new List<MapRoad> { start };

            var sCross01_gCross = new List<MapRoad>();
            var cost_sCross01_gCross = float.MaxValue;
            var _sCross01_gCross = GetBetterWaysBetweenCrosses(start.cross01, goal, exceptRoads);
            if (_sCross01_gCross.Count != 0)
            {
                sCross01_gCross = _sCross01_gCross.FindMin(w =>
                {
                    return CalcCost(w);
                });
                cost_sCross01_gCross = CalcCost(sCross01_gCross) + cost_sCross01;
            }
            //Print("Ways", start.cross01, "to", goal, "|", string.Join(",", sCross01_gCross));

            var _sCross02_gCross = GetBetterWaysBetweenCrosses(start.cross02, goal, exceptRoads);
            List<MapRoad> sCross02_gCross = new List<MapRoad>();
            var cost_sCross02_gCross = float.MaxValue;
            if (_sCross02_gCross.Count != 0)
            {
                sCross02_gCross = _sCross02_gCross.FindMin(w =>
                {
                    return CalcCost(w);
                });
                cost_sCross02_gCross = CalcCost(sCross02_gCross) + cost_sCross02;
            }
            //Print("Ways", start.cross02, "to", goal, "|", string.Join(",", sCross02_gCross));

            if (cost_sCross01_gCross > cost_sCross02_gCross)
                return sCross02_gCross;
            else
                return sCross01_gCross;
        }

        /// <summary>
        /// <c>start</c>の交差点から<c>goal</c>ルート上の<c>goalPosition</c>に行くルートを検索
        /// </summary>
        internal List<MapRoad> GetBetterRoute(MapLocation start, MapRoad goal, Vector3 goalPosition)
        {
            var connectedRoads = roads.FindAll(r => r.cross01 == start || r.cross02 == start);

            var costOnGoalRoad = goal.GetPositionOnRoad(goalPosition);
            var cost_Pos_gCross01 = costOnGoalRoad[goal.cross01] * goal.CostValue;
            var cost_Pos_gCross02 = costOnGoalRoad[goal.cross02] * goal.CostValue;

            //Print(string.Join(",", costOnGoalRoad.ToList()));
            //Print(start, goal.cross01, goal.cross02);

            var sCross_gCross01 = GetBetterWaysBetweenCrosses(start, goal.cross01, new List<MapRoad>()).FindMin(w =>
            {
                if (w.Last() == goal)
                    return float.MaxValue;
                return CalcCost(w);
            }); 
            var sCross_gCross02 = GetBetterWaysBetweenCrosses(start, goal.cross02, new List<MapRoad>()).FindMin(w =>
            {
                if (w.Last() == goal)
                    return float.MaxValue;
                return CalcCost(w);
            }); 

            //Print(string.Join(",", sCross_gCross01));
            //Print(string.Join(",", sCross_gCross02));

            // cost_Pos_g_Cross02はGoalのCross02経由で
            var cost_sCross_gCross01 = CalcCost(sCross_gCross01) + cost_Pos_gCross01;
            // Print($"{string.Join(",",sCross_gCross01)}\nThroughMapLocation:{goal.cross01}\ncost:{cost_sCross_gCross01}, {goal.cross01}:{cost_Pos_gCross01}");
            var cost_sCross_gCross02 = CalcCost(sCross_gCross02) + cost_Pos_gCross02;
            // Print($"{string.Join(",", sCross_gCross02)}\nThroughMapLocation:{goal.cross01}\ncost:{cost_sCross_gCross02}, {goal.cross02}:{cost_Pos_gCross02}");

            if (cost_sCross_gCross01 < cost_sCross_gCross02)
                return sCross_gCross01;
            else
                return sCross_gCross02;
        }

        /// <summary>
        /// <c>start</c>の交差点から<c>goal</c>の交差点に行くルートを検索
        /// </summary>
        internal List<MapRoad> GetBetterRoute(MapLocation start, MapLocation goal)
        {
            var routes = GetBetterWaysBetweenCrosses(start, goal, new List<MapRoad>());
            if (routes.Count == 0)
            {
                PrintError($"Failed to search route from {start} to {goal}");
                return new List<MapRoad>();
            }
            return routes.FindMin(w =>
            {
                if (w.Last() == goal) 
                    return float.MaxValue;
                return CalcCost(w);
            });
        }

        /// <summary>
        /// StartからGoalのMapLocationに行くためのCheckPointsをListの形で返す
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        internal List<Transform> GetBetterCheckPointsOfRoute(MapLocation start, MapLocation goal)
        {
            var cp = checkPointsOfRouteBuffers.Find(b => b.start == start && b.goal == goal);
            if (!cp.Equals(default(CheckPointsOfRouteBuffer)))
                return cp.checkpoints;

            var routeCheckPoints = new List<Transform>() {start.transform };
            // MapSquadがLocation上にいる場合は おそらく停止状態
            var roads = GetBetterRoute(start, goal);
            roads.ForEach(r =>
            {
                List<Transform> checkpoints = new List<Transform>();
                if (r.checkPoints.First() == routeCheckPoints.Last())
                    checkpoints = r.checkPoints.Slice(1);
                else if (r.checkPoints.Last() == routeCheckPoints.Last())
                    checkpoints = r.checkPoints.Reversed().Slice(1);
                routeCheckPoints.AddRange(checkpoints);
            });
            routeCheckPoints.Add(goal.transform);
            checkPointsOfRouteBuffers.Add(new CheckPointsOfRouteBuffer(start, goal, routeCheckPoints));

            return routeCheckPoints;
        }

        /// <summary>
        /// 以前検索したRouteを保存しておくもの
        /// </summary>
        struct CheckPointsOfRouteBuffer
        {
            public MapLocation start;
            public MapLocation goal;
            public List<Transform> checkpoints;

            public CheckPointsOfRouteBuffer(MapLocation start, MapLocation goal, List<Transform> checkpoints)
            {
                this.start = start;
                this.goal = goal;
                this.checkpoints = checkpoints;
            }
        }

        #endregion
    }
}