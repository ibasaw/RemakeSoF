using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Effects;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Reine Asset-Anwendung fuer das Gore-System: schaltet Surfaces on/off, spawnt Chunks als Rigidbody,
    /// befestigt BoltOn-Modelle und spielt Gore-FX ab. Arbeitet nur mit konkreten Daten (keine Loader-Referenzen).
    /// 1:1-Portierung des originalen SoF2 GHOUL2 Gore-Algorithmus.
    /// </summary>
    public class GoreApplier
    {
        private const float CHUNK_LIFETIME = 8f;
        private const float BOLT_ON_SCALE = 1f;
        private const string GORE_PIECE_PREFIX = "GorePiece_";

        /// <summary>
        /// Wendet eine aufgeloeste Gore-Area auf ein Charakter-Prefab an.
        /// Fuehrt den vollstaendigen SoF2-Gore-Algorithmus hierarchisch aus:
        /// 1. Surfaces_Off deaktivieren (Koerperteil entfernen)
        /// 2. Surfaces_On aktivieren (Gore-Caps zeigen)
        /// 3. Kinder rekursiv verarbeiten (mit Flag-Pruefung)
        /// 4. Chunk spawnen (abgetrenntes Teil mit Physik)
        /// 5. BoltOn befestigen (Knochen/Gehirn am Stumpf)
        /// 6. FX abspielen (Blut, Fleisch)
        /// </summary>
        /// <param name="characterRoot">Root-GameObject des Charakters.</param>
        /// <param name="area">Die aufgeloeste GoreArea (Platzhalter bereits ersetzt).</param>
        /// <param name="hitDirection">Richtung des Treffers fuer Chunk-Force.</param>
        /// <param name="goreDataLoader">Loader fuer Kinderzonen und Pieces.</param>
        /// <param name="isRightSide">Seite fuer Platzhalter-Aufloesung der Kinder.</param>
        /// <param name="parentFlags">Flags des Eltern-Bereichs fuer hierarchische Flag-Vererbung.</param>
        public void ApplyGoreArea(
            GameObject characterRoot,
            GoreArea area,
            Vector3 hitDirection,
            GoreDataLoader goreDataLoader,
            bool isRightSide,
            HashSet<string> parentFlags = null)
        {
            if (characterRoot == null || area == null)
            {
                return;
            }

            HashSet<string> flags = new(StringComparer.OrdinalIgnoreCase);
            if (area.Flags != null)
            {
                foreach (string flag in area.Flags)
                {
                    flags.Add(flag);
                }
            }

            // Resolve to model_root_0 where surfaces and *bolts live
            Transform modelRoot = ResolveCharacterModelRoot(characterRoot);
            Renderer[] allRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);

            // 1. Surfaces_Off — Koerperteil-Renderer deaktivieren
            bool suppressSurfacesOn = parentFlags != null && parentFlags.Contains("NoChildSurfacesOn");
            DeactivateSurfaces(allRenderers, area.Surfaces_Off);

            // 2. Surfaces_On — Gore-Cap-Renderer aktivieren (sofern nicht vom Eltern unterdrueckt)
            if (!suppressSurfacesOn)
            {
                ActivateSurfaces(allRenderers, area.Surfaces_On);
            }

            // 3. Kinder rekursiv verarbeiten
            if (area.Children != null && area.Children.Count > 0)
            {
                foreach (string childLocation in area.Children)
                {
                    // SoF2 Seitenkontext-Inversion: Wenn ein Kind-Template <OL> oder <OS> enthaelt,
                    // repraesentiert es die Gegenseite. Fuer dessen Kinder muss isRightSide invertiert werden,
                    // damit <PS>/<OS> in den Enkelkindern korrekt aufgeloest werden.
                    // Beispiel: "head" (isRightSide=true) → Kind "head_<OL>" → Enkel brauchen <PS>=l statt r.
                    bool childIsRightSide = isRightSide;
                    if (childLocation.Contains("<OL>") || childLocation.Contains("<OS>"))
                    {
                        childIsRightSide = !isRightSide;
                    }

                    // Kind-Location auflösen: <OL>/<PL> etc. mit ORIGINAL isRightSide
                    // (damit "head_<OL>" korrekt zu "head_left" wird)
                    string resolvedChildLocation = GorePlaceholderResolver.Resolve(childLocation, isRightSide);
                    GoreArea childArea = goreDataLoader.GetAreaByLocation(resolvedChildLocation);
                    if (childArea == null)
                    {
                        // Versuch mit unaufgeloestem Template-Namen
                        childArea = goreDataLoader.GetAreaByLocation(childLocation);
                    }

                    if (childArea == null)
                    {
                        Debug.LogWarning($"[GoreApplier] Child gore area not found: {resolvedChildLocation}");
                        continue;
                    }

                    // Kind-Area: eigener Inhalt mit ORIGINAL isRightSide aufloesen
                    // (Templates wie head_<OL> verwenden <PS>/<OS> relativ zum Original-Hit),
                    // aber fuer dessen Kinder den invertierten Seitenkontext verwenden.
                    GoreArea resolvedChild = ResolveArea(childArea, isRightSide);
                    ApplyGoreArea(characterRoot, resolvedChild, hitDirection, goreDataLoader, childIsRightSide, flags);
                }
            }

            // 4. Chunk spawnen (sofern nicht vom Eltern unterdrueckt)
            bool suppressChunks = parentFlags != null && parentFlags.Contains("NoChildChunks");
            if (!suppressChunks && area.Chunk != null)
            {
                SpawnChunk(modelRoot.gameObject, allRenderers, area.Chunk, hitDirection);
            }

            // 5. BoltOn befestigen (sofern nicht vom Eltern unterdrueckt)
            bool suppressBoltOns = parentFlags != null && parentFlags.Contains("NoChildBoltOns");
            if (!suppressBoltOns && area.BoltOn != null)
            {
                SpawnBoltOn(modelRoot.gameObject, area.BoltOn, goreDataLoader);
            }

            // 6. FX abspielen (sofern nicht vom Eltern unterdrueckt)
            bool suppressFX = parentFlags != null && parentFlags.Contains("NoChildFX");
            if (!suppressFX && area.FX != null && area.FX.Count > 0)
            {
                SpawnEffects(modelRoot.gameObject, area.FX);
            }
        }

        /// <summary>
        /// Loest alle Platzhalter in einer GoreArea auf und gibt eine Kopie mit aufgeloesten Strings zurueck.
        /// </summary>
        public GoreArea ResolveArea(GoreArea area, bool isRightSide)
        {
            if (area == null)
            {
                return null;
            }

            GoreArea resolved = new();
            resolved.Location = GorePlaceholderResolver.Resolve(area.Location, isRightSide);
            resolved.Flags = area.Flags != null ? new List<string>(area.Flags) : new List<string>();
            resolved.Surfaces_Off = GorePlaceholderResolver.ResolveList(area.Surfaces_Off, isRightSide);
            resolved.Surfaces_On = GorePlaceholderResolver.ResolveList(area.Surfaces_On, isRightSide);
            resolved.Bolts_Off = GorePlaceholderResolver.ResolveList(area.Bolts_Off, isRightSide);
            resolved.Children = area.Children != null ? new List<string>(area.Children) : new List<string>();

            if (area.Chunk != null)
            {
                resolved.Chunk = new GoreChunk
                {
                    root = GorePlaceholderResolver.Resolve(area.Chunk.root, isRightSide),
                    bone = GorePlaceholderResolver.Resolve(area.Chunk.bone, isRightSide),
                    MinForce = area.Chunk.MinForce,
                    MaxForce = area.Chunk.MaxForce,
                    Surfaces_On = GorePlaceholderResolver.ResolveList(area.Chunk.Surfaces_On, isRightSide),
                    Surfaces = GorePlaceholderResolver.ResolveList(area.Chunk.Surfaces, isRightSide),
                    Children_Off = GorePlaceholderResolver.ResolveList(area.Chunk.Children_Off, isRightSide)
                };
            }

            if (area.BoltOn != null)
            {
                resolved.BoltOn = new GoreBoltOn
                {
                    Name = area.BoltOn.Name,
                    Bolt = GorePlaceholderResolver.Resolve(area.BoltOn.Bolt, isRightSide)
                };
            }

            if (area.FX != null && area.FX.Count > 0)
            {
                resolved.FX = new List<GoreEffect>(area.FX.Count);
                foreach (GoreEffect fx in area.FX)
                {
                    resolved.FX.Add(new GoreEffect
                    {
                        Name = fx.Name,
                        Bolt = GorePlaceholderResolver.Resolve(fx.Bolt, isRightSide),
                        File = fx.File
                    });
                }
            }

            return resolved;
        }

        /// <summary>
        /// Deaktiviert Surface-GameObjects deren Name mit einem der Surface-Namen uebereinstimmt.
        /// Nutzt SetActive(false) konsistent mit PlayerSkinApplier.
        /// </summary>
        private void DeactivateSurfaces(Renderer[] allRenderers, List<string> surfaceNames)
        {
            if (surfaceNames == null || surfaceNames.Count == 0)
            {
                return;
            }

            foreach (string surfaceName in surfaceNames)
            {
                if (string.IsNullOrEmpty(surfaceName))
                {
                    continue;
                }

                bool found = false;
                foreach (Renderer renderer in allRenderers)
                {
                    if (MatchesSurface(renderer, surfaceName))
                    {
                        renderer.gameObject.SetActive(false);
                        found = true;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning($"[GoreApplier] Surface not found for deactivation: {surfaceName}");
                }
            }
        }

        /// <summary>
        /// Aktiviert Surface-GameObjects deren Name mit einem der Surface-Namen uebereinstimmt.
        /// Nutzt SetActive(true) konsistent mit PlayerSkinApplier.
        /// </summary>
        private void ActivateSurfaces(Renderer[] allRenderers, List<string> surfaceNames)
        {
            if (surfaceNames == null || surfaceNames.Count == 0)
            {
                return;
            }

            foreach (string surfaceName in surfaceNames)
            {
                if (string.IsNullOrEmpty(surfaceName))
                {
                    continue;
                }

                bool found = false;
                foreach (Renderer renderer in allRenderers)
                {
                    if (MatchesSurface(renderer, surfaceName))
                    {
                        renderer.gameObject.SetActive(true);
                        found = true;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning($"[GoreApplier] Surface not found for activation: {surfaceName}");
                }
            }
        }

        /// <summary>
        /// Prueft ob ein Renderer-GameObject-Name mit einem SoF2-Surface-Namen uebereinstimmt.
        /// Beruecksichtigt Unity-Suffixe (_0, _1, etc.) und case-insensitive Vergleich.
        /// </summary>
        private bool MatchesSurface(Renderer renderer, string surfaceName)
        {
            string rendererName = renderer.gameObject.name;

            // Exakter Match
            if (string.Equals(rendererName, surfaceName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Match ohne numerischen Suffix (Unity haengt _0, _1 etc. an)
            string cleanName = Regex.Replace(rendererName, @"_\d+$", "");
            if (string.Equals(cleanName, surfaceName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Laedt alle Gore-Piece-Prefabs via PrefabManager in den Cache (Cache-Warming).
        /// Es werden keine Instanzen erstellt — BoltOn-Pieces werden erst bei Dismemberment
        /// live instanziiert (1:1 SoF2-Verhalten).
        /// </summary>
        /// <param name="goreDataLoader">Loader fuer Gore-Piece-Definitionen.</param>
        public void PreloadGorePieceCache(GoreDataLoader goreDataLoader)
        {
            if (goreDataLoader == null)
            {
                return;
            }

            PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
            if (prefabManager == null)
            {
                Debug.LogWarning("[GoreApplier] PrefabManager not available for gore piece preloading.");
                return;
            }

            List<GorePiece> allPieces = goreDataLoader.GetAllPieces();
            int loadedCount = 0;
            foreach (GorePiece piece in allPieces)
            {
                if (string.IsNullOrEmpty(piece.modelName))
                {
                    continue;
                }

                // Lade Prefab via Addressables nur in den Cache (kein Instantiate)
                try
                {
                    GameObject piecePrefab = prefabManager.LoadPrefab<GameObject>(piece.modelName);
                    if (piecePrefab != null)
                    {
                        loadedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"[GoreApplier] Gore piece prefab not found: {piece.modelName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GoreApplier] Failed to preload gore piece '{piece.name}' with key '{piece.modelName}': {ex.Message}");
                }
            }

            Debug.Log($"[GoreApplier] Cache-warmed {loadedCount}/{allPieces.Count} gore piece prefabs.");
        }

        /// <summary>
        /// Spawnt ein abgetrenntes Chunk-Teil als eigenstaendiges GameObject mit Rigidbody und Force.
        /// 1:1 SoF2-Ansatz: Der Chunk wird aus Kopien der Charakter-Renderer erstellt (nicht aus separaten Prefabs).
        /// GHOUL2 hat die Surfaces des Spielermodells dupliziert und als fliegendes Teil mit Physik versehen.
        /// </summary>
        private void SpawnChunk(GameObject characterRoot, Renderer[] allRenderers, GoreChunk chunk, Vector3 hitDirection)
        {
            if (string.IsNullOrEmpty(chunk.root))
            {
                return;
            }

            // Finde den Bone-Transform fuer den Chunk-Ursprung
            Transform boneTransform = FindBoneTransform(characterRoot, chunk.root);
            if (boneTransform == null)
            {
                if (!string.IsNullOrEmpty(chunk.bone))
                {
                    boneTransform = FindBoneTransform(characterRoot, chunk.bone);
                }

                if (boneTransform == null)
                {
                    Debug.LogWarning($"[GoreApplier] Chunk bone not found: {chunk.root} / {chunk.bone}");
                    return;
                }
            }

            // Chunk.Surfaces_On am Charakter aktivieren (Cap-Surfaces auf dem abgetrennten Stueck)
            if (chunk.Surfaces_On != null && chunk.Surfaces_On.Count > 0)
            {
                ActivateSurfaces(allRenderers, chunk.Surfaces_On);
            }

            // Chunk.Children_Off verarbeiten — bei hip-Abtrennung z.B. leg_upper
            if (chunk.Children_Off != null && chunk.Children_Off.Count > 0)
            {
                foreach (string childOff in chunk.Children_Off)
                {
                    DeactivateSurfaces(allRenderers, new List<string> { childOff });
                }
            }

            // Erstelle das fliegende Chunk-GameObject
            GameObject chunkObj = new($"GoreChunk_{chunk.root}");
            chunkObj.transform.position = boneTransform.position;
            chunkObj.transform.rotation = boneTransform.rotation;

            // 1:1 SoF2: Klone die Renderer des Charakters die zu diesem Chunk gehoeren.
            // In GHOUL2 wurde das gleiche Modell dupliziert und nur die Chunk-Surfaces sichtbar gelassen.
            // Hier klonen wir die bereits deaktivierten Renderer (Surfaces_Off) des Hauptmodells.
            CloneChunkRenderers(allRenderers, chunk, chunkObj.transform);

            // Rigidbody mit zufaelliger Force hinzufuegen
            Rigidbody rb = chunkObj.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            float force = UnityEngine.Random.Range(chunk.MinForce, chunk.MaxForce);
            Vector3 forceDirection = hitDirection.normalized;
            if (forceDirection == Vector3.zero)
            {
                forceDirection = UnityEngine.Random.onUnitSphere;
            }

            forceDirection += UnityEngine.Random.insideUnitSphere * 0.3f;
            forceDirection.y = Mathf.Abs(forceDirection.y) + 0.2f;
            rb.AddForce(forceDirection.normalized * force, ForceMode.Impulse);

            rb.AddTorque(UnityEngine.Random.insideUnitSphere * force * 0.5f, ForceMode.Impulse);

            BoxCollider collider = chunkObj.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 0.15f;

            UnityEngine.Object.Destroy(chunkObj, CHUNK_LIFETIME);
        }

        /// <summary>
        /// Klont die Renderer des Charakters, die zu den Surfaces_Off des Chunks gehoeren,
        /// und befestigt sie als Kinder des Chunk-GameObjects.
        /// Entspricht dem SoF2 GHOUL2-Verfahren: Das Charakter-Mesh wird dupliziert und
        /// nur die abgetrennten Surfaces bleiben sichtbar.
        /// </summary>
        private void CloneChunkRenderers(Renderer[] allRenderers, GoreChunk chunk, Transform chunkParent)
        {
            // Wenn chunk.Surfaces gesetzt ist, nutze die explizite Surfaceliste fuer Multi-Surface-Chunks
            // (z.B. Kopf mit >12 Surfaces). Sonst Fallback auf chunk.root Matching.
            bool useSurfaceList = chunk.Surfaces != null && chunk.Surfaces.Count > 0;

            // Sammle alle deaktivierten Renderer die zum Chunk gehoeren
            // Diese wurden bereits durch DeactivateSurfaces(area.Surfaces_Off) deaktiviert
            foreach (Renderer renderer in allRenderers)
            {
                // Nur deaktivierte Renderer klonen (die gerade abgetrennt wurden)
                if (renderer.gameObject.activeSelf)
                {
                    continue;
                }

                // Pruefe ob dieser Renderer zu den Chunk-Surfaces gehoert
                bool matches = false;
                if (useSurfaceList)
                {
                    foreach (string surfaceName in chunk.Surfaces)
                    {
                        if (MatchesSurface(renderer, surfaceName))
                        {
                            matches = true;
                            break;
                        }
                    }
                }
                else
                {
                    matches = MatchesSurface(renderer, chunk.root);
                }

                if (!matches)
                {
                    continue;
                }

                // MeshRenderer/SkinnedMeshRenderer klonen
                if (renderer is MeshRenderer meshRenderer)
                {
                    CloneMeshRenderer(meshRenderer, chunkParent);
                }
                else if (renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    CloneSkinnedMeshRenderer(skinnedRenderer, chunkParent);
                }
            }
        }

        /// <summary>
        /// Klont einen MeshRenderer mit seinem MeshFilter in den Chunk.
        /// </summary>
        private void CloneMeshRenderer(MeshRenderer source, Transform chunkParent)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                return;
            }

            GameObject clone = new($"Chunk_{source.gameObject.name}");
            clone.transform.SetParent(chunkParent, false);
            clone.transform.localPosition = source.transform.position - chunkParent.position;
            clone.transform.localRotation = source.transform.rotation;

            MeshFilter cloneFilter = clone.AddComponent<MeshFilter>();
            cloneFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer cloneRenderer = clone.AddComponent<MeshRenderer>();
            cloneRenderer.sharedMaterials = source.sharedMaterials;
            clone.SetActive(true);
        }

        /// <summary>
        /// Klont einen SkinnedMeshRenderer: baked das Mesh in eine statische Pose
        /// und erstellt einen MeshRenderer-Chunk daraus.
        /// </summary>
        private void CloneSkinnedMeshRenderer(SkinnedMeshRenderer source, Transform chunkParent)
        {
            Mesh bakedMesh = new();
            source.BakeMesh(bakedMesh);

            GameObject clone = new($"Chunk_{source.gameObject.name}");
            clone.transform.SetParent(chunkParent, false);
            clone.transform.localPosition = source.transform.position - chunkParent.position;
            clone.transform.localRotation = source.transform.rotation;

            MeshFilter cloneFilter = clone.AddComponent<MeshFilter>();
            cloneFilter.sharedMesh = bakedMesh;

            MeshRenderer cloneRenderer = clone.AddComponent<MeshRenderer>();
            cloneRenderer.sharedMaterials = source.sharedMaterials;
            clone.SetActive(true);
        }

        /// <summary>
        /// Befestigt ein BoltOn-Modell (Knochen, Gehirn) am Stumpf des Charakters.
        /// Laedt das Piece live via PrefabManager (cache-first durch PreloadGorePieceCache)
        /// und instanziiert es am Bolt-Transform. 1:1 SoF2-Verhalten: BoltOns erscheinen
        /// nur bei Dismemberment, nicht vorher.
        /// </summary>
        private void SpawnBoltOn(GameObject characterRoot, GoreBoltOn boltOn, GoreDataLoader goreDataLoader)
        {
            if (string.IsNullOrEmpty(boltOn.Name) || string.IsNullOrEmpty(boltOn.Bolt))
            {
                return;
            }

            // Finde den Bolt-Transform (z.B. *headg, *bicep_rg, *shldr_rg)
            Transform boltTransform = FindBoltTransform(characterRoot, boltOn.Bolt);
            if (boltTransform == null)
            {
                Debug.LogWarning($"[GoreApplier] Bolt transform not found: {boltOn.Bolt}");
                return;
            }

            // Lade Gore-Piece via PrefabManager (cache-first)
            GorePiece piece = goreDataLoader.GetPieceByName(boltOn.Name);
            if (piece == null || string.IsNullOrEmpty(piece.modelName))
            {
                Debug.LogWarning($"[GoreApplier] Gore piece definition not found: {boltOn.Name}");
                return;
            }

            PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
            if (prefabManager == null)
            {
                return;
            }

            GameObject piecePrefab;
            try
            {
                piecePrefab = prefabManager.LoadPrefab<GameObject>(piece.modelName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GoreApplier] BoltOn prefab not found with key '{piece.modelName}': {ex.Message}");
                return;
            }

            if (piecePrefab == null)
            {
                return;
            }

            GameObject boltOnInstance = UnityEngine.Object.Instantiate(piecePrefab, boltTransform);
            boltOnInstance.name = $"GoreBoltOn_{boltOn.Name}";
            boltOnInstance.transform.localPosition = Vector3.zero;
            boltOnInstance.transform.localRotation = Quaternion.identity;
            boltOnInstance.transform.localScale = Vector3.one * BOLT_ON_SCALE;
        }

        /// <summary>
        /// Spielt Gore-Effekte an den definierten Bolt-Positionen ab.
        /// Nutzt den EffectFactory-Service fuer die Effekt-Instanziierung.
        /// </summary>
        private void SpawnEffects(GameObject characterRoot, List<GoreEffect> effects)
        {
            EffectFactory effectFactory = ServiceLocator.Get<EffectFactory>();

            foreach (GoreEffect fx in effects)
            {
                if (string.IsNullOrEmpty(fx.Name))
                {
                    continue;
                }

                // Bolt-Position finden
                Vector3 effectPosition = characterRoot.transform.position;
                Vector3 effectNormal = Vector3.up;

                if (!string.IsNullOrEmpty(fx.Bolt))
                {
                    Transform boltTransform = FindBoltTransform(characterRoot, fx.Bolt);
                    if (boltTransform != null)
                    {
                        effectPosition = boltTransform.position;
                        effectNormal = boltTransform.up;
                    }
                }

                // Effekt-ID aus dem Namen aufloesung:
                // gore_effects-Eintrag: Name="gore_mist_small", File="gore_mist_small.efx"
                // Die konvertierte EffectDefinition-ID basiert auf dem .efx-Dateinamen ohne Extension
                // z.B. "gore_mist_small.efx" → ID "effects/gore_mist_small" oder "gore_mist_small"
                string effectId = ResolveEffectId(fx);
                if (effectFactory != null && !string.IsNullOrEmpty(effectId))
                {
                    effectFactory.SpawnImpactEffect(effectPosition, effectNormal, effectId);
                }
                else
                {
                    Debug.LogWarning($"[GoreApplier] Could not spawn gore effect: {fx.Name} (effectId: {effectId})");
                }
            }
        }

        /// <summary>
        /// Loest die Effekt-ID aus einem GoreEffect-Eintrag auf.
        /// Probiert verschiedene ID-Formate die der EffectDataLoader verwenden koennte.
        /// </summary>
        private string ResolveEffectId(GoreEffect fx)
        {
            EffectDataLoader effectLoader = ServiceLocator.Get<EffectDataLoader>();
            if (effectLoader == null)
            {
                return null;
            }

            // Varianten probieren (verschiedene Namenskonventionen)
            string[] candidates = new string[]
            {
                fx.Name,
                $"effects/{fx.Name}",
                fx.File != null ? System.IO.Path.GetFileNameWithoutExtension(fx.File) : null,
                fx.File != null ? $"effects/{System.IO.Path.GetFileNameWithoutExtension(fx.File)}" : null,
            };

            foreach (string candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (effectLoader.GetById(candidate) != null)
                {
                    return candidate;
                }
            }

            // Kein passender Effekt gefunden — _mp-Variante probieren
            string mpName = fx.Name + "_mp";
            if (effectLoader.GetById(mpName) != null)
            {
                return mpName;
            }

            if (effectLoader.GetById($"effects/{mpName}") != null)
            {
                return $"effects/{mpName}";
            }

            return fx.Name;
        }

        /// <summary>
        /// Findet einen Bone-Transform anhand eines SoF2-Surface/Bone-Namens.
        /// Sucht rekursiv durch die Hierarchie des Charakter-GameObjects.
        /// </summary>
        private Transform FindBoneTransform(GameObject root, string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
            {
                return null;
            }

            // Rekursive Suche in der Hierarchie
            return FindTransformRecursive(root.transform, boneName);
        }

        /// <summary>
        /// Findet einen Bolt-Transform anhand eines SoF2-Bolt-Namens.
        /// SoF2 Bolt-Namen beginnen mit '*' (z.B. "*headg", "*hand_rg").
        /// In Unity suchen wir nach dem normalisierten Namen.
        /// </summary>
        private Transform FindBoltTransform(GameObject root, string boltName)
        {
            if (string.IsNullOrEmpty(boltName))
            {
                return null;
            }

            string cleanName = NormalizeTransformName(boltName);
            return FindTransformRecursive(root.transform, cleanName);
        }

        /// <summary>
        /// Normalisiert einen Transform-/Bone-Namen: entfernt '*'-Praefix und Unity-Suffix ('_0', '_1' etc.).
        /// Beispiele: "*headg_0" → "headg", "hand_r_0" → "hand_r", "*bicep_rg" → "bicep_rg".
        /// </summary>
        private string NormalizeTransformName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            string normalized = name.TrimStart('*');
            normalized = Regex.Replace(normalized, @"_\d+$", "");
            return normalized;
        }

        /// <summary>
        /// Sucht rekursiv nach einem Kind-Transform mit dem gegebenen Namen (case-insensitive).
        /// Normalisiert sowohl den Suchbegriff als auch die Transform-Namen
        /// (entfernt '*'-Praefix und '_N'-Suffix fuer robusten Vergleich).
        /// </summary>
        private Transform FindTransformRecursive(Transform parent, string name)
        {
            // Exakter Match
            if (string.Equals(parent.name, name, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            // Normalisierter Match: '*'-Praefix und '_N'-Suffix entfernen
            string cleanParentName = NormalizeTransformName(parent.name);
            if (string.Equals(cleanParentName, name, StringComparison.OrdinalIgnoreCase))
            {
                return parent;
            }

            // Rekursiv durch Kinder suchen
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindTransformRecursive(parent.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Loest den tatsaechlichen Visual-Model-Root des Charakters auf.
        /// SoF2-Surfaces und *Bolts leben unter model_root_0 in der Hierarchie:
        /// PlayerCharacter → VisualRoot → skinClone → model_root_0.
        /// Faellt auf characterRoot zurueck falls model_root nicht gefunden wird.
        /// </summary>
        private Transform ResolveCharacterModelRoot(GameObject characterRoot)
        {
            Transform modelRoot = FindTransformRecursive(characterRoot.transform, "model_root");
            if (modelRoot != null)
            {
                return modelRoot;
            }

            Debug.LogWarning("[GoreApplier] model_root not found in character hierarchy, using characterRoot as fallback.");
            return characterRoot.transform;
        }
    }
}
