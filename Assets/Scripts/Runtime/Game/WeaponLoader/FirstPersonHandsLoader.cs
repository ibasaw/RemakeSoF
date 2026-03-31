using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.WeaponManagement
{
    /// <summary>
    /// Interner Service fuer SoF2-Style First-Person-Hands.
    /// Laedt Buffer-Skeleton + Hand-Modelle (lhand/rhand) und attached sie
    /// am FP-Waffenmodell gemaess WeaponDefinition (buffer.boltToBone, hands.left/right.boltToBone).
    ///
    /// SoF2 Ghoul2 Composite-Modell (cg_weapons.c CG_RegisterWeapon):
    ///   Slot 0: viewG2Model (Waffe)
    ///   Slot 1: Buffer (unsichtbares Skeleton, bolted an Waffe bei "gun"/"handle")
    ///   Slot 2: rhand.glm (bolted an Buffer bei z.B. "handle_m4")
    ///   Slot 3: lhand.glm (bolted an Buffer bei z.B. "lhand_m4")
    ///
    /// Buffer ist ein unsichtbares Skeleton-Prefab mit Attachment-Bolts fuer
    /// Haende, Muzzle-Flash und optionale Bolton-Modelle (z.B. M203).
    /// Hand-Modelle (lhand/rhand) sind gemeinsam fuer alle Waffen — nur die
    /// Attachment-Bolts unterscheiden sich pro Waffe.
    ///
    /// Unterstuetzt Fire-Varianten (zufaellige Clip-Auswahl), Knife-Combo-Chaining
    /// (variants/ends/transitions) und Dual-Wield-States.
    ///
    /// Kein MonoBehaviour — wird von ClientPlayerCharacter orchestriert.
    /// </summary>
    public class FirstPersonHandsLoader
    {
        /// <summary>
        /// Addressable-Key fuer das linke Hand-Modell (lhand.glm).
        /// Gemeinsam fuer alle Waffen — nur das Attachment am Buffer variiert.
        /// </summary>
        private const string k_LeftHandKey = "lhand";

        /// <summary>
        /// Addressable-Key fuer das rechte Hand-Modell (rhand.glm).
        /// Gemeinsam fuer alle Waffen — nur das Attachment am Buffer variiert.
        /// </summary>
        private const string k_RightHandKey = "rhand";

        /// <summary>
        /// Instanziiertes Buffer-Skeleton (Child der Waffe am buffer.boltToBone-Bolt).
        /// </summary>
        private GameObject m_BufferInstance;

        /// <summary>
        /// Instanziiertes linkes Hand-Modell (Child des Buffers am hands.left.boltToBone-Bolt).
        /// </summary>
        private GameObject m_LeftHandInstance;

        /// <summary>
        /// Instanziiertes rechtes Hand-Modell (Child des Buffers am hands.right.boltToBone-Bolt).
        /// Null bei Waffen die nur einhaendig gehalten werden (z.B. M60, USAS-12, MSG90A1).
        /// </summary>
        private GameObject m_RightHandInstance;

        /// <summary>
        /// Gecachte Animation-Components fuer Waffe, lhand, rhand.
        /// Werden beim Laden gesetzt und fuer PlayState() verwendet.
        /// </summary>
        private Animation m_WeaponAnimation;
        private Animation m_LeftHandAnimation;
        private Animation m_RightHandAnimation;

        /// <summary>
        /// Aktuell geladene Animations-Konfiguration (aus WeaponDefinition.InviewAnimations).
        /// </summary>
        private InviewAnimationSet m_AnimationSet;

        /// <summary>
        /// Buffer-Instance fuer externen Zugriff (z.B. Muzzle-Bolt-Suche).
        /// </summary>
        public GameObject BufferInstance => m_BufferInstance;

        /// <summary>
        /// Laedt Buffer-Skeleton + Hand-Modelle und attached sie an der FP-Waffe.
        /// SoF2: CG_RegisterWeapon laedt alle Composite-Modell-Slots gleichzeitig.
        /// </summary>
        /// <param name="fpWeaponInstance">Instanziiertes FP-Waffenmodell (Slot 0).</param>
        /// <param name="definition">WeaponDefinition mit Buffer- und Hands-Konfiguration.</param>
        /// <returns>True wenn Buffer erfolgreich geladen (Haende optional).</returns>
        public bool LoadAndAttach(GameObject fpWeaponInstance, WeaponDefinition definition)
        {
            Clear();

            if (fpWeaponInstance == null || definition?.Buffer == null)
            {
                return false;
            }

            PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
            if (prefabManager == null)
            {
                Debug.LogWarning("[FirstPersonHandsLoader] PrefabManager nicht verfuegbar.");
                return false;
            }

            // 1. Buffer-Bolt auf der Waffe finden (z.B. "gun" oder "handle")
            Transform bufferBolt = FindDeepChild(fpWeaponInstance.transform, definition.Buffer.BoltToBone);
            if (bufferBolt == null)
            {
                Debug.LogWarning($"[FirstPersonHandsLoader] Buffer-Bolt '{definition.Buffer.BoltToBone}' " +
                                 "nicht auf FP-Waffe gefunden.");
                return false;
            }

            // 2. Waffen-Animation VOR dem Attachen der Kinder cachen,
            // damit GetComponentInChildren nicht die Hand-Animations findet.
            m_AnimationSet = definition.InviewAnimations;
            m_WeaponAnimation = fpWeaponInstance.GetComponentInChildren<Animation>();

            // 3. Buffer-Prefab laden und an den Bolt attachen
            // Der Addressable-Key ist direkt buffer.model aus der JSON (z.B. "models/weapons/buffer/rifle/buffer")
            string bufferKey = definition.Buffer.Model;
            GameObject bufferPrefab = prefabManager.LoadPrefab<GameObject>(bufferKey);
            if (bufferPrefab == null)
            {
                Debug.LogWarning($"[FirstPersonHandsLoader] Buffer-Prefab nicht gefunden: '{bufferKey}'");
                return false;
            }

            m_BufferInstance = Object.Instantiate(bufferPrefab, bufferBolt);
            m_BufferInstance.transform.SetLocalPositionAndRotation(
                Vector3.zero, Quaternion.Euler(0f, 0f, 180f)); // SoF2 Buffer ist um 180° gedreht
            // SoF2 Ghoul2: Gebolte Modelle erben NICHT die Scale des Parent-Modells.
            // Unity: Children erben immer Parent-Scale. Weapon-Bones haben FBX-interne
            // Scales (z.B. cm→m), deshalb muss der Buffer kompensieren damit seine
            // worldScale der Waffe entspricht (alle im gleichen QU-Raum).
            Vector3 targetWorldScale = fpWeaponInstance.transform.lossyScale;
            CompensateToTargetWorldScale(m_BufferInstance.transform, bufferBolt, targetWorldScale);

            // 4. Linke Hand laden (fast immer vorhanden)
            if (definition.Hands?.Left != null)
            {
                m_LeftHandInstance = LoadAndAttachHand(
                    prefabManager, k_LeftHandKey,
                    m_BufferInstance.transform, definition.Hands.Left.BoltToBone,
                    targetWorldScale);
            }

            // 5. Rechte Hand laden (optional — manche Waffen nur einhaendig)
            if (definition.Hands?.Right != null)
            {
                m_RightHandInstance = LoadAndAttachHand(
                    prefabManager, k_RightHandKey,
                    m_BufferInstance.transform, definition.Hands.Right.BoltToBone,
                    targetWorldScale);
            }

            Debug.Log($"[FirstPersonHandsLoader] Composite geladen: buffer='{bufferKey}', " +
                      $"lhand={m_LeftHandInstance != null}, rhand={m_RightHandInstance != null}");

            // 6. Hand-Animation-Components cachen
            if (m_LeftHandInstance != null)
            {
                m_LeftHandAnimation = m_LeftHandInstance.GetComponentInChildren<Animation>();
            }
            if (m_RightHandInstance != null)
            {
                m_RightHandAnimation = m_RightHandInstance.GetComponentInChildren<Animation>();
            }

            Debug.Log($"[FirstPersonHandsLoader] Animation-Components: " +
                      $"weapon={m_WeaponAnimation != null} (clips={m_WeaponAnimation?.GetClipCount()}), " +
                      $"lhand={m_LeftHandAnimation != null} (clips={m_LeftHandAnimation?.GetClipCount()}), " +
                      $"rhand={m_RightHandAnimation != null} (clips={m_RightHandAnimation?.GetClipCount()})");

            LogAvailableClips("weapon", m_WeaponAnimation);
            LogAvailableClips("lhand", m_LeftHandAnimation);
            LogAvailableClips("rhand", m_RightHandAnimation);

            // Idle als Default-Animation starten (SoF2: CG_RegisterWeapon setzt nach Load immer idle)
            PlayState(m_AnimationSet?.Idle, true);

            // Animation.Sample() erzwingt sofortige Bone-Evaluation.
            // Ohne Sample() bleiben Bones bis zum naechsten Frame in Default-Pose.
            // SoF2: Ghoul2 evaluiert Bone-Positionen sofort beim SetAnim.
            if (m_WeaponAnimation != null) m_WeaponAnimation.Sample();
            if (m_LeftHandAnimation != null) m_LeftHandAnimation.Sample();
            if (m_RightHandAnimation != null) m_RightHandAnimation.Sample();

            return true;
        }

        /// <summary>
        /// Spielt einen Animations-State auf allen Composite-Slots ab.
        /// SoF2: CG_SetWeaponAnim steuert alle Slots synchron.
        /// Wird von ClientPlayerCharacter bei Waffenaktionen aufgerufen
        /// (fire, reload, ready, done, idle).
        /// </summary>
        /// <param name="state">Der abzuspielende Animations-State (aus InviewAnimationSet).</param>
        /// <param name="crossFade">True fuer weiches Ueberblenden (Idle), false fuer harten Schnitt (Fire).</param>
        public void PlayState(InviewAnimationState state, bool crossFade = false)
        {
            if (state == null)
            {
                return;
            }

            PlayTrackOnAnimation(m_WeaponAnimation, state.Weapon, crossFade);

            // SoF2: Slots ohne Track fuer diesen State werden ausgeblendet.
            // Knife hat z.B. keine leftHand, Granaten keine rightHand etc.
            SetHandVisibility(m_LeftHandInstance, state.LeftHand != null);
            SetHandVisibility(m_RightHandInstance, state.RightHand != null);

            PlayTrackOnAnimation(m_LeftHandAnimation, state.LeftHand, crossFade);
            PlayTrackOnAnimation(m_RightHandAnimation, state.RightHand, crossFade);
        }

        /// <summary>
        /// Spielt die Idle-Animation ab (Convenience-Methode).
        /// </summary>
        public void PlayIdle()
        {
            PlayState(m_AnimationSet?.Idle, true);
        }

        /// <summary>
        /// Spielt einen Fire-State mit zufaelliger Varianten-Auswahl ab.
        /// SoF2: CG_SetWeaponAnim waehlt bei Fire-Events zufaellig aus der
        /// variants[]-Liste des Waffen-Tracks (z.B. AK-74: ak74fire/fire2/fire3/fire4).
        ///
        /// Fuer Knife-Combo: Der zurueckgegebene Varianten-Index kann mit
        /// <see cref="GetComboTransitionState"/> und <see cref="GetComboEndState"/>
        /// genutzt werden um den naechsten Combo-State zu bestimmen.
        /// </summary>
        /// <param name="state">Der abzuspielende State (default: Fire).</param>
        /// <returns>
        /// Varianten-Index (0-basiert). 0 wenn keine Varianten vorhanden.
        /// Wird fuer Knife-Combo-Lookup benoetigt.
        /// </returns>
        public int PlayFireState(InviewAnimationState state = null)
        {
            state ??= m_AnimationSet?.Fire;
            if (state == null)
            {
                return 0;
            }

            InviewAnimationTrack weaponTrack = state.Weapon;
            int variantIndex = SelectRandomVariantIndex(weaponTrack);
            string variantClip = GetVariantClip(weaponTrack, variantIndex);

            PlayTrackWithClipOverride(m_WeaponAnimation, weaponTrack, variantClip, false);

            SetHandVisibility(m_LeftHandInstance, state.LeftHand != null);
            SetHandVisibility(m_RightHandInstance, state.RightHand != null);

            PlayTrackOnAnimation(m_LeftHandAnimation, state.LeftHand, false);
            PlayTrackOnAnimation(m_RightHandAnimation, state.RightHand, false);

            return variantIndex;
        }

        /// <summary>
        /// Gibt den Combo-Transition-State zurueck fuer Knife-Combo-Chaining.
        /// SoF2: Wenn der Spieler waehrend des Fire-States erneut angreift,
        /// wird transitions[variantIndex] als naechster State abgespielt.
        /// </summary>
        /// <param name="currentState">Der State, dessen Waffen-Track die transitions[] enthaelt.</param>
        /// <param name="variantIndex">Der von <see cref="PlayFireState"/> zurueckgegebene Index.</param>
        /// <returns>Der Transition-State oder null wenn kein Combo moeglich.</returns>
        public InviewAnimationState GetComboTransitionState(InviewAnimationState currentState, int variantIndex)
        {
            InviewAnimationTrack weaponTrack = currentState?.Weapon;
            if (weaponTrack?.Transitions == null || variantIndex < 0 || variantIndex >= weaponTrack.Transitions.Length)
            {
                return null;
            }

            string transitionName = weaponTrack.Transitions[variantIndex];
            return m_AnimationSet?.GetStateByName(transitionName);
        }

        /// <summary>
        /// Gibt den Combo-End-State zurueck fuer Knife-Combo-Beendigung.
        /// SoF2: Wenn der Spieler nach dem Fire-State NICHT erneut angreift,
        /// wird ends[variantIndex] abgespielt (Rueckkehr zur Idle-Pose).
        /// </summary>
        /// <param name="currentState">Der State, dessen Waffen-Track die ends[] enthaelt.</param>
        /// <param name="variantIndex">Der von <see cref="PlayFireState"/> zurueckgegebene Index.</param>
        /// <returns>Der End-State oder null wenn kein spezieller End-State vorhanden.</returns>
        public InviewAnimationState GetComboEndState(InviewAnimationState currentState, int variantIndex)
        {
            InviewAnimationTrack weaponTrack = currentState?.Weapon;
            if (weaponTrack?.Ends == null || variantIndex < 0 || variantIndex >= weaponTrack.Ends.Length)
            {
                return null;
            }

            string endName = weaponTrack.Ends[variantIndex];
            return m_AnimationSet?.GetStateByName(endName);
        }

        /// <summary>
        /// Prueft ob ein State Combo-Transitions hat (Knife-Combo-System).
        /// </summary>
        /// <param name="state">Der zu pruefende State.</param>
        /// <returns>True wenn der Waffen-Track transitions[] enthaelt.</returns>
        public static bool HasComboTransitions(InviewAnimationState state)
        {
            return state?.Weapon?.Transitions != null && state.Weapon.Transitions.Length > 0;
        }

        /// <summary>
        /// Gibt das aktuelle InviewAnimationSet zurueck fuer externen Zugriff
        /// (z.B. ClientPlayerCharacter fuer Fire/Reload-Trigger).
        /// </summary>
        public InviewAnimationSet AnimationSet => m_AnimationSet;

        /// <summary>
        /// Setzt Sichtbarkeit des gesamten FP-Hand-Composites (Buffer + Haende).
        /// Buffer ist Parent — SetActive propagiert auf alle Kinder.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (m_BufferInstance != null)
            {
                m_BufferInstance.SetActive(visible);
            }
        }

        /// <summary>
        /// Zerstoert Buffer + Haende. Reihenfolge: Haende zuerst, dann Buffer.
        /// </summary>
        public void Clear()
        {
            m_WeaponAnimation = null;
            m_LeftHandAnimation = null;
            m_RightHandAnimation = null;
            m_AnimationSet = null;

            if (m_LeftHandInstance != null)
            {
                Object.Destroy(m_LeftHandInstance);
                m_LeftHandInstance = null;
            }

            if (m_RightHandInstance != null)
            {
                Object.Destroy(m_RightHandInstance);
                m_RightHandInstance = null;
            }

            if (m_BufferInstance != null)
            {
                Object.Destroy(m_BufferInstance);
                m_BufferInstance = null;
            }
        }

        /// <summary>
        /// Spielt einen einzelnen Animation-Track auf einer Animation-Component ab.
        /// Setzt Speed und waehlt zwischen CrossFade (smooth) und Play (sofort).
        /// </summary>
        /// <summary>
        /// Unity FBX-Import Prefix fuer Animation-Clip-Namen.
        /// FBX-Import fuegt "skeleton_root|" vor jeden Clip-Namen hinzu.
        /// </summary>
        private const string k_ClipPrefix = "skeleton_root|";

        /// <summary>
        /// Blendet ein Hand-Modell ein oder aus.
        /// SoF2: Haende ohne Track fuer den aktuellen State werden nicht gerendert.
        /// Z.B. Knife hat keine leftHand, Granaten haben teilweise keine rightHand.
        /// </summary>
        private static void SetHandVisibility(GameObject handInstance, bool visible)
        {
            if (handInstance != null)
            {
                handInstance.SetActive(visible);
            }
        }

        /// <summary>
        /// Waehlt einen zufaelligen Varianten-Index aus dem Track.
        /// Gibt 0 zurueck wenn keine Varianten vorhanden.
        /// </summary>
        private static int SelectRandomVariantIndex(InviewAnimationTrack track)
        {
            if (track?.Variants == null || track.Variants.Length == 0)
            {
                return 0;
            }

            return Random.Range(0, track.Variants.Length);
        }

        /// <summary>
        /// Gibt den Clip-Namen fuer den angegebenen Varianten-Index zurueck.
        /// Faellt auf Track.Clip zurueck wenn keine Varianten oder Index ausserhalb.
        /// </summary>
        private static string GetVariantClip(InviewAnimationTrack track, int variantIndex)
        {
            if (track?.Variants != null && variantIndex >= 0 && variantIndex < track.Variants.Length)
            {
                return track.Variants[variantIndex];
            }

            return track?.Clip;
        }

        /// <summary>
        /// Spielt einen Track mit einem ueberschriebenen Clip-Namen ab.
        /// Verwendet Speed/Loop vom Track, aber den Clip-Namen aus dem Override.
        /// Wird fuer Fire-Varianten benoetigt (anderer Clip, gleiche Speed/Loop-Settings).
        /// </summary>
        private static void PlayTrackWithClipOverride(
            Animation animation, InviewAnimationTrack track, string clipOverride, bool crossFade)
        {
            if (animation == null || track == null)
            {
                return;
            }

            string clipToPlay = string.IsNullOrEmpty(clipOverride) ? track.Clip : clipOverride;
            if (string.IsNullOrEmpty(clipToPlay))
            {
                return;
            }

            string clipName = k_ClipPrefix + clipToPlay;

            AnimationState clipState = animation[clipName];
            if (clipState == null)
            {
                Debug.LogWarning($"[FirstPersonHandsLoader] Variant-Clip '{clipName}' " +
                                 $"nicht gefunden auf '{animation.gameObject.name}'.");
                return;
            }

            clipState.speed = track.Speed;
            clipState.wrapMode = track.Loop ? WrapMode.Loop : WrapMode.Once;

            if (crossFade)
            {
                animation.CrossFade(clipName, 0.15f);
            }
            else
            {
                animation.Play(clipName);
            }
        }

        private static void PlayTrackOnAnimation(Animation animation, InviewAnimationTrack track, bool crossFade)
        {
            if (animation == null || track == null || string.IsNullOrEmpty(track.Clip))
            {
                return;
            }

            string clipName = k_ClipPrefix + track.Clip;

            AnimationState clipState = animation[clipName];
            if (clipState == null)
            {
                Debug.LogWarning($"[FirstPersonHandsLoader] Animation-Clip '{clipName}' " +
                                 $"nicht gefunden auf '{animation.gameObject.name}'.");
                return;
            }

            clipState.speed = track.Speed;
            clipState.wrapMode = track.Loop ? WrapMode.Loop : WrapMode.Once;

            if (crossFade)
            {
                animation.CrossFade(clipName, 0.15f);
            }
            else
            {
                animation.Play(clipName);
            }
        }

        /// <summary>
        /// Laedt ein Hand-Prefab und attached es an den passenden Buffer-Bolt.
        /// SoF2: trap_G2API_AttachG2Model mit bufferBolt als Attachment-Point.
        /// </summary>
        private static GameObject LoadAndAttachHand(
            PrefabManager prefabManager,
            string handKey,
            Transform bufferRoot,
            string boltToBone,
            Vector3 targetWorldScale)
        {
            Transform handBolt = FindDeepChild(bufferRoot, boltToBone);
            if (handBolt == null)
            {
                Debug.LogWarning($"[FirstPersonHandsLoader] Hand-Bolt '{boltToBone}' " +
                                 "nicht auf Buffer gefunden.");
                return null;
            }

            GameObject handPrefab = prefabManager.LoadPrefab<GameObject>(handKey);
            if (handPrefab == null)
            {
                Debug.LogWarning($"[FirstPersonHandsLoader] Hand-Prefab nicht gefunden: '{handKey}'");
                return null;
            }

            GameObject handInstance = Object.Instantiate(handPrefab, handBolt);
            handInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            // SoF2 Ghoul2: Gebolte Modelle sind eigenstaendig skaliert.
            // Kompensiere Buffer-Bone-Scale damit Hand im gleichen QU-Raum wie Waffe liegt.
            CompensateToTargetWorldScale(handInstance.transform, handBolt, targetWorldScale);

            return handInstance;
        }

        /// <summary>
        /// Kompensiert die Parent-Bone-Scale damit das Child die gleiche World-Scale
        /// wie die Waffe hat. SoF2 Ghoul2 gebolte Modelle sind eigenstaendig skaliert
        /// (kein Scale-Vererben). In Unity erben Children immer Parent-Scale,
        /// deshalb: localScale = targetWorldScale / parentBolt.lossyScale.
        /// </summary>
        private static void CompensateToTargetWorldScale(
            Transform child, Transform parentBolt, Vector3 targetWorldScale)
        {
            Vector3 boltScale = parentBolt.lossyScale;
            child.localScale = new Vector3(
                targetWorldScale.x / boltScale.x,
                targetWorldScale.y / boltScale.y,
                targetWorldScale.z / boltScale.z);
        }

        /// <summary>
        /// Rekursive Tiefensuche nach einem Child-Transform mit dem angegebenen Namen.
        /// </summary>
        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform result = FindDeepChild(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Debug: Listet alle verfuegbaren Animation-Clips auf einer Animation-Component auf.
        /// </summary>
        private static void LogAvailableClips(string label, Animation animation)
        {
            if (animation == null)
            {
                return;
            }

            System.Text.StringBuilder sb = new();
            sb.Append($"[FirstPersonHandsLoader] '{label}' clips: ");
            foreach (AnimationState state in animation)
            {
                sb.Append($"'{state.name}', ");
            }
            Debug.Log(sb.ToString());
        }
    }
}
