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
            }

            public LoginTextures login;
            public MainMenuTextures mainMenu;
        }

        [Serializable]
        public class GameplayConfiguration
        {
            public string playerAvatar;
        }

        public MetagameConfiguration metagame;
        public GameplayConfiguration gameplay;

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
        }

    }
}