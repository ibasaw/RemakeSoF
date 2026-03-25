using System;
using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
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

            // Low-Level Treffer: nur Blut-FX, kein Dismemberment
            if (hitData.DamageLevel < DAMAGE_LEVEL_DISMEMBER)
            {
                ProcessBloodEffect(hitData, mapping, goreDataLoader);
                return;
            }

            // Dismemberment-Level: Gore-Area vollstaendig anwenden
            ProcessDismemberment(hitData, mapping, goreDataLoader);
        }

        /// <summary>
        /// Verarbeitet einen Blut-Effekt ohne Dismemberment (DamageLevel 0-3).
        /// Spielt nur FX an der Treffer-Position ab.
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

            // Nur FX abspielen, kein Surface-Wechsel
            if (resolvedArea.FX != null && resolvedArea.FX.Count > 0)
            {
                Game.Effects.EffectFactory effectFactory = ServiceLocator.Get<Game.Effects.EffectFactory>();
                if (effectFactory != null)
                {
                    foreach (GoreEffect fx in resolvedArea.FX)
                    {
                        if (string.IsNullOrEmpty(fx.Name))
                        {
                            continue;
                        }

                        // Effekt an der Treffer-Position abspielen
                        effectFactory.SpawnImpactEffect(hitData.HitPoint, hitData.HitDirection * -1f, fx.Name);
                    }
                }
            }
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

            // Als abgetrennt markieren
            appliedSet.Add(trackingKey);

            // Bei High Death auch alle Kinderzonen als abgetrennt markieren
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
            Debug.Log("[GoreManager] All character gore states cleared.");
        }
    }
}
