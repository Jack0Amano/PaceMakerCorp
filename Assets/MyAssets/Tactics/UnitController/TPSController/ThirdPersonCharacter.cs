using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using static Utility;
using Cinemachine.Utility;
using DG.Tweening;
using UnityEditor.Rendering;
using System.Linq;
using System.Collections.Generic;
using Tactics.Object;

namespace Tactics.Control
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]
    public class ThirdPersonCharacter : MonoBehaviour
    {
        [SerializeField] float m_MovingTurnSpeed = 360;
        [SerializeField] float m_StationaryTurnSpeed = 180;
        [SerializeField] float m_JumpPower = 12f;
        [Range(1f, 4f)] [SerializeField] float m_GravityMultiplier = 2f;
        [SerializeField] float m_RunCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
        [SerializeField] float m_MoveSpeedMultiplier = 1f;
        [SerializeField] float m_AnimSpeedMultiplier = 1f;
        [SerializeField] float m_GroundCheckDistance = 0.1f;
        [SerializeField] float aimSensitivity = 3f;
        [Tooltip("BodyTrigger Layerに反応する　他のUnitとの接触感知")]
        [SerializeField] internal BodyTrigger BodyTrigger;

        [SerializeField] float WalkSpeed = 1.2f;
        [SerializeField] float DashSpeed = 2.2f;

        [Header("Along wall parameters")]
        [Tooltip("壁に沿う形で移動する際 壁のNormalと移動方向の最大角度\n これより上だと沿った移動を行わない")]
        [SerializeField] float FollowingWallMaxDegree = 165;
        [Tooltip("壁に沿う形で移動する際 壁のNormalと移動方向の最小角度\n これより下だと壁から離れる")]
        [SerializeField] float FollowingWallMinDegree = 35;
        [Tooltip("壁に沿って移動する際の移動開始カーブ")]
        [SerializeField] AnimationCurve MoveFollowingWallAnimationCurve;
        [Tooltip("壁から離れる方向の力をどれだけの時間与えれば離れるか")]
        [SerializeField] float LeaveFromWallSeconds = 0.4f;

        /// <summary>
        /// UnitがWallに張り付いておりここで移動を開始した時間
        /// </summary>
        private DateTime StartToMoveTimeOnFollowingWall;
        /// <summary>
        /// Wallに張り付き移動をした最後のOnMoveAnimatorCount
        /// </summary>
        private uint LastMoveFollowingWallCount = 0;
        /// <summary>
        /// 壁に沿って移動する際の移動開始カーブのカーブ経過時間 MoveAlongWallAnimationCurve.length.time
        /// </summary>
        private double MoveAlongWallAnimationTotalDuration;
        /// <summary>
        /// Animatorの移動の継続カウント 移動=0になったときにOnMoveAnimatorCount=0になる
        /// </summary>
        private uint OnMoveAnimatorCount = 0;
        /// <summary>
        /// Wallから離れるLeaveMoveの最後のOnMoveAnimatorCount
        /// </summary>
        private uint LastLeaveFromWallCount = 0;
        /// <summary>
        /// Leave動作を開始した時間
        /// </summary>
        private DateTime StartToLeaveFromWallTime;

        Sequence AlongWallRotationAnimation;

        public bool IsDashMode { private set; get; }

        Rigidbody m_Rigidbody;
        Animator m_Animator;
        // bool m_IsGrounded;
        float m_OrigGroundCheckDistance;
        const float k_Half = 0.5f;
        float m_TurnAmount;
        float m_ForwardAmount;
        Vector3 m_GroundNormal;
        float m_CapsuleHeight;
        Vector3 m_CapsuleCenter;
        CapsuleCollider m_Capsule;
        public bool m_Crouching { private set; get; }
        bool m_Aiming = false;
        const int UpperLayer = 1;

        /// <summary>
        /// UnitがAlongWallに接触しておりカバー状態である
        /// </summary>
        public bool IsFollowingWallMode { get => FollowWallObject != null; }
        /// <summary>
        /// Unitが沿っているWallのgameobject
        /// </summary>
        public GameObject FollowWallObject { private set; get; }
        private Vector3 FollowWallNormal;

        /// <summary>
        /// Unitが沿っているObjectがGimmickObjectである場合そのGimmickObject
        /// </summary>
        public GimmickObject FollowingGimmickObject { private set; get; }
        /// <summary>
        /// Unitが沿っているObjectの接触地点
        /// </summary>
        public Vector3 FollowingWallTouchPosition { private set; get; }

        /// <summary>
        /// アニメーションの再生の停止再開
        /// </summary>
        public bool PauseAnimation
        {
            get => _pauseAnimation;
            set
            {
                _pauseAnimation = value;
                if (m_Animator != null && m_Animator.enabled)
                    m_Animator.speed = value ? 0 : 1;
            }
        }
        private bool _pauseAnimation = false;

        /// <summary>
        /// 現在所持しているアイテム
        /// </summary>
        private Items.Item currentItem;

        /**
         * 最低限必要なAnimatorパラメータ
         *  - Forward
         **/

        #region CoverAction properties
        
        
        private int CollisionObjectLayer;
        int UnitHitsWallFrameCount;
        /// <summary>
        /// Unitが壁に接触している状態
        /// </summary>
        public bool UnitHitsWall { private set; get; } = false;
        bool IsCovering = false;
        Vector3 TakeCoverNormal;
        /// <summary>
        /// 接触している壁に対するDot積
        /// </summary>
        float WallDotProduct;
        /// <summary>
        /// 接触している壁
        /// </summary>
        public GameObject AlongWallObject { private set; get; }
        /// <summary>
        /// Unitの移動
        /// </summary>
        Vector3 AddVelocity;
        /// <summary>
        /// Unitが貼り付ける壁の最小角度
        /// </summary>
        const float TakeCoverMinAngle = 80;
        /// <summary>
        /// Unitが貼り付ける壁の最大角度
        /// </summary>
        const float TakeCoverMaxAngle = 110;
        /// <summary>
        /// Wallに沿って歩いているときの最大速度 MoveFollowingWallAnimationCurveのvalueの最終値となる
        /// </summary>
        private float MaxSpeedWhenFollowWall;

        #endregion

        void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            m_CapsuleHeight = m_Capsule.height;
            m_CapsuleCenter = m_Capsule.center;

            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            m_OrigGroundCheckDistance = m_GroundCheckDistance;

            CollisionObjectLayer = 1 << LayerMask.NameToLayer("Object");

            BodyTrigger.EnterAlongWallActionHandler = EnterAlongWallCallback;
            BodyTrigger.StayAlongWallActionHandler = StayAlonwWallCallback;
            BodyTrigger.ExitAlongWallActionHandler = ExitAlongWallCallback;

            MoveAlongWallAnimationTotalDuration = MoveFollowingWallAnimationCurve.keys.Last().time;
            MaxSpeedWhenFollowWall = MoveFollowingWallAnimationCurve.keys.Last().value;
            StartToMoveTimeOnFollowingWall = DateTime.Now;
        }



        void Start()
        {
        }

        #region Along Wall
        // Tag: AlongWall, Layer: ObjectのColliderに接触したときの呼び出し
        /// <summary>
        /// UnitがAlongWallに接触した際の呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="alongWallEventArgs"></param>
        private void EnterAlongWallCallback(object sender, HitEventArgs alongWallEventArgs)
        {
            if (IsFollowingWallMode && FollowWallObject != alongWallEventArgs.HitObject)
            {
                EndToFollowWall(alongWallEventArgs.HitObject);
            }
        }


        /// <summary>
        /// UnitがAlongWallに接触し続けている間の呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="followingWallEventArgs"></param>
        private void StayAlonwWallCallback(object sender, HitEventArgs followingWallEventArgs)
        {
            FollowingWallTouchPosition = followingWallEventArgs.Position;
            if (FollowWallObject == followingWallEventArgs.HitObject) return;
            if (IsFollowingWallMode) return;
            if (IsDashMode) return;
            if (m_ForwardAmount > 0)
            {
                var ray = new Ray(transform.position, followingWallEventArgs.Position - transform.position);
                if (Physics.Raycast(ray, out var hit, 5, BodyTrigger.WallLayerMask))
                {
                    followingWallEventArgs.Normal = hit.normal;
                    var angle = Vector3.Angle(transform.forward, followingWallEventArgs.Normal);
                    // 歩いている状況
                    if (FollowingWallMaxDegree < angle)
                    {
                        // 壁に向かってほぼ垂直に移動しようとしている
                        StartToFollowWall(followingWallEventArgs);
                    }
                }

            }
        }

        /// <summary>
        /// UnitがAlongWallから離れた際の呼び出し
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="alongWallEventArgs"></param>
        private void ExitAlongWallCallback(object sender, HitEventArgs alongWallEventArgs)
        {
            EndToFollowWall(alongWallEventArgs.HitObject);
        }

        /// <summary>
        /// Wallから離れる際の処理
        /// </summary>
        /// <param name="alongWallEventArgs"></param>
        private void EndToFollowWall(GameObject hitObject)
        {
            if (hitObject == FollowWallObject && IsFollowingWallMode)
            {
                print("AlongWallMode OFF");
                FollowWallObject = null;
                FollowingGimmickObject = null;
                m_Animator.SetBool("AlongWall", false);
            }
        }

        /// <summary>
        /// 壁に沿った風に移動する
        /// </summary>
        /// <param name="alongWallEventArgs"></param>
        private void StartToFollowWall(HitEventArgs alongWallEventArgs)
        {
            if (IsFollowingWallMode) return;

            // 新たな壁に張り付いた場合
            FollowWallObject = alongWallEventArgs.HitObject;
            FollowWallNormal = alongWallEventArgs.Normal;

            FollowingGimmickObject = FollowWallObject.transform.GetComponentInParent<GimmickObject>();

            if (AlongWallRotationAnimation != null && AlongWallRotationAnimation.IsActive())
                AlongWallRotationAnimation.Kill();
            AlongWallRotationAnimation = DOTween.Sequence();
            // Normal方向と同じ方向にUnitを回転させる
            var vectorA = transform.forward;
            var vectorB = alongWallEventArgs.Normal.normalized;
            var angle = Vector3.Angle(vectorA, vectorB);
            var endAngle = transform.rotation.eulerAngles;
            Vector3 cross = Vector3.Cross(vectorA, vectorB);
            if (cross.y < 0) angle = -angle;
            endAngle.y += angle;

            AlongWallRotationAnimation.Append(transform.DORotate(endAngle, 0.5f));
            AlongWallRotationAnimation.Play();

            print("AlongWall Mode ON");
            m_Animator.SetBool("AlongWall", true);
        }

        /// <summary>
        /// Gimmickに沿ってい移動している時このGimmickが破壊されたときの処理
        /// </summary>
        internal void FollowingGimmickIsDestroied()
        {
            FollowingGimmickObject = null;
            FollowWallObject = null;
        }
        #endregion

        #region Item animations
        /// <summary>
        /// haveItemAnimationで指定したアイテムを持つ
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        public IEnumerator HaveItemAnimation(Items.Item item)
        {
            if (item != null)
            {
                m_Animator.SetLayerWeight(UpperLayer, 1);
                m_Animator.CrossFade(item.havingItemAnimationClip.name, 0.2f);
                yield return StartCoroutine(WaitForSeconds(0.2f));
            }
            else
            {
                m_Animator.CrossFade("Default Upper", 0.3f);
                yield return StartCoroutine(WaitForSeconds(0.3f));
                m_Animator.SetLayerWeight(UpperLayer, 0);
            }
            currentItem = item;
        }

        public IEnumerator ChangeItemAnimation(AnimationClip changeItemAnimation)
        {
            // TODO 現在
            yield return StartCoroutine(WaitForSeconds(0.2f));
        }

        /// <summary>
        /// アイテムを構えた状態のアニメーションを再生
        /// </summary>
        /// <param name="active"></param>
        public void SetItemAnimation(Items.UseItemType useItemType, bool active)
        {
            //m_Animator.SetLayerWeight(UpperLayer, 1);
            m_Animator.SetInteger("ItemType", useItemType.GetHashCode());
            m_Animator.SetBool("SetItem", active);
        }

        /// <summary>
        /// アイテムを使用したときのアニメーションの再生開始
        /// </summary>
        /// <param name="active"></param>
        public void UseItemAnimation(Items.UseItemType useItemType, bool active)
        {
            m_Animator.SetInteger("ItemType", useItemType.GetHashCode());
            m_Animator.SetBool("UseItem", active);
        }
        #endregion

        #region Walk
        public void Move(Vector3 move, bool crouch, bool jump, bool dash)
        {
            IsDashMode = dash;
            move *= WalkSpeed;
            if (dash)
            {
                move *= DashSpeed;
                FollowWallObject = null;
            }

            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            if (move.magnitude > 1f) move.Normalize();
            move = transform.InverseTransformDirection(move);
            move = Vector3.ProjectOnPlane(move, m_GroundNormal);
            m_TurnAmount = Mathf.Atan2(move.x, move.z);
            m_ForwardAmount = move.z;

            ApplyExtraTurnRotation();

            // ScaleCapsuleForCrouching(crouch);
            // PreventStandingInLowHeadroom();

            // send input and other state parameters to the animator
            UpdateAnimator(move);
        }

        /// <summary>
        /// World座標軸を基準にした移動 XがTurn YがForward
        /// </summary>
        /// <param name="move">YがForward XがTurn</param>
        /// <param name="speed">1(歩行)~2(走る)</param>
        public void WorldMove(Vector2 move, float speed)
        {
            move.Normalize();
            var _speed = speed * 10f;
            var x = (float)Math.Ceiling(move.x * _speed) / 10f;
            var y = (float)Math.Ceiling(move.y * _speed) / 10f;

            m_TurnAmount = x;
            m_ForwardAmount = y;

            ApplyExtraTurnRotation();

            UpdateAnimator(new Vector2(x, y));
        }

        /// <summary>
        /// valueだけUnitを回転させる
        /// </summary>
        /// <param name="move"></param>
        public void Rotate(float value)
        {
            RotateWithoutAnimation(value);
            UpdateAnimator(Vector3.zero);
        }

        /// <summary>
        /// アニメーションなしで回転
        /// </summary>
        /// <param name="value"></param>
        public void RotateWithoutAnimation(float value)
        {
            m_TurnAmount = value;
            m_ForwardAmount = 0;
            ApplyExtraTurnRotation();

            m_Animator.SetFloat("Forward", 0, 0, Time.deltaTime);
            m_Animator.SetFloat("Turn", 0, 0, Time.deltaTime);
            if(!m_Rigidbody.isKinematic)
                m_Rigidbody.velocity = Vector3.zero;
        }

        void PreventStandingInLowHeadroom()
        {
            // prevent standing up in crouch-only zones
            if (!m_Crouching)
            {
                Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
                float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
                if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    m_Crouching = true;
                }
            }
        }


        void UpdateAnimator(Vector3 move)
        {
            // update the animator parameters
            if (!IsFollowingWallMode)
            {
                m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
                m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
                m_Animator.SetBool("Crouch", m_Crouching);
            }
            else
            {
                m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
                m_Animator.SetFloat("Forward", -0.0001f, 0.1f, Time.deltaTime);
            }

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
            // which affects the movement speed because of the root motion.
            if (move.magnitude > 0)
            {
                m_Animator.speed = m_AnimSpeedMultiplier;
            }
            else
            {
                // don't use that while airborne
                m_Animator.speed = 1;
            }
        }


        void HandleAirborneMovement()
        {
            // apply extra gravity from multiplier:
            Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
            m_Rigidbody.AddForce(extraGravityForce);

            m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
        }


        void HandleGroundedMovement(bool crouch, bool jump)
        {
            // check whether conditions are right to allow a jump:
            if (jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
            {
                // jump!
                m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
                m_Animator.applyRootMotion = false;
                m_GroundCheckDistance = 0.1f;
            }
        }

        void ApplyExtraTurnRotation()
        {
            if (!IsFollowingWallMode)
            {
                // help the character turn faster (this is in addition to root rotation in the animation)
                float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
                transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
            }
        }

        private uint LastFrameMoveOnFollowingWall = 0;
        public void OnAnimatorMove()
        {
            // KinematicなRigidbodyの場合はVelocityを設定できないためアニメーションからの移動を行わない
            if (m_Rigidbody.isKinematic) return;

            if (!IsFollowingWallMode)
            {
                // FollowWallModeでなく通常の移動
                // we implement this function to override the default root motion.
                // this allows us to modify the positional speed before it's applied.
                if (Time.deltaTime > 0 && m_Animator.deltaPosition != Vector3.zero)
                {
                    AddVelocity = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;
                    AddVelocity.y = m_Rigidbody.velocity.y;
                    if (m_Rigidbody.isKinematic)
                        print(this);
                    m_Rigidbody.velocity = AddVelocity;
                }
            }
            else
            {
                // すでに修正済みだが壁から離れる動作が遅れる == 離れるアニメーションが即座に実行されない原因はAnimatorの遷移のHasExitTimeがtrueになっている

                AddVelocity = new Vector3(m_TurnAmount, 0, m_ForwardAmount);
                if (AddVelocity.magnitude > 0.1)
                {
                    // Convert local velocity to velocity on world position
                    AddVelocity = (transform.TransformPoint(AddVelocity) - transform.position).normalized;
                    var angle = Vector3.Angle(AddVelocity, FollowWallNormal);
                    if (FollowingWallMaxDegree < angle)
                    {
                        // 壁に向かってほぼ垂直に移動しようとしている
                        StartToLeaveFromWallTime = DateTime.Now;
                    }
                    else if (FollowingWallMinDegree > angle)
                    {
                        // 壁から離れようとする移動方向
                        //m_Rigidbody.velocity = AddVelocity;
                        //AlongWallObject = null;
                        if (OnMoveAnimatorCount - LastLeaveFromWallCount > 30)
                        {
                            StartToLeaveFromWallTime = DateTime.Now;
                        }
                        else if ((float)((DateTime.Now - StartToLeaveFromWallTime).TotalSeconds) > LeaveFromWallSeconds)
                        {
                            // 離れる動作をLeaveFromWallSecond間行ったためLeave動作を開始する
                            EndToFollowWall(FollowWallObject);

                        }
                        LastLeaveFromWallCount = OnMoveAnimatorCount + 1;
                    }
                    else
                    {
                        StartToLeaveFromWallTime = DateTime.Now;

                        var moveAmount = Math.Abs(m_TurnAmount);
                        float secondsFromStartToMoveTimeOnFollowingWall = (float)(DateTime.Now - StartToMoveTimeOnFollowingWall).TotalSeconds;
                        if (OnMoveAnimatorCount - LastMoveFollowingWallCount > 60)
                        {
                            // 最後の入力から十分時間が経っている状況
                            StartToMoveTimeOnFollowingWall = DateTime.Now;
                        }
                        if (secondsFromStartToMoveTimeOnFollowingWall < MoveAlongWallAnimationTotalDuration)
                        {
                            // 移動を開始したCurve内
                            moveAmount = MoveFollowingWallAnimationCurve.Evaluate(secondsFromStartToMoveTimeOnFollowingWall);
                        }


                        // 壁に沿って移動する速度の最大値に達している
                        if (OnMoveAnimatorCount - LastFrameMoveOnFollowingWall < 3)
                        {
                            //IsMovingOnFollowingWall = true;
                        }
                                
                        else
                            LastFrameMoveOnFollowingWall = OnMoveAnimatorCount;
                        m_Rigidbody.velocity = Quaternion.AngleAxis(m_TurnAmount < 0 ? -90 : 90, Vector3.up) * FollowWallNormal * moveAmount;

                        LastMoveFollowingWallCount = OnMoveAnimatorCount + 1;

                        // 壁に沿って左右に移動する
                        
                    }
                }
            }

            OnMoveAnimatorCount++;
        }

        private void FixedUpdate()
        {
            if (IsFollowingWallMode && !m_Rigidbody.isKinematic)
            {
                if (m_ForwardAmount == 0 || m_TurnAmount == 0)
                    m_Rigidbody.velocity = Vector3.zero;
                // Rigidbodyの移動を停止する
            }
        }

        #endregion

        #region Crouching
        internal IEnumerator Crouch(bool active)
        {
            ScaleCapsuleForCrouching(active);
            m_Animator.SetBool("Crouch", m_Crouching);

            if (active)
            {
                var weight = 1f;
                while (weight > 0)
                {
                    weight -= 0.01f;
                    m_Animator.SetLayerWeight(UpperLayer, weight);
                    yield return null;
                }
                
            }
            else
            {
                var weight = 0f;
                while (weight < 1)
                {
                    weight += 0.01f;
                    m_Animator.SetLayerWeight(UpperLayer, weight);
                    yield return null;
                }
            }
        }

        void ScaleCapsuleForCrouching(bool crouch)
        {
            if (crouch)
            {
                if (m_Crouching) return;
                m_Capsule.height /= 2f;
                m_Capsule.center /= 2f;
                m_Crouching = true;
            }
            else
            {
                //Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
                //float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
                //if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                //{
                //	m_Crouching = true;
                //	return;
                //}
                m_Capsule.height = m_CapsuleHeight;
                m_Capsule.center = m_CapsuleCenter;
                m_Crouching = false;
            }
        }
        #endregion

        /// <summary>
        /// <c>PauseAnimation</c>に対応したWaitForSeconds
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        private IEnumerator WaitForSeconds(float duration)
        {
            var start = DateTime.Now;
            while((DateTime.Now - start).TotalMilliseconds < duration * 1000)
            {
                if (PauseAnimation)
                {
                    var startStopping = DateTime.Now;
                    while (PauseAnimation)
                        yield return null;
                    var stopTime = (DateTime.Now - startStopping).TotalMilliseconds;
                    start.AddMilliseconds(stopTime);
                }
                else
                {
                    yield return null;
                }
            }
        }


        #region Deprecated
        private void OnCollisionEnterTrigger(Collider collider)
        {
            if (((1 << collider.gameObject.layer) & CollisionObjectLayer) != 0)
            {
                UnitHitsWall = true;
            }
        }

        private void OnTriggerStayTrigger(GameObject other, Vector3 hitPosition)
        {

        }

        private void OnCollisionStay_(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & CollisionObjectLayer) != 0)
            {
                // もしAddVelocity!=.zeroでdotProduct~=-1.0の場合壁に向かって走っている
                UnitHitsWall = true;

                // 衝突した面の、接触した点における法線ベクトルを取得
                var contactNormal = collision.contacts[0].normal;

                var dotProduct = Vector3.Dot(contactNormal, AddVelocity.normalized);
                var angle = Vector3.Angle(contactNormal, Vector3.up);

                if (TakeCoverMinAngle < angle && angle < TakeCoverMaxAngle)
                {
                    if (UnitHitsWallFrameCount == Time.frameCount)
                    {
                        // 2つ以上のカバー可能な壁に張り付いている状態
                        if (IsCovering)
                        {
                            // 現在カバー中
                        }
                        else
                        {
                            // カバーに入っていない
                            // 2つのカバー可能な壁に同時に接触した
                            if (dotProduct < WallDotProduct)
                            {
                                // この呼び出しで呼び出されたconverのほうが垂直に接触している
                                TakeCoverNormal = contactNormal;
                                WallDotProduct = dotProduct;
                                IsCovering = true;
                                AlongWallObject = collision.gameObject;

                                print($"Start covering more: {collision.gameObject}");
                            }
                        }
                    }
                    else
                    {
                        // 1つのカバー可能な壁に張り付いている状態
                        if (IsCovering)
                        {
                            // 現在カバー中
                        }
                        else
                        {
                            // カバーに入っていない
                            TakeCoverNormal = contactNormal;
                            WallDotProduct = dotProduct;
                            IsCovering = true;
                            AlongWallObject = collision.gameObject;
                            print($"Start covering 1: {collision.gameObject}");
                        }
                    }
                }
                else
                {
                    // カバー不能な角度の壁についている
                }

                UnitHitsWallFrameCount = Time.frameCount;
            }
        }

        private void OnCollisionExitTrigger(Collider collider)
        {
            if (((1 << collider.gameObject.layer) & CollisionObjectLayer) != 0)
            {
                UnitHitsWall = false;
                IsCovering = false;
                print("End covering");
            }
        }
        #endregion

    }

}