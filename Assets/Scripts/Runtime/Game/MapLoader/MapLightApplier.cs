using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Management.MapManagement
{
    /// <summary>
    /// Erstellt zur Laufzeit Unity-Lichter aus SoF2/idTech3 light-Entities.
    /// Liest classname="light" Ghoul2Meta-Properties (_color, light, scale, light_type, target)
    /// und erstellt Point- oder Spot-Lights. Spot-Lights werden auf ihr Target (classname="info_notnull")
    /// ausgerichtet.
    /// </summary>
    public class MapLightApplier
    {
        /// <summary>
        /// idTech3 light-Einheiten zu Unity-Range Skalierung.
        /// 1 Quake-Unit = 1 Inch = 0.0254 Meter (Projekt-Standard SOF2_UNIT_SCALE).
        /// </summary>
        private const float k_LightRangeScale = 0.0254f;

        /// <summary>
        /// idTech3 Default-Lichtradius (300 Units). Wird als Referenz fuer die
        /// Intensitaets-Berechnung verwendet.
        /// </summary>
        private const float k_IdTech3DefaultLight = 300f;

        /// <summary>
        /// Basis-Intensitaet fuer ein Default-Light (light=300) in URP.
        /// Kalibriert fuer den SoF2/MapSurface Shader mit _LightBlend=0.25:
        /// Die Texturen enthalten bereits gebackenes Licht aus q3map2.
        /// Realtime-Licht ist nur ein subtiler Zusatz fuer Dynamik/Schatten.
        /// light=300 → 2.0 Intensity: Bei ~1m → tex*1.25, bei ~2m → tex*0.88, bei ~4m → tex*0.78
        /// </summary>
        private const float k_BaseIntensity = 5f;

        /// <summary>
        /// Range-Multiplikator fuer idTech3→Unity Falloff-Kompensation.
        /// idTech3 nutzt linearen Falloff (50% Helligkeit bei 50% Reichweite),
        /// Unity URP nutzt 1/d² (50% bei nur ~29% Reichweite).
        /// 1.5x vergroessert den sichtbaren Lichtradius, damit Lichter nicht
        /// zu frueh abklingen und die Raumbeleuchtung natuerlicher wirkt.
        /// </summary>
        private const float k_RangeMultiplier = 1.5f;

        /// <summary>
        /// Maximale Unity-Range fuer ein einzelnes Licht in Metern.
        /// Verhindert dass extrem grosse idTech3-Werte (z.B. 8000)
        /// riesige Lichtradien erzeugen die Performance und Optik beeintraechtigen.
        /// </summary>
        private const float k_MaxRange = 50f;

        /// <summary>
        /// Standard Spot-Winkel in Grad.
        /// </summary>
        private const float k_DefaultSpotAngle = 60f;

        /// <summary>
        /// Minimaler idTech3-Light-Wert. Lichter mit kleinerem Wert werden uebersprungen.
        /// URP cullt automatisch per Frame die sichtbarsten ~256 Lichter,
        /// daher muessen wir kein hartes Limit fuer die Erstellung setzen.
        /// Aber sehr schwache Lichter (unter 150) haetten mit k_BaseIntensity=2
        /// kaum sichtbaren Effekt und verschwenden nur Culling-Budget.
        /// </summary>
        private const float k_MinLightValue = 150f;

        /// <summary>
        /// Liste der zur Laufzeit erstellten Light-GameObjects für Cleanup.
        /// </summary>
        private readonly List<GameObject> m_CreatedLights = new();

        /// <summary>
        /// Versteckt die Renderer aller light- und info_notnull-Entities,
        /// ohne Unity-Lights zu erstellen. Wird verwendet wenn Map-Materialien
        /// den Unlit-Shader nutzen (Lichter haetten keinen sichtbaren Effekt).
        /// </summary>
        /// <param name="mapInstance">Die instanziierte Map.</param>
        public void HideEntityRenderers(GameObject mapInstance)
        {
            if (mapInstance == null)
            {
                return;
            }

            Ghoul2Meta[] allMeta = mapInstance.GetComponentsInChildren<Ghoul2Meta>(true);
            int hiddenCount = 0;

            foreach (Ghoul2Meta meta in allMeta)
            {
                string classname = meta.GetString("classname");
                if (classname != "light" && classname != "info_notnull")
                {
                    continue;
                }

                if (meta.TryGetComponent(out Renderer renderer))
                {
                    renderer.enabled = false;
                    hiddenCount++;
                }
            }

            Debug.Log($"[MapLightApplier] {hiddenCount} Entity-Renderer versteckt (light + info_notnull).");
        }

        /// <summary>
        /// Sucht in der Map-Instanz nach allen Renderern mit classname="light",
        /// erstellt Unity-Lichter und richtet Spot-Lights auf ihre Targets aus.
        /// Nur sinnvoll wenn Map-Materialien einen Lit-Shader verwenden.
        /// </summary>
        /// <param name="mapInstance">Die instanziierte Map.</param>
        public void ApplyLights(GameObject mapInstance)
        {
            if (mapInstance == null)
            {
                return;
            }

            ClearLights();

            Ghoul2Meta[] allMeta = mapInstance.GetComponentsInChildren<Ghoul2Meta>(true);

            // Erst alle Targets (info_notnull mit targetname) sammeln
            Dictionary<string, Transform> targetLookup = BuildTargetLookup(allMeta);

            int createdCount = 0;
            int skippedWeak = 0;

            foreach (Ghoul2Meta meta in allMeta)
            {
                string classname = meta.GetString("classname");
                if (classname != "light")
                {
                    continue;
                }

                // Renderer deaktivieren – Light-Entities sind unsichtbar
                if (meta.TryGetComponent(out Renderer renderer))
                {
                    renderer.enabled = false;
                }

                // Schwache Lichter ueberspringen — haetten kaum sichtbaren Effekt
                float lightValue = meta.GetFloat("light", k_IdTech3DefaultLight);
                if (lightValue < k_MinLightValue)
                {
                    skippedWeak++;
                    continue;
                }

                GameObject lightGO = CreateLight(meta, targetLookup);
                if (lightGO != null)
                {
                    m_CreatedLights.Add(lightGO);
                    createdCount++;
                }
            }

            // info_notnull Renderer ebenfalls unsichtbar machen (sind nur Target-Marker)
            HideTargetRenderers(allMeta);

            Debug.Log($"[MapLightApplier] {createdCount} Lichter erstellt, {skippedWeak} schwache uebersprungen, " +
                      $"{targetLookup.Count} Targets gefunden.");
        }

        /// <summary>
        /// Entfernt alle zur Laufzeit erstellten Light-GameObjects.
        /// Wird beim Map-Wechsel aufgerufen.
        /// </summary>
        public void ClearLights()
        {
            foreach (GameObject lightGO in m_CreatedLights)
            {
                if (lightGO != null)
                {
                    Object.Destroy(lightGO);
                }
            }

            m_CreatedLights.Clear();
        }

        #region Target Lookup

        /// <summary>
        /// Baut ein Dictionary aller info_notnull Entities mit targetname auf.
        /// Key = targetname, Value = Transform der Entity.
        /// </summary>
        private Dictionary<string, Transform> BuildTargetLookup(Ghoul2Meta[] allMeta)
        {
            Dictionary<string, Transform> lookup = new();

            foreach (Ghoul2Meta meta in allMeta)
            {
                string classname = meta.GetString("classname");
                if (classname != "info_notnull")
                {
                    continue;
                }

                string targetname = meta.GetString("targetname");
                if (string.IsNullOrEmpty(targetname))
                {
                    continue;
                }

                if (!lookup.ContainsKey(targetname))
                {
                    lookup[targetname] = meta.transform;
                }
                else
                {
                    Debug.LogWarning($"[MapLightApplier] Doppelter targetname '{targetname}' " +
                                     $"auf '{meta.gameObject.name}', wird ignoriert.");
                }
            }

            return lookup;
        }

        /// <summary>
        /// Deaktiviert die Renderer auf allen info_notnull Entities,
        /// da sie nur als unsichtbare Target-Marker dienen.
        /// </summary>
        private void HideTargetRenderers(Ghoul2Meta[] allMeta)
        {
            foreach (Ghoul2Meta meta in allMeta)
            {
                string classname = meta.GetString("classname");
                if (classname != "info_notnull")
                {
                    continue;
                }

                if (meta.TryGetComponent(out Renderer renderer))
                {
                    renderer.enabled = false;
                }
            }
        }

        #endregion

        #region Light Creation

        /// <summary>
        /// Erstellt ein einzelnes Unity-Light aus einer Ghoul2Meta mit classname="light".
        /// Liest _color, light (Range), scale, light_type und optional target.
        /// </summary>
        private GameObject CreateLight(Ghoul2Meta meta, Dictionary<string, Transform> targetLookup)
        {
            // _color parsen: "(r, g, b, a)" Format oder "(r g b a)"
            // Alpha wird ignoriert – Unity Light.color nutzt kein Alpha für Helligkeit.
            Color lightColor = ParseColor(meta.GetString("_color"), Color.white);

            // light = Lichtstaerke in idTech3-Einheiten (Default 300, typisch 100-4096)
            // Wird sowohl fuer Range als auch Intensitaet verwendet.
            float lightRange = meta.GetFloat("light", k_IdTech3DefaultLight);

            // Range: Quake-Units → Meter, mit Multiplikator fuer Falloff-Kompensation.
            // idTech3 linear Falloff deckt mehr Flaeche ab als URP 1/d².
            float unityRange = Mathf.Min(lightRange * k_LightRangeScale * k_RangeMultiplier, k_MaxRange);

            // Intensitaet: Quadratwurzel-Skalierung statt linear.
            // Daempft Hotspots nahe der Lichtquelle (wo URP 1/d² extrem hell wird),
            // behaelt aber relative Unterschiede zwischen schwachen und starken Lichtern bei.
            // light=300 → 2.0, light=1000 → 3.65, light=4096 → 7.39
            float intensity = k_BaseIntensity * Mathf.Sqrt(lightRange / k_IdTech3DefaultLight);

            // light_type bestimmt Point oder Spot
            string lightTypeStr = meta.GetString("light_type", "Point");
            LightType lightType = ParseLightType(lightTypeStr);

            // Optional: target für Spot-Ausrichtung
            string target = meta.GetString("target");

            // Light-GameObject an der Position des Entities erstellen
            GameObject lightGO = new($"SoF2_Light_{meta.gameObject.name}")
            {
                hideFlags = HideFlags.DontSave
            };
            lightGO.transform.position = meta.transform.position;
            lightGO.transform.SetParent(meta.transform.parent);

            Light light = lightGO.AddComponent<Light>();
            light.type = lightType;
            light.color = new Color(lightColor.r, lightColor.g, lightColor.b, 1f);
            light.intensity = intensity;
            light.range = unityRange;

            // Keine Schatten für Map-Lichter – URP Shadow-Atlas kann nicht
            // hunderte Punctual-Light Shadow Maps verwalten. Nur das
            // Directional Sun Light (MapSkyboxApplier) wirft Schatten.
            light.shadows = LightShadows.None;

            if (lightType == LightType.Spot)
            {
                light.spotAngle = k_DefaultSpotAngle;

                // Spot-Light auf Target ausrichten
                if (!string.IsNullOrEmpty(target) && targetLookup.TryGetValue(target, out Transform targetTransform))
                {
                    Vector3 direction = targetTransform.position - lightGO.transform.position;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        lightGO.transform.rotation = Quaternion.LookRotation(direction.normalized);
                    }
                }
                else if (!string.IsNullOrEmpty(target))
                {
                    Debug.LogWarning($"[MapLightApplier] Spot-Light '{meta.gameObject.name}' hat target='{target}', " +
                                     $"aber kein passendes info_notnull mit targetname='{target}' gefunden.");
                }
            }

            return lightGO;
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Parst eine idTech3/SoF2 Farbangabe.
        /// Unterstützte Formate:
        /// - "(r, g, b, a)" mit Komma
        /// - "(r g b a)" mit Leerzeichen
        /// - "r g b a" ohne Klammern
        /// Werte sind float 0.0-1.0.
        /// </summary>
        private Color ParseColor(string colorStr, Color fallback)
        {
            if (string.IsNullOrEmpty(colorStr))
            {
                return fallback;
            }

            string cleaned = colorStr.Trim().Trim('(', ')');

            // Kommas durch Leerzeichen ersetzen für einheitliches Parsing
            cleaned = cleaned.Replace(',', ' ');

            string[] parts = cleaned.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
            {
                Debug.LogWarning($"[MapLightApplier] Ungültiges Farbformat: '{colorStr}'");
                return fallback;
            }

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b))
            {
                Debug.LogWarning($"[MapLightApplier] Farbe konnte nicht geparst werden: '{colorStr}'");
                return fallback;
            }

            // Alpha ist optional, Standardwert 1.0
            float a = 1f;
            if (parts.Length >= 4)
            {
                float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out a);
            }

            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Parst den Light-Typ String zu Unity LightType.
        /// "Point" → LightType.Point, "Spot" → LightType.Spot.
        /// Fallback: Point.
        /// </summary>
        private LightType ParseLightType(string lightTypeStr)
        {
            if (string.IsNullOrEmpty(lightTypeStr))
            {
                return LightType.Point;
            }

            string lower = lightTypeStr.Trim().ToLowerInvariant();
            return lower switch
            {
                "spot" => LightType.Spot,
                "point" => LightType.Point,
                _ => LightType.Point
            };
        }

        #endregion
    }
}
