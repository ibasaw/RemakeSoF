using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Sammlung aller FP-Composite-Animationen fuer eine Waffe.
    /// Mappt SoF2 SOF2.inview Animation-States auf Unity Animation-Clips.
    /// Enthaelt alle SoF2-MP-relevanten States: Basis (idle/fire/reload/ready/done),
    /// Prone/Crawl, Knife-Combo, Dual-Wield, Shell-Reload, Scope und Throw.
    /// Die mp_speed-Werte aus SoF2 sind bereits in die Track-Speeds eingerechnet.
    /// </summary>
    [Serializable]
    public class InviewAnimationSet
    {
        // ──────────────────────────────────────────────
        // Basis-States (alle Waffen)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Idle-Animation (Standardpose bei gehaltener Waffe).
        /// </summary>
        [JsonProperty("idle")]
        public InviewAnimationState Idle;

        /// <summary>
        /// Feuer-Animation (primaerer Angriff).
        /// Waffen-Track kann <see cref="InviewAnimationTrack.Variants"/> enthalten
        /// fuer zufaellige Fire-Varianten (z.B. AK-74: 4 Varianten).
        /// </summary>
        [JsonProperty("fire")]
        public InviewAnimationState Fire;

        /// <summary>
        /// Alternativer Feuer-Modus (z.B. USAS-12 Vollautomatik).
        /// </summary>
        [JsonProperty("fire2")]
        public InviewAnimationState Fire2;

        /// <summary>
        /// Nachladen-Animation.
        /// </summary>
        [JsonProperty("reload")]
        public InviewAnimationState Reload;

        /// <summary>
        /// Waffe-Ziehen-Animation (Weapon-Switch rein).
        /// </summary>
        [JsonProperty("ready")]
        public InviewAnimationState Ready;

        /// <summary>
        /// Waffe-Wegstecken-Animation (Weapon-Switch raus).
        /// </summary>
        [JsonProperty("done")]
        public InviewAnimationState Done;

        /// <summary>
        /// Alternativfeuer-Animation (optional, z.B. M203, Bayonet, Pistol-Whip).
        /// </summary>
        [JsonProperty("altfire")]
        public InviewAnimationState Altfire;

        /// <summary>
        /// Alternativ-Nachladen-Animation (optional, z.B. M4 M203 reload).
        /// </summary>
        [JsonProperty("altreload")]
        public InviewAnimationState Altreload;

        /// <summary>
        /// Leer-Feuer-Animation (Klick bei leerer Waffe).
        /// </summary>
        [JsonProperty("dryfire")]
        public InviewAnimationState Dryfire;

        // ──────────────────────────────────────────────
        // Prone / Crawl States
        // ──────────────────────────────────────────────

        /// <summary>
        /// Idle-Animation im Prone (Bauchlage).
        /// </summary>
        [JsonProperty("proneidle")]
        public InviewAnimationState Proneidle;

        /// <summary>
        /// Waffe-Ziehen im Prone.
        /// </summary>
        [JsonProperty("proneready")]
        public InviewAnimationState Proneready;

        /// <summary>
        /// Waffe-Wegstecken im Prone.
        /// </summary>
        [JsonProperty("pronedone")]
        public InviewAnimationState Pronedone;

        /// <summary>
        /// Feuer-Animation im Prone.
        /// </summary>
        [JsonProperty("pronefire")]
        public InviewAnimationState Pronefire;

        /// <summary>
        /// Uebergang von Stand zu Prone.
        /// </summary>
        [JsonProperty("standtoprone")]
        public InviewAnimationState Standtoprone;

        /// <summary>
        /// Uebergang von Prone zu Stand.
        /// </summary>
        [JsonProperty("pronetostand")]
        public InviewAnimationState Pronetostand;

        /// <summary>
        /// Kriech-Animation (Prone-Bewegung).
        /// </summary>
        [JsonProperty("crawl")]
        public InviewAnimationState Crawl;

        // ──────────────────────────────────────────────
        // Knife-Combo States
        // ──────────────────────────────────────────────

        /// <summary>
        /// Knife Pre-Fire (Aushol-Animation vor dem Schlag).
        /// SoF2: knifepullback — Messer wird zurueckgezogen.
        /// </summary>
        [JsonProperty("prefire")]
        public InviewAnimationState Prefire;

        /// <summary>
        /// Knife Combo-Transition 1.
        /// SoF2: Uebergang zwischen Fire-Varianten im Combo-System.
        /// Track.Variants/Ends/Transitions steuern die Combo-Verkettung.
        /// </summary>
        [JsonProperty("firetrans1")]
        public InviewAnimationState Firetrans1;

        /// <summary>
        /// Knife Combo-End 1 (Rueckkehr zur Idle-Pose von links-unten).
        /// SoF2: knifeupfromll.
        /// </summary>
        [JsonProperty("fireend1")]
        public InviewAnimationState Fireend1;

        /// <summary>
        /// Knife Combo-End 2 (Rueckkehr zur Idle-Pose von links-rechts).
        /// SoF2: knifeupfromlr.
        /// </summary>
        [JsonProperty("fireend2")]
        public InviewAnimationState Fireend2;

        // ──────────────────────────────────────────────
        // Dual-Wield States (M1911A1, US SOCOM, Micro Uzi)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Dual-Wield Ready (beide Waffen gleichzeitig ziehen).
        /// SoF2: Wird beim Switch zu Dual-Wield-Modus abgespielt.
        /// </summary>
        [JsonProperty("dualready")]
        public InviewAnimationState Dualready;

        /// <summary>
        /// Rechte Waffe leer-nachladen (Magazin leer → Slide-Lock-Reload).
        /// SoF2: Nur bei Dual-Wield-Pistolen.
        /// </summary>
        [JsonProperty("emptyreload")]
        public InviewAnimationState Emptyreload;

        /// <summary>
        /// Linke Waffe feuern.
        /// </summary>
        [JsonProperty("leftfire")]
        public InviewAnimationState Leftfire;

        /// <summary>
        /// Linke Waffe ziehen.
        /// </summary>
        [JsonProperty("leftready")]
        public InviewAnimationState Leftready;

        /// <summary>
        /// Linke Waffe nachladen (Magazin nicht leer).
        /// </summary>
        [JsonProperty("leftreload")]
        public InviewAnimationState Leftreload;

        /// <summary>
        /// Linke Waffe leer-nachladen.
        /// </summary>
        [JsonProperty("leftemptyreload")]
        public InviewAnimationState Leftemptyreload;

        /// <summary>
        /// Linke Waffe Idle.
        /// </summary>
        [JsonProperty("leftidle")]
        public InviewAnimationState Leftidle;

        /// <summary>
        /// Linke Waffe wegstecken.
        /// </summary>
        [JsonProperty("leftdone")]
        public InviewAnimationState Leftdone;

        /// <summary>
        /// Linke Waffe Alternativfeuer (z.B. Pistol-Whip links).
        /// </summary>
        [JsonProperty("leftaltfire")]
        public InviewAnimationState Leftaltfire;

        /// <summary>
        /// Linke Waffe Kriech-Animation.
        /// </summary>
        [JsonProperty("leftcrawl")]
        public InviewAnimationState Leftcrawl;

        /// <summary>
        /// Linke Waffe Stand-zu-Prone.
        /// </summary>
        [JsonProperty("leftstandtoprone")]
        public InviewAnimationState Leftstandtoprone;

        /// <summary>
        /// Linke Waffe Prone-zu-Stand.
        /// </summary>
        [JsonProperty("leftpronetostand")]
        public InviewAnimationState Leftpronetostand;

        /// <summary>
        /// Rechte Waffe Kriech-Animation (Dual-Wield-spezifisch).
        /// </summary>
        [JsonProperty("rightcrawl")]
        public InviewAnimationState Rightcrawl;

        // ──────────────────────────────────────────────
        // Shell-Reload States (M590, MM-1)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Shell-Reload Beginn (Waffe oeffnen / Kammer aufklappen).
        /// SoF2: Erster Teil einer mehrstufigen Nachladesequenz.
        /// </summary>
        [JsonProperty("reloadbegin")]
        public InviewAnimationState Reloadbegin;

        /// <summary>
        /// Shell-Reload einzelne Patrone (wird N-mal wiederholt pro fehlender Patrone).
        /// SoF2: Looping-Teil der mehrstufigen Nachladesequenz.
        /// </summary>
        [JsonProperty("reloadshell")]
        public InviewAnimationState Reloadshell;

        /// <summary>
        /// Shell-Reload Ende (Waffe schliessen / Kammer zuklappen).
        /// SoF2: Abschluss der mehrstufigen Nachladesequenz.
        /// </summary>
        [JsonProperty("reloadend")]
        public InviewAnimationState Reloadend;

        // ──────────────────────────────────────────────
        // Scope States (MSG90A1, OICW)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Scope-Zoom-In-Animation.
        /// </summary>
        [JsonProperty("zoomin")]
        public InviewAnimationState Zoomin;

        /// <summary>
        /// Scope-Zoom-Out-Animation.
        /// </summary>
        [JsonProperty("zoomout")]
        public InviewAnimationState Zoomout;

        // ──────────────────────────────────────────────
        // Throw States (Granaten: F1, ANM14, M67, M84, SMOHG92, MDN11, L2A2, M15)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Granate werfen Beginn (Ueberkopfwurf).
        /// </summary>
        [JsonProperty("throwbegin")]
        public InviewAnimationState Throwbegin;

        /// <summary>
        /// Granate werfen Ende / Follow-Through (Ueberkopfwurf).
        /// </summary>
        [JsonProperty("throwend")]
        public InviewAnimationState Throwend;

        /// <summary>
        /// Granate werfen Beginn (Unterhaltswurf / Roll).
        /// </summary>
        [JsonProperty("altthrowbegin")]
        public InviewAnimationState Altthrowbegin;

        /// <summary>
        /// Granate werfen Ende / Follow-Through (Unterhaltswurf / Roll).
        /// </summary>
        [JsonProperty("altthrowend")]
        public InviewAnimationState Altthrowend;

        // ──────────────────────────────────────────────
        // State-Lookup fuer dynamische Combo-Verkettung
        // ──────────────────────────────────────────────

        /// <summary>
        /// Sucht einen InviewAnimationState anhand seines JSON-Key-Namens.
        /// Wird fuer Knife-Combo-Chaining benoetigt: Fire.Weapon.Transitions[i]
        /// liefert einen State-Namen (z.B. "firetrans1"), der hier nachgeschlagen wird.
        /// </summary>
        /// <param name="stateName">JSON-Key des States (z.B. "fire", "firetrans1", "fireend1").</param>
        /// <returns>Der InviewAnimationState oder null wenn nicht vorhanden.</returns>
        public InviewAnimationState GetStateByName(string stateName)
        {
            return stateName switch
            {
                "idle" => Idle,
                "fire" => Fire,
                "fire2" => Fire2,
                "reload" => Reload,
                "ready" => Ready,
                "done" => Done,
                "altfire" => Altfire,
                "altreload" => Altreload,
                "dryfire" => Dryfire,
                "proneidle" => Proneidle,
                "proneready" => Proneready,
                "pronedone" => Pronedone,
                "pronefire" => Pronefire,
                "standtoprone" => Standtoprone,
                "pronetostand" => Pronetostand,
                "crawl" => Crawl,
                "prefire" => Prefire,
                "firetrans1" => Firetrans1,
                "fireend1" => Fireend1,
                "fireend2" => Fireend2,
                "dualready" => Dualready,
                "emptyreload" => Emptyreload,
                "leftfire" => Leftfire,
                "leftready" => Leftready,
                "leftreload" => Leftreload,
                "leftemptyreload" => Leftemptyreload,
                "leftidle" => Leftidle,
                "leftdone" => Leftdone,
                "leftaltfire" => Leftaltfire,
                "leftcrawl" => Leftcrawl,
                "leftstandtoprone" => Leftstandtoprone,
                "leftpronetostand" => Leftpronetostand,
                "rightcrawl" => Rightcrawl,
                "reloadbegin" => Reloadbegin,
                "reloadshell" => Reloadshell,
                "reloadend" => Reloadend,
                "zoomin" => Zoomin,
                "zoomout" => Zoomout,
                "throwbegin" => Throwbegin,
                "throwend" => Throwend,
                "altthrowbegin" => Altthrowbegin,
                "altthrowend" => Altthrowend,
                _ => null
            };
        }
    }
}
