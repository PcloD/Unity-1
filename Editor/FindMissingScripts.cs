/* Introduction:to find all missing script on gameobject, prefab
 * Author:Mars
 * Update Date:
 * */
using UnityEngine;
using UnityEditor;
using System.IO;

public class FindMissingScripts : EditorWindow
{
	private static int go_count = 0, components_count = 0, missing_count = 0;
	
	[MenuItem("FP/Tools/FindMissingScripts")]
	public static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(FindMissingScripts));
	}

	public void OnGUI()
	{
		if(GUILayout.Button("Find Missing Scripts in selected Prefabs"))
		{
			go_count = 0;
			components_count = 0;
			missing_count = 0;

			FindInSelected(Selection.gameObjects);
			Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", go_count, components_count, missing_count));
		}

		if(GUILayout.Button("Find Missing Scripts in selected Directory"))
		{
			go_count = 0;
			components_count = 0;
			missing_count = 0;

			string mainPath = "";
			if(Selection.activeObject)
			{
				mainPath = AssetDatabase.GetAssetPath(Selection.activeObject);
				if(!Directory.Exists(mainPath))
					mainPath = Directory.GetParent(mainPath).ToString();
			}
			else
			{
				mainPath = Application.dataPath;
			}
			mainPath += "/";

			FindInDirectories(mainPath);
			Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", go_count, components_count, missing_count));
		}
	}

	private static void FindInSelected(GameObject[] go)
	{
		foreach(GameObject g in go)
		{
			FindInGameObject(g);
		}
	}

	private static void FindInGameObject(GameObject go)
	{
		go_count++;
		Component[] components = go.GetComponents<Component>();
		for(int i = 0; i < components.Length; i++)
		{
			components_count++;
			if(components[i] == null)
			{
				missing_count++;
				Debug.LogError(go.name + " has an empty script attached in position: " + i);
			}
		}

		if(go.transform.childCount == 0)
			return;

		for(int i = 0; i < go.transform.childCount; i++)
		{
			GameObject gochild = go.transform.GetChild(i).gameObject;
			FindInGameObject(gochild);
		}
	}

	private static void FindInDirectories(string path)
	{
		string[] objs = Directory.GetFiles(path, "*.prefab");
		foreach(string obj in objs)
		{
			GameObject go = AssetDatabase.LoadAssetAtPath(obj, typeof(Object)) as GameObject;
			if(go)
				FindInGameObject(go);
		}

		// sub dir
		string[] dirs = Directory.GetDirectories(path);
		if(dirs.Length == 0)
			return;
		foreach(string dir in dirs)
		{
			FindInDirectories(dir+"/");
		}
	}
}
