using System;
using System.IO;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.TextureManagement
{
    [Serializable]
    public class TextureConfiguration
    {

        [Serializable]
        public class MetagameConfiguration
        {
            [Serializable]
            public class LoginTextures
            {
                public string background;
                public string logo;
                public string buttonBackground;
                public string inputBackground;
            }

            [Serializable]
            public class MainMenuTextures
            {
                public string background;
                public string joinServerButtonGlow;
                public string joinServerButton;
                public string createServerButtonGlow;
                public string createServerButton;
                public string optionsButtonGlow;
                public string optionsButton;
                public string loadoutButtonGlow;
                public string loadoutButton;
                public string logoutButtonGlow;
                public string logoutButton;
            }

            [Serializable]
            public class JoinServerTextures
            {
                public string background;
                public string scanline;
                public string lockIcon;
                public string getListButton;
                public string getListButtonAlt;
                public string refreshButton;
                public string refreshButtonAlt;
                public string joinButton;
                public string joinButtonAlt;
            }

            public LoginTextures login;
            public MainMenuTextures mainMenu;
            public JoinServerTextures joinServer;
        }

        [Serializable]
        public class GameplayConfiguration
        {
            public string playerAvatar;
        }
        [Serializable]
        public class ConsoleTextures
        {
            public string background;
            public string glowline;
            public string glowlineWide;
        }

        public MetagameConfiguration metagame;
        public GameplayConfiguration gameplay;
        public ConsoleTextures console;

        /// <summary>
        /// Lädt die TextureConfiguration aus StreamingAssets/TextureConfiguration.json
        /// </summary>
        public void Initialize()
        {
            // Lade von StreamingAssets statt Resources
            string configPath = Path.Combine(Application.streamingAssetsPath, "TextureConfiguration.json");
            if (!File.Exists(configPath))
                throw new FileNotFoundException($"TextureConfiguration.json not found at {configPath}");

            string json = File.ReadAllText(configPath);
            TextureConfiguration loadedConfig = JsonUtility.FromJson<TextureConfiguration>(json);
            metagame = loadedConfig.metagame;
            gameplay = loadedConfig.gameplay;
            console = loadedConfig.console;
        }

    }
}