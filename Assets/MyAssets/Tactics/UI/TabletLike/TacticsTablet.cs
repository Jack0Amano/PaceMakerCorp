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

namespace Tactics.UI
{
    public class TacticsTablet : MonoBehaviour
    {
        [SerializeField] public CinemachineVirtualCamera CinemachineVirtualCamera;

        internal CameraUserController cameraController;
        internal TilesController tilesController;

        internal IEnumerator Hide()
        {
            // TODO Tabletを下に下ろすアニメーション
            yield return new WaitForSeconds(0.8f);
            gameObject.SetActive(false);
        }

        internal void Show()
        {
            // TODO Tabletを上に上げるアニメーション
            gameObject.SetActive(true);
            
        }

        /// <summary>
        /// TabletをTactics開始位置のTileIDに配置する
        /// </summary>
        /// <param name="tileID"></param>
        internal void SetStartPosition(string tileID)
        {
            var startTileCell = tilesController.Tiles.Find(t => t.id == tileID);
            if (startTileCell)
            {
                if (startTileCell.StartTabletPosition)
                {
                    transform.SetPositionAndRotation(startTileCell.StartTabletPosition.position, 
                                                     startTileCell.StartTabletPosition.rotation);
                }
                else
                {
                    PrintWarning($"TilesControllerに{tileID}のStartTabletPositionが設定されていません。\n" +
                                 $"Tactics開始位置のTileにStartTabletPositionを設定してください。");
                }
            }
            else
            {
                PrintWarning($"TilesControllerに{tileID}のTileが設定されていません。\n" +
                             $"Tactics開始位置のTileにTilesControllerを設定してください。");
            }
        }
    }
}