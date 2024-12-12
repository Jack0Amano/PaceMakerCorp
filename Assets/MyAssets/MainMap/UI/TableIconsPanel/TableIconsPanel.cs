using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using DG.Tweening;
using static Utility;

namespace MainMap.UI.TableIcons
{
    /// <summary>
    /// Squadやlocationなどのアイコンにして2DでMap上に表示する物のPanel
    /// </summary>
    public class TableIconsPanel : MonoBehaviour
    {
        internal MapSquads MapSquads;

        [Header("アイコンの親オブジェクト")]
        [SerializeField] Transform SquadIconParent;

        [Header("アイコンのTemplate")]
        [SerializeField] internal BoxProgressBar templateProgress;
        [SerializeField] internal Balloon templateBalloon;

        [SerializeField] private SquadImage SquadImage;
        [SerializeField] private LocationImage TemplateLocationImage;

        [SerializeField] public HourBalloon HourBalloon;
        [SerializeField] public SupplyBalloon SupplyBalloon;
        [SerializeField] public LocationBalloon LocationBalloon;
        [SerializeField] public ReturnBaseBalloon ReturnBaseBalloon;

        [Tooltip("早送り中に表示されるIcon")]
        [SerializeField] public FastForwardIcon FastForwardIcon;

        [Header("Values")]
        [Tooltip("Squadが十分接近した際にLocationのUIを消す。その距離")]
        [SerializeField] float HideLocationDistance = 0.3f;

        [Header("rayを飛ばすための仮想平面を作成するための三点")]
        [SerializeField] Transform PlanePosition01;
        [SerializeField] Transform PlanePosition02;
        [SerializeField] Transform PlanePosition03;


        List<SquadImage> squadImages = new List<SquadImage>();
        List<LocationImage> locationImages = new List<LocationImage>();

        /// <summary>
        /// Squadを選択したときの呼び出し
        /// </summary>
        public Action<SquadImage> SelectSquadAction;
        /// <summary>
        /// Locationを選択したときの呼び出し
        /// </summary>
        public Action<LocationImage> SelectLocationAction;

        public Action<SquadImage> OnPointerEnterSquadAction;
        public Action<SquadImage> OnPointerExitSquadAction;

        public Action<LocationImage> OnPointerEnterLocationAction;
        public Action<LocationImage> OnPointerExitLocationAction;

        Camera Camera;

        CanvasGroup CanvasGroup;

        Sequence appearDisappearSequence;

        RectTransform RectTransform;

        Plane Plane;

        Vector3 PreviousMousePosition;
        Vector3 PreviousIconPosition;

        // Start is called before the first frame update
        void Start()
        {
            //IEnumerator show()
            //{
            //    yield return new WaitForSeconds(2);
            //    while (true)
            //    {
            //        templateBalloon.Show();
            //        yield return new WaitForSeconds(1.5f);
            //        templateBalloon.Hide();
            //        yield return new WaitForSeconds(1.5f);
            //    }
            //}
            //StartCoroutine(show());
            CanvasGroup = GetComponent<CanvasGroup>();
            Camera = Camera.main;
            RectTransform = GetComponent<RectTransform>();

            Plane = new Plane(PlanePosition01.position, PlanePosition02.position, PlanePosition03.position);
        }

        // Update is called once per frame
        void Update()
        {
        }

        /// <summary>
        /// 一時的に消えているOverlayPanelを再表示
        /// </summary>
        public void Show()
        {
            if (appearDisappearSequence != null && appearDisappearSequence.IsActive())
                appearDisappearSequence.Kill();
            appearDisappearSequence = DOTween.Sequence();
            appearDisappearSequence.Append(CanvasGroup.DOFade(1, 0.3f));
        }

        /// <summary>
        /// OverlayPanelを消す
        /// </summary>
        public void Hide()
        {
            if (appearDisappearSequence != null && appearDisappearSequence.IsActive())
                appearDisappearSequence.Kill();
            appearDisappearSequence = DOTween.Sequence();
            appearDisappearSequence.Append(CanvasGroup.DOFade(0, 0.3f));
        }

        #region SquadImage
        /// <summary>
        /// SquadImageをOverlayPanelに表示する
        /// </summary>
        /// <returns></returns>
        public SquadImage AddSquadImage(MapSquad newSquad, bool spawnMode = false)
        {
            var newPanel = Instantiate(SquadImage, SquadIconParent);
            newPanel.button1.onClick.AddListener(() => SelectSquadAction?.Invoke(newPanel));
            newPanel.buttonEvents1.onPointerEnter += (e => OnPointerEnterSquadAction?.Invoke(newPanel));
            newPanel.buttonEvents1.onPointerExit += (e => OnPointerExitSquadAction?.Invoke(newPanel));
            newPanel.MapSquad = newSquad;
            newPanel.TableIconsPanel = this;
            if (spawnMode)
                newPanel.RaycastTarget = false;
            newPanel.IsSpawnMode = spawnMode;
            squadImages.Add(newPanel);
            return newPanel;
        }

        /// <summary>
        /// SquadImageをOverlayPanelから消去する
        /// </summary>
        /// <param name="squadImage"></param>
        public void RemoveSquadImage(SquadImage squadImage)
        {
            Destroy(squadImage);
            squadImages.Remove(squadImage);
        }

        /// <summary>
        /// Squadの位置を更新する
        /// </summary>
        /// <param name="squadImage"></param>
        public void UpdateSquadPosition(SquadImage squadImage = null)
        {
            
            Vector3 GetPositionOnPlane()
            {
                if (UserController.MousePosition == PreviousMousePosition)
                    return PreviousIconPosition;
                var ray = Camera.ScreenPointToRay(UserController.MousePosition);
                if (Plane.Raycast(ray, out var rayHitDistance))
                {
                    PreviousIconPosition = ray.GetPoint(rayHitDistance);
                    PreviousMousePosition = UserController.MousePosition;
                    return PreviousIconPosition;
                }
                return Vector3.zero;
            }


            if (squadImage != null)
            {

                if (squadImage.IsSpawnMode) 
                    squadImage.UpdateAsSpawning(GetPositionOnPlane());
                else
                    squadImage.UpdatePosition();
            }
            else
            {
                squadImages.ForEach(s => 
                {
                    if (s.IsSpawnMode)
                        s.UpdateAsSpawning(GetPositionOnPlane());
                    else
                        s.UpdatePosition();
                });
            }

            // Squadの位置がLocationと十分に近い場合はLocationの表示を消す
            locationImages.ForEach(l =>
            {
                var isNearToSquad = false;
                foreach(var s in squadImages)
                {
                    if (s.IsSpawnMode)
                        continue;

                    var dist = Vector3.Distance(s.RectTransform.position, l.RectTransform.position);
                    if (dist < HideLocationDistance)
                    {
                        isNearToSquad = true;
                        l.IsEnable = false;
                        break;
                    }
                }
                if (!isNearToSquad)
                    l.IsEnable = true;
            });
        }
        #endregion

        #region MapLocation
        /// <summary>
        /// MapLocationをOverlayPanelに表示する
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public LocationImage AddLocationImage(MapLocation location)
        {
            var newPanel = Instantiate(TemplateLocationImage, transform);
            newPanel.button.onClick.AddListener(() => SelectLocationAction?.Invoke(newPanel));
            newPanel.ButtonEvents.onPointerEnter += (e => { if (newPanel.IsEnable) OnPointerEnterLocationAction?.Invoke(newPanel); });
            newPanel.ButtonEvents.onPointerExit += (e => { if (newPanel.IsEnable) OnPointerExitLocationAction?.Invoke(newPanel); });
            newPanel.MapLocation = location;
            newPanel.TableIconsPanel = this;
            locationImages.Add(newPanel);
            return newPanel;
        }

        void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log(eventData.eligibleForClick);
}

        /// <summary>
        /// OverlayCanvasに表示されているLocationの位置を更新する
        /// </summary>
        public void UpdateLocationsPosition()
        {
            locationImages.ForEach(l =>
            {
                l.UpdateLocation();
            });
        }
        #endregion

    }
}