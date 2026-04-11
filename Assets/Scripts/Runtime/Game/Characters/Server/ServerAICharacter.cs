using Tolik.RemakeSoF.Runtime.AI;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using Tolik.RemakeSoF.Runtime.GametypeManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Server
{
    /// <summary>
    /// Server-seitige AI-Logik fuer Bot-Characters.
    /// Wird nur auf dem Server ausgefuehrt.
    /// Steuert Bewegung und Entscheidungen des AI-Bots.
    /// Nutzt dieselbe PlayerPhysicsSimulation wie menschliche Spieler (SoF2/Quake3 Physik).
    /// Physics-Collider wird von ClientColliderSystem bereitgestellt (identisch auf Server und Client).
    /// AI-Entscheidungen kommen vom AIBotController (GOAP Intent-basiert).
    /// </summary>
    [RequireComponent(typeof(NetworkedAICharacter))]
    public class ServerAICharacter : MonoBehaviour
    {
        /// <summary>SoF2/Quake3 Physik-Simulation (Gravity, Ground-Trace, Friction, etc.).</summary>
        [SerializeField]
        private PlayerPhysicsSimulation m_Simulation = new();

        /// <summary>
        /// AI-Controller (selbes GameObject, per GetComponent gefunden).
        /// Sensoren + GOAP-Intents → PlayerCommand.
        /// </summary>
        private AIBotController m_AIController;

        /// <summary>
        /// Referenz auf NetworkedAICharacter fuer Animations-State-Synchronisation.
        /// </summary>
        private NetworkedAICharacter m_NetworkedAICharacter;

        /// <summary>
        /// Referenz auf NetworkedCharacterState fuer Waffen-/Ammo-Daten.
        /// </summary>
        private NetworkedCharacterState m_CharacterState;

        /// <summary>Ob der Bot im letzten Frame angegriffen hat (fuer AttackSequence-Inkrement).</summary>
        private bool m_WasAttacking;

        /// <summary>Laufende Attack-Sequenznummer (wird bei jedem neuen Angriff inkrementiert).</summary>
        private byte m_AttackSequence;

        /// <summary>Glaettungsfaktor fuer Animations-Parameter (identisch wie ClientPlayerCharacter).</summary>
        private float m_AnimParamSmooth = 10f;

        /// <summary>Geglätteter Horizontal-Wert fuer Animator Blend Tree.</summary>
        private float m_AnimHorizontal;

        /// <summary>Geglätteter Vertical-Wert fuer Animator Blend Tree.</summary>
        private float m_AnimVertical;

        /// <summary>Referenz auf das eigene HitboxSystem (fuer Self-Hit-Vermeidung bei Raycast).</summary>
        private ClientHitboxSystem m_OwnHitboxSystem;

        /// <summary>Naechster Zeitpunkt ab dem ein Angriff moeglich ist (Cooldown).</summary>
        private float m_NextAttackTime;

        /// <summary>SoF2-QU zu Unity-Meter Umrechnungsfaktor.</summary>
        private const float SOF2_UNIT_SCALE = 0.0254f;

        /// <summary>Augen-Hoehe relativ zur Capsule-Hoehe (SoF2: 72/89).</summary>
        private const float EYE_HEIGHT_RATIO = 72f / 89f;

        /// <summary>
        /// Server: Wendet einen Stun auf diesen Bot an.
        /// Setzt den StunTime-Timer der Physik-Simulation (massive Friction, keine Beschleunigung).
        /// Buttons (Angriff) werden waehrend Stun blockiert (leerer Command).
        /// </summary>
        /// <param name="duration">Stun-Dauer in Sekunden.</param>
        public void ApplyStun(float duration)
        {
            m_Simulation.StunTime = duration;
        }

        /// <summary>
        /// Referenz auf das ClientColliderSystem, das den Physics-BoxCollider verwaltet.
        /// Wird von NetworkedAICharacter nach Visual-Load gesetzt.
        /// </summary>
        private ClientColliderSystem m_ColliderSystem;

        /// <summary>
        /// Ob der AI-Character bereit ist zu agieren (Map geladen, Spawn-Position gesetzt).
        /// </summary>
        private bool m_IsReady;

        /// <summary>
        /// Initialisiert die Physik-Simulation.
        /// Capsule-Dimensionen werden spaeter vom ClientColliderSystem uebernommen.
        /// </summary>
        private void InitializePhysics()
        {
            m_Simulation.GroundMask = ~0;
        }

        /// <summary>
        /// Setzt die Referenz auf das ClientColliderSystem und uebernimmt dessen Capsule-Dimensionen
        /// fuer die Physik-Simulation. Wird von NetworkedAICharacter nach Visual-Load aufgerufen.
        /// </summary>
        public void SetColliderSystem(ClientColliderSystem colliderSystem)
        {
            m_ColliderSystem = colliderSystem;

            if (m_ColliderSystem != null)
            {
                m_Simulation.SetCapsuleDimensions(
                    m_ColliderSystem.GetCurrentCapsuleHeight(),
                    m_ColliderSystem.GetCurrentCapsuleRadius(),
                    m_ColliderSystem.GetCurrentCapsuleCenter()
                );
            }
        }

        /// <summary>
        /// Initialisiert die Sensoren am Visual-Root (wird nach Visual-Load aufgerufen).
        /// </summary>
        public void InitializeSensors(Transform visualRoot)
        {
            if (m_AIController != null)
            {
                m_AIController.InitializeSensors(visualRoot);
            }
        }

        /// <summary>Zugriff auf den AIBotController (fuer GOAP-Integration).</summary>
        public AIBotController AIController => m_AIController;

        /// <summary>
        /// Markiert den AI-Character als bereit.
        /// Wird von NetworkedAICharacter nach Spawn-Position-Zuweisung aufgerufen.
        /// Findet den AIBotController per GetComponent (liegt als Component auf dem Prefab).
        /// </summary>
        public void SetReady()
        {
            if (!m_IsReady)
            {
                InitializePhysics();
                m_AIController = GetComponent<AIBotController>();
                m_NetworkedAICharacter = GetComponent<NetworkedAICharacter>();
                m_CharacterState = GetComponent<NetworkedCharacterState>();
                m_OwnHitboxSystem = GetComponent<ClientHitboxSystem>();

                // Physik-Simulation an den Controller uebergeben (fuer isGrounded, Velocity etc.)
                if (m_AIController != null)
                {
                    m_AIController.SetPhysicsSimulation(m_Simulation);
                }
            }

            m_IsReady = true;
            m_Simulation.SetState(Vector3.zero, true, false, false);
            Debug.Log($"[AI·Character] Bot bereit | Pos={transform.position:F1}");
        }

        private void Update()
        {
            if (!m_IsReady)
            {
                return;
            }

            SimulatePhysics();
        }

        /// <summary>
        /// Fuehrt einen Physik-Schritt aus.
        /// Wenn der AIBotController bereit ist, liefert das neuronale Netz den PlayerCommand.
        /// Ansonsten wird ein leerer Command verwendet (Idle / Gravity only).
        /// Identische Physik wie menschliche Spieler (Gravity, Ground-Trace, Friction).
        /// </summary>
        private void SimulatePhysics()
        {
            // Eigenen Physik-Collider deaktivieren BEVOR Sensoren feuern,
            // damit SphereCasts nicht den eigenen Collider auf Layer 7 (Player) treffen.
            BoxCollider physicsCollider = m_ColliderSystem != null ? m_ColliderSystem.PhysicsCollider : null;

            if (physicsCollider != null)
            {
                physicsCollider.enabled = false;
            }

            PlayerCommand cmd;

            // Waehrend Stun: leerer Command (keine Bewegung, kein Angriff).
            // Velocity wird NICHT auf 0 gesetzt — Physik-Simulation bremst per Friction.
            if (m_Simulation.StunTime > 0f)
            {
                cmd = new PlayerCommand
                {
                    MoveInput = Vector2.zero,
                    YawAngle = transform.eulerAngles.y,
                    PitchAngle = 0f,
                    MoveYawAngle = transform.eulerAngles.y,
                    Buttons = 0,
                    DeltaTime = Time.deltaTime,
                    SequenceNumber = 0,
                };
            }
            else if (m_AIController != null && m_AIController.IsReady)
            {
                cmd = m_AIController.Tick(transform.eulerAngles.y);
            }
            else
            {
                cmd = new PlayerCommand
                {
                    MoveInput = Vector2.zero,
                    YawAngle = transform.eulerAngles.y,
                    PitchAngle = 0f,
                    MoveYawAngle = transform.eulerAngles.y,
                    Buttons = 0,
                    DeltaTime = Time.deltaTime,
                    SequenceNumber = 0,
                };
            }

            Vector3 position = transform.position;
            m_Simulation.Simulate(ref position, cmd);
            transform.position = position;

            // Rotation aus NN-Yaw-Output anwenden
            transform.rotation = Quaternion.Euler(0f, cmd.YawAngle, 0f);

            if (physicsCollider != null)
            {
                physicsCollider.enabled = true;

                // Server-Collider an Crouch-State anpassen (identisch wie ServerPlayerCharacter)
                float h = m_Simulation.CapsuleHeight;
                float r = m_Simulation.CapsuleRadius;
                physicsCollider.size = new Vector3(r * 2f, h, r * 2f);
                physicsCollider.center = m_Simulation.CapsuleCenter;
            }

            // Waffen-Angriff verarbeiten (Hitscan-Raycast + Schaden)
            if ((cmd.Buttons & CommandButtons.Attack) != 0)
            {
                ProcessAIAttack(cmd);
            }

            // Animations-State berechnen und an NetworkVariable schreiben
            if (m_NetworkedAICharacter != null)
            {
                UpdateAnimationState(cmd);
            }
        }

        /// <summary>
        /// Berechnet die Augen-Position des AI-Bots (identisch wie beim Spieler).
        /// SoF2-Ratio: ViewHeight/TotalHeight = 72/89.
        /// </summary>
        private Vector3 GetEyePosition()
        {
            float eyeHeight = m_Simulation.CapsuleHeight * EYE_HEIGHT_RATIO;
            return transform.position + new Vector3(0f, eyeHeight, 0f);
        }

        /// <summary>
        /// Server: Verarbeitet einen Hitscan-Angriff des AI-Bots.
        /// Raycast von Augen-Position in Blickrichtung gegen Hitbox-Layer.
        /// Identische Hit-Detection wie bei Spielern (Bone-basierte BoxCollider auf Hitbox-Layer).
        /// Damage wird ueber ServerCharacterController.ApplyDamage(damage, attackerState) angewendet.
        /// </summary>
        private void ProcessAIAttack(PlayerCommand cmd)
        {
            // Cooldown pruefen (Waffenspezifische Feuerrate)
            if (Time.time < m_NextAttackTime)
            {
                return;
            }

            // Waffendaten laden
            string weaponName = m_CharacterState != null ? m_CharacterState.CurrentWeaponName : "knife";
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(weaponName);
            WeaponAttackDefinition attackDef = weapon?.Attack;

            if (attackDef == null)
            {
                return;
            }

            // Cooldown setzen: fireDelay in ms → Sekunden (Fallback: 0.5s)
            float cooldown = attackDef.FireDelay > 0 ? attackDef.FireDelay / 1000f : 0.5f;
            m_NextAttackTime = Time.time + cooldown;

            // Waffen-Sound an alle Clients senden
            m_NetworkedAICharacter.PlayAttackSound(weaponName);

            // Munition verbrauchen (Messer hat unendlich, andere Waffen nicht)
            if (m_CharacterState != null && !weapon.IsMelee)
            {
                if (!m_CharacterState.TryConsumeAmmo())
                {
                    return;
                }
            }

            // Augen-Position und Blickrichtung berechnen
            Vector3 eyePos = GetEyePosition();
            Quaternion aimRotation = Quaternion.Euler(cmd.PitchAngle, cmd.YawAngle, 0f);
            Vector3 aimDirection = aimRotation * Vector3.forward;
            float rangeMeters = attackDef.Range * SOF2_UNIT_SCALE;

            // Eigene Hitboxen + Collider deaktivieren (Self-Hit-Vermeidung)
            BoxCollider physicsCollider = m_ColliderSystem != null ? m_ColliderSystem.PhysicsCollider : null;
            if (physicsCollider != null)
            {
                physicsCollider.enabled = false;
            }
            m_OwnHitboxSystem?.SetHitboxesEnabled(false);

            // Physik-Transforms synchronisieren (damit Hitboxen aktuelle Bone-Positionen haben)
            Physics.SyncTransforms();

            // Hitscan-Raycast gegen Hitbox-Layer (Bone-basierte Trefferkennung)
            int hitboxLayerMask = LayerMask.GetMask("Hitbox");
            bool didHitBone = Physics.Raycast(eyePos, aimDirection, out RaycastHit boneHit,
                rangeMeters, hitboxLayerMask, QueryTriggerInteraction.Collide);

            if (didHitBone)
            {
                HitboxCollider hitboxCollider = boneHit.collider.GetComponent<HitboxCollider>();
                if (hitboxCollider != null)
                {
                    float damageMultiplier = hitboxCollider.DamageMultiplier;
                    int finalDamage = Mathf.RoundToInt(attackDef.Damage * damageMultiplier);

                    NetworkedCharacterState targetState = boneHit.collider.GetComponentInParent<NetworkedCharacterState>();
                    if (targetState != null && targetState != m_CharacterState
                        && targetState.IsAlive && finalDamage > 0)
                    {
                        // Gametype-Schadens-Modifier anwenden (z.B. HideAndSeek: Seeker→Hider = 1000 Instant-Kill)
                        int modifiedDamage = finalDamage;
                        GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
                        if (gametypeManager != null)
                        {
                            GametypeTeam attackerTeam = (GametypeTeam)m_CharacterState.TeamId;
                            GametypeTeam victimTeam = (GametypeTeam)targetState.TeamId;

                            GametypeDamageResult damageResult = gametypeManager.OnDamage(
                                m_CharacterState.OwnerClientId,
                                targetState.OwnerClientId,
                                attackerTeam,
                                victimTeam,
                                finalDamage,
                                weaponName,
                                false
                            );

                            modifiedDamage = damageResult.ModifiedDamage;

                            // Stun auf das Opfer anwenden
                            if (damageResult.ApplyStun && damageResult.StunDuration > 0f)
                            {
                                NetworkedPlayerCharacter targetCharacter = targetState.GetComponent<NetworkedPlayerCharacter>();
                                if (targetCharacter != null)
                                {
                                    targetCharacter.ApplyStun(damageResult.StunDuration);
                                }
                                else
                                {
                                    ServerAICharacter targetBot = targetState.GetComponent<ServerAICharacter>();
                                    if (targetBot != null)
                                    {
                                        targetBot.ApplyStun(damageResult.StunDuration);
                                    }
                                }
                            }

                            // Kill-Nachricht an das Opfer senden (falls vorhanden)
                            if (!string.IsNullOrEmpty(damageResult.VictimMessage))
                            {
                                string msg = damageResult.VictimMessage
                                    .Replace("{attackerName}", m_CharacterState.CharacterName)
                                    .Replace("{victimName}", targetState.CharacterName);
                                NetworkedPlayerCharacter targetCharacter = targetState.GetComponent<NetworkedPlayerCharacter>();
                                if (targetCharacter != null)
                                {
                                    targetCharacter.SendGametypeMessage(msg);
                                }
                            }
                        }

                        int previousHealth = targetState.Health;

                        ServerCharacterController targetController = targetState.GetComponent<ServerCharacterController>();
                        if (targetController != null)
                        {
                            targetController.ApplyDamage(modifiedDamage, m_CharacterState);
                        }

                        int newHealth = targetState.Health;
                        bool isKill = newHealth <= 0 && previousHealth > 0;

                        Debug.Log($"[AI·Attack] Bot '{m_CharacterState.CharacterName}' → " +
                                  $"'{targetState.CharacterName}' | Region={hitboxCollider.HitRegion} | " +
                                  $"Damage={modifiedDamage} (Base={attackDef.Damage}×{damageMultiplier:F2}) | " +
                                  $"HP={newHealth}{(isKill ? " KILL!" : "")}");
                    }
                }
            }

            // Eigene Hitboxen + Collider wieder aktivieren
            m_OwnHitboxSystem?.SetHitboxesEnabled(true);
            if (physicsCollider != null)
            {
                physicsCollider.enabled = true;
            }
        }

        /// <summary>
        /// Berechnet den Animations-State aus dem PlayerCommand und der Physik-Simulation.
        /// Schreibt das Ergebnis in die NetworkVariable des NetworkedAICharacter.
        /// </summary>
        private void UpdateAnimationState(PlayerCommand cmd)
        {
            Vector3 velocity = m_Simulation.Velocity;
            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;

            bool isAttacking = (cmd.Buttons & CommandButtons.Attack) != 0;
            if (isAttacking && !m_WasAttacking)
            {
                m_AttackSequence++;
            }
            m_WasAttacking = isAttacking;

            // Animations-Parameter glaetten (identisch wie ClientPlayerCharacter)
            // Ohne Smoothing springt der Blend Tree bei NN-Outputs jeden Frame und
            // zeigt dauerhaft Walking-Animation statt korrekte Idle/Crouch-Transitions.
            float animT = Mathf.Clamp01(m_AnimParamSmooth * Time.deltaTime);
            m_AnimHorizontal = Mathf.Lerp(m_AnimHorizontal, cmd.MoveInput.x, animT);
            m_AnimVertical = Mathf.Lerp(m_AnimVertical, cmd.MoveInput.y, animT);

            string weaponName = m_CharacterState != null ? m_CharacterState.CurrentWeaponName : "knife";
            short ammo = (short)(m_CharacterState != null ? m_CharacterState.CurrentClipAmmo : 0);

            // JustLanded: wie beim Spieler — beim Sprung-Spam landet und springt im selben Frame,
            // IsGrounded ist am Frame-Ende false, aber JustLanded sagt der Animation "Boden beruehrt".
            bool animGrounded = m_Simulation.IsGrounded || m_Simulation.JustLanded;

            NetworkAnimationState state = new()
            {
                Speed = horizontalSpeed,
                Horizontal = m_AnimHorizontal,
                Vertical = m_AnimVertical,
                MoveInputX = cmd.MoveInput.x,
                MoveInputY = cmd.MoveInput.y,
                PitchAngle = cmd.PitchAngle,
                CurrentWeapon = (byte)NetworkedCharacterState.GetWeaponAnimatorIndex(weaponName),
                Ammo = ammo,
                AttackSequence = m_AttackSequence,
            };

            state.IsMoving = horizontalSpeed > 0.01f;
            state.IsGrounded = animGrounded;
            state.IsWalking = horizontalSpeed > 0.01f && horizontalSpeed < m_Simulation.PmMaxSpeed * m_Simulation.PmWalkScale;
            state.IsAttacking = isAttacking;
            state.IsCrouching = m_Simulation.IsCrouching;
            state.IsReloading = false;
            state.IsAltAttacking = false;
            state.IsSwapping = false;

            m_NetworkedAICharacter.WriteAnimationState(state);
        }
    }
}
