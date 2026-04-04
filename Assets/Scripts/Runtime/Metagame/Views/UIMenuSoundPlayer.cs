using UnityEngine;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.SoundManagement;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Plays SoF2-style menu UI sounds (hover, click, select, etc.) via the SoundManager.
    /// Sound keys are read from SoundConfiguration.json through SoundManager.Configuration.
    /// Centralises the pattern so each View does not need its own PlayUiSound method.
    /// </summary>
    internal static class UIMenuSoundPlayer
    {
        /// <summary>Cached reference to the menu sounds config section.</summary>
        static SoundConfiguration.MenuSounds s_Menu;

        /// <summary>Returns the menu sounds config, caching on first access.</summary>
        static SoundConfiguration.MenuSounds Menu
        {
            get
            {
                if (s_Menu != null) return s_Menu;
                SoundManager soundManager = ServiceLocator.Get<SoundManager>();
                s_Menu = soundManager?.Configuration?.menu;
                return s_Menu;
            }
        }

        /// <summary>Hover / highlight sound key from config.</summary>
        public static string Hilite => Menu?.hilite;

        /// <summary>Generic button click sound key from config.</summary>
        public static string Click => Menu?.click;

        /// <summary>Item selection sound key from config.</summary>
        public static string Select => Menu?.select;

        /// <summary>Apply / confirm action sound key from config.</summary>
        public static string ApplyChanges => Menu?.applyChanges;

        /// <summary>Invalid action sound key from config.</summary>
        public static string Invalid => Menu?.invalid;

        /// <summary>Data printout / info display sound key from config.</summary>
        public static string Printout => Menu?.printout;

        /// <summary>Object or status update sound key from config.</summary>
        public static string ObjUpdate => Menu?.objUpdate;

        /// <summary>Rotary dial / selection variant 1 sound key from config.</summary>
        public static string Rotary01 => Menu?.rotary01;

        /// <summary>Rotary dial / selection variant 2 sound key from config.</summary>
        public static string Rotary02 => Menu?.rotary02;

        /// <summary>Lock / disable sound key from config.</summary>
        public static string Lock => Menu?.lockSound;

        /// <summary>Unlock / enable sound key from config.</summary>
        public static string Unlock => Menu?.unlockSound;

        /// <summary>
        /// Plays a 2D UI sound through the SFX mixer.
        /// Creates a temporary AudioSource that self-destructs after playback.
        /// </summary>
        public static void Play(string soundKey)
        {
            if (string.IsNullOrEmpty(soundKey)) return;

            SoundManager soundManager = ServiceLocator.Get<SoundManager>();
            if (soundManager == null) return;

            AudioClip clip = soundManager.GetClip(soundKey);
            if (clip == null) return;

            GameObject soundObj = new("UIMenuSound");
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.playOnAwake = false;

            if (soundManager.SfxGroup != null)
            {
                source.outputAudioMixerGroup = soundManager.SfxGroup;
            }

            source.Play();
            Object.Destroy(soundObj, clip.length + 0.1f);
        }
    }
}
