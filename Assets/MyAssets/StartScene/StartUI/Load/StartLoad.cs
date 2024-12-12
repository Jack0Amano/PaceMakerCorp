using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using UnityEngine;
using System;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using DG.Tweening;
using static Utility;

namespace StartWindow
{
    public class StartLoad : MonoBehaviour
    {
        [SerializeField] GameObject scrollContent;
        [SerializeField] RectTransform panelRectTransform;
        [SerializeField] BackPanel backPanel;

        private List<LoadCell> cells = new List<LoadCell>();

        /// <summary>
        /// 全てのSaveDataのディレクトリとそのセーブ情報
        /// </summary>
        public List<(SaveDataInfo info, string path)> infoAndPath = new List<(SaveDataInfo info, string path)>();

        private Action<string> cellSelected;

        private float animationTime = 0.5f;

        private void Awake()
        {
        }

        private void Start()
        {
            SetCells();
        }

        public void Show(Action<string> completed)
        {
            cellSelected = completed;
            gameObject.SetActive(true);
            panelRectTransform.pivot = new Vector2(1, 0.5f);

            panelRectTransform.DOPivotX(0, animationTime).SetEase(Ease.InOutCubic);

            backPanel.Fadein(animationTime);
        }

        [SerializeField] public void Hide()
        {
            panelRectTransform
                .DOPivotX(1, animationTime)
                .SetEase(Ease.InOutCubic)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });

            backPanel.Fadeout(animationTime);
        }

        private void HideWithoutAnimation()
        {
            panelRectTransform.pivot = new Vector2(1, 0.5f);
            backPanel.Fadeout(0);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Cellを選択したときのコールバック
        /// </summary>
        /// <param name="cell"></param>
        private void CellSelected(LoadCell cell)
        {
            HideWithoutAnimation();
            cellSelected(cell.path);
        }

        /// <summary>
        /// セーブ情報のCellを表示する
        /// </summary>
        private void SetCells()
        {
            GetDataDirectories();

            var cellObj = scrollContent.transform.GetChild(0);

            var cellOrigin = cellObj.GetComponent<LoadCell>();
            cells.Add(cellOrigin);
            cellOrigin.button.onClick.AddListener(() => CellSelected(cellOrigin));
            cellOrigin.SetData(infoAndPath[0].info, infoAndPath[0].path);

            for (int i = 1; i < infoAndPath.Count; i++)
            {
                var cell = Instantiate(cellOrigin, scrollContent.transform);
                cell.SetData(infoAndPath[i].info, infoAndPath[i].path);
                cell.button.onClick.AddListener(() => CellSelected(cell));
                cells.Add(cell);
            }
        }

        /// <summary>
        /// セーブディレクトリから全てのセーブデータのHeaderファイルを読み込む
        /// </summary>
        private void GetDataDirectories()
        {
            var directories = Directory.GetDirectories(GameManager.SaveDataRootPath);
            foreach (var d in directories)
            {
                var path = Path.Combine(d, "header.bin");
                if (File.Exists(path))
                {
                    try
                    {
                        var info = Binary.LoadFrom<SaveDataInfo>(path);
                        infoAndPath.Add((info, d));
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            infoAndPath.Sort((a, b) => a.info.SaveTime.CompareTo(b.info.SaveTime));
        }
    }
}