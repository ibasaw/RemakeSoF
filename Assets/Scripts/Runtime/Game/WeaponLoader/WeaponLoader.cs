using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.WeaponManagement
{
    /// <summary>
    /// Interner Service fuer Waffen-Loading und Bone-Attachment.
    /// Laedt Waffen-Prefabs ueber PrefabManager (Addressables) und
    /// instanziiert sie als Child eines Hand-Bones.
    /// Kein MonoBehaviour — wird von ClientPlayerCharacter orchestriert.
    /// </summary>
    public class WeaponLoader
    {
        /// <summary>
        /// Z-Rotation-Override fuer SoF2-Waffen-Achsenkorrektur (Grad).
        /// </summary>
        private const float k_WeaponZRotation = -90f;

        /// <summary>
        /// Scale-Override fuer SoF2-Waffen-Groessenkorrektur. TODO: derzeit nicht benötigt eventuell später nochmal schauen!
        /// </summary>
        private const float k_WeaponScale = 1f;

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

            // Instanziieren als Child des Hand-Bones
            m_CurrentWeaponInstance = Object.Instantiate(weaponPrefab, m_AttachmentBone);
            Transform weaponTransform = m_CurrentWeaponInstance.transform;
            weaponTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 0f, k_WeaponZRotation));
            //weaponTransform.localScale = Vector3.one * k_WeaponScale;

            m_CurrentWeaponName = weaponKey;

            Debug.Log($"[WeaponLoader] Waffe '{weaponKey}' an '{m_AttachmentBone.name}' attached.");
            return true;
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
