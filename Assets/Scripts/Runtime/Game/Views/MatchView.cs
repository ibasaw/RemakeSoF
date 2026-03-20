using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime
{
    [UnityEngine.RequireComponent(typeof(UIDocument))]

    internal class MatchView : View<GameApplication>
    {
        UIDocument m_UIDocument;
        Label m_TimerLabel;
        Label m_PlayersConnectedLabel;
        Label m_FpsLabel;

        // Debug HUD Labels
        Label m_GroundedLabel;
        Label m_AttackingLabel;
        Label m_CrouchingLabel;
        Label m_HorizSpeedLabel;
        Label m_VertSpeedLabel;
        Label m_PositionLabel;
        Label m_AirtimeLabel;
        Label m_JumpPhaseLabel;
        Label m_FallPhaseLabel;
        Label m_JumpHeightLabel;
        Label m_FallHeightLabel;
        Label m_AirDistHorizLabel;
        Label m_VertPathLabel;
        Label m_FullAirtimeLabel;
        Label m_FullJumpPhaseLabel;
        Label m_FullFallPhaseLabel;
        Label m_FullJumpHeightLabel;
        Label m_FullFallHeightLabel;
        Label m_FullDistHorizLabel;
        Label m_FullVertPathLabel;
        Label m_BhopSpeedLabel;
        Label m_BhopChainLabel;
        Label m_BhopLastChainLabel;

        // Weapon HUD Labels
        Label m_WeaponNameLabel;
        Label m_FireModeLabel;
        Label m_AmmoTypeLabel;
        Label m_ClipAmmoLabel;
        Label m_ReserveAmmoLabel;
        VisualElement m_AmmoRow;
        Label m_AmmoSeparatorLabel;
        VisualElement m_WeaponIconImage;

        // Alt Ammo HUD Labels
        Label m_AltAmmoTypeLabel;
        Label m_AltClipAmmoLabel;
        Label m_AltReserveAmmoLabel;
        VisualElement m_AltAmmoRow;

        // Health HUD Labels
        Label m_HealthLabel;
        Label m_LastSurfaceTypeLabel;

        // Round Start Countdown
        Label m_RoundStartLabel;

        /// <summary>
        /// Wird gefeuert sobald die MatchView aktiviert und alle UI-Elemente neu gebunden sind.
        /// Controller koennen hier ihren HUD-Refresh triggern.
        /// </summary>
        internal event Action OnViewEnabled;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            VisualElement root = m_UIDocument.rootVisualElement;
            m_TimerLabel = root.Query<Label>("timerLabel");
            m_PlayersConnectedLabel = root.Query<Label>("playersConnectedLabel");
            m_FpsLabel = root.Query<Label>("fpsLabel");

            // Debug HUD
            m_GroundedLabel = root.Query<Label>("groundedLabel");
            m_AttackingLabel = root.Query<Label>("attackingLabel");
            m_CrouchingLabel = root.Query<Label>("crouchingLabel");
            m_HorizSpeedLabel = root.Query<Label>("horizSpeedLabel");
            m_VertSpeedLabel = root.Query<Label>("vertSpeedLabel");
            m_PositionLabel = root.Query<Label>("positionLabel");
            m_AirtimeLabel = root.Query<Label>("airtimeLabel");
            m_JumpPhaseLabel = root.Query<Label>("jumpPhaseLabel");
            m_FallPhaseLabel = root.Query<Label>("fallPhaseLabel");
            m_JumpHeightLabel = root.Query<Label>("jumpHeightLabel");
            m_FallHeightLabel = root.Query<Label>("fallHeightLabel");
            m_AirDistHorizLabel = root.Query<Label>("airDistHorizLabel");
            m_VertPathLabel = root.Query<Label>("vertPathLabel");
            m_FullAirtimeLabel = root.Query<Label>("fullAirtimeLabel");
            m_FullJumpPhaseLabel = root.Query<Label>("fullJumpPhaseLabel");
            m_FullFallPhaseLabel = root.Query<Label>("fullFallPhaseLabel");
            m_FullJumpHeightLabel = root.Query<Label>("fullJumpHeightLabel");
            m_FullFallHeightLabel = root.Query<Label>("fullFallHeightLabel");
            m_FullDistHorizLabel = root.Query<Label>("fullDistHorizLabel");
            m_FullVertPathLabel = root.Query<Label>("fullVertPathLabel");
            m_BhopSpeedLabel = root.Query<Label>("bhopSpeedLabel");
            m_BhopChainLabel = root.Query<Label>("bhopChainLabel");
            m_BhopLastChainLabel = root.Query<Label>("bhopLastChainLabel");

            // Weapon HUD
            m_WeaponIconImage = root.Query<VisualElement>("weaponIconImage");
            m_WeaponNameLabel = root.Query<Label>("weaponNameLabel");
            m_FireModeLabel = root.Query<Label>("fireModeLabel");
            m_AmmoTypeLabel = root.Query<Label>("ammoTypeLabel");
            m_ClipAmmoLabel = root.Query<Label>("clipAmmoLabel");
            m_ReserveAmmoLabel = root.Query<Label>("reserveAmmoLabel");
            m_AmmoRow = root.Query<VisualElement>("AmmoRow");
            m_AmmoSeparatorLabel = root.Query<Label>("ammoSeparatorLabel");

            // Alt Ammo HUD
            m_AltAmmoTypeLabel = root.Query<Label>("altAmmoTypeLabel");
            m_AltClipAmmoLabel = root.Query<Label>("altClipAmmoLabel");
            m_AltReserveAmmoLabel = root.Query<Label>("altReserveAmmoLabel");
            m_AltAmmoRow = root.Query<VisualElement>("AltAmmoRow");

            // Health HUD
            m_HealthLabel = root.Query<Label>("healthLabel");
            m_LastSurfaceTypeLabel = root.Query<Label>("lastSurfaceTypeLabel");

            // Round Start Countdown
            m_RoundStartLabel = root.Query<Label>("roundStartLabel");

            OnViewEnabled?.Invoke();
        }

        internal void OnCountdownChanged(uint newValue)
        {
            m_TimerLabel.text = string.Format("{0:D2}:{1:D2}", newValue / 60, newValue % 60);
        }

        internal void OnPlayersConnectedChanged(int newValue)
        {
            m_PlayersConnectedLabel.text = $"Players connected: {newValue}";
        }

        internal void OnFpsChanged(float newValue)
        {
            m_FpsLabel.text = $"FPS: {newValue:F1}";
        }

        /// <summary>
        /// Aktualisiert alle Debug-HUD-Labels mit den aktuellen Spielerdaten.
        /// </summary>
        internal void UpdateDebugHud(
            bool isGrounded,
            bool isAttacking,
            bool isCrouching,
            float horizSpeed,
            float vertSpeed,
            Vector3 position,
            float currentAirtime,
            float currentJumpPhase,
            float currentFallPhase,
            float currentJumpHeight,
            float currentFallHeight,
            float currentAirDistHoriz,
            float currentVertPath,
            float fullAirtime,
            float fullJumpPhase,
            float fullFallPhase,
            float fullJumpHeight,
            float fullFallHeight,
            float fullAirDistHoriz,
            float fullVertPath)
        {
            m_GroundedLabel.text = $"Grounded: {isGrounded}";
            m_AttackingLabel.text = $"Attacking: {isAttacking}";
            m_CrouchingLabel.text = $"Crouching: {isCrouching}";
            m_HorizSpeedLabel.text = $"Horiz Speed: {horizSpeed:F1} u/s";
            m_VertSpeedLabel.text = $"Vert Speed: {vertSpeed:F1} u/s";
            m_PositionLabel.text = $"Pos: ({position.x:F1}, {position.y:F1}, {position.z:F1})";
            m_AirtimeLabel.text = $"Airtime: {currentAirtime:F2}s";
            m_JumpPhaseLabel.text = $"Jump Phase: {currentJumpPhase:F2}s";
            m_FallPhaseLabel.text = $"Fall Phase: {currentFallPhase:F2}s";
            m_JumpHeightLabel.text = $"Jump Height: {currentJumpHeight:F1}m";
            m_FallHeightLabel.text = $"Fall Height: {currentFallHeight:F1}m";
            m_AirDistHorizLabel.text = $"Air Dist Horiz: {currentAirDistHoriz:F1}m";
            m_VertPathLabel.text = $"Vert Path: {currentVertPath:F1}m";
            m_FullAirtimeLabel.text = $"Full Airtime: {fullAirtime:F2}s";
            m_FullJumpPhaseLabel.text = $"Full Jump Phase: {fullJumpPhase:F2}s";
            m_FullFallPhaseLabel.text = $"Full Fall Phase: {fullFallPhase:F2}s";
            m_FullJumpHeightLabel.text = $"Full Jump Height: {fullJumpHeight:F1}m";
            m_FullFallHeightLabel.text = $"Full Fall Height: {fullFallHeight:F1}m";
            m_FullDistHorizLabel.text = $"Full Dist Horiz: {fullAirDistHoriz:F1}m";
            m_FullVertPathLabel.text = $"Full Vert Path: {fullVertPath:F1}m";
        }

        /// <summary>
        /// Aktualisiert die Bhop-Chain-Anzeige am unteren Bildschirmrand.
        /// </summary>
        internal void UpdateBhopDisplay(
            float horizSpeed,
            int chainCount,
            float chainPeakSpeed,
            float chainDistance,
            int lastChainCount,
            float lastChainPeakSpeed,
            float lastChainDistance)
        {
            m_BhopSpeedLabel.text = $"{horizSpeed:F1}";

            if (chainCount > 1)
            {
                m_BhopChainLabel.text = $"Bhop x{chainCount}  |  Peak: {chainPeakSpeed:F1}  |  Dist: {chainDistance:F1}m";
            }
            else
            {
                m_BhopChainLabel.text = "";
            }

            if (lastChainCount > 1 && chainCount <= 1)
            {
                m_BhopLastChainLabel.text = $"Last: x{lastChainCount}  |  Peak: {lastChainPeakSpeed:F1}  |  Dist: {lastChainDistance:F1}m";
            }
            else
            {
                m_BhopLastChainLabel.text = "";
            }
        }

        /// <summary>
        /// Aktualisiert die Waffen-Anzeige (Name, Clip, Reserve).
        /// Bei infinite Ammo wird die Ammo-Row ausgeblendet.
        /// </summary>
        internal void UpdateWeaponHud(Texture2D weaponIcon, string displayName, string ammoType, int clipAmmo, int reserveAmmo, bool hideAmmoRow, bool hasAltAmmo, string altAmmoType, int altClipAmmo, int altReserveAmmo)
        {
            if (weaponIcon != null)
            {
                m_WeaponIconImage.style.backgroundImage = new StyleBackground(weaponIcon);
                m_WeaponIconImage.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_WeaponIconImage.style.display = DisplayStyle.None;
            }

            m_WeaponNameLabel.text = displayName.ToUpperInvariant();

            if (hideAmmoRow)
            {
                m_AmmoTypeLabel.style.display = DisplayStyle.None;
                m_AmmoRow.style.display = DisplayStyle.None;
            }
            else
            {
                if (string.IsNullOrEmpty(ammoType))
                {
                    m_AmmoTypeLabel.style.display = DisplayStyle.None;
                }
                else
                {
                    m_AmmoTypeLabel.style.display = DisplayStyle.Flex;
                    m_AmmoTypeLabel.text = ammoType;
                }

                m_AmmoRow.style.display = DisplayStyle.Flex;
                m_ClipAmmoLabel.text = clipAmmo.ToString();
                m_ReserveAmmoLabel.text = reserveAmmo.ToString();
            }

            if (hasAltAmmo)
            {
                if (string.IsNullOrEmpty(altAmmoType))
                {
                    m_AltAmmoTypeLabel.style.display = DisplayStyle.None;
                }
                else
                {
                    m_AltAmmoTypeLabel.style.display = DisplayStyle.Flex;
                    m_AltAmmoTypeLabel.text = altAmmoType;
                }

                m_AltAmmoRow.style.display = DisplayStyle.Flex;
                m_AltClipAmmoLabel.text = altClipAmmo.ToString();
                m_AltReserveAmmoLabel.text = altReserveAmmo.ToString();
            }
            else
            {
                m_AltAmmoTypeLabel.style.display = DisplayStyle.None;
                m_AltAmmoRow.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Aktualisiert die Munitions-Anzeige (Clip + Reserve).
        /// </summary>
        internal void UpdateAmmoHud(int clipAmmo, int reserveAmmo)
        {
            m_ClipAmmoLabel.text = clipAmmo.ToString();
            m_ReserveAmmoLabel.text = reserveAmmo.ToString();
        }

        /// <summary>
        /// Aktualisiert die Alt-Ammo-Anzeige (z.B. M203 Granaten).
        /// </summary>
        internal void UpdateAltAmmoHud(int altClipAmmo, int altReserveAmmo)
        {
            m_AltClipAmmoLabel.text = altClipAmmo.ToString();
            m_AltReserveAmmoLabel.text = altReserveAmmo.ToString();
        }

        /// <summary>
        /// Aktualisiert die Health-Anzeige.
        /// </summary>
        internal void UpdateHealthHud(int health)
        {
            m_HealthLabel.text = health.ToString();
        }

        /// <summary>
        /// Aktualisiert die Debug-Anzeige des zuletzt getroffenen Surface-Typs.
        /// </summary>
        internal void UpdateLastSurfaceType(string surfaceType)
        {
            if (m_LastSurfaceTypeLabel != null)
            {
                m_LastSurfaceTypeLabel.text = $"Surface: {surfaceType}";
            }
        }

        /// <summary>
        /// Aktualisiert die FireMode-Anzeige.
        /// Zeigt den Modus nur an wenn die Waffe wechselbare Feuermodi hat.
        /// </summary>
        internal void UpdateFireModeHud(string fireMode, bool hasMultipleModes)
        {
            if (!hasMultipleModes || string.IsNullOrEmpty(fireMode))
            {
                m_FireModeLabel.style.display = DisplayStyle.None;
                return;
            }

            m_FireModeLabel.style.display = DisplayStyle.Flex;
            m_FireModeLabel.text = fireMode.ToUpperInvariant();
        }

        /// <summary>
        /// Zeigt den Round-Start-Countdown (3, 2, 1) als zentriertes Overlay an.
        /// </summary>
        internal void ShowRoundStartCountdown(uint value)
        {
            m_RoundStartLabel.text = value > 0 ? value.ToString() : "";
            m_RoundStartLabel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Zeigt "GO!" als zentriertes Overlay an.
        /// </summary>
        internal void ShowGoText()
        {
            m_RoundStartLabel.text = "GO!";
            m_RoundStartLabel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Versteckt das Round-Start-Countdown-Overlay.
        /// </summary>
        internal void HideRoundStartCountdown()
        {
            m_RoundStartLabel.style.display = DisplayStyle.None;
        }
    }
}
