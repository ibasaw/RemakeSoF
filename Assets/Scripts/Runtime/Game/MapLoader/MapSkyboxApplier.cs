using System.Globalization;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tolik.RemakeSoF.Runtime.Management.MapManagement
{
    /// <summary>
    /// Erstellt zur Laufzeit eine Unity Skybox aus SoF2 skyParms-Daten (sky_types_json_N)
    /// und ein Directional Light aus sun_N / surfacelight_N Properties.
    /// </summary>
    public class MapSkyboxApplier
    {
        /// <summary>
        /// Prefix für sky_types_json_N Properties in Ghoul2Meta.
        /// </summary>
        private const string k_SkyTypesJsonPrefix = "sky_types_json_";

        /// <summary>
        /// Prefix für sun_N Properties in Ghoul2Meta.
        /// Format: "r g b intensity degrees elevation"
        /// </summary>
        private const string k_SunPrefix = "sun_";

        /// <summary>
        /// Prefix für surfacelight_N Properties in Ghoul2Meta.
        /// </summary>
        private const string k_SurfaceLightPrefix = "surfacelight_";

        /// <summary>
        /// Maximale Anzahl durchsuchter Slots.
        /// </summary>
        private const int k_MaxSlots = 32;

        /// <summary>
        /// Shader-Name für die Unity 6-Sided Skybox.
        /// </summary>
        private const string k_SkyboxShaderName = "Skybox/6 Sided";

        /// <summary>
        /// Skalierungsfaktor fuer die Umrechnung von idTech3 sun intensity zu Unity Light intensity.
        /// idTech3 nutzt Werte wie 300, Unity URP Directional typisch 0.5-3.0.
        /// 300 * 0.01 = 3.0 → natuerliche Aussenbeleuchtung.
        /// 63 * 0.01 = 0.63 → schwaches Mondlicht.
        /// </summary>
        private const float k_IntensityScale = 0.01f;

        /// <summary>
        /// Suffixe für die 6 Skybox-Faces (idTech3/SoF2-Konvention).
        /// </summary>
        private static readonly string[] s_FaceSuffixes = { "_ft", "_bk", "_up", "_dn", "_rt", "_lf" };

        /// <summary>
        /// Zugehörige Shader-Property-Namen im Skybox/6 Sided Shader.
        /// Reihenfolge muss mit s_FaceSuffixes übereinstimmen.
        /// </summary>
        private static readonly string[] s_ShaderProperties = { "_FrontTex", "_BackTex", "_UpTex", "_DownTex", "_RightTex", "_LeftTex" };

        /// <summary>
        /// Das zur Laufzeit erstellte Directional Light für die Sonne.
        /// </summary>
        private GameObject m_SunLightObject;

        /// <summary>
        /// Sucht in der Map-Instanz nach einem Renderer mit sky_types_json_N,
        /// erstellt die Skybox und optional ein Directional Light aus sun_N.
        /// </summary>
        /// <param name="mapInstance">Die instanziierte Map.</param>
        public void ApplySkybox(GameObject mapInstance)
        {
            if (mapInstance == null)
            {
                return;
            }

            Ghoul2Meta skyMeta = FindSkyMeta(mapInstance);
            if (skyMeta == null)
            {
                Debug.Log("[MapSkyboxApplier] No sky_types_json property found. Skipping skybox creation.");
                return;
            }

            string skyParms = GetSkyParms(skyMeta);
            if (string.IsNullOrEmpty(skyParms))
            {
                Debug.LogWarning("[MapSkyboxApplier] sky_types_json found but value is empty.");
                return;
            }

            string basePath = ParseBasePath(skyParms);
            if (string.IsNullOrEmpty(basePath))
            {
                Debug.LogWarning($"[MapSkyboxApplier] Could not parse skyParms base path from: {skyParms}");
                return;
            }

            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                Debug.LogError("[MapSkyboxApplier] TextureManager not available via ServiceLocator.");
                return;
            }

            CreateSkybox(basePath, textureManager);
            CreateSunLight(skyMeta);
            ApplyAmbientLighting(skyMeta);
        }

        #region Sky Meta Discovery

        /// <summary>
        /// Durchsucht alle Ghoul2Meta in der Map nach dem ersten mit sky_types_json_N.
        /// Gibt die gesamte Ghoul2Meta-Referenz zurück, damit auch sun_/surfacelight_ gelesen werden können.
        /// </summary>
        private Ghoul2Meta FindSkyMeta(GameObject mapInstance)
        {
            Ghoul2Meta[] allMeta = mapInstance.GetComponentsInChildren<Ghoul2Meta>(true);

            foreach (Ghoul2Meta meta in allMeta)
            {
                for (int i = 0; i < k_MaxSlots; i++)
                {
                    string propertyName = k_SkyTypesJsonPrefix + i;
                    if (!meta.HasProperty(propertyName))
                    {
                        continue;
                    }

                    string value = meta.GetString(propertyName);
                    if (!string.IsNullOrEmpty(value))
                    {
                        Debug.Log($"[MapSkyboxApplier] Found skyParms on '{meta.gameObject.name}': {value}");
                        return meta;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Liest den sky_types_json_N Wert aus einer bereits gefundenen Ghoul2Meta.
        /// </summary>
        private string GetSkyParms(Ghoul2Meta meta)
        {
            for (int i = 0; i < k_MaxSlots; i++)
            {
                string propertyName = k_SkyTypesJsonPrefix + i;
                if (!meta.HasProperty(propertyName))
                {
                    continue;
                }

                string value = meta.GetString(propertyName);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return null;
        }

        #endregion

        #region Skybox Creation

        /// <summary>
        /// Parst den Basispfad aus dem skyParms-Wert.
        /// Eingabe z.B.: '["textures/skies/cemetary 0 -"]' → "textures/skies/cemetary"
        /// </summary>
        private string ParseBasePath(string skyParmsRaw)
        {
            string cleaned = skyParmsRaw.Trim();

            // JSON-Array-Wrapper entfernen: ["..."] → ...
            if (cleaned.StartsWith("["))
            {
                cleaned = cleaned.TrimStart('[').TrimEnd(']');
            }

            // Anführungszeichen entfernen
            cleaned = cleaned.Trim('"', '\'', ' ');

            // skyParms Format: "<basepath> <cloudheight> <innerbox>"
            // Wir brauchen nur den ersten Teil (Basispfad).
            string[] parts = cleaned.Split(' ');
            if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
            {
                return null;
            }

            return parts[0];
        }

        /// <summary>
        /// Lädt die 6 Face-Texturen und erstellt das Skybox/6 Sided Material.
        /// _up und _dn werden um 90° gegen den Uhrzeigersinn rotiert (idTech3 Z-up → Unity Y-up Koordinatensystem-Korrektur).
        /// </summary>
        private void CreateSkybox(string basePath, TextureManager textureManager)
        {
            Shader skyboxShader = Shader.Find(k_SkyboxShaderName);
            if (skyboxShader == null)
            {
                Debug.LogError($"[MapSkyboxApplier] Shader '{k_SkyboxShaderName}' not found. Is it included in project settings?");
                return;
            }

            Material skyboxMaterial = new(skyboxShader) { name = $"Skybox_{basePath}" };
            int loadedFaces = 0;

            for (int i = 0; i < s_FaceSuffixes.Length; i++)
            {
                string textureKey = basePath + s_FaceSuffixes[i];
                Texture2D faceTexture = LoadFaceTexture(textureKey, textureManager);

                if (faceTexture == null)
                {
                    Debug.LogWarning($"[MapSkyboxApplier] Missing skybox face texture: {textureKey}");
                    continue;
                }

                // _up und _dn Faces um 90° CCW rotieren (idTech3 Z-up → Unity Y-up Koordinatensystem)
                if (s_FaceSuffixes[i] == "_up" || s_FaceSuffixes[i] == "_dn")
                {
                    faceTexture = RotateTexture90CCW(faceTexture);
                }

                skyboxMaterial.SetTexture(s_ShaderProperties[i], faceTexture);
                loadedFaces++;
            }

            if (loadedFaces == 0)
            {
                Debug.LogError($"[MapSkyboxApplier] No skybox face textures found for base path: {basePath}");
                Object.Destroy(skyboxMaterial);
                return;
            }

            RenderSettings.skybox = skyboxMaterial;
            DynamicGI.UpdateEnvironment();
            Debug.Log($"[MapSkyboxApplier] Skybox created from '{basePath}' with {loadedFaces}/6 faces.");
        }

        /// <summary>
        /// Lädt eine einzelne Face-Textur über den TextureManager.
        /// Versucht zuerst den direkten Key, dann den Alias.
        /// </summary>
        private Texture2D LoadFaceTexture(string textureKey, TextureManager textureManager)
        {
            TextureData textureData = textureManager.GetTextureData(textureKey);

            if (textureData == null)
            {
                textureData = textureManager.GetTextureDataByAlias(textureKey);
            }

            if (textureData != null && textureData.IsValid() && textureData.HasTexture())
            {
                return textureData.Texture;
            }

            return null;
        }

        /// <summary>
        /// Rotiert eine Texture2D um 90° gegen den Uhrzeigersinn (CCW).
        /// Wird für _up/_dn benötigt wegen idTech3 (Z-up) → Unity (Y-up) Konvertierung.
        /// </summary>
        private Texture2D RotateTexture90CCW(Texture2D original)
        {
            int width = original.width;
            int height = original.height;
            Texture2D rotated = new(height, width, original.format, false)
            {
                name = original.name + "_rot90ccw"
            };

            Color32[] srcPixels = original.GetPixels32();
            Color32[] dstPixels = new Color32[srcPixels.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 90° CCW: (x, y) → (y, width-1-x) mit getauschten Dimensionen
                    dstPixels[(width - 1 - x) * height + y] = srcPixels[y * width + x];
                }
            }

            rotated.SetPixels32(dstPixels);
            rotated.Apply();

            return rotated;
        }

        #endregion

        #region Sun / Directional Light

        /// <summary>
        /// Erstellt ein Directional Light aus sun_N und surfacelight_N Properties.
        /// idTech3 sun Format: "r g b intensity degrees elevation"
        /// - r/g/b: Farbkomponenten (0-1)
        /// - intensity: Lichtintensität (idTech3-Skala, z.B. 300)
        /// - degrees: Kompassrichtung der Sonne (0-360°, Gegenuhrzeigersinn)
        /// - elevation: Höhenwinkel über dem Horizont (0-90°)
        /// </summary>
        private void CreateSunLight(Ghoul2Meta meta)
        {
            string sunRaw = GetFirstPropertyValue(meta, k_SunPrefix);
            if (string.IsNullOrEmpty(sunRaw))
            {
                Debug.Log("[MapSkyboxApplier] No sun_ property found. Skipping directional light.");
                return;
            }

            if (!TryParseSunParameters(sunRaw, out Color sunColor, out float intensity, out float degrees, out float elevation))
            {
                Debug.LogWarning($"[MapSkyboxApplier] Could not parse sun parameters: {sunRaw}");
                return;
            }

            // Altes Sun-Light entfernen falls vorhanden
            if (m_SunLightObject != null)
            {
                RenderSettings.sun = null;
                Object.Destroy(m_SunLightObject);
            }
            m_SunLightObject = new("SoF2_SunLight") { hideFlags = HideFlags.DontSave };
            Light sunLight = m_SunLightObject.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = sunColor;
            sunLight.intensity = intensity * k_IntensityScale;
            sunLight.shadows = LightShadows.Soft;

            // Explizit als Main Light fuer URP registrieren.
            // Ohne das erkennt URP das dynamisch erstellte Light nicht zuverlaessig
            // als Hauptlicht fuer den Shadow-Atlas (besonders nach Runtime-Aenderungen).
            RenderSettings.sun = sunLight;

            // idTech3: degrees = Kompasswinkel (Gegenuhrzeigersinn von Osten, 0°=Ost)
            // Unity: Y-Rotation = Uhrzeigersinn von +Z (0°=Nord)
            // Offset -90° weil idTech3 0°=Ost(+X) vs Unity 0°=Nord(+Z)
            // Elevation: direkt als X-Rotation (positiv = nach unten gerichtet)
            m_SunLightObject.transform.rotation = Quaternion.Euler(elevation, -degrees - 90f, 0f);

            Debug.Log($"[MapSkyboxApplier] Sun light created: color={sunColor}, " +
                      $"intensity={sunLight.intensity:F2}, degrees={degrees}, elevation={elevation}, " +
                      $"rotation={m_SunLightObject.transform.rotation.eulerAngles}");
        }

        /// <summary>
        /// Setzt Unity Ambient-Beleuchtung basierend auf surfacelight_N und sun_N Werten.
        /// surfacelight war in q3map2 die Lichtstaerke der Himmelsoberflaeche fuer gebackene Lightmaps.
        /// Hier steuert es die Ambient-Intensitaet: niedrig (10) = dunkle Nacht, hoch (200+) = heller Tag.
        /// Das Ambient wird auf Flat-Modus gesetzt damit die Skybox-Textur nicht die Beleuchtung uebersteuert.
        /// Der SoF2/MapSurface Shader liest dieses Ambient ueber SampleSH() fuer minimale Fuellung dunkler Bereiche.
        /// </summary>
        private void ApplyAmbientLighting(Ghoul2Meta meta)
        {
            // Sun-Farbe als Ambient-Tonus nutzen (Fallback: weiss)
            Color ambientTint = Color.white;
            string sunRaw = GetFirstPropertyValue(meta, k_SunPrefix);
            if (!string.IsNullOrEmpty(sunRaw) &&
                TryParseSunParameters(sunRaw, out Color sunColor, out float _, out float _, out float _))
            {
                ambientTint = sunColor;
            }

            // surfacelight bestimmt die Ambient-Staerke
            float surfaceLight = 0f;
            string surfaceLightRaw = GetFirstPropertyValue(meta, k_SurfaceLightPrefix);
            if (!string.IsNullOrEmpty(surfaceLightRaw))
            {
                string cleaned = surfaceLightRaw.Trim();
                if (cleaned.StartsWith("["))
                {
                    cleaned = cleaned.TrimStart('[').TrimEnd(']');
                }
                cleaned = cleaned.Trim('"', '\'', ' ');
                float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out surfaceLight);
            }

            // surfacelight zu Ambient-Faktor: 10 (Nacht) → 0.05, 70 (Indoor) → 0.35, 200 (Tag) → 1.0
            float ambientFactor = Mathf.Clamp01(surfaceLight / 200f);

            // Flat-Modus: kontrollierte Ambient-Farbe statt automatischer Skybox-Berechnung
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(
                ambientTint.r * ambientFactor * 0.5f,
                ambientTint.g * ambientFactor * 0.5f,
                ambientTint.b * ambientFactor * 0.5f,
                1f
            );
            RenderSettings.ambientIntensity = 1f;

            Debug.Log($"[MapSkyboxApplier] Ambient set: surfacelight={surfaceLight}, " +
                      $"factor={ambientFactor:F3}, color={RenderSettings.ambientLight}");
        }

        /// <summary>
        /// Parst die sun-Parameter aus dem Rohwert.
        /// Eingabe z.B.: '["0.8 0.8 0.8 300 0 50"]' → r=0.8, g=0.8, b=0.8, intensity=300, degrees=0, elevation=50
        /// </summary>
        private bool TryParseSunParameters(string sunRaw, out Color color, out float intensity,
            out float degrees, out float elevation)
        {
            color = Color.white;
            intensity = 1f;
            degrees = 0f;
            elevation = 45f;

            string cleaned = sunRaw.Trim();

            // JSON-Array-Wrapper entfernen
            if (cleaned.StartsWith("["))
            {
                cleaned = cleaned.TrimStart('[').TrimEnd(']');
            }

            cleaned = cleaned.Trim('"', '\'', ' ');

            string[] parts = cleaned.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
            {
                Debug.LogWarning($"[MapSkyboxApplier] Sun parameter needs 6 values (r g b intensity degrees elevation), got {parts.Length}");
                return false;
            }

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) ||
                !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out intensity) ||
                !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out degrees) ||
                !float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out elevation))
            {
                return false;
            }

            color = new Color(r, g, b, 1f);
            return true;
        }

        /// <summary>
        /// Liest den ersten gefundenen Wert für ein Property-Prefix (z.B. sun_0, sun_1, ...).
        /// Entfernt JSON-Array-Wrapper.
        /// </summary>
        private string GetFirstPropertyValue(Ghoul2Meta meta, string prefix)
        {
            for (int i = 0; i < k_MaxSlots; i++)
            {
                string propertyName = prefix + i;
                if (!meta.HasProperty(propertyName))
                {
                    continue;
                }

                string value = meta.GetString(propertyName);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return null;
        }

        #endregion
    }
}
