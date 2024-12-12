using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using EventScene;
using static Utility;

namespace GameSetting.SaveLoadData
{
    public class DataPanel : MonoBehaviour
    {
        [SerializeField] DataBasicListAdapter dataBasicListAdapter;

        private CanvasGroup canvasGroup;

        private readonly float animationDuration = 0.3f;

        [SerializeField, ReadOnly] public GameSettingTab.GameSettingType Type = GameSettingTab.GameSettingType.None;

        private GameManager gameManager;

        internal GameSettingCanvas parentCanvas;

        ///<summary>
        /// DataPanelからDataのLoadが開始された時
        /// </summary>
        public event Action<SaveData> SelectSavedDataToLoad;

        /// <summary>
        /// DataPanelからDataのSaveが開始された時
        /// </summary>
        public event Action OnStartToSaveData;

        /// <summary>
        /// escキー等での画面遷移をロックする
        /// </summary>
        public bool IsKeyLocked { private set; get; } = true;

        /// <summary>
        /// データのロード中かセーブ中か
        /// </summary>
        [SerializeField, ReadOnly] public bool IsLoadingOrSaving = false;

        protected void Awake()
        {
            gameManager = GameManager.Instance;
            canvasGroup = GetComponent<CanvasGroup>();
        }

        protected void Start()
        {
            
            dataBasicListAdapter.ItemViewHolderSelected += HolderSelected;
            dataBasicListAdapter.ItemViewHolderTryToRemove += TryRemoveHolder;
        }

        /// <summary>
        /// 指定した内容を表示する
        /// </summary>
        /// <param name="type"></param>
        internal void Show(GameSettingTab.GameSettingType type, float duration = GameManager.ANIMATION_DURATION)
        {
            if(gameObject.activeSelf && type == this.Type)
                return;

            IsKeyLocked = false;

            if (Type != GameSettingTab.GameSettingType.None)
            {
                // 既に表示されているため
                // 一度表示されている物を消してから再度表示
                //gameObject.SetActive(true);
                canvasGroup.DOFade(0, animationDuration).OnComplete(() =>
                {
                    dataBasicListAdapter.RetrieveDataAndUpdate();
                    canvasGroup.DOFade(1,duration);
                });
            }
            else
            {
                canvasGroup.alpha = 0;
                gameObject.SetActive(true);
                // 新たに表示する
                canvasGroup.DOFade(1, duration);
                dataBasicListAdapter.RetrieveDataAndUpdate();
            }
            
            Type = type;
        }

        /// <summary>
        /// DataPanelを非表示にする
        /// </summary>
        internal void Hide(bool animation, float animationDuration = GameManager.ANIMATION_DURATION)
        {
            if (gameObject.activeSelf == false)
                return;

            Type = GameSettingTab.GameSettingType.None;
            if (animation)
            {
                   canvasGroup.DOFade(0, animationDuration).OnComplete(() =>
                   {
                    IsKeyLocked = true;
                    gameObject.SetActive(false);
                });
            }
            else
            {
                IsKeyLocked = true;
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Cellが選択された
        /// </summary>
        /// <param name="saveData"></param>
        private void HolderSelected(SaveData saveData, string path)
        {
            IsKeyLocked = true;
            var dialog = gameManager.EventSceneController.dialogEvent;

            EventScene.Dialog.WindowInput windowInput;
            if (Type == GameSettingTab.GameSettingType.Load)
            {
                windowInput = new EventScene.Dialog.WindowInput("Load", "", Vector2.zero);
                windowInput.onHidden = ((win, r, o) =>
                {
                    IsKeyLocked = false;
                    if (r == EventScene.Dialog.Result.Yes)
                        SelectSavedDataToLoad?.Invoke(saveData);
                });
            }
            else if (Type == GameSettingTab.GameSettingType.Save)
            {
                windowInput = new EventScene.Dialog.WindowInput("Save", "", Vector2.zero);
                windowInput.onHidden = ((win, r, o) =>
                {
                    IsKeyLocked = false;
                    if (r == EventScene.Dialog.Result.Yes)
                        StartCoroutine(SaveData(path));
                });
            }
            else
            {
                return;
            }
            dialog.YesNoWindow.Show(windowInput);
        }

        #region Load and Save

        /// <summary>
        /// 選択した場所に現状のデータを上書きセーブする adapterのcellが選択された時に呼び出される
        /// </summary>
        /// <param name="data"></param>
        private IEnumerator SaveData(string path)
        {
            //TODO: 行の先端以外を選択して保存した時の挙動がおかしい
            var index = dataBasicListAdapter.SavedDatas.FindIndex((s) => s.path == path);
            if (index == -1)
                yield break;
            IsLoadingOrSaving = true;
            OnStartToSaveData?.Invoke();
            print("Save data to " + path);
            // Write story data on save
            gameManager.DataSavingController.Save(path);
            dataBasicListAdapter.UpdateSaveDataInfos();
            dataBasicListAdapter.UpdateOn(index);
            IsLoadingOrSaving = false;
        }

        /// <summary>
        /// セーブを削除する adapterのcellが選択された時に呼び出される
        /// </summary>
        /// <param name="data"></param>
        private void TryRemoveHolder(SaveData data, string path)
        {
            print($"Remove data {path}");
            gameManager.DataSavingController.RemoveSave(path);
            dataBasicListAdapter.RemoveSave(data, true);
        }

        /// <summary>
        /// 新たなセーブをListに追加する
        /// </summary>
        public void CreateNewSave(SaveData saveData, string path)
        {
            if (Type != GameSettingTab.GameSettingType.None)
            {
                print("Add and show new save on DataPanel.dataBasicListAdapter");
                dataBasicListAdapter.AddNewSave(saveData, path);
            }
        }

        /// <summary>
        /// 新しいセーブデータで始める
        /// </summary>
        
        #endregion
    }
}