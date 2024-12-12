using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Linq;

public class SearchMaterialUseShader : EditorWindow
{
	const string menuItemName = "Assets/Find/shader <- material";
	[MenuItem(menuItemName, false, 53)]
	static void Init()
	{
		var window = SearchMaterialUseShader.CreateInstance<SearchMaterialUseShader>();
		window.CollectionFileReferenceList();
		window.Search(Selection.activeObject);
		window.Show();
	}

	[MenuItem(menuItemName, true)]
	static bool ValidateLogSelectedTransformName()
	{
		if (Selection.activeObject == null)
		{
			return false;
		}
		var path = AssetDatabase.GetAssetPath(Selection.activeObject);
		return Path.GetExtension(path) == ".shader";
	}


	Vector2 scrollPos;
	string[] searchResults;

	void OnGUI()
	{
		using (var horizonal = new EditorGUILayout.HorizontalScope())
		{
			if (GUILayout.Button("select all material", EditorStyles.toolbarButton))
			{
				Selection.objects = referencedByList
					.Select(c => AssetDatabase.LoadAssetAtPath<Object>(c))
					.ToArray();
			}
		}
		using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPos))
		{
			scrollPos = scroll.scrollPosition;

			foreach (var filereference in referencedByList)
			{
				using (var horizonal = new EditorGUILayout.HorizontalScope())
				{
					var icon = AssetDatabase.GetCachedIcon(filereference);
					GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

					if (GUILayout.Button(filereference, EditorStyles.label))
					{
						Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(filereference);
					}
				}
			}
		}
	}

	List<Reference> fileReferenceList = new List<Reference>();
	string[] referencedByList = new string[0] { };

	void Search(Object target)
	{

		var path = AssetDatabase.GetAssetPath(target);
		var guid = AssetDatabase.AssetPathToGUID(path);

		referencedByList = fileReferenceList
			.Where(c => c.from != path)
			.Where(c => c.to == guid)
			.Select(c => c.from).ToArray();
	}

	void CollectionFileReferenceList()
	{
		fileReferenceList.Clear();
		var allMaterialFiles = Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories);
		foreach (var materialFile in allMaterialFiles)
		{
			var referenceList = AssetDatabase.GetDependencies(new string[] { materialFile });

			foreach (var reference in referenceList)
			{
				fileReferenceList.Add(new Reference()
				{
					from = materialFile,
					to = AssetDatabase.AssetPathToGUID(reference)
				});
			}
		}
	}

	class Reference
	{
		public string from;
		public string to;
	}
}