using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SearchUnitAndItem : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    UnitData unitData;

    [MenuItem("Window/UI Toolkit/SearchUnitAndItem")]
    public static void ShowExample()
    {
        SearchUnitAndItem wnd = GetWindow<SearchUnitAndItem>();
        wnd.titleContent = new GUIContent("SearchUnitAndItem");
    }

    private void OnEnable()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
    }

    public void CreateGUI()
    {


        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        var unitPanel = labelFromUXML.Q<VisualElement>("Unit");
    }
}
