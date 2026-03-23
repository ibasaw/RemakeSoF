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
        /// Kalibriert fuer den SoF2/MapSurface Shader mit _LightBlend=0.7:
        /// Point/Spot-Lights sind primaere Lichtquellen — unbeleuchtete
        /// Bereiche werden auf 30% abgedunkelt, Lichter hellen klar auf.
        /// light=300 → 8.0 Intensity.
        /// </summary>
        private const float k_BaseIntensity = 8f;

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
        /// Minimale Unity-Range in Metern.
        /// Auch schwache Lichter sollten innerhalb von 2m sichtbar sein.
        /// </summary>
        private const float k_MinRange = 2f;

        /// <summary>
        /// Standard Spot-Winkel in Grad.
        /// </summary>
        private const float k_DefaultSpotAngle = 50f;

        /// <summary>
        /// Minimaler effektiver Light-Wert (light × scale). Lichter darunter werden uebersprungen.
        /// 0 = nichts ueberspringen, alle Lichter erstellen.
        /// </summary>
        private const float k_MinLightValue = 0f;

        /// <summary>
        /// Aktivierungsradius fuer Proximity-Culling in Metern.
        /// Lichter ausserhalb dieses Radius zur Kamera werden deaktiviert.
        /// Forward+ hat kein Per-Object-Limit, aber hunderte aktive Lights
        /// kosten trotzdem GPU-Performance (Cluster-Evaluierung).
        /// 120m deckt typische SoF2-Map-Bereiche grosszuegig ab.
        /// </summary>
        private const float k_ProximityRadius = 120f;

        /// <summary>
        /// Quadrierter Aktivierungsradius (vermeidet Sqrt pro Licht pro Frame).
        /// </summary>
        private const float k_ProximityRadiusSqr = k_ProximityRadius * k_ProximityRadius;

        /// <summary>
        /// Gecachte Lichtdaten fuer Proximity-Culling.
        /// Position wird beim Erstellen gespeichert (Map-Lichter bewegen sich nicht).
        /// </summary>
        private struct ProximityLightData
        {
            public Light LightComponent;
            public Vector3 Position;
        }

        /// <summary>
        /// Liste der zur Laufzeit erstellten Light-GameObjects fuer Cleanup.
        /// </summary>
        private readonly List<GameObject> m_CreatedLights = new();

        /// <summary>
        /// Alle erstellten Lichter mit Position fuer Proximity-Culling.
        /// </summary>
        private readonly List<ProximityLightData> m_ProximityData = new();

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

                // Schwache Lichter ueberspringen — haetten kaum sichtbaren Effekt.
                // Beruecksichtigt scale-Multiplikator (z.B. light=8192, scale=0.008 → effektiv 65.5).
                float lightValue = meta.GetFloat("light", k_IdTech3DefaultLight);
                float scale = meta.GetFloat("scale", 1f);
                if (lightValue * scale < k_MinLightValue)
                {
                    skippedWeak++;
                    continue;
                }

                GameObject lightGO = CreateLight(meta, targetLookup);
                if (lightGO != null)
                {
                    m_CreatedLights.Add(lightGO);
                    createdCount++;

                    // Debug: Erste 10 Lichter loggen fuer Diagnose.
                    if (createdCount <= 10)
                    {
                        Light dbgLight = lightGO.GetComponent<Light>();
                        Debug.Log($"[MapLightApplier] Light #{createdCount}: '{meta.gameObject.name}' " +
                                  $"light={lightValue} scale={scale} " +
                                  $"range={dbgLight.range:F1}m intensity={dbgLight.intensity:F1} " +
                                  $"color={dbgLight.color} type={dbgLight.type} " +
                                  $"pos={lightGO.transform.position}");
                    }
                }
            }

            // info_notnull Renderer ebenfalls unsichtbar machen (sind nur Target-Marker)
            HideTargetRenderers(allMeta);

            Debug.Log($"[MapLightApplier] {createdCount} Lichter erstellt (Forward+, Proximity-Culling aktiv), " +
                      $"{skippedWeak} schwache uebersprungen, {targetLookup.Count} Targets gefunden.");
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
            m_ProximityData.Clear();

            // Sicherheitsnetz: verwaiste SoF2_Light_ Objekte aus vorherigen Play-Sessions zerstoeren
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light != null && light.gameObject.name.StartsWith("SoF2_Light_"))
                {
                    Object.Destroy(light.gameObject);
                }
            }
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

            // light = idTech3-Lichtradius in Quake-Units (Default 300, typisch 100-8192).
            // Bestimmt die REICHWEITE des Lichts.
            float lightValue = meta.GetFloat("light", k_IdTech3DefaultLight);

            // scale = Helligkeits-Multiplikator (idTech3: "scale" Key, Default 1.0).
            // Reduziert nur die INTENSITAET, nicht die Reichweite.
            // Beispiel: light=8192, scale=0.008 → grossflaechig (8192 Units) aber schwach (0.8%).
            float scale = meta.GetFloat("scale", 1f);
            float effectiveBrightness = lightValue * scale;

            // Range: basiert auf dem rohen light-Wert (Reichweite in Quake-Units).
            // Quake-Units → Meter, mit Multiplikator fuer Falloff-Kompensation.
            float unityRange = Mathf.Clamp(
                lightValue * k_LightRangeScale * k_RangeMultiplier,
                k_MinRange,
                k_MaxRange);

            // Intensitaet: basiert auf effektiver Helligkeit (light × scale).
            // Quadratwurzel-Skalierung daempft Hotspots nahe der Lichtquelle.
            float intensity = k_BaseIntensity * Mathf.Sqrt(effectiveBrightness / k_IdTech3DefaultLight);

            // Minimum-Intensitaet damit auch schwache Lichter sichtbar bleiben.
            intensity = Mathf.Max(intensity, 1f);

            // light_type bestimmt Point oder Spot
            string lightTypeStr = meta.GetString("light_type", "Point");
            LightType lightType = ParseLightType(lightTypeStr);

            // Optional: target für Spot-Ausrichtung
            string target = meta.GetString("target");

            // Light-GameObject an der Position des Entities erstellen
            GameObject lightGO = new($"SoF2_Light_{meta.gameObject.name}");
            lightGO.transform.position = meta.transform.position;
            lightGO.transform.SetParent(meta.transform.parent);

            Light light = lightGO.AddComponent<Light>();
            light.type = lightType;
            light.color = new Color(lightColor.r, lightColor.g, lightColor.b, 1f);
            light.intensity = intensity;
            light.range = unityRange;

            // Start aktiviert — Forward+ kann viele Lichter gleichzeitig verarbeiten.
            // ProximityCuller deaktiviert entfernte Lichter fuer GPU-Performance.
            light.enabled = true;

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
                else
                {
                    // idTech3 "angle" Key: Yaw-Richtung der Entity.
                    // Spezialwerte: -1 = straight up, -2 = straight down.
                    // Fallback fuer Spot-Lights ohne Target-Entity.
                    ApplyAngle(lightGO.transform, meta);

                    if (!string.IsNullOrEmpty(target))
                    {
                        Debug.LogWarning($"[MapLightApplier] Spot-Light '{meta.gameObject.name}' hat target='{target}', " +
                                         $"aber kein passendes info_notnull mit targetname='{target}' gefunden.");
                    }
                }
            }
            else
            {
                // Point-Lights: "angle" kann trotzdem vorhanden sein (Entity-Ausrichtung).
                // Hat keinen visuellen Effekt auf Point-Lights, aber korrekt setzen
                // falls spaeter der Typ geaendert wird oder Cookies genutzt werden.
                ApplyAngle(lightGO.transform, meta);
            }

            // Fuer Proximity-Culling registrieren
            m_ProximityData.Add(new ProximityLightData
            {
                LightComponent = lightGO.GetComponent<Light>(),
                Position = lightGO.transform.position
            });

            return lightGO;
        }

        #endregion

        #region Proximity Culling

        /// <summary>
        /// Aktiviert Lichter innerhalb des Proximity-Radius und deaktiviert entfernte.
        /// Wird vom MapLightProximityCuller-MonoBehaviour aufgerufen.
        /// </summary>
        /// <param name="viewerPosition">Position des Betrachters (Kamera).</param>
        /// <returns>Anzahl der aktuell aktiven Lichter.</returns>
        public int UpdateProximity(Vector3 viewerPosition)
        {
            int activeCount = 0;

            for (int i = 0; i < m_ProximityData.Count; i++)
            {
                ProximityLightData data = m_ProximityData[i];
                if (data.LightComponent == null)
                {
                    continue;
                }

                float sqrDist = (data.Position - viewerPosition).sqrMagnitude;
                bool shouldBeActive = sqrDist <= k_ProximityRadiusSqr;

                if (data.LightComponent.enabled != shouldBeActive)
                {
                    data.LightComponent.enabled = shouldBeActive;
                }

                if (shouldBeActive)
                {
                    activeCount++;
                }
            }

            return activeCount;
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Wendet den idTech3 "angle" Key auf ein Transform an.
        /// Spezialwerte: -1 = straight up (90° Pitch), -2 = straight down (-90° Pitch).
        /// Sonst: Yaw-Rotation 0-360° (0=Nord/+Z, 90=Ost/+X, 180=Sued/-Z, 270=West/-X).
        /// Ohne "angle" Key: keine Aenderung (Default forward = +Z).
        /// </summary>
        private void ApplyAngle(Transform transform, Ghoul2Meta meta)
        {
            string angleStr = meta.GetString("angle");
            if (string.IsNullOrEmpty(angleStr))
            {
                return;
            }

            if (!float.TryParse(angleStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float angle))
            {
                return;
            }

            // idTech3 Spezialwerte
            if (Mathf.Approximately(angle, -1f))
            {
                // Straight up
                transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            }
            else if (Mathf.Approximately(angle, -2f))
            {
                // Straight down
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                // idTech3: angle = Yaw in Grad (0=Nord, 90=Ost, etc.)
                // Unity: Y-Rotation entspricht Yaw
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

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
