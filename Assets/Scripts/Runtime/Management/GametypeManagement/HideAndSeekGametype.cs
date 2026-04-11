using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Environment;
using Tolik.RemakeSoF.Runtime.Game.Networked;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Phase des Hide-and-Seek-Rundenablaufs.
    /// </summary>
    public enum HideAndSeekPhase
    {
        /// <summary>Versteckphase: Hider verstecken sich, Seeker sind geblindet/eingefroren.</summary>
        Hiding,
        /// <summary>Suchphase: Seeker suchen, Hider muessen ueberleben.</summary>
        Seeking,
        /// <summary>Runde vorbei, Ergebnis wird angezeigt.</summary>
        RoundOver
    }

    /// <summary>
    /// Hide-and-Seek Gametype-Implementierung.
    ///
    /// Ablauf:
    /// 1. Spieler werden in Hider (Rot) und Seeker (Blau) aufgeteilt.
    /// 2. Versteckphase: Hider haben X Sekunden zum Verstecken, Seeker sind eingefroren.
    /// 3. Suchphase: Seeker suchen und eliminieren Hider.
    /// 4. Suchzeit laeuft ab → Hider gewinnen, oder alle Hider gefunden → Seeker gewinnen.
    /// 5. Naechste Runde: Teams werden getauscht.
    ///
    /// CVARs aus ServerConfiguration:
    /// - hideandseek_hidetime: Versteckzeit in Sekunden (Default 30).
    /// - hideandseek_seektime: Suchzeit in Sekunden (Default 120).
    /// - hideandseek_seekercount: Anzahl Seeker (Default 1).
    /// - hideandseek_hiderweapons: Hider haben Waffen (Default false).
    /// - hideandseek_seekerweapons: Seeker haben Waffen (Default true).
    /// - hideandseek_roundlimit: Max. Runden (Default 5).
    /// </summary>
    public class HideAndSeekGametype : BaseGametype
    {
        /// <inheritdoc />
        public override string GametypeId => "hideandseek";

        /// <summary>Aktuelle Phase der Runde.</summary>
        public HideAndSeekPhase CurrentPhase { get; private set; }

        /// <summary>Verbleibende Zeit in der aktuellen Phase (Sekunden).</summary>
        public float PhaseTimeRemaining { get; private set; }

        /// <summary>Anzahl lebender Hider in der aktuellen Runde.</summary>
        public int AliveHiderCount { get; internal set; }

        /// <summary>Ob Hider die Runde durch Zeitablauf ueberlebt haben (fuer Scoring in OnTimeExpired).</summary>
        bool m_HidersSurvived;

        /// <summary>Vergangene Zeit seit Rundenstart (fuer Lucky-M4-Delay).</summary>
        float m_RoundElapsed;

        /// <summary>Konfigurierte Seeker-Anzahl.</summary>
        int SeekerCount => ServerConfig.hideandseek_seekercount > 0 ? ServerConfig.hideandseek_seekercount : 1;

        /// <summary>Konfigurierte Versteckzeit.</summary>
        int HideTime => ServerConfig.hideandseek_hidetime > 0 ? ServerConfig.hideandseek_hidetime : 30;

        /// <summary>Konfigurierte Suchzeit.</summary>
        int SeekTime => ServerConfig.hideandseek_seektime > 0 ? ServerConfig.hideandseek_seektime : 120;

        /// <summary>Konfiguriertes Rundenlimit.</summary>
        int RoundLimit => ServerConfig.hideandseek_roundlimit > 0 ? ServerConfig.hideandseek_roundlimit : 5;

        /// <inheritdoc />
        public override void Initialize(GametypeDefinition definition, ServerConfiguration serverConfig)
        {
            base.Initialize(definition, serverConfig);
            CurrentPhase = HideAndSeekPhase.RoundOver;
            PhaseTimeRemaining = 0f;
        }

        /// <inheritdoc />
        public override void OnRoundStart()
        {
            base.OnRoundStart();

            // Starte mit Versteckphase
            CurrentPhase = HideAndSeekPhase.Hiding;
            PhaseTimeRemaining = HideTime;
            m_HidersSurvived = false;
            m_RoundElapsed = 0f;

            Debug.Log($"[HideAndSeek] Runde {CurrentRound}/{RoundLimit} — Versteckphase: {HideTime}s");
        }

        /// <inheritdoc />
        public override void OnRunFrame(float deltaTime)
        {
            if (CurrentPhase == HideAndSeekPhase.RoundOver)
            {
                return;
            }

            PhaseTimeRemaining -= deltaTime;

            if (PhaseTimeRemaining <= 0f)
            {
                OnPhaseTimeExpired();
            }

            // Lucky M4: 3 Sekunden nach Rundenstart vergeben
            if (m_RoundElapsed >= 0f)
            {
                m_RoundElapsed += deltaTime;
                if (m_RoundElapsed >= 3f)
                {
                    m_RoundElapsed = -1f;
                    AwardLuckyM4();
                }
            }
        }

        /// <summary>
        /// Vergibt die Lucky M4 an den Hider mit dem zweithöchsten persoenlichen Kill-Score.
        /// Bei Gleichstand wird zufaellig ein Hider gewaehlt.
        /// Broadcastet "XY got a lucky m4" an alle Clients.
        /// </summary>
        void AwardLuckyM4()
        {
            if (NetworkManager.Singleton == null || NetworkedGameState.Singleton == null)
            {
                return;
            }

            // Alle lebenden Hider (Red) sammeln mit ihrem Kill-Score
            System.Collections.Generic.List<NetworkedCharacterState> hiders = new();

            // Menschliche Spieler
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObj != null && playerObj.TryGetComponent(out NetworkedCharacterState state))
                {
                    if ((GametypeTeam)state.TeamId == GametypeTeam.Red && state.IsAlive)
                    {
                        hiders.Add(state);
                    }
                }
            }

            // AI-Bots
            AIBotSpawner botSpawner = NetworkedGameState.Singleton.AIBotSpawner;
            if (botSpawner != null)
            {
                foreach (NetworkObject bot in botSpawner.SpawnedBots)
                {
                    if (bot != null && bot.TryGetComponent(out NetworkedCharacterState state))
                    {
                        if ((GametypeTeam)state.TeamId == GametypeTeam.Red && state.IsAlive)
                        {
                            hiders.Add(state);
                        }
                    }
                }
            }

            if (hiders.Count == 0)
            {
                Debug.Log("[HideAndSeek] Keine Hider vorhanden fuer Lucky M4.");
                return;
            }

            NetworkedCharacterState winner;

            if (hiders.Count == 1)
            {
                // Einziger Hider bekommt die M4 direkt
                winner = hiders[0];
            }
            else
            {
                // Nach Kills absteigend sortieren
                hiders.Sort((a, b) => b.Kills.CompareTo(a.Kills));

                // Zweithöchsten Kill-Score finden
                int secondHighestKills = hiders[1].Kills;

                // Alle Hider mit diesem Score sammeln (fuer Random-Auswahl bei Gleichstand)
                System.Collections.Generic.List<NetworkedCharacterState> candidates = new();
                for (int i = 1; i < hiders.Count; i++)
                {
                    if (hiders[i].Kills == secondHighestKills)
                    {
                        candidates.Add(hiders[i]);
                    }
                }

                // Zufaellig einen Kandidaten waehlen
                winner = candidates[Random.Range(0, candidates.Count)];
            }

            // M4 mit gametype-spezifischer Ammo vergeben
            (int clip, int reserve, int altClip, int altReserve)? ammoOverride = GetStartAmmo("m4");
            if (ammoOverride.HasValue)
            {
                winner.PreloadWeaponAmmo("m4", ammoOverride.Value.clip, ammoOverride.Value.reserve, ammoOverride.Value.altClip, ammoOverride.Value.altReserve);
            }
            winner.AddWeapon("m4");

            // Broadcast an alle Clients
            string playerName = winner.CharacterName;
            NetworkedGameState.Singleton.BroadcastGametypeMessage($"{playerName} got a lucky m4");

            Debug.Log($"[HideAndSeek] Lucky M4 vergeben an '{playerName}' (Kills: {winner.Kills}).");
        }

        /// <summary>
        /// Wird aufgerufen wenn die aktuelle Phasenzeit abgelaufen ist.
        /// </summary>
        void OnPhaseTimeExpired()
        {
            switch (CurrentPhase)
            {
                case HideAndSeekPhase.Hiding:
                    // Versteckzeit vorbei → Suchphase beginnt
                    CurrentPhase = HideAndSeekPhase.Seeking;
                    PhaseTimeRemaining = SeekTime;
                    Debug.Log($"[HideAndSeek] Suchphase beginnt! Seeker duerfen suchen. Zeit: {SeekTime}s");
                    break;

                case HideAndSeekPhase.Seeking:
                    // Suchzeit vorbei → Hider gewinnen (Scoring passiert in OnTimeExpired)
                    Debug.Log("[HideAndSeek] Suchzeit abgelaufen! Hider haben ueberlebt!");
                    m_HidersSurvived = true;
                    CurrentPhase = HideAndSeekPhase.RoundOver;
                    break;
            }
        }

        /// <inheritdoc />
        public override GametypeEventResult OnClientDeath(ulong victimClientId, ulong killerClientId, GametypeTeam victimTeam, GametypeTeam killerTeam)
        {
            if (CurrentPhase != HideAndSeekPhase.Seeking)
            {
                return GametypeEventResult.None;
            }

            // Nur Hider (Red) Tode zaehlen
            if (victimTeam != GametypeTeam.Red)
            {
                return GametypeEventResult.None;
            }

            AliveHiderCount--;

            // Seeker bekommt Score fuer Fund
            GametypeEventResult result = new()
            {
                ClientScoreDelta = 1,
                ClientScoreTargetId = killerClientId
            };

            Debug.Log($"[HideAndSeek] Hider {victimClientId} gefunden! Verbleibende Hider: {AliveHiderCount}");

            // Alle Hider gefunden → Seeker gewinnen
            if (AliveHiderCount <= 0)
            {
                result.BlueTeamScoreDelta = 1;
                result.RestartRound = true;
                result.RestartDelaySeconds = 5f;
                result.BroadcastMessage = "Alle Hider gefunden! Seeker gewinnen die Runde!";
                CurrentPhase = HideAndSeekPhase.RoundOver;
                Debug.Log("[HideAndSeek] Alle Hider eliminiert! Seeker gewinnen.");
            }

            return result;
        }

        /// <inheritdoc />
        public override GametypeEventResult OnTeamEliminated(GametypeTeam eliminatedTeam)
        {
            if (CurrentPhase == HideAndSeekPhase.RoundOver)
            {
                return GametypeEventResult.None;
            }

            // Hider-Team komplett eliminiert → Seeker gewinnen
            if (eliminatedTeam == GametypeTeam.Red)
            {
                CurrentPhase = HideAndSeekPhase.RoundOver;
                return new GametypeEventResult
                {
                    BlueTeamScoreDelta = 1,
                    RestartRound = true,
                    RestartDelaySeconds = 5f,
                    BroadcastMessage = "Alle Hider eliminiert! Seeker gewinnen!"
                };
            }

            return GametypeEventResult.None;
        }

        /// <inheritdoc />
        public override GametypeEventResult OnTimeExpired()
        {
            // Phase-Timer (OnRunFrame) lief vor dem Countdown ab → RoundOver bereits gesetzt.
            // Scoring anhand m_HidersSurvived vergeben.
            if (CurrentPhase == HideAndSeekPhase.RoundOver)
            {
                if (m_HidersSurvived)
                {
                    m_HidersSurvived = false;
                    return new GametypeEventResult
                    {
                        RedTeamScoreDelta = 1,
                        RestartRound = true,
                        RestartDelaySeconds = 5f,
                        BroadcastMessage = "Zeit abgelaufen! Hider gewinnen die Runde!",
                        AwardSurvivalKillsToTeam = GametypeTeam.Red
                    };
                }

                // Runde endete durch Eliminierung → Score bereits vergeben, nur Restart
                return new GametypeEventResult
                {
                    RestartRound = true,
                    RestartDelaySeconds = 5f,
                    BroadcastMessage = "Runde beendet!",
                };
            }

            // Countdown lief vor Phase-Timer ab → direkt scoren
            CurrentPhase = HideAndSeekPhase.RoundOver;
            return new GametypeEventResult
            {
                RedTeamScoreDelta = 1,
                RestartRound = true,
                RestartDelaySeconds = 5f,
                BroadcastMessage = "Zeit abgelaufen! Hider gewinnen die Runde!",
                AwardSurvivalKillsToTeam = GametypeTeam.Red
            };
        }

        /// <inheritdoc />
        public override uint GetRoundTimeLimit()
        {
            // Gesamte Rundenzeit = Versteckzeit + Suchzeit
            return (uint)(HideTime + SeekTime);
        }

        /// <inheritdoc />
        public override bool AllowRespawn()
        {
            // Kein Respawning in Hide and Seek
            return false;
        }

        /// <inheritdoc />
        public override float GetPhaseTimeRemaining()
        {
            return PhaseTimeRemaining;
        }

        /// <inheritdoc />
        public override void OnRoundEnd()
        {
            base.OnRoundEnd();
            CurrentPhase = HideAndSeekPhase.RoundOver;
            CleanupFences();
        }

        /// <inheritdoc />
        public override bool IsRoundLimitReached()
        {
            return RoundLimit > 0 && CurrentRound >= RoundLimit;
        }

        /// <summary>
        /// Gibt zurueck ob Seeker in der aktuellen Phase eingefroren sein sollen.
        /// True waehrend der Versteckphase.
        /// </summary>
        public bool AreSeekersFrozen()
        {
            return CurrentPhase == HideAndSeekPhase.Hiding;
        }

        /// <summary>
        /// Gibt zurueck ob Hider Waffen haben duerfen (aus Server-Config).
        /// </summary>
        public bool HidersHaveWeapons()
        {
            return ServerConfig.hideandseek_hiderweapons;
        }

        /// <summary>
        /// Gibt zurueck ob Seeker Waffen haben duerfen (aus Server-Config).
        /// </summary>
        public bool SeekersHaveWeapons()
        {
            return ServerConfig.hideandseek_seekerweapons;
        }

        /// <inheritdoc />
        public override GametypeTeam AssignTeam(int currentRedCount, int currentBlueCount)
        {
            // Seeker (Blau) zuerst bis SeekerCount erreicht, dann Hider (Rot)
            if (currentBlueCount < SeekerCount)
            {
                return GametypeTeam.Blue;
            }

            return GametypeTeam.Red;
        }

        /// <inheritdoc />
        public override string[] GetStartWeapons(GametypeTeam team)
        {
            // Alle Spieler starten nur mit Knife
            return new[] { "knife" };
        }

        /// <inheritdoc />
        public override (int clip, int reserve, int altClip, int altReserve)? GetStartAmmo(string weaponName)
        {
            // HideAndSeek: Stark limitierte Munition fuer alle Waffen
            // knife: 3 Alt-Ammo (Wurfmesser), kein Primary
            // m4: 3 Primary Clip + 2 Alt-Ammo (Granatwerfer)
            return weaponName switch
            {
                "knife" => (1, 3, 0, 0),
                "m4" => (3, 0, 12, 0),
                _ => (3, 0, 0, 0),
            };
        }

        /// <inheritdoc />
        public override int GetCurrentPhase()
        {
            return (int)CurrentPhase;
        }

        /// <inheritdoc />
        public override void InitializeRoundState(int redCount, int blueCount)
        {
            AliveHiderCount = redCount;
            Debug.Log($"[HideAndSeek] InitializeRoundState: {blueCount} Seeker, {redCount} Hider (AliveHiderCount={AliveHiderCount})");
        }

        /// <inheritdoc />
        public override bool AreTeamsReady(int currentRedCount, int currentBlueCount)
        {
            // HideAndSeek benoetigt mindestens 1 Seeker (Blau) UND 1 Hider (Rot)
            return currentBlueCount >= 1 && currentRedCount >= 1;
        }

        /// <inheritdoc />
        public override int GetRoundLimit()
        {
            return RoundLimit;
        }

        /// <summary>Stun-Dauer in Sekunden wenn ein Hider einen Seeker mit dem Messer trifft.</summary>
        const float STUN_DURATION = 2f;

        /// <inheritdoc />
        public override GametypeDamageResult OnDamage(ulong attackerClientId, ulong victimClientId, GametypeTeam attackerTeam, GametypeTeam victimTeam, int damage, string weaponName, bool isAltAttack = false)
        {
            // Nur waehrend der Suchphase relevant
            if (CurrentPhase != HideAndSeekPhase.Seeking)
            {
                return GametypeDamageResult.Default(0);
            }

            // M4 Primary macht in HideAndSeek keinen Schaden, nur Stun (egal welches Team).
            // M4 Alt-Attack spawnt Kaefig — kein Stun, kein Schaden.
            if (weaponName == "m4")
            {
                if (isAltAttack)
                {
                    return new GametypeDamageResult
                    {
                        ModifiedDamage = 0,
                        ApplyStun = false,
                        StunDuration = 0f,
                    };
                }

                return new GametypeDamageResult
                {
                    ModifiedDamage = 0,
                    ApplyStun = true,
                    StunDuration = STUN_DURATION,
                    AttackerMessage = "You stunned {victimName}",
                    VictimMessage = "You got stunned by {attackerName}"
                };
            }

            // Knife Alt-Attack (Wurf) macht keinen Schaden, nur Stun (egal welches Team)
            if (weaponName == "knife" && isAltAttack)
            {
                return new GametypeDamageResult
                {
                    ModifiedDamage = 0,
                    ApplyStun = true,
                    StunDuration = STUN_DURATION,
                    AttackerMessage = "You stunned {victimName}",
                    VictimMessage = "You got stunned by {attackerName}"
                };
            }

            // Seeker (Blue) trifft Hider (Red) → Instant Kill (volle HP als Schaden)
            if (attackerTeam == GametypeTeam.Blue && victimTeam == GametypeTeam.Red)
            {
                return new GametypeDamageResult
                {
                    ModifiedDamage = 1000,
                    ApplyStun = false,
                    StunDuration = 0f,
                    AttackerMessage = "You killed {victimName}!",
                    VictimMessage = "You got killed by {attackerName}"
                };
            }

            // Hider (Red) trifft Seeker (Blue) → Kein Schaden, Stun stattdessen
            if (attackerTeam == GametypeTeam.Red && victimTeam == GametypeTeam.Blue)
            {
                return new GametypeDamageResult
                {
                    ModifiedDamage = 0,
                    ApplyStun = true,
                    StunDuration = STUN_DURATION,
                    AttackerMessage = "You stunned {victimName}",
                    VictimMessage = "You got stunned by {attackerName}"
                };
            }

            return GametypeDamageResult.Default(damage);
        }

        /// <summary>Addressable-Key fuer Eckstuecke (fungieren gleichzeitig als Zaun-Segment).</summary>
        private const string CORNER_PREFAB_KEY = "objects/fence_corner";

        /// <summary>Halbe Kaefig-Seitenlaenge in Metern (Aussenrand).</summary>
        private const float CAGE_HALF_SIZE = 5f;

        /// <summary>Lebensdauer des Kaefigs in Sekunden.</summary>
        private const float FENCE_DURATION = 15f;

        /// <summary>
        /// Y-Rotations-Offset fuer Corner-Segmente (SoF2→Unity Koordinaten-Korrektur).
        /// Anpassen falls Meshes nach Import anders orientiert sind.
        /// </summary>
        private const float CORNER_Y_ROTATION_OFFSET = -90f;

        /// <summary>Alle gespawnten Fence-NetworkObjects fuer Cleanup bei Rundenende.</summary>
        private readonly List<NetworkObject> m_SpawnedFences = new();

        /// <inheritdoc />
        public override void OnProjectileDetonated(string weaponName, bool isAltAttack, Vector3 position, Vector3 normal)
        {
            // Nur M4 Alt-Attack spawnt einen Kaefig
            if (weaponName != "m4" || !isAltAttack)
            {
                return;
            }

            SpawnFenceCage(position);
        }

        /// <summary>
        /// Spawnt einen Fence-Kaefig aus 4 Corner-Stuecken um die angegebene Position.
        /// Corner-Prefabs muessen FenceBarrier + NetworkObject Komponenten im Prefab haben,
        /// damit NGO sie korrekt auf den Client repliziert.
        /// </summary>
        /// <param name="center">Mittelpunkt des Kaefigs.</param>
        private void SpawnFenceCage(Vector3 center)
        {
            PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
            if (prefabManager == null)
            {
                Debug.LogWarning("[HideAndSeek] PrefabManager nicht verfuegbar — Kaefig-Spawn uebersprungen.");
                return;
            }

            GameObject cornerPrefab = prefabManager.LoadPrefab<GameObject>(CORNER_PREFAB_KEY);

            if (cornerPrefab == null)
            {
                Debug.LogWarning("[HideAndSeek] Corner-Prefab nicht gefunden.");
                return;
            }

            // co = Offset der Eckstueck-Mittelpunkte vom Kaefig-Zentrum
            float cornerWidth = GetSegmentWidth(cornerPrefab);
            float co = CAGE_HALF_SIZE - cornerWidth * 0.5f;

            // 4 Eckstuecke — Basis-Rotation + Offset fuer SoF2→Unity Korrektur
            float cOff = CORNER_Y_ROTATION_OFFSET;
            SpawnCagePiece(cornerPrefab, center + new Vector3(-co, 0f, +co), 270f + cOff);  // NW
            SpawnCagePiece(cornerPrefab, center + new Vector3(+co, 0f, +co), 0f + cOff);    // NE
            SpawnCagePiece(cornerPrefab, center + new Vector3(+co, 0f, -co), 90f + cOff);   // SE
            SpawnCagePiece(cornerPrefab, center + new Vector3(-co, 0f, -co), 180f + cOff);  // SW

            Debug.Log($"[HideAndSeek] Fence-Kaefig gespawnt bei {center} | 4 Corners | Dauer={FENCE_DURATION}s");
        }

        /// <summary>
        /// Spawnt ein einzelnes Fence-/Corner-Segment als NetworkObject.
        /// WICHTIG: Das Prefab MUSS FenceBarrier + NetworkObject Komponenten haben,
        /// damit NGO sie auf dem Client repliziert (dynamisch hinzugefuegte
        /// Komponenten werden NICHT repliziert).
        /// </summary>
        /// <param name="prefab">Prefab fuer dieses Segment.</param>
        /// <param name="position">Weltposition des Segments.</param>
        /// <param name="yRotation">Y-Rotation in Grad.</param>
        private void SpawnCagePiece(GameObject prefab, Vector3 position, float yRotation)
        {
            GameObject instance = Object.Instantiate(prefab, position, Quaternion.Euler(0f, yRotation, 0f));

            NetworkObject networkObject = instance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogWarning("[HideAndSeek] Fence-Prefab hat kein NetworkObject — Segment uebersprungen.");
                Object.Destroy(instance);
                return;
            }

            FenceBarrier barrier = instance.GetComponent<FenceBarrier>();
            if (barrier == null)
            {
                Debug.LogError("[HideAndSeek] Fence-Prefab hat kein FenceBarrier-Component! " +
                               "FenceBarrier muss auf dem Prefab sein, damit NGO es auf den Client repliziert.");
                Object.Destroy(instance);
                return;
            }

            networkObject.Spawn();
            barrier.Initialize(FENCE_DURATION);
            m_SpawnedFences.Add(networkObject);
        }

        /// <summary>
        /// Ermittelt die Breite eines Fence-Segments anhand der kombinierten Renderer-Bounds
        /// aller Kinder im Prefab. Das Prefab kann aus vielen Einzelteilen bestehen
        /// (COL_, _clip, worldspawn_textures_* etc.) — alle werden zusammengefasst.
        /// Verwendet die groesste horizontale Ausdehnung (X oder Z).
        /// </summary>
        /// <param name="prefab">Das Fence-Prefab.</param>
        /// <returns>Segmentbreite in Metern.</returns>
        private float GetSegmentWidth(GameObject prefab)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }

                float width = Mathf.Max(combinedBounds.size.x, combinedBounds.size.z);
                if (width > 0.01f)
                {
                    Debug.Log($"[HideAndSeek] Fence-Segmentbreite aus {renderers.Length} Renderern ermittelt: {width}m (Bounds: {combinedBounds.size})");
                    return width;
                }
            }

            // Fallback: MeshFilter-Bounds (z.B. fuer COL_ Objekte ohne Renderer)
            MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
            if (meshFilters.Length > 0)
            {
                Bounds combinedBounds = meshFilters[0].sharedMesh.bounds;
                for (int i = 1; i < meshFilters.Length; i++)
                {
                    if (meshFilters[i].sharedMesh != null)
                    {
                        combinedBounds.Encapsulate(meshFilters[i].sharedMesh.bounds);
                    }
                }

                float width = Mathf.Max(combinedBounds.size.x, combinedBounds.size.z);
                if (width > 0.01f)
                {
                    Debug.Log($"[HideAndSeek] Fence-Segmentbreite aus {meshFilters.Length} MeshFiltern ermittelt: {width}m (Bounds: {combinedBounds.size})");
                    return width;
                }
            }

            Debug.LogWarning("[HideAndSeek] Keine Bounds ermittelbar — verwende Fallback-Breite 1m.");
            return 1f;
        }

        /// <summary>
        /// Despawnt alle noch existierenden Fence-Segmente (Cleanup bei Rundenende).
        /// </summary>
        private void CleanupFences()
        {
            foreach (NetworkObject fence in m_SpawnedFences)
            {
                if (fence != null && fence.IsSpawned)
                {
                    fence.Despawn();
                }
            }

            m_SpawnedFences.Clear();
        }
    }
}
