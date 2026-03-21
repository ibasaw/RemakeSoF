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
        /// Z-Rotation-Override fuer SoF2-Waffen-Achsenkorrektur (Grad).
        /// </summary>
        private const float k_WeaponZRotation = -90f;

        /// <summary>
        /// URP Unlit Shader fuer Waffen — Textur wird 1:1 angezeigt.
        /// Specular-Highlights via Emission-Kanal simuliert (SoF2: additive blendFunc).
        /// </summary>
        private const string k_WeaponShader = "Universal Render Pipeline/Unlit";

        /// <summary>
        /// Suffix fuer Specular-Texturen (z.B. "models/weapons/knife/knife_spec").
        /// </summary>
        private const string k_SpecSuffix = "_spec";

        /// <summary>
        /// Aktuell instanziiertes Waffen-GameObject.
        /// </summary>
        private GameObject m_CurrentWeaponInstance;

        /// <summary>
        /// Name der aktuell geladenen Waffe.
        /// </summary>
        private string m_CurrentWeaponName;

        /// <summary>
        /// Transform des Hand-Bones, an dem die Waffe haengt.
        /// </summary>
        private Transform m_AttachmentBone;

        /// <summary>
        /// Aktuell geladener Waffen-Name (leer wenn keine Waffe).
        /// </summary>
        public string CurrentWeaponName => m_CurrentWeaponName;

        /// <summary>
        /// Aktuell instanziiertes Waffen-GameObject (null wenn keine Waffe).
        /// </summary>
        public GameObject CurrentWeaponInstance => m_CurrentWeaponInstance;

        /// <summary>
        /// Setzt den Attachment-Bone (rhang_tag_bone) fuer die Waffen-Instanziierung.
        /// Muss aufgerufen werden bevor LoadAndAttachWeapon() funktioniert.
        /// </summary>
        public void SetAttachmentBone(Transform bone)
        {
            m_AttachmentBone = bone;
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

            // Prefab-Scale vor Instantiierung lesen (wird durch Parenting überschrieben).
            Vector3 prefabScale = weaponPrefab.transform.localScale;

            // Instanziieren als Child des Hand-Bones.
            m_CurrentWeaponInstance = Object.Instantiate(weaponPrefab, m_AttachmentBone);

            // Texturen via viewModel-Pfad anwenden (Base + Specular)
            ApplyWeaponTextures(m_CurrentWeaponInstance, weaponKey);

            Transform weaponTransform = m_CurrentWeaponInstance.transform;
            weaponTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 0f, k_WeaponZRotation));

            // Der Character-Bone hat lossyScale=2.54 (FBX-Import-Konvertierung: 1 inch = 2.54 cm).
            // Ohne Kompensation erbt die Waffe diesen Factor und erscheint 2.54x zu groß.
            // Lösung: localScale = prefabScale / boneLossyScale → worldScale = prefabScale (wie im Prefab designed).
            Vector3 boneScale = m_AttachmentBone.lossyScale;
            weaponTransform.localScale = new Vector3(
                prefabScale.x / boneScale.x,
                prefabScale.y / boneScale.y,
                prefabScale.z / boneScale.z);

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
        /// Erstellt ein URP/Unlit Material fuer Waffen.
        /// Base-Textur wird 1:1 angezeigt (keine Lichtberechnung).
        /// Specular-Map wird als Emission addiert — simuliert SoF2's additive blendFunc GL_SRC_ALPHA GL_ONE.
        /// </summary>
        private static Material BuildWeaponMaterial(Shader shader, string name, Texture2D baseTexture, Texture2D specTexture)
        {
            Material material = new(shader) { name = name };

            // Base-Textur: wird direkt angezeigt (Unlit = kein Licht, Textur 1:1)
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", baseTexture);
            }
            else
            {
                material.mainTexture = baseTexture;
            }
            material.SetColor("_BaseColor", Color.white);

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0f);
            }

            // Specular via Emission simulieren: SoF2 blendFunc GL_SRC_ALPHA GL_ONE = additiv
            // Emission addiert die _spec Textur dezent auf die Base-Textur → metallischer Glanz
            if (specTexture != null && material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", specTexture);
                // Dezente Intensitaet: 15% der Spec-Textur additiv → subtiler Highlight-Effekt
                material.SetColor("_EmissionColor", new Color(0.15f, 0.15f, 0.15f, 1f));
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
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
