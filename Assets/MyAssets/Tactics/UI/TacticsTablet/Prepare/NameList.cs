using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TMPro.SpriteAssetUtilities;
using UnityEngine.UI;
using System;
using DG.Tweening;
using System.Linq;
using Tactics;

namespace TacticsTablet.Prepare
{
    /// <summary>
    /// Prepare画面の出撃Unitとかを表示しておくListの
    /// </summary>
    public class NameList : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI location;
        [SerializeField] public List<TextMeshProUGUI> labels;
        [Tooltip("道順選択のドロップダウン")]
        [SerializeField] internal List<TMP_Dropdown> WaysDropdownLists;
        [SerializeField] internal UnitAttribute UnitAttribute;
        [SerializeField] int weight = 4;
        [SerializeField] Color DisactiveColor;
        [SerializeField] Color ActiveColor;
        [SerializeField] internal Button button;
        [Tooltip("Debugでunitのスポーンする位置を指定するdropdown")]
        [SerializeField] internal TMP_Dropdown SelectLocationDropdown;


        internal List<UnitData> UnitDatas = new List<UnitData>();
        [NonSerialized] public List<CanvasGroup> labelCanvases = new List<CanvasGroup>();
        [NonSerialized] public List<Button> labelDeleteButtons = new List<Button>();

        private Image BackgroundImage;

        /// <summary>
        /// 道順選択のドロップダウン
        /// </summary>
        public List<string> WaysDropdownValues
        {
            get
            {
                if (WaysDropdownLists == null || WaysDropdownLists.Count == 0) return new List<string>();
                return WaysDropdownLists[0].options.ConvertAll(o => o.text);
            }
            set
            {
                if (WaysDropdownLists == null) return;
                var options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData() };
                options.AddRange( value.ConvertAll(v => new TMP_Dropdown.OptionData(v)));

                WaysDropdownLists.ForEach(d =>
                {
                    d.options = options;
                });
            }
        }

        /// <summary>
        /// 現在選択中のNameListであるか
        /// </summary>
        internal bool IsActive
        {
            get => isActive;
            set
            {
                BackgroundImage.DOColor(value ? ActiveColor : DisactiveColor, 0.3f);
                isActive = value;
            }
        }
        private bool isActive;

        /// <summary>
        /// Unitの位置する初期tileのid
        /// </summary>
        internal string TileID
        {
            get => tileID;
            set
            {
                tileID = value;
                location.text = value;
            }
        }
        private string tileID;

        /// <summary>
        /// UnitをSpawnPointから削除する際の呼び出し
        /// </summary>
        public Action<UnitData> removeUnitAction;

        private CanvasGroup CanvasGroup;

        protected private void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            BackgroundImage = GetComponent<Image>();
            labels.ForEach((l) =>
            {
                var canvas = l.GetComponent<CanvasGroup>();
                canvas.alpha = 0;
                labelCanvases.Add(canvas);
                //var button = l.GetComponentInChildren<Button>();
                //button.onClick.AddListener(() => RemoveUnitFromSpawnPoint(button));
                //labelDeleteButtons.Add(button);
            });
            SelectLocationDropdown.onValueChanged.AddListener((i) => DropdownLocationIsChanged());
        }

        /// <summary>
        /// UnitDataがどの道順を辿るループを再生するかのindexを取得
        /// </summary>
        /// <param name="unitData"></param>
        /// <returns></returns>
        public int GetIndexOfWaysDropdown(UnitData unitData)
        {
            var index = UnitDatas.IndexOf(unitData);
            if (WaysDropdownLists.IndexAt(index, out var dropdown))
            {
                return dropdown.value-1;
            }
            return -1;
        }

        /// <summary>
        /// スポーンする位置のid一覧
        /// </summary>
        /// <param name="idList"></param>
        internal void SetDropdownLocations(List<string> idList)
        {
            SelectLocationDropdown.ClearOptions();
            SelectLocationDropdown.AddOptions(idList);
            tileID = idList.First();
        }

        /// <summary>
        /// SelectLocationDropdownの値が変更されたときの呼び出し
        /// </summary>
        private void DropdownLocationIsChanged()
        {
            SelectLocationDropdown.options.ConvertAll(o => o.text).IndexAt_Bug(SelectLocationDropdown.value, out tileID);
        }

        /// <summary>
        /// どのUnitSpawnPointPairのどこのUnitが削除されるのか出す
        /// </summary>
        /// <param name="pair"></param>
        /// <param name="button"></param>
        private void RemoveUnitFromSpawnPoint(Button button)
        {
            var labelIndex = labelDeleteButtons.FindIndex(b => b.Equals(button));
            if (UnitDatas.IndexAt_Bug(labelIndex, out var removedUnit))
            {
                removeUnitAction?.Invoke(removedUnit);
                RemoveUnit(labelIndex);
            }
        }

        /// <summary>
        /// UnitをCellに設置可能であれば設置する
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool SetUnit(UnitData parameter)
        {
            var sumWeight = UnitDatas.Sum(u => u.Weight);
            if (parameter.Weight <= (weight - sumWeight))
            {
                UnitDatas.Add(parameter);
                if (labels.IndexAt_Bug(UnitDatas.Count - 1, out var label))
                {
                    label.SetText(parameter.Name);
                    labelCanvases[UnitDatas.Count - 1].DOFade(1, 0.5f);
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// 指定されたパラメーターのラベルを揺らす
        /// </summary>
        /// <param name="unitParameter"></param>
        public bool ShakeLabelIfUnitInList(UnitData unitParameter)
        {
            var index = UnitDatas.FindIndex(p => p.Equals(unitParameter));
            if (index == -1)
                return false;

            if (labels.IndexAt_Bug(index, out var label))
            {
                label.transform.DOShakeX();
                return true;
            }

            return false;
        }

        /// <summary>
        /// UnitParamterのUnitを出撃リストから削除する
        /// </summary>
        /// <param name="unitParameter"></param>
        public void RemoveUnitIfUnitInList(UnitData unitParameter)
        {
            var index = UnitDatas.FindIndex(p => p.Equals(unitParameter));
            if (index == -1)
                return;

            RemoveUnit(index);
        }

        /// <summary>
        /// すべてのUnitを出撃リストアkら削除する
        /// </summary>
        public void RemoveAllUnitsInList()
        {
            var disappear = DOTween.Sequence();
            //UnitDatas.Clear();
            for(var i=0; i < UnitDatas.Count; i++)
            {
                var label = labels[i];
                disappear.Join(labelCanvases[i].DOFade(0, 0.3f).OnComplete(() =>
                {
                    label.SetText("");
                }));
            }
            UnitDatas.Clear();
        }

        /// <summary>
        /// labelIndex番目のUnitを削除する
        /// </summary>
        /// <param name="labelIndex"></param>
        /// <returns></returns>
        private void RemoveUnit(int labelIndex)
        {
            var disappear = DOTween.Sequence();
            UnitDatas.RemoveAt(labelIndex);
            //seq.Append(labelCanvases[labelIndex].DOFade(0, 3f));

            var delay = 0f;
            for (int i = 0; i < UnitDatas.Count; i++)
            {
                var label = labels[i];
                var name = UnitDatas[i].Name;
                var canvas = labelCanvases[i];
                if (name != label.text)
                {
                    delay += 0.1f;
                    disappear.Join(canvas
                        .DOFade(0, 0.3f)
                        .SetDelay(delay)
                        .OnComplete(() =>
                        {
                            label.SetText(name);
                        }));
                }
            }

            var lastCanvas = labelCanvases[UnitDatas.Count];
            var lastLabel = labels[UnitDatas.Count];
            disappear.Join(lastCanvas
                .DOFade(0, 0.3f)
                .SetDelay(delay)
                .OnComplete(() =>
                {
                    lastLabel.SetText("");
                }));

            delay = 0;
            var appear = DOTween.Sequence();
            for (int i = UnitDatas.Count - 1; i >= 0; i--)
            {
                appear.Join(labelCanvases[i]
                    .DOFade(1, 0.3f)
                    .SetDelay(delay));
                delay += 0.1f;
            }

            disappear.Append(appear);
            disappear.Play();
        }
    }
}