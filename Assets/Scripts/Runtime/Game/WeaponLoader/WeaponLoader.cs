using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tolik.RemakeSoF.Runtime.Game.WeaponManagement
{
    /// <summary>
    /// Interner Service fuer Waffen-Loading und Bone-Attachment.
    /// Laedt Waffen-Prefabs ueber PrefabManager (Addressables) und
    /// instanziiert sie als Child eines Hand-Bones.
    /// Wendet Texturen direkt via viewModel-Pfad an (Base + Specular).
    /// Kein MonoBehaviour — wird von ClientPlayerCharacter orchestriert.
    /// </summary>
    public class WeaponLoader
    {
        /// <summary>
        /// Standard Z-Rotation fuer TP-Waffen am Hand-Bone (Grad).
        /// FP-Waffen ueberschreiben dies via SetLocalRotation().
        /// </summary>
        private const float k_DefaultZRotation = -90f;

        /// <summary>
        /// Konfigurierbare lokale Rotation der Waffe.
        /// Default: (0,0,-90) fuer TP-Waffen am Hand-Bone.
        /// FP-Waffen setzen (0,0,90) via SetLocalRotation().
        /// </summary>
        private Quaternion m_LocalRotation = Quaternion.Euler(0f, 0f, k_DefaultZRotation);

        /// <summary>
        /// SoF2/MapSurface Shader fuer Waffen — Unlit-Basis mit anteiliger Lambert-Beleuchtung.
        /// Waffen nutzen einen hoeheren LightBlend als Map-Geometrie damit sie
        /// deutlich auf Directional- und Point-Lights reagieren.
        /// </summary>
        private const string k_WeaponShader = "SoF2/MapSurface";

        /// <summary>
        /// LightBlend fuer Waffen-Materialien.
        /// Hoeher als Map-Geometrie (0.25) damit Waffen staerker auf Szenen-Licht reagieren.
        /// 0.5 = 50% Unlit-Basis + 50% Lambert-Beleuchtung.
        /// </summary>
        private const float k_WeaponLightBlend = 0.5f;

        /// <summary>
        /// Suffix fuer Specular-Texturen (z.B. "models/weapons/knife/knife_spec").
        /// </summary>
        private const string k_SpecSuffix = "_spec";

        /// <summary>
        /// Aktuell instanziiertes Waffen-GameObject.
        /// </summary>
        private GameObject m_CurrentWeaponInstance;

        /// <summary>
        /// Basis-LocalScale nach Bone-Kompensation (ohne Foreshorten).
        /// </summary>
        private Vector3 m_BaseLocalScale;

        /// <summary>
        /// Name der aktuell geladenen Waffe.
        /// </summary>
        private string m_CurrentWeaponName;

        /// <summary>
        /// Transform des Hand-Bones, an dem die Waffe haengt.
        /// </summary>
        private Transform m_AttachmentBone;

        /// <summary>
        /// Ob Bone-Scale-Kompensation angewendet werden soll.
        /// True fuer TP-Waffen (Hand-Bone hat FBX-Import-Scale 2.54).
        /// False fuer FP-Waffen (FP_WeaponHolder-Scale 0.0254 ist beabsichtigt QU→Meter).
        /// </summary>
        private bool m_CompensateBoneScale = true;

        /// <summary>
        /// Aktuell geladener Waffen-Name (leer wenn keine Waffe).
        /// </summary>
        public string CurrentWeaponName => m_CurrentWeaponName;

        /// <summary>
        /// Aktuell instanziiertes Waffen-GameObject (null wenn keine Waffe).
        /// </summary>
        public GameObject CurrentWeaponInstance => m_CurrentWeaponInstance;

        /// <summary>
        /// Wendet SoF2 Foreshorten-Skalierung auf die aktuelle Waffe an.
        /// SoF2: VectorScale(hand.axis[0], foreshorten, hand.axis[0]) in CG_AddViewWeapon.
        /// Skaliert NUR die Forward-Achse (Z) — Waffe wird kuerzer/flacher,
        /// aber nicht schmaler. Perspektivischer Trick fuer weniger Bildschirmanteil.
        /// </summary>
        public void ApplyForeshorten(float foreshorten)
        {
            if (m_CurrentWeaponInstance == null)
            {
                return;
            }

            if (foreshorten <= 0f)
            {
                foreshorten = 1f;
            }

            // SoF2 skaliert nur axis[0] (Forward) — in Unity ist das localScale.z
            m_CurrentWeaponInstance.transform.localScale = new Vector3(
                m_BaseLocalScale.x,
                m_BaseLocalScale.y,
                m_BaseLocalScale.z * foreshorten);
        }

        /// <summary>
        /// Setzt den Attachment-Bone (rhang_tag_bone) fuer die Waffen-Instanziierung.
        /// Muss aufgerufen werden bevor LoadAndAttachWeapon() funktioniert.
        /// </summary>
        public void SetAttachmentBone(Transform bone)
        {
            m_AttachmentBone = bone;
        }

        /// <summary>
        /// Ueberschreibt die lokale Rotation der Waffe.
        /// TP-Waffen: (0,0,-90) fuer Hand-Bone-Korrektur (Default).
        /// FP-Waffen: (0,0,90) fuer Kamera-Ausrichtung.
        /// SoF2: viewG2Model nutzt Kamera-Achsen direkt (cg_weapons.c CG_AddViewWeapon).
        /// </summary>
        public void SetLocalRotation(Quaternion rotation)
        {
            m_LocalRotation = rotation;
        }

        /// <summary>
        /// Aktiviert/deaktiviert Bone-Scale-Kompensation.
        /// TP (true): Hand-Bone hat FBX-Import-Scale — Waffe muss kompensieren.
        /// FP (false): FP_WeaponHolder-Scale 0.0254 ist beabsichtigte QU-zu-Meter-Konvertierung.
        /// </summary>
        public void SetCompensateBoneScale(bool compensate)
        {
            m_CompensateBoneScale = compensate;
        }

        /// <summary>
        /// Laedt ein Waffen-Prefab ueber Addressables (via PrefabManager) und
        /// instanziiert es als Child des Attachment-Bones.
        /// Wendet Base- und Specular-Texturen via viewModel-Pfad aus WeaponDefinition an.
        /// Entfernt vorherige Waffe automatisch.
        /// </summary>
        /// <param name="weaponKey">Addressable-Key der Waffe (z.B. "knife").</param>
        /// <returns>True wenn erfolgreich geladen und attached.</returns>
        public bool LoadAndAttachWeapon(string weaponKey)
        {
            if (string.IsNullOrEmpty(weaponKey))
            {
                Debug.LogWarning("[WeaponLoader] LoadAndAttachWeapon: weaponKey ist leer.");
                return false;
            }

            if (m_AttachmentBone == null)
            {
                Debug.LogWarning("[WeaponLoader] LoadAndAttachWeapon: AttachmentBone nicht gesetzt.");
                return false;
            }

            // Idempotent: nicht nochmal laden wenn gleiche Waffe
            if (weaponKey == m_CurrentWeaponName && m_CurrentWeaponInstance != null)
            {
                return true;
            }

            // Vorherige Waffe entfernen
            ClearCurrentWeapon();

            // Prefab ueber PrefabManager laden (Addressables, cache-first)
            PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
            if (prefabManager == null)
            {
                Debug.LogWarning("[WeaponLoader] PrefabManager nicht ueber ServiceLocator verfuegbar.");
                return false;
            }

            GameObject weaponPrefab = prefabManager.LoadPrefab<GameObject>(weaponKey);
            if (weaponPrefab == null)
            {
                Debug.LogWarning($"[WeaponLoader] Waffen-Prefab nicht gefunden: '{weaponKey}'");
                return false;
            }

            // Instanziieren als Child des Hand-Bones.
            m_CurrentWeaponInstance = Object.Instantiate(weaponPrefab, m_AttachmentBone);

            // Texturen via viewModel-Pfad anwenden (Base + Specular)
            ApplyWeaponTextures(m_CurrentWeaponInstance, weaponKey);

            Transform weaponTransform = m_CurrentWeaponInstance.transform;
            weaponTransform.SetLocalPositionAndRotation(Vector3.zero, m_LocalRotation);

            // Bone-Scale-Kompensation: Nur fuer TP-Waffen noetig.
            // TP-Hand-Bone hat lossyScale=2.54 (FBX inch→cm) — Waffe muss kompensieren.
            // FP_WeaponHolder hat 0.0254 als beabsichtigte QU→Meter-Konvertierung —
            // dort darf NICHT kompensiert werden, sonst wird die Waffe 39x zu gross.
            Vector3 prefabScale = weaponPrefab.transform.localScale;
            if (m_CompensateBoneScale)
            {
                Vector3 boneScale = m_AttachmentBone.lossyScale;
                weaponTransform.localScale = new Vector3(
                    prefabScale.x / boneScale.x,
                    prefabScale.y / boneScale.y,
                    prefabScale.z / boneScale.z);
            }
            else
            {
                weaponTransform.localScale = prefabScale;
            }

            m_BaseLocalScale = weaponTransform.localScale;
            m_CurrentWeaponName = weaponKey;

            Debug.Log($"[WeaponLoader] Waffe '{weaponKey}' attached. worldScale={weaponTransform.lossyScale}");
            return true;
        }

        /// <summary>
        /// Wendet Base- und Specular-Texturen auf alle Renderer der Waffe an.
        /// Nutzt den viewModel-Pfad aus WeaponDefinition als Textur-Key.
        /// SoF2 Shader-Konvention: viewModel = Base-Textur, viewModel_spec = Specular-Map.
        /// URP/Lit mit Specular-Workflow fuer metallischen Glanz.
        /// </summary>
        private static void ApplyWeaponTextures(GameObject instance, string weaponKey)
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                return;
            }

            // viewModel-Pfad aus WeaponDefinition holen
            WeaponDataLoader weaponLoader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition definition = weaponLoader?.GetById(weaponKey);
            if (definition == null || string.IsNullOrEmpty(definition.ViewModel))
            {
                Debug.LogWarning($"[WeaponLoader] Keine WeaponDefinition fuer '{weaponKey}' — Texturen nicht anwendbar.");
                return;
            }

            string viewModelPath = definition.ViewModel;

            // Base-Textur laden (z.B. "models/weapons/knife/knife")
            Texture2D baseTexture = LoadTexture(textureManager, viewModelPath);

            // Specular-Textur laden (z.B. "models/weapons/knife/knife_spec")
            Texture2D specTexture = LoadTexture(textureManager, viewModelPath + k_SpecSuffix);

            if (baseTexture == null)
            {
                Debug.LogWarning($"[WeaponLoader] Base-Textur nicht gefunden: '{viewModelPath}'");
                return;
            }

            Shader weaponShader = Shader.Find(k_WeaponShader);
            if (weaponShader == null)
            {
                Debug.LogWarning("[WeaponLoader] URP/Unlit Shader nicht gefunden.");
                return;
            }

            // Material erstellen: URP/Unlit mit Specular via Emission
            Material weaponMaterial = BuildWeaponMaterial(weaponShader, viewModelPath, baseTexture, specTexture);

            // Auf alle Renderer anwenden
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                // Alte Instanz-Materialien freigeben bevor neue zugewiesen werden
                Material[] oldMats = renderer.materials;
                foreach (Material oldMat in oldMats)
                {
                    if (oldMat != null)
                    {
                        Object.Destroy(oldMat);
                    }
                }

                Material[] materials = renderer.sharedMaterials;
                Material[] newMaterials = new Material[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    newMaterials[i] = weaponMaterial;
                }
                renderer.materials = newMaterials;
            }
        }

        /// <summary>
        /// Erstellt ein SoF2/MapSurface Material fuer Waffen.
        /// Unlit-Basis mit anteiliger Lambert-Beleuchtung (LightBlend).
        /// Waffen reagieren auf Directional- und Point-Lights der Szene.
        /// </summary>
        private static Material BuildWeaponMaterial(Shader shader, string name, Texture2D baseTexture, Texture2D specTexture)
        {
            Material material = new(shader) { name = name };

            // Base-Textur
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", baseTexture);
            }
            else
            {
                material.mainTexture = baseTexture;
            }
            material.SetColor("_BaseColor", Color.white);

            // LightBlend: Waffen staerker beleuchtet als Map-Geometrie
            if (material.HasProperty("_LightBlend"))
            {
                material.SetFloat("_LightBlend", k_WeaponLightBlend);
            }

            // Backface-Culling OFF (SoF2: cull disable)
            material.SetFloat("_Cull", (float)CullMode.Off);

            return material;
        }

        /// <summary>
        /// Laedt eine Texture2D ueber TextureManager (Key oder Alias).
        /// </summary>
        private static Texture2D LoadTexture(TextureManager textureManager, string texturePath)
        {
            TextureData textureData = textureManager.GetTextureData(texturePath);
            if (textureData == null)
            {
                textureData = textureManager.GetTextureDataByAlias(texturePath);
            }

            if (textureData != null && textureData.HasTexture())
            {
                return textureData.Texture;
            }

            return null;
        }

        /// <summary>
        /// Setzt die Sichtbarkeit der aktuellen Waffe (SetActive).
        /// Wird fuer FP/TP-Umschaltung benoetigt: FP-Waffe nur im First-Person-Modus sichtbar,
        /// TP-Waffe wird ueber ShadowCastingMode gesteuert (nicht hier).
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (m_CurrentWeaponInstance != null)
            {
                m_CurrentWeaponInstance.SetActive(visible);
            }
        }

        /// <summary>
        /// Entfernt die aktuelle Waffe und zerstoert das GameObject.
        /// </summary>
        public void ClearCurrentWeapon()
        {
            if (m_CurrentWeaponInstance != null)
            {
                Object.Destroy(m_CurrentWeaponInstance);
                m_CurrentWeaponInstance = null;
            }

            m_CurrentWeaponName = null;
        }
    }
}
