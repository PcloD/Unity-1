using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ParticleShaderHelper
{
    private enum ChangeType
    {
        Need,
        NoNeed,
        Cannot
    }

    private const string MobileShader = "Mobile";

    static readonly Dictionary<string, string> ReplacementMap = new Dictionary<string, string>
    {
        { "Bumped Diffuse", "Mobile/Bumped Diffuse" },
        { "Bumped Specular", "Mobile/Bumped Specular" },
        { "Diffuse", "Mobile/Diffuse" },
        { "VertexLit", "Mobile/VertexLit" },
        { "Particles/Additive", "Mobile/Particles/Additive" },
        { "Particles/Additive (Soft)", "Mobile/Particles/Additive" },
        { "Particles/Alpha Blended", "Mobile/Particles/Alpha Blended" },
        //{ "Particles/Alpha Blended Premultiply", "Mobile/Particles/Alpha Blend_" },
        { "Particles/Multiply", "Mobile/Particles/Multiply" },
        { "Particles/Multiply (Double)", "Mobile/Particles/Multiply" },
        { "Particles/VertexLit Blended", "Mobile/Particles/VertexLit Blended" },
        //{ "~Additive-Multiply", "" },
    };

    private static readonly string[] ValidShaders =
    {
        "Additive",
        "Additive_",
        "Alpha Blend_",
        "Alpha Blended",
        "Multiply",
        "VertexLit Blended"
    };

    [MenuItem("Tools/Replace Shaders of Particle Materials to Mobile Version")]
    public static void ReplaceShadersOfParticleMaterialsToMobileVersion()
    {
        var searchPath = Path.Combine(Application.dataPath, "Particles");
        var dirs = Directory.GetDirectories(searchPath);
        var materialsNeedToProcess = new List<Material>();

        // Find all of materials which referenced by renderer.
        foreach (var dir in dirs)
        {
            var files = Directory.GetFiles(dir, "*.prefab");

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var assetPath = Path.Combine(fileInfo.DirectoryName, Path.GetFileName(file));
                assetPath = assetPath.Substring(Application.dataPath.Length - 6);
                var prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;

                if (prefab == null)
                {
                    continue;
                }

                var renderers = ComponentUtil.GetComponents<Renderer>(prefab);

                if (renderers == null)
                {
                    continue;
                }

                foreach (var renderer in renderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (renderer.sharedMaterials == null ||
                        renderer.sharedMaterials.Length == 0)
                    {
                        continue;
                    }

                    foreach (var material in renderer.sharedMaterials)
                    {
                        materialsNeedToProcess.Add(material);
                    }
                }
            }
        }

        if (materialsNeedToProcess.Count == 0)
        {
            return;
        }

        var count = 0;

        foreach (var material in materialsNeedToProcess)
        {
            if (EditorUtility.DisplayCancelableProgressBar(
                "Replace Shaders of Particle Materials to Mobile Version...",
                string.Format("Proessing \"{0}\"", material.name),
                (float)count / materialsNeedToProcess.Count))
            {
                break;
            }

            if (ReplaceShaderIfNeeded(material))
            {
                EditorUtility.SetDirty(material);
            }

            ++count;
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // Returns whether shader of material is need to replace.
    static bool ReplaceShaderIfNeeded(Material material)
    {
        if (material == null)
        {
            return false;
        }

        if (material.shader == null)
        {
            Debug.Log(material.name + " no shader.");
            return false;
        }

        Shader replacement;
        if (TryGetReplacementShader(material.shader.name, out replacement))
        {
            Debug.Log(string.Format("<b>Shader replaced:</b> Material='{0}' Prev='{1}' Current='{2}'",
                material.name, material.shader.name, replacement.name));
            material.shader = replacement;
            return true;
        }

        return false;
    }

    static bool TryGetReplacementShader(string shaderName, out Shader replacementShader)
    {
        replacementShader = null;

        if (string.IsNullOrEmpty(shaderName))
        {
            return false;
        }

        foreach (var replacement in ReplacementMap)
        {
            if (shaderName == replacement.Key)
            {
                replacementShader = Shader.Find(replacement.Value);

                if (replacementShader == null)
                {
                    Debug.LogError(replacement.Value + " not found. Use \"Mobile/Diffuse\"");
                    replacementShader = Shader.Find("Mobile/Diffuse");
                }

                return true;
            }
        }

        return false;
    }

    private static ChangeType IsNeedChange(string name)
    {
        var splits = name.Split('/');
        if (splits.Length == 0)
        {
            return ChangeType.Cannot;
        }

        return string.CompareOrdinal(splits[0], MobileShader) == 0 ? ChangeType.NoNeed : ChangeType.Need;
    }

    private static bool HasMobileShdaer(string name)
    {
        var splits = name.Split('/');
        if (splits.Length == 0)
        {
            return false;
        }

        var shader = splits[splits.Length - 1];
        foreach (var vs in ValidShaders)
        {
            if (string.CompareOrdinal(shader, vs) == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static Shader RelpaceShader(string name)
    {
        if (HasMobileShdaer(name))
        {
            var newShader = string.Format("{0}/{1}", MobileShader, name);
            var s = Shader.Find(newShader);
            if (s == null)
            {
                Debug.LogError(string.Format("Relpace shader failed! [{0}]", newShader));
            }

            return s;
        }
        else
        {
            Debug.LogError(string.Format("Have no shader to replace! [{0}]", name));
            return null;
        }
    }
}
