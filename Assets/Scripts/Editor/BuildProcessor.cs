using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tolik.RemakeSoF.Runtime;
using Unity.Multiplayer;
using Unity.Multiplayer.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tolik.RemakeSoF.Editor
{
    ///<summary>
    ///Performs additional operations before/after the build is done
    ///</summary>
    public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static readonly string[] k_BuildOnlySymbols = new string[]
        {
            //"LIVE", //this is an example, add your own symbols instead
        };

        static readonly string[] k_EditorOnlySymbols = new string[]
        {
            //"DEV", //this is an example, add your own symbols instead
        };

        /// <summary>
        /// CallbackOrder of the preprocessing and postprocessing calls.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Called at the beginning of the build process
        /// </summary>
        /// <param name="report">The generated build report.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            EnsureAlwaysIncludedShaders();
            DisableBurstCompiler();
            //ApplyChangesToMetagameApplication();

            string definesString = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            List<string> allDefines = definesString.Split(';').ToList();
            if (k_BuildOnlySymbols.Length > 0)
            {
                allDefines.AddRange(k_BuildOnlySymbols.Except(allDefines));
            }

            if (k_EditorOnlySymbols.Length > 0)
            {
                allDefines.RemoveAll(def => k_EditorOnlySymbols.Contains(def));
            }

            Debug.Log($"Symbols used for build: {string.Join(";", allDefines.ToArray())}");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), string.Join(";", allDefines.ToArray()));
        }

        /// <summary>
        /// Alle Shader die per Shader.Find() zur Laufzeit geladen werden,
        /// muessen in Always Included Shaders stehen, da Unity sie sonst aus dem Build strippt.
        /// </summary>
        static void EnsureAlwaysIncludedShaders()
        {
            string[] requiredShaderNames = new string[]
            {
                "Universal Render Pipeline/Unlit",
                "SoF2/MapSurface",
                "Universal Render Pipeline/Particles/Unlit",
                "Particles/Standard Unlit",
                "Sprites/Default",
                "Skybox/6 Sided",
                "Unlit/Color",
            };

            SerializedObject graphicsSettings = new(GraphicsSettings.GetGraphicsSettings());
            SerializedProperty alwaysIncluded = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");

            foreach (string shaderName in requiredShaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning($"[BuildProcessor] Shader '{shaderName}' not found in project, skipping.");
                    continue;
                }

                bool alreadyIncluded = false;
                for (int i = 0; i < alwaysIncluded.arraySize; i++)
                {
                    if (alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        alreadyIncluded = true;
                        break;
                    }
                }

                if (!alreadyIncluded)
                {
                    int index = alwaysIncluded.arraySize;
                    alwaysIncluded.InsertArrayElementAtIndex(index);
                    alwaysIncluded.GetArrayElementAtIndex(index).objectReferenceValue = shader;
                    Debug.Log($"[BuildProcessor] Added '{shaderName}' to Always Included Shaders.");
                }
            }

            graphicsSettings.ApplyModifiedProperties();
        }

        void DisableBurstCompiler()
        {
            //unfortunately we can't use burst compilation due to a 
            //bug in its latest version, so we need to disable it.
            //It annoyingly re-enables every time you switch platform...
            //Burst.BurstCompiler.Options.EnableBurstCompilation = false;
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Called at the end of the build process
        /// </summary>
        /// <param name="report">The generated build report.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            DisableBurstCompiler();
            //RevertChangesToMetagameApplication();
            string definesString = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            List<string> allDefines = definesString.Split(';').ToList();

            if (k_BuildOnlySymbols.Length > 0)
            {
                allDefines.RemoveAll(def => k_BuildOnlySymbols.Contains(def));
            }

            if (k_EditorOnlySymbols.Length > 0)
            {
                allDefines.AddRange(k_EditorOnlySymbols.Except(allDefines));
            }

            Debug.Log($"Symbols restored after build: {string.Join(";", allDefines.ToArray())}");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), string.Join(";", allDefines.ToArray()));
            AssetDatabase.SaveAssets();
#if !CLOUD_BUILD_WINDOWS && !CLOUD_BUILD_LINUX && !CLOUD_BUILD_MAX
            bool isServerBuild = EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server;
            Debug.Log($"Manually Doing PostExport: {report.summary.outputPath} (isServer={isServerBuild})");
            CloudBuildHelpers.PostExport(report.summary.outputPath, isServerBuild);

            if (!isServerBuild)
            {
                CopyLooseAssetsToClientBuild(report.summary.outputPath);
            }
#endif
        }

        /// <summary>
        /// Kopiert Art/ und Resources/Data/shaders/ in Game_Data/ des Client-Builds.
        /// Application.dataPath zeigt im Build auf Game_Data/, daher muessen die Dateien dort liegen.
        /// Server-Builds brauchen keine Texturen, Sounds oder Legacy-Shader.
        /// </summary>
        static void CopyLooseAssetsToClientBuild(string outputPath)
        {
            string buildDir = File.Exists(outputPath)
                ? Path.GetDirectoryName(outputPath)
                : outputPath;

            // Game_Data Ordner ermitteln (Name = Executable ohne Extension + _Data)
            string exeName = Path.GetFileNameWithoutExtension(outputPath);
            string gameDataDir = Path.Combine(buildDir, exeName + "_Data");

            string artSource = Path.Combine(Application.dataPath, "Art");
            string artDest = Path.Combine(gameDataDir, "Art");
            if (Directory.Exists(artSource))
            {
                Debug.Log($"[BuildProcessor] Copying Art/ to {artDest}");
                CopyDirectoryRecursive(artSource, artDest);
            }

            string shaderSource = Path.Combine(Application.dataPath, "Resources", "Data", "shaders");
            string shaderDest = Path.Combine(gameDataDir, "Resources", "Data", "shaders");
            if (Directory.Exists(shaderSource))
            {
                Debug.Log($"[BuildProcessor] Copying shaders/ to {shaderDest}");
                CopyDirectoryRecursive(shaderSource, shaderDest);
            }
        }

        /// <summary>
        /// Kopiert ein Verzeichnis rekursiv. Ueberschreibt vorhandene Dateien, ignoriert .meta Dateien.
        /// </summary>
        static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                if (file.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectoryRecursive(dir, destSubDir);
            }
        }

        void ApplyChangesToMetagameApplication()
        {
            MetagameApplication app = FindMetagameAppInProject();

            //add your code to apply changes to the MetagameApplication here, I.E: to reference different testing environments
            PrefabUtility.SavePrefabAsset(app.gameObject, out bool savedSuccessfully);
            if (!savedSuccessfully)
            {
                throw new BuildPlayerWindow.BuildMethodException("Failed to alter MetagameApplication before building");
            }

            Debug.Log("Updated MetagameApp before build");
        }

        void RevertChangesToMetagameApplication()
        {
            MetagameApplication app = FindMetagameAppInProject();

            //add your code to revert changes to the MetagameApplication here, I.E: to reference different testing environments
            PrefabUtility.SavePrefabAsset(app.gameObject, out bool savedSuccessfully);
            if (!savedSuccessfully)
            {
                throw new BuildPlayerWindow.BuildMethodException("Failed to restore MetagameApplication after building");
            }

            Debug.Log("Updated MetagameApp after build");
        }

        MetagameApplication FindMetagameAppInProject()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] {"Assets/Prefabs/Metagame"}))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = (GameObject) AssetDatabase.LoadMainAssetAtPath(path);
                if (root.GetComponent<MetagameApplication>())
                {
                    return root.GetComponent<MetagameApplication>();
                }
            }

            return null;
        }

        internal static void BuildServer(BuildTarget target, string locationPathName, bool exitApplicationOnFailure = false)
        {
            Debug.Log($"Building {target} server in {locationPathName}");
            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, BuildTarget.StandaloneLinux64);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = locationPathName,
                target = target,
                subtarget = (int)StandaloneBuildSubtarget.Server,
            });
            if (exitApplicationOnFailure && report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        internal static void BuildClient(BuildTarget target, string locationPathName, bool exitApplicationOnFailure = false)
        {
            Debug.Log($"Building {target} client in {locationPathName}");
            
            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Standalone, target);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = locationPathName,
                target = target,
                subtarget = (int)StandaloneBuildSubtarget.Player,
            });
            if (exitApplicationOnFailure && report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        static string[] GetScenePaths()
        {
            var scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = EditorBuildSettings.scenes[i].path;
            }

            return scenes;
        }
    }
}
