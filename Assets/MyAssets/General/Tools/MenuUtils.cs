using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using static Utility;
using UnityEngine.UIElements;

/// <summary>
/// ヒエラルキービューに右クリックメニューを追加
/// </summary>
public class MenuUtils
{
    /// <summary>
    /// 複数のイメージからマテリアルを作成する
    /// </summary>
    /// <param name="command"></param>
    [MenuItem("Assets/Image To Material")]
    private static void ImageToMaterial(MenuCommand command)
    {
        const string shaderName = "LoneStar/Common";
        const string baseColorMatch = "(_BaseMap)$";
        const string maskMatch = "(_MaskMap)$";
        const string roughnessMatch = "(_Roughness)$";
        const string normalMapMatch = "(_Normal)$";
        const string occlusionMatch = "(_AO)$";
        const string heightMapMatch = "(_Height)$";

        var material = new Material(Shader.Find(shaderName));
        var materialPath = "";
        var objs = Selection.objects;
        var textureCount = 0;
        foreach(var obj in objs)
        {
            var path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
            var ext = Path.GetExtension(path);
            if (ext == ".png")
            {
                var fileName = Path.GetFileNameWithoutExtension(path);

                if (obj.GetType() != typeof(Texture2D))
                    continue;
                var texture = obj as Texture2D;

                var pattern = "";
                if (Regex.IsMatch(fileName, baseColorMatch))
                {
                    material.SetTexture("_BaseColorMap", texture);
                    pattern = baseColorMatch;

                }
                else if (Regex.IsMatch(fileName, maskMatch))
                {
                    material.SetTexture("_MaskMap", texture);
                    pattern = maskMatch;
                }
                else if (Regex.IsMatch(fileName, roughnessMatch))
                {
                    material.SetTexture("_SpecGlossMap", texture);
                    pattern = roughnessMatch;
                }
                else if (Regex.IsMatch(fileName, normalMapMatch))
                {
                    // NormalはテクスチャタイプがNormalのため
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    importer.textureType = TextureImporterType.NormalMap;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                    material.SetTexture("_NormalMap", texture);
                    pattern = normalMapMatch;
                }
                else if (Regex.IsMatch(fileName, occlusionMatch))
                {
                    material.SetTexture("_OcclusionMap", texture);
                    pattern = occlusionMatch;
                }
                else if (Regex.IsMatch(fileName, heightMapMatch))
                {
                    material.SetTexture("_ParallaxMap", texture);
                    pattern = heightMapMatch;
                }
                else
                {
                    continue;
                }

                textureCount++;
                // マテリアル保存用のパスが作成されていない場合作成
                if (materialPath.Length == 0)
                {
                    var imageFileName = Path.GetFileNameWithoutExtension(path);
                    var nameReg = new Regex(pattern);
                    var matName = nameReg.Replace(imageFileName, "");

                    var imageFileDirectory = Path.GetDirectoryName(path);
                    // とりあえず同一ディレクトリにmatファイルを作成している
                    materialPath = Path.Combine(imageFileDirectory, $"{matName}.mat");
                    // 同じファイルが存在する場合は消す
                    if (File.Exists(materialPath))
                    {
                        AssetDatabase.DeleteAsset(materialPath);
                    }
                }

            }
        }

        // 保存できるマテリアルが作成できている場合は保存
        if (materialPath.Length != 0)
        {
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Make new material in {materialPath} from {textureCount} textures");
        }
    }

}

public class PutMaterialProps: EditorWindow
{
    //スクロール位置
    private Vector2 _scrollPosition = Vector2.zero;
    DropdownField dropdown;
    TextField textField;
    Button submitButton;
    List<Material> selectedMaterials;

    [MenuItem("Assets/Put material props")]
    public static void Open()
    {
        if (Selection.objects.ToList().Exists(o => typeof(Material) == o.GetType()))
            PutMaterialProps.GetWindow(typeof(PutMaterialProps));
    }

    private void OnEnable()
    {
        Selection.selectionChanged += Repaint;
        Selection.selectionChanged += UpdateList;

        var address = "Assets/MyAssets/General/Tools/PutMaterialProps.uxml";
        VisualTreeAsset uiTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(address);
        // Attach the UI from the visual tree asset
        uiTemplate.CloneTree(rootVisualElement);
        dropdown =  rootVisualElement.Q<DropdownField>("DropdownField");
        dropdown.RegisterValueChangedCallback(DropdownValueChangedCallback);

        textField = rootVisualElement.Q<TextField>("NewValue");
        submitButton = rootVisualElement.Q<Button>("Submit");
        submitButton.clicked += OnClickSubmitButton;

        UpdateList();
    }

    private void OnGUI()
    {
        
    }

    private void UpdateList()
    {
        selectedMaterials = Selection.objects.ToList().FindAll(o => typeof(Material) == o.GetType()).ConvertAll(o => (Material)o);
        if (selectedMaterials.Count == 0)
        {
            dropdown.choices.Clear();
            return;
        }

        var allProperties = new List<string>();
        for (var i = 0; i < selectedMaterials[0].shader.GetPropertyCount(); i++)
            allProperties.Add(selectedMaterials[0].shader.GetPropertyName(i));

        foreach (var m in selectedMaterials)
        {
            var all = new List<string>();
            for (var i = 0; i < m.shader.GetPropertyCount(); i++)
                all.Add(m.shader.GetPropertyName(i));
            for (var j = 0; j < allProperties.Count; j++)
                if (!all.Contains(allProperties[j]))
                    allProperties.RemoveAt(j);
        }
        dropdown.choices = allProperties;
    }

    private void DropdownValueChangedCallback(ChangeEvent<string> changeEvent)
    {
        if (changeEvent.newValue.Length == 0)
            return;
        // Floatのみとりあえず実装

    }

    /// <summary>
    /// 選択されたMaterialにValueを設置する
    /// </summary>
    private void OnClickSubmitButton()
    {
        if (float.TryParse(textField.value, out var value) && dropdown.value.Length != 0)
        {
            selectedMaterials.ForEach(m =>
            {
                m.SetFloat(dropdown.value, value);
            });
        }
    }
}


/// <summary>
/// Prefabを差し替えるウィンドウ
/// </summary>
public class ReplacingPrefabWindow : EditorWindow
{

    //スクロール位置
    private Vector2 _scrollPosition = Vector2.zero;

    //差し替え先のPrefab
    private GameObject _taretPrefab;

    //各値を固定するか
    private bool _isFixPosition = true, _isFixRotation = true, _isFixScale = true;

    //Undo時の名前
    private const string UndoName = " Prefabを差し替え";

    //=================================================================================
    //初期化
    //=================================================================================

    //メニューからウィンドウを表示
    [MenuItem("Tools/Open/ReplacingPrefabWindow")]
    public static void Open()
    {
        ReplacingPrefabWindow.GetWindow(typeof(ReplacingPrefabWindow));
    }

    private void OnEnable()
    {
        //選択されている物が変わったらGUIを更新するように
        Selection.selectionChanged += Repaint;
    }

    //=================================================================================
    //表示するGUIの設定
    //=================================================================================

    private void OnGUI()
    {
        //スクロール位置保存
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.scrollView);

        //差し替え先のPrefab選択UI
        EditorGUILayout.LabelField("差し替え先のPrefab");
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        _taretPrefab = (GameObject)EditorGUILayout.ObjectField(_taretPrefab, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        //各値を固定するかを設定するUI
        EditorGUILayout.LabelField("差し替え元の固定する項目");
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        _isFixPosition = EditorGUILayout.ToggleLeft("Position", _isFixPosition, GUILayout.Width(100));
        _isFixRotation = EditorGUILayout.ToggleLeft("Rotation", _isFixRotation, GUILayout.Width(100));
        _isFixScale = EditorGUILayout.ToggleLeft("Scale", _isFixScale);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        //選択中(差し替え元)のオブジェクト一覧
        EditorGUILayout.LabelField("差し替える(選択している)オブジェクト一覧");
        EditorGUILayout.BeginVertical(GUI.skin.box);
        var selectingTransforms = Selection.transforms;
        foreach (var selectingTransform in selectingTransforms)
        {
            EditorGUILayout.LabelField($"{selectingTransform.name}");
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        //差し替え実行ボタン
        if (_taretPrefab == null)
        {
            EditorGUILayout.HelpBox("差し替え先のPrefabが設定されていません", MessageType.Error);
        }
        else if (selectingTransforms.Length == 0)
        {
            EditorGUILayout.HelpBox("差し替えるオブジェクトが選択されていません", MessageType.Error);
        }
        else if (GUILayout.Button("差し替え実行"))
        {
            Replace(selectingTransforms);
        }

        //描画範囲が足りなかればスクロール
        EditorGUILayout.EndScrollView();
    }

    //=================================================================================
    //差し替え
    //=================================================================================

    private void Replace(Transform[] transforms)
    {
        foreach (var transform in transforms)
        {
            //元のオブジェクトの位置にPrefabのまま設置
            var newObject = (PrefabUtility.InstantiatePrefab(_taretPrefab) as GameObject).transform;
            Undo.RegisterCreatedObjectUndo(newObject.gameObject, UndoName);
            Undo.SetTransformParent(newObject, transform.parent, UndoName);
            newObject.SetSiblingIndex(transform.GetSiblingIndex());
            if (_isFixPosition)
            {
                newObject.position = transform.position;
            }
            if (_isFixRotation)
            {
                newObject.rotation = transform.rotation;
            }
            if (_isFixScale)
            {
                newObject.localScale = transform.localScale;
            }

            //元のオブジェクトをUndoに登録して削除
            Undo.DestroyObjectImmediate(transform.gameObject);
        }
    }

}