using UnityEditor;
using UnityEngine;
using System.Text;

public static class HierarchyDumper
{
    [MenuItem("Tools/Dump Selected Hierarchy")]
    static void Dump()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) { Debug.Log("Nichts selektiert!"); return; }
        StringBuilder sb = new();
        Print(go.transform, 0, sb);
        Debug.Log(sb.ToString());
    }

    static void Print(Transform t, int depth, StringBuilder sb)
    {
        string indent = new(' ', depth * 2);
        string components = "";
        foreach (Component c in t.GetComponents<Component>())
            if (c != null) components += c.GetType().Name + ", ";
        sb.AppendLine($"{indent}{t.name} [{components.TrimEnd(',', ' ')}]");
        for (int i = 0; i < t.childCount; i++)
            Print(t.GetChild(i), depth + 1, sb);
    }
}