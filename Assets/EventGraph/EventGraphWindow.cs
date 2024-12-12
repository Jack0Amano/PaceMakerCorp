using UnityEditor;
using EventGraph.Nodes;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine;
using System.IO;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using PopupWindow = UnityEditor.PopupWindow;
using static Utility;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Build.Content;
using Unity.Logging.Sinks;
using Unity.VisualScripting;
using MouseButton = UnityEngine.UIElements.MouseButton;
using AIGraph;

namespace EventGraph
{
    public class EventGraphWindow : EditorWindow
    {
        /// <summary>
        /// Graphのパス
        /// </summary>
        public string Path;

        public EventGraphView GraphView;

        TextField gUIDTextField;

        /// <summary>
        /// DescriptionPopupが表示されているかどうか
        /// </summary>
        public bool IsDescriptionPopupOpen;

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
                {
                    SaveChanges();
                    EditorWindow.GetWindow(typeof(EventGraphWindow)).Close();
                    EditorWindow.GetWindow(typeof(EventGraphWindow)).Show();
                }
                    

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

            GraphView = new EventGraphView()
            {
                style = { flexGrow = 1 },
            };
            GraphView.graphWindow = this;
            
            GraphView.graphViewChanged += ((arg) =>
            {
                hasUnsavedChanges = true;
                return arg;
            });

            if (File.Exists(Path))
            {
                Editor.EventGraphSaveUtility.LoadGraph(Path, GraphView);
            }
            rootVisualElement.Add(GraphView);

            // 実行ボタン]
            if (gUIDTextField == null)
                gUIDTextField = new TextField("GUID") { value = "GUID"};
            rootVisualElement.Add(gUIDTextField);
            rootVisualElement.Add(new Button(SearchNodesByGUID) { text = "Search" });

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

        private void OnDestroy()
        {
            //Debug.Log("Destroy and refresh");
            AssetDatabase.Refresh();
            Editor.EventGraphSaveUtility.SaveWindowInfo(GraphView);
        }

        /// <summary>
        /// Contentの内容を保存する
        /// </summary>
        private void Save()
        {
            if (!hasUnsavedChanges)
                return;
            
            var dataIsSaved = Editor.EventGraphSaveUtility.SaveGraph(GraphView);
            hasUnsavedChanges = !dataIsSaved;
        }

        /// <summary>
        /// pathからデータをロードする
        /// </summary>
        public void Load()
        {
            if (File.Exists(Path))
            {
                Editor.EventGraphSaveUtility.LoadGraph(Path, GraphView);
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
        /// Nodeの説明Popupを表示
        /// </summary>
        internal void ShowNodeDescription(SampleNode node)
        {
            var content = new DescriptionPopup(node);
            content.OnOpenListener += () =>
            {
                IsDescriptionPopupOpen = true;
            };
            content.OnCloseListener += () =>
            {
                IsDescriptionPopupOpen = false;
            };
            UnityEditor.PopupWindow.Show(node.worldBound, content);
        }

        /// <summary>
        /// テスト用のWindowを出して実行
        /// </summary>
        private void TestExecute()
        {
        }

        // EventGraphWindowクラス内のOpenメソッド内に追加
        private void SearchNodesByGUID()
        {
            var guid = gUIDTextField.value;
            if (guid.Length == 0)
            {
                PrintWarning("GUID is empty");
                return;
            }
            Print("SearchNodeByGUID", guid);
            // グラフ内のすべてのノードを取得
            var nodes = GraphView.graphElements.ToList();

            // GUIDを検索して一致するノードを表示するUIを作成
            var node = nodes.FirstOrDefault(n => n is SampleNode baseNode && baseNode.Guid == guid);
            if (node != null)
            {
                // ノードを表示するUIを作成する処理
                // 例えば、Debug.Logでノードの情報を表示するなど
                Debug.Log($"Node found: {node}");
                node.selected = true;
            }
            else
            {
                PrintWarning("Node not found");
            }
        }
    }
}