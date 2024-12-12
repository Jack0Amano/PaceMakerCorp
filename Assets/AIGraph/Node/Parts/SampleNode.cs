using UnityEditor.Experimental.GraphView;
using System;
using UnityEngine;
using AIGraph.Editor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Tactics.Map;
using Tactics.Character;
using AIGraph.InOut;

namespace AIGraph.Nodes
{

    public abstract class SampleNode : Node
    {
        /// <summary>
        /// NodeのID 実行時の検索用に使う
        /// </summary>
        public string Guid;

        public Color Color;
        public bool IsEntryPoint;

        /// <summary>
        /// NodeのSearchWindow上での位置を指定するpath
        /// </summary>
        public string NodePath = "";

        /// <summary>
        /// node内のfieldに何らかのvalueの変更が行われた際に呼び出すための登録 
        /// </summary>
        protected Action<SampleNode> RegisterAnyValueChangedCallback;

        public string DebugText;

        /// <summary>
        /// Tacticsから与えられる状況
        /// </summary>
        public EnvironmentData EnvironmentData;

        /// <summary>
        /// AIが動かすUnit
        /// </summary>
        public UnitController MyUnitController
        {
            get => EnvironmentData.MyUnitController;
        }
        /// <summary>
        /// Units全体のコントローラー
        /// </summary>
        public UnitsController UnitsController
        {
            get => EnvironmentData.UnitsController;
        }
        /// <summary>
        /// Tile全体のコントローラー
        /// </summary>
        public TilesController TilesController
        {
            get => EnvironmentData.TilesController;
        }
        /// <summary>
        /// 移動ルーチンのPassPoints
        /// </summary>
        public List<(Transform point, TileCell tile)> WayPassPoints
        {
            get => EnvironmentData.WayPassPoints;
        }

        public TileCell CurrentTile
        {
            get => MyUnitController.tileCell;
        }

        public Tactics.AI.AIController AIController
        {
            get => MyUnitController.aiController;
        }

        public SampleNode()
        {
            // PanelがEnableになったとき
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                // 各種登録されたポートの色を決定
                outputContainer.Query<Port>().ToList().ForEach(p => SetPortColorFromType(p));
                inputContainer.Query<Port>().ToList().ForEach(p => SetPortColorFromType(p));
            });
        }

        private void SetPortColorFromType(Port port)
        {
        }

        public static string GetNodePath()
        {
            return nameof(SampleNode).Replace("Node", "");
        }

        /// <summary>
        /// Nodeの開閉ボタン
        /// </summary>
        protected override void ToggleCollapse()
        {
            base.ToggleCollapse();
        }

        /// <summary>
        /// node内のfieldに何らかのvalueの変更が行われた際に呼び出すための登録 *通常fieldのRegisterAnyValueChangedCallbackにactionを登録する*
        /// </summary>
        /// <param name="action"></param>
        public virtual void RegisterAnyValueChanged(Action<SampleNode> action)
        {
            RegisterAnyValueChangedCallback = action;
        }

        // NOTE Castしても中身は失われない ただしInit内でSimple内の使用するUIElementを予め初期化して置かなければならない
        /// <summary>
        /// NodeDataからデータを読み込み各UIElementのvalueに配置する
        /// </summary>
        /// <param name="data"></param>
        public virtual void Load(Editor.NodeData data)
        {
            SetPosition(new Rect(data.Position, data.Size));

            Guid = data.Guid;
            IsEntryPoint = data.IsEntryPoint;
            expanded = data.Expanded;
        }

        /// <summary>
        /// NodeDataの実データRawにNodeの保存すべき情報をSaveする
        /// </summary>
        /// <param name="raw"></param>
        public virtual NodeData Save()
        {
            var data = new NodeData();
            data.Guid = Guid;
            data.Keyword = GetType().Name;
            data.IsEntryPoint = IsEntryPoint;
            data.Position = GetPosition().position;
            data.Size = GetPosition().size;
            data.Expanded = expanded;



            return data;
        }

        /// <summary>
        /// gridの位置が<c>enemyPos</c>からカバーされる位置にいるかどうか
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="enemyPos"></param>
        /// <returns></returns>
        internal bool IsCoveredFromEnemy(PointInTile grid, Vector3 enemyPos)
        {
            if (grid.coverObject == null)
                return false;

            var posC = new Vector2(grid.coverObject.transform.position.x,
                       grid.coverObject.transform.position.z);
            var posE = new Vector2(enemyPos.x, enemyPos.z);
            var posG = new Vector2(grid.location.x, grid.location.z);
            var rad = Utility.RadianOfTwoVector(posE - posC, posG - posC);
            var deg = rad / (Mathf.PI / 180);

            return deg > 100;
        }

        /// <summary>
        /// 合計を<c>a</c>を<c>rateA</c>倍の値に、<c>b</c>を<c>rateB</c>倍にしその合計値を返す
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="rateA"></param>
        /// <param name="rateB"></param>
        /// <returns></returns>
        protected float CalcRate(float a, float b, float rateA, float rateB)
        {
            return a * rateA + b * rateB;
        }

        public override string ToString()
        {
            return $"{base.GetType()}";
        }
    }
}