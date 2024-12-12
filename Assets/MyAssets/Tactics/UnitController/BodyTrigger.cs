using System;
using System.Collections;
using System.Collections.Generic;
using Tactics.Character;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using static Utility;

namespace Tactics.Control
{
    [RequireComponent(typeof(MeshFilter))]
    public class BodyTrigger : MonoBehaviour
    {
        [Tooltip("�Փ˂���Tag Name")]
        [SerializeField] string WallTagName;
        [SerializeField] internal LayerMask WallLayerMask;

        [SerializeField] string UnitTagName;
        [SerializeField] LayerMask UnitLayerMask;

        private List<Vector3> vertices = new List<Vector3>();

        private Vector3 TouchPosition;

        /// <summary>
        /// Unit�������Tag������gameobjec�ɐڐG�����ۂ̌Ăяo��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal delegate void HitActionHandler(object sender, HitEventArgs e);

        /// <summary>
        /// AlongWall��tag�̂���wall��Unit���ڐG�����ۂ̌Ăяo��
        /// </summary>
        internal HitActionHandler EnterAlongWallActionHandler;
        private HitEventArgs EnterAlongWallEventArgs = new HitEventArgs();
        /// <summary>
        /// AlongWall��tag�̂���wall��Unit���ڐG���Ă���ԌĂяo��
        /// </summary>
        internal HitActionHandler StayAlongWallActionHandler;
        private HitEventArgs StayAlongWallEventArgs = new HitEventArgs();
        /// <summary>
        /// AlongWall��tag�̂���wall����Unit�����ꂽ�ۂ̌Ăяo��
        /// </summary>
        internal HitActionHandler ExitAlongWallActionHandler;
        private HitEventArgs ExitAlongWallEventArgs = new HitEventArgs();

        /// <summary>
        /// Unit��tag�̂���gameobject�ɐڐG�����ۂ̌Ăяo��
        /// </summary>
        internal HitActionHandler EnterOtherUnitActionHandler;
        internal HitEventArgs EnterOtherUnitEventArgs = new HitEventArgs();
             
        private void Awake()
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            vertices = new List<Vector3>(meshFilter.mesh.vertices);
        }

        protected private void OnTriggerEnter(Collider other)
        {
            void Call(HitEventArgs args, HitActionHandler handler, LayerMask layerMask)
            {
                if (handler == null) return;

                TouchPosition = GetTouchPosition(other);
                var ray = new Ray(transform.position, TouchPosition - transform.position);
                if (Physics.Raycast(ray, out var hit, 5, layerMask))
                {
                    args.HitObject = other.gameObject;
                    args.Normal = hit.normal;
                    args.Position = TouchPosition;
                    handler?.Invoke(this, args);
                }
            }

            if (other.gameObject.CompareTag(WallTagName))
            {
                Call(EnterAlongWallEventArgs, EnterAlongWallActionHandler, WallLayerMask);
            }
            else if (other.gameObject.CompareTag(UnitTagName))
            {
                Call(EnterOtherUnitEventArgs, EnterOtherUnitActionHandler, UnitLayerMask);
            }
        }

        protected private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.CompareTag(WallTagName))
            {
                //TouchPosition = GetTouchPosition(other)
                StayAlongWallEventArgs.HitObject = other.gameObject;
                StayAlongWallEventArgs.Position = GetTouchPosition(other);
                StayAlongWallActionHandler?.Invoke(this, StayAlongWallEventArgs);
            }
        }

        protected private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag(WallTagName))
            {
                //TouchPosition = GetTouchPosition(other);
                ExitAlongWallEventArgs.HitObject = other.gameObject;
                ExitAlongWallEventArgs.Position = GetTouchPosition(other);
                ExitAlongWallActionHandler?.Invoke(this, ExitAlongWallEventArgs);
            }
        }

        private Vector3 GetTouchPosition(Collider collider)
        {
            var worldVertices = vertices.ConvertAll(v => v + transform.position);
            var attachingVertices = worldVertices.FindAll(v => {
                var d = Vector3.Distance(collider.ClosestPoint(v), v);
                return d < 0.2;
            });

            if (attachingVertices.Count == 0)
                return Vector3.positiveInfinity;

            var center = new Vector3();
            attachingVertices.ForEach(v => center += v);
            center /= attachingVertices.Count;
            return center;
        }
    }

    /// <summary>
    /// �\��t����ǂ�Unit���ڐG�◣�ꂽ�ۂ�arg
    /// </summary>
    internal class HitEventArgs: EventArgs
    {
        /// <summary>
        /// �ڐG�������̂�GameObject
        /// </summary>
        internal GameObject HitObject;
        /// <summary>
        /// �ڐG�����ʂ�Normal
        /// </summary>
        internal Vector3 Normal;
        /// <summary>
        /// �ڐG�����ꏊ��position
        /// </summary>
        internal Vector3 Position;
    }
}