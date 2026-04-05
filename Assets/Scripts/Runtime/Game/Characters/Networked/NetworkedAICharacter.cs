using System.Collections;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Server;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Networked
{
    /// <summary>
    /// Networked AI-Character. Server-owned (kein Owner-Client).
    /// Server steuert Position/Rotation direkt, Clients interpolieren.
    /// Nutzt dieselbe NetworkedCharacterState fuer Skin, Health, Team etc.
    /// Hat ClientHitboxSystem fuer Bone-basierte Trefferkennung (analog zu Spielern).
    /// Hat ServerCharacterController fuer Damage/Death/Respawn-Logik.
    /// </summary>
    public class NetworkedAICharacter : NetworkedCharacter, ICharacter
    {
        /// <summary>
        /// Server-seitige AI-Logik-Komponente.
        /// </summary>
        [SerializeField]
        private ServerAICharacter m_ServerAICharacter;

        /// <summary>
        /// Client-seitige Skin/Visual-Komponente (fuer Remote-Rendering).
        /// </summary>
        [SerializeField]
        private ClientCharacterSkinHandler m_SkinHandler;

        /// <summary>
        /// NetworkedCharacterState-Referenz fuer Identity, Health, Team etc.
        /// </summary>
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        /// <summary>
        /// Server-seitiger Damage/Death/Respawn-Controller (analog zu Spielern).
        /// </summary>
        [SerializeField]
        private ServerCharacterController m_ServerCharacterController;

        /// <summary>
        /// Client-seitiges Hitbox-System fuer Bone-basierte Trefferkennung.
        /// Wird nach Visual-Instanziierung aufgebaut (29 BoxCollider auf Skeleton-Bones).
        /// </summary>
        [SerializeField]
        private ClientHitboxSystem m_HitboxSystem;

        /// <summary>
        /// Client-seitiges Collider-System fuer physische Kollision (SoF2 AABB).
        /// Erzeugt identischen BoxCollider wie bei Spielern (draufspringen, Kollision etc.).
        /// Wird nur auf Clients initialisiert (Server hat BoxCollider via ServerAICharacter).
        /// </summary>
        [SerializeField]
        private ClientColliderSystem m_ColliderSystem;

        /// <summary>
        /// Interpolationsgeschwindigkeit fuer Clients.
        /// </summary>
        private const float k_InterpolationSpeed = 15f;

        /// <summary>
        /// Server: Initialisiert den AI-Character an einem Spawn-Point.
        /// </summary>
        protected override void OnServerSpawn()
        {
            base.OnServerSpawn();

            // Hitboxen aufbauen sobald Visual geladen ist (Server braucht Hitboxes fuer Bone-Tracking)
            SubscribeToVisualInstantiated();

            if (ServerPlayerSpawnPoints.Instance == null)
            {
                Debug.Log("[NetworkedAICharacter] ServerPlayerSpawnPoints noch nicht verfuegbar — warte auf Map-Laden.");
                StartCoroutine(WaitForMapAndPosition());
                return;
            }

            AssignSpawnPosition();
        }

        /// <summary>
        /// Remote-Client: Startet Interpolation und Hitbox-Aufbau nach Visual-Load.
        /// AI-Bots haben keinen Owner-Client, daher gibt es kein OnOwnerSpawn.
        /// </summary>
        protected override void OnRemoteSpawn()
        {
            base.OnRemoteSpawn();

            // Hitboxen aufbauen sobald Visual geladen ist (Client braucht Hitboxes fuer Trefferkennung)
            SubscribeToVisualInstantiated();

            Debug.Log($"[NetworkedAICharacter] Remote-Client: AI-Character {NetworkObjectId} interpoliert.");
        }

        /// <summary>
        /// Abonniert das OnVisualInstantiated-Event des SkinHandlers.
        /// </summary>
        private void SubscribeToVisualInstantiated()
        {
            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated += OnVisualInstantiated;
            }
        }

        /// <summary>
        /// Callback nach Visual-Instanziierung: Baut Hitboxen und Collider auf den Skeleton-Bones auf.
        /// Collider wird auf Server UND Client identisch erstellt (ClientColliderSystem).
        /// </summary>
        private void OnVisualInstantiated(GameObject visualInstance)
        {
            if (m_HitboxSystem != null)
            {
                m_HitboxSystem.BuildHitboxes(visualInstance.transform);
                Debug.Log("[NetworkedAICharacter] Hitboxen aufgebaut fuer AI-Bot.");
            }

            // Physik-Collider auf Server UND Client identisch aufbauen (SoF2 AABB)
            if (m_ColliderSystem != null)
            {
                Transform highestPoint = FindDeepChild(visualInstance.transform, "*head_t_0");
                Transform cranium = FindDeepChild(visualInstance.transform, "cranium");
                Transform rightHandBolt = FindDeepChild(visualInstance.transform, "rhang_tag_bone");
                Transform leftHandBolt = FindDeepChild(visualInstance.transform, "lhand_tag_bone");
                Transform rightFoot = FindDeepChild(visualInstance.transform, "rtarsal");
                Transform leftFoot = FindDeepChild(visualInstance.transform, "ltarsal");
                Transform pelvis = FindDeepChild(visualInstance.transform, "pelvis");

                m_ColliderSystem.CalculateAutoCapsuleSize(
                    highestPoint != null ? highestPoint : cranium,
                    pelvis, leftHandBolt, rightHandBolt, leftFoot, rightFoot);
                Debug.Log("[NetworkedAICharacter] Collider aufgebaut fuer AI-Bot.");
            }

            // Server: Collider-Referenz an ServerAICharacter uebergeben fuer Physik-Simulation
            if (IsServer && m_ServerAICharacter != null && m_ColliderSystem != null)
            {
                m_ServerAICharacter.SetColliderSystem(m_ColliderSystem);
            }
        }

        /// <summary>
        /// Sucht rekursiv ein Kind-Transform per Name.
        /// </summary>
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

        /// <summary>
        /// Raeumt Event-Abos auf beim Despawn.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated -= OnVisualInstantiated;
            }

            if (m_HitboxSystem != null)
            {
                m_HitboxSystem.ClearHitboxes();
            }
        }

        /// <summary>
        /// Wartet bis die Map geladen ist und ServerPlayerSpawnPoints verfuegbar sind.
        /// </summary>
        private IEnumerator WaitForMapAndPosition()
        {
            yield return new WaitUntil(() => ServerPlayerSpawnPoints.Instance != null);
            AssignSpawnPosition();
        }

        /// <summary>
        /// Weist dem AI-Character einen Spawn-Point zu.
        /// </summary>
        private void AssignSpawnPosition()
        {
            (Vector3 position, Quaternion rotation) = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
            transform.SetPositionAndRotation(position, rotation);

            m_ServerPosition.Value = position;
            m_ServerRotation.Value = rotation;

            m_ServerAICharacter.SetReady();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedAICharacter] Server: AI-Bot gespawnt bei {position}");
#endif
        }

        /// <summary>
        /// Server-seitiger Respawn auf den naechsten Spawn-Point.
        /// </summary>
        public void RespawnAtNextSpawnPoint()
        {
            if (!IsServer)
            {
                return;
            }

            if (ServerPlayerSpawnPoints.Instance == null)
            {
                Debug.LogWarning("[NetworkedAICharacter] RespawnAtNextSpawnPoint: Keine ServerPlayerSpawnPoints vorhanden.");
                return;
            }

            (Vector3 position, Quaternion rotation) = ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint();
            transform.SetPositionAndRotation(position, rotation);

            m_ServerPosition.Value = position;
            m_ServerRotation.Value = rotation;

            m_ServerAICharacter.SetReady();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[NetworkedAICharacter] Server: AI-Bot respawned bei {position}");
#endif
        }

        private void Update()
        {
            if (IsServer)
            {
                // Server: Position direkt aus AI-Logik synchronisieren
                m_ServerPosition.Value = transform.position;
                m_ServerRotation.Value = transform.rotation;
                return;
            }

            // Client: Interpolation zur Server-Position
            transform.position = Vector3.Lerp(transform.position, m_ServerPosition.Value, Time.deltaTime * k_InterpolationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, m_ServerRotation.Value, Time.deltaTime * k_InterpolationSpeed);
        }

        /// <summary>
        /// Setzt Name, Skin, Team und Waffen auf dem NetworkedCharacterState.
        /// Wird vom AIBotSpawner nach dem Spawn aufgerufen.
        /// </summary>
        /// <param name="botName">Name des Bots.</param>
        /// <param name="skinName">Skin-Name (Addressable-Key).</param>
        /// <param name="teamId">Team-ID (0 oder 1).</param>
        public void InitializeBot(string botName, string skinName, uint teamId)
        {
            if (!IsServer)
            {
                return;
            }

            m_CharacterState.SetCharacterName(botName);
            m_CharacterState.SetCurrentSkinName(skinName);
            m_CharacterState.SetTeam(teamId);
            m_CharacterState.AddWeapon("knife");
            m_CharacterState.SetCurrentWeaponName("knife");

            Debug.Log($"[NetworkedAICharacter] Bot initialisiert: Name='{botName}', Skin='{skinName}', Team={teamId}");
        }
    }
}
