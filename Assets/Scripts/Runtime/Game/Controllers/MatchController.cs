using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Client;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.SoundManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;

namespace Tolik.RemakeSoF.Runtime
{
    internal class MatchController : Controller<GameApplication>
    {
        MatchView View => App.View.Match;

        /// <summary>
        /// Intervall in Sekunden zwischen FPS-Updates in der View.
        /// </summary>
        private const float k_FpsUpdateInterval = 0.5f;

        /// <summary>
        /// Intervall in Sekunden zwischen Debug-HUD-Updates.
        /// </summary>
        private const float k_DebugHudUpdateInterval = 0.05f;

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

        void Awake()
        {
            App.Model.Countdown.OnValueChanged += OnCountdownChanged;
            App.Model.PlayersConnected.OnValueChanged += OnPlayersConnectedChanged;
            App.Model.NetworkedGameState.OnMatchStarted += OnMatchStarted;
            App.Model.NetworkedGameState.OnMatchEnded += OnMatchEnded;
            App.Model.NetworkedGameState.OnRoundStarting += OnRoundStarting;
            App.Model.NetworkedGameState.roundStartCountdown.OnValueChanged += OnRoundStartCountdownChanged;
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
            m_PlayerCharacter.OnWeaponSwapRaiseStarted += OnWeaponSwapRaiseStarted;
            m_PlayerCharacter.OnWeaponSwapTargetChanged += OnWeaponSwapTargetChanged;
            m_PlayerCharacter.OnFireModeChanged += OnFireModeChanged;

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

            if (m_PlayerCharacter != null)
            {
                m_PlayerCharacter.OnWeaponSwapRaiseStarted -= OnWeaponSwapRaiseStarted;
                m_PlayerCharacter.OnWeaponSwapTargetChanged -= OnWeaponSwapTargetChanged;
                m_PlayerCharacter.OnFireModeChanged -= OnFireModeChanged;
                m_PlayerCharacter = null;
            }

            m_CharacterState = null;
        }

        void OnCountdownChanged(uint previousValue, uint newValue)
        {
            View.OnCountdownChanged(newValue);
        }

        void OnPlayersConnectedChanged(int previousValue, int newValue)
        {
            View.OnPlayersConnectedChanged(newValue);
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
                App.Model.PlayerCharacter.SetInputsActive(false);
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

        void OnMatchStarted()
        {
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
                App.Model.PlayerCharacter.SetInputsActive(true);
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
            // Falls CharacterState noch nicht verfuegbar, jetzt versuchen.
            if (m_CharacterState == null && App.Model.PlayerCharacter != null)
            {
                SubscribeToCharacterState();
            }

            TryInitializeHudFromCurrentState();
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

            m_DebugHudTimer += Time.unscaledDeltaTime;
            if (m_DebugHudTimer >= k_DebugHudUpdateInterval)
            {
                m_DebugHudTimer = 0f;
                UpdateDebugHud();
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
    }
}
