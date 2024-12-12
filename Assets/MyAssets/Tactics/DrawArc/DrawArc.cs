using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DG.Tweening;
using System.Linq;
using static Utility;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System;
using Cinemachine.Utility;
using Tactics.Character;

namespace Arc
{

    /// <summary>
    /// 初速度又はHit地点から放物線を描写する
    /// </summary>
    public class DrawArc : MonoBehaviour
    {
        [Tooltip("放物線の開始座標")]
        [SerializeField] public Transform arcStartTransform;

        [SerializeField] float minHitDistance = 1;

        [SerializeField, Tooltip("放物線のマテリアル")]
        private Material arcMaterial;

        [SerializeField, Tooltip("着弾地点に表示するマーカーのPrefab")]
        private GameObject pointerPrefab;

        [SerializeField, Tooltip("マーカーの半径")]
        public float PointerRadius = 1f;

        [SerializeField, Tooltip("放物線の幅")]
        private float arcWidth = 0.02F;

        [Tooltip("放物線を構成する線分の数 数が多いほど滑らかになる")]
        [SerializeField]
        private int SegmentCount = 120;

        [Header("Calc Type of Arc")]
        [SerializeField] internal CalcType calcType;

        [Header("ThrowForce")]
        [Tooltip("投射角度から計算した初速度")]
        [SerializeField, ReadOnly] private float ThrowForce = 1;

        [Tooltip("投げる角度 calcType == .FromVelocityで使用される")]
        [SerializeField] public float ThrowAngle = 15;

        [Tooltip("投げる角度の係数 calcType == .FromVelocityで使用される")]
        [SerializeField] public float ThrowAnglePower = 1;

        [Tooltip("弾の初速度")]
        [SerializeField] public Vector3 InitialVelocity;

        [Tooltip("ArcのHit位置 calcType == .FromHitPositionの場合setし.FromVelocityの場合getできる")]
        [SerializeField] public Vector3 HitPosition;

        [Tooltip("ArcのHit位置から軌道を予想するときのTimeStampの変化")]
        [SerializeField] AnimationCurve timeStampTransition;

        /// <summary>
        /// 予測値で放物線がHitするgameobject
        /// </summary>
        public GameObject HitObject { private set; get; }


        [Tooltip("UnitがItemを投げる時の角度に対するForceの増加グラフ\n" +
            "Angleのとる値はUnitがどれだけ俯角を取れるかで決まるがおおよそ-1~1の値をとる")]
        public AnimationCurve ThrowItemAngleAndForceCurve;


        private bool _showArc;
        public bool ShowArc {
            get => _showArc;
            set
            {
                _showArc = value;
                if (!_showArc)
                    DisappearArc();
                else
                    AppearArc();
            }
        }

        /// <summary>
        /// HitするまでのArcの位置
        /// </summary>
        public List<Vector3> ArcPositions
        {
            get
            {
                return lineRenderers.ToList().Slice(0, hitSegmentIndex+2).ConvertAll(l => l.GetPosition(0));
            }
        }
        /// <summary>
        /// Arcを構成する各線分の開始時間のリスト
        /// </summary>
        public List<float> StartTimeAtArcLines = new List<float>();
        /// <summary>
        /// Arcを構成する各線分の終了時間のリスト
        /// </summary>
        public List<float> EndTimeAtArcLines = new List<float>();

        private int hitSegmentIndex;

        private LayerMask arcHitLayer;
        private LayerMask groundLayer;
        private LayerMask objectLayer;

        /// <summary>
        /// 放物線を何秒分計算するか
        /// </summary>
        private float predictionTime = 6.0F;

        /// <summary>
        /// 放物線を構成するLineRenderer
        /// </summary>
        private LineRenderer[] lineRenderers;

        /// <summary>
        /// 着弾点のマーカーのオブジェクト
        /// </summary>
        private GameObject pointerObject;
        private Material pointerMaterial;
        private bool pointerIsActive = true;

        private bool disappearingArc = false;

        GeneralParameter generalParameter;

        void Start()
        {
            arcHitLayer = LayerMask.GetMask("Ground", "Object");
            groundLayer = LayerMask.GetMask("Ground");
            objectLayer = LayerMask.GetMask("Object");
            generalParameter = GameManager.Instance.GeneralParameter;

            // 放物線のLineRendererオブジェクトを用意
            CreateLineRendererObjects();

            // マーカーのオブジェクトを用意
            pointerObject = Instantiate(pointerPrefab, Vector3.zero, Quaternion.identity, transform);
            pointerObject.SetActive(false);
            pointerMaterial = pointerObject.GetComponentInChildren<MeshRenderer>().material;
            pointerMaterial.SetColor("_Color", Color.white);
            
        }

        private void FixedUpdate()
        {
            if (ShowArc)
            {
                //if (Vector3.Distance(hitPosition, arcStartTransform.position) < minHitDistance)
                //{
                //    if (!disappearingArc)
                //    {
                //        DisappearArc();
                //    }
                //}
                //else if (disappearingArc)
                //{
                //    AppearArc();
                //}

                if (calcType == CalcType.FromHitPosition)
                {
                    InitialVelocity = GetInitialVelocityHitPoint(HitPosition);
                }
                else if (calcType == CalcType.FromVelocity)
                {
                    var _velocity = arcStartTransform.transform.forward;
                    _velocity.y = ThrowAngle * ThrowAnglePower;
                    ThrowForce = ThrowItemAngleAndForceCurve.Evaluate(_velocity.y);
                    InitialVelocity = _velocity * ThrowForce;
                }
                    
                if (float.IsInfinity(InitialVelocity.x))
                    return;

                // 放物線を表示
                float timeStep = predictionTime / SegmentCount;
                bool lineEnable = true;
                float hitTime = float.MaxValue;

                StartTimeAtArcLines.Clear();
                EndTimeAtArcLines.Clear();
                for (int i = 0; i < SegmentCount; i++)
                {
                    // 線の座標を更新
                    float startTime = timeStep * i;
                    float endTime = startTime + timeStep;
                    SetLineRendererPosition(i, startTime, endTime, lineEnable);
                    if (lineEnable)
                    {
                        StartTimeAtArcLines.Add(startTime);
                        EndTimeAtArcLines.Add(endTime);
                    }
                    
                    // 衝突判定
                    if (lineEnable)
                    {
                        (hitTime, HitObject) = GetArcHitTime(startTime, endTime);
                        if (HitObject != null)
                        {
                            hitSegmentIndex = i;
                            lineEnable = false; // 衝突したらその先の放物線は表示しない
                        }
                    }
                }

                // マーカーの表示
                if (hitTime != float.MaxValue)
                {
                    Vector3 hitPosition = GetArcPositionAtTime(hitTime);
                    ShowPointer(hitPosition);
                    if (calcType == CalcType.FromVelocity)
                        HitPosition = hitPosition;
                }

            }
        }

        /// <summary>
        /// ArcをFadeinさせる
        /// </summary>
        private void AppearArc()
        {
            if (lineRenderers == null)
                return;

            disappearingArc = false;
            pointerIsActive = true;

            HitPosition = arcStartTransform.position;
            var seq = DOTween.Sequence();
            seq.Append(arcMaterial.DOColor(Color.white, 1f));
            seq.Join(pointerMaterial.DOColor(Color.white, 1f));
            seq.Play();
        }

        /// <summary>
        /// ArcをFadeoutさせる
        /// </summary>
        private void DisappearArc()
        {
            if (lineRenderers == null)
                return;

            disappearingArc = true;
            pointerIsActive = false;

            var fadeSequence = DOTween.Sequence();
            // 放物線とマーカーを表示しない
            fadeSequence.Join(arcMaterial.DOFade(0, 0.5f).OnComplete(() =>
            {
                lineRenderers.ToList().ForEach((l) =>
                {
                    l.enabled = false;
                });
            }));
            fadeSequence.Join(pointerMaterial.DOColor(Color.clear, 0.5f).OnComplete(() => pointerObject.SetActive(false)));
            fadeSequence.Play();
        }

        /// <summary>
        /// 指定時間に対するアーチの放物線上の座標を返す
        /// </summary>
        /// <param name="time">経過時間</param>
        /// <returns>座標</returns>
        private Vector3 GetArcPositionAtTime(float time)
        {
            return (arcStartTransform.position + ((InitialVelocity * time) + (0.5f * time * time) * Physics.gravity));
        }

        /// <summary>
        /// 着弾位置からArcStartPositionから打ち出された物体のVelocityを計算
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Vector3 GetInitialVelocityHitPoint(Vector3 hitPosition)
        {
            var dist = Vector3.Distance(hitPosition, arcStartTransform.position) /5;
            var time = timeStampTransition.Evaluate(dist);
            if (time == 0)
                return Vector3.positiveInfinity;
            return ((hitPosition - arcStartTransform.position) / time) - (Physics.gravity * 0.5f * time);
        }

        /// <summary>
        /// LineRendererの座標を更新
        /// </summary>
        /// <param name="index"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        private void SetLineRendererPosition(int index, float startTime, float endTime, bool draw = true)
        {
            lineRenderers[index].SetPosition(0, GetArcPositionAtTime(startTime));
            lineRenderers[index].SetPosition(1, GetArcPositionAtTime(endTime));
            lineRenderers[index].enabled = draw;
        }

        /// <summary>
        /// LineRendererオブジェクトを作成
        /// </summary>
        private void CreateLineRendererObjects()
        {

            lineRenderers = new LineRenderer[SegmentCount];
            for (int i = 0; i < SegmentCount; i++)
            {
                GameObject newObject = new GameObject("LineRenderer_" + i);
                newObject.transform.SetParent(transform);
                lineRenderers[i] = newObject.AddComponent<LineRenderer>();

                // 光源関連を使用しない
                lineRenderers[i].receiveShadows = false;
                lineRenderers[i].reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                lineRenderers[i].lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                lineRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // 線の幅とマテリアル
                lineRenderers[i].material = arcMaterial;
                if (i == 0)
                    lineRenderers[i].startWidth = 0;
                else
                    lineRenderers[i].startWidth = arcWidth;
                lineRenderers[i].endWidth = arcWidth;
                lineRenderers[i].numCapVertices = 5;
                lineRenderers[i].enabled = false;
            }

            arcMaterial.DOFade(0, 0);
        }

        /// <summary>
        /// 現在のHitPositionからPointerRadius内にいるUnitを返す
        /// </summary>
        /// <param name="allUnits"></param>
        /// <returns></returns>
        public List<UnitController> GetUnitsWithinRadius(List<UnitController> allUnits)
        {
            List<UnitController> unitsWithinRadius = new List<UnitController>();
            foreach (UnitController unit in allUnits)
            {
                if (Vector3.Distance(unit.transform.position, HitPosition) <= PointerRadius)
                {
                    unitsWithinRadius.Add(unit);
                }
            }
            return unitsWithinRadius;
        }


        /// <summary>
        /// 指定座標にマーカーを表示
        /// </summary>
        /// <param name="position"></param>
        private void ShowPointer(Vector3 position)
        {
            pointerObject.transform.localScale = Vector3.one * PointerRadius;
            pointerObject.transform.position = position;
            pointerObject.SetActive(true);
        }

        /// <summary>
        /// 2点間の線分で衝突判定し、衝突する時間を返す
        /// </summary>
        /// <returns>衝突した時間(してない場合はfloat.MaxValue)</returns>
        private (float, GameObject) GetArcHitTime(float startTime, float endTime)
        {
            // Linecastする線分の始終点の座標
            Vector3 startPosition = GetArcPositionAtTime(startTime);
            Vector3 endPosition = GetArcPositionAtTime(endTime);

            // 衝突判定
            RaycastHit hitInfo;
            if (Physics.Linecast(startPosition, endPosition, out hitInfo, arcHitLayer))
            {
                var hitObject = hitInfo.collider.gameObject;
                if (((1 << hitObject.layer) & groundLayer.value) != 0)
                {
                    // 地面に衝突
                    if (!pointerIsActive)
                    {
                        pointerMaterial.SetColor("_Color", Color.white);
                        pointerIsActive = true;
                    }
                }
                else if ((( 1<<hitObject.layer) & objectLayer.value) != 0)
                {
                    // Objectに衝突
                    if (pointerMaterial.GetColor("_Color") != Color.clear)
                    {
                        
                        pointerMaterial.SetColor("_Color", Color.clear);
                        pointerIsActive = false;
                    }
                }

                // 衝突したColliderまでの距離から実際の衝突時間を算出
                float distance = Vector3.Distance(startPosition, endPosition);
                return (startTime + (endTime - startTime) * (hitInfo.distance / distance), hitObject);
            }
            return (float.MaxValue, null);
        }
    }



    /// <summary>
    /// 放物線をどのような形で描写するか
    /// </summary>
    enum CalcType
    {
        FromVelocity,
        FromHitPosition
    }

}