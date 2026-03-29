using System.Collections.Generic;
using UnityEngine;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Bone-basiertes Hit-Detection-System mit BoxCollidern auf allen Skeleton-Bones.
    /// Jeder relevanter Knochen bekommt einen Trigger-BoxCollider auf dem Hitbox-Layer
    /// mit HitboxCollider-Komponente fuer Region-spezifische Schadenserkennung.
    /// 28 BoxCollider auf allen Bones des skeleton_root fuer maximale Praezision:
    /// Mehrere Collider pro Glied (z.B. lhumerus + lhumerusX fuer Oberarm) geben
    /// eine deutlich bessere Koerperform-Annaeherung als ein einzelner Collider pro Region.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientHitboxSystem : MonoBehaviour
    {
        private const string HITBOX_LAYER_NAME = "Hitbox";

        [Header("Visual Hitbox Debug")]
        [SerializeField]
        private bool m_ShowVisualHitboxes;

        [SerializeField]
        [Range(0.05f, 1f)]
        private float m_VisualAlpha = 0.3f;

        /// <summary>Alle erstellten Hitbox-GameObjects fuer Cleanup.</summary>
        private List<GameObject> m_HitboxObjects;

        /// <summary>Mapping von HitRegion auf alle zugehoerigen Collider fuer Dismemberment.</summary>
        private Dictionary<HitRegion, List<Collider>> m_RegionColliders;

        /// <summary>Debug-Visualisierungs-GameObjects fuer Cleanup.</summary>
        private List<GameObject> m_DebugVisuals;

        /// <summary>HitRegions die durch Dismemberment deaktiviert wurden.</summary>
        private HashSet<HitRegion> m_DismemberedRegions = new();

        /// <summary>Flag ob System bereits initialisiert wurde.</summary>
        private bool m_IsInitialized;

        /// <summary>Ob das System erfolgreich initialisiert wurde.</summary>
        public bool IsInitialized => m_IsInitialized;

        /// <summary>
        /// Erstellt Trigger-BoxCollider auf allen Skeleton-Bones fuer Hit-Detection.
        /// 28 BoxCollider auf dem Hitbox-Layer — mehrere pro Glied fuer bessere Praezision.
        /// </summary>
        public void BuildHitboxes(Transform visualRoot)
        {
            if (m_IsInitialized)
            {
                Debug.LogWarning("[ClientHitboxSystem] Already initialized. Call ClearHitboxes() first.");
                return;
            }

            if (visualRoot == null)
            {
                Debug.LogError("[ClientHitboxSystem] Visual Root is null.");
                return;
            }

            int hitboxLayer = LayerMask.NameToLayer(HITBOX_LAYER_NAME);
            if (hitboxLayer < 0)
            {
                Debug.LogError("[ClientHitboxSystem] Layer '" + HITBOX_LAYER_NAME + "' not found!");
                return;
            }

            m_HitboxObjects = new List<GameObject>();
            m_RegionColliders = new Dictionary<HitRegion, List<Collider>>();

            // ===================== Alle Bones aus skeleton_root finden =====================
            // Kopf / Wirbelsaeule
            Transform cranium = FindDeepChild(visualRoot, "cranium");
            Transform cervical = FindDeepChild(visualRoot, "cervical");
            Transform thoracic = FindDeepChild(visualRoot, "thoracic");
            Transform upperLumbar = FindDeepChild(visualRoot, "upper_lumbar");
            Transform lowerLumbar = FindDeepChild(visualRoot, "lower_lumbar");
            Transform pelvis = FindDeepChild(visualRoot, "pelvis");

            // Linker Arm (alle Bones der Kette)
            Transform lclavical = FindDeepChild(visualRoot, "lclavical");
            Transform lhumerus = FindDeepChild(visualRoot, "lhumerus");
            Transform lhumerusX = FindDeepChild(visualRoot, "lhumerusX");
            Transform lradius = FindDeepChild(visualRoot, "lradius");
            Transform lradiusX = FindDeepChild(visualRoot, "lradiusX");
            Transform lhand = FindDeepChild(visualRoot, "lhand");

            // Rechter Arm (alle Bones der Kette)
            Transform rclavical = FindDeepChild(visualRoot, "rclavical");
            Transform rhumerus = FindDeepChild(visualRoot, "rhumerus");
            Transform rhumerusX = FindDeepChild(visualRoot, "rhumerusX");
            Transform rradius = FindDeepChild(visualRoot, "rradius");
            Transform rradiusX = FindDeepChild(visualRoot, "rradiusX");
            Transform rhand = FindDeepChild(visualRoot, "rhand");

            // Linkes Bein (alle Bones der Kette)
            Transform lfemurYZ = FindDeepChild(visualRoot, "lfemurYZ");
            Transform lfemurX = FindDeepChild(visualRoot, "lfemurX");
            Transform ltibia = FindDeepChild(visualRoot, "ltibia");
            Transform ltalus = FindDeepChild(visualRoot, "ltalus");
            Transform ltarsal = FindDeepChild(visualRoot, "ltarsal");

            // Rechtes Bein (alle Bones der Kette)
            Transform rfemurYZ = FindDeepChild(visualRoot, "rfemurYZ");
            Transform rfemurX = FindDeepChild(visualRoot, "rfemurX");
            Transform rtibia = FindDeepChild(visualRoot, "rtibia");
            Transform rtalus = FindDeepChild(visualRoot, "rtalus");
            Transform rtarsal = FindDeepChild(visualRoot, "rtarsal");

            // ===================== Hitboxen erstellen =====================
            // 28 Trigger-BoxCollider auf dem Hitbox-Layer.
            // Default BoxCollider (1,1,1) an Bone-Origin (0,0,0) — keine Skalierung.
            //
            // === Kopf / Hals ===
            CreateBoneHitbox(hitboxLayer, cranium,      HitRegion.Head,           1.75f);
            CreateBoneHitbox(hitboxLayer, cervical,     HitRegion.Neck,           1.75f);

            // === Torso ===
            CreateBoneHitbox(hitboxLayer, thoracic,     HitRegion.Chest,          1.0f);
            CreateBoneHitbox(hitboxLayer, upperLumbar,  HitRegion.Gut,            1.0f);
            CreateBoneHitbox(hitboxLayer, lowerLumbar,  HitRegion.Groin,          1.0f);
            CreateBoneHitbox(hitboxLayer, pelvis,       HitRegion.Groin,          1.0f);

            // === Linker Arm ===
            CreateBoneHitbox(hitboxLayer, lclavical,    HitRegion.LeftShoulder,   0.7f);
            CreateBoneHitbox(hitboxLayer, lhumerus,     HitRegion.LeftArm,        0.7f);
            CreateBoneHitbox(hitboxLayer, lhumerusX,    HitRegion.LeftArm,        0.7f);
            CreateBoneHitbox(hitboxLayer, lradius,      HitRegion.LeftForearm,    0.3f);
            CreateBoneHitbox(hitboxLayer, lradiusX,     HitRegion.LeftForearm,    0.3f);
            CreateBoneHitbox(hitboxLayer, lhand,        HitRegion.LeftHand,       0.3f);

            // === Rechter Arm ===
            CreateBoneHitbox(hitboxLayer, rclavical,    HitRegion.RightShoulder,  0.7f);
            CreateBoneHitbox(hitboxLayer, rhumerus,     HitRegion.RightArm,       0.7f);
            CreateBoneHitbox(hitboxLayer, rhumerusX,    HitRegion.RightArm,       0.7f);
            CreateBoneHitbox(hitboxLayer, rradius,      HitRegion.RightForearm,   0.3f);
            CreateBoneHitbox(hitboxLayer, rradiusX,     HitRegion.RightForearm,   0.3f);
            CreateBoneHitbox(hitboxLayer, rhand,        HitRegion.RightHand,      0.3f);

            // === Linkes Bein ===
            CreateBoneHitbox(hitboxLayer, lfemurYZ,     HitRegion.LeftThigh,      0.7f);
            CreateBoneHitbox(hitboxLayer, lfemurX,      HitRegion.LeftThigh,      0.7f);
            CreateBoneHitbox(hitboxLayer, ltibia,       HitRegion.LeftLeg,        0.7f);
            CreateBoneHitbox(hitboxLayer, ltalus,       HitRegion.LeftFoot,       0.4f);
            CreateBoneHitbox(hitboxLayer, ltarsal,      HitRegion.LeftFoot,       0.4f);

            // === Rechtes Bein ===
            CreateBoneHitbox(hitboxLayer, rfemurYZ,     HitRegion.RightThigh,     0.7f);
            CreateBoneHitbox(hitboxLayer, rfemurX,      HitRegion.RightThigh,     0.7f);
            CreateBoneHitbox(hitboxLayer, rtibia,       HitRegion.RightLeg,       0.7f);
            CreateBoneHitbox(hitboxLayer, rtalus,       HitRegion.RightFoot,      0.4f);
            CreateBoneHitbox(hitboxLayer, rtarsal,      HitRegion.RightFoot,      0.4f);

            if (m_ShowVisualHitboxes)
            {
                CreateVisualDebug();
            }

            m_IsInitialized = true;
            Debug.Log($"[ClientHitboxSystem] {m_HitboxObjects.Count} bone hitbox colliders created on {m_RegionColliders.Count} regions.");
        }

        /// <summary>
        /// Erstellt einen Trigger-BoxCollider auf dem angegebenen Bone als Kind-GameObject.
        /// Default BoxCollider (1,1,1) an lokaler Position (0,0,0) — keine Skalierung/Positionierung.
        /// </summary>
        private void CreateBoneHitbox(int hitboxLayer, Transform bone, HitRegion hitRegion,
            float damageMultiplier)
        {
            if (bone == null)
            {
                return;
            }

            GameObject hitboxGO = new($"Hitbox_{hitRegion}_{bone.name}");
            hitboxGO.layer = hitboxLayer;
            hitboxGO.transform.SetParent(bone, false);
            hitboxGO.transform.localPosition = Vector3.zero;
            hitboxGO.transform.localRotation = Quaternion.identity;

            BoxCollider boxCollider = hitboxGO.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(0.1f, 0.1f, 0.1f);

            HitboxCollider hitboxComponent = hitboxGO.AddComponent<HitboxCollider>();
            hitboxComponent.Initialize(hitRegion, damageMultiplier);

            m_HitboxObjects.Add(hitboxGO);

            if (!m_RegionColliders.TryGetValue(hitRegion, out List<Collider> colliders))
            {
                colliders = new List<Collider>();
                m_RegionColliders[hitRegion] = colliders;
            }

            colliders.Add(boxCollider);
        }

        // ===================================================================
        // Hitbox-Verwaltung
        // ===================================================================

        /// <summary>
        /// Entfernt alle Hitbox-GameObjects, Debug-Visualisierungen und setzt alles zurueck.
        /// </summary>
        public void ClearHitboxes()
        {
            if (m_HitboxObjects != null)
            {
                for (int i = 0; i < m_HitboxObjects.Count; i++)
                {
                    if (m_HitboxObjects[i] != null)
                    {
                        Destroy(m_HitboxObjects[i]);
                    }
                }

                m_HitboxObjects = null;
            }

            if (m_DebugVisuals != null)
            {
                for (int i = 0; i < m_DebugVisuals.Count; i++)
                {
                    if (m_DebugVisuals[i] != null)
                    {
                        Destroy(m_DebugVisuals[i]);
                    }
                }

                m_DebugVisuals = null;
            }

            if (m_RegionColliders != null)
            {
                m_RegionColliders.Clear();
                m_RegionColliders = null;
            }

            m_DismemberedRegions.Clear();
            m_IsInitialized = false;
        }

        /// <summary>
        /// Aktiviert oder deaktiviert alle Hitbox-Collider.
        /// Wird fuer Self-Hit-Vermeidung beim Schiessen verwendet.
        /// </summary>
        public void SetHitboxesEnabled(bool enabled)
        {
            if (m_HitboxObjects == null)
            {
                return;
            }

            for (int i = 0; i < m_HitboxObjects.Count; i++)
            {
                if (m_HitboxObjects[i] != null && m_HitboxObjects[i].TryGetComponent<Collider>(out Collider col))
                {
                    col.enabled = enabled;
                }
            }
        }

        /// <summary>
        /// Markiert eine HitRegion als dismembered und deaktiviert ALLE zugehoerigen Collider.
        /// </summary>
        public void DisableHitboxForRegion(HitRegion region)
        {
            m_DismemberedRegions.Add(region);

            if (m_RegionColliders != null && m_RegionColliders.TryGetValue(region, out List<Collider> colliders))
            {
                for (int i = 0; i < colliders.Count; i++)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].enabled = false;
                    }
                }
            }

            Debug.Log($"[ClientHitboxSystem] Region dismembered: {region}");
        }

        private void OnDestroy()
        {
            ClearHitboxes();
        }

        // ===================================================================
        // Dismemberment — Region + Kinder-Kaskade
        // ===================================================================

        /// <summary>
        /// Markiert die angegebene HitRegion UND alle anatomisch untergeordneten Regionen als dismembered.
        /// </summary>
        public void DisableHitboxForRegionAndChildren(HitRegion region)
        {
            DisableHitboxForRegion(region);

            HitRegion[] children = GetChildRegions(region);
            for (int i = 0; i < children.Length; i++)
            {
                DisableHitboxForRegion(children[i]);
            }
        }

        /// <summary>
        /// Gibt alle anatomisch untergeordneten HitRegions fuer eine gegebene Region zurueck.
        /// Bildet die Koerperteil-Hierarchie ab: Schulter -> Arm -> Unterarm -> Hand, etc.
        /// </summary>
        private static HitRegion[] GetChildRegions(HitRegion region)
        {
            switch (region)
            {
                case HitRegion.LeftShoulder:
                case HitRegion.LeftArm:
                    return new[] { HitRegion.LeftArm, HitRegion.LeftForearm, HitRegion.LeftHand };
                case HitRegion.LeftForearm:
                    return new[] { HitRegion.LeftHand };
                case HitRegion.RightShoulder:
                case HitRegion.RightArm:
                    return new[] { HitRegion.RightArm, HitRegion.RightForearm, HitRegion.RightHand };
                case HitRegion.RightForearm:
                    return new[] { HitRegion.RightHand };
                case HitRegion.LeftThigh:
                    return new[] { HitRegion.LeftLeg, HitRegion.LeftFoot };
                case HitRegion.LeftLeg:
                    return new[] { HitRegion.LeftFoot };
                case HitRegion.RightThigh:
                    return new[] { HitRegion.RightLeg, HitRegion.RightFoot };
                case HitRegion.RightLeg:
                    return new[] { HitRegion.RightFoot };
                case HitRegion.Neck:
                    return new[] { HitRegion.Head };
                default:
                    return System.Array.Empty<HitRegion>();
            }
        }

        // ===================================================================
        // Helpers
        // ===================================================================

        /// <summary>
        /// Rekursive DFS-Suche nach einem Bone per Name (case-sensitive).
        /// </summary>
        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent.name == childName)
            {
                return parent;
            }

            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform result = FindDeepChild(parent.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        // ===================================================================
        // Visual Debug
        // ===================================================================

        /// <summary>
        /// Erstellt semi-transparente Debug-Cubes an allen Hitbox-Positionen
        /// mit den tatsaechlichen BoxCollider-Groessen fuer visuelle Ueberpruefung.
        /// </summary>
        private void CreateVisualDebug()
        {
            if (m_HitboxObjects == null)
            {
                return;
            }

            m_DebugVisuals = new List<GameObject>();

            for (int i = 0; i < m_HitboxObjects.Count; i++)
            {
                GameObject hitboxGO = m_HitboxObjects[i];

                if (hitboxGO == null || !hitboxGO.TryGetComponent<BoxCollider>(out BoxCollider box))
                {
                    continue;
                }

                if (!hitboxGO.TryGetComponent<HitboxCollider>(out HitboxCollider hitboxComp))
                {
                    continue;
                }

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Debug_Hitbox_{hitboxComp.HitRegion}_{hitboxGO.transform.parent.name}";

                if (cube.TryGetComponent<Collider>(out Collider cubeCol))
                {
                    Destroy(cubeCol);
                }

                cube.transform.SetParent(hitboxGO.transform, false);
                cube.transform.localPosition = box.center;
                cube.transform.localScale = box.size;

                Color regionColor = GetRegionColor(hitboxComp.HitRegion);
                regionColor.a = m_VisualAlpha;
                ApplyDebugMaterial(cube, regionColor);
                m_DebugVisuals.Add(cube);
            }
        }

        /// <summary>
        /// Wendet ein semi-transparentes URP Unlit Material auf ein Debug-Visual an.
        /// </summary>
        private static void ApplyDebugMaterial(GameObject visualGO, Color color)
        {
            if (!visualGO.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
            {
                return;
            }

            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpUnlit == null)
            {
                return;
            }

            Material material = new(urpUnlit);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");

            meshRenderer.material = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        // ===================================================================
        // Farben fuer Debug-Visualisierung
        // ===================================================================

        /// <summary>
        /// Gibt die Debug-Farbe fuer eine HitRegion zurueck, farbcodiert nach Koerperbereich.
        /// </summary>
        private static Color GetRegionColor(HitRegion hitRegion)
        {
            switch (hitRegion)
            {
                // Kopf: leuchtend Rot
                case HitRegion.Head:
                    return new Color(1f, 0.1f, 0.1f);

                // Hals: dunkles Orange
                case HitRegion.Neck:
                    return new Color(0.9f, 0.4f, 0.1f);

                // Brust: Gelb
                case HitRegion.Chest:
                    return new Color(1f, 0.9f, 0.1f);

                // Bauch: Orange-Gelb
                case HitRegion.Gut:
                    return new Color(1f, 0.65f, 0.1f);

                // Leiste: dunkles Gelb-Gruen
                case HitRegion.Groin:
                    return new Color(0.7f, 0.8f, 0.1f);

                // Schulter: mittelblau
                case HitRegion.LeftShoulder:
                case HitRegion.RightShoulder:
                    return new Color(0.3f, 0.3f, 1f);

                // Oberarm: dunkelblau
                case HitRegion.LeftArm:
                case HitRegion.RightArm:
                    return new Color(0.15f, 0.15f, 0.85f);

                // Unterarm: lila
                case HitRegion.LeftForearm:
                case HitRegion.RightForearm:
                    return new Color(0.6f, 0.2f, 0.9f);

                // Hand: magenta
                case HitRegion.LeftHand:
                case HitRegion.RightHand:
                    return new Color(1f, 0.2f, 0.8f);

                // Oberschenkel: dunkelgruen
                case HitRegion.LeftThigh:
                case HitRegion.RightThigh:
                    return new Color(0.1f, 0.7f, 0.1f);

                // Unterschenkel: hellgruen
                case HitRegion.LeftLeg:
                case HitRegion.RightLeg:
                    return new Color(0.3f, 0.9f, 0.3f);

                // Fuss: tuerkis
                case HitRegion.LeftFoot:
                case HitRegion.RightFoot:
                    return new Color(0.1f, 0.8f, 0.8f);

                default:
                    return Color.white;
            }
        }
    }
}
