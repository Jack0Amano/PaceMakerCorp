using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using TMPro;
using DG.Tweening;
using static Utility;

namespace StartWindow
{
    /// <summary>
    /// スタートのロゴとか
    /// </summary>
    public class StartTopWindow : MonoBehaviour
    {
        [SerializeField] Lists.StartListAdapter listAdapter;
        [SerializeField] GameObject selectGameTypePanel;
        [SerializeField] BackPanel backPanel;
        [SerializeField] Button saiGonButton;
        [SerializeField] Button loadButton;
        [SerializeField] Button newGameButton;
        [SerializeField] Button tacticsButton;
        [SerializeField] Button settingButton;
        [SerializeField] DebugPanel debugPanel;

        private readonly string saiGonMapID = "SaiGon";

        private readonly List<(string name, string mapID, string id)> tacticsScenes = new List<(string, string, string)>()
        {
            ("Test", "SaiGon", "TestTactics" )
        };

        GameManager gameManager;

        protected void Awake()
        {
            saiGonButton.onClick.AddListener(() => SelectSaiGon());
            loadButton.onClick.AddListener(() => LoadButton());
            newGameButton.onClick.AddListener(() => NewGameButton());
            settingButton.onClick.AddListener(() => SettingButton());
            tacticsButton.onClick.AddListener(() => SelectTacticsScenes());
            backPanel.gameObject.SetActive(false);
            debugPanel.gameObject.SetActive(false);
           gameManager = GameManager.Instance;
        }

        protected void Start()
        {

            if (selectGameTypePanel.activeSelf)
                selectGameTypePanel.SetActive(false);
        }

        /// <summary>
        /// データをロードする
        /// </summary>
        private void LoadButton()
        {
            /// セーブディレクトリから全てのセーブデータのHeaderファイルを読み込む
            var directories = Directory.GetDirectories(GameManager.SaveDataRootPath);
            var data = new List<(SaveDataInfo, string)>();
            foreach (var d in directories)
            {
                var path = Path.Combine(d, "header.bin");
                if (File.Exists(path))
                {
                    try
                    {
                        var info = Binary.LoadFrom<SaveDataInfo>(path);
                        data.Add((info, d));
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            data.Sort((a, b) => a.Item1.SaveTime.CompareTo(b.Item1.SaveTime));

            var models = data.ConvertAll(a =>
            {
                return new Lists.MyListItemModel()
                {
                    directory = a.Item2,
                    saveTime = a.Item1.SaveTime.ToString(),
                    gameTime = a.Item1.GameTime.ToString(),
                    location = a.Item1.MainMapSceneName
                };
            });

            listAdapter.SetItems(models, Lists.ListMode.LoadData, (model) =>
            {
                StartCoroutine(LoadScene(model.location));
            });
        }

        private void NewGameButton()
        {
            backPanel.gameObject.SetActive(true);
            backPanel.Fadein(0.5f);
            selectGameTypePanel.SetActive(true);

            backPanel.backpanelButton.onClick.AddListener(() =>
            {
                backPanel.Fadeout(0.5f, () => backPanel.gameObject.SetActive(false));
                selectGameTypePanel.SetActive(false);
            });

            // TODO MainMapControllerからStartDateTimeをGameManagerのCurrentTimeに上げる DebugではMainMapControllerが毎回あげてる
            //GameController.Instance.data.MakeNewData();
            //StartCoroutine(LoadScene(null));
        }

        private void SettingButton()
        {

        }

        private void SelectSaiGon()
        {
            //gameManager.StartNewData(saiGonMapID);
        }

        /// <summary>
        /// デバック用に直接TacticsSceneに飛ぶ
        /// </summary>
        private void SelectTacticsScenes()
        {
            debugPanel.gameObject.SetActive(true);

            var models = tacticsScenes.ConvertAll(a =>
            {
                return new Lists.MyListItemModel()
                {
                    saveTime = a.name,
                    tacticsSceneID = a.id,
                    mapSceneID = a.mapID
                };
            });

            listAdapter.SetItems(models, Lists.ListMode.LoadTactics, (model) =>
            {
                // 選択時呼び出し idはSceneのid
                gameManager.StaticData.LoadStaticSceneData(model.mapSceneID);
                StartCoroutine(gameManager.ShowTactics(model.tacticsSceneID)) ;
            });
        }

        /// <summary>
        /// シーンとセーブデータをロードする
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private IEnumerator LoadScene(string directory = null)
        {
            gameManager.DataSavingController.Load(directory);
            gameManager.StaticData.LoadStaticSceneData(gameManager.DataSavingController.SaveDataInfo.MainMapSceneName);
            while (!gameManager.DataSavingController.HasDataLoaded)
                yield return null;
            gameManager.EventSceneController.LoadStories(gameManager.DataSavingController.SaveDataInfo.MainMapSceneName);

            Print("Load Scene", gameManager.DataSavingController.SaveDataInfo.MainMapSceneName);
           // StartCoroutine(gameManager.LoadSceneFromLoadedData());
        }
    }
}
