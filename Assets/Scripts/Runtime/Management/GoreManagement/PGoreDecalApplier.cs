using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Projiziert PGORE-Wund-Decals auf Charakter-Meshes.
    /// SoF2-Aequivalent: trap_G2API_AddSkinGore() — projiziert Texturen via Ghoul2 auf das Skelett-Mesh.
    ///
    /// Unity-Umsetzung: Quad-basierte Decals, am naechsten Bone geparentet,
    /// mit SoF2/Decal-Shader (Alpha-Blend, Depth-Bias gegen Z-Fighting).
    ///
    /// Wachsende Decals (PGORE_KNIFE_SOAK): PGoreGrowBehaviour animiert die Skalierung
    /// von GoreScaleStartFraction auf 1.0 ueber GrowDuration Millisekunden.
    /// </summary>
    public class PGoreDecalApplier
    {
        /// <summary>Offset vom Bone-Surface weg, um Z-Fighting zu vermeiden.</summary>
        private const float DECAL_SURFACE_OFFSET = 0.002f;

        /// <summary>Standard-Lebensdauer fuer permanente Decals in Sekunden (0 = unendlich im SoF2-Sinne).</summary>
        private const float DEFAULT_LIFETIME_SECONDS = 120f;

        /// <summary>SoF2/Decal Shader Name.</summary>
        private const string SOF2_DECAL_SHADER = "SoF2/Decal";

        /// <summary>Fallback-Shader falls SoF2/Decal nicht verfuegbar.</summary>
        private const string FALLBACK_SHADER = "SoF2/EffectParticle";

        /// <summary>URP Particle Shader als letzter Fallback.</summary>
        private const string URP_FALLBACK_SHADER = "Universal Render Pipeline/Particles/Unlit";

        /// <summary>Gecachter Decal-Shader.</summary>
        private static Shader s_CachedDecalShader;

        /// <summary>Material-Cache (Texturpfad → Material) um Duplikate zu vermeiden.</summary>
        private readonly Dictionary<string, Material> m_MaterialCache = new();

        /// <summary>
        /// Wendet eine Liste von PGoreData-Eintraegen auf einen Charakter an.
        /// Fuer jeden Eintrag wird ein Decal-Quad am naechsten Bone erstellt.
        /// </summary>
        /// <param name="goreEntries">Liste der PGORE-Eintraege aus PGoreWeaponDispatch.</param>
        /// <param name="characterRoot">Root-GameObject des getroffenen Charakters.</param>
        public void ApplyGoreDecals(List<PGoreData> goreEntries, GameObject characterRoot)
        {
            if (goreEntries == null || goreEntries.Count == 0 || characterRoot == null)
            {
                return;
            }

            // Alle Bones des Charakters einmalig sammeln
            Transform[] bones = characterRoot.GetComponentsInChildren<Transform>();
            if (bones.Length == 0)
            {
                return;
            }

            foreach (PGoreData entry in goreEntries)
            {
                SpawnGoreDecal(entry, characterRoot, bones);
            }
        }

        /// <summary>
        /// Erstellt ein einzelnes Gore-Decal fuer einen PGoreData-Eintrag.
        /// Findet den naechsten Bone, erstellt ein Quad mit dem passenden Material,
        /// und konfiguriert Wachstum/Lebensdauer.
        /// </summary>
        private void SpawnGoreDecal(PGoreData data, GameObject characterRoot, Transform[] bones)
        {
            // Naechsten Bone zum Trefferpunkt finden
            Transform nearestBone = FindNearestBone(data.HitLocation, bones);
            if (nearestBone == null)
            {
                return;
            }

            // Texturpfad aus dem Shader-Mapping holen
            string texturePath = PGoreShaderMapping.GetTexturePath(data.GoreType);

            // Material erstellen/cachen
            Material material = GetOrCreateMaterial(texturePath);
            if (material == null)
            {
                return;
            }

            // Quad-Decal erstellen
            GameObject decalObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decalObj.name = $"PGore_{data.GoreType}";

            // Collider entfernen (nur visuell)
            Collider col = decalObj.GetComponent<Collider>();
            if (col != null)
            {
                Object.Destroy(col);
            }

            // Am Bone parenten damit das Decal dem Skelett folgt
            decalObj.transform.SetParent(nearestBone, true);

            // Position: Trefferpunkt vom Hitbox-Collider Richtung Bone-Zentrum verschieben,
            // damit das Decal naeher an der visuellen Mesh-Oberflaeche liegt statt am Hitbox-Rand.
            // Der Hitbox-Collider umschliesst das Mesh mit Abstand — ohne Korrektur schwebt das Decal.
            Vector3 toBone = nearestBone.position - data.HitLocation;
            float distToBone = toBone.magnitude;
            // 30% des Weges zum Bone zuruecklegen: nah genug an der Mesh-Oberflaeche,
            // ohne durch sie hindurchzugehen (Bones sitzen im Inneren des Meshes).
            float pushFraction = distToBone > 0.001f ? 0.3f : 0f;
            Vector3 meshProjection = data.HitLocation + toBone * pushFraction;
            // Minimaler Offset entlang der Schussrichtung (gegen Z-Fighting)
            Vector3 surfaceOffset = data.RayDirection.normalized * -DECAL_SURFACE_OFFSET;
            decalObj.transform.position = meshProjection + surfaceOffset;

            // Rotation: Quad-Normale (-Z) zeigt entgegen der Schussrichtung,
            // Theta-Rotation aus SoF2 wird als Z-Achsen-Roll angewendet
            Vector3 forward = data.RayDirection.normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) < 0.99f
                ? Vector3.up
                : Vector3.forward;
            decalObj.transform.rotation = Quaternion.LookRotation(forward, up)
                * Quaternion.Euler(0f, 0f, data.Theta * Mathf.Rad2Deg);

            // Groesse: SSize × TSize (bereits in Metern dank SOF2_UNIT_SCALE)
            bool isGrowing = data.GrowDuration > 0;
            float startScaleS = isGrowing ? data.SSize * data.GoreScaleStartFraction : data.SSize;
            float startScaleT = isGrowing ? data.TSize * data.GoreScaleStartFraction : data.TSize;
            decalObj.transform.localScale = new Vector3(startScaleS, startScaleT, 1f);

            // Material zuweisen
            Renderer renderer = decalObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // Wachsende Decals: GrowBehaviour hinzufuegen
            if (isGrowing)
            {
                PGoreGrowBehaviour growBehaviour = decalObj.AddComponent<PGoreGrowBehaviour>();
                growBehaviour.Initialize(
                    new Vector3(data.SSize, data.TSize, 1f),
                    data.GrowDuration / 1000f);
            }

            // Lebensdauer: 0 = SoF2 "permanent" → DEFAULT_LIFETIME_SECONDS
            float lifetime = data.LifeTime > 0
                ? data.LifeTime / 1000f
                : DEFAULT_LIFETIME_SECONDS;
            Object.Destroy(decalObj, lifetime);
        }

        /// <summary>
        /// Findet den naechsten Bone (Transform) zum angegebenen Weltpunkt.
        /// </summary>
        private Transform FindNearestBone(Vector3 worldPoint, Transform[] bones)
        {
            Transform nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Transform bone in bones)
            {
                float dist = Vector3.SqrMagnitude(bone.position - worldPoint);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = bone;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Erstellt oder cached ein Material fuer den angegebenen Gore-Texturpfad.
        /// Nutzt den SoF2/Decal-Shader mit Alpha-Blending und Depth-Bias.
        /// </summary>
        private Material GetOrCreateMaterial(string texturePath)
        {
            string cacheKey = "pgore_" + (texturePath ?? "");

            if (m_MaterialCache.TryGetValue(cacheKey, out Material cached))
            {
                return cached;
            }

            if (s_CachedDecalShader == null)
            {
                s_CachedDecalShader = Shader.Find(SOF2_DECAL_SHADER);
            }

            Shader shader = s_CachedDecalShader;
            if (shader == null)
            {
                shader = Shader.Find(FALLBACK_SHADER) ?? Shader.Find(URP_FALLBACK_SHADER);
            }

            if (shader == null)
            {
                Debug.LogWarning("[PGoreDecalApplier] No suitable shader found for gore decals.");
                return null;
            }

            Material material = new(shader) { name = $"PGore_{texturePath}" };

            // Cull Off: Wund-Decals muessen von beiden Seiten sichtbar sein
            // (Quad hat nur eine Face-Richtung, aber Spieler sehen den Charakter aus allen Winkeln)
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            // Textur laden via TextureManager
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager != null && !string.IsNullOrEmpty(texturePath))
            {
                TextureData textureData = textureManager.GetTextureData(texturePath);
                if (textureData != null && textureData.HasTexture())
                {
                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", textureData.Texture);
                    }
                    else
                    {
                        material.mainTexture = textureData.Texture;
                    }
                }
                else
                {
                    Debug.LogWarning($"[PGoreDecalApplier] Gore texture not found: {texturePath}");
                }
            }

            m_MaterialCache[cacheKey] = material;
            return material;
        }

        /// <summary>
        /// Cache leeren (z.B. bei Scene-Wechsel oder ServiceLocator.ClearAll).
        /// </summary>
        public void ClearCache()
        {
            foreach (Material mat in m_MaterialCache.Values)
            {
                if (mat != null)
                {
                    Object.Destroy(mat);
                }
            }
            m_MaterialCache.Clear();
        }
    }
}
