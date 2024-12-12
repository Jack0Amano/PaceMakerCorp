using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RootMotion;
using Tactics.Control;
using Cinemachine;
using Tactics.Map;
using static Utility;
using MainMap;
using Tactics.Character;
using TacticsTablet.OnTactics;
using System;
using System.Linq;

namespace TacticsTablet
{
    public class TacticsTablet : MonoBehaviour
    {
        [SerializeField] public CameraUserController CameraController;
        [SerializeField] public TilesController TilesController;
        [SerializeField] public CinemachineVirtualCamera CinemachineVirtualCamera;
        [Header("UI panels")]
        [SerializeField] public Prepare.PreparePanel PreparePanel;
        [SerializeField] public OnTacticsPanel OnTacticsPanel;

        /// <summary>
        /// エンカウント情報
        /// </summary>
        public ReachedEventArgs ReachedEventArgs => GameManager.Instance.ReachedEventArgs;

        /// <summary>
        /// Tabletが表示されているか
        /// </summary>
        public bool IsVisible => gameObject.activeSelf;
        /// <summary>
        /// Tabletがアニメーション中か
        /// </summary>
        public bool IsAnimating { private set; get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator Hide(bool animation=true)
        {
            IsAnimating = true;
            PreparePanel.Hide(animation);
            OnTacticsPanel.Hide(animation);
            UserController.enableCursor = false;
            // TODO Tabletを下に下ろすアニメーション
            yield return new WaitForSeconds(0.8f);
            gameObject.SetActive(false);
            IsAnimating = false;
        }

        /// <summary>
        /// TacticsTabletを表示する
        /// </summary>
        /// <returns></returns>
        public IEnumerator Show()
        {
            IsAnimating = true;
            // TODO Tabletを上に上げるアニメーション
            gameObject.SetActive(true);
            UserController.enableCursor = true;
            yield return new WaitForSeconds(0.8f);
            IsAnimating = false;
        }

        /// <summary>
        /// TabletをStartPositionのTileに配置する
        /// </summary>
        /// <param name="tileID"></param>
        public void SetStartPosition(StartPosition startPosition)
        {
            var startTile = TilesController.GetStartTiles(startPosition, false).First();
            transform.SetPositionAndRotation(startTile.StartTabletPosition.position,
                                                     startTile.StartTabletPosition.rotation);
        }

        /// <summary>
        /// preparePanelを表示する
        /// </summary>
        public void ShowPreparePanel()
        {
            OnTacticsPanel.Hide(false);
            StartCoroutine(Show());
            PreparePanel.Show();
        }

        /// <summary>
        /// TacticsTabletをCharacterの位置に持ってきてtabletを表示する
        /// </summary>
        public void ShowOnTacticsPanel(UnitController unitController, WindowTabType windowTabType)
        {
            if (IsAnimating)
                return;

            if (gameObject.activeSelf == false)
            {
                gameObject.SetActive(true);
                transform.SetPositionAndRotation(unitController.tabletPosition.position, unitController.tabletPosition.rotation);
                
                StartCoroutine(Show());
            }
            OnTacticsPanel.Show(windowTabType);

            OnTacticsPanel.onTacticsTabController.InteractiveTabButton(windowTabType, true);
            StartCoroutine(OnTacticsPanel.SwitchMode(windowTabType));
        }

        internal void ShowOnTacticsPanel(UnitController watchingUnitAtStationaryMode, object none)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 何も表示されていないスクリーンを表示する
        /// </summary>
        /// <param name="animation"></param>
        public void ShowEmptyScreen(bool animation)
        {
            IsAnimating = true;
            PreparePanel.Hide(animation);
            OnTacticsPanel.Hide(animation);
            IsAnimating = false;
        }
    }
}