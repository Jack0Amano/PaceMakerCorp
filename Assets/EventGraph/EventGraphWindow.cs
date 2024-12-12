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
        /// Graph�̃p�X
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
                    SaveChanges();

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

            // ���s�{�^��]
            rootVisualElement.Add(new Button(TestExecute) { text = "Execute" });

            // Ctrl+S�L�[�ŕۑ�����V���[�g�J�b�g�L�[
            graphView.RegisterCallback<KeyDownEvent>(evt =>
            {
               if (evt.actionKey && evt.keyCode == KeyCode.S)
               {
                   Save();
               }
            });

            // �E�N���b�N�Ń��j���[���o����SearchWindowProvider����Node���쐬����ۂɉE�N���b�N�����ʒu��Node������悤�ɂ��邽�߂�Callback
            graphView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == MouseButton.RightMouse.GetHashCode())
                {
                    graphView.searchWindowProvider.rightClickPosition = graphView.viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);
                }
            });

            // SearchWindowProvider��Node���쐬�����ۂɃZ�[�u��Ԃ�false�ɂ���d�g��
            graphView.nodeCreationRequest += ((ct) =>
            {
                hasUnsavedChanges = true;
            });

            graphView.searchWindowProvider.AddElementAction = AddElementFromProvider;

            // Script����OnEnable�����ꍇ�ł�callback�𕜋A�ł���悤��
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
        /// Content�̓��e��ۑ�����
        /// </summary>
        private void Save()
        {
            if (!hasUnsavedChanges)
                return;
            
            var dataIsSaved = Editor.EventGraphSaveUtility.SaveGraph(graphView);
            hasUnsavedChanges = !dataIsSaved;
        }

        /// <summary>
        /// path����f�[�^�����[�h����
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
        /// �e�X�g�p��Window���o���Ď��s
        /// </summary>
        private void TestExecute()
        {
        }
    }
}