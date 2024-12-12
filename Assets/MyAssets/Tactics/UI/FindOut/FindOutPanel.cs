using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using static Utility;
using DG.Tweening;
using Tactics.Character;
using Tactics.AI;

namespace Tactics.UI.Overlay.FindOutUI
{
    /// <summary>
    /// コンパス上に表示す敵を表す点
    /// </summary>
    public class FindOutPanel : MonoBehaviour
    {
        //[SerializeField] GameObject circlePrefab;
        [SerializeField] GameObject compassPrefab;

        [SerializeField] Color normalEnemyColor;
        [SerializeField] Color guardEnemyColor;
        [SerializeField] Color battleEnemyColor;

        List<TargetCircleSet> targetCircleSets = new List<TargetCircleSet>();

        Camera mainCamera;

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            UpdateAngles();
        }

        /// <summary>
        /// 新たにTargetが現れたもしくはTargetが消失した場合のUpdate
        /// </summary>
        /// <param name="findOutLevels">ターゲットとその認識レベル</param>
        internal void UpdateTargets(UnitController active, List<FindOutLevel> findOutLevels)
        {
            var existingTargetCircleSets = new List<TargetCircleSet>();
            var deletedTargetCircleSets = new List<TargetCircleSet>(targetCircleSets);
            var newFindOutLevels = new List<FindOutLevel>();
            foreach (var findOutLevel in findOutLevels)
            {
                var existing = targetCircleSets.Find(t => t.target == findOutLevel.Enemy);
                if (existing == null)
                {
                    // 既存のに存在しないため新たに作成
                    newFindOutLevels.Add(findOutLevel);
                }
                else
                {
                    // 既存のに存在するためExistingでLevelを更新しDeletedListから削除
                    existing.level = findOutLevel.Level;
                    existingTargetCircleSets.Add(existing);
                    deletedTargetCircleSets.Remove(existing);
                }
            }

            // deletedListからリサイクルするか新規に作成
            for (var i = 0; i < newFindOutLevels.Count; i++)
            {
                var findOutLevel = newFindOutLevels[i];
                if (deletedTargetCircleSets.Count != 0)
                {
                    // リサイクルしてdeletedListから削除
                    var recycled = deletedTargetCircleSets[0];
                    deletedTargetCircleSets.RemoveAt(0);
                    recycled.Recycle(findOutLevel.Enemy, findOutLevel.Level, active);
                    // existingListに入れる
                    existingTargetCircleSets.Add(recycled);
                }
                else
                {
                    // deletedがない場合は新規作成
                    //var circle = Instantiate(circlePrefab);
                    var compass = Instantiate(compassPrefab, transform);
                    if (mainCamera == null)
                        mainCamera = Camera.main;
                    var newTargetCircleSet = new TargetCircleSet(findOutLevel.Enemy, 
                                                                 findOutLevel.Level, 
                                                                 compass, 
                                                                 mainCamera, 
                                                                 active);
                    newTargetCircleSet.normalEnemyColor = normalEnemyColor;
                    newTargetCircleSet.battleEnemyColor = battleEnemyColor;
                    newTargetCircleSet.guardEnemyColor = guardEnemyColor;
                    // existingListに入れる
                    existingTargetCircleSets.Add(newTargetCircleSet);
                }
            }

            // RecycleもされないDeletedされたtargetCircleSetを消す
            deletedTargetCircleSets.ForEach(d =>
            {
                var destroied = d.Destroy();
                Destroy(destroied);
            });

            // ExistingListのAngleを更新してtargetCircleSetsに入れる
            targetCircleSets = existingTargetCircleSets;
            targetCircleSets.ForEach(s => s.UpdateDegree());
        }

        /// <summary>
        /// Targetへの角度をUpdateする (ほぼ毎フレーム呼ぶ)
        /// </summary>
        public void UpdateAngles()
        {
            targetCircleSets.ForEach(s =>
            { 
                s.UpdateDegree();
                //s.UpdateArrowPosition();
            });
        }

        /// <summary>
        /// TargetへのLevelが更新された時呼び出し
        /// </summary>
        /// <param name="targetAndLevels"></param>
        internal void UpdateLevels(List<FindOutLevel> targetAndLevels)
        {
            foreach (var targetAndLevel in targetAndLevels)
            {
                var updatedSet = targetCircleSets.Find(s => s.target == targetAndLevel.Enemy);
                if (updatedSet != null)
                    updatedSet.level = targetAndLevel.Level;
            }
        }
    }

    /// <summary>
    /// TargetとCompass上のTargetUI及び発見時の画面端UIのSet
    /// </summary>
    internal class TargetCircleSet
    {
        internal UnitController target;

        //GameObject circle;
        //Material circleMaterial;
        //Transform circleTransform;

        GameObject compass;
        Material compassMaterial;

        internal Color normalEnemyColor;
        internal Color guardEnemyColor;
        internal Color battleEnemyColor;


        private float circleMaxAlpha = 0.4f;

        private UnitController activeUnit;
        private Camera camera;

        /// <summary>
        /// TargetEnemyの警戒レベル 0~4の数値をとる
        /// </summary>
        internal float level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    var v = value / 4f;
                    if (compassMaterial != null)
                    {
                        if (value > 10)
                        {
                            compassMaterial.DOColor(battleEnemyColor, 0.25f);
                        }
                        else if (value == 0)
                        {
                            compassMaterial.DOColor(normalEnemyColor, 0.25f);
                        }
                        else
                        {
                            var normalColorVector = normalEnemyColor.ToVector4() * (1 - v);
                            var guardColorVector = guardEnemyColor.ToVector4() * v;
                            var colorVector = normalColorVector + guardColorVector;
                            compassMaterial.DOColor(colorVector.ToColor(), 0.25f);
                        }
                    }
                }

                _level = value;
            }
        }
        private float _level = float.MinValue;

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="target">Circleが追跡するTarget</param>
        /// <param name="level">Circleが表示するTargetの表示レベル</param>
        /// <param name="circle">SearchingShaderを持ったC設置済みのircle</param>
        /// <param name="compass">コンパス上に表示される敵の表示 設置済み</param>
        /// <param name="camera">写しているメインのカメラ</param>
        /// <param name="parent">TargetCircleのParentで通常は全画面を覆う透明Panel</param>
        public TargetCircleSet(UnitController target, float level, GameObject compass, Camera camera, UnitController active)
        {
            this.target = target;
            // this.targetRenderer = target.meshRenderer;
            //this.circle = circle;
            //var meshRenderer = circle.GetComponent<MeshRenderer>();
            //circleMaterial = new Material(meshRenderer.material);
            //meshRenderer.material = circleMaterial;

            //circleMaterial.SetColor("_Color", Color.white);
            //circleTransform = circle.transform;

            this.level = level;

            this.compass = compass;
            var compassImage = compass.GetComponent<Image>();
            compassMaterial = new Material(compassImage.material);
            compassImage.material = compassMaterial;

            activeUnit = active;
            this.camera = camera;
        }

        /// <summary>
        /// 他のパラメータはそのままに別のTargetに使用するためにRecycleする
        /// </summary>
        /// <param name="target"></param>
        /// <param name="level"></param>
        public void Recycle(UnitController target, float level, UnitController active)
        {
            this.target = target;
            this.level = level;
            this.activeUnit = active;
        }

        /// <summary>
        /// 削除状態にする
        /// </summary>
        /// <returns>circleのGameObject</returns>
        public GameObject Destroy()
        {
            return this.compass;
        }

        /// <summary>
        /// 設定内容でCircleを描写する Dupricate 画面端を通る不安定な表示
        /// </summary>
        /// <param name="level"></param>
        public void UpdateDegree()
        {
            if (activeUnit == null || target == null)
                return;

            // ===== FindOut用の画面端を移動するPanelの設置 =====
            var activePos = activeUnit.transform.position;
            var targetPos = target.transform.position;
            var r = Mathf.Atan2(targetPos.x - activePos.x, targetPos.z - activePos.z);
            r *= Mathf.Rad2Deg;
            r += 180;

            //circleTransform.rotation = Quaternion.Euler(0, r, 0);

            // ===== Compass上のEnemyパネルの設置 =====
            var degFromCam = GetDegreeToTargetFromCamera(targetPos);

            var _deg = degFromCam - 180;
            if (_deg < 0)
                _deg += 360;
            compassMaterial.SetFloat("_Degree", _deg);
        }

        public void UpdateArrowPosition()
        {
            var circlePosition = activeUnit.transform.position;
            circlePosition.y += 0.7f;
           // circleTransform.position = circlePosition;
        }

        /// <summary>
        /// Targetへの角度を取得する カメラからの角度
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        float GetDegreeToTargetFromCamera(Vector3 targetPos)
        {
            var from = camera.transform;
            var r = Mathf.Atan2(targetPos.x - from.position.x, targetPos.z - from.position.z);
            r *= Mathf.Rad2Deg;
            if (r < 0)
                r = 360 + r;
            var deg = 0f;
            if (r < from.rotation.eulerAngles.y)
                deg = 360 - from.rotation.eulerAngles.y + r;
            else
                deg = r - from.rotation.eulerAngles.y;

            return deg;
        }



        public override bool Equals(object obj)
        {
            if (obj is TargetCircleSet b)
            {
                if (this.level.Equals(b.level) &&
                    this.target.Equals(b.target))
                    return true;
            }
            return false;
        }
    }
}