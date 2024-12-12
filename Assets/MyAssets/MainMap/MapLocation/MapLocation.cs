using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MainMap.Spawn;
using static Utility;
using DG.Tweening;

namespace MainMap
{
    public class MapLocation : MonoBehaviour
    {
        [Tooltip("LocationParameterのidと同じでパラメーターのすり合わせを行うためのid")]
        [SerializeField] internal string id;

        internal GameObject locationObject;

        [Tooltip("Spawnする際にどこから出撃したていにするか costが影響")]
        [SerializeField] public MapLocation BaseLocation;

        [Tooltip("通常エンカウント時のTacticsSceneのID")]
        [SerializeField] public string DefaultTacticsSceneID;

        [Tooltip("都市の敵の存在を表示する Material")]
        [SerializeField] public MeshRenderer DetectSpawningMesh;

        [Tooltip("Materialの光具合 のDefault")]
        [SerializeField] float DefaultIntensity = 9;
        [Tooltip("Materialの光具合 strongの場合")]
        [SerializeField] float StrongIntensity = 11;

        [Tooltip("別Squadが接近してきたときの退避エリア")]
        [SerializeField] public List<Transform> TurnoutTransforms;

        internal Color WarningColor01;
        internal Color WarningColor02;
        internal Color WarningColor03;
        internal Color SafeColor;
        internal AnimationCurve DetectSpawningAnimationCurve;

        private List<Color> DetectSpawningColors;

        /// <summary>
        /// SpawnSquadがLocationに存在しているか
        /// </summary>
        public SpawnSquad SpawnSquadOnLocation
        {
            set
            {
                _SpawnSquadOnLocation = value;
                if (DetectSpawningMesh != null)
                {
                    StartCoroutine(DetectSpawningMaterial.SetColor("_Emission", GetColorOfTowerTopMaterial(DefaultIntensity), DetectSpawningAnimationCurve));
                    StartCoroutine(SetColor(DetectSpawningLight, GetColorOfTowerLight(), DetectSpawningAnimationCurve));
                }
            }
            get => _SpawnSquadOnLocation;
        }
        private SpawnSquad _SpawnSquadOnLocation;

        /// <summary>
        /// SpawnEnemyに対しての鉄塔のtopのmaterialの色
        /// </summary>
        private Color GetColorOfTowerTopMaterial(float intensity)
        {
            var factor = Mathf.Pow(2, intensity);

            if (SpawnSquadOnLocation == null)
                return SafeColor * factor;
            else if (SpawnSquadOnLocation.SpawnRequestData.Level > 3)
                return DetectSpawningColors[3] * factor;
            else
                return DetectSpawningColors[SpawnSquadOnLocation.SpawnRequestData.Level] * factor;
            
        }

        /// <summary>
        /// SpawnEnemyに対しての鉄塔の上のLightの色
        /// </summary>
        /// <param name="intensity"></param>
        /// <returns></returns>
        private Color GetColorOfTowerLight()
        {
            return GetColorOfTowerTopMaterial(0);
        }

        /// <summary>
        /// Locationのデータ
        /// </summary>
        internal LocationParamter Data;
        /// <summary>
        /// RadioTowerの上にある敵のスポーン表示用の光のLightコンポーネント
        /// </summary>
        Light DetectSpawningLight;
        /// <summary>
        /// RadioTowerの上にある敵のスポーン表示用のMaterial
        /// </summary>
        Material DetectSpawningMaterial;

        public Vector3 Position
        {
            get => transform.position;
        }

        /// <summary>
        /// ロケーションを強調表示にする
        /// </summary>
        public bool IsStressed
        {
            get => _isStressed;
            set {
                // meshRenderer.material.SetFloat("_Stressed", value ? 1 : 0);
                _isStressed = value;
            }
        }
        private bool _isStressed = false;

        internal MainMap.UI.TableIcons.LocationImage LocationImage;
        /// <summary>
        /// 街が有効的な場所であるか
        /// </summary>
        public bool IsFriend
        {
            get => Data.type == LocationParamter.Type.friend;
        }

        protected private void Awake()
        {
            if (BaseLocation == null)
                BaseLocation = this;
            locationObject = gameObject;

            if (DetectSpawningMesh != null)
            {
                DetectSpawningLight = DetectSpawningMesh.GetComponentInChildren<Light>();

                DetectSpawningMaterial = new Material(DetectSpawningMesh.material);
                DetectSpawningMesh.material = DetectSpawningMaterial;
                DetectSpawningColors = new List<Color>
                {
                    SafeColor, WarningColor01, WarningColor02, WarningColor03
                };
                var factor = Mathf.Pow(2, 9);
                DetectSpawningLight.color = (DetectSpawningColors[0].ToVector3() / factor).ToColor();
            }
            
        }

        /// <summary>
        /// Locationデータが読み込まれたときの呼び出し
        /// </summary>
        internal void Loaded()
        {
        }

        internal void SelectLocation()
        {

        }

        internal void DeSelectLocation()
        {

        }

        public void ReduceDefencePoint(int cost)
        {

        }

        /// <summary>
        /// タワーの明かりを点滅しているか
        /// </summary>
        public bool IsFlashingTowerLight { private set; get; }
        private Sequence FlashingSequence;

        /// <summary>
        /// タワーの明かりを点滅させる
        /// </summary>
        public void StartFlashingTowerLight()
        {
            if (IsFlashingTowerLight) return;
            if (FlashingSequence != null && FlashingSequence.IsActive())
                FlashingSequence.Kill();
            var defaultColor = GetColorOfTowerTopMaterial(DefaultIntensity);
            var strongColor = GetColorOfTowerTopMaterial(StrongIntensity);
            FlashingSequence = DOTween.Sequence();
            FlashingSequence.Append(DetectSpawningMaterial.DOColor(strongColor, "_Emission", 0.3f));
            FlashingSequence.Append(DetectSpawningMaterial.DOColor(defaultColor, "_Emission", 0.3f));
            FlashingSequence.SetLoops(-1);
            FlashingSequence.Play();
            IsFlashingTowerLight = true;

        }

        /// <summary>
        /// タワーの明かりの点滅を停止
        /// </summary>
        public void EndFlashingTowerLight()
        {
            if (!IsFlashingTowerLight)
                return;
            if (FlashingSequence != null && FlashingSequence.IsActive())
                FlashingSequence.Kill();
            var defaultColor = GetColorOfTowerTopMaterial(DefaultIntensity);
            FlashingSequence = DOTween.Sequence();
            FlashingSequence.Append(DetectSpawningMaterial.DOColor(defaultColor, "_Emission", 0.3f));
            FlashingSequence.OnComplete(() => IsFlashingTowerLight = false);
            FlashingSequence.Play();
        }


        /// <summary>
        /// AnimationCurveに即した形でLightの色を変更する
        /// </summary>
        public IEnumerator SetColor(Light light, Color endColor, AnimationCurve animationCurve)
        {
            var start = Time.time;
            var t = 0f;
            var startColor = light.color;
            var end = animationCurve.keys[animationCurve.keys.Length - 1].time;

            while (t < end)
            {
                var rate = animationCurve.Evaluate(t);
                var _value = (1 - rate) * startColor + rate * endColor;
                light.color = _value;
                
                yield return null;
                t = Time.time - start;
            }
            light.color = endColor;

        }

        public override string ToString()
        {
            return gameObject.name.ToString();
        }
    }
}

[Serializable]
public class LocationParamter
{
    /// <summary>
    /// LocationのID
    /// </summary>
    public string id;

    [Tooltip("何時間で出撃したEnemyのLevelが回復するか")]
    public float HourToRecoverPower;

    [Tooltip("最後にRecoveryが発動したときの時間")]
    [SerializeField] private string StrPreviousRecoveryDate;

    /// <summary>
    /// 基地防衛と部隊出撃能力値の現在ポイント
    /// </summary>
    public float defencePoint;

    /// <summary>
    /// 基地防衛と部隊出撃能力値の最大ポイント
    /// </summary>
    public float maxDefencePoint;

    /// <summary>
    /// 基地の敵味方識別
    /// </summary>
    public Type type = Type.neutral;

    /// <summary>
    /// Locationの名前
    /// </summary>
    public string Name
    {
        get
        {
            if (name == null || name.Equals(id))
                name = GameManager.Instance.Translation.SceneObjectsIni.ReadValue("Location", id, id);
            return name;
        }
    }
    private string name;


    public static LocationParamter Default()
    {
        return new LocationParamter()
        {
            id = "",
            defencePoint = 0
        };
    }

    /// <summary>
    /// Locationの敵味方識別
    /// </summary>
    public enum Type : int
    {
        friend = 0,
        neutral = 1,
        enemy = 2
    }

    public override string ToString()
    {
        return $"LocationParameter: id.{id}";
    }
}