using System;
using System.Collections.Generic;
using UnityEngine;

public static class ComponentUtil
{
    public static T[] GetComponents<T>(GameObject root, Predicate<T> predicate = null)
        where T : Component
    {
        if (root == null)
        {
            return null;
        }

        
        var components = root.GetComponentsInChildren<T>(true);
        var list = new List<T>();
        foreach (var component in components)
        {
            if (predicate != null)
            {
                if (predicate(component))
                {
                    list.Add(component);
                }
            }
            else
            {
                list.Add(component);
            }
        }

        return list.ToArray();
    }

    public static T GetOrAddComponent<T>(GameObject go, ref T component)
        where T : Component
    {
        if (go == null)
        {
            return component;
        }

        if (component != null)
        {
            return component;
        }

        component = go.GetComponent<T>() ?? go.AddComponent<T>();
        return component;
    }
}
