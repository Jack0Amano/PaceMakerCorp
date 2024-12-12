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
        /// Graph�̃p�X
        /// </summary>
        public string Path;

        public EventGraphView GraphView;

        TextField gUIDTextField;

        /// <summary>
        /// DescriptionPopup���\������Ă��邩�ǂ���
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
            // Graph�̕ύX�����m 
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
        /// �o�[����EventGraphWindow���J��
        /// </summary>
        [MenuItem("Window/Open EventGraphView")]
        public static void Open()
        {
            GetWindow<EventGraphWindow>("EventGraphView");
        }

        /// <summary>
        /// Window���L�������ꂽ�Ƃ��̌Ăяo��
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

            // ���s�{�^��]
            if (gUIDTextField == null)
                gUIDTextField = new TextField("GUID") { value = "GUID"};
            rootVisualElement.Add(gUIDTextField);
            rootVisualElement.Add(new Button(SearchNodesByGUID) { text = "Search" });

            // Ctrl+S�L�[�ŕۑ�����V���[�g�J�b�g�L�[
            GraphView.RegisterCallback<KeyDownEvent>(evt =>
            {
               if (evt.actionKey && evt.keyCode == KeyCode.S)
               {
                   Save();
               }
            });

            // �E�N���b�N�Ń��j���[���o����SearchWindowProvider����Node���쐬����ۂɉE�N���b�N�����ʒu��Node������悤�ɂ��邽�߂�Callback
            GraphView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == MouseButton.RightMouse.GetHashCode())
                {
                    GraphView.searchWindowProvider.rightClickPosition = GraphView.viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);
                }
            });

            // SearchWindowProvider��Node���쐬�����ۂɃZ�[�u��Ԃ�false�ɂ���d�g��
            GraphView.nodeCreationRequest += ((ct) =>
            {
                hasUnsavedChanges = true;
            });

            GraphView.searchWindowProvider.AddElementAction = AddElementFromProvider;

            // Script����OnEnable�����ꍇ�ł�callback�𕜋A�ł���悤��
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
        /// Content�̓��e��ۑ�����
        /// </summary>
        private void Save()
        {
            if (!hasUnsavedChanges)
                return;
            
            var dataIsSaved = Editor.EventGraphSaveUtility.SaveGraph(GraphView);
            hasUnsavedChanges = !dataIsSaved;
        }

        /// <summary>
        /// path����f�[�^�����[�h����
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
        /// Widnow�����ۂɃZ�[�u���m�F����_�C�A���O����Save�{�^�����I�����ꂽ�Ƃ�
        /// </summary>
        public override void SaveChanges()
        {
            Save();
            base.SaveChanges();
        }

        /// <summary>
        /// Widnow�����ۂɃZ�[�u���m�F����_�C�A���O����Discard�{�^�����I�����ꂽ�Ƃ�
        /// </summary>
        public override void DiscardChanges()
        {
            base.DiscardChanges();
        }


        /// <summary>
        /// Provider����Element��ǉ������Ƃ��̌Ăяo��
        /// </summary>
        private void AddElementFromProvider(SampleNode node)
        {
            node.RegisterAnyValueChanged(NodeValueChanged);
        }

        /// <summary>
        /// Node��Value���ύX���ꂽ�ۂɌĂяo��
        /// </summary>
        private void NodeValueChanged(SampleNode node)
        {
            hasUnsavedChanges = true;
        }

        /// <summary>
        /// Node�̐���Popup��\��
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
        /// �e�X�g�p��Window���o���Ď��s
        /// </summary>
        private void TestExecute()
        {
        }

        // EventGraphWindow�N���X����Open���\�b�h���ɒǉ�
        private void SearchNodesByGUID()
        {
            var guid = gUIDTextField.value;
            if (guid.Length == 0)
            {
                PrintWarning("GUID is empty");
                return;
            }
            Print("SearchNodeByGUID", guid);
            // �O���t���̂��ׂẴm�[�h���擾
            var nodes = GraphView.graphElements.ToList();

            // GUID���������Ĉ�v����m�[�h��\������UI���쐬
            var node = nodes.FirstOrDefault(n => n is SampleNode baseNode && baseNode.Guid == guid);
            if (node != null)
            {
                // �m�[�h��\������UI���쐬���鏈��
                // �Ⴆ�΁ADebug.Log�Ńm�[�h�̏���\������Ȃ�
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