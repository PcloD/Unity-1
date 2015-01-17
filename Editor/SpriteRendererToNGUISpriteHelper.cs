using System;
using UnityEditor;
using UnityEngine;

public class SpriteRendererToNGUISpriteHelper : Editor
{
    [MenuItem("Tools/Sprite/Change to NGUI Sprite")]
    static void SpriteRendererChangeToUISprite()
    {
        //var assetPath = AssetDatabase.GetAssetPath(Selection.activeGameObject);
        //var prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
        var prefab = Selection.activeGameObject;
        if (prefab == null)
        {
            Debug.LogError("GameObject is null");
            return;
        }

        Debug.Log(string.Format("Doing... [{0}]", prefab));

        var renderers = ComponentUtil.GetComponents<Renderer>(prefab);
        foreach (var renderer in renderers)
        {
            var r = renderer as SpriteRenderer;
            if (r != null)
            {
                var go = r.gameObject;
                try
                {
                    DestroyImmediate(r, true);
                    if (null == go.GetComponent<UISprite>())
                    {
                        go.AddComponent<UISprite>();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                }
            }
        }

        Debug.Log("Done...");
    }
}
