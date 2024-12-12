using UnityEditor;
using AIGraph.Nodes;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using System.Linq;
using System.Collections.Generic;
using static Utility;

namespace AIGraph
{
    public class AIGraphWindow : EditorWindow
    {
        /// <summary>
        /// Graphのパス
        /// </summary>
        public string path;
        /// <summary>
        /// Windowに表示するview
        /// </summary>
        public AIGraphView GraphView;

        public bool IsDebugMode = false;

        static void Init()
        {

            var window = CreateInstance<AIGraphWindow>();
            window.titleContent = new GUIContent("AIGraph");

            window.saveChangesMessage = "This window has unsaved changes. Would you like to save?";
            window.Show();
        }

        void OnGUI()
        {
            // Graphの変更を検知 
            // https://docs.unity3d.com/ScriptReference/EditorWindow.SaveChanges.html

            //saveChangesMessage = EditorGUILayout.TextField(saveChangesMessage);
            //EditorGUILayout.LabelField(hasUnsavedChanges ? "I have changes!" : "No changes.", EditorStyles.wordWrappedLabel);
            //EditorGUILayout.LabelField("Try to close the window.");

            //using (new EditorGUI.DisabledScope(hasUnsavedChanges))
            //{
            //    if (GUILayout.Button("Create unsaved changes"))
            //        hasUnsavedChanges = true;
            //}

            //using (new EditorGUI.DisabledScope(!hasUnsavedChanges))
            //{
            //    if (GUILayout.Button("Save"))
            //        SaveChanges();

            //    if (GUILayout.Button("Discard"))
            //        DiscardChanges();
            //}
        }

        /// <summary>
        /// バーからAIGraphWindowを開く
        /// </summary>
        [MenuItem("Window/Open AIGraphView")]
        public static void Open()
        {
            GetWindow<AIGraphWindow>("AIGraphView");
        }

        /// <summary>
        /// Windowが有効化されたときの呼び出し
        /// </summary>
        protected void OnEnable()
        {
            DrawGraph();
        }

        /// <summary>
        /// 表示内容を与えられたGraphViewの形にupdateする
        /// </summary>
        public void DrawGraph()
        {

            if (!IsDebugMode)
            {
                GraphView = new AIGraphView()
                {
                    style = { flexGrow = 1 },
                };

                GraphView.graphViewChanged += ((arg) =>
                {
                    hasUnsavedChanges = true;
                    return arg;
                });

                if (File.Exists(path))
                {
                    Editor.AIGraphSaveUtility.LoadGraph(path, GraphView);
                }
            }
            else
            {
                rootVisualElement.Children().ToList().ForEach(e =>
                {
                    rootVisualElement.Remove(e);
                });

                GraphView.style.flexGrow = 1;
                GraphView = Editor.AIGraphSaveUtility.LoadGraph(GraphView.dataContainer, GraphView);
            }

            rootVisualElement.Add(GraphView);

            // 実行ボタン]
            //rootVisualElement.Add(new Button(TestExecute) { text = "Execute" });

            // Ctrl+Sキーで保存するショートカットキー
            GraphView.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.actionKey && evt.keyCode == KeyCode.S)
                {
                    Save();
                }
            });

            // 右クリックでメニューを出してSearchWindowProviderからNodeを作成する際に右クリックした位置にNodeを作れるようにするためのCallback
            GraphView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == MouseButton.RightMouse.GetHashCode())
                {
                    GraphView.searchWindowProvider.rightClickPosition = GraphView.viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);
                }
            });

            // SearchWindowProviderでNodeを作成した際にセーブ状態をfalseにする仕組み
            GraphView.nodeCreationRequest += ((ct) =>
            {
                hasUnsavedChanges = true;
            });

            GraphView.searchWindowProvider.AddElementAction = AddElementFromProvider;

            // ScriptからOnEnableした場合でもcallbackを復帰できるように
            var nodes = GraphView.nodes.ToList().Cast<SampleNode>().ToList();
            nodes.ForEach(n =>
            {
                n.RegisterAnyValueChanged(NodeValueChanged);

                if (n is RootNode root)
                    GraphView.RootNode = root;
            });
        }

        /// <summary>
        /// 開かれているすべてのEditorWindowを取得
        /// </summary>
        /// <returns></returns>
        public static List<AIGraphWindow> GetAllOpenEditorWindows()
        {
            return Resources.FindObjectsOfTypeAll<AIGraphWindow>().ToList();
        }

        private void OnDestroy()
        {
            if (!IsDebugMode)
            {
                //Debug.Log("Destroy and refresh");
                AssetDatabase.Refresh();
                Editor.AIGraphSaveUtility.SaveWindowInfo(GraphView);
            }

        }

        /// <summary>
        /// Contentの内容を保存する
        /// </summary>
        private void Save()
        {
            if (!hasUnsavedChanges || IsDebugMode)
                return;

            var dataIsSaved = Editor.AIGraphSaveUtility.SaveGraph(GraphView);
            hasUnsavedChanges = !dataIsSaved;
        }

        /// <summary>
        /// pathからデータをロードする
        /// </summary>
        public void Load()
        {
            if (File.Exists(path))
            {
                Editor.AIGraphSaveUtility.LoadGraph(path, GraphView);
            }
            else
            {
                PrintError($"File dosent exist on {path}");
            }

            var nodes = GraphView.nodes.ToList().Cast<SampleNode>().ToList();
            nodes.ForEach(n =>
            {
                n.RegisterAnyValueChanged(NodeValueChanged);

                if (n is RootNode root)
                    GraphView.RootNode = root;
            });
        }

        /// <summary>
        /// Widnowを閉じる際にセーブを確認するダイアログからSaveボタンが選択されたとき
        /// </summary>
        public override void SaveChanges()
        {
            Save();
            base.SaveChanges();
        }

        /// <summary>
        /// Widnowを閉じる際にセーブを確認するダイアログからDiscardボタンが選択されたとき
        /// </summary>
        public override void DiscardChanges()
        {
            base.DiscardChanges();
        }

        /// <summary>
        /// ProviderからElementを追加したときの呼び出し
        /// </summary>
        private void AddElementFromProvider(SampleNode node)
        {
            node.RegisterAnyValueChanged(NodeValueChanged);
        }

        /// <summary>
        /// NodeのValueが変更された際に呼び出し
        /// </summary>
        private void NodeValueChanged(SampleNode node)
        {
            hasUnsavedChanges = true;
        }

        /// <summary>
        /// テスト用のWindowを出して実行
        /// </summary>
        private void TestExecute()
        {
        }
    }
}