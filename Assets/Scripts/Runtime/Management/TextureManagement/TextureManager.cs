using System;
using System.Collections.Generic;
using System.IO;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Manager für automatisches Laden von Texturen aus Verzeichnissen.
    /// Verwaltet eine interne TextureRegistry und lädt Texturen automatisch beim Start.
    /// </summary>
    public class TextureManager
    {
        private readonly string m_CustomDirectory = "CustomTextures";
        private readonly string[] m_SupportedExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".tif", ".tiff" };
        private readonly string m_ShaderRenderName = "SoF2/MapSurface";
        private readonly TextureRegistry m_Registry;
        private readonly TextureConfiguration m_Configuration;
        public TextureConfiguration Configuration => m_Configuration;

        public TextureManager()
        {
            m_Registry = new TextureRegistry();
            m_Configuration = new TextureConfiguration();
            Initialize();
            Debug.Log($"[TextureManager] Initialized. Found {m_Registry.TextureCache.Count} textures.");
        }

        #region Initialization

        /// <summary>
        /// Initialisiert den TextureManager: lädt Konfiguration und scannt Verzeichnisse.
        /// </summary>
        private void Initialize()
        {
            if (m_Registry == null)
            {
                Debug.LogError("[TextureManager] TextureRegistry is null!");
                return;
            }
            if (m_Configuration == null)
            {
                Debug.LogError("[TextureManager] TextureConfiguration is null!");
                return;
            }

            // Alle relevanten Verzeichnisse scannen (System zuerst, dann Custom)
            ScanAllTextureDirectories();
            m_Configuration.Initialize();
        }

        /// <summary>
        /// Gibt den vollständigen Pfad für Custom-Texturen zurück. 
        /// Sie überschreiben System-Texturen. Liegen im AppDataPath(system).
        /// </summary>
        private string GetCustomTexturesFullPath()
        {
            return Path.Combine(Application.persistentDataPath, m_CustomDirectory);
        }

        /// <summary>
        /// Gibt den vollständigen Pfad für System-Texturen zurück.
        /// System-Texturen liegen im Art/Textures Verzeichnis innerhalb des Unity-Projekts.
        /// </summary>
        private string GetSystemTexturesFullPath()
        {
            return Path.Combine(Application.dataPath, "Art", "Textures");
        }

        /// <summary>
        /// Scannt System- und Custom-Textur-Verzeichnisse.
        /// System wird zuerst geladen, Custom kann Keys überschreiben.
        /// </summary>
        private void ScanAllTextureDirectories()
        {
            m_Registry.ClearCache();

            string systemPath = GetSystemTexturesFullPath();
            if (Directory.Exists(systemPath))
            {
                ScanDirectory(systemPath, TextureSource.System);
            }
            else
            {
                Debug.LogWarning($"[TextureManager] System texture directory missing: {systemPath}");
            }

            string customPath = GetCustomTexturesFullPath();
            if (!Directory.Exists(customPath))
            {
                Debug.LogWarning($"[TextureManager] Custom texture directory does not exist: {customPath}");
                Debug.Log($"[TextureManager] Creating directory: {customPath}");
                Directory.CreateDirectory(customPath);
            }

            ScanDirectory(customPath, TextureSource.Custom);
        }

        #endregion

        #region Scanning

        /// <summary>
        /// Scannt das Verzeichnis nach Textur-Dateien.
        /// </summary>
        private void ScanDirectory(string directoryPath, TextureSource source)
        {
            SearchOption searchOption = SearchOption.AllDirectories;

            List<string> allFiles = new();

            // Alle unterstützten Dateien sammeln
            foreach (var extension in m_SupportedExtensions)
            {
                try
                {
                    var files = Directory.GetFiles(directoryPath, "*" + extension, searchOption);
                    allFiles.AddRange(files);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TextureManager] Error scanning for {extension} files: {ex.Message}");
                }
            }

            Debug.Log($"[TextureManager] Found {allFiles.Count} texture files in {directoryPath}");

            foreach (string filePath in allFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string relativePath = GetRelativePath(directoryPath, filePath);

                // Key generieren (ohne Extension, mit relativem Pfad)
                string key = Path.Combine(Path.GetDirectoryName(relativePath) ?? "", fileName)
                    .Replace('\\', '/');

                // Vorregistrieren in der Registry ohne geladene Texture und Material (werden on demand geladen)
                TextureData data = TextureDataFactory.Create(key, filePath, null, null, source);
                m_Registry.RegisterTextureData(data);
            }
        }

        public void CreateMaterialsFromSkinDefinition(SkinDefinition skinDefinition)
        {

            foreach (var mdef in skinDefinition.materials)
            {
                string partName = mdef.name ?? "unnamed_part";
                foreach (var g in mdef.groups)
                {
                    //TODO hier evtl. noch erweitern für mehrfache Texturen pro Material (texture1, texture2, ...)
                    //Derzeit nur texture1 oder shader1 Key genutzt
                    string cacheKey = g.texture1 == null || g.texture1.Length == 0 ? g.shader1 : g.texture1;
                    if (m_Registry.TextureCache.ContainsKey(cacheKey))
                    //TODO: das könnte bullshit logik sein im else, müsste evtl cache überschreiben! MUSS ICH TESTEN!
                    { //try material from g2skin definition
                        TextureData textureData = GetTextureData(cacheKey);
                        if (textureData.IsValid() && textureData.HasTexture())
                        {
                            // Shader & Material erzeugen
                            Shader shader = Shader.Find(m_ShaderRenderName);
                            Material material = new(shader) { name = $"{partName}_{cacheKey}" };

                            SetupMaterialProperties(material, textureData.Texture, partName.IndexOf("2sided",
                                StringComparison.OrdinalIgnoreCase) >= 0);
                            // In Cache speichern
                            m_Registry.UpdateTextureData(cacheKey, material);
                        }
                    }
                    else
                    { // try material from legacy shader definition
                        ShaderEntry foundEntry = null;
                        LegacyShaderLoader shaderLoader = ServiceLocator.Get<LegacyShaderLoader>();
                        Dictionary<string, Dictionary<string, ShaderEntry>> allShaderDefinitions = shaderLoader.GetAll();
                        // Durch alle Model-Shader-Dictionaries iterieren
                        if (allShaderDefinitions != null)
                        {
                            foreach (var modelShaders in allShaderDefinitions.Values)
                            {
                                if (modelShaders.TryGetValue(cacheKey, out ShaderEntry entry))
                                {
                                    foundEntry = entry;
                                    break;
                                }
                            }
                        }

                        if (foundEntry != null)
                        {
                            CreateMaterialFromShaderEntry(cacheKey, foundEntry);
                        }
                        else
                        {
                            Debug.LogWarning($"[TextureManager] No legacy ShaderEntry found for key: {cacheKey}");
                        }
                    }
                }
            }
        }

        internal void CreateMaterialFromShaderEntry(string shaderName, ShaderEntry entry)
        {
            string partName = Path.GetFileNameWithoutExtension(shaderName);
            bool isTwoSided = entry.CullDisabled;
            string texturePath = !string.IsNullOrEmpty(entry.MainTexture) ? entry.MainTexture : entry.EditorImage;

            // Shader & Material erzeugen
            Shader shader = Shader.Find(m_ShaderRenderName);
            Material material = new(shader) { name = partName };

            // Textur setzen
            if (!string.IsNullOrEmpty(texturePath))
            {
                TextureData textureData = GetTextureData(texturePath);
                if (textureData != null && textureData.IsValid() && textureData.HasTexture())
                {
                    SetupMaterialProperties(material, textureData.Texture, isTwoSided);
                    // In Cache speichern
                    if (m_Registry.TextureCache.ContainsKey(shaderName))
                    {
                        m_Registry.UpdateTextureData(shaderName, material);
                    }
                    else
                    {
                        m_Registry.UpdateTextureWithAlias(texturePath, shaderName, material);
                    }
                }
                else
                {
                    Debug.LogWarning($"[TextureManager] TextureData invalid or missing texture for key: {texturePath}");
                }
            }
        }

        /// <summary>
        /// Setzt Standard-Materialeigenschaften (Textur, Smoothness, TwoSided) für URP-Materialien.
        /// </summary>
        private void SetupMaterialProperties(Material material, Texture texture, bool isTwoSided)
        {
            if (material == null)
                return;

            // Textur setzen
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            else
                material.mainTexture = texture;

            // Smoothness -> 0.0
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.0f);

            // SoF2/MapSurface: Player-Models reagieren staerker auf Licht als Map-Surfaces
            if (material.HasProperty("_LightBlend"))
                material.SetFloat("_LightBlend", 0.4f);

            // Zwei-seitig rendern
            if (isTwoSided)
                SetTwoSidedURP(material, true);
        }

        private void SetTwoSidedURP(Material mat, bool twoSided)
        {
            if (mat == null) return;
            if (mat.HasProperty("_CullMode"))
                mat.SetInt("_CullMode", twoSided ? 0 : 2);

            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0.0f); // 0 = Both

            // Optional auch Keyword aktivieren, falls Shader es nutzt
            mat.EnableKeyword("_DOUBLESIDED_ON");
        }

        /// <summary>
        /// Berechnet den relativen Pfad von einer Datei zum Basis-Verzeichnis.
        /// </summary>
        private string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new(basePath + Path.DirectorySeparatorChar);
            Uri fullUri = new(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString());
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gibt TextureData nach Key zurück.
        /// </summary>
        public TextureData GetTextureData(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            return m_Registry?.GetTextureData(key);
        }

        /// <summary>
        /// Gibt TextureData nach Alias-Key zurück.
        /// Nutzen Sie dies, wenn für einen Key nicht direkt eine Textur gefunden wurde.
        /// </summary>
        public TextureData GetTextureDataByAlias(string aliasKey)
        {
            if (string.IsNullOrEmpty(aliasKey))
                return null;

            return m_Registry?.GetTextureDataByAlias(aliasKey);
        }

        /// <summary>
        /// Lädt das Verzeichnis neu.
        /// </summary>
        /*public void Reload()
        {
            m_Registry?.ClearCache();
            Initialize();
        }*/

        /// <summary>
        /// Löscht den Cache und optional auch die Texturen aus dem Speicher.
        /// </summary>
        public void ClearCache()
        {
            m_Registry?.ClearCache();
        }

        #endregion

    }
}
