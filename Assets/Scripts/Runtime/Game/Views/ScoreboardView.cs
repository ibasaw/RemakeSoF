using System;
using System.Collections.Generic;
using System.Linq;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Core;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Networked;
using Tolik.RemakeSoF.Runtime.GametypeManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// SoF2-Style Team-Scoreboard. Wird per Tab-Taste ein-/ausgeblendet.
    /// Liest Spielerdaten aus NetworkedCharacterState und Team-Scores aus NetworkedGameState.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    internal class ScoreboardView : View<GameApplication>
    {
        UIDocument m_UIDocument;
        VisualElement m_ScoreboardRoot;
        VisualElement m_ScoreboardContainer;
        VisualElement m_ScoreHeaderBar;
        Label m_ScoreHeaderLabel;
        Label m_GametypeLabel;
        Label m_GameTimeLabel;
        Label m_RedTeamNameLabel;
        Label m_RedTeamScoreLabel;
        Label m_BlueTeamNameLabel;
        Label m_BlueTeamScoreLabel;
        VisualElement m_RedTeamHeader;
        VisualElement m_BlueTeamHeader;
        VisualElement m_RedTeamLogo;
        VisualElement m_BlueTeamLogo;
        VisualElement m_RedTeamColumnHeaders;
        VisualElement m_BlueTeamColumnHeaders;
        VisualElement m_RedPlayersList;
        VisualElement m_BluePlayersList;
        VisualElement m_RedTeamFooter;
        VisualElement m_BlueTeamFooter;
        Label m_RedTeamPlayersLabel;
        Label m_BlueTeamPlayersLabel;
        Texture2D m_ScorelineTexture;
        Texture2D m_BigcharsAtlas;
        Texture2D m_DeadIconTexture;

        /// <summary>
        /// Approximate height per player row in pixels (padding + content).
        /// </summary>
        const float ROW_HEIGHT = 26f;

        /// <summary>
        /// Approximate height consumed by container padding (130), score header (40),
        /// sub-header (28), team header (60), column headers (28), footer (4),
        /// and HUD clearance at bottom (80). Total ~450px.
        /// </summary>
        const float HEADER_OVERHEAD = 350f;

        void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            VisualElement root = m_UIDocument.rootVisualElement;
            m_ScoreboardRoot = root.Q<VisualElement>("ScoreboardRoot");
            m_ScoreboardContainer = root.Q<VisualElement>("ScoreboardContainer");
            m_ScoreHeaderBar = root.Q<VisualElement>("ScoreHeaderBar");
            m_ScoreHeaderLabel = root.Q<Label>("scoreHeaderLabel");
            m_GametypeLabel = root.Q<Label>("gametypeLabel");
            m_GameTimeLabel = root.Q<Label>("gameTimeLabel");
            m_RedTeamNameLabel = root.Q<Label>("redTeamNameLabel");
            m_RedTeamScoreLabel = root.Q<Label>("redTeamScoreLabel");
            m_BlueTeamNameLabel = root.Q<Label>("blueTeamNameLabel");
            m_BlueTeamScoreLabel = root.Q<Label>("blueTeamScoreLabel");
            m_RedTeamHeader = root.Q<VisualElement>("RedTeamHeader");
            m_BlueTeamHeader = root.Q<VisualElement>("BlueTeamHeader");
            m_RedTeamLogo = root.Q<VisualElement>("RedTeamLogo");
            m_BlueTeamLogo = root.Q<VisualElement>("BlueTeamLogo");
            m_RedTeamColumnHeaders = root.Q<VisualElement>("RedTeamColumnHeaders");
            m_BlueTeamColumnHeaders = root.Q<VisualElement>("BlueTeamColumnHeaders");
            m_RedPlayersList = root.Q<VisualElement>("RedPlayersList");
            m_BluePlayersList = root.Q<VisualElement>("BluePlayersList");
            m_RedTeamFooter = root.Q<VisualElement>("RedTeamFooter");
            m_BlueTeamFooter = root.Q<VisualElement>("BlueTeamFooter");
            m_RedTeamPlayersLabel = root.Q<Label>("redTeamPlayersLabel");
            m_BlueTeamPlayersLabel = root.Q<Label>("blueTeamPlayersLabel");

            ApplyScoreboardTextures();
        }

        /// <summary>
        /// Lädt die SoF2-Scoreboard-Texturen aus der TextureConfiguration und wendet sie auf die UI-Elemente an.
        /// </summary>
        private void ApplyScoreboardTextures()
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

            // Background is now CSS-based (no texture needed)

            // Team headers (scoreboard.png green metallic bar)
            ApplyTexture(textureManager, m_RedTeamHeader, config.scoreHeader);
            ApplyTexture(textureManager, m_BlueTeamHeader, config.scoreHeader);

            // Team logos (helmet + crossed swords)
            ApplyTexture(textureManager, m_RedTeamLogo, config.teamRedLogo);
            ApplyTexture(textureManager, m_BlueTeamLogo, config.teamBlueLogo);

            // Column headers (scorelineheader.png dark grid)
            ApplyTexture(textureManager, m_RedTeamColumnHeaders, config.scorelineHeader);
            ApplyTexture(textureManager, m_BlueTeamColumnHeaders, config.scorelineHeader);

            // Footers
            ApplyTexture(textureManager, m_RedTeamFooter, config.scorelineFooter);
            ApplyTexture(textureManager, m_BlueTeamFooter, config.scorelineFooter);

            // Cache scoreline texture for dynamic player rows (dark grid)
            TextureData scorelineData = textureManager.GetTextureData(config.scoreline);
            if (scorelineData?.Texture != null)
            {
                m_ScorelineTexture = scorelineData.Texture;
            }

            // Cache bigchars atlas for QuakeColorLabel player names
            if (!string.IsNullOrEmpty(config.bigcharsAtlas))
            {
                TextureData atlasData = textureManager.GetTextureData(config.bigcharsAtlas);
                if (atlasData?.Texture != null)
                {
                    m_BigcharsAtlas = atlasData.Texture;
                }
            }

            // Cache dead icon for player status column
            if (!string.IsNullOrEmpty(config.deadIcon))
            {
                TextureData deadData = textureManager.GetTextureData(config.deadIcon);
                if (deadData?.Texture != null)
                {
                    m_DeadIconTexture = deadData.Texture;
                }
            }
        }

        /// <summary>
        /// Wendet eine Textur aus dem TextureManager auf ein VisualElement als Hintergrundbild an.
        /// </summary>
        private void ApplyTexture(TextureManager textureManager, VisualElement element, string textureKey)
        {
            if (element == null || string.IsNullOrEmpty(textureKey))
            {
                return;
            }

            TextureData textureData = textureManager.GetTextureData(textureKey);
            if (textureData?.Texture != null)
            {
                element.style.backgroundImage = new StyleBackground(textureData.Texture);
            }
        }

        /// <summary>
        /// Zeigt das Scoreboard an und aktualisiert alle Daten.
        /// </summary>
        internal void ShowScoreboard()
        {
            if (m_ScoreboardRoot == null)
            {
                return;
            }

            RefreshScoreboard();
            m_ScoreboardRoot.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Versteckt das Scoreboard.
        /// </summary>
        internal void HideScoreboard()
        {
            if (m_ScoreboardRoot == null)
            {
                return;
            }

            m_ScoreboardRoot.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Aktualisiert alle Scoreboard-Daten aus dem aktuellen Netzwerk-State.
        /// </summary>
        private void RefreshScoreboard()
        {
            NetworkedGameState gameState = NetworkedGameState.Singleton;
            if (gameState == null)
            {
                return;
            }

            // Team-Scores
            int redScore = gameState.redTeamScore.Value;
            int blueScore = gameState.blueTeamScore.Value;
            m_RedTeamScoreLabel.text = redScore.ToString();
            m_BlueTeamScoreLabel.text = blueScore.ToString();

            // Score Header
            UpdateScoreHeader(redScore, blueScore);

            // Gametype-Name und Team-Namen aus Definition
            UpdateGametypeInfo(gameState);

            // Game-Time aus Countdown
            uint countdown = gameState.matchCountdown.Value;
            m_GameTimeLabel.text = $"Game Time: {countdown / 60}:{countdown % 60:D2}";

            // Spieler-Listen aufbauen
            RefreshPlayerLists();
        }

        /// <summary>
        /// Aktualisiert den Score-Header (z.B. "Game Tied at 0", "Red leads 3-1").
        /// </summary>
        private void UpdateScoreHeader(int redScore, int blueScore)
        {
            if (redScore == blueScore)
            {
                m_ScoreHeaderLabel.text = $"Game Tied at {redScore}";
            }
            else if (redScore > blueScore)
            {
                m_ScoreHeaderLabel.text = $"Red leads {redScore}-{blueScore}";
            }
            else
            {
                m_ScoreHeaderLabel.text = $"Blue leads {blueScore}-{redScore}";
            }
        }

        /// <summary>
        /// Aktualisiert Gametype-Name und Team-Namen aus der GametypeDefinition.
        /// </summary>
        private void UpdateGametypeInfo(NetworkedGameState gameState)
        {
            string gametypeId = gameState.activeGametypeId.Value.ToString();
            GametypeDefinitionLoader definitionLoader = ServiceLocator.Get<GametypeDefinitionLoader>();
            if (definitionLoader == null || string.IsNullOrEmpty(gametypeId))
            {
                return;
            }

            GametypeDefinition definition = definitionLoader.GetByGametypeId(gametypeId);
            if (definition == null)
            {
                return;
            }

            m_GametypeLabel.text = definition.gametypeName ?? gametypeId;

            if (!string.IsNullOrEmpty(definition.team1Name))
            {
                m_RedTeamNameLabel.text = $"RED TEAM ({definition.team1Name})";
            }

            if (!string.IsNullOrEmpty(definition.team2Name))
            {
                m_BlueTeamNameLabel.text = $"BLUE TEAM ({definition.team2Name})";
            }
        }

        /// <summary>
        /// Baut die Spieler-Listen dynamisch auf, getrennt nach Team.
        /// Iteriert alle gespawnten NetworkObjects und liest deren NetworkedCharacterState.
        /// Nutzt SpawnedObjectsList statt GetPlayerNetworkObject (letzteres ist server-only).
        /// </summary>
        private void RefreshPlayerLists()
        {
            m_RedPlayersList.Clear();
            m_BluePlayersList.Clear();

            int redCount = 0;
            int blueCount = 0;

            int maxVisibleRows = GetMaxVisibleRows();

            List<NetworkedCharacterState> redPlayers = new();
            List<NetworkedCharacterState> bluePlayers = new();

            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.SpawnManager != null)
            {
                foreach (NetworkObject spawnedObj in networkManager.SpawnManager.SpawnedObjectsList)
                {
                    if (!spawnedObj.TryGetComponent(out NetworkedCharacterState characterState))
                    {
                        continue;
                    }

                    GametypeTeam team = (GametypeTeam)characterState.TeamId;
                    if (team == GametypeTeam.Red)
                    {
                        redPlayers.Add(characterState);
                    }
                    else if (team == GametypeTeam.Blue)
                    {
                        bluePlayers.Add(characterState);
                    }
                }
            }

            redPlayers.Sort((a, b) => b.Kills.CompareTo(a.Kills));
            bluePlayers.Sort((a, b) => b.Kills.CompareTo(a.Kills));

            int redVisible = 0;
            foreach (NetworkedCharacterState state in redPlayers)
            {
                if (redVisible >= maxVisibleRows)
                {
                    break;
                }
                VisualElement row = CreatePlayerRow(state);
                m_RedPlayersList.Add(row);
                redVisible++;
                redCount++;
            }

            int blueVisible = 0;
            foreach (NetworkedCharacterState state in bluePlayers)
            {
                if (blueVisible >= maxVisibleRows)
                {
                    break;
                }
                VisualElement row = CreatePlayerRow(state);
                m_BluePlayersList.Add(row);
                blueVisible++;
                blueCount++;
            }

//Einkommentieren für Editor-Tests mit Mock-Spielern
//#if UNITY_EDITOR
//            PopulateMockPlayers(ref redCount, ref blueCount, maxVisibleRows, redVisible, blueVisible);
//#endif

            m_RedTeamPlayersLabel.text = $"Players: {redCount}";
            m_BlueTeamPlayersLabel.text = $"Players: {blueCount}";
        }

        /// <summary>
        /// Berechnet die maximale Anzahl sichtbarer Zeilen pro Team basierend auf der Bildschirmhöhe.
        /// </summary>
        private int GetMaxVisibleRows()
        {
            float screenHeight = Screen.height;
            float topPadding = screenHeight * 0.1f;
            float availableHeight = screenHeight - topPadding - HEADER_OVERHEAD;
            int maxRows = Mathf.FloorToInt(availableHeight / ROW_HEIGHT);
            int result = Mathf.Max(maxRows, 4);
            return result;
        }

#if UNITY_EDITOR
        List<VisualElement> m_CachedRedMockRows;
        List<VisualElement> m_CachedBlueMockRows;
        int m_CachedRedMockCount;
        int m_CachedBlueMockCount;

        /// <summary>
        /// Füllt das Scoreboard mit Mock-Spielern bis zum Maximum (128) für visuelle Tests im Editor.
        /// Rows werden einmal generiert und danach gecacht.
        /// </summary>
        private void PopulateMockPlayers(ref int redCount, ref int blueCount, int maxVisibleRows, int redVisible, int blueVisible)
        {
            if (m_CachedRedMockRows == null)
            {
                BuildMockRowCache(redCount + blueCount);
            }

            int redAdded = 0;
            foreach (VisualElement row in m_CachedRedMockRows)
            {
                if (redVisible >= maxVisibleRows)
                {
                    break;
                }
                m_RedPlayersList.Add(row);
                redVisible++;
                redAdded++;
            }

            int blueAdded = 0;
            foreach (VisualElement row in m_CachedBlueMockRows)
            {
                if (blueVisible >= maxVisibleRows)
                {
                    break;
                }
                m_BluePlayersList.Add(row);
                blueVisible++;
                blueAdded++;
            }

            redCount += m_CachedRedMockCount;
            blueCount += m_CachedBlueMockCount;
        }

        /// <summary>
        /// Erstellt alle Mock-Rows einmalig und speichert sie im Cache.
        /// </summary>
        private void BuildMockRowCache(int totalReal)
        {
            int maxPlayers = 128;
            NetworkedGameState gameState = NetworkedGameState.Singleton;
            if (gameState != null && gameState.MaxPlayers > 0)
            {
                maxPlayers = gameState.MaxPlayers;
            }

            string[] mockNames = new string[]
            {
                "^1Sgt^7.Mayhem",
                "^4Fr0stBit3",
                "^3xXDarkLordXx",
                "^6SniperWolf",
                "^2CampKing42",
                "^5R4g3Quit",
                "^1BulletSponge",
                "^4HeadHunter",
                "^3NoobSlayer99",
                "^7Ghost^1Rider",
                "^2TacoMaster",
                "^5FragMachine",
                "^6ShadowBlade",
                "^1DeathWish",
                "^4IceQu33n"
            };

            System.Random rng = new(42);
            int mockIndex = 0;
            int tempRedCount = 0;
            int tempBlueCount = 0;

            List<(string name, int kills, int deaths, int ping, bool isAlive, bool isRed)> mockPlayers = new();

            for (int i = totalReal; i < maxPlayers; i++)
            {
                string name = mockNames[mockIndex % mockNames.Length];
                mockIndex++;
                int kills = rng.Next(0, 30);
                int deaths = rng.Next(0, 25);
                int ping = rng.Next(15, 120);
                bool isAlive = rng.Next(0, 3) > 0;
                bool isRed = tempRedCount <= tempBlueCount;

                if (isRed)
                {
                    tempRedCount++;
                }
                else
                {
                    tempBlueCount++;
                }

                mockPlayers.Add((name, kills, deaths, ping, isAlive, isRed));
            }

            m_CachedRedMockCount = tempRedCount;
            m_CachedBlueMockCount = tempBlueCount;

            m_CachedRedMockRows = mockPlayers
                .Where(p => p.isRed)
                .OrderByDescending(p => p.kills)
                .Select(p => CreateMockPlayerRow(p.name, p.kills, p.deaths, p.ping, p.isAlive))
                .ToList();

            m_CachedBlueMockRows = mockPlayers
                .Where(p => !p.isRed)
                .OrderByDescending(p => p.kills)
                .Select(p => CreateMockPlayerRow(p.name, p.kills, p.deaths, p.ping, p.isAlive))
                .ToList();
        }

        /// <summary>
        /// Erstellt eine Mock-Spielerzeile für visuelle Tests.
        /// </summary>
        private VisualElement CreateMockPlayerRow(string playerName, int kills, int deaths, int ping, bool isAlive)
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 3;
            row.style.paddingBottom = 3;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(1f, 1f, 1f, 0.1f);

            if (m_ScorelineTexture != null)
            {
                row.style.backgroundImage = new StyleBackground(m_ScorelineTexture);
            }

            Color textColor = isAlive
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.4f);

            QuakeColorLabel nameLabel = new();
            nameLabel.Text = playerName;
            nameLabel.CharWidth = 13f;
            nameLabel.CharHeight = 20f;
            if (m_BigcharsAtlas != null)
            {
                nameLabel.Atlas = m_BigcharsAtlas;
            }
            nameLabel.style.width = new Length(44, LengthUnit.Percent);
            nameLabel.style.flexShrink = 0;
            nameLabel.style.opacity = isAlive ? 1f : 0.4f;
            row.Add(nameLabel);

            VisualElement statusIcon = new();
            statusIcon.style.width = new Length(6, LengthUnit.Percent);
            statusIcon.style.height = 20;
            statusIcon.style.flexShrink = 0;
            statusIcon.style.alignSelf = Align.Center;
            if (!isAlive && m_DeadIconTexture != null)
            {
                statusIcon.style.backgroundImage = new StyleBackground(m_DeadIconTexture);
                statusIcon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }
            row.Add(statusIcon);

            Label killsLabel = new(kills.ToString());
            killsLabel.style.fontSize = 20;
            killsLabel.style.color = textColor;
            killsLabel.style.width = new Length(14, LengthUnit.Percent);
            killsLabel.style.flexShrink = 0;
            killsLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            row.Add(killsLabel);

            Label deathsLabel = new(deaths.ToString());
            deathsLabel.style.fontSize = 20;
            deathsLabel.style.color = textColor;
            deathsLabel.style.width = new Length(14, LengthUnit.Percent);
            deathsLabel.style.flexShrink = 0;
            deathsLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            row.Add(deathsLabel);

            Label pingLabel = new($"{ping}ms");
            pingLabel.style.fontSize = 20;
            pingLabel.style.color = textColor;
            pingLabel.style.width = new Length(16, LengthUnit.Percent);
            pingLabel.style.flexShrink = 0;
            pingLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            row.Add(pingLabel);

            return row;
        }
#endif

        /// <summary>
        /// Erstellt eine Spieler-Zeile mit Name, Kills, Deaths und Health.
        /// </summary>
        private VisualElement CreatePlayerRow(NetworkedCharacterState characterState)
        {
            VisualElement row = new();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.paddingTop = 3;
            row.style.paddingBottom = 3;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(1f, 1f, 1f, 0.1f);

            if (m_ScorelineTexture != null)
            {
                row.style.backgroundImage = new StyleBackground(m_ScorelineTexture);
            }

            // Tote Spieler werden ausgegraut
            Color textColor = characterState.IsAlive
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.4f);

            QuakeColorLabel nameLabel = new();
            nameLabel.Text = characterState.CharacterName;
            nameLabel.CharWidth = 13f;
            nameLabel.CharHeight = 20f;
            if (m_BigcharsAtlas != null)
            {
                nameLabel.Atlas = m_BigcharsAtlas;
            }
            nameLabel.style.width = new Length(44, LengthUnit.Percent);
            nameLabel.style.flexShrink = 0;
            nameLabel.style.opacity = characterState.IsAlive ? 1f : 0.4f;
            row.Add(nameLabel);

            VisualElement statusIcon = new();
            statusIcon.style.width = new Length(6, LengthUnit.Percent);
            statusIcon.style.height = 20;
            statusIcon.style.flexShrink = 0;
            statusIcon.style.alignSelf = Align.Center;
            if (m_DeadIconTexture != null)
            {
                statusIcon.style.backgroundImage = new StyleBackground(m_DeadIconTexture);
                statusIcon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }
            row.Add(statusIcon);

            Label killsLabel = new(characterState.Kills.ToString());
            killsLabel.style.fontSize = 20;
            killsLabel.style.color = textColor;
            killsLabel.style.width = new Length(14, LengthUnit.Percent);
            killsLabel.style.flexShrink = 0;
            killsLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            row.Add(killsLabel);

            Label deathsLabel = new(characterState.Deaths.ToString());
            deathsLabel.style.fontSize = 20;
            deathsLabel.style.color = textColor;
            deathsLabel.style.width = new Length(14, LengthUnit.Percent);
            deathsLabel.style.flexShrink = 0;
            deathsLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            row.Add(deathsLabel);

            Label healthLabel = new($"{characterState.Ping}ms");
            healthLabel.style.fontSize = 20;
            healthLabel.style.color = textColor;
            healthLabel.style.width = new Length(16, LengthUnit.Percent);
            healthLabel.style.flexShrink = 0;
            healthLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            row.Add(healthLabel);

            return row;
        }
    }
}
