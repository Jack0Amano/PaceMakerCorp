using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;
using DG.Tweening;
using static Utility;
using Tactics.Character;
using System.Runtime.CompilerServices;
using DG.Tweening.Core.Easing;
using Tactics.Object;
using Tactics.UI;

namespace Tactics.Control
{
    public class CameraUserController : MonoBehaviour
    {
        [SerializeField] public Camera mainCamera;
        [Tooltip("カメラのマウスコントロールの是非")]
        [SerializeField] public bool enableCameraControlling = true;
        [SerializeField] UnitsController unitsController;
        [Tooltip("カメラ遷移の時間")]
        [SerializeField] float MoveCameraDuration = 0.7f;

        [Header("Follow Camera Properties")]
        [Tooltip("FollowCameraが非操作時targetの裏に自動で回り込むまでの時間")]
        [SerializeField] float cameraRotateBehindSecond = 3;
        [Tooltip("FollowTargetの高さ")]
        public float targetHeight = 1.4f;
        [Tooltip("FollowCameraのDefault距離")]
        public float distance = 3.0f; // Default Distance 
        public float offsetFromWall = 0.05f; // Bring camera away from any colliding objects 
        [Tooltip("FollowCameraのMax Zoom")]
        public float maxDistance = 5f; // Maximum zoom Distance 
        [Tooltip("FollowCameraのMin Zoom")]
        public float minDistance = 1f; // Minimum zoom Distance 
        [Tooltip("FollowCamera X軸の移動速度")]
        public float xSpeed = 200.0f; // Orbit speed (Left/Right) 
        [Tooltip("FollowCamera Y軸の移動速度")]
        public float ySpeed = 200.0f; // Orbit speed (Up/Down) 
        [Tooltip("FollowCamera Y軸方向の最小角度")]
        public float yMinLimit = -80f; // Looking up limit 
        [Tooltip("FollowCamera Y軸の最大角度")]
        public float yMaxLimit = 80f; // Looking down limit 
        public float zoomRate = 40f; // Zoom Speed 
        public float rotationDampening = 0.5f; // Auto Rotation speed (higher = faster) 
        public float zoomDampening = 5.0f; // Auto Zoom speed (Higher = faster) 
        [Tooltip("Cameraが衝突して避けるCollisionのLayers")]
        public LayerMask collisionLayers = -1; // What the camera will collide with 
        public bool lockToRearOfTarget = false; // Lock camera to rear of target 
        public bool allowMouseInputX = true; // Allow player to control camera angle on the X axis (Left/Right) 
        public bool allowMouseInputY = true; // Allow player to control camera angle on the Y axis (Up/Down) 
        [Tooltip("FollowCameraの横にずれる距離")]
        [SerializeField] float FollowCameraXAxisGap = 0.5f;

        [Header("Free Camera Properties")]
        [Tooltip("自由な移動を行えるカメラ")]
        [SerializeField] CinemachineVirtualCamera virtualFreeCamera;
        [Tooltip("FreeCameraの加速度")]
        public float freeCameraSpeed = 0.05f;
        [Tooltip("FreeCameraの開始時の初期位置でTargetからどれだけ離れているか")]
        public float distanceOfStartFreeCamera = 5;

        [Header("Stationary Camera Properties")]
        [Tooltip("StationaryCameraの位置を更新する時間")]
        [SerializeField] float updatePositionDuration = 1;
        /// <summary>
        /// StationaryCameraの位置のアップデートを停止する
        /// </summary>
        [Tooltip("StationaryCameraのEnemyの移動によるVirtualCameraのPositionの行進を停止する")]
        [SerializeField,ReadOnly] public bool lockStationaryCameraPosition = false;

        [Header("Start prepare caemra")]
        [Tooltip("カメラ切り替えの際にカメラの移動をCutにする距離")]
        [SerializeField] float DistanceOfChangeCameraBlendToCut = 20;

        /// <summary>
        /// Followカメラが操作されていないときにターゲットの裏に自動的に回り込む機能を停止
        /// </summary>
        public bool StopAutoRotateBehindTarget = true;

        /// <summary>
        /// カットして瞬時にカメラ移動するblend
        /// </summary>
        private CinemachineBlend cutCameraBlend;
        /// <summary>
        /// カメラ移動死ながらblend
        /// </summary>
        private CinemachineBlend moveCameraBlend;

        /// <summary>
        /// マウスX軸の移動量
        /// </summary>
        public float MouseDeltaX { private set; get; }
        /// <summary>
        /// マウスY軸の移動量
        /// </summary>
        public float MouseDeltaY { private set; get; }
        /// <summary>
        /// キャラクターを中心としたX軸の度数法 Default 90
        /// </summary>
        private float xDeg = 90f;
        /// <summary>
        /// キャラクターを中心としたY軸の度数法 Default 20
        /// </summary>
        private float yDeg = 20f;
        /// <summary>
        /// カメラとの距離  Default 3
        /// </summary>
        private float currentDistance = 3;
        private float desiredDistance = 3;
        private float correctedDistance = 3;
        private bool rotateBehind = false;
        private float pbuffer = 0.0f; //Cooldownpuffer for SideButtons 
        //private float coolDown = 0.5f; //Cooldowntime for SideButtons  
        private float timeDeltaFromControlled = 0;

        /// <summary>
        /// Followなどの際に中心に置かれるUserController
        /// </summary>
        private ThirdPersonUserControl ActiveTPSController;
        /// <summary>
        /// FollowCameraやStationaryCameraで追従する対象
        /// </summary>
        private GameObject TargetToFollow; // Target to follow 
        /// <summary>
        /// Followの際に動くObject
        /// </summary>
        private GameObject FollowObject;

        private bool isOnAnimation = false;

        public CameraMode Mode { private set; get; } = CameraMode.None;
        /// <summary>
        /// Maincameraの移動brain
        /// </summary>
        CinemachineBrain cinemachineBrain;
        /// <summary>
        /// StationaryPositionが変更されるまでの時間
        /// </summary>
        private float timeLastStationaryPositionUpdate = 0;
        private List<Character.UnitController> watchingUnitsAtStationaryMode;
        /// <summary>
        /// StationaryCameraを使う時のraycastのmask
        /// </summary>
        private int stationaryCameraMask;
        /// <summary>
        /// Cinemachineによるカメラ移動の時間
        /// </summary>
        public float CameraChangeDuration
        {
            get => cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.BlendTime;
        }
        /// <summary>
        /// マウスホイールによる距離の調整を停止
        /// </summary>
        private bool allowMouseWheel = true;

        GameManager gameManager;
        /// <summary>
        /// FollowCameraを左右にずらすアニメーションのsequence
        /// </summary>
        Sequence changeFollowCameraCenterPositionAnimation;
        /// <summary>
        /// OverShoulderCaemraのRotationX
        /// </summary>
        public float OverShoulderCameraRotationX { private set; get; }

        /// <summary>
        /// CameraUserControllerが初期化されたか
        /// </summary>
        public bool IsActivated = false;

        /// <summary>
        /// TacticsのUI表示Tablet
        /// </summary>
        public TacticsTablet TacticsTablet;

        #region Base functions
        private void Awake()
        {
            stationaryCameraMask = LayerMask.GetMask(new string[] { "Object", "SeeThrough" });
            gameManager = GameManager.Instance;

        }

        // Start is called before the first frame update
        void Start()
        {
            Vector3 angles = mainCamera.transform.eulerAngles;
            xDeg = angles.x;
            yDeg = angles.y;
            currentDistance = distance;
            desiredDistance = distance;
            correctedDistance = distance;

            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
            if (cinemachineBrain == null)
                PrintError("CinemachineBrain is not attached to MainCamera");

            SetCutBlend();

            // Make the rigid body not change rotation 
            //        if (rigidbody)
            //            rigidbody.freezeRotation = true;

            if (lockToRearOfTarget)
                rotateBehind = true;

            if (ActiveTPSController == null && TargetToFollow != null)
                ActiveTPSController = TargetToFollow.GetComponent<ThirdPersonUserControl>();

            IsActivated = true;
        }

        private void LateUpdate()
        {
            if (gameManager != null && (gameManager.debugController.IsActive || gameManager.StartCanvasController.IsEnable))
                return;

            if (Mode == CameraMode.Free)
            {
                if (!enableCameraControlling)
                    return;
                FreeCameraUpdate(ActiveTPSController);
            }
            else if (Mode == CameraMode.Follow)
            {
                if (TargetToFollow == null || isOnAnimation || !enableCameraControlling)
                    return;
                FollowCameraUpdate(FollowObject, TargetToFollow);
                if (UserController.ChangeFollowCameraPositionRightOrLeft)
                    ChangeFollowCameraXAxisGap();
            }
            else if (Mode == CameraMode.FollowMortar)
            {
                if (TargetToFollow == null || isOnAnimation || !enableCameraControlling)
                    return;
                FollowCameraUpdate(FollowObject, TargetToFollow);
            }
            else if (Mode == CameraMode.Stationary)
            {
                if (TargetToFollow == null || !enableCameraControlling)
                    return;
                UpdatePositionStationaryCameraAtNear();
            }
            else if (Mode == CameraMode.OverShoulder)
            {
                if (TargetToFollow == null || isOnAnimation || !enableCameraControlling)
                    return;
                UpdateOverShoulderCamera();
            }
            else if (Mode == CameraMode.OverShoulderFar)
            {
                if (TargetToFollow == null || isOnAnimation || !enableCameraControlling)
                    return;
                UpdateOverShoulderCameraFar();
            }
                
        }
        #endregion

        #region Tablet Camera Mode
        /// <summary>
        /// StartTabletを見る形のカメラモード
        /// </summary>
        /// <returns>Tabletがカメラに映る位置</returns>
        public void ChangeModeStartTablet()
        {
            Mode = CameraMode.StartTablet;

            if (TacticsTablet == null)
                PrintError("TabletCinemachineVirtualCamera is not set");
            
            StartCoroutine(MakeVirtualCameraToActive(TacticsTablet.CinemachineVirtualCamera, null));
        }

        /// <summary>
        /// すべてのTabletCaemraのPriortyを0にする
        /// </summary>
        private void ClearAllTabletCamera()
        {
            TacticsTablet.CinemachineVirtualCamera.Priority = 0;
        }
        #endregion

        #region Follow Unit Camera Mode

        public IEnumerator ChangeModeFollowTarget(ThirdPersonUserControl thirdPersonUserControl)
        {
            if (Mode == CameraMode.Follow && thirdPersonUserControl == ActiveTPSController) yield break ;

            Mode = CameraMode.Follow;
            allowMouseWheel = true;

            SetFollowCameraXAxisGap(thirdPersonUserControl, false);
            StartCoroutine(MakeVirtualCameraToActive(thirdPersonUserControl.followCamera, thirdPersonUserControl));
            yield return StartCoroutine(_ChangeModeFollowTarget(thirdPersonUserControl.followCamera, 
                                                                thirdPersonUserControl.FollowCameraParent,
                                                                thirdPersonUserControl.FollowCameraCenter));
        }

        /// <summary>
        /// FollowするTargetを切り替える
        /// </summary>
        /// <param name="target"></param>
        private IEnumerator _ChangeModeFollowTarget(CinemachineVirtualCamera followCamera, 
                                                    GameObject followObject,
                                                    GameObject targetToFollow,
                                                    float defaultDistance = 3)
        {

            allowMouseInputY = true;
            timeDeltaFromControlled = 0;
            this.TargetToFollow = targetToFollow;
            FollowObject = followObject;

            // カメラ位置をデフォルトに
            RotateBehindTarget(false);

            yDeg = 20f;
            currentDistance = defaultDistance;
            desiredDistance = defaultDistance;
            correctedDistance = defaultDistance;

            FollowCameraUpdate(followObject, targetToFollow);
            

            yield return new WaitForSeconds(CameraChangeDuration);
            enableCameraControlling = true;
            isOnAnimation = false;
        }

        /// <summary>
        /// カメラ位置をマウス位置からアップデートする
        /// </summary>
        /// <param name="followObject">followして移動するobject</param>
        /// <param name="target">followされて周りを回られるObject</param>
        private void FollowCameraUpdate(GameObject followObject, GameObject target)
        {
            //pushbuffer 
            if (pbuffer > 0)
                pbuffer -= Time.deltaTime;
            if (pbuffer < 0)
                pbuffer = 0;

            // If either mouse buttons are down, let the mouse govern camera position 
            if (GUIUtility.hotControl == 0)
            {
                //Check to see if mouse input is allowed on the axis 
                MouseDeltaX = 0f;
                if (allowMouseInputX)
                {
                    MouseDeltaX = UserController.MouseDeltaX * xSpeed * 0.02f;
                    xDeg += MouseDeltaX;
                }
                else
                {
                    //RotateBehindTarget(true);
                }
                MouseDeltaY = UserController.MouseDeltaY * ySpeed * 0.02f;
                if (allowMouseInputY)
                    yDeg -= MouseDeltaY;

                //Interrupt rotating behind if mouse wants to control rotation 
                if (!lockToRearOfTarget)
                    rotateBehind = false;

                if (Mode != CameraMode.OverShoulder)
                {
                    // ease behind the target if the character is not controlled
                    bool isNotControlled = MouseDeltaX == 0 && MouseDeltaY == 0;
                    if (isNotControlled)
                        timeDeltaFromControlled += Time.deltaTime;
                    else
                        timeDeltaFromControlled = 0;

                    if ((rotateBehind || timeDeltaFromControlled > cameraRotateBehindSecond) && !StopAutoRotateBehindTarget)
                    {
                        //RotateBehindTarget(true);
                    }
                }
            }

            yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);
            // Set camera rotation 
            Quaternion rotation = Quaternion.Euler(yDeg, xDeg, 0);

            // Calculate the desired distance 
            if (allowMouseWheel)
            {
                desiredDistance -= UserController.MouseWheel * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);
                desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
                correctedDistance = desiredDistance;
            }

            // Calculate desired camera position 
            Vector3 position = target.transform.position - (rotation * Vector3.forward * desiredDistance);

            // Check for collision using the true target's desired registration point as set by user using height 
            Vector3 trueTargetPosition = new Vector3(target.transform.position.x,
                target.transform.position.y + targetHeight, target.transform.position.z);

            // ? カメラがCollisionを持つ物に遮られた場合のいち補正?
            // If there was a collision, correct the camera position and calculate the corrected distance 
            var isCorrected = false;
            if (Physics.Linecast(trueTargetPosition, position, out RaycastHit collisionHit, collisionLayers))
            {
                // Calculate the distance from the original estimated position to the collision location, 
                // subtracting out a safety "offset" distance from the object we hit.  The offset will help 
                // keep the camera from being right on top of the surface we hit, which usually shows up as 
                // the surface geometry getting partially clipped by the camera's front clipping plane. 
                correctedDistance = Vector3.Distance(trueTargetPosition, collisionHit.point) - offsetFromWall;
                isCorrected = true;
            }

            // For smoothing, lerp distance only if either distance wasn't corrected, or correctedDistance is more than currentDistance
            currentDistance = !isCorrected || correctedDistance > currentDistance
                ? Mathf.Lerp(currentDistance, correctedDistance, Time.deltaTime * zoomDampening)
                : correctedDistance;

            // Keep within limits 
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            // Recalculate position based on the new currentDistance 
            position = target.transform.position - (rotation * Vector3.forward * currentDistance);

            //Finally Set rotation and position of camera 
            followObject.transform.SetPositionAndRotation(position, rotation);
            //tpsControl.followCamera.transform.rotation = rotation;
        }

        /// <summary>
        /// Followcamraを撮影中のUnitの背後に移動させる
        /// </summary>
        /// <param name="lerp">移動をなめらかにアニメーションさせるか</param>
        public void RotateBehindTarget(bool lerp)
        {
            float targetRotationAngle = TargetToFollow.transform.eulerAngles.y;
            float currentRotationAngle = mainCamera.transform.eulerAngles.y;
            float lerpAngle = Mathf.LerpAngle(currentRotationAngle, targetRotationAngle, rotationDampening * Time.deltaTime);
            xDeg = lerp ? lerpAngle : targetRotationAngle;

            // Stop rotating behind if not completed 
            if (targetRotationAngle == currentRotationAngle || !lerp)
            {
                if (!lockToRearOfTarget)
                    rotateBehind = false;
            }
            else
                rotateBehind = true;
        }

        private float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f)
                angle += 360f;
            if (angle > 360f)
                angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        /// <summary>
        /// FollowCameraの位置を左右に変更する
        /// </summary>
        /// <returns></returns>
        private void ChangeFollowCameraXAxisGap()
        {
            var setting = gameManager.StaticData.CommonSetting;
            setting.IsFollowCameraCenterRight = !setting.IsFollowCameraCenterRight;
            SetFollowCameraXAxisGap(ActiveTPSController, true);
        }

        /// <summary>
        /// FollowCamraの位置をx軸でずらす
        /// </summary>
        /// <param name="animation"></param>
        private void SetFollowCameraXAxisGap(ThirdPersonUserControl activeTPSControl, bool animation)
        {
            var setting = gameManager.StaticData.CommonSetting;
            var gap = setting.IsFollowCameraCenterRight ? FollowCameraXAxisGap : -FollowCameraXAxisGap;
            if (changeFollowCameraCenterPositionAnimation != null && changeFollowCameraCenterPositionAnimation.IsActive())
                changeFollowCameraCenterPositionAnimation.Kill();
            if (animation)
            {
                changeFollowCameraCenterPositionAnimation = DOTween.Sequence();
                changeFollowCameraCenterPositionAnimation.Append(activeTPSControl.followCamera.transform.DOLocalMoveX(gap, 0.3f));
                changeFollowCameraCenterPositionAnimation.Play();
            }
            else
            {
                activeTPSControl.followCamera.transform.localPosition = new Vector3(gap, 0f, 0f);
            }
        }

        #endregion

        #region Aiming Camera Mode
        /// <summary>
        /// エイムモードを主観視点に変更する
        /// </summary>
        /// <param name="active"></param>
        public void ChangeModeSubjective()
        {
            if (ActiveTPSController == null || Mode == CameraMode.Subjective) return;

            Mode = CameraMode.Subjective;
            StartCoroutine(MakeVirtualCameraToActive(ActiveTPSController.aimCamera, ActiveTPSController));
            allowMouseInputY = false;
        }
        #endregion

        #region Over shoulder camera mode
        /// <summary>
        /// 肩越しで操作を行うカメラモード
        /// </summary>
        /// <param name="thirdPersonUserControl"></param>
        public void ChangeModeOverShoulder(UnitController active)
        {
            ActiveTPSController = active.TpsController;
            if (Mode == CameraMode.OverShoulder) return;

            ActiveTPSController.OverShoulderCameraParent.transform.rotation = Quaternion.Euler(ActiveTPSController.OverShoulderCameraDefaultXRotation, 0, 0);
            OverShoulderCameraRotationX = ActiveTPSController.OverShoulderCameraDefaultXRotation;

            SetEraceInOutBlend();
            Mode = CameraMode.OverShoulder;
            allowMouseWheel = false;
            Print(ActiveTPSController.OverShoulderCamera, ActiveTPSController) ;
            StartCoroutine(MakeVirtualCameraToActive(ActiveTPSController.OverShoulderCamera, ActiveTPSController));
        }

        /// <summary>
        /// 少し離れた箇所のカメラ位置
        /// </summary>
        /// <param name="thirdPersonUserControl"></param>
        public void ChangeModeOverShoulderFar(UnitController active)
        {
            ActiveTPSController = active.TpsController;
            if (Mode == CameraMode.OverShoulderFar) return;

            ActiveTPSController.OverShoulderCameraParent.transform.rotation = Quaternion.Euler(ActiveTPSController.OverShoulderCameraDefaultXRotation, 0, 0);
            OverShoulderCameraRotationX = ActiveTPSController.OverShoulderCameraDefaultXRotation;

            SetEraceInOutBlend();
            Mode = CameraMode.OverShoulderFar;
            allowMouseWheel = false;
            //StartCoroutine(_ChangeModeFollowTarget(thirdPersonUserControl, 2));
            StartCoroutine(MakeVirtualCameraToActive(ActiveTPSController.OverShoulderCameraFar, ActiveTPSController));
        }

        /// <summary>
        /// OverShoulderCameraの上下移動をUpdateする
        /// </summary>
        public void UpdateOverShoulderCamera()
        {
            MouseDeltaY = UserController.MouseDeltaY * ySpeed * 0.02f;
            OverShoulderCameraRotationX -= MouseDeltaY;
            if (OverShoulderCameraRotationX >= ActiveTPSController.OverShoulderCameraMaxXRotation)
                OverShoulderCameraRotationX = ActiveTPSController.OverShoulderCameraMaxXRotation;
            else if (OverShoulderCameraRotationX <= ActiveTPSController.OverShoulderCameraMinXRotation)
                OverShoulderCameraRotationX = ActiveTPSController.OverShoulderCameraMinXRotation;
            ActiveTPSController.OverShoulderCameraParent.localRotation = Quaternion.Euler(OverShoulderCameraRotationX, 0, 0);
        }

        /// <summary>
        /// OverShoulderCameraFarの上下移動をUpdateする
        /// </summary>
        public void UpdateOverShoulderCameraFar()
        {
              MouseDeltaY = UserController.MouseDeltaY * ySpeed * 0.02f;
            OverShoulderCameraRotationX -= MouseDeltaY;
            if (OverShoulderCameraRotationX >= ActiveTPSController.OverShoulderCameraMaxXRotation)
                OverShoulderCameraRotationX = ActiveTPSController.OverShoulderCameraMaxXRotation;
            else if (OverShoulderCameraRotationX <= ActiveTPSController.OverShoulderCameraMinXRotation)
                OverShoulderCameraRotationX = ActiveTPSController.OverShoulderCameraMinXRotation;
            ActiveTPSController.OverShoulderCameraFarParent.localRotation = Quaternion.Euler(OverShoulderCameraRotationX, 0, 0);
        }
        #endregion

        #region Stationary camera at near position
        /// <summary>
        /// 射線とかは考えずにただ単に近いUnitの場所から敵を追跡する
        /// </summary>
        /// <param name="target"></param>
        /// <param name="callerName"></param>
        public void ChangeModeStationaryAtNear(UnitController target, [CallerMemberName] string callerName = "")
        {
            Print(FuncName(), target, callerName);
            this.Mode = CameraMode.Stationary;

            TargetToFollow = target.headUpIconPosition.gameObject;

            watchingUnitsAtStationaryMode = unitsController.UnitsList.FindAll(u => u.Attribute != target.Attribute);
            UpdatePositionStationaryCameraAtNear();
        }

        /// <summary>
        /// Stationary cameraを位置をtargetToFollowに最も近いものに変更する
        /// </summary>
        /// <returns></returns>
        private bool UpdatePositionStationaryCameraAtNear()
        {
            if (watchingUnitsAtStationaryMode.Count == 0) return false;
            if (lockStationaryCameraPosition ||
                Time.time - timeLastStationaryPositionUpdate < updatePositionDuration)
                return false;
            List<(UnitController unit, int dist)> distances = watchingUnitsAtStationaryMode.ConvertAll(u => 
            { 
                return (u, (int)Vector3.Distance(u.stationaryCamera.transform.position, TargetToFollow.transform.position)); 
            });

            distances.RemoveAll(d => d.dist == int.MaxValue);
            if (distances.Count == 0)
            {
                watchingUnitsAtStationaryMode.ForEach(u => u.stationaryCamera.LookAt = null);
                return false;
            }
            distances.Sort((a, b) => a.dist - b.dist);

            distances.Select((unit, index) => (unit, index)).ToList().ForEach(item =>
            {
                if (item.index == 0)
                {
                    item.unit.unit.stationaryCamera.LookAt = TargetToFollow.transform;
                    StartCoroutine(MakeVirtualCameraToActive(item.unit.unit.stationaryCamera, null));
                }
                else
                {
                    item.unit.unit.stationaryCamera.Priority = 0;
                }
            });
            return true;
        }
        #endregion

        // Deprecated
        #region Free camera mode　*Deprecated*
        [Obsolete("This function is deprecated and should no longer be used.")]
        /// <summary>
        /// カメラをフリーモードにする
        /// </summary>
        /// <param name="active"></param>
        public void ChangeModeFree([CallerMemberName] string callerName = "")
        {
            // TODO FreeCameraの移動制限(Activeから一定以上離れれない)をつけるか？
            // freeCameraをTargetの後ろdistanceOfStartFreeCameraだけ離れた位置に初期化
            var rad = ActiveTPSController.transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
            rad -= 180 * Mathf.Deg2Rad;

            Print(FuncName(), callerName);

            var newPos = ActiveTPSController.transform.position;
            newPos.x += Mathf.Sin(rad) * distanceOfStartFreeCamera;
            newPos.z += Mathf.Cos(rad) * distanceOfStartFreeCamera;
            newPos.y = virtualFreeCamera.transform.position.y;

            virtualFreeCamera.transform.position = newPos;
            virtualFreeCamera.transform.LookAt(ActiveTPSController.transform.position);

            Mode = CameraMode.Free;
            ActiveTPSController.IsTPSControllActive = false;
            StartCoroutine(MakeVirtualCameraToActive(virtualFreeCamera, null));
            if (ActiveTPSController != null)
            {
                ActiveTPSController.followCamera.Priority = 1;
                ActiveTPSController.aimCamera.Priority = 1;
            }
        }

        [Obsolete("This function is deprecated and should no longer be used.")]
        /// <summary>
        /// Free Cameraの移動
        /// </summary>
        /// <param name="activeTPSController"></param>
        private void FreeCameraUpdate(ThirdPersonUserControl activeTPSController)
        {
            MouseDeltaX = UserController.MouseDeltaX * xSpeed * 0.02f;
            MouseDeltaY = UserController.MouseDeltaY * ySpeed * 0.02f;
            var _rotation = virtualFreeCamera.transform.rotation.eulerAngles;

            _rotation.x -= MouseDeltaY;
            _rotation.y += MouseDeltaX;
            _rotation.z = 0;

            if (_rotation.x > 270)
                _rotation.x -= 360;

            if (_rotation.x > 85)
                _rotation.x = 85;
            else if (_rotation.x < -85)
                _rotation.x = -85;

            virtualFreeCamera.transform.rotation = Quaternion.Euler(_rotation);

            if (UserController.KeyVertical != 0 || UserController.KeyHorizontal != 0)
            {

                var forward = Mathf.Deg2Rad * virtualFreeCamera.transform.rotation.eulerAngles.y;
                var forwardDeltaX = Mathf.Sin(forward) * UserController.KeyVertical;
                var forwardDeltaZ = Mathf.Cos(forward) * UserController.KeyVertical;
                var vectForward = new Vector2(forwardDeltaX, forwardDeltaZ);

                var side = Mathf.Deg2Rad * virtualFreeCamera.transform.rotation.eulerAngles.y;
                side += 90 * Mathf.Deg2Rad;
                var sideDeltaX = Mathf.Sin(side) * UserController.KeyHorizontal;
                var sideDeltaZ = Mathf.Cos(side) * UserController.KeyHorizontal;
                var vectSide = new Vector2(sideDeltaX, sideDeltaZ);

                var vect = vectForward + vectSide;
                vect *= Max(Mathf.Abs(UserController.KeyHorizontal), Mathf.Abs(UserController.KeyVertical)) * freeCameraSpeed;

                var newPos = virtualFreeCamera.transform.position;
                newPos.x += vect.x;
                newPos.z += vect.y;

                virtualFreeCamera.transform.position = newPos;
            }
        }
        #endregion

        #region Stationary Camera Mode *Deprecated*
        [Obsolete("This function is deprecated and should no longer be used.")]
        /// <summary>
        /// Targetに向かってPositionsから定点観測するカメラモード
        /// </summary>
        /// <param name="target">定点観測で追うtarget</param>
        /// <param name="watchPositions">定点観測の位置 最も近い場所から見る</param>
        public void ChangeModeStationary(UnitController target, [CallerMemberName] string callerName = "")
        {
            Print(FuncName(), target, callerName);
            
            TargetToFollow = target.headUpIconPosition.gameObject;
            this.Mode = CameraMode.Stationary;

            watchingUnitsAtStationaryMode = unitsController.UnitsList.FindAll(u => u.Attribute != target.Attribute);

            ActiveTPSController.followCamera.Priority = 0;
            ActiveTPSController.aimCamera.Priority = 0;
            virtualFreeCamera.Priority = 0;
            ClearAllTabletCamera();

            UpdatePositionStationaryCamera();
        }

        [Obsolete("This function is deprecated and should no longer be used.")]
        /// <summary>
        /// 定点カメラの位置をアップデートする
        /// </summary>
        private bool UpdatePositionStationaryCamera()
        {
            if (lockStationaryCameraPosition ||
                Time.time - timeLastStationaryPositionUpdate < updatePositionDuration)
                return false;

            if (watchingUnitsAtStationaryMode.Count == 0)
                return false;

            var mask = LayerMask.GetMask(new string[] { "Object", "SeeThrough" });
            List<(UnitController unit, int dist)> distances = watchingUnitsAtStationaryMode.ConvertAll(u =>(u, ShootRaycast(u.stationaryCamera.transform.position)));

            distances.RemoveAll(d => d.dist == int.MaxValue);
            if (distances.Count == 0)
            {
                watchingUnitsAtStationaryMode.ForEach(u => u.stationaryCamera.LookAt = null);
                return false;
            }
            distances.Sort((a, b) => a.dist - b.dist);

            distances.Select((unit, index) => (unit, index)).ToList().ForEach(item =>
            {
                if (item.index == 0)
                {
                    item.unit.unit.stationaryCamera.LookAt = TargetToFollow.transform;
                    item.unit.unit.stationaryCamera.Priority = 10;
                }
                else
                {
                    item.unit.unit.stationaryCamera.Priority = 0;
                }
            });

            timeLastStationaryPositionUpdate = Time.time;
            return true;
        }

        [Obsolete("This function is deprecated and should no longer be used.")]
        /// <summary>
        /// すべてのStationaryCameraのPriorityを0にする
        /// </summary>
        private void ClearAllStationaryCamera()
        {
            if (unitsController.UnitsList == null) return;
            unitsController.UnitsList.ForEach(u => u.stationaryCamera.Priority = 0);
        }

        [Obsolete("This function is deprecated and should no longer be used.")]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public int ShootRaycast(Vector3 from)
        {
            var direction = TargetToFollow.transform.position - from;
            var ray = new Ray(from, direction * 30);
            //Debug.DrawRay(ray.origin, ray.direction * 30, Color.green);
            if (Physics.Raycast(ray, out var hit, 100, stationaryCameraMask))
            {
                if (hit.collider.transform.Equals(TargetToFollow.transform))
                {
                    return (int)(hit.distance * 100f);
                }
                else
                {
                    // print(hit.collider.gameObject);
                }
            }
            return int.MaxValue;
        }
        #endregion

        #region Follow Mortar Camera mode Deprecated
        public IEnumerator ChangeModeFollowMortar(MortarGimmick mortorGimmick)
        {
            if (Mode == CameraMode.FollowMortar) yield break;
            Mode = CameraMode.FollowMortar;
            StartCoroutine(MakeVirtualCameraToActive(mortorGimmick.MortarVirtualCamera, null));
            yield return StartCoroutine(_ChangeModeFollowTarget(mortorGimmick.MortarVirtualCamera, mortorGimmick.FollowObject, mortorGimmick.FollowTarget));
        }
        #endregion
        // +++++++++++++

        #region Camera move animations
        /// <summary>
        /// CinemachineBlendのカメラ遷移にカットを使用
        /// </summary>
        private void SetCutBlend()
        {
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Style = CinemachineBlendDefinition.Style.Cut;
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Time = 0;
        }

        /// <summary>
        /// CinemachineBlendのカメラ遷移にEaseInOut移動を使用
        /// </summary>
        private void SetEraceInOutBlend()
        {
            Print(cinemachineBrain, cinemachineBrain.m_CustomBlends.m_CustomBlends.Length, cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend);
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            cinemachineBrain.m_CustomBlends.m_CustomBlends[0].m_Blend.m_Time = MoveCameraDuration;
        }

        /// <summary>
        /// 与えられたvirtualCameraをアクティブにして、それ以外のvirtualCameraを非アクティブにする
        /// </summary>
        /// <param name="cam">アクティブにするVirtualCamera</param>
        /// <param name="thirdPersonUserControl">新しいTPSControllerがあればこれをセットする 以前のTPSCamのCameraは非アクティブになる</param>
        private IEnumerator MakeVirtualCameraToActive(CinemachineVirtualCamera cam, ThirdPersonUserControl thirdPersonUserControl)
        {
            if (thirdPersonUserControl != ActiveTPSController)
            {
                EnableCamerasPriority(thirdPersonUserControl, cam);
                EnableCamerasPriority(ActiveTPSController, cam);
                ActiveTPSController = thirdPersonUserControl;
            }
            else
            {
                EnableCamerasPriority(ActiveTPSController, cam);
            }

            static bool EnableCamerasPriority(ThirdPersonUserControl thirdPersonUserControl, CinemachineVirtualCamera target)
            {
                if (thirdPersonUserControl == null) return false;
                var output = false;
                thirdPersonUserControl.CinemachineVirtualCameras.ForEach(c =>
                {
                    if (c == target)
                    {
                        output = true;
                        c.Priority = 10;
                    }
                    else
                    {
                        c.Priority = 0;
                    }
                        
                });
                return output;
            }

            virtualFreeCamera.Priority = virtualFreeCamera == cam ? 10 : 0;
            TacticsTablet.CinemachineVirtualCamera.Priority = TacticsTablet.CinemachineVirtualCamera == cam ? 10 : 0;

            var cameraModeCut = false;
            var distOldToNewCamera = Vector3.Distance(mainCamera.transform.position, cam.transform.position);
            var direction = cam.transform.position - mainCamera.transform.position;
            if (distOldToNewCamera > DistanceOfChangeCameraBlendToCut)
                cameraModeCut = true;

            var ray = new Ray(mainCamera.transform.position, direction * 30);
            if (Physics.Raycast(ray, out var hit, 100, stationaryCameraMask))
            {
                if (hit.distance < distOldToNewCamera)
                    cameraModeCut = true;
            }
            
            if (cameraModeCut)
            {
                var fadeInOutCanvas = gameManager.FadeInOutCanvas;
                SetCutBlend();
                yield return StartCoroutine(fadeInOutCanvas.Show(0.25f));
                cam.Priority = 10;
                yield return StartCoroutine(fadeInOutCanvas.Hide(0.25f));
            }
            else
            {
                SetEraceInOutBlend();
                cam.Priority = 10;
            }
        }
        #endregion

        /// <summary>
        /// カメラの動作モード
        /// </summary>
        public enum CameraMode
        {
            None,
            /// <summary>
            /// キーによって一定高度を移動するカメラ
            /// </summary>
            Free,
            /// <summary>
            //　肩越し視点
            /// </summary>
            OverShoulder,
            /// <summary>
            /// 肩越しで少し離れた視点
            /// </summary>
            OverShoulderFar,
            /// <summary>
            /// 主観視点
            /// </summary>
            Subjective,
            /// <summary>
            /// TPSでUnitの周りを周回する
            /// </summary>
            Follow,
            /// <summary>
            /// Mortarの周囲でカメラを移動
            /// </summary>
            FollowMortar,
            /// <summary>
            /// 位置から定点観測
            /// </summary>
            Stationary,
            /// <summary>
            /// 開始地点のTabletを見るようにCameraが設置されている
            /// </summary>
            StartTablet,
        }
    }
}