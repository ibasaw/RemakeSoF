using UnityEngine;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Erstellt 17 per-bone BoxCollider als Hitboxen basierend auf SoF2 Hit Regions.
    /// Collider-Groessen werden automatisch aus Bone-zu-Bone Distanzen berechnet.
    /// Jeder Collider erhaelt eine HitboxCollider-Komponente fuer Region-Identifikation.
    /// Optional: Visual Debug zeichnet semi-transparente Boxen farbcodiert nach Region-Typ.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientHitboxSystem : MonoBehaviour
    {
        /// <summary>Physics Layer fuer Hitbox-Collider (muss in Unity Layer Settings existieren).</summary>
        private const string HITBOX_LAYER_NAME = "Hitbox";

        /// <summary>Fallback-Layer falls "Hitbox" nicht konfiguriert ist.</summary>
        private const int DEFAULT_LAYER = 0;

        /// <summary>Anzahl der SoF2 Hit Regions.</summary>
        private const int HITBOX_COUNT = 17;

        [Header("Visual Hitbox Debug")]
        [SerializeField]
        private bool m_ShowVisualHitboxes;

        [SerializeField]
        [Range(0.05f, 1f)]
        private float m_VisualAlpha = 0.3f;

        /// <summary>Array aller erstellten Hitbox-GameObjects fuer Cleanup.</summary>
        private GameObject[] m_HitboxObjects;

        /// <summary>Flag ob Hitboxen bereits erstellt wurden.</summary>
        private bool m_IsInitialized;

        /// <summary>Ob Hitboxen erfolgreich erstellt wurden.</summary>
        public bool IsInitialized => m_IsInitialized;

        /// <summary>
        /// Erstellt alle 17 Hitbox-Collider aus den Bones des Character-Visuals.
        /// Muss nach Visual-Instantiation aufgerufen werden wenn alle Bones verfuegbar sind.
        /// </summary>
        public void BuildHitboxes(Transform visualRoot)
        {
            if (m_IsInitialized)
            {
                Debug.LogWarning("[ClientHitboxSystem] Hitboxen bereits erstellt. ClearHitboxes() zuerst aufrufen.");
                return;
            }

            if (visualRoot == null)
            {
                Debug.LogError("[ClientHitboxSystem] Visual Root ist null — kann keine Hitboxen erstellen.");
                return;
            }

            int hitboxLayer = LayerMask.NameToLayer(HITBOX_LAYER_NAME);
            if (hitboxLayer == -1)
            {
                Debug.LogWarning($"[ClientHitboxSystem] Layer '{HITBOX_LAYER_NAME}' nicht gefunden. Erstelle Layer in Edit > Project Settings > Tags and Layers. Verwende Default-Layer.");
                hitboxLayer = DEFAULT_LAYER;
            }

            // Alle benoetigten Bones finden
            Transform cranium = FindDeepChild(visualRoot, "cranium");
            Transform cervical = FindDeepChild(visualRoot, "cervical");
            Transform thoracic = FindDeepChild(visualRoot, "thoracic");
            Transform upperLumbar = FindDeepChild(visualRoot, "upper_lumbar");
            Transform lowerLumbar = FindDeepChild(visualRoot, "lower_lumbar");
            Transform pelvis = FindDeepChild(visualRoot, "pelvis");

            Transform lclavical = FindDeepChild(visualRoot, "lclavical");
            Transform lhumerus = FindDeepChild(visualRoot, "lhumerus");
            Transform lradius = FindDeepChild(visualRoot, "lradius");
            Transform lhand = FindDeepChild(visualRoot, "lhand");

            Transform rclavical = FindDeepChild(visualRoot, "rclavical");
            Transform rhumerus = FindDeepChild(visualRoot, "rhumerus");
            Transform rradius = FindDeepChild(visualRoot, "rradius");
            Transform rhand = FindDeepChild(visualRoot, "rhand");

            Transform lfemurYZ = FindDeepChild(visualRoot, "lfemurYZ");
            Transform ltibia = FindDeepChild(visualRoot, "ltibia");
            Transform ltarsal = FindDeepChild(visualRoot, "ltarsal");

            Transform rfemurYZ = FindDeepChild(visualRoot, "rfemurYZ");
            Transform rtibia = FindDeepChild(visualRoot, "rtibia");
            Transform rtarsal = FindDeepChild(visualRoot, "rtarsal");

            m_HitboxObjects = new GameObject[HITBOX_COUNT];
            int index = 0;

            // --- Kopf / Hals / Torso ---
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_Head", cranium, cervical, HitRegion.Head, 1.75f, 0.55f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_Neck", cervical, thoracic, HitRegion.Neck, 1.75f, 0.35f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_Chest", thoracic, upperLumbar, HitRegion.Chest, 1.0f, 0.8f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_Gut", upperLumbar, lowerLumbar, HitRegion.Gut, 1.0f, 0.7f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_Groin", lowerLumbar, pelvis, HitRegion.Groin, 1.0f, 0.6f, hitboxLayer);

            // --- Linker Arm ---
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_LeftShoulder", lclavical, lhumerus, HitRegion.LeftShoulder, 0.7f, 0.35f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_LeftArm", lhumerus, lradius, HitRegion.LeftArm, 0.7f, 0.3f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_LeftHand", lradius, lhand, HitRegion.LeftHand, 0.3f, 0.25f, hitboxLayer);

            // --- Rechter Arm ---
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_RightShoulder", rclavical, rhumerus, HitRegion.RightShoulder, 0.7f, 0.35f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_RightArm", rhumerus, rradius, HitRegion.RightArm, 0.7f, 0.3f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_RightHand", rradius, rhand, HitRegion.RightHand, 0.3f, 0.25f, hitboxLayer);

            // --- Linkes Bein ---
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_LeftThigh", lfemurYZ, ltibia, HitRegion.LeftThigh, 0.7f, 0.4f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_LeftLeg", ltibia, ltarsal, HitRegion.LeftLeg, 0.7f, 0.3f, hitboxLayer);
            m_HitboxObjects[index++] = CreateEndBoneHitbox("Hitbox_LeftFoot", ltarsal, ltibia, HitRegion.LeftFoot, 0.4f, 0.25f, 0.35f, hitboxLayer);

            // --- Rechtes Bein ---
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_RightThigh", rfemurYZ, rtibia, HitRegion.RightThigh, 0.7f, 0.4f, hitboxLayer);
            m_HitboxObjects[index++] = CreateBoneHitbox("Hitbox_RightLeg", rtibia, rtarsal, HitRegion.RightLeg, 0.7f, 0.3f, hitboxLayer);
            m_HitboxObjects[index++] = CreateEndBoneHitbox("Hitbox_RightFoot", rtarsal, rtibia, HitRegion.RightFoot, 0.4f, 0.25f, 0.35f, hitboxLayer);

            m_IsInitialized = true;
            Debug.Log($"[ClientHitboxSystem] {index} Hitboxen erstellt.");
        }

        /// <summary>
        /// Entfernt alle erstellten Hitbox-Collider.
        /// </summary>
        public void ClearHitboxes()
        {
            if (m_HitboxObjects != null)
            {
                for (int i = 0; i < m_HitboxObjects.Length; i++)
                {
                    if (m_HitboxObjects[i] != null)
                    {
                        Destroy(m_HitboxObjects[i]);
                    }
                }

                m_HitboxObjects = null;
            }

            m_IsInitialized = false;
        }

        private void OnDestroy()
        {
            ClearHitboxes();
        }

        // ===================================================================
        // Hitbox Creation
        // ===================================================================

        /// <summary>
        /// Erstellt einen BoxCollider zwischen zwei Bones.
        /// Der Collider wird als Kind des Start-Bones erstellt und entlang der Bone-Achse ausgerichtet.
        /// Laenge = Bone-zu-Bone Distanz, Breite/Tiefe = Laenge * widthFactor.
        /// </summary>
        private GameObject CreateBoneHitbox(
            string hitboxName,
            Transform startBone,
            Transform endBone,
            HitRegion hitRegion,
            float damageMultiplier,
            float widthFactor,
            int layer)
        {
            if (startBone == null || endBone == null)
            {
                Debug.LogWarning($"[ClientHitboxSystem] Bone nicht gefunden fuer {hitboxName} — uebersprungen.");
                return null;
            }

            float boneLength = Vector3.Distance(startBone.position, endBone.position);
            if (boneLength < 0.001f)
            {
                Debug.LogWarning($"[ClientHitboxSystem] Bone-Distanz ~0 fuer {hitboxName} — uebersprungen.");
                return null;
            }

            float width = boneLength * widthFactor;

            GameObject hitboxGO = new(hitboxName)
            {
                layer = layer
            };
            hitboxGO.transform.SetParent(startBone, false);

            // Collider entlang der Achse vom Start- zum End-Bone ausrichten
            Vector3 boneDirection = startBone.InverseTransformPoint(endBone.position).normalized;
            Vector3 midpoint = startBone.InverseTransformPoint(endBone.position) * 0.5f;

            hitboxGO.transform.SetLocalPositionAndRotation(midpoint, Quaternion.FromToRotation(Vector3.up, boneDirection));
            BoxCollider boxCollider = hitboxGO.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(width, boneLength, width);

            HitboxCollider hitboxComponent = hitboxGO.AddComponent<HitboxCollider>();
            hitboxComponent.Initialize(hitRegion, damageMultiplier);

            // Visual Debug Mesh erstellen
            if (m_ShowVisualHitboxes)
            {
                CreateVisualMesh(hitboxGO, boxCollider.size, hitRegion);
            }

            return hitboxGO;
        }

        /// <summary>
        /// Erstellt einen BoxCollider fuer ein End-Bone (Fuss, ohne Kind-Bone fuer Laengenberechnung).
        /// Laenge wird als Bruchteil der Parent-Bone-Distanz geschaetzt.
        /// </summary>
        private GameObject CreateEndBoneHitbox(
            string hitboxName,
            Transform endBone,
            Transform parentBone,
            HitRegion hitRegion,
            float damageMultiplier,
            float widthFactor,
            float lengthFactor,
            int layer)
        {
            if (endBone == null || parentBone == null)
            {
                Debug.LogWarning($"[ClientHitboxSystem] Bone nicht gefunden fuer {hitboxName} — uebersprungen.");
                return null;
            }

            // Laenge aus Parent-Bone-Distanz schaetzen
            float parentBoneLength = Vector3.Distance(parentBone.position, endBone.position);
            float estimatedLength = parentBoneLength * lengthFactor;

            if (estimatedLength < 0.001f)
            {
                Debug.LogWarning($"[ClientHitboxSystem] Geschaetzte Laenge ~0 fuer {hitboxName} — uebersprungen.");
                return null;
            }

            float width = estimatedLength * widthFactor;

            GameObject hitboxGO = new(hitboxName);
            hitboxGO.layer = layer;
            hitboxGO.transform.SetParent(endBone, false);

            // Fuss-Collider leicht nach vorne/unten versetzt
            hitboxGO.transform.SetLocalPositionAndRotation(new Vector3(0f, -estimatedLength * 0.3f, estimatedLength * 0.3f), Quaternion.identity);
            BoxCollider boxCollider = hitboxGO.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(width, estimatedLength * 0.5f, estimatedLength);

            HitboxCollider hitboxComponent = hitboxGO.AddComponent<HitboxCollider>();
            hitboxComponent.Initialize(hitRegion, damageMultiplier);

            // Visual Debug Mesh erstellen
            if (m_ShowVisualHitboxes)
            {
                CreateVisualMesh(hitboxGO, boxCollider.size, hitRegion);
            }

            return hitboxGO;
        }

        // ===================================================================
        // Utility
        // ===================================================================

        /// <summary>
        /// Rekursive Tiefensuche nach einem Kind-Transform mit exaktem Namen.
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
        /// Erstellt ein semi-transparentes Cube-Mesh als Kind des Hitbox-GameObjects.
        /// Farbe wird nach HitRegion-Typ bestimmt (Kopf=Rot, Torso=Gelb, Arme=Blau, etc.).
        /// </summary>
        private void CreateVisualMesh(GameObject hitboxGO, Vector3 colliderSize, HitRegion hitRegion)
        {
            GameObject visualGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualGO.name = "Visual";

            // Primitive erzeugt automatisch einen Collider — entfernen
            if (visualGO.TryGetComponent<Collider>(out var primitiveCollider))
            {
                Destroy(primitiveCollider);
            }

            visualGO.transform.SetParent(hitboxGO.transform, false);
            visualGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visualGO.transform.localScale = colliderSize;

            if (visualGO.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                Color regionColor = GetRegionColor(hitRegion);
                regionColor.a = m_VisualAlpha;

                Material material = new(Shader.Find("Unlit/Color"));
                material.color = regionColor;

                // Transparenz aktivieren
                material.SetFloat("_Mode", 3);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = 3000;

                meshRenderer.material = material;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }
        }

        /// <summary>
        /// Gibt eine Farbe basierend auf dem Region-Typ zurueck.
        /// Rot=Kopf/Hals, Gelb=Torso, Blau=Schultern/Arme, Cyan=Haende/Fuesse, Gruen=Beine.
        /// </summary>
        private static Color GetRegionColor(HitRegion hitRegion)
        {
            switch (hitRegion)
            {
                case HitRegion.Head:
                case HitRegion.Neck:
                    return Color.red;

                case HitRegion.Chest:
                case HitRegion.Gut:
                case HitRegion.Groin:
                    return Color.yellow;

                case HitRegion.LeftShoulder:
                case HitRegion.RightShoulder:
                case HitRegion.LeftArm:
                case HitRegion.RightArm:
                    return Color.blue;

                case HitRegion.LeftHand:
                case HitRegion.RightHand:
                case HitRegion.LeftFoot:
                case HitRegion.RightFoot:
                    return Color.cyan;

                case HitRegion.LeftThigh:
                case HitRegion.RightThigh:
                case HitRegion.LeftLeg:
                case HitRegion.RightLeg:
                    return Color.green;

                default:
                    return Color.white;
            }
        }
    }
}
