using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Utility;

namespace MainMap.Roads
{
    class MapRoad : MonoBehaviour
    {
        [SerializeField] internal string id;
        [SerializeField] Transform spawnLocationParent;
        [SerializeField] internal MapLocation cross01;
        [SerializeField] internal MapLocation cross02;
        [SerializeField] internal RoadCost cost = RoadCost.Normal;
        [SerializeField] internal Color color = Color.green;

        /// <summary>
        /// Roadの曲がるpoints
        /// </summary>
        [NonSerialized] internal readonly List<Transform> checkPoints = new List<Transform>();

        // ! UnitのRoad上の中途半端な位置での停止機能を廃止してLocation上のみにしたため使っていない
        /// <summary>
        /// Road上の微細距離 
        /// </summary>
        [NonSerialized] internal readonly List<Transform> dots = new List<Transform>();

        const float DistanceBetweenDot = 0.001f;

        /// <summary>
        /// CheckPoint01と02の距離 (距離が変わったということはScaleが変更されTotalDistanceの再計算が必要だということ)
        /// </summary>
        private float baseDistance = 0;

        /// <summary>
        /// Roadを通るために必要なコスト (通りやすい道(征服下の街の間の移動))
        /// </summary>
        internal float CostValue
        {
            get
            {
                if (cost == RoadCost.Low)
                    return TotalDistance;
                else if (cost == RoadCost.High)
                    return TotalDistance * 2.3f;
                else
                    return TotalDistance * 1.5f;
            }
        }

        #region Init
        private void Awake()
        {

            if (checkPoints.Count == 0)
            {
                var count = 0;
                Transform old = null;
                foreach (Transform pass in transform)
                {
                    if (!pass.gameObject.name.Contains("pass"))
                        continue;

                    if (old != null)
                    {
                        //Gizmos.DrawLine(old.position, old.position + (old.forward * 0.3f));
                        if (count == 1)
                        {
                            var near = new List<Transform> { cross01.transform, cross02.transform }.FindMin(c => Vector3.Distance(c.position, old.position));
                            checkPoints.Add(near);
                            checkPoints.Add(old);
                        }
                        else
                        {
                            checkPoints.Add(old);
                        }
                    }

                    old = pass.GetComponent<Transform>();
                    count++;
                }

                if (old != null)
                {
                    checkPoints.Add(old);
                    var near = new List<Transform> { cross01.transform, cross02.transform }.FindMin(c => Vector3.Distance(c.position, old.position));
                    checkPoints.Add(near);
                }
            }

            // SetDots();
        }

        /// <summary>
        /// 滑らかな移動になるように道路上に微細距離のDotを設置
        /// </summary>
        private void SetDots()
        {
            if (dots.Count != 0) return;
            var xAxis = new Vector3(1, 0, 0);

            var count = 0;
            for (var i = 1; i < checkPoints.Count; i++)
            {
                var cp0 = checkPoints[i - 1];
                var cp1 = checkPoints[i];

                var vectorCP0_1 = cp1.position - cp0.position;
                var angle = Vector3.Angle(xAxis, vectorCP0_1) * Mathf.Deg2Rad;
                if (cp0.position.z > cp1.position.z)
                    angle = 360 * Mathf.Deg2Rad - angle;

                var distCP0_1 = Vector3.Distance(cp0.position, cp1.position);
                int dotsCount = (int)(distCP0_1 / DistanceBetweenDot);
                for (var j = 0; j < dotsCount; j++)
                {
                    var deltaZ = j * DistanceBetweenDot * MathF.Sin(angle);
                    var deltaX = j * DistanceBetweenDot * MathF.Cos(angle);
                    var position = cp0.position + new Vector3(deltaX, 0, deltaZ);
                    var dot = new GameObject();
                    dot.transform.parent = this.transform;
                    dot.name = $"{count}";
                    dot.transform.position = position;
                    dots.Add(dot.transform);
                    count++;
                }
            }
        }
        #endregion


        /// <summary>
        /// 指定した道がこの道にMapLocationで接続されているか
        /// </summary>
        /// <param name="road"></param>
        /// <returns></returns>
        internal bool IsConnected(MapRoad road)
        {
            if (road.cross01 == cross01)
                return true;
            if (road.cross02 == cross01)
                return true;
            if (road.cross01 == cross02)
                return true;
            if (road.cross02 == cross02)
                return true;
            return false;
        }

        /// <summary>
        /// Roadとthisを直接接続しているMapLocationがあればこれを返す
        /// </summary>
        /// <param name="road"></param>
        /// <returns></returns>
        internal MapLocation GetConnectedMapLocation(MapRoad road)
        {
            if (road.cross01 == cross01)
                return cross01;
            if (road.cross02 == cross01)
                return cross01;
            if (road.cross01 == cross02)
                return cross02;
            if (road.cross02 == cross02)
                return cross02;
            return null;
        }

        /// <summary>
        /// 与えられた交差点の反対側の交差点を取得する
        /// </summary>
        /// <param name="cross"></param>
        /// <returns></returns>
        internal MapLocation GetOppositeCross(MapLocation cross)
        {
            return cross01 == cross ? cross02 : cross01;
        }

        /// <summary>
        /// 与えられた交差点はroadに接しているものか
        /// </summary>
        /// <param name="cross"></param>
        /// <returns></returns>
        internal bool IsConnected(MapLocation cross)
        {
            return cross01 == cross || cross02 == cross;
        }

        /// <summary>
        /// Roadの距離 (Scaleによって変動する)
        /// </summary>
        internal float TotalDistance
        {
            get
            {
                var _baseDist = DistanceBetweenCheckPoint(checkPoints[0].position, checkPoints[1].position);
                
                if (_baseDist == baseDistance)
                    return _totalDist;

                baseDistance = _baseDist;
                var output = 0f;
                var oldPts = checkPoints.First();
                for (var i = 1; i < checkPoints.Count; i++)
                {
                    output += DistanceBetweenCheckPoint(oldPts.position, checkPoints[i].position);
                    oldPts = checkPoints[i];
                }
                _totalDist = output;
                return output;
            }
        }
        private float _totalDist = 0;

        /// <summary>
        /// Scaleに影響されないCheckPointの間隔を取得
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        internal float DistanceBetweenCheckPoint(Vector3 pt1, Vector3 pt2)
        {
            return Vector3.Distance(pt1, pt2) / MainMapController.MapScale;
        }

        /// <summary>
        /// road上の位置にあるpositionは両端の交差点からどの程度の割合進行しているか
        /// </summary>
        /// <param name="position">Road上の位置</param>
        /// <returns><c>key=crossのtransfrom</c>、<c>value=crossにどれだけ近いかの割合</c></returns>
        internal Dictionary<MapLocation, float> GetPositionOnRoad(Vector3 position)
        {
            var near = checkPoints.FindMin(p => DistanceBetweenCheckPoint(p.position, position));
            var nearIndex = checkPoints.IndexOf(near);

            var firstCheckPts = checkPoints.First().GetComponent<MapLocation>();
            var lastCheckPts = checkPoints.Last().GetComponent<MapLocation>();
            // Positionが交差路に十分近い場合
            if (nearIndex == 0)
            {
                var smallDist = DistanceBetweenCheckPoint(firstCheckPts.Position, position);
                return new Dictionary<MapLocation, float>
                {
                    { firstCheckPts, smallDist / TotalDistance } ,
                    {lastCheckPts, (TotalDistance - smallDist) / TotalDistance }
                };
            }
            else if (nearIndex == checkPoints.Count - 1)
            {
                var smallDist = DistanceBetweenCheckPoint(lastCheckPts.Position, position);
                return new Dictionary<MapLocation, float>
                {
                    { lastCheckPts, smallDist / TotalDistance } ,
                    {firstCheckPts, (TotalDistance - smallDist) / TotalDistance }
                };
            }

            var (distFromForwardCross, distFromBackwardCross) = DistanceFromCrossEdge(nearIndex);

            var distFromNear = DistanceBetweenCheckPoint(position, near.position);

            // forward-near > forward-posの場合 positionはnearIndexの前側にいる
            var nearForward = checkPoints[nearIndex - 1];
            var distForward2Near = DistanceBetweenCheckPoint(nearForward.position, near.position);
            var distForward2Position = DistanceBetweenCheckPoint(nearForward.position, position);

            if (distForward2Near > distForward2Position)
            {
                // positionはnearindexの前側（左側にいる)
                // <-(forward)-*-----x--o--------*---(backward)--
                return new Dictionary<MapLocation, float>
                {
                    {firstCheckPts, (distFromForwardCross - distFromNear) / TotalDistance },
                    {lastCheckPts, (distFromBackwardCross + distFromNear) / TotalDistance }
                };
            }
            else
            {
                // positionはnearIndexの後ろ側（右側にいる)
                // <-(forward)-*--------o---x----*---(backward)--
                return new Dictionary<MapLocation, float>
                {
                    {firstCheckPts, (distFromForwardCross + distFromNear) /TotalDistance },
                    {lastCheckPts, (distFromBackwardCross - distFromNear) / TotalDistance }
                };
            }
        }

        /// <summary>
        /// <c>pointIndex</c>の端からの最短距離を取得する
        /// </summary>
        /// <param name="pointIndex"></param>
        /// <returns></returns>
        private (float forwardFrom, float backFrom) DistanceFromCrossEdge(int pointIndex)
        {
            var distFromForwardCross = 0f;
            if (pointIndex > 0)
            {
                var oldPot = checkPoints[0];
                for (var i = 1; i <= pointIndex; i++)
                {
                    // 前方のCrossPointからの距離を求める
                    distFromForwardCross += DistanceBetweenCheckPoint(oldPot.position, checkPoints[i].position);
                    oldPot = checkPoints[i];
                }
            }

            var distFromBackCross = 0f;
            if (pointIndex != checkPoints.Count - 1)
            {
                var oldPot = checkPoints[checkPoints.Count - 1];
                for (var i = checkPoints.Count - 2; pointIndex <= i; i--)
                {
                    // 後ろのCrossPointからの距離を求める
                    distFromBackCross += DistanceBetweenCheckPoint(oldPot.position, checkPoints[i].position);
                    oldPot = checkPoints[i];
                }
            }

            return (distFromForwardCross, distFromBackCross);
        }

        /// <summary>
        /// MapLocationに向かうCheckPointのリストを道のり順に整形した形に返す
        /// </summary>
        /// <param name="from"></param>
        /// <returns>TKeyは向かうMapLocationのTransform, TValueは経由するTransform</returns>
        internal Dictionary<MapLocation, List<Transform>> GetCheckPoints(Transform from)
        {
            var output = new Dictionary<MapLocation, List<Transform>>();
            var nearIndex = checkPoints.IndexOf(checkPoints.FindMin((p) => Vector3.Distance(p.position, from.position)));
            if (nearIndex == 0)
            {
                var toStart = new List<Transform> { from, checkPoints[0] };
                output[GetMapLocation(toStart.Last())] = toStart;

                //Print("ToStart", string.Join(",", toStart));

                var toEnd = new List<Transform> { from };
                var _toEnd = checkPoints.Slice(1, checkPoints.LastIndex());
                toEnd.AddRange(_toEnd);
                output[GetMapLocation(toEnd.Last())] = toEnd;

                //Print("ToEnd", string.Join(",", toEnd));
            }
            else if (nearIndex == checkPoints.Count - 1)
            {
                var toStart = new List<Transform> { from };
                var _toStart = checkPoints.Slice(0, checkPoints.LastIndex() - 1).Reversed();
                toStart.AddRange(_toStart);
                output[GetMapLocation(toStart.Last())] = toStart;

                //Print("ToStart", string.Join(",", toStart));

                var toEnd = new List<Transform> { from, checkPoints.Last() };
                output[GetMapLocation(toEnd.Last())] = toEnd;

                //Print("ToEnd", string.Join(",", toEnd));
            }
            else
            {
                // AngleのnearVectorをBaseとするnearVector-fromとnearVector-nearVector0, nearVector-nearVector1の角度の小さい方が
                // near0 -- x --- near ----- near1　　の時angleAのほうが小さい
                // near0 ----- near -- x --- near1　　のときはAngleBのほうが小さい
                var nearVector = checkPoints[nearIndex].position;
                var nearVector0 = checkPoints[nearIndex - 1].position;
                var nearVector1 = checkPoints[nearIndex + 1].position;

                var a = from.position - nearVector;
                var _a = nearVector0 - nearVector;
                var angleA = Vector3.Angle(a, _a);

                var b = from.position - nearVector;
                var _b = nearVector1 - nearVector;
                var angleB = Vector3.Angle(b, _b);

                // Print($"AngleBase{nearIndex}, Angle0 {nearIndex-1}, Angle1 {nearIndex+1}, A0 {angleA}, B1 {angleB}");

                if (angleA < angleB)
                {
                    // - near0 -- x --- near ----- near1 -　の時
                    var toStart = new List<Transform> { from };
                    var _toStart = checkPoints.Slice(0, nearIndex - 1).Reversed();
                    toStart.AddRange(_toStart);
                    output[GetMapLocation(toStart.Last())] = toStart;

                    //Print("ToStart", string.Join(",", toStart));

                    var toEnd = new List<Transform> { from };
                    var _toEnd = checkPoints.Slice(nearIndex, checkPoints.LastIndex());
                    toEnd.AddRange(_toEnd);
                    output[GetMapLocation(toEnd.Last())] = toEnd;

                    //Print("ToEnd", string.Join(",", toEnd));
                }
                else
                {
                    // - near0 ----- near -- x --- near1 -　　のとき
                    var toStart = new List<Transform> { from };
                    var _toStart = checkPoints.Slice(0, nearIndex).Reversed();
                    toStart.AddRange(_toStart);
                    output[GetMapLocation(toStart.Last())] = toStart;

                    //Print("ToStart", string.Join(",", toStart));

                    var toEnd = new List<Transform> { from };
                    var _toEnd = checkPoints.Slice(nearIndex + 1, checkPoints.LastIndex());
                    toEnd.AddRange(_toEnd);
                    output[GetMapLocation(toEnd.Last())] = toEnd;

                    //Print("ToEnd", string.Join(",", toEnd));
                }
            }

            return output;
        }

        /// <summary>
        /// TargetのTransfromがRoadの上に存在しているか
        /// </summary>
        /// <param name="target"></param>
        internal bool _TargetOnRoad(Vector3 target)
        {
            // TODO CancelMOveしたときにTargetがroadの上にいるか判別できてない
            var nearIndex = checkPoints.IndexOf(checkPoints.FindMin((p) => Vector3.Distance(p.position, target)));
            const float MaxAngleOfTargetOnRoad = 10;
            if (nearIndex == 0)
            {
                // crossing -- x ---- near1 ----
                // ( near )
                // crossing をbaseとして crossing-xベクトルとcrossing-near1ベクトルの角度が十分小さい場合 x は crossing-near1の線上にある

                var vectorA = target - checkPoints[nearIndex].position;
                var vectorB = checkPoints[nearIndex + 1].position - checkPoints[nearIndex].position;
                var angleAB = Vector3.Angle(vectorA, vectorB);

                Print($"{this}\nTargetOnRoad:", target, angleAB, angleAB < MaxAngleOfTargetOnRoad);
                return angleAB < MaxAngleOfTargetOnRoad;
            }
            else if (nearIndex == checkPoints.Count - 1)
            {
                // --- near1 ----- x -- crossing
                //                      ( near )
                var vectorA = target - checkPoints[nearIndex].position;
                var vectorB = checkPoints[nearIndex - 1].position - checkPoints[nearIndex].position;
                var angleAB = Vector3.Angle(vectorA, vectorB);

                Print($"{this}\nTargetOnRoad:", target, angleAB, angleAB < MaxAngleOfTargetOnRoad);
                return angleAB < MaxAngleOfTargetOnRoad;
            }
            else
            {
                // AngleのnearVectorをBaseとするnearVector-fromとnearVector-nearVector0, nearVector-nearVector1の角度の小さい方が
                // near0 -- x --- near ----- near1　　の時angleAのほうが小さい
                // near0 ----- near -- x --- near1　　のときはAngleBのほうが小さい
                var nearVector = checkPoints[nearIndex].position;
                var nearVector0 = checkPoints[nearIndex - 1].position;
                var nearVector1 = checkPoints[nearIndex + 1].position;

                var a = target - nearVector;
                var _a = nearVector0 - nearVector;
                var angleA = Vector3.Angle(a, _a);

                var b = target - nearVector;
                var _b = nearVector1 - nearVector;
                var angleB = Vector3.Angle(b, _b);

                Print($"{this}\nAngleBaseIndex{nearIndex}, AngleIndex0 {nearIndex-1}, AngleIndex1 {nearIndex+1}, A0 {angleA}, B1 {angleB}\n" +
                    $"{angleA < MaxAngleOfTargetOnRoad || angleB < MaxAngleOfTargetOnRoad}");

                // angleAとangleB のどちらかが maxangleoftargetonraodより小さい場合 targetはnear0-near上もしくはnear-near1上に存在する
                return angleA < MaxAngleOfTargetOnRoad || angleB < MaxAngleOfTargetOnRoad;
            }
        }

        /// <summary>
        /// TargetがRoadの上に存在しているか
        /// </summary>
        /// <param name="target"></param>
        internal bool TargetOnRoad(Transform target)
        {
            const float MaxDistanceBetweenDotAndTarget = 0.01f;
            var nearCheckPoint = dots.FindMin(p => Vector3.Distance(p.position, target.position));
            var dist = Vector3.Distance(nearCheckPoint.position, target.position);
            return dist < MaxDistanceBetweenDotAndTarget;
        }

        /// <summary>
        /// <c>to</c>に向かうためのCheckPointを取得する
        /// </summary>
        /// <param name="to"></param>
        internal List<Transform> GetCheckPoints(Vector3 to, MapLocation from)
        {
            var nearIndex = checkPoints.IndexOf(checkPoints.FindMin((p) => Vector3.Distance(p.position, to)));
            if (nearIndex == 0)
            {
                // crossing -- x ---- near1 ----
                // ( near )
                if (checkPoints.First() == from.transform)
                    return new List<Transform>() { from.transform };
                else
                    return checkPoints.Reversed();
            }
            else if (nearIndex == checkPoints.Count - 1)
            {
                // --- near1 ----- x -- crossing
                //                      ( near )
                if (checkPoints.Last() == from.transform)
                    return new List<Transform> { from.transform };
                else
                    return new List<Transform>(checkPoints);
            }
            else
            {
                // AngleのnearVectorをBaseとするnearVector-fromとnearVector-nearVector0, nearVector-nearVector1の角度の小さい方が
                // near0 -- x --- near ----- near1　　の時angleAのほうが小さい
                // near0 ----- near -- x --- near1　　のときはAngleBのほうが小さい
                var nearVector = checkPoints[nearIndex].position;
                var nearVector0 = checkPoints[nearIndex - 1].position;
                var nearVector1 = checkPoints[nearIndex + 1].position;

                var a = to - nearVector;
                var _a = nearVector0 - nearVector;
                var angleA = Vector3.Angle(a, _a);

                var b = to - nearVector;
                var _b = nearVector1 - nearVector;
                var angleB = Vector3.Angle(b, _b);

                // Print($"AngleBase{nearIndex}, Angle0 {nearIndex-1}, Angle1 {nearIndex+1}, A0 {angleA}, B1 {angleB}");

                if (angleA < angleB)
                {
                    // - near0 ---- x -- near ----- near1 -　の時
                    if (checkPoints.First() == from.transform)
                    {
                        // cross -- ~~ -- near0 ---- x - near -- 
                        return checkPoints.Slice(0, nearIndex - 1);
                    }
                    else
                    {
                        // --- near0 ---- x - near -- ~~ -- cross
                        return checkPoints.Slice(nearIndex, checkPoints.LastIndex()).Reversed();
                    }

                }
                else
                {
                    // - near0 ----- near -- x --- near1 -　　のとき
                    if (checkPoints.First() == from.transform)
                    {
                        // cross -- ~~ -- near0 ----- near - x --- near1 -
                        return checkPoints.Slice(0, nearIndex);
                    }
                    else
                    {
                        // -- near0 ----- near - x --- near1 -- ~~ -- cross
                        return checkPoints.Slice(nearIndex + 1, checkPoints.LastIndex()).Reversed();
                    }
                }
            }
        }

        /// <summary>
        /// Route上のfromからtoに向かうためのCheckPointsを取得
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        internal List<Transform> GetCheckPoints(Vector3 from, Vector3 to)
        {
            //Print($"cross01: {cross01.name}, cross02: {cross02.name}");
            var checkPoints_From_Cross01 = GetCheckPoints(from, cross01);
            //Print("From-Cross01", string.Join("_", checkPoints_From_Cross01.ConvertAll(p => p.name)));
            var checkPoints_From_Cross02 = GetCheckPoints(from, cross02);
            //Print("From-Cross02", string.Join("_", checkPoints_From_Cross02.ConvertAll(p => p.name)));
            var checkPoints_To_Cross01 = GetCheckPoints(to, cross01);
            //Print("To-Cross01", string.Join("_", checkPoints_To_Cross01.ConvertAll(p => p.name)));
            var checkPoints_To_Cross02 = GetCheckPoints(to, cross02);
            //Print("To-Cross02", string.Join("_", checkPoints_To_Cross02.ConvertAll(p => p.name)));

            var routeCombos = new List<(List<Transform> from, List<Transform> to)>
            {
                (checkPoints_From_Cross01, checkPoints_To_Cross01),
                (checkPoints_From_Cross01, checkPoints_To_Cross02),
                (checkPoints_From_Cross02, checkPoints_To_Cross01),
                (checkPoints_From_Cross02, checkPoints_To_Cross02)
            };

            // --+-----+-----f-----+---+----t-------+----+--
            // |---from---|   ここの間を求める   |---to-------|
            // そのために重ならない 上のfromとtoを求めておいて もともとのcheckPointsからそこを除外する
            var noOverlaped = routeCombos.Find(combo =>
            {
                // checkPointsの方向が拡散する方向 (重ならない方向) を取得
                var overlap = combo.from.Find(c => combo.to.Contains(c));
                return overlap == null;
            });
            //Print("From", string.Join("_", noOverlaped.from.ConvertAll(p => p.name)));
            //Print("To", string.Join("_", noOverlaped.to.ConvertAll(p => p.name)));

            var output = new List<Transform>(checkPoints);
            // fromがcheckPoints.lastにより近い場合はcheckPointsを逆流して移動するということ

            var dist_TopOfCPs_FromPt = Vector3.Distance(from, checkPoints.First().position);
            var dist_TopOfCPs_ToPt = Vector3.Distance(to, checkPoints.First().position);
            //       (TopOfCPs) ----+-------+----(from)----+--------+-------(to)-------+
            // の場合    |---dist_TopOfCPs_FromPt--|                         
            //           |-----------------dist_TopOfCPs_ToPt----------------|
            // 正順なら dist_TopOfCPs_FromPt < dist_TopOfCPs_ToPt になる逆なら dist_TopOfCPs_FromPtのほうが大きくなる
            if (dist_TopOfCPs_FromPt > dist_TopOfCPs_ToPt)
                output.Reverse();

            noOverlaped.from.ForEach(p => output.Remove(p));
            noOverlaped.to.ForEach(p => output.Remove(p));

            return output;
        }

        /// <summary>
        /// Transformから対応したMapLocationを取得する
        /// </summary>
        /// <param name="crossing"></param>
        /// <returns></returns>
        internal MapLocation GetMapLocation(Transform crossing)
        {
            if (cross01.transform == crossing)
                return cross01;
            else if (cross02.transform == crossing)
                return cross02;
            return null;
        }

        public override string ToString()
        {
            return $"Road({id})";
        }
    }

    /// <summary>
    /// Roadを通過する際のコスト
    /// </summary>
    [Serializable]
    enum RoadCost
    {
        Low,
        Normal,
        High
    }
}