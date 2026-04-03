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
        /// Interpolationsgeschwindigkeit fuer Clients.
        /// </summary>
        private const float k_InterpolationSpeed = 15f;

        /// <summary>
        /// Server: Initialisiert den AI-Character an einem Spawn-Point.
        /// </summary>
        protected override void OnServerSpawn()
        {
            base.OnServerSpawn();

            if (ServerPlayerSpawnPoints.Instance == null)
            {
                Debug.Log("[NetworkedAICharacter] ServerPlayerSpawnPoints noch nicht verfuegbar — warte auf Map-Laden.");
                StartCoroutine(WaitForMapAndPosition());
                return;
            }

            AssignSpawnPosition();
        }

        /// <summary>
        /// Remote-Client: Startet Interpolation.
        /// AI-Bots haben keinen Owner-Client, daher gibt es kein OnOwnerSpawn.
        /// </summary>
        protected override void OnRemoteSpawn()
        {
            base.OnRemoteSpawn();
            Debug.Log($"[NetworkedAICharacter] Remote-Client: AI-Character {NetworkObjectId} interpoliert.");
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
