using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Vollständige Waffendefinition aus SoF2_Weapons_New.json.
    /// Statische Referenzdaten — wird einmal geladen und per ID abgefragt.
    /// </summary>
    [Serializable]
    public class WeaponDefinition
    {
        /// <summary>
        /// Eindeutige Waffen-ID (z.B. "knife", "m4", "rpg7").
        /// Wird auch als Key im NetworkList-Inventory verwendet.
        /// </summary>
        [JsonProperty("id")]
        public string Id;

        /// <summary>
        /// Anzeigename für UI (z.B. "AK-74", "RPG-7").
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName;

        /// <summary>
        /// Waffenkategorie (1=Melee, 2=Pistol, 5=Rifle, 7=Heavy).
        /// </summary>
        [JsonProperty("category")]
        public int Category;

        /// <summary>
        /// Index für den Animator-Parameter "CurrentWeapon".
        /// </summary>
        [JsonProperty("animatorIndex")]
        public int AnimatorIndex;

        /// <summary>
        /// HUD-Icon-Pfad.
        /// </summary>
        [JsonProperty("menuImage")]
        public string MenuImage;

        /// <summary>
        /// Pfad zum World-Model (3rd-Person-Darstellung).
        /// </summary>
        [JsonProperty("worldModel")]
        public string WorldModel;

        /// <summary>
        /// Pfad zum View-Model (1st-Person-Darstellung).
        /// </summary>
        [JsonProperty("viewModel")]
        public string ViewModel;

        /// <summary>
        /// SoF2 Foreshorten-Faktor fuer First-Person-Darstellung.
        /// Skaliert das Waffenmodell entlang der Z-Achse (0.6 = Standard, 0.0 = keine Verkuerzung).
        /// </summary>
        [JsonProperty("foreshorten")]
        public float Foreshorten = 0.6f;

        /// <summary>
        /// SoF2 View-Offset in Quake-Units (Forward/Right/Up).
        /// Verschiebt die First-Person-Kamera relativ zum Standard-Viewpoint pro Waffe.
        /// Konvertierung: QU × 0.0254 = Unity-Meter.
        /// </summary>
        [JsonProperty("viewOffset")]
        public WeaponViewOffsetDefinition ViewOffset;

        /// <summary>
        /// SoF2 Waffen-FOV (Horizontal, Grad).
        /// Waffe wird mit separatem FOV gerendert damit sie bei weitem Welt-FOV
        /// nicht verzerrt aussieht. 0 = Standard (65°).
        /// SoF2: fov_x in weaponInfo_t, genutzt in CG_CalculateWeaponFov.
        /// </summary>
        [JsonProperty("fovX")]
        public float FovX;

        /// <summary>
        /// Ob die Waffe eine Nahkampfwaffe ist.
        /// </summary>
        [JsonProperty("isMelee")]
        public bool IsMelee;

        /// <summary>
        /// Munitionsdefinition.
        /// </summary>
        [JsonProperty("ammo")]
        public WeaponAmmoDefinition Ammo;

        /// <summary>
        /// Primärangriff-Definition.
        /// </summary>
        [JsonProperty("attack")]
        public WeaponAttackDefinition Attack;

        /// <summary>
        /// Alternativangriff-Definition (optional).
        /// </summary>
        [JsonProperty("altAttack")]
        public WeaponAttackDefinition AltAttack;

        /// <summary>
        /// Sound-Definitionen als flexible Key-Value-Paare.
        /// Werte können einzelne Strings oder String-Arrays sein.
        /// </summary>
        [JsonProperty("sounds")]
        public Dictionary<string, object> Sounds;

        /// <summary>
        /// Animationsdefinitionen (mp_idle, mp_attack, mp_raise, mp_drop, mp_reload, etc.).
        /// </summary>
        [JsonProperty("animations")]
        public Dictionary<string, WeaponAnimationEntry> Animations;

        /// <summary>
        /// Buffer-Skeleton-Definition fuer SoF2-Style First-Person-Darstellung.
        /// Unsichtbares Skeleton zwischen Waffe und Haenden mit waffen-spezifischen Bolts.
        /// </summary>
        [JsonProperty("buffer")]
        public WeaponBufferDefinition Buffer;

        /// <summary>
        /// Hand-Definitionen (links/rechts) fuer SoF2-Style First-Person-Darstellung.
        /// Haende werden an Buffer-Bolts befestigt.
        /// </summary>
        [JsonProperty("hands")]
        public WeaponHandsDefinition Hands;

        /// <summary>
        /// Reload-Sound-Events: Definiert wann waehrend der Reload-Animation welche Sounds gespielt werden.
        /// SoF2-Referenz: Animation-Events in GLM-Dateien triggerten clipOut, clipIn, boltRelease etc.
        /// </summary>
        [JsonProperty("reloadSounds")]
        public ReloadSoundDefinition ReloadSounds;

        /// <summary>
        /// FP-Composite-Animationen (SoF2 SOF2.inview).
        /// Pro State (idle, fire, reload, ready, done) separate Clips fuer Waffe, lhand, rhand.
        /// Speeds sind MP-Werte (mp_speed wo vorhanden, sonst speed).
        /// </summary>
        [JsonProperty("inviewAnimations")]
        public InviewAnimationSet InviewAnimations;
    }
}
