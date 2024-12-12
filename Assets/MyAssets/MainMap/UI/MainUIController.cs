using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using DG.Tweening;
using static Utility;

namespace MainMap.UI
{
    public class MainUIController : MonoBehaviour
    {
        [Header("Tab's panels")]
        [SerializeField] MainMapTabController TabController;
        /// <summary>
        /// Squadsを表示する時に使用されるBaseWIndowの中のコンポーネント的な
        /// </summary>
        [SerializeField] public Squads.SquadsPanel SquadsPanel;
        [SerializeField] Shop.ShopPanel ShopPanel;
        [SerializeField] Item.ItemPanel ItemPanel;
        [SerializeField] HQ.HQPanel HQPanel;
        /// <summary>
        /// Locationを表示する時に使用されるBaseWindowの中のコンポーネント
        /// </summary>
        //[SerializeField] LocationWindow locationPanel;
        // [SerializeField] Data.DataPanel dataWindow;
        [Header("Components")]
        [Tooltip("Map上に位置しているSquadの情報を表示するpanel")]
        [SerializeField] MainMap.UI.InfoPanel.SquadsInfoPanel squadsInfoPanel;
        [Tooltip("倍速モードでシミュレートしているときに表示されるPanel")]
        [SerializeField] Wait.WaitSupplyingPanel WaitSupplyingPanel;
        [Tooltip("Mapの上に表示されるIconのためのPanel")]
        [SerializeField] TableIcons.TableIconsPanel TableOverlayPanel;

        [SerializeField] Image backgroundImage;
        /// <summary>
        /// UI表示状態の場合のbackgroundの色
        /// </summary>
        internal Color BackgroundColor { private set; get; }

        /// <summary>
        /// UIを表示可能な状態であるか
        /// </summary>
        internal bool AbleToShow { private set; get; }
        /// <summary>
        /// UIで表示中の内容
        /// </summary>
        [NonSerialized] public WindowType windowType = WindowType.None;

        internal CanvasGroup CanvasGroup { private set; get; }

        /// <summary>
        /// ShowHideのアニメーションするのに十分な時間が経ったか
        /// </summary>
        private bool AbleToShowHideAnim
        {
            get
            {
                if (PreviousAnimTime == null)
                    return true;
                if ((DateTime.Now - PreviousAnimTime).TotalSeconds > 0.5f)
                {
                    PreviousAnimTime = DateTime.Now;
                    return true;
                }
                return false;
            }
        }
        private DateTime PreviousAnimTime;

        protected private void Awake()
        {
            Application.targetFrameRate = 60;

            //locationPanel.requestOpenWindowHandler += OpenWindow;
            TabController.requestOpenWindowHandler += OpenWindow;
            TabController.SetUp();

            // DataWindowから親Windowを閉じる
            //dataWindow.RequestCloseWindow += ((obj, args) =>
            //{
            //    if (args.windowType == WindowType.Close)
            //    {
            //        if (args.closeSelectInfo)
            //            StartCoroutine(infoPanel.Hide());
            //        requestReloadData?.Invoke(this, null);
            //        Hide();
            //    }
            //});

            CanvasGroup = GetComponent<CanvasGroup>();
            ShopPanel.parentWindowCanvasGroup = CanvasGroup;
            ItemPanel.parentWindowCanvasGroup = CanvasGroup;
            SquadsPanel.parentWindowCanvasGroup = CanvasGroup;
            HQPanel.parentWindowCanvasGroup = CanvasGroup;
            BackgroundColor = backgroundImage.color;
            backgroundImage.color = Color.clear;
            backgroundImage.gameObject.SetActive(false);

            GameManager.Instance.AddTimeEventHandlerAsync += AddTimeEventAsync;

            AbleToShow = false;
        }

        /// <summary>
        /// Loadが完了した際に呼び出し
        /// </summary>
        internal void CompleteToLoad()
        {
            AbleToShow = true;
        }

        /// <summary>
        /// WindowをHQTabを選択した状態で開く
        /// </summary>
        public bool Show()
        {
            if (!AbleToShow || !AbleToShowHideAnim)
                return false;

            windowType = WindowType.HQ;
            TabController.Show(windowType);
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.DOColor(BackgroundColor, 0.5f);
            SquadsPanel.selectedSquad = null;
            squadsInfoPanel.Hide();

            // TODO: 多分Commander用のWindowコンポーネントを作成する、以下の設定はそのcompltionで実行
            AbleToShow = false;

            TableOverlayPanel.Hide();
            return true;
        }

        /// <summary>
        /// Windowを特定のSquadを選択した状態で開く
        /// </summary>
        /// <param name="squad">最初に表示しておくSquad</param>
        /// <param name="location">Squadの位置しているLocation情報 道端にいるならnull</param>
        public bool Show(Squad squad)
        {
            if (!AbleToShow || !AbleToShowHideAnim)
                return false ;

            AbleToShow = false;
            windowType = WindowType.SquadsWindow;

            TabController.Show(windowType);
            SquadsPanel.selectedSquad = squad;
            SquadsPanel.Show();
            squadsInfoPanel.Hide();
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.DOColor(BackgroundColor, 0.5f);
            TableOverlayPanel.Hide();
            return true;
        }

        /// <summary>
        /// 時間追加のイベントが呼ばれた際に呼び出される
        /// </summary>
        private void AddTimeEventAsync(object o, EventArgs args)
        {
            if (SquadsPanel.squadDetail.gameObject.activeSelf)
            {
                SquadsPanel.squadDetail.UpdateInfo();
            }
        }
        
        /// <summary>
        /// エンカウント時に呼び出し
        /// </summary>
        internal void CalledWhenEncount()
        {
            AbleToShow = false;
        }

        /// <summary>
        /// TacticsからMainMapSceneに戻ってきたときの呼び出し
        /// </summary>
        internal void CalledWhenReturnFromTactics()
        {
            AbleToShow = true;
        }

        /// <summary>
        /// 全てのWindowを閉じる
        /// </summary>
        public bool Hide()
        {
            if (AbleToShow && !AbleToShowHideAnim && WaitSupplyingPanel.isActive)
                return false;

            void hideAction()
            {
                AbleToShow = true;
            }

            // 現在表示中のWindowの内容を非表示にする
            switch (windowType)
            {
                //case (WindowType.LocationWindow):
                //    locationPanel.Hide(hideAction);
                //    break;

                case (WindowType.SquadsWindow):
                    SquadsPanel.Hide(hideAction);
                    break;

                case (WindowType.HQ):
                    HQPanel.Hide(hideAction);
                    break;

                //case (WindowType.DataWindow):
                //    dataWindow.Hide(hideAction);
                //    break;

                case (WindowType.ShopWindow):
                    ShopPanel.Hide(hideAction);
                    break;

                case (WindowType.ItemWindow):
                    ItemPanel.Hide(hideAction);
                    break;

                default:
                    break;
            }
            TabController.Hide();
            squadsInfoPanel.Show();
            SquadsPanel.selectedSquad = null;
            TableOverlayPanel.Show() ;
            backgroundImage.DOColor(Color.clear, 0.5f).OnComplete(() => backgroundImage.gameObject.SetActive(false));

            return true;
        }

        /// <summary>
        /// 各子WindowからのHandlerとしてOpenWindowRequest受け取り指定されたWindowを開く 主にtabでのMainWindowの入れ替えよう
        /// </summary>
        private void OpenWindow(object o, OpenWindowArgs args)
        {
            if (windowType == args.WindowType)
                return;

            // TabButtonのDisableを手動で行う (Tab以外のボタンで画面遷移のRequestが送られる場合)
            if (args.TabButtonDisabledManual)
            {
                TabController.InteractiveTabButton(args.WindowType);
            }

            // 現在表示中のWindowの内容を非表示にする
            switch (windowType)
            {
                //case (WindowType.LocationWindow):
                //    locationPanel.Hide();
                //    break;

                case (WindowType.SquadsWindow):
                    SquadsPanel.Hide();
                    break;

                case (WindowType.HQ):
                    HQPanel.Hide();
                    break;

                //case (WindowType.DataWindow):
                //    dataWindow.Hide();
                //    break;

                case (WindowType.ShopWindow):
                    ShopPanel.Hide();
                    break;

                case (WindowType.ItemWindow):
                    ItemPanel.Hide();
                    break;

                default:
                    break;
            }

            

            // 指定されたWindowの内容を（Argsにpropertiesがあればソレを反映して）表示する
            switch (args.WindowType)
            {
                //case (WindowType.LocationWindow):
                //    locationPanel.Show();
                //    break;

                case (WindowType.SquadsWindow):
                    SquadsPanel.Show();
                    break;

                case (WindowType.HQ):
                    //commanderWindow.Show();
                    break;

                //case (WindowType.DataWindow):
                //    dataWindow.Show();
                //    break;

                case (WindowType.ShopWindow):
                    ShopPanel.Show();
                    break;

                case (WindowType.ItemWindow):
                    ItemPanel.Show();
                    break;

                default:
                    break;
            }

            windowType = args.WindowType;
        }
    
    }

    /// <summary>
    /// 各WindowはMainControllerにこのEventArgsを返し別の親Windowを開いてもらう WindowType=.Closeの場合は親Windowを閉じる
    /// </summary>
    public class OpenWindowArgs: EventArgs
    {
        public WindowType WindowType;

        public bool TabButtonDisabledManual = false;

        public bool ReloadData = false;

        /// <summary>
        /// Infoパネルでのオブジェクト選択を取り消す
        /// </summary>
        public bool CloseSelectInfo = false;
    }

    public enum WindowType
    {
        None,
        Close,
        LocationWindow,
        SquadsWindow,
        HQ,
        ItemWindow,
        ShopWindow,
        DataWindow
    }
}