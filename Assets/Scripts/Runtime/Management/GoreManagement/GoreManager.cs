using System;
using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Orchestriert das SoF2 Gore-System: empfaengt Treffer-Daten, loest HitRegion auf GoreArea auf,
    /// delegiert an GoreApplier fuer Surface-Aenderungen, Chunk-Spawning, BoltOns, und FX.
    /// Verwaltet Gore-State pro Charakter (welche Areas bereits abgetrennt sind).
    /// Pure Service, zugreifbar ueber ServiceLocator.
    ///
    /// SoF2 DamageLevel-Semantik:
    /// 0 = Low          → Blut-Effekt, kein Dismemberment
    /// 1 = Medium       → Staerkerer Blut-Effekt, kein Dismemberment
    /// 2 = High         → Starker Blut-Effekt, eventuell kleine Chunks
    /// 3 = Low Death    → Tod, minimales Gore (kein Dismemberment)
    /// 4 = Medium Death → Tod + Dismemberment der getroffenen Zone
    /// 5 = High Death   → Tod + massives Gore + Kinderzonen fliegen mit
    /// </summary>
    public class GoreManager
    {
        /// <summary>Minimaler DamageLevel fuer Blut-Effekte (Treffer ohne Tod).</summary>
        private const int DAMAGE_LEVEL_BLOOD_FX = 0;

        /// <summary>Minimaler DamageLevel fuer Dismemberment (Tod mit Gore).</summary>
        private const int DAMAGE_LEVEL_DISMEMBER = 4;

        /// <summary>DamageLevel fuer maximales Gore (alle Kinderzonen fliegen ebenfalls).</summary>
        private const int DAMAGE_LEVEL_HIGH_DEATH = 5;

        private readonly GoreApplier m_Applier = new();

        /// <summary>PGORE Wund-Decal-Applier fuer direkte Mesh-Projektion.</summary>
        private readonly PGoreDecalApplier m_PGoreApplier = new();

        /// <summary>
        /// Tracking welche Gore-Areas pro Charakter bereits angewendet wurden.
        /// Key = Character InstanceID, Value = Set von bereits abgetrennten GoreArea-Locations.
        /// </summary>
        private readonly Dictionary<int, HashSet<string>> m_AppliedAreas = new();

        /// <summary>
        /// Verarbeitet einen Gore-Treffer. Haupteintrittspunkt fuer das Gore-System.
        /// Wird vom Damage/Hit-System aufgerufen nachdem Schaden berechnet wurde.
        /// </summary>
        /// <param name="hitData">Treffer-Daten mit Charakter, Region, DamageLevel und Richtung.</param>
        public void ProcessGoreHit(GoreHitData hitData)
        {
            if (hitData.CharacterRoot == null)
            {
                return;
            }

            GoreDataLoader goreDataLoader = ServiceLocator.Get<GoreDataLoader>();
            if (goreDataLoader == null)
            {
                Debug.LogWarning("[GoreManager] GoreDataLoader not registered in ServiceLocator.");
                return;
            }

            // HitRegion → GoreArea mappen
            if (!GoreHitRegionMapping.TryGetMapping(hitData.HitRegion, out GoreHitRegionMapping.GoreAreaMapping mapping))
            {
                Debug.LogWarning($"[GoreManager] No gore area mapping for HitRegion: {hitData.HitRegion}");
                return;
            }

            // Fuer zentrale Koerperregionen (Kopf, Hals, Brust, Bauch, Leiste):
            // Seite anhand der Treffer-Position bestimmen statt hardcoded isRightSide.
            // Nutzt Bone-Distanz-Vergleich (lfemurYZ vs rfemurYZ) statt lokaler X-Achse.
            GoreHitRegionMapping.GoreAreaMapping effectiveMapping = mapping;
            if (IsCenterBodyRegion(hitData.HitRegion) && hitData.CharacterRoot != null)
            {
                bool isRightSide = DetermineSideFromHitPosition(hitData);
                effectiveMapping = new GoreHitRegionMapping.GoreAreaMapping(mapping.GoreAreaLocation, isRightSide);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[GoreManager] GORE-HIT: HitRegion={hitData.HitRegion}, Area={effectiveMapping.GoreAreaLocation}, IsRightSide={effectiveMapping.IsRightSide}, DL={hitData.DamageLevel}, IsCenter={IsCenterBodyRegion(hitData.HitRegion)}");
#endif

            // Low-Level Treffer: nur Blut-FX, kein Dismemberment
            if (hitData.DamageLevel < DAMAGE_LEVEL_DISMEMBER)
            {
                ProcessBloodEffect(hitData, effectiveMapping, goreDataLoader);
                return;
            }

            // Dismemberment-Level: Gore-Area vollstaendig anwenden
            // Hip/Groin: BEIDE Seiten abtrennen (hip_l + hip_r)
            if (hitData.HitRegion == HitRegion.Groin)
            {
                ProcessDismemberment(hitData, new GoreHitRegionMapping.GoreAreaMapping("hip", true), goreDataLoader);
                ProcessDismemberment(hitData, new GoreHitRegionMapping.GoreAreaMapping("hip", false), goreDataLoader);
            }
            else
            {
                ProcessDismemberment(hitData, effectiveMapping, goreDataLoader);
            }
        }

        /// <summary>
        /// Verarbeitet einen Blut-Effekt ohne Dismemberment (DamageLevel 0-3).
        /// Spielt Blood-FX an den definierten Bolt-Positionen ab und projiziert PGORE-Wund-Decals.
        /// Bolt-Positionen kommen aus den BloodFX-Definitionen (z.B. *neckg, *hip_rg),
        /// NICHT aus hitData.HitPoint — damit erscheinen Effekte am Koerper statt auf der Hitbox-Oberflaeche.
        /// </summary>
        private void ProcessBloodEffect(GoreHitData hitData, GoreHitRegionMapping.GoreAreaMapping mapping, GoreDataLoader goreDataLoader)
        {
            // GoreArea laden und Platzhalter aufloesen
            GoreArea area = goreDataLoader.GetAreaByLocation(mapping.GoreAreaLocation);
            if (area == null)
            {
                return;
            }

            GoreArea resolvedArea = m_Applier.ResolveArea(area, mapping.IsRightSide);

            // BloodFX bevorzugen (speziell fuer nicht-letale Treffer), Fallback auf FX
            List<GoreEffect> bloodEffects = resolvedArea.BloodFX != null && resolvedArea.BloodFX.Count > 0
                ? resolvedArea.BloodFX
                : resolvedArea.FX;

            if (bloodEffects != null && bloodEffects.Count > 0)
            {
                Game.Effects.EffectFactory effectFactory = ServiceLocator.Get<Game.Effects.EffectFactory>();
                if (effectFactory != null)
                {
                    // Model-Root fuer Bolt-Suche finden
                    Transform modelRoot = FindModelRoot(hitData.CharacterRoot);

                    foreach (GoreEffect fx in bloodEffects)
                    {
                        if (string.IsNullOrEmpty(fx.Name))
                        {
                            continue;
                        }

                        // Effekt-Position: Bolt-Position bevorzugen, hitData.HitPoint als Fallback.
                        // Bolt-Positionen platzieren Blut-Effekte korrekt am Koerper
                        // statt auf der abstrakten Hitbox-Collider-Oberflaeche.
                        Vector3 effectPosition = hitData.HitPoint;
                        Vector3 effectNormal = hitData.HitDirection * -1f;

                        if (!string.IsNullOrEmpty(fx.Bolt) && modelRoot != null)
                        {
                            Transform boltTransform = FindBoltTransform(modelRoot, fx.Bolt);
                            if (boltTransform != null)
                            {
                                effectPosition = boltTransform.position;
                            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            else
                            {
                                Debug.LogWarning($"[GoreManager] BloodFX bolt not found: '{fx.Bolt}' for effect '{fx.Name}'");
                            }
#endif
                        }

                        // Effekt-ID aufloesen (fx.Name → "effects/fx.Name" falls noetig)
                        string effectId = ResolveBloodEffectId(fx.Name, effectFactory);
                        effectFactory.SpawnImpactEffect(effectPosition, effectNormal, effectId);
                    }
                }
            }

            // PGORE Wund-Decals auf dem Charakter-Mesh projizieren
            ApplyPGoreDecals(hitData);

            // Blutfleck am Boden unter dem Trefferpunkt spawnen (SoF2: blood_splat_mp_small)
            // Nur wenn ein Charakter-Root vorhanden ist (fuer Boden-Raycast)
            SpawnFloorBloodSplat(hitData);
        }

        /// <summary>
        /// Findet den model_root-Transform unter dem Character-Root-GameObject.
        /// SoF2-Surfaces und *Bolts leben unter model_root in der Hierarchie:
        /// PlayerCharacter → VisualRoot → skinClone → model_root_0.
        /// </summary>
        private static Transform FindModelRoot(GameObject characterRoot)
        {
            if (characterRoot == null)
            {
                return null;
            }

            Transform result = FindDeepChildStatic(characterRoot.transform, "model_root");
            return result != null ? result : characterRoot.transform;
        }

        /// <summary>
        /// Findet einen Bolt-Transform anhand eines SoF2-Bolt-Namens.
        /// SoF2 Bolt-Namen beginnen mit '*' (z.B. "*neckg", "*hip_rg").
        /// Entfernt '*'-Praefix und Unity-Suffix fuer die Suche.
        /// </summary>
        private static Transform FindBoltTransform(Transform root, string boltName)
        {
            if (string.IsNullOrEmpty(boltName))
            {
                return null;
            }

            string cleanName = boltName.TrimStart('*');
            // Unity-Suffix (_0, _1 etc.) ebenfalls entfernen
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"_\d+$", "");
            return FindBoltTransformRecursive(root, cleanName);
        }

        /// <summary>
        /// Rekursive Suche nach einem Bolt-Transform (case-insensitive, mit Name-Normalisierung).
        /// </summary>
        private static Transform FindBoltTransformRecursive(Transform parent, string normalizedName)
        {
            // Exakter Match
            if (string.Equals(parent.name, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            // Normalisierter Match: '*'-Praefix und '_N'-Suffix entfernen
            string cleanParentName = parent.name.TrimStart('*');
            cleanParentName = System.Text.RegularExpressions.Regex.Replace(cleanParentName, @"_\d+$", "");
            if (string.Equals(cleanParentName, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform result = FindBoltTransformRecursive(parent.GetChild(i), normalizedName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Spawnt einen kleinen Blutfleck-Decal am Boden unterhalb des Trefferpunkts.
        /// Nutzt Raycast nach unten um die exakte Bodenposition zu finden.
        /// Fuer nicht-letale Treffer (DamageLevel 0-3) wo kein Dismemberment stattfindet.
        /// </summary>
        private void SpawnFloorBloodSplat(GoreHitData hitData)
        {
            Game.Effects.EffectFactory effectFactory = ServiceLocator.Get<Game.Effects.EffectFactory>();
            if (effectFactory == null)
            {
                return;
            }

            // Raycast nach unten um Bodenposition zu finden
            Vector3 origin = hitData.HitPoint;
            int groundMask = ~LayerMask.GetMask("Hitbox", "Player", "BrushCollision");
            Vector3 floorPos = origin;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, 3f, groundMask))
            {
                floorPos = groundHit.point;
            }
            else if (hitData.CharacterRoot != null)
            {
                floorPos = hitData.CharacterRoot.transform.position;
            }

            // Kleinen Blutfleck am Boden spawnen (blood_splat_mp_small fuer nicht-letale Treffer)
            effectFactory.SpawnImpactEffect(floorPos, Vector3.up, "effects/blood_splat_mp_small");
        }

        /// <summary>
        /// Erzeugt und projiziert PGORE-Wund-Decals auf das Charakter-Mesh.
        /// Nutzt PGoreWeaponDispatch fuer waffenspezifische Decal-Auswahl (1:1 SoF2)
        /// und PGoreDecalApplier fuer die Quad-basierte Mesh-Projektion.
        /// </summary>
        private void ApplyPGoreDecals(GoreHitData hitData)
        {
            if (string.IsNullOrEmpty(hitData.WeaponId) || hitData.CharacterRoot == null)
            {
                return;
            }

            List<PGoreData> goreEntries = PGoreWeaponDispatch.CreateGoreEntries(
                hitData.WeaponId,
                hitData.IsAltAttack,
                hitData.HitPoint,
                hitData.HitDirection);

            if (goreEntries.Count > 0)
            {
                m_PGoreApplier.ApplyGoreDecals(goreEntries, hitData.CharacterRoot);
            }
        }

        /// <summary>
        /// Loest eine Blood-Effekt-ID auf. Probiert den Namen direkt und mit "effects/"-Praefix.
        /// </summary>
        private string ResolveBloodEffectId(string effectName, Game.Effects.EffectFactory effectFactory)
        {
            // Direkt probieren
            if (effectFactory.GetDefinition(effectName) != null)
            {
                return effectName;
            }

            // Mit "effects/"-Praefix probieren
            string prefixed = $"effects/{effectName}";
            if (effectFactory.GetDefinition(prefixed) != null)
            {
                return prefixed;
            }

            // _mp-Variante probieren
            string mpName = $"effects/{effectName}_mp";
            if (effectFactory.GetDefinition(mpName) != null)
            {
                return mpName;
            }

            return effectName;
        }

        /// <summary>
        /// Verarbeitet Dismemberment: Gore-Area komplett anwenden (Surfaces, Chunks, BoltOns, FX).
        /// Prueft ob die Area bereits abgetrennt wurde und verhindert Doppel-Dismemberment.
        /// </summary>
        private void ProcessDismemberment(GoreHitData hitData, GoreHitRegionMapping.GoreAreaMapping mapping, GoreDataLoader goreDataLoader)
        {
            int characterId = hitData.CharacterRoot.GetInstanceID();

            // Tracking-Set fuer diesen Charakter anlegen falls noetig
            if (!m_AppliedAreas.TryGetValue(characterId, out HashSet<string> appliedSet))
            {
                appliedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                m_AppliedAreas[characterId] = appliedSet;
            }

            // Seitlich aufgeloesten Area-Key erstellen
            string resolvedLocation = GorePlaceholderResolver.Resolve(mapping.GoreAreaLocation, mapping.IsRightSide);
            string sideKey = mapping.IsRightSide ? "r" : "l";
            string trackingKey = $"{resolvedLocation}_{sideKey}";

            // Bereits abgetrennt → nur FX abspielen
            if (appliedSet.Contains(trackingKey))
            {
                Debug.Log($"[GoreManager] Area already dismembered: {trackingKey}");
                ProcessBloodEffect(hitData, mapping, goreDataLoader);
                return;
            }

            // GoreArea laden
            GoreArea area = goreDataLoader.GetAreaByLocation(mapping.GoreAreaLocation);
            if (area == null)
            {
                Debug.LogWarning($"[GoreManager] GoreArea not found: {mapping.GoreAreaLocation}");
                return;
            }

            // Platzhalter aufloesen
            GoreArea resolvedArea = m_Applier.ResolveArea(area, mapping.IsRightSide);

            // Gore-Area auf den Charakter anwenden (Surfaces, Chunks, BoltOns, FX)
            m_Applier.ApplyGoreArea(
                hitData.CharacterRoot,
                resolvedArea,
                hitData.HitDirection,
                goreDataLoader,
                mapping.IsRightSide);

            // PGORE Wund-Decals auch bei Dismemberment anwenden (Todestreferwunde)
            ApplyPGoreDecals(hitData);

            // Als abgetrennt markieren
            appliedSet.Add(trackingKey);

            // Hitboxen der abgetrennten Region deaktivieren
            DisableHitboxesForArea(hitData.CharacterRoot, mapping.GoreAreaLocation, mapping.IsRightSide);

            // Kinder-Hitboxen IMMER deaktivieren: wenn z.B. ein Bein abgetrennt wird,
            // muss auch der Fuss-Hitbox ausgeschaltet werden (unabhaengig vom DamageLevel).
            // Die visuelle Kinder-Dismemberment-Kaskade bleibt auf High Death beschraenkt.
            if (area.Children != null)
            {
                DisableHitboxesForChildren(hitData.CharacterRoot, area.Children, mapping.IsRightSide, goreDataLoader);
            }

            // Bei High Death auch alle Kinderzonen als visuell abgetrennt markieren
            if (hitData.DamageLevel >= DAMAGE_LEVEL_HIGH_DEATH && area.Children != null)
            {
                MarkChildrenAsApplied(area.Children, mapping.IsRightSide, appliedSet, goreDataLoader);
            }

            Debug.Log($"[GoreManager] Dismemberment applied: {trackingKey} (DamageLevel={hitData.DamageLevel})");
        }

        /// <summary>
        /// Markiert alle Kinderzonen rekursiv als abgetrennt im Tracking-Set.
        /// </summary>
        private void MarkChildrenAsApplied(List<string> children, bool isRightSide, HashSet<string> appliedSet, GoreDataLoader goreDataLoader)
        {
            if (children == null)
            {
                return;
            }

            string sideKey = isRightSide ? "r" : "l";

            foreach (string child in children)
            {
                string resolvedChild = GorePlaceholderResolver.Resolve(child, isRightSide);
                string childTrackingKey = $"{resolvedChild}_{sideKey}";
                appliedSet.Add(childTrackingKey);

                // Rekursiv fuer Enkelkinder
                GoreArea childArea = goreDataLoader.GetAreaByLocation(resolvedChild);
                if (childArea == null)
                {
                    childArea = goreDataLoader.GetAreaByLocation(child);
                }

                if (childArea?.Children != null)
                {
                    MarkChildrenAsApplied(childArea.Children, isRightSide, appliedSet, goreDataLoader);
                }
            }
        }

        /// <summary>
        /// Deaktiviert alle Hitboxen die zur angegebenen GoreArea-Location gehoeren.
        /// Findet das ClientHitboxSystem am Charakter und deaktiviert passende HitRegions.
        /// Fuer zentrale Areas (head, torso, hip) ignoriert GetHitRegionsForArea die Seite,
        /// da diese nur einen Hitbox-Collider besitzen.
        /// </summary>
        private void DisableHitboxesForArea(GameObject characterRoot, string goreAreaLocation, bool isRightSide)
        {
            ClientHitboxSystem hitboxSystem = characterRoot.GetComponentInChildren<ClientHitboxSystem>();
            if (hitboxSystem == null)
            {
                return;
            }

            List<HitRegion> regions = GoreHitRegionMapping.GetHitRegionsForArea(goreAreaLocation, isRightSide);

            foreach (HitRegion region in regions)
            {
                hitboxSystem.DisableHitboxForRegion(region);
            }
        }

        /// <summary>
        /// Deaktiviert Hitboxen fuer alle Kinderzonen rekursiv (bei High Death Dismemberment).
        /// </summary>
        private void DisableHitboxesForChildren(GameObject characterRoot, List<string> children, bool isRightSide, GoreDataLoader goreDataLoader)
        {
            if (children == null)
            {
                return;
            }

            foreach (string child in children)
            {
                string resolvedChild = GorePlaceholderResolver.Resolve(child, isRightSide);
                DisableHitboxesForArea(characterRoot, resolvedChild, isRightSide);

                GoreArea childArea = goreDataLoader.GetAreaByLocation(resolvedChild);
                if (childArea == null)
                {
                    childArea = goreDataLoader.GetAreaByLocation(child);
                }

                if (childArea?.Children != null)
                {
                    DisableHitboxesForChildren(characterRoot, childArea.Children, isRightSide, goreDataLoader);
                }
            }
        }

        /// <summary>
        /// Prueft ob eine bestimmte Gore-Area an einem Charakter bereits abgetrennt wurde.
        /// </summary>
        /// <param name="characterRoot">Das Charakter-GameObject.</param>
        /// <param name="goreAreaLocation">Die Gore-Area-Location (z.B. "hand", "arm_lower").</param>
        /// <param name="isRightSide">Seite der Abtrennung.</param>
        /// <returns>True wenn die Area bereits abgetrennt wurde.</returns>
        public bool IsAreaDismembered(GameObject characterRoot, string goreAreaLocation, bool isRightSide)
        {
            if (characterRoot == null || string.IsNullOrEmpty(goreAreaLocation))
            {
                return false;
            }

            int characterId = characterRoot.GetInstanceID();
            if (!m_AppliedAreas.TryGetValue(characterId, out HashSet<string> appliedSet))
            {
                return false;
            }

            string sideKey = isRightSide ? "r" : "l";
            string trackingKey = $"{goreAreaLocation}_{sideKey}";
            return appliedSet.Contains(trackingKey);
        }

        /// <summary>
        /// Setzt den Gore-State eines Charakters zurueck (z.B. bei Respawn).
        /// </summary>
        /// <param name="characterRoot">Das Charakter-GameObject.</param>
        public void ResetCharacterGore(GameObject characterRoot)
        {
            if (characterRoot == null)
            {
                return;
            }

            int characterId = characterRoot.GetInstanceID();
            m_AppliedAreas.Remove(characterId);
        }

        /// <summary>
        /// Entfernt alle gespeicherten Gore-States. Wird bei Scene-Wechsel oder Cleanup aufgerufen.
        /// </summary>
        public void ClearCache()
        {
            m_AppliedAreas.Clear();
            m_PGoreApplier.ClearCache();
            Debug.Log("[GoreManager] All character gore states cleared.");
        }

        /// <summary>
        /// Prueft ob eine HitRegion zum zentralen Koerperbereich gehoert (kein festes Links/Rechts).
        /// Fuer diese Regionen wird die Seite dynamisch aus der Treffer-Position bestimmt.
        /// </summary>
        private static bool IsCenterBodyRegion(HitRegion hitRegion)
        {
            return hitRegion == HitRegion.Head
                || hitRegion == HitRegion.Neck
                || hitRegion == HitRegion.Chest
                || hitRegion == HitRegion.Gut;
        }

        /// <summary>
        /// Bestimmt die Koerperseite anhand der Treffer-Position relativ zum Charakter.
        /// Vergleicht die Distanz des HitPoints zu bekannten linken/rechten Referenz-Bones
        /// (lfemurYZ / rfemurYZ). Diese Methode ist rotationsunabhaengig — funktioniert
        /// korrekt unabhaengig von der aktuellen Yaw-Drehung des Charaktermodells.
        /// </summary>
        private static bool DetermineSideFromHitPosition(GoreHitData hitData)
        {
            Transform root = hitData.CharacterRoot.transform;
            Transform leftBone = FindDeepChildStatic(root, "lfemurYZ");
            Transform rightBone = FindDeepChildStatic(root, "rfemurYZ");

            if (leftBone != null && rightBone != null)
            {
                float distToLeft = (hitData.HitPoint - leftBone.position).sqrMagnitude;
                float distToRight = (hitData.HitPoint - rightBone.position).sqrMagnitude;
                bool isRight = distToRight <= distToLeft;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[GoreManager] SIDE-DETECT: distToLeft={distToLeft:F4}, distToRight={distToRight:F4} → isRightSide={isRight}");
#endif
                return isRight;
            }

            // Fallback: lokale X-Achse (unzuverlaessig bei gedrehtem Modell)
            Vector3 localHitPoint = root.InverseTransformPoint(hitData.HitPoint);
            Debug.LogWarning($"[GoreManager] SIDE-DETECT fallback (bones not found): localX={localHitPoint.x:F3} → isRightSide={localHitPoint.x >= 0f}");
            return localHitPoint.x >= 0f;
        }

        /// <summary>
        /// Rekursive DFS-Suche nach einem Kind-Transform per Name (case-insensitive).
        /// Statische Hilfsmethode fuer GoreManager ohne MonoBehaviour-Abhaengigkeit.
        /// </summary>
        private static Transform FindDeepChildStatic(Transform parent, string childName)
        {
            if (string.Equals(parent.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform result = FindDeepChildStatic(parent.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
