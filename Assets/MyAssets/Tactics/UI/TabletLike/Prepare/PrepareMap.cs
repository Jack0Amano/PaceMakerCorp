using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Linq;
using DG.Tweening;

namespace Tactics.Prepare
{

    public class PrepareMap : MonoBehaviour
    {
        
        [SerializeField] List<NameList> NameLists;
        [SerializeField] List<NameList> DebugNameLists;

        [SerializeField] GameObject DebugArea;
        [SerializeField] GameObject NameListsArea;

        /// <summary>
        /// UnitをSpawnPointから削除する際の呼び出し
        /// </summary>
        public Action<NameList, UnitData> removeUnitAction;

        /// <summary>
        /// PrepareにおかれているEnemyの一覧
        /// </summary>
        internal List<NameList> EnemiesLists;
        /// <summary>
        /// PrepareにおかれているPlayerの一覧
        /// </summary>
        internal List<NameList> PlayerLists;

        /// <summary>
        /// 現在選択中のNameList
        /// </summary>
        public NameList ActiveNameList { private set; get; }

        internal bool IsDebugMode
        {
            get => isDebugMode;
            set
            {
                isDebugMode = value;
                DebugArea.SetActive(value);
                NameListsArea.SetActive(!value);

                if (value)
                {
                    EnemiesLists = DebugNameLists.FindAll(l => l.UnitAttribute == UnitAttribute.ENEMY);
                    PlayerLists = DebugNameLists.FindAll(l => l.UnitAttribute == UnitAttribute.PLAYER);
                }
                else
                {
                    EnemiesLists = new List<NameList>();
                    PlayerLists = NameLists;
                }
            }
        }
        private bool isDebugMode = false;

        /// <summary>
        /// ListのUnitがGameを開始できる状態にあるか
        /// </summary>
        public bool IsEnoughtUnitsInList
        {
            get
            {
                if (!IsDebugMode)
                {
                    var playerCount = NameLists.Sum(l => l.UnitDatas.Count);
                    return playerCount != 0;
                }
                else
                {
                    var enemyCount = EnemiesLists.Sum(l => l.UnitDatas.Count);
                    var playerCount = PlayerLists.Sum(l => l.UnitDatas.Count);
                    return enemyCount != 0 && playerCount != 0;
                }
            }
        }

        protected private void Awake()
        {

            NameLists.ForEach(l => l.button.onClick.AddListener(() => SelectNameList(l)));
            DebugNameLists.ForEach(l => l.button.onClick.AddListener(() => SelectNameList(l)));
        }

        /// <summary>
        /// 道順のリストの設置
        /// </summary>
        /// <param name="values"></param>
        internal void SetWaysDropdown(List<string> values)
        {
            DebugNameLists.ForEach(l => l.WaysDropdownValues = values);
        }

        /// <summary>
        /// 開始位置のタイルIDを指定
        /// </summary>
        /// <param name="idList"></param>
        internal void SetStartTileIDs(List<string> idList)
        {
            if (IsDebugMode)
            {
                // DebugMode
                DebugArea.SetActive(true);
                NameListsArea.SetActive(false);
                DebugNameLists.ForEach(l => l.SetDropdownLocations(idList));
                ActiveNameList = DebugNameLists.First();
            }
            else
            {
                DebugArea.SetActive(false);
                NameListsArea.SetActive(true);
                foreach(var item in NameLists.Select((list, index) => new {list, index }))
                {
                    if (idList.IndexAt(item.index, out var id))
                    {
                        item.list.gameObject.SetActive(true);
                        item.list.TileID = id;
                        item.list.UnitAttribute = UnitAttribute.PLAYER;
                    }
                    else
                    {
                        item.list.gameObject.SetActive(false);
                    }
                }
                ActiveNameList = NameLists.Find(l => l.TileID == idList.First());
            }
            ActiveNameList.IsActive = true;
            
        }

        /// <summary>
        /// NameListを選択する
        /// </summary>
        private void SelectNameList(NameList nameList)
        {
            ActiveNameList.IsActive = false;
            ActiveNameList = nameList;
            ActiveNameList.IsActive = true;

        }

        /// <summary>
        /// 指定されたパラメーターのTextLabelを揺らす
        /// </summary>
        /// <param name="unitData"></param>
        public void ShakeNameAnimation(UnitData unitData)
        {
            if (IsDebugMode)
                DebugNameLists.ForEach(l => l.ShakeLabelIfUnitInList(unitData));
            else
                NameLists.ForEach(l => l.ShakeLabelIfUnitInList(unitData));
        }

        /// <summary>
        /// 指定されたUnitParameterの出撃を取りやめる 
        /// </summary>
        /// <param name="unitData"></param>
        public void RemoveUnit(UnitData unitData)
        {
            if (IsDebugMode)
                DebugNameLists.ForEach(l => l.RemoveUnitIfUnitInList(unitData));
            else
                NameLists.ForEach(l => l.RemoveUnitIfUnitInList(unitData));
        }

        /// <summary>
        /// すべてのUnitの出撃を取りやめる
        /// </summary>
        public void RemoveAllUnits()
        {
            DebugNameLists.ForEach(l => l.RemoveAllUnitsInList());
        }

        /// <summary>
        /// 指定されたUnitの出撃を準備状態にする
        /// </summary>
        /// <param name="tileID"></param>
        /// <param name="unitParameter"></param>
        public void AddUnitOnActiveList(UnitData unitData)
        {
            void SetOrRemoveUnit(NameList list)
            {
                if (list == ActiveNameList)
                    list.SetUnit(unitData);
                else
                    list.RemoveUnitIfUnitInList(unitData);
            }

            if (IsDebugMode)
                DebugNameLists.ForEach(l => SetOrRemoveUnit(l));
            else
                NameLists.ForEach(l => SetOrRemoveUnit(l));
        }

    }
}
