using System;
using System.IO;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.SoundManagement
{
    /// <summary>
    /// JSON-driven configuration for all UI sounds.
    /// Analogous to TextureConfiguration — loaded from StreamingAssets/SoundConfiguration.json.
    /// </summary>
    [Serializable]
    public class SoundConfiguration
    {
        [Serializable]
        public class MenuSounds
        {
            /// <summary>Hover / highlight over a button or interactive element.</summary>
            public string hilite;

            /// <summary>Generic button click.</summary>
            public string click;

            /// <summary>Confirm / select an item.</summary>
            public string select;

            /// <summary>Apply changes / confirm action.</summary>
            public string applyChanges;

            /// <summary>Invalid action / error feedback.</summary>
            public string invalid;

            /// <summary>Data printout / info display sound.</summary>
            public string printout;

            /// <summary>Object or status update notification.</summary>
            public string objUpdate;

            /// <summary>Rotary dial / selection variant 1.</summary>
            public string rotary01;

            /// <summary>Rotary dial / selection variant 2.</summary>
            public string rotary02;

            /// <summary>Lock / disable sound.</summary>
            public string lockSound;

            /// <summary>Unlock / enable sound.</summary>
            public string unlockSound;
        }

        public MenuSounds menu;

        /// <summary>
        /// Loads the SoundConfiguration from StreamingAssets/SoundConfiguration.json.
        /// </summary>
        public void Initialize()
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, "SoundConfiguration.json");
            if (!File.Exists(configPath))
            {
                Debug.LogWarning($"[SoundConfiguration] SoundConfiguration.json not found at {configPath}, using defaults.");
                ApplyDefaults();
                return;
            }

            string json = File.ReadAllText(configPath);
            SoundConfiguration loadedConfig = JsonUtility.FromJson<SoundConfiguration>(json);
            menu = loadedConfig.menu;
        }

        /// <summary>
        /// Fallback defaults matching the original SoF2 menu sounds.
        /// </summary>
        void ApplyDefaults()
        {
            menu = new MenuSounds
            {
                hilite = "sound/misc/menus/hilite.wav",
                click = "sound/misc/menus/click.wav",
                select = "sound/misc/menus/select.wav",
                applyChanges = "sound/misc/menus/apply_changes.wav",
                invalid = "sound/misc/menus/invalid.wav",
                printout = "sound/misc/menus/printout.wav",
                objUpdate = "sound/misc/menus/obj_update.wav",
                rotary01 = "sound/misc/menus/rotary01.wav",
                rotary02 = "sound/misc/menus/rotary02.wav",
                lockSound = "sound/misc/menus/lock.wav",
                unlockSound = "sound/misc/menus/unlock.wav"
            };
        }
    }
}
