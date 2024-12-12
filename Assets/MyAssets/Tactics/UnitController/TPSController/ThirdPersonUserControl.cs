using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System;
using Cinemachine;
using static Utility;
using System.Linq;
using System.Collections.Generic;
using Tactics.Character;
using Tactics.Object;

namespace Tactics.Control
{
    [RequireComponent(typeof(ThirdPersonCharacter))]
    public class ThirdPersonUserControl : MonoBehaviour
    {
        [SerializeField] internal CinemachineVirtualCamera aimCamera;
        [SerializeField] internal CinemachineVirtualCamera followCamera;
        [Tooltip("UnitをUserControl可能か")]
        [SerializeField] public bool IsTPSControllActive = false;
        [SerializeField] public bool haveItem = false;
        [Tooltip("マウスのX軸移動でUnitを回転させる")]
        [SerializeField] public bool IsMouseHandleMode = false;
        [SerializeField] internal GameObject FollowCameraCenter;
        [Header("肩越しカメラ")]
        [Tooltip("肩越しカメラ")]
        [SerializeField] internal CinemachineVirtualCamera OverShoulderCamera;
        [SerializeField] internal float OverShoulderCameraMaxXRotation = 1.0f;
        [SerializeField] internal float OverShoulderCameraMinXRotation = 0;

        [Header("肩越しの少し離れた箇所のカメラ")]
        [SerializeField] internal CinemachineVirtualCamera OverShoulderCameraFar;


        internal Transform OverShoulderCameraParent;
        internal float OverShoulderCameraDefaultXRotation;

        internal Transform overShoulderCameraFarParent;

        private Transform followCameraTransform;
        private ThirdPersonCharacter m_Character; // A reference to the ThirdPersonCharacter on the object
        private Vector3 m_CamForward;             // The current forward direction of the camera
        private Vector3 m_Move;
        private bool dash = false;

        private float horizontal = 0;
        private float vertical = 0;

        /// <summary>
        /// FollowCameraを横にずらしておくためのparent
        /// </summary>
        internal GameObject followCameraParent;

        /// <summary>
        /// Userに関連付けられているcinemachineVirtualCamera
        /// </summary>
        public List<CinemachineVirtualCamera> CinemachineVirtualCameras
        {
            get
            {
                _cinemachineVirtualCameras ??= new List<CinemachineVirtualCamera>()
                {
                    aimCamera, 
                    followCamera, 
                    OverShoulderCamera, 
                    OverShoulderCameraFar,
                    UnitController.stationaryCamera, 
                    UnitController.watchTabletCamera
                };
                return _cinemachineVirtualCameras;
            }
        }
        private List<CinemachineVirtualCamera> _cinemachineVirtualCameras;

        /// <summary>
        /// Animationの再生と停止
        /// </summary>
        public bool PauseAnimation
        {
            get => m_Character.PauseAnimation;
            set
            {
                m_Character.PauseAnimation = value;
            }
        }
        /// <summary>
        /// 現在動いているか
        /// </summary>
        internal bool IsMoving
        {
            get => isMoving;
            set
            {
                if (!value)
                    m_Character.Move(Vector3.zero, false, false, false);
                isMoving = value;
            }
        }
        private bool isMoving = false;
        /// <summary>
        /// 自動で経路にコントロール中
        /// </summary>
        public bool IsAutoMoving { private set; get; } = false;
        Vector2 smoothDeltaPosition = Vector2.zero;
        Vector2 velocity = Vector2.zero;
        /// <summary>
        /// 現在手に持っているItem
        /// </summary>
        private Items.Item currentItem;

        private List<Vector3> debugAutoMovingWay;

        /// <summary>
        /// 現在しゃがんでいるか
        /// </summary>
        internal bool IsCrouching 
        {
            get => m_Character.m_Crouching;
        }

        public UnitController UnitController
        {
            get
            {
                if (unitController == null)
                    unitController = GetComponent<UnitController>();
                return unitController;
            }
        }
        private UnitController unitController;

        /// <summary>
        /// UnitがAlongWallに接触しておりカバー状態である
        /// </summary>
        public bool IsFollowingWallMode { get => m_Character.IsFollowingWallMode; }
        /// <summary>
        /// Unitが沿っているWallのgameobject
        /// </summary>
        public GameObject FollowWallObject { get => m_Character.FollowWallObject; }
        /// <summary>
        /// Unitが沿っているObjectがGimmickObjectである場合そのGimmickObject
        /// </summary>
        public GimmickObject FollowingGimmickObject { get => m_Character.FollowingGimmickObject; }
        /// <summary>
        /// Unitが沿っているObjectの接触地点
        /// </summary>
        public Vector3 FollowingWallTouchPosition { get => m_Character.FollowingWallTouchPosition; }

        private void Awake()
        {
            m_Character = GetComponent<ThirdPersonCharacter>();
            followCameraParent = followCamera.transform.parent.gameObject;
            OverShoulderCameraParent = OverShoulderCamera.transform.parent;
            OverShoulderCameraDefaultXRotation = OverShoulderCameraParent.localRotation.eulerAngles.x;

            overShoulderCameraFarParent = OverShoulderCameraFar.transform.parent;

            // UnitのvirtualCameraのPriorityを0に初期化する
            CinemachineVirtualCameras.ForEach(c => c.Priority = 0);
        }

        private void Start()
        {
            // get the third person character ( this should never be null due to require component )
            followCameraTransform = followCamera.transform;
        } 

        // Fixed update is called in sync with physics
        private void FixedUpdate()
        {
            if (PauseAnimation)
                return;

            if (IsAutoMoving)
                return;

            if (IsTPSControllActive)
            {
                // read inputs
                if (m_Character.IsFollowingWallMode)
                {
                    horizontal = UserController.KeyHorizontalRaw;
                    vertical = UserController.KeyVerticalRaw;
                }
                else
                {
                    horizontal = UserController.KeyHorizontal;
                    vertical = UserController.KeyVertical;
                }
                IsMoving = horizontal != 0 || vertical != 0;

                if (IsMoving)
                {
                    // calculate move direction to pass to character
                    var forward = transform.parent.transform.TransformDirection(followCameraTransform.forward);
                    m_CamForward = Vector3.Scale(forward, new Vector3(1, 0, 1)).normalized;
                    var right = transform.parent.transform.TransformDirection(followCameraTransform.right);
                    m_Move = vertical * m_CamForward + horizontal * right;
                    m_Character.Move(m_Move, false, false, UserController.KeyCodeDash);
                }
            }
            else if (IsMouseHandleMode)
            {
                var rotate = UserController.MouseDeltaX;
                m_Character.RotateWithoutAnimation(rotate);
            }
            else
            {
                IsMoving = false;
            }

            // walk speed multiplier
            // if (TacticsController.KeyCodeLeftShift) m_Move *= 0.5f;

            // pass all parameters to the character control script
            //if (isAimMode && !isMoving)
            //{
            //    m_Character.Rotation(new Vector3(0, cameraUserController.mouseDeltaX, 0));
            //}
            //else
            //    m_Character.Move(m_Move, UserController.KeyCodeC, false);
        }

        #region Crouching
        /// <summary>
        /// しゃがんだ状態で待機
        /// </summary>
        internal void SwitchCrouching()
        {
            StartCoroutine( m_Character.Crouch(!m_Character.m_Crouching) );
        }
        #endregion

        #region Weapons
        /// <summary>
        /// アイテムを手に持って装備した状態に
        /// </summary>
        public IEnumerator HaveItem(Items.Item item)
        {
            currentItem = item;
            if (!haveItem)
            {

                haveItem = true;
                yield return StartCoroutine( m_Character.HaveItemAnimation(item) );
            }
            else
            {
                // 別のアイテムから指定されたものに切り替え
                yield return StartCoroutine(m_Character.HaveItemAnimation(item));
            }
        }

        /// <summary>
        /// アイテムを構えた状態にする
        /// </summary>
        public void AimItem(bool active)
        {
            if (haveItem)
                m_Character.SetItemAnimation(currentItem.useItemType, active);
            else
            {
                // アイテムを持っていない状態でとっさに構える
            }
        }

        /// <summary>
        /// アイテムを使用する
        /// </summary>
        /// <param name="active"></param>
        internal IEnumerator UseItem()
        {
            if (haveItem)
            {
                m_Character.UseItemAnimation(currentItem.useItemType, true);
                yield return new WaitForSeconds(currentItem.UseItemDuration);
                m_Character.UseItemAnimation(currentItem.useItemType, false);
            }
            else
            {
                // アイテムを持っていない状態でとっさに使用する
            }
        }

        /// <summary>
        /// 構えている武器を下ろす 何も持っていない状態
        /// </summary>
        public void RemoveItem()
        {
            haveItem = false;
            StartCoroutine(m_Character.HaveItemAnimation(null));
        }
        #endregion

        #region Damage
        /// <summary>
        /// 張り付いていたり使用中のギミックが破壊されたときに呼び出される
        /// </summary>
        internal void FollowingGimmickIsDestroied(GimmickObject gimmickObject)
        {
            
        }

        /// <summary>
        /// 使用中のギミックが破壊されたときに呼び出される
        /// </summary>
        internal void UsingGimmickIsDestroied(GimmickObject gimmickObject)
        {
            // TODO ここでGimmickを破壊したことを通知する
        }
        #endregion

        #region Auto moving
        /// <summary>
        /// Unitを自動で目的地まで走らせる
        /// </summary>
        /// <param name="to"></param>
        /// <param name="navMeshAgent"></param>
        /// <returns></returns>
        internal IEnumerator AutoMove(Vector3 to, NavMeshAgent navMeshAgent, bool debug = false)
        {
            navMeshAgent.SetDestination(to);
            while (!navMeshAgent.hasPath)
                yield return null;

            var corners = navMeshAgent.path.corners.ToList();
            debugAutoMovingWay = corners;
            navMeshAgent.ResetPath();

            IsAutoMoving = true;
            IsMoving = true;


            if (debug)
            {
                for (var i = 1; i < corners.Count; i++)
                {
                    Debug.DrawLine(corners[i - 1], corners[i], Color.green, 3);
                }
            }

            const float TimeOver = 5000f;
            for (var i = 1; i < corners.Count; i++)
            {
                var startTime = DateTime.Now;
                while (IsAutoMoving)
                {
                    var corner = corners[i];
                    Vector3 worldDeltaPosition = corner - transform.position;
                    var dx = Vector3.Dot(transform.right, worldDeltaPosition);
                    var dy = Vector3.Dot(transform.forward, worldDeltaPosition);
                    Vector2 deltaPosition = new Vector2(dx, dy);

                    float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.05f);
                    smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

                    if (Time.deltaTime > 1e-5f)
                        velocity = smoothDeltaPosition / Time.deltaTime;

                    bool shouldMove = velocity.magnitude > 0.5f && Vector3.Distance(transform.position, corner) > navMeshAgent.radius / 5;
                    if (!shouldMove) break;

                    m_Character.WorldMove(smoothDeltaPosition, 2);
                    yield return null;

                    // 一時停止に対応
                    if (PauseAnimation)
                    {
                        var startPause = DateTime.Now;
                        while (PauseAnimation)
                            yield return null;
                        startTime.Add(DateTime.Now - startPause);
                    }

                    if ((DateTime.Now - startTime).TotalMilliseconds > TimeOver)
                    {
                        PrintWarning("Time is over to move to corner of", corner);
                        transform.position = new Vector3(corner.x, 2, corner.z);

                        break;
                    }
                }
            }

            IsAutoMoving = false;
            IsMoving = false;
        }

        private void OnDrawGizmos()
        {
            if (debugAutoMovingWay == null || debugAutoMovingWay.Count <= 1)
                return;

            Gizmos.color = Color.green;
            for(var i=1; i<debugAutoMovingWay.Count; i++)
            {
                Gizmos.DrawLine(debugAutoMovingWay[i - 1], debugAutoMovingWay[i]);
            }
        }

        /// <summary>
        /// Characterを回転移動させTargetポジションに向ける
        /// </summary>
        /// <param name="rotation"></param>
        public IEnumerator RotateTo(Vector3 target, bool fastRotation = false)
        {
            // TODO 回転速度の上昇を行う
            IsAutoMoving = true;
            IsMoving = true;
            while (IsAutoMoving)
            {
                Vector3 worldDeltaPosition = target - transform.position;
                var dx = Vector3.Dot(transform.right, worldDeltaPosition);
                var dy = Vector3.Dot(transform.forward, worldDeltaPosition);
                Vector2 deltaPosition = new Vector2(dx, dy);

                float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.05f);
                smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);
                smoothDeltaPosition = smoothDeltaPosition.normalized;

                if (Math.Abs(smoothDeltaPosition.x) < 0.05)
                    break;

                yield return null;
                m_Character.Rotate(smoothDeltaPosition.x);
            }
            IsAutoMoving = false;
            IsMoving = false;
        }

        /// <summary>
        /// 現在のTPSControllerのオート移動をキャンセルする
        /// </summary>
        public void CancelAutoMoving()
        {
            IsAutoMoving = false;
        }

        #endregion
    
    }
}

