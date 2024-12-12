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
        /// DescriptionPoupuが表示されたときのListener
        /// </summary>
        public event System.Action OnOpenListener;
        /// <summary>
        /// DescriptionPopupが閉じたときのListener
        /// </summary>
        public event System.Action OnCloseListener;

        public SampleNode Node { get; private set; }

        Label label;

        public DescriptionPopup(SampleNode node)
        {
            Node = node;
        }

        // サイズ
        public override Vector2 GetWindowSize()
        {
            
            return new Vector2(300, 100);
        }

        public override void OnGUI(Rect rect)
        {
            // UI Toolkitを使う場合にはOnGUIには何も書かない
        }

        // 開く時の処理
        public override void OnOpen()
        {
            // editorWindow.rootVisualElementに対してUIを追加していく
            // UXMLファイルを使う場合には、editorWindow.rootVisualElementにCloneTreeしてもOK
            label = new Label(Node.Description);
            // LabelをNode.Descriptionが表示できるサイズに変更
            editorWindow.rootVisualElement.Add(label);
            OnOpenListener?.Invoke();
        }

        // 閉じる時の処理
        public override void OnClose()
        {
            OnCloseListener?.Invoke();
        }
    }
}
