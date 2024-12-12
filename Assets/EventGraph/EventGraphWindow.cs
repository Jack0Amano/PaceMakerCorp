using UnityEditor;
using EventGraph.Nodes;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using System.Linq;

namespace EventGraph
{
    public class EventGraphWindow : EditorWindow
    {
        /// <summary>
        /// Graphのパス
        /// </summary>
        public string path;

        public EventGraphView graphView;

        [MenuItem("Window/EventGraph")]
        static void Init()
        {
            var window = (EventGraphWindow)EditorWindow.GetWindow(typeof(EventGraphWindow));

            window.saveChangesMessage = "This window has unsaved changes. Would you like to save?";
            window.Show();

            
        }

        void OnGUI()
        {
            // Graphの変更を検知 
            // https://docs.unity3d.com/ScriptReference/EditorWindow.SaveChanges.html
            saveChangesMessage = EditorGUILayout.TextField(saveChangesMessage);
            EditorGUILayout.LabelField(hasUnsavedChanges ? "I have changes!" : "No changes.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Try to close the window.");

            using (new EditorGUI.DisabledScope(hasUnsavedChanges))
            {
                if (GUILayout.Button("Create unsaved changes"))
                    hasUnsavedChanges = true;
            }

            using (new EditorGUI.DisabledScope(!hasUnsavedChanges))
            {
                if (GUILayout.Button("Save"))
                    SaveChanges();

                if (GUILayout.Button("Discard"))
                    DiscardChanges();
            }
        }

        /// <summary>
        /// バーからEventGraphWindowを開く
        /// </summary>
        [MenuItem("Window/Open EventGraphView")]
        public static void Open()
        {
            GetWindow<EventGraphWindow>("EventGraphView");
        }

        /// <summary>
        /// Windowが有効化されたときの呼び出し
        /// </summary>
        protected void OnEnable()
        {
            //hasUnsavedChanges = true;

            graphView = new EventGraphView()
            {
                style = { flexGrow = 1 },
            };
            
            graphView.graphViewChanged += ((arg) =>
            {
                hasUnsavedChanges = true;
                return arg;
            });

            if (File.Exists(path))
            {
                Editor.EventGraphSaveUtility.LoadGraph(path, graphView);
            }
            rootVisualElement.Add(graphView);

            // 実行ボタン]
            rootVisualElement.Add(new Button(TestExecute) { text = "Execute" });

            // Ctrl+Sキーで保存するショートカットキー
            graphView.RegisterCallback<KeyDownEvent>(evt =>
            {
               if (evt.actionKey && evt.keyCode == KeyCode.S)
               {
                   Save();
               }
            });

            // 右クリックでメニューを出してSearchWindowProviderからNodeを作成する際に右クリックした位置にNodeを作れるようにするためのCallback
            graphView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == MouseButton.RightMouse.GetHashCode())
                {
                    graphView.searchWindowProvider.rightClickPosition = graphView.viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);
                }
            });

            // SearchWindowProviderでNodeを作成した際にセーブ状態をfalseにする仕組み
            graphView.nodeCreationRequest += ((ct) =>
            {
                hasUnsavedChanges = true;
            });

            graphView.searchWindowProvider.AddElementAction = AddElementFromProvider;

            // ScriptからOnEnableした場合でもcallbackを復帰できるように
            var nodes = graphView.nodes.ToList().Cast<SampleNode>().ToList();
            nodes.ForEach(n =>
            {
                n.RegisterAnyValueChanged(NodeValueChanged);

                if (n is RootNode root)
                    graphView.RootNode = root;
            });

           
        }

        private void OnDestroy()
        {
            //Debug.Log("Destroy and refresh");
            AssetDatabase.Refresh();
            Editor.EventGraphSaveUtility.SaveWindowInfo(graphView);
        }

        /// <summary>
        /// Contentの内容を保存する
        /// </summary>
        private void Save()
        {
            if (!hasUnsavedChanges)
                return;
            
            var dataIsSaved = Editor.EventGraphSaveUtility.SaveGraph(graphView);
            hasUnsavedChanges = !dataIsSaved;
        }

        /// <summary>
        /// pathからデータをロードする
        /// </summary>
        public void Load()
        {
            if (File.Exists(path))
            {
                Editor.EventGraphSaveUtility.LoadGraph(path, graphView);
            }

            var nodes = graphView.nodes.ToList().Cast<SampleNode>().ToList();
            nodes.ForEach(n =>
            {
                n.RegisterAnyValueChanged(NodeValueChanged);

                if (n is RootNode root)
                    graphView.RootNode = root;
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