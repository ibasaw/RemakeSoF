using Tolik.RemakeSoF.Runtime.AI;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Networked;
using Tolik.RemakeSoF.Runtime.GametypeManagement;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Server
{
    /// <summary>
    /// Server-seitige Orchestrierung für Character-Kampflogik.
    /// Delegiert Health-/State-Management an <see cref="NetworkedCharacterState"/> (Single Source of Truth).
    /// Integriert Gametype-Events bei Tod (Scoring, Runden-Restart, Respawn-Gating).
    /// Keine eigene State-Haltung — nur Logik und Timing.
    /// </summary>
    public class ServerCharacterController : MonoBehaviour
    {
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        [SerializeField]
        private int m_MaxHealth = 100;

        [SerializeField]
        private float m_RespawnTime = 5f;

        /// <summary>
        /// Aktuelle Health aus dem NetworkedCharacterState.
        /// </summary>
        public int CurrentHealth => m_CharacterState.Health;

        /// <summary>
        /// Ist der Character am Leben? (aus NetworkedCharacterState).
        /// </summary>
        public bool IsAlive => m_CharacterState.IsAlive;

        /// <summary>
        /// Server: Schaden anwenden.
        /// Nutzt <see cref="NetworkedCharacterState.SetHealth"/> als Single Source of Truth.
        /// </summary>
        /// <param name="damageAmount">Schaden-Menge.</param>
        /// <param name="attackerId">Die Client-ID des Angreifers (optional).</param>
        public void ApplyDamage(int damageAmount, ulong attackerId = ulong.MaxValue)
        {
            if (!m_CharacterState.IsAlive)
            {
                return;
            }

            int newHealth = Mathf.Max(0, m_CharacterState.Health - damageAmount);
            m_CharacterState.SetHealth(newHealth);
            Debug.Log($"[ServerCharacterController] Character took {damageAmount} damage. Health: {newHealth}");

            // AI-Bot ueber Schaden informieren (fuer Damage-Reaction)
            NotifyAIBotDamage(damageAmount, attackerId);

            if (newHealth <= 0)
            {
                KillCharacter(attackerId);
            }
        }

        /// <summary>
        /// Server: Schaden anwenden mit direkter Attacker-State-Referenz.
        /// Fuer AI-Bots die keine eigene ClientId haben (server-owned).
        /// Kill-Attribution erfolgt direkt ueber den uebergebenen State.
        /// </summary>
        /// <param name="damageAmount">Schaden-Menge.</param>
        /// <param name="attackerState">NetworkedCharacterState des Angreifers (Bot oder Spieler).</param>
        public void ApplyDamage(int damageAmount, NetworkedCharacterState attackerState)
        {
            if (!m_CharacterState.IsAlive)
            {
                return;
            }

            int newHealth = Mathf.Max(0, m_CharacterState.Health - damageAmount);
            m_CharacterState.SetHealth(newHealth);
            Debug.Log($"[ServerCharacterController] Character took {damageAmount} damage from '{attackerState?.CharacterName}'. Health: {newHealth}");

            // AI-Bot ueber Schaden informieren (fuer Damage-Reaction)
            if (attackerState != null)
            {
                NotifyAIBotDamage(damageAmount, attackerState.transform.position);
            }

            if (newHealth <= 0)
            {
                KillCharacterByState(attackerState);
            }
        }

        /// <summary>
        /// Benachrichtigt einen AI-Bot wenn er Schaden erhaelt (fuer Damage-Reaction).
        /// Uebergibt die Angreifer-Position an den AIBotController per NotifyDamageTaken.
        /// Nur wirksam wenn dieses GameObject einen AIBotController hat.
        /// </summary>
        /// <param name="damageAmount">Erlittener Schaden.</param>
        /// <param name="attackerPosition">Weltposition des Angreifers.</param>
        private void NotifyAIBotDamage(int damageAmount, Vector3 attackerPosition)
        {
            AIBotController botController = GetComponent<AIBotController>();
            if (botController != null)
            {
                botController.NotifyDamageTaken(attackerPosition, damageAmount);
            }
        }

        /// <summary>
        /// Benachrichtigt einen AI-Bot (Angreifer per ClientId aufgeloest).
        /// Versucht die Angreifer-Position ueber NetworkManager.ConnectedClients zu ermitteln.
        /// </summary>
        /// <param name="damageAmount">Erlittener Schaden.</param>
        /// <param name="attackerId">Client-ID des Angreifers.</param>
        private void NotifyAIBotDamage(int damageAmount, ulong attackerId)
        {
            AIBotController botController = GetComponent<AIBotController>();
            if (botController == null)
            {
                return;
            }

            // Angreifer-Position ueber NetworkManager ermitteln
            if (attackerId != ulong.MaxValue
                && NetworkManager.Singleton != null
                && NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerId, out NetworkClient client)
                && client.PlayerObject != null)
            {
                botController.NotifyDamageTaken(client.PlayerObject.transform.position, damageAmount);
            }
            else
            {
                // Fallback: Angreifer-Position unbekannt, Richtung nicht bestimmbar
                botController.NotifyDamageTaken(transform.position + transform.forward * 5f, damageAmount);
            }
        }

        /// <summary>
        /// Server: Character töten.
        /// Setzt IsAlive auf false, addiert Death.
        /// Meldet den Tod an den GametypeManager fuer Scoring und Runden-Logik.
        /// Respawn wird nur geplant wenn der aktive Gametype es erlaubt.
        /// </summary>
        /// <param name="attackerId">Die Client-ID des Killers (optional).</param>
        private void KillCharacter(ulong attackerId)
        {
            m_CharacterState.SetIsAlive(false);
            m_CharacterState.AddDeath();

            ulong victimClientId = m_CharacterState.OwnerClientId;
            GametypeTeam victimTeam = (GametypeTeam)m_CharacterState.TeamId;
            Debug.Log($"[ServerCharacterController] Character {victimClientId} died. Killed by {attackerId}");

            // Gametype-Event verarbeiten (Scoring, Runden-Restart)
            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            if (gametypeManager != null)
            {
                // Killer-Team ermitteln
                GametypeTeam killerTeam = GametypeTeam.None;
                if (attackerId != ulong.MaxValue && attackerId != victimClientId)
                {
                    NetworkObject killerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(attackerId);
                    if (killerObj != null && killerObj.TryGetComponent(out NetworkedCharacterState killerState))
                    {
                        killerTeam = (GametypeTeam)killerState.TeamId;

                        // Killer bekommt Kill-Score
                        killerState.AddKill();
                    }
                }

                GametypeEventResult result = gametypeManager.OnClientDeath(victimClientId, attackerId, victimTeam, killerTeam);

                // Team-Score-Deltas auf NetworkedGameState anwenden
                if (NetworkedGameState.Singleton != null)
                {
                    NetworkedGameState.Singleton.ApplyGametypeResult(result);
                }

                // Runden-Restart anfordern (z.B. alle Hider eliminiert)
                if (result.RestartRound && NetworkedGameState.Singleton != null)
                {
                    NetworkedGameState.Singleton.RequestRoundRestart(result.RestartDelaySeconds);
                }

                // Respawn nur wenn Gametype es erlaubt
                if (gametypeManager.AllowRespawn())
                {
                    Invoke(nameof(RespawnCharacter), m_RespawnTime);
                }
            }
            else
            {
                // Kein GametypeManager → Default-Respawn
                Invoke(nameof(RespawnCharacter), m_RespawnTime);
            }
        }

        /// <summary>
        /// Server: Character toeten mit direkter Attacker-State-Referenz.
        /// Fuer AI-Bots die keine eigene ClientId haben.
        /// Kill-Attribution und Gametype-Integration direkt ueber den State.
        /// </summary>
        /// <param name="attackerState">NetworkedCharacterState des Angreifers.</param>
        private void KillCharacterByState(NetworkedCharacterState attackerState)
        {
            m_CharacterState.SetIsAlive(false);
            m_CharacterState.AddDeath();

            ulong victimClientId = m_CharacterState.OwnerClientId;
            GametypeTeam victimTeam = (GametypeTeam)m_CharacterState.TeamId;
            Debug.Log($"[ServerCharacterController] Character {m_CharacterState.CharacterName} died. Killed by '{attackerState?.CharacterName}'");

            GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
            if (gametypeManager != null)
            {
                GametypeTeam killerTeam = GametypeTeam.None;
                ulong killerClientId = ulong.MaxValue;

                if (attackerState != null && attackerState != m_CharacterState)
                {
                    killerTeam = (GametypeTeam)attackerState.TeamId;
                    killerClientId = attackerState.OwnerClientId;
                    attackerState.AddKill();
                }

                GametypeEventResult result = gametypeManager.OnClientDeath(victimClientId, killerClientId, victimTeam, killerTeam);

                if (NetworkedGameState.Singleton != null)
                {
                    NetworkedGameState.Singleton.ApplyGametypeResult(result);
                }

                if (result.RestartRound && NetworkedGameState.Singleton != null)
                {
                    NetworkedGameState.Singleton.RequestRoundRestart(result.RestartDelaySeconds);
                }

                if (gametypeManager.AllowRespawn())
                {
                    Invoke(nameof(RespawnCharacter), m_RespawnTime);
                }
            }
            else
            {
                Invoke(nameof(RespawnCharacter), m_RespawnTime);
            }
        }

        /// <summary>
        /// Server: Character respawnen.
        /// Setzt Health auf Max und IsAlive auf true.
        /// Client-Benachrichtigung erfolgt automatisch via NetworkVariable-Change.
        /// </summary>
        private void RespawnCharacter()
        {
            m_CharacterState.SetHealth(m_MaxHealth);
            m_CharacterState.SetIsAlive(true);
            Debug.Log($"[ServerCharacterController] Character respawned. Health: {m_MaxHealth}");

            // TODO: Spawn-Position zurücksetzen via NetworkedPlayerCharacter
        }

        /// <summary>
        /// Server: Heilen.
        /// </summary>
        /// <param name="healAmount">Heilungs-Menge.</param>
        public void Heal(int healAmount)
        {
            if (!m_CharacterState.IsAlive)
            {
                return;
            }

            int newHealth = Mathf.Min(m_MaxHealth, m_CharacterState.Health + healAmount);
            m_CharacterState.SetHealth(newHealth);
            Debug.Log($"[ServerCharacterController] Character healed by {healAmount}. Health: {newHealth}");
        }
    }
}
