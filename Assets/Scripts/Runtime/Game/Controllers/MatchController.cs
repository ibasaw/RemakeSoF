using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Networked;
using Tolik.RemakeSoF.Runtime.CrosshairManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using Tolik.RemakeSoF.Runtime.GametypeManagement;
using Tolik.RemakeSoF.Runtime.SoundManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;

namespace Tolik.RemakeSoF.Runtime
{
    internal class MatchController : Controller<GameApplication>
    {
        MatchView View => App.View.Match;
        ScoreboardView ScoreboardView => App.View.Scoreboard;

        /// <summary>
        /// Intervall in Sekunden zwischen FPS-Updates in der View.
        /// </summary>
        private const float k_FpsUpdateInterval = 0.5f;

        /// <summary>
        /// Intervall in Sekunden zwischen Debug-HUD-Updates.
        /// </summary>
        private const float k_DebugHudUpdateInterval = 0.1f;

        /// <summary>
        /// Akkumulierte Zeit seit letztem FPS-Update.
        /// </summary>
        private float m_FpsTimer;

        /// <summary>
        /// Anzahl gerendeter Frames seit letztem FPS-Update.
        /// </summary>
        private int m_FrameCount;

        /// <summary>
        /// Akkumulierte Zeit seit letztem Debug-HUD-Update.
        /// </summary>
        private float m_DebugHudTimer;

        /// <summary>
        /// Ob das Debug-HUD sichtbar ist. Im Inspector umschaltbar.
        /// </summary>
        [SerializeField]
        private bool m_ShowDebugHud = false;

        /// <summary>
        /// Gecachte CharacterState-Referenz fuer Event-Subscriptions (Ammo/Weapon/Health HUD).
        /// </summary>
        private NetworkedCharacterState m_CharacterState;

        /// <summary>
        /// Gecachte PlayerCharacter-Referenz fuer HUD Weapon-Swap Prediction.
        /// </summary>
        private ClientPlayerCharacter m_PlayerCharacter;

        /// <summary>
        /// Client-seitiger Ammo-Cache pro Waffe. Wird bei jedem OnAmmoChanged/OnWeaponChangedHud
        /// aktualisiert, damit Weapon-Swap-Prediction korrekte Werte anzeigt statt StartClip/StartReserve.
        /// </summary>
        private readonly Dictionary<string, (int clip, int reserve)> m_ClientAmmoCache = new();

        /// <summary>
        /// SoF2-Soundpfad fuer den Countdown-Beep (3, 2, 1).
        /// </summary>
        private const string k_CountdownBeepSound = "sound/misc/c4/beep.mp3";

        /// <summary>
        /// SoF2-Soundpfad fuer das "GO!"-Signal.
        /// </summary>
        private const string k_GoSound = "sound/radio/male/move.mp3";

        /// <summary>
        /// Laufende Coroutine fuer das kurzfristige "GO!"-Overlay nach Countdown.
        /// </summary>
        private Coroutine m_GoTextCoroutine;

        /// <summary>
        /// Gecachte NetworkedPlayerCharacter-Referenz fuer Hit-Confirm-Events.
        /// </summary>
        private NetworkedPlayerCharacter m_NetworkedPlayerCharacter;

        /// <summary>
        /// Laufende Coroutine fuer das kurzfristige Hit-Confirm-Overlay.
        /// </summary>
        private Coroutine m_HitConfirmCoroutine;

        /// <summary>
        /// Dauer in Sekunden, wie lange die Hit-Confirmation angezeigt wird.
        /// </summary>
        private const float k_HitConfirmDisplayDuration = 1.0f;

        /// <summary>
        /// Dauer in Sekunden, wie lange eine Gametype-Nachricht angezeigt wird.
        /// </summary>
        private const float k_GametypeMessageDisplayDuration = 3.0f;

        /// <summary>
        /// Laufende Coroutine fuer das kurzfristige Gametype-Nachrichten-Overlay.
        /// </summary>
        private Coroutine m_GametypeMessageCoroutine;

        /// <summary>
        /// Ob der lokale Spieler aktuell gestunnt ist (fuer Status-Anzeige).
        /// </summary>
        private bool m_IsStunned;

        /// <summary>
        /// Ob der lokale Spieler aktuell als Seeker in der Hiding-Phase eingefroren ist.
        /// Wird bei OnMatchStarted gesetzt und bei Phase-Wechsel (Hiding→Seeking) aufgeloest.
        /// </summary>
        private bool m_SeekerFrozen;

        void Awake()
        {
            App.Model.Countdown.OnValueChanged += OnCountdownChanged;
            App.Model.PlayersConnected.OnValueChanged += OnPlayersConnectedChanged;
            App.Model.NetworkedGameState.OnMatchStarted += OnMatchStarted;
            App.Model.NetworkedGameState.OnMatchEnded += OnMatchEnded;
            App.Model.NetworkedGameState.OnRoundStarting += OnRoundStarting;
            App.Model.NetworkedGameState.roundStartCountdown.OnValueChanged += OnRoundStartCountdownChanged;
            App.Model.NetworkedGameState.waitingForPlayers.OnValueChanged += OnWaitingForPlayersChanged;
            App.Model.NetworkedGameState.warmupCountdown.OnValueChanged += OnWarmupCountdownChanged;
            App.Model.NetworkedGameState.redTeamScore.OnValueChanged += OnTeamScoreChanged;
            App.Model.NetworkedGameState.blueTeamScore.OnValueChanged += OnTeamScoreChanged;
            App.Model.NetworkedGameState.gametypePhase.OnValueChanged += OnGametypePhaseChanged;
            App.Model.NetworkedGameState.phaseTimeRemaining.OnValueChanged += OnPhaseTimeRemainingChanged;
            App.Model.NetworkedGameState.currentRound.OnValueChanged += OnRoundChanged;
            App.Model.NetworkedGameState.roundLimit.OnValueChanged += OnRoundChanged;
            AddListener<ScoreboardShowEvent>(OnScoreboardShow);
            AddListener<ScoreboardHideEvent>(OnScoreboardHide);
            View.OnViewEnabled += OnMatchViewEnabled;
            Debug.Log("MatchController Awake: Listeners added to NetworkedGameState events.");
        }

        void OnDestroy()
        {
            View.OnViewEnabled -= OnMatchViewEnabled;
            RemoveListeners();
            UnsubscribeFromCharacterState();
            Debug.Log("MatchController OnDestroy: Listeners removed from NetworkedGameState events.");
        }

        internal override void RemoveListeners()
        {
            App.Model.Countdown.OnValueChanged -= OnCountdownChanged;
            App.Model.PlayersConnected.OnValueChanged -= OnPlayersConnectedChanged;
            App.Model.NetworkedGameState.OnMatchStarted -= OnMatchStarted;
            App.Model.NetworkedGameState.OnMatchEnded -= OnMatchEnded;
            App.Model.NetworkedGameState.OnRoundStarting -= OnRoundStarting;
            App.Model.NetworkedGameState.roundStartCountdown.OnValueChanged -= OnRoundStartCountdownChanged;
            App.Model.NetworkedGameState.waitingForPlayers.OnValueChanged -= OnWaitingForPlayersChanged;
            App.Model.NetworkedGameState.warmupCountdown.OnValueChanged -= OnWarmupCountdownChanged;
            App.Model.NetworkedGameState.redTeamScore.OnValueChanged -= OnTeamScoreChanged;
            App.Model.NetworkedGameState.blueTeamScore.OnValueChanged -= OnTeamScoreChanged;
            App.Model.NetworkedGameState.gametypePhase.OnValueChanged -= OnGametypePhaseChanged;
            App.Model.NetworkedGameState.phaseTimeRemaining.OnValueChanged -= OnPhaseTimeRemainingChanged;
            App.Model.NetworkedGameState.currentRound.OnValueChanged -= OnRoundChanged;
            App.Model.NetworkedGameState.roundLimit.OnValueChanged -= OnRoundChanged;
            RemoveListener<ScoreboardShowEvent>(OnScoreboardShow);
            RemoveListener<ScoreboardHideEvent>(OnScoreboardHide);
        }

        /// <summary>
        /// Verbindet sich mit dem CharacterState des lokalen Spielers fuer HUD-Events.
        /// Wird lazy in Update aufgerufen, sobald PlayerCharacter verfuegbar ist.
        /// </summary>
        private void SubscribeToCharacterState()
        {
            ClientPlayerCharacter player = App.Model.PlayerCharacter;
            if (player == null || player.CharacterState == null)
            {
                return;
            }

            m_PlayerCharacter = player;
            m_CharacterState = player.CharacterState;
            m_CharacterState.OnAmmoChanged += OnAmmoChanged;
            m_CharacterState.OnAltAmmoChanged += OnAltAmmoChanged;
            m_CharacterState.OnWeaponChanged += OnWeaponChangedHud;
            m_CharacterState.OnHealthChanged += OnHealthChanged;
            m_CharacterState.OnKillsChanged += OnKillsChanged;
            m_CharacterState.OnDeathsChanged += OnDeathsChanged;
            m_CharacterState.OnIsAliveChanged += OnIsAliveChanged;
            m_PlayerCharacter.OnWeaponSwapRaiseStarted += OnWeaponSwapRaiseStarted;
            m_PlayerCharacter.OnWeaponSwapTargetChanged += OnWeaponSwapTargetChanged;
            m_PlayerCharacter.OnFireModeChanged += OnFireModeChanged;

            // Hit-Confirmation vom NetworkedPlayerCharacter abonnieren
            m_NetworkedPlayerCharacter = player.GetComponent<NetworkedPlayerCharacter>();
            if (m_NetworkedPlayerCharacter != null)
            {
                m_NetworkedPlayerCharacter.OnHitConfirmed += OnHitConfirmed;
                m_NetworkedPlayerCharacter.OnGametypeMessage += OnGametypeMessage;
                m_NetworkedPlayerCharacter.OnStunnedChanged += OnStunnedChanged;
            }

            // Sofort aus aktuellem State initialisieren (falls Werte schon da sind).
            TryInitializeHudFromCurrentState();
        }

        /// <summary>
        /// Trennt die Verbindung zum CharacterState.
        /// </summary>
        private void UnsubscribeFromCharacterState()
        {
            if (m_CharacterState == null)
            {
                return;
            }

            m_CharacterState.OnAmmoChanged -= OnAmmoChanged;
            m_CharacterState.OnAltAmmoChanged -= OnAltAmmoChanged;
            m_CharacterState.OnWeaponChanged -= OnWeaponChangedHud;
            m_CharacterState.OnHealthChanged -= OnHealthChanged;
            m_CharacterState.OnKillsChanged -= OnKillsChanged;
            m_CharacterState.OnDeathsChanged -= OnDeathsChanged;
            m_CharacterState.OnIsAliveChanged -= OnIsAliveChanged;

            if (m_PlayerCharacter != null)
            {
                m_PlayerCharacter.OnWeaponSwapRaiseStarted -= OnWeaponSwapRaiseStarted;
                m_PlayerCharacter.OnWeaponSwapTargetChanged -= OnWeaponSwapTargetChanged;
                m_PlayerCharacter.OnFireModeChanged -= OnFireModeChanged;
                m_PlayerCharacter = null;
            }

            if (m_NetworkedPlayerCharacter != null)
            {
                m_NetworkedPlayerCharacter.OnHitConfirmed -= OnHitConfirmed;
                m_NetworkedPlayerCharacter.OnGametypeMessage -= OnGametypeMessage;
                m_NetworkedPlayerCharacter.OnStunnedChanged -= OnStunnedChanged;
                m_NetworkedPlayerCharacter = null;
            }

            m_CharacterState = null;
        }

        void OnCountdownChanged(uint previousValue, uint newValue)
        {
            View.OnCountdownChanged(newValue);
        }

        void OnPlayersConnectedChanged(int previousValue, int newValue)
        {
            int maxPlayers = App.Model.NetworkedGameState.MaxPlayers > 0 ? App.Model.NetworkedGameState.MaxPlayers : 16;
            View.OnPlayersConnectedChanged(newValue, maxPlayers);
        }

        void OnMatchEnded()
        {
            Broadcast(new EndMatchEvent());
            Debug.Log("[MatchController] Match ended, broadcasting EndMatchEvent.");
        }

        /// <summary>
        /// Wird aufgerufen wenn der Server den Round-Start-Countdown beginnt.
        /// Deaktiviert Spielerinput und zeigt den Countdown in der View.
        /// </summary>
        void OnRoundStarting()
        {
            if (App.Model.PlayerCharacter != null)
            {
                App.Model.PlayerCharacter.SetMovementFrozen(true);
            }

            uint countdownValue = App.Model.NetworkedGameState.roundStartCountdown.Value;
            View.ShowRoundStartCountdown(countdownValue);
            PlayUiSound(k_CountdownBeepSound);
        }

        /// <summary>
        /// Aktualisiert das Countdown-Overlay bei jeder Aenderung der NetworkVariable.
        /// </summary>
        void OnRoundStartCountdownChanged(uint previousValue, uint newValue)
        {
            if (newValue > 0)
            {
                View.ShowRoundStartCountdown(newValue);
                PlayUiSound(k_CountdownBeepSound);
            }
        }

        /// <summary>
        /// Zeigt oder versteckt die "Waiting for players..." Meldung.
        /// </summary>
        void OnWaitingForPlayersChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                View.ShowWaitingMessage("Waiting for more Players to start round...");
            }
            else
            {
                View.HideWaitingMessage();
            }
        }

        /// <summary>
        /// Zeigt den Warmup-Countdown ("Round starts in X...") oder versteckt die Meldung.
        /// </summary>
        void OnWarmupCountdownChanged(uint previousValue, uint newValue)
        {
            if (newValue > 0)
            {
                View.ShowWaitingMessage($"Round starts in {newValue}...");
            }
            else
            {
                View.HideWaitingMessage();
            }
        }

        /// <summary>
        /// Zeigt das Scoreboard an (Tab-Taste gedrueckt).
        /// </summary>
        void OnScoreboardShow(ScoreboardShowEvent evt)
        {
            ScoreboardView?.ShowScoreboard();
        }

        /// <summary>
        /// Versteckt das Scoreboard (Tab-Taste losgelassen).
        /// </summary>
        void OnScoreboardHide(ScoreboardHideEvent evt)
        {
            ScoreboardView?.HideScoreboard();
        }

        void OnMatchStarted()
        {
            // HideAndSeek Seeker-Freeze: Waehrend der Hiding-Phase bleibt Input fuer Seeker deaktiviert
            bool isSeekerInHideAndSeek = IsLocalPlayerSeekerInHideAndSeek();
            int currentPhase = App.Model.NetworkedGameState.gametypePhase.Value;

            if (isSeekerInHideAndSeek && currentPhase == (int)HideAndSeekPhase.Hiding)
            {
                // Seeker: Movement-Freeze explizit setzen (Safety — OnRoundStarting hat es bereits gesetzt,
                // aber bei Timing-Problemen koennte es fehlen). Warmup-Countdown anzeigen statt "GO!".
                m_SeekerFrozen = true;
                if (App.Model.PlayerCharacter != null)
                {
                    App.Model.PlayerCharacter.SetMovementFrozen(true);
                }
                uint phaseTime = App.Model.NetworkedGameState.phaseTimeRemaining.Value;
                View.ShowRoundStartCountdown(phaseTime);
                Debug.Log($"[MatchController] Seeker frozen during Hiding phase. Warmup: {phaseTime}s");
                return;
            }

            m_SeekerFrozen = false;

            // "GO!" anzeigen, dann nach kurzer Verzoegerung ausblenden
            View.ShowGoText();
            PlayUiSound(k_GoSound);

            if (m_GoTextCoroutine != null)
            {
                StopCoroutine(m_GoTextCoroutine);
            }
            m_GoTextCoroutine = StartCoroutine(HideGoTextAfterDelay());

            if (App.Model.PlayerCharacter != null)
            {
                App.Model.PlayerCharacter.SetMovementFrozen(false);
            }

            Broadcast(new StartMatchEvent());
            Debug.Log("[MatchController] Match started, broadcasting StartMatchEvent.");
        }

        /// <summary>
        /// Blendet den "GO!"-Text nach einer Sekunde aus.
        /// </summary>
        private IEnumerator HideGoTextAfterDelay()
        {
            yield return CoroutinesHelper.OneSecond;
            View.HideRoundStartCountdown();
            m_GoTextCoroutine = null;
        }

        /// <summary>
        /// Reagiert auf Gametype-Phase-Wechsel (z.B. HideAndSeek: Hiding→Seeking).
        /// Wenn Seeker eingefroren war und die Seeking-Phase beginnt: Input freigeben + "GO!" anzeigen.
        /// </summary>
        void OnGametypePhaseChanged(int previousValue, int newValue)
        {
            // Seeker-Freeze aufloesen wenn Hiding-Phase endet
            if (m_SeekerFrozen && newValue == (int)HideAndSeekPhase.Seeking)
            {
                m_SeekerFrozen = false;

                // "GO!" anzeigen + Sound
                View.ShowGoText();
                PlayUiSound(k_GoSound);

                if (m_GoTextCoroutine != null)
                {
                    StopCoroutine(m_GoTextCoroutine);
                }
                m_GoTextCoroutine = StartCoroutine(HideGoTextAfterDelay());

                // Input freigeben
                if (App.Model.PlayerCharacter != null)
                {
                    App.Model.PlayerCharacter.SetMovementFrozen(false);
                }

                Broadcast(new StartMatchEvent());
                Debug.Log("[MatchController] Seeker unfrozen — Seeking phase started!");
            }
        }

        /// <summary>
        /// Aktualisiert den Seeker-Warmup-Countdown in der UI waehrend der Hiding-Phase.
        /// Nur relevant wenn der lokale Spieler als Seeker eingefroren ist.
        /// </summary>
        void OnPhaseTimeRemainingChanged(uint previousValue, uint newValue)
        {
            if (!m_SeekerFrozen)
            {
                return;
            }

            if (newValue > 0)
            {
                View.ShowRoundStartCountdown(newValue);
            }
        }

        /// <summary>
        /// Prueft ob der lokale Spieler ein Seeker (Blue) im HideAndSeek-Gametype ist.
        /// </summary>
        private bool IsLocalPlayerSeekerInHideAndSeek()
        {
            string gametypeId = App.Model.NetworkedGameState.activeGametypeId.Value.ToString();
            if (gametypeId != "hideandseek")
            {
                return false;
            }

            ClientPlayerCharacter player = App.Model.PlayerCharacter;
            if (player == null || player.CharacterState == null)
            {
                return false;
            }

            return (GametypeTeam)player.CharacterState.TeamId == GametypeTeam.Blue;
        }

        /// <summary>
        /// Spielt einen 2D-UI-Sound ueber den SFX-Mixer ab.
        /// Erstellt ein temporaeres GameObject mit AudioSource, das nach Abspielen zerstoert wird.
        /// </summary>
        private void PlayUiSound(string soundKey)
        {
            SoundManager soundManager = ServiceLocator.Get<SoundManager>();
            if (soundManager == null)
            {
                return;
            }

            AudioClip clip = soundManager.GetClip(soundKey);
            if (clip == null)
            {
                return;
            }

            GameObject soundObj = new("UiSound");
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.playOnAwake = false;

            if (soundManager.SfxGroup != null)
            {
                source.outputAudioMixerGroup = soundManager.SfxGroup;
            }

            source.Play();
            Destroy(soundObj, clip.length + 0.1f);
        }

        /// <summary>
        /// Callback wenn die MatchView sichtbar wird (OnEnable).
        /// Initialisiert den HUD erneut aus dem aktuellen CharacterState,
        /// damit UXML-Defaults nach Hide/Show nicht stehen bleiben.
        /// </summary>
        private void OnMatchViewEnabled()
        {
            // Debug-HUD Sichtbarkeit anhand des SerializeField-Flags setzen.
            View.SetDebugHudVisible(m_ShowDebugHud);

            // Crosshair aus Default-Definition aufbauen.
            InitializeCrosshair();

            // Team-Logo-Texturen laden und auf HUD anwenden.
            LoadTeamLogoTextures();

            // Bigchars-Atlas fuer QuakeColorLabel-Nachrichten laden.
            LoadBigcharsAtlas();

            // Falls CharacterState noch nicht verfuegbar, jetzt versuchen.
            if (m_CharacterState == null && App.Model.PlayerCharacter != null)
            {
                SubscribeToCharacterState();
            }

            TryInitializeHudFromCurrentState();

            // Waiting/Warmup-Status aus aktuellem NetworkVariable-Wert initialisieren,
            // da OnValueChanged nicht fuer den initialen Wert feuert.
            InitializeWaitingState();

            // PlayersConnected aus aktuellem NetworkVariable-Wert initialisieren.
            int maxPlayers = App.Model.NetworkedGameState.MaxPlayers > 0 ? App.Model.NetworkedGameState.MaxPlayers : 16;
            View.OnPlayersConnectedChanged(App.Model.PlayersConnected.Value, maxPlayers);

            // Runden-Anzeige aus aktuellem NetworkVariable-Wert initialisieren.
            UpdateRoundDisplay();
        }

        /// <summary>
        /// Laedt die Team-Logo-Texturen und wendet sie auf die HUD-Score-Icons an.
        /// </summary>
        private void LoadTeamLogoTextures()
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                return;
            }

            TextureConfiguration.ScoreboardTextures config = textureManager.Configuration?.scoreboard;
            if (config == null)
            {
                return;
            }

            Texture2D redLogo = null;
            Texture2D blueLogo = null;

            if (!string.IsNullOrEmpty(config.teamRedLogo))
            {
                TextureData redData = textureManager.GetTextureData(config.teamRedLogo);
                if (redData?.Texture != null)
                {
                    redLogo = redData.Texture;
                }
            }

            if (!string.IsNullOrEmpty(config.teamBlueLogo))
            {
                TextureData blueData = textureManager.GetTextureData(config.teamBlueLogo);
                if (blueData?.Texture != null)
                {
                    blueLogo = blueData.Texture;
                }
            }

            View.ApplyTeamLogoTextures(redLogo, blueLogo);
        }

        /// <summary>
        /// Laedt die Bigchars-Atlas-Textur fuer QuakeColorLabel-Darstellung von Gametype-Nachrichten.
        /// </summary>
        private void LoadBigcharsAtlas()
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                return;
            }

            TextureConfiguration.ScoreboardTextures config = textureManager.Configuration?.scoreboard;
            if (config == null || string.IsNullOrEmpty(config.bigcharsAtlas))
            {
                return;
            }

            TextureData atlasData = textureManager.GetTextureData(config.bigcharsAtlas);
            if (atlasData?.Texture != null)
            {
                View.SetBigcharsAtlas(atlasData.Texture);
            }
        }

        /// <summary>
        /// Callback fuer Gametype-Nachrichten (z.B. Stun-Benachrichtigungen).
        /// Zeigt die Nachricht als QuakeColorLabel im HUD an und blendet sie nach einer Verzoegerung aus.
        /// </summary>
        private void OnGametypeMessage(string message)
        {
            View.ShowGametypeMessage(message);

            if (m_GametypeMessageCoroutine != null)
            {
                StopCoroutine(m_GametypeMessageCoroutine);
            }
            m_GametypeMessageCoroutine = StartCoroutine(HideGametypeMessageAfterDelay());
        }

        /// <summary>
        /// Blendet die Gametype-Nachricht nach einer kurzen Verzoegerung aus.
        /// </summary>
        private IEnumerator HideGametypeMessageAfterDelay()
        {
            yield return new WaitForSeconds(k_GametypeMessageDisplayDuration);
            View.HideGametypeMessage();
            m_GametypeMessageCoroutine = null;
        }

        /// <summary>
        /// Initialisiert die Waiting/Warmup-Anzeige aus den aktuellen NetworkVariable-Werten.
        /// Behebt Race-Condition wenn Server den Wert setzt bevor der Client subscribed.
        /// </summary>
        private void InitializeWaitingState()
        {
            NetworkedGameState gameState = App.Model.NetworkedGameState;

            uint warmup = gameState.warmupCountdown.Value;
            if (warmup > 0)
            {
                View.ShowWaitingMessage($"Round starts in {warmup}...");
                return;
            }

            bool waiting = gameState.waitingForPlayers.Value;
            if (waiting)
            {
                View.ShowWaitingMessage("Waiting for more Players to start round...");
                return;
            }

            View.HideWaitingMessage();
        }

        /// <summary>
        /// Initialisiert den HUD aus dem aktuellen CharacterState.
        /// Wird nach Subscribe und beim View-OnEnable aufgerufen,
        /// um Reihenfolge-Races zwischen Spawn, Delta-Events und View-Visibility abzufangen.
        /// </summary>
        private void TryInitializeHudFromCurrentState()
        {
            if (m_CharacterState == null)
            {
                return;
            }

            string weapon = m_CharacterState.CurrentWeaponName;
            if (!string.IsNullOrEmpty(weapon))
            {
                UpdateWeaponHud(weapon, m_CharacterState.CurrentClipAmmo, m_CharacterState.ReserveAmmo);
            }

            OnHealthChanged(m_CharacterState.Health);

            // Team-Anzeige initialisieren
            GametypeTeam team = (GametypeTeam)m_CharacterState.TeamId;
            View.SetYourTeam(team);

            // Kills/Deaths/Status initialisieren
            View.UpdatePlayerStats(m_CharacterState.Kills, m_CharacterState.Deaths);
            View.UpdatePlayerStatus(m_CharacterState.IsAlive);

            // Team-Score initialisieren
            UpdateTeamScoreDisplay();
        }

        /// <summary>
        /// FPS berechnen und View aktualisieren in regelmäßigen Intervallen.
        /// Debug-HUD mit Spielerdaten aktualisieren.
        /// </summary>
        private void Update()
        {
            // Lazy subscribe zum CharacterState wenn PlayerCharacter verfuegbar wird
            if (m_CharacterState == null && App.Model.PlayerCharacter != null)
            {
                SubscribeToCharacterState();
            }

            m_FrameCount++;
            m_FpsTimer += Time.unscaledDeltaTime;

            if (m_FpsTimer >= k_FpsUpdateInterval)
            {
                float fps = m_FrameCount / m_FpsTimer;
                View.OnFpsChanged(fps);
                m_FrameCount = 0;
                m_FpsTimer = 0f;
            }

            if (m_ShowDebugHud)
            {
                m_DebugHudTimer += Time.unscaledDeltaTime;
                if (m_DebugHudTimer >= k_DebugHudUpdateInterval)
                {
                    m_DebugHudTimer = 0f;
                    UpdateDebugHud();
                }
            }
        }

        /// <summary>
        /// Liest aktuelle Daten vom PlayerCharacter und aktualisiert das Debug-HUD.
        /// </summary>
        private void UpdateDebugHud()
        {
            ClientPlayerCharacter player = App.Model.PlayerCharacter;
            if (player == null)
            {
                return;
            }

            View.UpdateDebugHud(
                player.IsGrounded,
                player.IsAttacking,
                player.IsCrouching,
                player.HorizontalSpeed,
                player.VerticalSpeed,
                player.transform.position,
                player.CurrentAirtime,
                player.CurrentJumpPhaseAirtime,
                player.CurrentFallPhaseAirtime,
                player.CurrentJumpHeight,
                player.CurrentFallHeight,
                player.CurrentAirDistanceHoriz,
                player.CurrentAirDistanceVert,
                player.FullAirtime,
                player.FullJumpPhaseAirtime,
                player.FullFallPhaseAirtime,
                player.FullJumpHeight,
                player.FullFallHeight,
                player.FullAirDistanceHoriz,
                player.FullAirDistanceVert
            );

            View.UpdateBhopDisplay(
                player.HorizontalSpeed,
                player.BhopChainCount,
                player.BhopChainPeakSpeed,
                player.BhopChainDistance,
                player.LastBhopChainCount,
                player.LastBhopChainPeakSpeed,
                player.LastBhopChainDistance
            );
        }

        // ===== Weapon / Ammo / Health HUD =====

        /// <summary>
        /// Callback wenn sich die Munition aendert.
        /// </summary>
        private void OnAmmoChanged(int clipAmmo, int reserveAmmo)
        {
            string weaponName = m_CharacterState.CurrentWeaponName;
            if (!string.IsNullOrEmpty(weaponName))
            {
                m_ClientAmmoCache[weaponName] = (clipAmmo, reserveAmmo);
                UpdateWeaponHud(weaponName, clipAmmo, reserveAmmo);
            }
        }

        /// <summary>
        /// Callback wenn sich die Waffe aendert. Aktualisiert den gesamten Waffen-HUD.
        /// </summary>
        private void OnWeaponChangedHud(string weaponName)
        {
            if (m_CharacterState == null)
            {
                return;
            }

            int clip = m_CharacterState.CurrentClipAmmo;
            int reserve = m_CharacterState.ReserveAmmo;
            m_ClientAmmoCache[weaponName] = (clip, reserve);
            UpdateWeaponHud(weaponName, clip, reserve);
        }

        /// <summary>
        /// Client-Prediction Callback: Feuert bei jedem Scroll waehrend der Drop-Phase.
        /// HUD zeigt sofort den Waffennamen der Zielwaffe (SoF2-authentisch: on click update).
        /// </summary>
        private void OnWeaponSwapTargetChanged(string weaponName)
        {
            // Client-Cache hat die tatsaechlichen Werte der letzten Nutzung dieser Waffe
            if (m_ClientAmmoCache.TryGetValue(weaponName, out (int clip, int reserve) cached))
            {
                UpdateWeaponHud(weaponName, cached.clip, cached.reserve);
                return;
            }

            // Fallback: Erste Auswahl dieser Waffe, noch nie equipped → StartClip/StartReserve
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            WeaponDefinition weapon = loader.GetById(weaponName);
            int predictedClip = weapon?.Ammo?.StartClip ?? 0;
            int predictedReserve = weapon?.Ammo?.StartReserve ?? 0;

            UpdateWeaponHud(weaponName, predictedClip, predictedReserve);
        }

        /// <summary>
        /// Client-Prediction Callback: Feuert wenn der lokale Client die Drop→Raise Grenze
        /// erreicht (SoF2: PM_FinishWeaponChange). HUD zeigt sofort die neue Waffe
        /// mit vorhergesagter Munition, ohne auf den Server-Roundtrip zu warten.
        /// </summary>
        private void OnWeaponSwapRaiseStarted(string weaponName)
        {
            // Primaer: Server hat schon committed → NetworkVariable aktuell
            if (m_CharacterState != null &&
                string.Equals(m_CharacterState.CurrentWeaponName, weaponName, StringComparison.Ordinal))
            {
                int clip = m_CharacterState.CurrentClipAmmo;
                int reserve = m_CharacterState.ReserveAmmo;
                m_ClientAmmoCache[weaponName] = (clip, reserve);
                UpdateWeaponHud(weaponName, clip, reserve);
                return;
            }

            // Sekundaer: Client-Cache hat Werte der letzten Nutzung
            if (m_ClientAmmoCache.TryGetValue(weaponName, out (int clip, int reserve) cached))
            {
                UpdateWeaponHud(weaponName, cached.clip, cached.reserve);
                return;
            }

            // Fallback: Erste Auswahl, noch nie equipped
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            WeaponDefinition weapon = loader.GetById(weaponName);
            int predictedClip = weapon?.Ammo?.StartClip ?? 0;
            int predictedReserve = weapon?.Ammo?.StartReserve ?? 0;

            UpdateWeaponHud(weaponName, predictedClip, predictedReserve);
        }

        /// <summary>
        /// Aktualisiert den Waffen-HUD mit Waffenname und Ammo-Daten.
        /// Liest infinite-Flag aus dem WeaponDataLoader.
        /// </summary>
        private void UpdateWeaponHud(string weaponName, int clipAmmo, int reserveAmmo)
        {
            bool hideAmmoRow = false;
            string displayName = weaponName;
            string ammoType = "";
            bool hasAltAmmo = false;
            string altAmmoType = "";
            int altClipAmmo = 0;
            int altReserveAmmo = 0;
            Texture2D weaponIcon = null;

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader != null)
            {
                WeaponDefinition weapon = loader.GetById(weaponName);
                if (weapon != null)
                {
                    displayName = weapon.DisplayName ?? weaponName;

                    if (!string.IsNullOrEmpty(weapon.MenuImage))
                    {
                        TextureManager textureManager = ServiceLocator.Get<TextureManager>();
                        if (textureManager != null)
                        {
                            TextureData iconData = textureManager.GetTextureData(weapon.MenuImage);
                            weaponIcon = iconData?.Texture;
                        }
                    }
                }

                if (weapon?.Ammo != null)
                {
                    // Ammo-Type nur anzeigen wenn er sich vom Waffen-ID unterscheidet
                    if (!string.Equals(weapon.Ammo.Type, weapon.Id, System.StringComparison.OrdinalIgnoreCase))
                    {
                        ammoType = weapon.Ammo.Type ?? "";
                    }

                    bool altUsesMainAmmo = weapon.AltAttack?.Projectile != null && weapon.AltAttack.Ammo == null;
                    hideAmmoRow = weapon.Ammo.Infinite && !altUsesMainAmmo;
                }

                if (weapon?.AltAttack?.Ammo != null)
                {
                    hasAltAmmo = true;
                    altAmmoType = weapon.AltAttack.Ammo.Type ?? "";
                    altClipAmmo = m_CharacterState.AltClipAmmo;
                    altReserveAmmo = m_CharacterState.AltReserveAmmo;
                }
            }

            View.UpdateWeaponHud(weaponIcon, displayName, ammoType, clipAmmo, reserveAmmo, hideAmmoRow, hasAltAmmo, altAmmoType, altClipAmmo, altReserveAmmo);

            // FireMode-Anzeige aktualisieren (Waffe koennte andere verfuegbare Modi haben)
            if (m_PlayerCharacter != null)
            {
                string fireMode = m_PlayerCharacter.CurrentFireMode;
                WeaponDataLoader fmLoader = ServiceLocator.Get<WeaponDataLoader>();
                WeaponDefinition fmWeapon = fmLoader?.GetById(weaponName);
                bool hasMultipleModes = fmWeapon?.Attack?.FireModes != null && fmWeapon.Attack.FireModes.Count > 1;
                View.UpdateFireModeHud(fireMode, hasMultipleModes);
            }
        }

        /// <summary>
        /// Callback wenn sich die Alt-Munition aendert (z.B. M203 Granaten).
        /// </summary>
        private void OnAltAmmoChanged(int altClipAmmo, int altReserveAmmo)
        {
            View.UpdateAltAmmoHud(altClipAmmo, altReserveAmmo);
        }

        /// <summary>
        /// Callback wenn sich Health aendert.
        /// </summary>
        private void OnHealthChanged(int health)
        {
            View.UpdateHealthHud(health);
        }

        /// <summary>
        /// Callback wenn sich Kills aendern.
        /// </summary>
        private void OnKillsChanged(int kills)
        {
            if (m_CharacterState != null)
            {
                View.UpdatePlayerStats(kills, m_CharacterState.Deaths);
            }
        }

        /// <summary>
        /// Callback wenn sich Deaths aendern.
        /// </summary>
        private void OnDeathsChanged(int deaths)
        {
            if (m_CharacterState != null)
            {
                View.UpdatePlayerStats(m_CharacterState.Kills, deaths);
            }
        }

        /// <summary>
        /// Callback wenn sich der Alive-Status aendert.
        /// </summary>
        private void OnIsAliveChanged(bool isAlive)
        {
            if (isAlive)
            {
                m_IsStunned = false;
            }

            View.UpdatePlayerStatus(isAlive, m_IsStunned);
        }

        /// <summary>
        /// Callback wenn sich der Stun-Status aendert.
        /// </summary>
        private void OnStunnedChanged(bool isStunned)
        {
            m_IsStunned = isStunned;
            bool isAlive = m_CharacterState != null && m_CharacterState.IsAlive;
            View.UpdatePlayerStatus(isAlive, m_IsStunned);
        }

        /// <summary>
        /// Callback wenn sich ein Team-Score aendert.
        /// </summary>
        private void OnTeamScoreChanged(int oldValue, int newValue)
        {
            UpdateTeamScoreDisplay();
        }

        /// <summary>
        /// Aktualisiert die Team-Score-Anzeige basierend auf dem eigenen Team.
        /// </summary>
        private void UpdateTeamScoreDisplay()
        {
            NetworkedGameState gameState = App.Model.NetworkedGameState;
            if (gameState == null)
            {
                return;
            }

            View.UpdateTeamScore(gameState.redTeamScore.Value, gameState.blueTeamScore.Value);
        }

        /// <summary>
        /// Callback wenn sich die aktuelle Runde oder das Rundenlimit aendert.
        /// </summary>
        private void OnRoundChanged(int oldValue, int newValue)
        {
            UpdateRoundDisplay();
        }

        /// <summary>
        /// Aktualisiert die Runden-Anzeige basierend auf den aktuellen NetworkVariable-Werten.
        /// </summary>
        private void UpdateRoundDisplay()
        {
            NetworkedGameState gameState = App.Model.NetworkedGameState;
            if (gameState == null)
            {
                return;
            }

            View.UpdateRoundDisplay(gameState.currentRound.Value, gameState.roundLimit.Value);
        }

        /// <summary>
        /// Callback wenn der Feuermodus gewechselt wird.
        /// Aktualisiert die FireMode-Anzeige im Waffen-HUD.
        /// </summary>
        private void OnFireModeChanged(string fireMode)
        {
            bool hasMultipleModes = false;
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader != null && m_CharacterState != null)
            {
                WeaponDefinition weapon = loader.GetById(m_CharacterState.CurrentWeaponName);
                hasMultipleModes = weapon?.Attack?.FireModes != null && weapon.Attack.FireModes.Count > 1;
            }

            View.UpdateFireModeHud(fireMode, hasMultipleModes);
        }

        /// <summary>
        /// Callback wenn der Server einen Treffer auf einen Gegner bestaetigt.
        /// Zeigt die getroffene HitRegion und den Schaden kurzfristig als HUD-Overlay an.
        /// </summary>
        private void OnHitConfirmed(HitRegion hitRegion, int damage, bool isKill)
        {
            string regionName = FormatHitRegionName(hitRegion);
            View.ShowHitConfirm(regionName, damage, isKill);

            if (m_HitConfirmCoroutine != null)
            {
                StopCoroutine(m_HitConfirmCoroutine);
            }
            m_HitConfirmCoroutine = StartCoroutine(HideHitConfirmAfterDelay());
        }

        /// <summary>
        /// Blendet die Hit-Confirmation nach einer kurzen Verzoegerung aus.
        /// </summary>
        private IEnumerator HideHitConfirmAfterDelay()
        {
            yield return new WaitForSeconds(k_HitConfirmDisplayDuration);
            View.HideHitConfirm();
            m_HitConfirmCoroutine = null;
        }

        /// <summary>
        /// Wandelt die HitRegion-Enum in einen lesbaren Anzeigenamen um.
        /// </summary>
        private static string FormatHitRegionName(HitRegion region)
        {
            switch (region)
            {
                case HitRegion.Head: return "HEAD";
                case HitRegion.Neck: return "NECK";
                case HitRegion.Chest: return "CHEST";
                case HitRegion.Gut: return "GUT";
                case HitRegion.Groin: return "GROIN";
                case HitRegion.LeftShoulder: return "L. SHOULDER";
                case HitRegion.RightShoulder: return "R. SHOULDER";
                case HitRegion.LeftArm: return "L. ARM";
                case HitRegion.RightArm: return "R. ARM";
                case HitRegion.LeftForearm: return "L. FOREARM";
                case HitRegion.RightForearm: return "R. FOREARM";
                case HitRegion.LeftHand: return "L. HAND";
                case HitRegion.RightHand: return "R. HAND";
                case HitRegion.LeftThigh: return "L. THIGH";
                case HitRegion.RightThigh: return "R. THIGH";
                case HitRegion.LeftLeg: return "L. LEG";
                case HitRegion.RightLeg: return "R. LEG";
                case HitRegion.LeftFoot: return "L. FOOT";
                case HitRegion.RightFoot: return "R. FOOT";
                default: return region.ToString().ToUpperInvariant();
            }
        }

        /// <summary>
        /// Laedt das Default-Crosshair aus dem CrosshairDataLoader und baut es in der View auf.
        /// </summary>
        private void InitializeCrosshair()
        {
            CrosshairDataLoader crosshairLoader = ServiceLocator.Get<CrosshairDataLoader>();
            if (crosshairLoader == null)
            {
                return;
            }

            CrosshairDefinition definition = crosshairLoader.GetDefault();
            View.BuildCrosshair(definition);
        }
    }
}
