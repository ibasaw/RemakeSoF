using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.WeaponManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Client-seitige Komponente fuer AI-Bot-Characters.
    /// Wird auf allen Clients instanziiert (auch Host).
    /// Verantwortlich fuer visuelle Darstellung, Interpolation und
    /// spaeter: Animationssteuerung basierend auf NetworkVariables.
    /// Server ignoriert diese Komponente.
    /// </summary>
    [RequireComponent(typeof(NetworkedAICharacter))]
    public class ClientAICharacter : MonoBehaviour
    {
        /// <summary>
        /// Referenz auf die vernetzte AI-Character-Komponente.
        /// </summary>
        [SerializeField]
        private NetworkedAICharacter m_NetworkedAICharacter;

        /// <summary>
        /// Referenz auf den NetworkedCharacterState fuer Events (Health, Skin, etc.).
        /// </summary>
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        /// <summary>
        /// Waffen-Loader fuer TP-Waffen-Visual (Hand-Bone Attachment).
        /// </summary>
        private readonly WeaponLoader m_WeaponLoader = new();

        /// <summary>
        /// Pending Weapon-Name: gesetzt wenn OnWeaponChanged vor OnVisualInstantiated kommt.
        /// </summary>
        private string m_PendingWeaponName;

        private ClientCharacterSkinHandler m_SkinHandler;

        private void Awake()
        {
            m_SkinHandler = GetComponent<ClientCharacterSkinHandler>();
        }

        private void OnEnable()
        {
            if (m_CharacterState != null)
            {
                m_CharacterState.OnCharacterDied += OnBotDied;
                m_CharacterState.OnCharacterRespawned += OnBotRespawned;
                m_CharacterState.OnWeaponChanged += OnWeaponChanged;
            }

            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated += OnVisualInstantiated;
            }
        }

        private void OnDisable()
        {
            if (m_CharacterState != null)
            {
                m_CharacterState.OnCharacterDied -= OnBotDied;
                m_CharacterState.OnCharacterRespawned -= OnBotRespawned;
                m_CharacterState.OnWeaponChanged -= OnWeaponChanged;
            }

            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated -= OnVisualInstantiated;
            }
        }

        /// <summary>
        /// Visual wurde instantiiert: Hand-Bone fuer Waffen-Attachment setzen.
        /// Falls bereits eine Waffe pending ist, sofort laden.
        /// </summary>
        private void OnVisualInstantiated(GameObject visualInstance)
        {
            Transform rightHandBolt = FindDeepChild(visualInstance.transform, "rhang_tag_bone");
            if (rightHandBolt != null)
            {
                m_WeaponLoader.ClearCurrentWeapon();
                m_WeaponLoader.SetAttachmentBone(rightHandBolt);
            }

            if (!string.IsNullOrEmpty(m_PendingWeaponName))
            {
                m_WeaponLoader.LoadAndAttachWeapon(m_PendingWeaponName);
                m_PendingWeaponName = null;
            }
        }

        /// <summary>
        /// Callback: Bot ist gestorben. Spaeter: Ragdoll/Death-Animation ausloesen.
        /// </summary>
        private void OnBotDied()
        {
            Debug.Log($"[ClientAICharacter] Bot gestorben: {m_CharacterState.CharacterName}");
        }

        /// <summary>
        /// Callback: Bot ist respawned. Visual zuruecksetzen.
        /// </summary>
        private void OnBotRespawned()
        {
            Debug.Log($"[ClientAICharacter] Bot respawned: {m_CharacterState.CharacterName}");
        }

        /// <summary>
        /// Callback: Waffe geaendert. Laedt und attached die neue Waffe visuell am Hand-Bone.
        /// </summary>
        private void OnWeaponChanged(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName))
            {
                m_WeaponLoader.ClearCurrentWeapon();
                m_PendingWeaponName = null;
                return;
            }

            if (m_WeaponLoader.LoadAndAttachWeapon(weaponName))
            {
                m_PendingWeaponName = null;
            }
            else
            {
                // Visual noch nicht bereit — merken und bei OnVisualInstantiated nachladen
                m_PendingWeaponName = weaponName;
            }
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform result = FindDeepChild(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
