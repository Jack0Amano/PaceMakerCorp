using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utility;
using System.Linq;
using DG.Tweening;
using Tactics.Character;
using Arc;

namespace Tactics
{
    public class AimLineController : MonoBehaviour
    {
        [Header("RendererMaterial")]
        [Tooltip("放物線のマテリアル")]
        [SerializeField] Material lineMaterial;
        [SerializeField] AnimationCurve animationLineCurve;
        [SerializeField] Color lineColor;

        /// <summary>
        /// 現在Aim中のUnit
        /// </summary>
        private UnitController aimingUnit;
        /// <summary>
        /// すべてのUnitへのAimLine
        /// </summary>
        private List<AimLine> aimLines = new List<AimLine>();
        /// <summary>
        /// 
        /// </summary>
        internal UnitsController unitsController;

        // Start is called before the first frame update
        void Start()
        {
            aimLines = new List<AimLine>();
        }

        private void Update()
        {
            
        }

        /// <summary>
        /// ActiveUnitから攻撃可能な相手へのラインの位置を更新する
        /// </summary>
        /// <param name="aiming"></param>
        /// <param name="aimed"></param>
        public void SetAimLine(UnitController aiming, List<UnitController> aimed)
        {
            if (aimLines.Count == 0 && unitsController.UnitsList.Count > 1)
            {
                // 初回のみ すべてのUnitに対してAimLineを作成する
                aimingUnit = aiming;
                unitsController.UnitsList.ForEach(e =>
                {
                    var  newline = Instantiate(new GameObject(), transform);
                    var aimLine = new AimLine(newline, e, lineMaterial);
                    aimLines.Add(aimLine);
                    //SetLocation(newLine, aimingUnit.transform.position, e.transform.position);
                });
            }

            foreach (var line in aimLines)
            {
                if (!aimed.Contains(line.target))
                {
                    // 射線から外れたためAimLineを削除
                    line.Hide();
                }
                else
                {
                    line.Update(aimingUnit.transform.position);
                }
            }
        }

        /// <summary>
        /// 指定されたUnitのAimLineを消す
        /// </summary>
        /// <param name="unit"></param>
        public void Destroy(UnitController unit)
        {
            var destroied = aimLines.Find(l => l.target.Equals(unit));
            if (destroied != null)
            {
                destroied.Hide(true);
            }
        }
        
        /// <summary>
        /// すべてのAimLineを消す
        /// </summary>
        public void ClearAll()
        {
            aimLines.ForEach(l => 
            { 
                l.Hide();
            });
        }

        public void SetAimLineActive(UnitController target)
        {
            var targetLineIndex = aimLines.FindIndex(a => a.target == target);
            if (targetLineIndex == -1)
            {
                PrintWarning($"AimLineController.SetAimLineActive: aimLine to {target} is missing");
                return;
            }
            for (var i = 0; i < aimLines.Count; i++)
            {
                if (targetLineIndex == i)
                {
                    aimLines[i].Update(aimingUnit.transform.position);
                }
                else
                {
                    aimLines[i].Hide();
                }
            }

        }

        public void SetAllAimLineActive()
        {
            aimLines.ForEach(l => l.Update(aimingUnit.transform.position));
        }

    }

    public class AimLine
    {
        internal GameObject lineObject;
        internal UnitController target;

        internal LineRenderer lineRenderer;

        public float LineWidth = 0.1F;
        Material lineMaterial;

        internal Animation animationLineCurve;

        /// <summary>
        /// lineが表示されているかどうか
        /// </summary>
        internal bool isVisible = false;

        Sequence sequence;

        /// <summary>
        /// AimLineのLinerendererを作成する
        /// </summary>
        /// <param name="lineObject">worldに既に設置されたlineとなるGameObject</param>
        /// <param name="target"></param>
        /// <param name="materialTemplate"></param>
        internal AimLine(GameObject lineObject, UnitController target, Material materialTemplate)
        {
            this.lineObject = lineObject;
            this.target = target;
            lineRenderer = lineObject.AddComponent<LineRenderer>();

            lineObject.name = $"AimLine_{target.CurrentParameter.Data.Name}";

            // 光源関連を使用しない
            lineRenderer.receiveShadows = false;
            lineRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            lineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            lineRenderer.textureMode = LineTextureMode.Tile;
            // 線の幅とマテリアル
            lineMaterial = new Material(materialTemplate);
            lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = LineWidth;
            lineRenderer.endWidth = LineWidth;
            lineRenderer.numCapVertices = 5;
            lineRenderer.enabled = false;

            lineMaterial.DOFade(0, 0);
        }

        /// <summary>
        /// linerendreerを更新する
        /// </summary>
        /// <param name="from"></param>
        internal void Update(Vector3 from)
        {
            if (!isVisible)
            {
                if (sequence != null || sequence.IsActive())
                    sequence.Kill();
                sequence = DOTween.Sequence();

                isVisible = true;
                lineObject.SetActive(true);
                lineRenderer.enabled = true;
                sequence.Append(lineMaterial.DOFloat(1, "_Alpha", 0.5f));
                sequence.Play();
            }

            lineRenderer.SetPosition(0, from);
            lineRenderer.SetPosition(1, target.transform.position);
            lineMaterial.SetVector("_StartPosition", from);
            lineMaterial.SetVector("_EndPosition", target.transform.position);
        }

        /// <summary>
        /// AimLineを消す
        /// </summary>
        internal void Hide(bool destroy=false)
        {
            if (!isVisible)
                return;

            if (sequence != null || sequence.IsActive())
                sequence.Kill();
            sequence = DOTween.Sequence();

            isVisible = false;

            sequence.Append(lineMaterial.DOFloat(0, "_Alpha", 0.5f));
            sequence.OnComplete(() =>
            {
                lineRenderer.enabled = false;
                if (destroy)
                {
                    GameObject.Destroy(lineObject);
                }
            });
            sequence.Play();
        }
    }
}