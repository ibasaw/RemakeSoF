using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    /// <summary>
    /// Manager für automatisches Laden von Texturen aus Verzeichnissen.
    /// Verwaltet eine interne TextureRegistry und lädt Texturen automatisch beim Start.
    /// </summary>
    public class TextureManager : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Basis-Verzeichnis für Custom-Texturen (relativ zu Application.persistentDataPath)")]
        [SerializeField] private string m_CustomDirectory = "CustomTextures";

        [Tooltip("Lazy Loading (nur bei Bedarf) oder Eager Loading (alles sofort)")]

        [Header("Supported Formats")]
        [SerializeField] private string[] m_SupportedExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".tif", ".tiff" };

        private readonly string m_ShaderRenderName = "Universal Render Pipeline/Unlit";

        // State
        private TextureRegistry m_Registry;

        public TextureConfiguration Configuration => m_Configuration;
        private TextureConfiguration m_Configuration;

        private bool m_IsInitialized = false;

        public bool IsInitialized => m_IsInitialized;

        //TODO 
        private Dictionary<string, Dictionary<string, List<Material>>> m_MaterialsByFile { get; } =
            new Dictionary<string, Dictionary<string, List<Material>>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Dictionary<string, List<Material>>> MaterialsByFile => m_MaterialsByFile;

        // Caches für Performance
        private Dictionary<string, Material> materialCache = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Material> MaterialCache => materialCache;

        #region Lifecycle

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Initialize();
            Debug.Log($"[TextureManager] Initialized. Found {m_Registry.TextureCache.Count} textures.");
        }

        void OnDestroy()
        {
            m_Registry?.ClearCache(true);
            m_Registry = null;
            m_Configuration = null;
            Debug.Log("[TextureManager] Destroyed");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialisiert den TextureManager und scannt das konfigurierte Verzeichnis.
        /// </summary>
        private void Initialize()
        {
            if (m_IsInitialized)
            {
                Debug.LogWarning("[TextureManager] Already initialized");
                return;
            }

            m_Registry = new TextureRegistry();
            m_Configuration = new TextureConfiguration();
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
            m_IsInitialized = true;
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
            return Path.Combine(Application.dataPath, "Art/Textures");
        }

        /// <summary>
        /// Scannt System- und Custom-Textur-Verzeichnisse.
        /// System wird zuerst geladen, Custom kann Keys überschreiben.
        /// </summary>
        private void ScanAllTextureDirectories()
        {
            m_Registry.ClearCache(true);

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

        public void CreateMaterialsFromSkinDefinition(string selectedSkinName, SkinDefinition skinDefinition)
        {
            var map = new Dictionary<string, List<Material>>(StringComparer.OrdinalIgnoreCase);

            foreach (var mdef in skinDefinition.materials)
            {
                string partName = mdef.name ?? "unnamed_part";

                if (!map.TryGetValue(partName, out List<Material> list))
                {
                    list = new List<Material>();
                    map[partName] = list;
                }

                foreach (var g in mdef.groups)
                {
                    string cacheKey = g.texture1 == null || g.texture1.Length == 0 ? g.shader1 : g.texture1;
                    if (MaterialCache.TryGetValue(cacheKey, out Material cached))
                    {
                        list.Add(cached);
                        continue;
                    }
                    else
                    {
                        /*definitions.TryGetValue("default", out var defaultDefinitionVariant);
                        definitions.TryGetValue(selectedModelName, out var modelSpecificVariant);

                        PartDef foundPartDef = null;
                        if (defaultDefinitionVariant != null)
                        {
                            defaultDefinitionVariant.parts.TryGetValue(partName, out foundPartDef);
                        }
                        if (modelSpecificVariant != null && foundPartDef == null)
                        {
                            modelSpecificVariant.parts.TryGetValue(partName, out foundPartDef);
                        }*/

                        //analog ähnlich in CreateMaterialFromShaderEntry()
                        Shader shader = Shader.Find(m_ShaderRenderName);
                        var material = new Material(shader) { name = $"{partName}_{cacheKey}" };
                        // Load texture
                        var texture = GetTexture(cacheKey);
                        if (texture != null)
                        {
                            if (material.HasProperty("_BaseMap"))
                                material.SetTexture("_BaseMap", texture);
                            else
                                material.mainTexture = texture;
                        }
                        // Smoothness -> 0.0
                        if (material.HasProperty("_Smoothness"))
                            material.SetFloat("_Smoothness", 0.0f);

                        if (partName.IndexOf("2sided", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            SetTwoSidedURP(material, true);
                        }
                        MaterialCache[cacheKey] = material;
                        list.Add(material);
                    }
                }
            }

            MaterialsByFile[selectedSkinName] = map;
            Debug.Log($"[PlayerSkinManager] Zugewiesene Materialien für '{selectedSkinName}': {map.Count} parts. Keys: {string.Join(", ", map.Keys)}");
        }

        public void CreateMaterialFromShaderEntry(string shaderName, ShaderEntry entry)
        {
            string partName = Path.GetFileNameWithoutExtension(shaderName);
            bool isTwoSided = entry.CullDisabled;
            string texturePath = !string.IsNullOrEmpty(entry.MainTexture) ? entry.MainTexture : entry.EditorImage;

            // Shader & Material erzeugen
            Shader shader = Shader.Find(m_ShaderRenderName);
            var material = new Material(shader) { name = partName };

            // Textur setzen
            if (!string.IsNullOrEmpty(texturePath))
            {
                Texture texture = GetTexture(texturePath);
                if (texture != null)
                {
                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", texture);
                    else
                        material.mainTexture = texture;
                }
            }

            // Smoothness -> 0.0
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.0f);

            // Zwei-seitig rendern
            if (isTwoSided)
                SetTwoSidedURP(material, true);

            // In Cache speichern
            MaterialCache[shaderName] = material;
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
        /// Gibt nur die Texture2D zurück (Legacy).
        /// </summary>
        public Texture2D GetTexture(string key)
        {
            return GetTextureData(key)?.Texture;
        }

        /// <summary>
        /// Lädt das Verzeichnis neu.
        /// </summary>
        /*public void Reload()
        {
            m_IsInitialized = false;
            m_Registry?.ClearCache(true);
            Initialize();
        }*/

        /// <summary>
        /// Löscht den Cache und optional auch die Texturen aus dem Speicher.
        /// </summary>
        /*public void ClearCache(bool destroy = false)
        {
            m_Registry?.ClearCache(destroy);
        }*/

        #endregion
        
    }
}
