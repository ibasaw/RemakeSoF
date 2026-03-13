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
    }
}
