using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using Tactics.Character;
using System.Linq;
using static Utility;
using UnityEditor;

public class TitleTest : MonoBehaviour
{

    [SerializeField] float difference = 0.1f;

    [SerializeField] float distance = 0.1f;
    [SerializeField] float chooseRate = 0.7f;

    List<Vector3> gridPoints = new List<Vector3>();

    [SerializeField] List<Vector3> pt;

    [SerializeField] private GUIStyle gUIStyle;

    [SerializeField] private Transform basePanel;

    List<bool> test;

    // Start is called before the first frame update
    void Start()
    {

        var filter = GetComponent<MeshFilter>();
        var filter2 = basePanel.GetComponent<MeshFilter>();

        var pts = GetPoints(basePanel, filter2);

        var minx = pts.FindMin(p => p.x);
        var minz = pts.FindMin(p => p.z);

        gridPoints = MakeGrid(basePanel, new Vector3(minx.x, pts[0].y, minz.z));
        test = PointOnCubeY(gridPoints, filter);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for(var i=0; i<gridPoints.Count; i++)
        {
            Gizmos.DrawSphere(gridPoints[i], 0.07f);
        }

        for(var i=0; i<gridPoints.Count; i++)
        {
            var p = gridPoints[i];
            if (test != null && test.Count == gridPoints.Count &&  test[i])
                Gizmos.color = Color.blue;
            else
                Gizmos.color = Color.green;

            Gizmos.DrawSphere(p, 0.07f);
        }
    }

    private List<Vector3> MakeGrid(Transform transform, Vector3 minPos)
    {
        var scale = transform.localScale;

        var pts = new List<Vector3>();
        for (var i = 1; i < ((scale.x - difference * 2) / distance); i++)
        {
            var _pt = minPos;
            _pt.x += difference + distance * i;
            _pt.z += difference;
            for (var j = 1; j < ((scale.z - difference * 2) / distance); j++)
            {
                var pt = _pt;
                pt.z += distance * j;
                pts.Add(pt);
            }
        }
        return pts;
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
    /// <c>pts</c>が<c>filter</c>のポリゴン内に存在する場合true、存在しないときfalseになるような配列を返す
    /// </summary>
    /// <param name="pts"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    private List<bool> PointOnCubeY(List<Vector3> pts, MeshFilter filter)
    {
        var cubePts = GetPoints(transform, filter);

        var output = new List<bool>();
        foreach (var pt in pts)
            output.Add(Check(cubePts.ToArray(), pt, new Vector3(0, 1, 0)));

        return output;
    }

    /// <summary>
    /// CellとTriggerの間の隙間を設定する
    /// </summary>
    /// <param name="difference">隙間の距離</param>
    private Vector3 SetDifferenceCellAndTrigger(float difference)
    {
        var scaleRate = (transform.localScale.Subtraction(difference)).Divide(transform.localScale);
        var triggerScale = Vector3.one.Multiply( scaleRate );
        return triggerScale;
    }

    // Update is called once per frame
    void Update()
    {
    }

}
