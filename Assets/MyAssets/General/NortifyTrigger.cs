using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using static Utility;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Experimental.UIElements;

/// <summary>
/// Colliderの衝突情報をGetするためのTrigger
/// </summary>
public class NortifyTrigger : MonoBehaviour
{
    [Tooltip("衝突するLayerマスク")]
    [SerializeField] LayerMask layerMask;

    [Tooltip("衝突するTag Name")]
    [SerializeField] string tagName;

    [Tooltip("衝突の位置を取得する場合MeshFilterの設定が必要")]
    [SerializeField] MeshFilter meshFilter;

    /// <summary>
    /// Trigger内にObjectが入った場合のObjectを取得する
    /// </summary>
    public Action<Collider> OnTriggerEnterAction;
    /// <summary>
    /// Trigger外にObjectが出た場合のObjectを取得する
    /// </summary>
    public Action<Collider> OnTriggerExitAction;
    /// <summary>
    /// Trigger内にStayしているObjectを取得する
    /// </summary>
    public Action<Collider> OnTriggerStayAction;
    /// <summary>
    /// Trigger内にObjectが入った場合のObjectとその位置の中点を取得する
    /// </summary>
    public Action<GameObject, Vector3> OnTriggerEnterPositionAction;
    /// <summary>
    /// Trigger外にObjectが出た場合のObjectとその位置を取得する
    /// </summary>
    public Action<GameObject, Vector3> OnTriggerExitPositionAction;
    /// <summary>
    /// Trigger内にStayしているObjectとその位置を取得する
    /// </summary>
    public Action<GameObject, Vector3> OnTriggerStayPositionAction;
    /// <summary>
    /// Trigger内に存在するObjectを保持
    /// </summary>
    public HashSet<GameObject> objectsInTrigger = new HashSet<GameObject>();

    private Collider collider;

    private List<Vector3> vertices = new List<Vector3>();

    private List<Vector3> wv = new List<Vector3>();
    private Vector3 cwv = Vector3.zero;

    

    public bool isActive
    {
        set
        {
            collider.isTrigger = !value;
        }
        get => collider.isTrigger!;
    }

    protected private void Awake()
    {
        collider = GetComponent<Collider>();

        if (meshFilter != null)
            vertices = new List<Vector3>( meshFilter.mesh.vertices);
    }

    private bool CheckHit(GameObject gameObject)
    {
        return (layerMask.value & (1 << gameObject.layer)) != 0 || gameObject.tag.Equals(tagName);
    }

    protected private void OnTriggerEnter(Collider other)
    {
        if (CheckHit(other.gameObject))
        {
            objectsInTrigger.Add(other.gameObject);
            OnTriggerEnterAction?.Invoke(other);
            if (OnTriggerEnterPositionAction != null)
            {
                var worldVertices = vertices.ConvertAll(v => v + transform.position);
                var attachingVertices = worldVertices.FindAll(v => {
                    var d = Vector3.Distance(other.ClosestPoint(v), v);
                    return d < 0.2;
                });
                var center = new Vector3();
                attachingVertices.ForEach(v => center += v);
                center /= attachingVertices.Count;
                OnTriggerEnterPositionAction.Invoke(other.gameObject, center);
            }
        }
            
    }

    protected private void OnTriggerExit(Collider other)
    {
        if (CheckHit(other.gameObject))
        {
            objectsInTrigger.Remove(other.gameObject);
            OnTriggerExitAction?.Invoke(other);

            OnTriggerExitPositionAction?.Invoke(other.gameObject,
                                                other.ClosestPointOnBounds(this.transform.position));
        }
    }

    protected private void OnTriggerStay(Collider other)
    {
        if (CheckHit(other.gameObject))
        {
            OnTriggerStayAction?.Invoke(other);

            if (OnTriggerStayPositionAction != null)
            {
                var worldVertices = vertices.ConvertAll(v => v + transform.position);
                var attachingVertices = worldVertices.FindAll(v => {
                    var d = Vector3.Distance(other.ClosestPoint(v), v);
                    return d < 0.2;
                });

                if (attachingVertices.Count == 0)
                    return;

                var center = new Vector3();
                attachingVertices.ForEach(v => center += v);
                center /= attachingVertices.Count;
                cwv = center;
                OnTriggerStayPositionAction?.Invoke(other.gameObject, center);
            }
        }
    }
}
