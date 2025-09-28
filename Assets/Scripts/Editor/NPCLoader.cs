// Datei: Assets/Editor/NPCLoaderWindow.cs
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public class NPCLoaderWindow : EditorWindow
{
    private string status = "Ready";
    private Vector2 scroll;

    [MenuItem("Tools/NPCLoader")]
    public static void ShowWindow()
    {
        var w = GetWindow<NPCLoaderWindow>("NPCLoader");
        w.minSize = new Vector2(350, 150);
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("NPCLoader (Editor)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Hello World — dieses Fenster ist dein NPCLoader-Editor-Tool.\n\n" +
                                "Drücke den Button unten, um ein Beispiel-Asset zu erzeugen (CharacterTemplate, falls vorhanden).", MessageType.Info);

        GUILayout.Space(6);

        if (GUILayout.Button("Hello World / Erzeuge Beispiel-Asset", GUILayout.Height(30)))
        {
            Debug.Log("Hello World from NPCLoaderWindow!");
        }

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Status:");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(60));
        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            status = "Refreshed at " + DateTime.Now.ToLongTimeString();
        }
        if (GUILayout.Button("Close", GUILayout.Width(80)))
        {
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
