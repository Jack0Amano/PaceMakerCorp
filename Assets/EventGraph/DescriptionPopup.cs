using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using EventGraph.Nodes;

namespace EventGraph
{
    public class DescriptionPopup : PopupWindowContent
    {
        /// <summary>
        /// DescriptionPoupu���\�����ꂽ�Ƃ���Listener
        /// </summary>
        public event System.Action OnOpenListener;
        /// <summary>
        /// DescriptionPopup�������Ƃ���Listener
        /// </summary>
        public event System.Action OnCloseListener;

        public SampleNode Node { get; private set; }

        Label label;

        public DescriptionPopup(SampleNode node)
        {
            Node = node;
        }

        // �T�C�Y
        public override Vector2 GetWindowSize()
        {
            
            return new Vector2(300, 100);
        }

        public override void OnGUI(Rect rect)
        {
            // UI Toolkit���g���ꍇ�ɂ�OnGUI�ɂ͉��������Ȃ�
        }

        // �J�����̏���
        public override void OnOpen()
        {
            // editorWindow.rootVisualElement�ɑ΂���UI��ǉ����Ă���
            // UXML�t�@�C�����g���ꍇ�ɂ́AeditorWindow.rootVisualElement��CloneTree���Ă�OK
            label = new Label(Node.Description);
            // Label��Node.Description���\���ł���T�C�Y�ɕύX
            editorWindow.rootVisualElement.Add(label);
            OnOpenListener?.Invoke();
        }

        // ���鎞�̏���
        public override void OnClose()
        {
            OnCloseListener?.Invoke();
        }
    }
}
