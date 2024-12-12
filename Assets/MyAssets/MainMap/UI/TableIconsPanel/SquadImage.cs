using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;


namespace MainMap.UI.TableIcons
{
    /// <summary>
    /// Squadの位置を示すためのIcon
    /// </summary>
    public class SquadImage : MonoBehaviour
    {
        [SerializeField] public Image Image;
        [SerializeField] public RectTransform ImageRectTransform;
        [SerializeField] public RectTransform RectTransform;
        [SerializeField] internal Button button1;
        [SerializeField] internal ButtonEvents buttonEvents1;
        [SerializeField] internal Image ManIconImage;
        [SerializeField] internal Image LocationIconImage;
        [SerializeField] internal RectTransform hudPosition;
        [SerializeField] internal RectTransform hudPosition1;
        [SerializeField] internal RectTransform hudPosition2;
        [SerializeField] internal RectTransform hudPosition3;

        /// <summary>
        /// IconがRaycastの対象か falseの場合はbuttonが反応しない
        /// </summary>
        public bool RaycastTarget
        {
            set
            {
                ManIconImage.raycastTarget = LocationIconImage.raycastTarget = value;
            }
            get => ManIconImage.raycastTarget && LocationIconImage.raycastTarget;
        }

        /// <summary>
        /// Squadを配置するためにマウスカーソルについていっている状態
        /// </summary>
        internal bool IsSpawnMode = false;

        /// <summary>
        /// SquadImageに対応するMap上のSquad
        /// </summary>
        public MapSquad MapSquad { internal set; get; }

        public bool IsAnimating { private set; get; } = false;

        internal TableIconsPanel TableIconsPanel;

        Camera Camera;

        private void Awake()
        {
        }

        // Start is called before the first frame update
        void Start()
        {
            Camera = Camera.main;
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void SetActive()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// SpawnMode状態にあるImageをMapに置く
        /// </summary>
        public void PutSquadOnMap()
        {
            IsSpawnMode = false;
            RaycastTarget = true;
            IsAnimating = true;
            RectTransform.DOMove(MapSquad.transform.position, 0.5f).OnComplete(() =>
            {
                IsAnimating = false;
            });
        }

        /// <summary>
        /// ImageをSpawn位置選択中のモードにする
        /// </summary>
        internal void UpdateAsSpawning(Vector3 worldPosition)
        {
            RectTransform.position = worldPosition;
        }

        /// <summary>
        /// Squadの位置をMapSquadの位置に更新する
        /// </summary>
        internal void UpdatePosition()
        {
            if (IsAnimating) return;
            if (Camera == null)
                Camera = Camera.main;
            RectTransform.position = MapSquad.transform.position;
        }

        /// <summary>
        /// 強制的にReturnさせるアニメーション
        /// </summary>
        internal IEnumerator ReturnToBaseForced()
        {
            yield return RectTransform.DOScale(0, 0.5f).OnComplete(() =>
            {
                TableIconsPanel.RemoveSquadImage(this);
            }).WaitForCompletion(); ;
        }

        /// <summary>
        /// Squadが既に選択されているにも関わらず再選択しようとしたときのアニメーション
        /// </summary>
        internal void AnimationSquadIsAlreadySelected()
        {

        }
    }
}