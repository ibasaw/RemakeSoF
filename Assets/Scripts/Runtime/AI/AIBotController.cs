using Tolik.RemakeSoF.Runtime.AI.Audio;
using Tolik.RemakeSoF.Runtime.AI.Personality;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using Tolik.RemakeSoF.Runtime.Game.Networked;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Bridge zwischen GOAP-Planner und ServerAICharacter.
    /// Verwaltet Sensor3D-Array fuer Wahrnehmung, empfaengt Bewegungs-/Angriffs-Intents
    /// von GOAP-Aktionen und baut daraus jeden Tick einen PlayerCommand.
    /// Laeuft nur auf dem Server. Liegt als Component auf dem AICharacter-Prefab.
    /// </summary>
    public class AIBotController : MonoBehaviour
    {
        /// <summary>Sensor-Array-Generator (selbes GameObject, per GetComponent gefunden).</summary>
        private SensorArrayGenerator m_SensorArray;

        /// <summary>NetworkedCharacterState (selbes GameObject, per GetComponent gefunden).</summary>
        private NetworkedCharacterState m_CharacterState;

        /// <summary>NetworkedAICharacter fuer Debug-Pfad-Sync an Clients.</summary>
        private NetworkedAICharacter m_NetworkedAICharacter;

        /// <summary>
        /// Feste Sensor-Kopfhoehe ueber VisualRoot (Meter).
        /// SoF2-Standard: Standhoehe ~2.26m, Augenhoehe-Ratio 72/89 = ~1.83m.
        /// </summary>
        private const float k_SensorHeightOffset = 1.83f;

        /// <summary>Layer-Index fuer Player.</summary>
        private const int k_PlayerLayer = 7;

        /// <summary>SoF2-Quake-Unit zu Unity-Meter Umrechnungsfaktor (1 QU = 1 inch = 0.0254m).</summary>
        private const float k_Sof2UnitScale = 0.0254f;

        /// <summary>Pitch-Decay: Grad pro Sekunde Rueckfuehrung Richtung 0° ohne LookTarget.</summary>
        private const float k_PitchDecayDegPerSec = 15f;

        /// <summary>Referenz auf die Physik-Simulation (fuer isGrounded, Velocity etc.).</summary>
        private PlayerPhysicsSimulation m_PhysicsSimulation;

        /// <summary>Aktueller Yaw-Winkel in Grad.</summary>
        private float m_CurrentYaw;

        /// <summary>Aktueller Pitch-Winkel in Grad (-89..89).</summary>
        private float m_CurrentPitch;

        /// <summary>Ob das Sensor-Array initialisiert wurde.</summary>
        private bool m_SensorsReady;

        /// <summary>Ob die Sensoren in diesem Frame bereits getickt wurden (Lazy-Tick).</summary>
        private bool m_SensorsTickedThisFrame;

        /// <summary>Tick-Zaehler fuer periodisches Debug-Logging.</summary>
        private int m_TickCount;

        /// <summary>Gecachte effektive Waffenreichweite in Unity-Metern.</summary>
        private float m_WeaponRangeMeters = 1.5f;

        /// <summary>Name der zuletzt gecachten Waffe (fuer Waffenwechsel-Erkennung).</summary>
        private string m_CachedWeaponName;

        /// <summary>Zeitpunkt der letzten Waffen-Evaluierung (fuer periodische Checks).</summary>
        private float m_LastWeaponEvalTime;

        /// <summary>Intervall zwischen Waffen-Evaluierungen in Sekunden.</summary>
        private const float k_WeaponEvalInterval = 1.5f;

        /// <summary>
        /// HideAndSeek: Distanz in Metern, ab der der Bot vom Ranged-Stunner (M4) auf
        /// das Messer wechselt, weil das Ziel ohnehin schnell erreichbar ist.
        /// In H&S ist nur das Messer toetlich; M4 stunnt nur (3 Schuss).
        /// </summary>
        private const float k_HideAndSeekRushRange = 4f;

        /// <summary>Ob dieser Bot ein Seeker ist.</summary>
        public bool IsSeeker { get; private set; }

        /// <summary>Ob GOAP aktiv ist (Sensoren bereit).</summary>
        public bool IsReady => m_SensorsReady;

        // ===== GOAP Intent-Felder (werden pro Frame von GOAP-Aktionen gesetzt) =====

        /// <summary>Bewegungs-Zielposition (null = stehen bleiben).</summary>
        private Vector3? m_MoveTarget;

        /// <summary>Blick-/Zielposition (null = Blickrichtung gleich Bewegungsrichtung).</summary>
        private Vector3? m_LookTarget;

        /// <summary>Ob der Bot im aktuellen Frame angreifen soll.</summary>
        private bool m_ShouldAttack;

        /// <summary>Ob der Bot springen soll (z.B. waehrend Flucht).</summary>
        private bool m_ShouldJump;

        /// <summary>Ob der Bot ducken soll.</summary>
        private bool m_ShouldCrouch;

        /// <summary>Checkpoint-Positionen aus der geladenen Map (statisch, identisch fuer alle Bots).</summary>
        private static Vector3[] s_Checkpoints;

        // ===== Player Breadcrumbs (dynamische Patrol-Ziele aus Spielerbewegung) =====

        /// <summary>Circular Buffer fuer Spieler-Breadcrumbs (Positionen wo Spieler gelaufen sind).</summary>
        private static Vector3[] s_Breadcrumbs;

        /// <summary>Schreibindex im Breadcrumb-Buffer (naechste Position zum Ueberschreiben).</summary>
        private static int s_BreadcrumbWriteIdx;

        /// <summary>Anzahl gueltige Breadcrumbs im Buffer (maximal s_Breadcrumbs.Length).</summary>
        private static int s_BreadcrumbCount;

        /// <summary>Maximale Anzahl gespeicherter Breadcrumbs.</summary>
        private const int k_MaxBreadcrumbs = 128;

        /// <summary>Intervall in Sekunden zwischen Breadcrumb-Aufnahmen.</summary>
        private const float k_BreadcrumbInterval = 2f;

        /// <summary>Mindestdistanz zwischen Breadcrumbs beim Waehlen eines Ziels (Meter).</summary>
        private const float k_BreadcrumbMinPickDistance = 8f;

        /// <summary>Timer fuer Breadcrumb-Aufnahme (statisch, ein Scan fuer alle Bots).</summary>
        private static float s_BreadcrumbTimer;

        /// <summary>Ob die Breadcrumb-Aufnahme initialisiert wurde.</summary>
        private static bool s_BreadcrumbsInitialized;

        /// <summary>Wahrscheinlichkeit (0-1) dass ein Breadcrumb-Ziel statt eines statischen Checkpoints gewaehlt wird.</summary>
        private const float k_BreadcrumbChance = 0.5f;

        /// <summary>Aktuelle Breadcrumb-Zielposition (null = nutze statischen Checkpoint).</summary>
        private Vector3? m_BreadcrumbTarget;

        /// <summary>Besuchszaehler pro Checkpoint-Index — ermoeglicht Least-Visited-Auswahl.</summary>
        private int[] m_CheckpointVisitCounts;

        /// <summary>Temporaer als unerreichbar markierte Checkpoint-Indizes (werden nach einem vollen Zyklus zurueckgesetzt).</summary>
        private System.Collections.Generic.HashSet<int> m_UnreachableCheckpoints = new();

        /// <summary>Index des aktuell angesteuerten Checkpoints (-1 = keiner gewaehlt).</summary>
        private int m_CurrentCheckpointIdx = -1;

        // ===== Stuck Detection (4-Phasen-Eskalation) =====

        /// <summary>Letzte Position fuer Stuck-Erkennung.</summary>
        private Vector3 m_StuckCheckPos;

        /// <summary>Timer seit letzter signifikanter Bewegung (Sekunden).</summary>
        private float m_StuckTimer;

        /// <summary>Intervall fuer Stuck-Pruefung in Sekunden.</summary>
        private const float k_StuckCheckInterval = 0.5f;

        /// <summary>Minimale horizontale Distanz die der Bot in k_StuckCheckInterval zuruecklegen muss um nicht als stuck zu gelten.</summary>
        private const float k_StuckDistThreshold = 0.3f;

        /// <summary>Phase 1: Wegpunkt ueberspringen + vertikaler Sprung (Sekunden stuck).</summary>
        private const float k_StuckPhase1 = 2.0f;

        /// <summary>Phase 2: Vorwaerts-Sprung Richtung Wegpunkt (Sekunden stuck).</summary>
        private const float k_StuckPhase2 = 3.0f;

        /// <summary>Phase 3: 180° drehen + rueckwaerts laufen + ducken (Sekunden stuck).</summary>
        private const float k_StuckPhase3 = 5.0f;

        /// <summary>Phase 4: Checkpoint aufgeben + neuen waehlen (Sekunden stuck).</summary>
        private const float k_StuckPhase4 = 7.0f;

        /// <summary>Akkumulierte Zeit seit letzter Stuck-Pruefung.</summary>
        private float m_StuckCheckAccum;

        /// <summary>Aktuelle Stuck-Phase (0 = nicht stuck, 1-4 = Eskalation).</summary>
        private int m_StuckPhase;

        /// <summary>Erzwungener Yaw-Override waehrend Phase 3 (180° drehen). NaN = kein Override.</summary>
        private float m_StuckOverrideYaw = float.NaN;

        /// <summary>Timer fuer Phase-3 Rueckwaertsbewegung (Sekunden).</summary>
        private float m_StuckReverseTimer;

        // ===== Damage Reaction =====

        /// <summary>Weltposition des letzten Angreifers (null wenn kein Schaden erhalten).</summary>
        private Vector3? m_LastAttackerPosition;

        /// <summary>Zeitpunkt des letzten erlittenen Schadens (Time.time).</summary>
        private float m_LastDamageTime;

        /// <summary>Zeitfenster in Sekunden in dem Schaden als "kuerzlich" gilt fuer GOAP-Reaktion.</summary>
        private const float k_DamageReactionWindow = 5f;

        /// <summary>Ob der Bot kuerzlich Schaden erlitten hat (fuer GOAP-Sensoren).</summary>
        public bool WasDamagedRecently => m_LastAttackerPosition.HasValue
            && (Time.time - m_LastDamageTime) < k_DamageReactionWindow;

        /// <summary>Position des letzten Angreifers (nur gueltig wenn WasDamagedRecently true).</summary>
        public Vector3? LastAttackerPosition => m_LastAttackerPosition;

        // ===== NavMesh-Pathfinding =====

        /// <summary>Gecachter NavMeshPath fuer wiederverwendbare Pfadberechnung.</summary>
        private NavMeshPath m_NavPath;

        /// <summary>Berechnete Wegpunkte des aktuellen Pfads.</summary>
        private Vector3[] m_PathCorners = System.Array.Empty<Vector3>();

        /// <summary>Aktueller Index im Pfad-Array (naechster Wegpunkt).</summary>
        private int m_PathIndex;

        /// <summary>Letztes Ziel fuer das ein Pfad berechnet wurde (Cache-Key).</summary>
        private Vector3 m_LastPathTarget;

        /// <summary>Distanz, ab der ein neuer Pfad berechnet wird wenn sich das Ziel bewegt hat (Patrol-Modus).</summary>
        private const float k_PathRecalcThreshold = 3f;

        /// <summary>Reduzierter Recalc-Threshold waehrend Chase/Shoot (reaktiveres Umpathing).</summary>
        private const float k_PathRecalcThresholdChase = 1.0f;

        /// <summary>Distanz, bei der ein Wegpunkt als erreicht gilt und zum naechsten gewechselt wird.</summary>
        private const float k_WaypointArrivalDist = 1.5f;

        /// <summary>Maximale Hoehe fuer NavMesh-SamplePosition.</summary>
        private const float k_NavMeshSampleHeight = 15f;

        /// <summary>Distanz, bei der ein Checkpoint als besucht gilt (Pac-Man-Logik).</summary>
        private const float k_CheckpointArrivalDist = 3f;

        // ===== Radius-Sensor (360° Spieler-Erkennung) =====

        /// <summary>Maximale Erkennungsreichweite fuer Spieler in Metern (OverlapSphere-Radius).</summary>
        private const float k_PlayerDetectionRadius = 30f;

        /// <summary>Ergebnis-Array fuer OverlapSphere (wiederverwendbar, vermeidet Allokationen).</summary>
        /// <summary>
        /// Buffer fuer OverlapSphere-Hits. Muss gross genug sein um zusaetzlich zu den
        /// eigenen Hitbox-Collidern (~29 pro Character) auch gegnerische Spieler zu fassen,
        /// sonst saturieren die eigenen Hitboxen den Buffer und Feinde werden nie gefunden.
        /// </summary>
        private readonly Collider[] m_RadiusHits = new Collider[128];

        /// <summary>Gecachte Spieler-LayerMask fuer OverlapSphere.</summary>
        private readonly int k_PlayerLayerMask = 1 << 7;

        /// <summary>Gecachter naechster lebender Spieler (pro Frame aktualisiert).</summary>
        private Transform m_NearestPlayerTransform;

        /// <summary>Gecachte Distanz zum naechsten lebenden Spieler (pro Frame aktualisiert).</summary>
        private float m_NearestPlayerDistance = float.MaxValue;

        /// <summary>Gecachte horizontale XZ-Distanz zum naechsten lebenden Spieler (fuer Reichweiten-Vergleich).</summary>
        private float m_NearestPlayerDistanceXZ = float.MaxValue;

        /// <summary>Gecachter NetworkedPlayerCharacter des naechsten Spielers (fuer Bewegungs-Mirroring).</summary>
        private NetworkedPlayerCharacter m_NearestPlayerCharacter;

        /// <summary>Ob im aktuellen Frame ein Radius-Scan durchgefuehrt wurde (Lazy-Tick).</summary>
        private bool m_RadiusScanDoneThisFrame;

        // ===== Spieler-Gedaechtnis (kurzfristig nach Sichtverlust) =====

        /// <summary>Dauer in Sekunden, die der Bot einen Spieler nach Sichtverlust weiter verfolgt.</summary>
        private const float k_PlayerMemoryDuration = 4.0f;

        /// <summary>Letzte bekannte Spieler-Position (gesetzt bei jedem erfolgreichen Radius-Scan).</summary>
        private Vector3 m_LastKnownPlayerPosition;

        /// <summary>Letzte bekannte Spieler-Distanz (3D, bei letztem Sichtkontakt).</summary>
        private float m_LastKnownPlayerDistance = float.MaxValue;

        /// <summary>Letzte bekannte horizontale XZ-Distanz (bei letztem Sichtkontakt).</summary>
        private float m_LastKnownPlayerDistanceXZ = float.MaxValue;

        /// <summary>Zeitpunkt des letzten direkten Sichtkontakts mit einem Spieler (Time.time).</summary>
        private float m_LastPlayerSeenTime;

        /// <summary>Ob das Spieler-Gedaechtnis aktiv ist (Sichtverlust innerhalb der Grace-Period).</summary>
        private bool PlayerMemoryActive => m_LastPlayerSeenTime > 0f
            && (Time.time - m_LastPlayerSeenTime) < k_PlayerMemoryDuration;

        /// <summary>Letzte bekannte Spieler-Position (fuer "Zum letzten Ort laufen" nach Gedaechtnisablauf).</summary>
        public Vector3? LastKnownPlayerPosition => m_LastPlayerSeenTime > 0f ? m_LastKnownPlayerPosition : null;

        /// <summary>Distanz bei der die letzte bekannte Position als erreicht gilt (Meter).</summary>
        private const float k_LastKnownArrivalDist = 2.5f;

        // ===== Geteilter Team-LKP (Squad-Awareness) =====

        /// <summary>Geteilter Last-Known-Position-Slot pro Team (Server-only, statisch).</summary>
        private struct TeamSharedSighting
        {
            public Vector3 Position;
            public float Time;
        }

        /// <summary>
        /// Statisches Dictionary mit dem aktuellsten LKP pro Bot-Team.
        /// Server-only: Bots laufen alle in derselben Server-Instanz, kein Netzwerk-Sync noetig.
        /// Eintraege expirieren automatisch via Timestamp-Check (kein explizites Cleanup noetig).
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<uint, TeamSharedSighting> s_TeamSharedLkp
            = new();

        /// <summary>Maximales Alter eines geteilten LKP bevor er ignoriert wird (Sekunden).</summary>
        private const float k_TeamSharedLkpDuration = 6f;

        /// <summary>Mindestabstand fuer den geteilten LKP zur eigenen LKP-Position (verhindert Snap-Spam).</summary>
        private const float k_TeamSharedLkpMinDelta = 1.5f;

        // ===== Personality (per-Bot Verhaltens-Multiplikatoren) =====

        /// <summary>Aktives Persoenlichkeitsprofil dieses Bots (in InitializeSensors zugewiesen).</summary>
        private BotPersonality m_Personality = new();

        /// <summary>Per-Bot Seed fuer deterministisches Aim-Noise (verschiedene Bots zittern verschieden).</summary>
        private float m_AimNoiseSeed;

        /// <summary>
        /// Server-Zeitpunkt an dem der Bot zum ersten Mal einen Feind erspaeht hat
        /// (nach vorheriger leerer Wahrnehmung). Dient als Startpunkt fuer ReactionTime-Gating.
        /// </summary>
        private float m_FirstSpotTime = -1f;

        /// <summary>True wenn beim letzten Radius-Scan ein Feind gefunden wurde (fuer Edge-Detection).</summary>
        private bool m_HadSightingLastScan;

        /// <summary>Maximaler Aim-Fehler in Grad bei Accuracy = 0 (skaliert linear).</summary>
        private const float k_MaxAimErrorDegrees = 4f;

        // ===== Fire-Pattern (Trigger-Disziplin pro FireMode) =====

        /// <summary>
        /// Letzter beobachteter Clip-Wert. Wird genutzt um "Schuss gefeuert"-Events
        /// zu erkennen (Clip dekrementiert) ohne Server-Round-Trip-Lag.
        /// </summary>
        private int m_LastObservedClip = -1;

        /// <summary>Schuesse die im aktuellen Burst-Trigger-Druck bereits gefeuert wurden.</summary>
        private int m_BurstShotsFired;

        /// <summary>Time.time bis zu dem der Trigger "losgelassen" bleiben muss (Pause).</summary>
        private float m_TriggerReleaseUntil;

        // ===== Predictive Aiming (Vorhalte-Zielen) =====

        /// <summary>Geschaetzte Geschwindigkeit des naechsten sichtbaren Spielers (aus Position-Delta).</summary>
        private Vector3 m_TrackedPlayerVelocity;

        /// <summary>Letzte bekannte Position des getrackten Spielers (fuer Velocity-Berechnung).</summary>
        private Vector3 m_TrackedPlayerLastPos;

        /// <summary>Zeitpunkt der letzten Spieler-Position-Messung (Time.time).</summary>
        private float m_TrackedPlayerLastTime;

        /// <summary>Vorhalt-Faktor in Sekunden (simulierte Reaktionszeit des Bots).</summary>
        private const float k_AimLeadTimeSec = 0.15f;

        /// <summary>Maximaler Vorhalt-Offset in Metern (verhindert absurdes Vorhalten bei Lags).</summary>
        private const float k_AimLeadMaxOffset = 3f;

        // ===== Direct LOS Steering (Chase-Modus) =====

        /// <summary>Ob der Bot aktuell im Chase/Shoot-Modus ist (Spieler direkt sichtbar → DirectLOS statt NavMesh).</summary>
        private bool m_IsDirectLOSSteering;

        // ===== Stealth Awareness (unsichtbare Spieler-Naeherung) =====

        /// <summary>Grosser Erkennungsradius fuer Stealth Awareness (Meter, ohne LOS-Check).</summary>
        private const float k_StealthAwarenessRadius = 60f;

        /// <summary>Geschaetzte Spieler-Position aus Stealth Awareness (ohne LOS, nur Proximity).</summary>
        private Vector3 m_StealthAwarenessPosition;

        /// <summary>Ob die Stealth Awareness aktuell einen Spieler im Grossradius erkannt hat.</summary>
        private bool m_StealthAwarenessActive;

        /// <summary>
        /// Ergebnis-Array fuer Stealth-Awareness OverlapSphere (wiederverwendbar).
        /// Gross dimensioniert weil eigene Hitbox-Collider (~29 pro Character) den Buffer
        /// sonst saturieren bevor Feinde erfasst werden.
        /// </summary>
        private readonly Collider[] m_StealthHits = new Collider[128];

        /// <summary>Intervallzaehler fuer Stealth-Awareness-Scan (nicht jeden Frame noetig).</summary>
        private float m_StealthScanAccum;

        /// <summary>Intervall in Sekunden zwischen Stealth-Awareness-Scans.</summary>
        private const float k_StealthScanInterval = 2f;

        // ===== Letzte gespiegelte Spieler-Aktion (fuer Stuck-Replay) =====

        /// <summary>Ob der Spieler beim letzten Mirror gesprungen ist.</summary>
        private bool m_LastMirrorJump;

        /// <summary>Ob der Spieler beim letzten Mirror geduckt war.</summary>
        private bool m_LastMirrorCrouch;

        /// <summary>Ob der Spieler beim letzten Mirror angegriffen hat.</summary>
        private bool m_LastMirrorAttack;

        // ===== Oeffentliche Properties fuer GOAP-Sensoren =====

        /// <summary>Ob mindestens ein lebender Spieler innerhalb des Erkennungsradius ist (360° OverlapSphere + LOS) oder im Gedaechtnis.</summary>
        public bool PlayerSensorDetected
        {
            get
            {
                EnsureRadiusScan();
                return m_NearestPlayerTransform != null || PlayerMemoryActive;
            }
        }

        /// <summary>Ob der Spieler aktuell direkt sichtbar ist (ohne Gedaechtnis).</summary>
        public bool PlayerDirectlyVisible
        {
            get
            {
                EnsureRadiusScan();
                return m_NearestPlayerTransform != null;
            }
        }

        /// <summary>Distanz zum naechsten lebenden Spieler in Metern (oder letzte bekannte Distanz im Gedaechtnis).</summary>
        public float PlayerSensorDistance
        {
            get
            {
                EnsureRadiusScan();
                if (m_NearestPlayerTransform != null)
                {
                    return m_NearestPlayerDistance;
                }

                return PlayerMemoryActive ? m_LastKnownPlayerDistance : float.MaxValue;
            }
        }

        /// <summary>Horizontale XZ-Distanz zum naechsten Spieler (oder letzte bekannte im Gedaechtnis).</summary>
        public float PlayerSensorDistanceXZ
        {
            get
            {
                EnsureRadiusScan();
                if (m_NearestPlayerTransform != null)
                {
                    return m_NearestPlayerDistanceXZ;
                }

                return PlayerMemoryActive ? m_LastKnownPlayerDistanceXZ : float.MaxValue;
            }
        }

        /// <summary>Aktuelle Waffenreichweite in Metern (fuer GOAP PlayerRangeSensor).</summary>
        public float WeaponRangeMeters
        {
            get
            {
                UpdateWeaponRange();
                // Personality-Multiplikator: Sniper engagiert weiter, Cautious naeher.
                return m_WeaponRangeMeters * m_Personality.engageRangeMul;
            }
        }

        /// <summary>
        /// True wenn die aktuelle Waffe eine Nahkampfwaffe ist (Messer etc.).
        /// Wird von GOAP-Aktionen genutzt um Combat-Distanz-Verhalten zu unterscheiden:
        /// Nahkampf = bis zum Spieler laufen, Fernkampf = Position halten in Sichtreichweite.
        /// </summary>
        public bool IsCurrentWeaponMelee
        {
            get
            {
                if (m_CharacterState == null)
                {
                    return true;
                }

                WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
                if (loader == null)
                {
                    return true;
                }

                WeaponDefinition weaponDef = loader.GetById(m_CharacterState.CurrentWeaponName);
                return weaponDef == null || weaponDef.IsMelee;
            }
        }

        /// <summary>Augenposition des Bots in Weltkoordinaten.</summary>
        public Vector3 EyePosition => transform.position + new Vector3(0f, k_SensorHeightOffset, 0f);

        /// <summary>Eigene Team-ID des Bots (0 = None / FFA, 1 = Red, 2 = Blue).</summary>
        public uint OwnTeamId => m_CharacterState != null ? m_CharacterState.TeamId : 0u;

        /// <summary>
        /// Prueft ob ein anderer Charakter ein gueltiges feindliches Ziel ist.
        /// Returns false wenn null, tot, oder im selben Team (sofern nicht FFA).
        /// In FFA (TeamId == 0 auf BEIDEN Seiten) sind alle anderen Spieler hostile.
        /// In Team-Spielen sind Spieler mit gleicher TeamId immer freundlich (auch ohne g_friendlyfire,
        /// damit Bots nicht aus Versehen Teamkollegen anvisieren).
        /// </summary>
        public bool IsHostileTarget(NetworkedCharacterState other)
        {
            if (other == null || !other.IsAlive)
            {
                return false;
            }

            uint ownTeam = OwnTeamId;
            uint otherTeam = other.TeamId;

            // FFA: keiner hat ein Team → alle sind feindlich
            if (ownTeam == 0u && otherTeam == 0u)
            {
                return true;
            }

            // Team-Spiel: nur unterschiedliche Teams sind feindlich
            return ownTeam != otherTeam;
        }

        /// <summary>Setzt die Bewegungs-Zielposition fuer diesen Frame. Berechnet ggf. einen NavMesh-Pfad.</summary>
        public void SetMoveTarget(Vector3 position)
        {
            m_MoveTarget = position;

            // Direct LOS Steering: Wenn Spieler direkt sichtbar ist, pruefen ob
            // eine unblockierte Linie auf dem NavMesh zum Spieler existiert.
            // Falls ja → kein NavMesh-Pfad noetig, direkte Bewegung zum Spieler.
            if (PlayerDirectlyVisible)
            {
                Vector3 botPos = transform.position;
                if (NavMesh.SamplePosition(botPos, out NavMeshHit startHit, k_NavMeshSampleHeight, NavMesh.AllAreas)
                    && NavMesh.SamplePosition(position, out NavMeshHit endHit, k_NavMeshSampleHeight, NavMesh.AllAreas))
                {
                    // NavMesh.Raycast: true = blockiert durch NavMesh-Kante, false = Weg frei
                    if (!NavMesh.Raycast(startHit.position, endHit.position, out NavMeshHit _, NavMesh.AllAreas))
                    {
                        m_IsDirectLOSSteering = true;
                        // Pfad-Cache leeren damit GetSteeringTarget direkt zum Ziel steuert
                        m_PathCorners = System.Array.Empty<Vector3>();
                        m_PathIndex = 0;
                        m_LastPathTarget = Vector3.zero;
                        return;
                    }
                }
            }

            m_IsDirectLOSSteering = false;
            UpdateNavPath(position);
        }

        /// <summary>
        /// Stoppt jede Bewegung (Move-Target = null) und leert den NavMesh-Pfad.
        /// Genutzt von GOAP-Aktionen wenn der Bot Position halten soll
        /// (z.B. Fernkampfwaffe in Sichtreichweite).
        /// </summary>
        public void StopMovement()
        {
            m_MoveTarget = null;
            m_IsDirectLOSSteering = false;
            m_PathCorners = System.Array.Empty<Vector3>();
            m_PathIndex = 0;
            m_LastPathTarget = Vector3.zero;
        }

        /// <summary>Setzt die Blick-/Zielposition fuer diesen Frame.</summary>
        public void SetLookTarget(Vector3 position) { m_LookTarget = position; }

        /// <summary>Aktiviert den Angriffsbutton fuer diesen Frame.</summary>
        public void SetShouldAttack(bool attack) { m_ShouldAttack = attack; }

        /// <summary>Aktiviert den Sprung-Button.</summary>
        public void SetShouldJump(bool jump) { m_ShouldJump = jump; }

        /// <summary>Aktiviert den Duck-Button.</summary>
        public void SetShouldCrouch(bool crouch) { m_ShouldCrouch = crouch; }

        /// <summary>
        /// Spiegelt die Aktionen des naechsten sichtbaren Spielers:
        /// Springen und Schiessen werden uebernommen. Ducken wird NICHT gespiegelt
        /// da es den Bot verlangsamt und beim Verfolgen kontraproduktiv ist.
        /// Sollte aus ChasePlayerAction/ShootPlayerAction aufgerufen werden.
        /// </summary>
        public void MirrorNearestPlayerActions()
        {
            EnsureRadiusScan();

            if (m_NearestPlayerCharacter == null)
            {
                return;
            }

            NetworkAnimationState animState = m_NearestPlayerCharacter.CurrentAnimationState;

            // Letzte gespiegelte Aktion cachen (fuer Stuck-Replay)
            m_LastMirrorJump = !animState.IsGrounded;
            m_LastMirrorCrouch = animState.IsCrouching;
            m_LastMirrorAttack = animState.IsAttacking;

            // Springen wenn der Spieler in der Luft ist
            if (m_LastMirrorJump)
            {
                m_ShouldJump = true;
            }

            // Ducken wird absichtlich NICHT gespiegelt — verlangsamt den Bot beim Verfolgen

            // Schiessen wenn der Spieler schiesst
            if (m_LastMirrorAttack)
            {
                m_ShouldAttack = true;
            }
        }

        // ===== Checkpoint-Management =====

        /// <summary>Setzt die Checkpoint-Positionen fuer alle Bots (aus Map geladen).</summary>
        public static void SetCheckpoints(Vector3[] checkpoints)
        {
            s_Checkpoints = checkpoints;
            Debug.Log($"[AI·GOAP] Checkpoints gesetzt: {(checkpoints != null ? checkpoints.Length : 0)}");
        }

        /// <summary>Setzt den Checkpoint-Fortschritt zurueck (z.B. bei neuem Rundenstart).</summary>
        public void ResetCheckpointProgress()
        {
            m_CheckpointVisitCounts = null;
            m_UnreachableCheckpoints.Clear();
            m_CurrentCheckpointIdx = -1;
            m_PathCorners = System.Array.Empty<Vector3>();
            m_PathIndex = 0;
            m_LastPathTarget = Vector3.zero;
            m_MoveTarget = null;
            m_LookTarget = null;
            m_ShouldAttack = false;
            m_ShouldJump = false;
            m_ShouldCrouch = false;
            m_StuckTimer = 0f;
            m_StuckCheckAccum = 0f;
            m_StuckCheckPos = transform.position;
            m_StuckPhase = 0;
            m_StuckOverrideYaw = float.NaN;
            m_StuckReverseTimer = 0f;
            ClearDamageReaction();
            m_LastPlayerSeenTime = 0f;
            m_TrackedPlayerVelocity = Vector3.zero;
            m_TrackedPlayerLastTime = 0f;
            m_IsDirectLOSSteering = false;
            m_StealthAwarenessActive = false;
            m_StealthScanAccum = 0f;
            m_BreadcrumbTarget = null;
        }

        /// <summary>Prueft ob die Checkpoints auf dem NavMesh erreichbar sind (nach NavMesh-Bake aufrufen).</summary>
        public static void ValidateCheckpointsOnNavMesh()
        {
            if (s_Checkpoints == null)
            {
                return;
            }

            int reachable = 0;
            for (int i = 0; i < s_Checkpoints.Length; i++)
            {
                bool onNavMesh = NavMesh.SamplePosition(s_Checkpoints[i], out NavMeshHit hit, 5f, NavMesh.AllAreas);
                if (onNavMesh)
                {
                    reachable++;
                    Debug.Log($"[AI·GOAP] Checkpoint #{i} ({s_Checkpoints[i].x:F1},{s_Checkpoints[i].y:F1},{s_Checkpoints[i].z:F1}) → NavMesh ({hit.position.x:F1},{hit.position.y:F1},{hit.position.z:F1}) d={hit.distance:F2}");
                }
                else
                {
                    Debug.LogWarning($"[AI·GOAP] Checkpoint #{i} ({s_Checkpoints[i].x:F1},{s_Checkpoints[i].y:F1},{s_Checkpoints[i].z:F1}) NICHT auf NavMesh!");
                }
            }

            Debug.Log($"[AI·GOAP] NavMesh-Check: {reachable}/{s_Checkpoints.Length} Checkpoints erreichbar");
        }

        /// <summary>
        /// Gibt die Position des aktuellen Ziel-Checkpoints zurueck.
        /// Nutzt ein Visit-Count-System das die am wenigsten besuchten Checkpoints priorisiert.
        /// Kann stattdessen ein Player-Breadcrumb-Ziel waehlen (Positionen wo Spieler gelaufen sind).
        /// Garantiert gleichmaessige Abdeckung aller Checkpoints ueber Zeit.
        /// </summary>
        public Vector3? GetNearestCheckpoint(Vector3 fromPosition)
        {
            // Breadcrumbs aufnehmen (einmal pro Intervall, statisch fuer alle Bots)
            RecordPlayerBreadcrumbs();

            if (s_Checkpoints == null || s_Checkpoints.Length == 0)
            {
                // Kein statischer Checkpoint → versuche Breadcrumb als Fallback
                return PickRandomBreadcrumb(fromPosition);
            }

            // Visit-Counts erstmalig erstellen
            if (m_CheckpointVisitCounts == null || m_CheckpointVisitCounts.Length != s_Checkpoints.Length)
            {
                m_CheckpointVisitCounts = new int[s_Checkpoints.Length];
            }

            // Breadcrumb-Ankunft erkennen
            if (m_BreadcrumbTarget.HasValue)
            {
                float dist = HorizontalDistance(fromPosition, m_BreadcrumbTarget.Value);
                if (dist < k_CheckpointArrivalDist)
                {
                    Debug.Log($"[AI·CP] Breadcrumb besucht (d={dist:F1}m)");
                    m_BreadcrumbTarget = null;
                    // Naechsten Checkpoint waehlen (faellt durch zum normalen System)
                }
                else
                {
                    return m_BreadcrumbTarget.Value;
                }
            }

            // Ankunft erkennen: Checkpoint als besucht markieren → Visit-Count erhoehen
            if (m_CurrentCheckpointIdx >= 0 && m_CurrentCheckpointIdx < s_Checkpoints.Length)
            {
                float dist = HorizontalDistance(fromPosition, s_Checkpoints[m_CurrentCheckpointIdx]);
                if (dist < k_CheckpointArrivalDist)
                {
                    m_CheckpointVisitCounts[m_CurrentCheckpointIdx]++;
                    Debug.Log($"[AI·CP] Checkpoint #{m_CurrentCheckpointIdx} besucht (d={dist:F1}m) | Visits={m_CheckpointVisitCounts[m_CurrentCheckpointIdx]} | Counts=[{string.Join(",", m_CheckpointVisitCounts)}]");
                    m_CurrentCheckpointIdx = -1;
                }
            }

            // Neuen Checkpoint waehlen: mit Chance ein Breadcrumb, sonst Least-Visited
            if (m_CurrentCheckpointIdx < 0 && !m_BreadcrumbTarget.HasValue)
            {
                // Breadcrumb-Chance nur wenn Breadcrumbs vorhanden und kein Spieler sichtbar
                if (s_BreadcrumbCount > 0 && !PlayerSensorDetected && Random.value < k_BreadcrumbChance)
                {
                    Vector3? bc = PickRandomBreadcrumb(fromPosition);
                    if (bc.HasValue)
                    {
                        m_BreadcrumbTarget = bc.Value;
                        return m_BreadcrumbTarget.Value;
                    }
                }

                m_CurrentCheckpointIdx = PickLeastVisitedCheckpoint();
            }

            return m_CurrentCheckpointIdx >= 0 ? s_Checkpoints[m_CurrentCheckpointIdx] : null;
        }

        /// <summary>
        /// Waehlt den naechsten Checkpoint aus den am wenigsten besuchten.
        /// Unter den Kandidaten mit dem niedrigsten Visit-Count wird zufaellig gewaehlt.
        /// Unerreichbare Checkpoints werden uebersprungen.
        /// Wenn alle erreichbaren gleich oft besucht wurden, werden unerreichbare zurueckgesetzt.
        /// </summary>
        private int PickLeastVisitedCheckpoint()
        {
            if (m_CheckpointVisitCounts == null || s_Checkpoints == null)
            {
                return -1;
            }

            // Minimalen Visit-Count unter erreichbaren Checkpoints finden
            int minVisits = int.MaxValue;
            for (int i = 0; i < s_Checkpoints.Length; i++)
            {
                if (m_UnreachableCheckpoints.Contains(i))
                {
                    continue;
                }

                if (m_CheckpointVisitCounts[i] < minVisits)
                {
                    minVisits = m_CheckpointVisitCounts[i];
                }
            }

            // Keine erreichbaren Checkpoints → unerreichbare zuruecksetzen und nochmal versuchen
            if (minVisits == int.MaxValue)
            {
                if (m_UnreachableCheckpoints.Count > 0)
                {
                    Debug.Log($"[AI·CP] Alle erreichbaren besucht — {m_UnreachableCheckpoints.Count} unerreichbare zurueckgesetzt");
                    m_UnreachableCheckpoints.Clear();
                    return PickLeastVisitedCheckpoint();
                }

                return -1;
            }

            // Alle Kandidaten mit minVisits sammeln
            System.Collections.Generic.List<int> candidates = new();
            for (int i = 0; i < s_Checkpoints.Length; i++)
            {
                if (m_UnreachableCheckpoints.Contains(i))
                {
                    continue;
                }

                if (m_CheckpointVisitCounts[i] == minVisits)
                {
                    candidates.Add(i);
                }
            }

            // Zufaellig aus den Least-Visited waehlen
            if (candidates.Count == 0)
            {
                return -1;
            }

            int chosen;

            // Stealth Awareness Bias: Wenn ein Spieler im Grossradius erkannt wurde,
            // bevorzuge den Kandidaten der am naechsten zur erkannten Spielerposition liegt.
            // Gewichtete Zufallsauswahl: naehere Checkpoints haben hoehere Chancen.
            if (m_StealthAwarenessActive && candidates.Count > 1)
            {
                // Inverse-Distance-Weighting: Checkpoints naeher am Spieler bekommen hoehere Gewichtung
                float totalWeight = 0f;
                float[] weights = new float[candidates.Count];
                for (int c = 0; c < candidates.Count; c++)
                {
                    float cpDist = HorizontalDistance(s_Checkpoints[candidates[c]], m_StealthAwarenessPosition);
                    // Inverse Distanz + Minimum um Division by Zero zu vermeiden
                    weights[c] = 1f / (cpDist + 1f);
                    totalWeight += weights[c];
                }

                // Gewichtete Zufallsauswahl
                float roll = Random.Range(0f, totalWeight);
                chosen = candidates[candidates.Count - 1]; // Fallback
                float accumWeight = 0f;
                for (int c = 0; c < candidates.Count; c++)
                {
                    accumWeight += weights[c];
                    if (roll <= accumWeight)
                    {
                        chosen = candidates[c];
                        break;
                    }
                }

                float chosenDist = HorizontalDistance(transform.position, s_Checkpoints[chosen]);
                float playerDist = HorizontalDistance(s_Checkpoints[chosen], m_StealthAwarenessPosition);
                Debug.Log($"[AI·CP] Stealth-Biased Checkpoint #{chosen} (visits={minVisits}, candidates={candidates.Count}, d={chosenDist:F1}m, playerProx={playerDist:F1}m)");
            }
            else
            {
                chosen = candidates[Random.Range(0, candidates.Count)];
                float dist = HorizontalDistance(transform.position, s_Checkpoints[chosen]);
                Debug.Log($"[AI·CP] Least-Visited Checkpoint #{chosen} (visits={minVisits}, candidates={candidates.Count}, d={dist:F1}m)");
            }

            return chosen;
        }

        /// <summary>Berechnet die horizontale (XZ) Distanz zwischen zwei Punkten (Y wird ignoriert).</summary>
        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // ===== Player Breadcrumbs (dynamische Patrol-Ziele aus Spielerbewegung) =====

        /// <summary>
        /// Nimmt Spieler-Positionen als Breadcrumbs auf (statisch, einmal pro Intervall fuer alle Bots).
        /// Sucht alle lebenden Spieler per OverlapSphere und speichert ihre Positionen
        /// in einen Circular Buffer wenn sie sich genug bewegt haben.
        /// </summary>
        private void RecordPlayerBreadcrumbs()
        {
            if (!s_BreadcrumbsInitialized)
            {
                s_Breadcrumbs = new Vector3[k_MaxBreadcrumbs];
                s_BreadcrumbWriteIdx = 0;
                s_BreadcrumbCount = 0;
                s_BreadcrumbTimer = 0f;
                s_BreadcrumbsInitialized = true;
            }

            s_BreadcrumbTimer += Time.deltaTime;
            if (s_BreadcrumbTimer < k_BreadcrumbInterval)
            {
                return;
            }

            s_BreadcrumbTimer = 0f;

            // Alle lebenden Spieler auf der Map finden (grosser Radius, kein LOS)
            Collider[] hits = new Collider[16];
            int hitCount = Physics.OverlapSphereNonAlloc(
                Vector3.zero, 9999f, hits, k_PlayerLayerMask);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = hits[i];
                if (col == null)
                {
                    continue;
                }

                // Bots ignorieren (nur echte Spieler)
                if (col.GetComponentInParent<AIBotController>() != null)
                {
                    continue;
                }

                // Lebt der Spieler?
                NetworkedCharacterState targetState = col.GetComponentInParent<NetworkedCharacterState>();
                if (targetState == null || !targetState.IsAlive)
                {
                    continue;
                }

                Vector3 playerPos = col.transform.position;

                s_Breadcrumbs[s_BreadcrumbWriteIdx] = playerPos;
                s_BreadcrumbWriteIdx = (s_BreadcrumbWriteIdx + 1) % k_MaxBreadcrumbs;
                if (s_BreadcrumbCount < k_MaxBreadcrumbs)
                {
                    s_BreadcrumbCount++;
                }
            }
        }

        /// <summary>
        /// Waehlt eine Breadcrumb-Position als Patrol-Ziel mit Recency-Gewichtung.
        /// Neuere Breadcrumbs werden stark bevorzugt (Spieler war dort kuerzlich).
        /// Mindestdistanz zum Bot wird eingehalten.
        /// Gibt null zurueck wenn keine geeignete Breadcrumb vorhanden.
        /// </summary>
        private Vector3? PickRandomBreadcrumb(Vector3 fromPosition)
        {
            if (s_BreadcrumbCount == 0)
            {
                return null;
            }

            // Gewichtete Auswahl: neuere Breadcrumbs haben hoehere Gewichtung.
            // Index 0 = aelteste, s_BreadcrumbCount-1 = neueste im logischen Ring.
            // Gewicht = (logicalIndex + 1)^2 → neueste ~128x wahrscheinlicher als aelteste.
            for (int attempt = 0; attempt < 15; attempt++)
            {
                // Quadratische Verteilung: sqrt(uniform) erzeugt Bias zu hohen Werten
                float t = Mathf.Sqrt(Random.value);
                int logicalIdx = Mathf.FloorToInt(t * s_BreadcrumbCount);
                if (logicalIdx >= s_BreadcrumbCount)
                {
                    logicalIdx = s_BreadcrumbCount - 1;
                }

                // Logischen Index in Ring-Index umrechnen (0 = aelteste → neueste zuerst)
                int ringIdx = (s_BreadcrumbWriteIdx - s_BreadcrumbCount + logicalIdx + k_MaxBreadcrumbs) % k_MaxBreadcrumbs;
                Vector3 bc = s_Breadcrumbs[ringIdx];

                // Zu nah am Bot → ueberspringen (sonst laeuft er im Kreis)
                if (HorizontalDistance(fromPosition, bc) < k_BreadcrumbMinPickDistance)
                {
                    continue;
                }

                // Pruefen ob die Position auf dem NavMesh liegt
                if (NavMesh.SamplePosition(bc, out NavMeshHit _, k_NavMeshSampleHeight, NavMesh.AllAreas))
                {
                    int age = s_BreadcrumbCount - 1 - logicalIdx;
                    Debug.Log($"[AI·CP] Breadcrumb gewaehlt ({bc.x:F1},{bc.y:F1},{bc.z:F1}) d={HorizontalDistance(fromPosition, bc):F1}m age={age} | Pool={s_BreadcrumbCount}");
                    return bc;
                }
            }

            return null;
        }

        /// <summary>Setzt den Breadcrumb-Buffer zurueck (z.B. bei Map-Wechsel).</summary>
        public static void ClearBreadcrumbs()
        {
            s_BreadcrumbWriteIdx = 0;
            s_BreadcrumbCount = 0;
            s_BreadcrumbTimer = 0f;
        }

        // ===== Proaktive Sensor-Hindernis-Reaktion =====

        /// <summary>Schwelldistanz fuer nahe Hindernisse in Metern.</summary>
        private const float k_ObstacleNearThreshold = 2.5f;

        /// <summary>Schwelldistanz fuer sehr nahe Hindernisse (sofortiges Ausweichen).</summary>
        private const float k_ObstacleCloseThreshold = 1.2f;

        // ===== Wand-Abstossung (Corridor Centering) =====

        /// <summary>Seitensensor-Distanz ab der die Wand-Abstossung aktiv wird (Meter).</summary>
        private const float k_WallRepulsionDistance = 5f;

        /// <summary>Maximale laterale Eingabe durch Wand-Abstossung (0..1).</summary>
        private const float k_WallRepulsionStrength = 0.5f;

        /// <summary>
        /// Liest die 7x5 Sensor3D-Werte und reagiert proaktiv auf Hindernisse:
        /// - Untere Sensoren nah + obere frei → Springen (ueber Hindernis).
        /// - Obere Sensoren nah + untere frei → Ducken (unter Hindernis durch).
        /// - Frontale Sensoren blockiert → seitliches Ausweichen zur freieren Seite.
        /// Wird nur bei aktiver Bewegung ausgefuehrt (hat MoveTarget).
        /// Sensor-Grid: 7 horizontal (h0=-60°..h6=+60°) x 5 vertikal (v0=-20°..v4=+20°).
        /// Index = v * 7 + h, Mittelstrahl = h=3, v=2.
        /// </summary>
        private void ReactToSensorObstacles()
        {
            // Nur bei aktiver Bewegung reagieren und wenn Sensoren bereit sind
            if (!m_MoveTarget.HasValue || m_SensorArray == null || !m_SensorsReady)
            {
                return;
            }

            // Im Kampf: 180°-Turn unterdruecken (Bot wuerde das Ziel komplett verlieren).
            // Springen, Ducken und kurzes Side-Step bleiben erlaubt — sonst bleibt der Bot
            // im Melee-Chase an Hindernissen kleben (Stufen, Kanten, Tueren).
            bool combatMode = m_ShouldAttack;

            Sensor3D[] sensors = m_SensorArray.Sensors;
            if (sensors == null || sensors.Length < 35)
            {
                return;
            }

            // Zentrale frontale Sensoren pruefen (h=2,3,4 = ±17° um Mitte)
            // Untere Reihen (v=0,1): Hindernis auf Beinhoehe → drueberspringen
            // Obere Reihen (v=3,4): Hindernis auf Kopfhoehe → drunterducken
            // Mittlere Reihe (v=2): Blockade direkt voraus

            float bottomMinDist = float.MaxValue;
            float topMinDist = float.MaxValue;
            float centerMinDist = float.MaxValue;
            float leftMinDist = float.MaxValue;
            float rightMinDist = float.MaxValue;

            // Untere Sensoren (v=0,1) im frontalen Bereich (h=2,3,4)
            for (int v = 0; v <= 1; v++)
            {
                for (int h = 2; h <= 4; h++)
                {
                    Sensor3D s = sensors[v * 7 + h];
                    if (s != null && s.HitLayer >= 0 && s.HitLayer != k_PlayerLayer)
                    {
                        if (s.RawDistance < bottomMinDist)
                        {
                            bottomMinDist = s.RawDistance;
                        }
                    }
                }
            }

            // Obere Sensoren (v=3,4) im frontalen Bereich (h=2,3,4)
            for (int v = 3; v <= 4; v++)
            {
                for (int h = 2; h <= 4; h++)
                {
                    Sensor3D s = sensors[v * 7 + h];
                    if (s != null && s.HitLayer >= 0 && s.HitLayer != k_PlayerLayer)
                    {
                        if (s.RawDistance < topMinDist)
                        {
                            topMinDist = s.RawDistance;
                        }
                    }
                }
            }

            // Mittlere Sensoren (v=2) im frontalen Bereich (h=2,3,4)
            for (int h = 2; h <= 4; h++)
            {
                Sensor3D s = sensors[2 * 7 + h];
                if (s != null && s.HitLayer >= 0 && s.HitLayer != k_PlayerLayer)
                {
                    if (s.RawDistance < centerMinDist)
                    {
                        centerMinDist = s.RawDistance;
                    }
                }
            }

            // Linke Sensoren (h=0,1) ueber alle vertikalen Reihen
            for (int v = 0; v < 5; v++)
            {
                for (int h = 0; h <= 1; h++)
                {
                    Sensor3D s = sensors[v * 7 + h];
                    if (s != null && s.HitLayer >= 0 && s.HitLayer != k_PlayerLayer)
                    {
                        if (s.RawDistance < leftMinDist)
                        {
                            leftMinDist = s.RawDistance;
                        }
                    }
                }
            }

            // Rechte Sensoren (h=5,6) ueber alle vertikalen Reihen
            for (int v = 0; v < 5; v++)
            {
                for (int h = 5; h <= 6; h++)
                {
                    Sensor3D s = sensors[v * 7 + h];
                    if (s != null && s.HitLayer >= 0 && s.HitLayer != k_PlayerLayer)
                    {
                        if (s.RawDistance < rightMinDist)
                        {
                            rightMinDist = s.RawDistance;
                        }
                    }
                }
            }

            // Entscheidungslogik:

            // 1. Springen: Unten nah + oben frei → Hindernis auf Beinhoehe
            if (bottomMinDist < k_ObstacleNearThreshold && topMinDist > k_ObstacleNearThreshold * 2f)
            {
                m_ShouldJump = true;
            }

            // 2. Ducken: Oben nah + unten frei → Hindernis auf Kopfhoehe
            if (topMinDist < k_ObstacleNearThreshold && bottomMinDist > k_ObstacleNearThreshold * 2f)
            {
                m_ShouldCrouch = true;
            }

            // 3. Umdrehen: ALLE Seiten blockiert (Front + Links + Rechts) → 180° Kehrtwendung
            //    Im Kampf NICHT umdrehen (sonst verliert der Bot das Ziel).
            if (!combatMode
                && centerMinDist < k_ObstacleCloseThreshold
                && leftMinDist < k_ObstacleNearThreshold
                && rightMinDist < k_ObstacleNearThreshold)
            {
                Vector3 behindBot = transform.position - transform.forward * 5f;
                m_MoveTarget = behindBot;
                // Pfad-Cache invalidieren damit NavMesh nach dem Umdrehen neu berechnet
                m_LastPathTarget = Vector3.zero;
                m_PathCorners = System.Array.Empty<Vector3>();
                m_PathIndex = 0;
            }
            // 4. Seitliches Ausweichen: Nur Mitte blockiert, eine Seite frei.
            //    Im Kampf trotzdem ausweichen damit Hindernisse beim Chase nicht haengen
            //    bleiben — Aim wird im naechsten Frame durch SetLookTarget wiederhergestellt.
            else if (centerMinDist < k_ObstacleCloseThreshold)
            {
                if (leftMinDist > rightMinDist)
                {
                    Vector3 leftDir = -transform.right * 3f + transform.forward * 2f;
                    m_MoveTarget = transform.position + leftDir;
                }
                else
                {
                    Vector3 rightDir = transform.right * 3f + transform.forward * 2f;
                    m_MoveTarget = transform.position + rightDir;
                }
            }
        }

        /// <summary>
        /// Berechnet eine laterale Abstossungs-Komponente basierend auf linken/rechten Seitensensoren.
        /// Positiver Wert = nach rechts strafen (linke Wand naeher).
        /// Negativer Wert = nach links strafen (rechte Wand naeher).
        /// Zentriert den Bot in Korridoren und verhindert Wand-Kleben.
        /// </summary>
        private float CalculateWallRepulsion()
        {
            if (m_SensorArray == null || !m_SensorsReady)
            {
                return 0f;
            }

            Sensor3D[] sensors = m_SensorArray.Sensors;
            if (sensors == null || sensors.Length < 35)
            {
                return 0f;
            }

            // Durchschnittliche Wand-Distanz links (h=0,1) und rechts (h=5,6)
            // ueber mittlere vertikale Reihen (v=1,2,3) — vermeidet Boden/Decken-Einfluss
            float leftSum = 0f;
            int leftCount = 0;
            float rightSum = 0f;
            int rightCount = 0;

            for (int v = 1; v <= 3; v++)
            {
                for (int h = 0; h <= 1; h++)
                {
                    Sensor3D s = sensors[v * 7 + h];
                    if (s != null && s.HitLayer >= 0 && s.HitLayer != k_PlayerLayer)
                    {
                        leftSum += s.RawDistance;
                        leftCount++;
                    }
                }

                for (int h = 5; h <= 6; h++)
                {
                    Sensor3D s = sensors[v * 7 + h];
                    if (s != null && s.HitLayer >= 0 && s.HitLayer != k_PlayerLayer)
                    {
                        rightSum += s.RawDistance;
                        rightCount++;
                    }
                }
            }

            // Keine Waende erkannt → keine Abstossung
            if (leftCount == 0 && rightCount == 0)
            {
                return 0f;
            }

            // Sensor-Max-Distanz als Fallback fuer Seiten ohne Treffer (= keine Wand)
            float maxDist = k_WallRepulsionDistance;
            float leftAvg = leftCount > 0 ? leftSum / leftCount : maxDist;
            float rightAvg = rightCount > 0 ? rightSum / rightCount : maxDist;

            // Nur aktiv wenn mindestens eine Seite innerhalb der Schwelldistanz
            if (leftAvg >= k_WallRepulsionDistance && rightAvg >= k_WallRepulsionDistance)
            {
                return 0f;
            }

            // Abstossungskraft: linear stärker je naeher die Wand
            // leftForce > 0 = linke Wand nah → nach rechts druecken
            float leftForce = leftAvg < k_WallRepulsionDistance
                ? (1f - leftAvg / k_WallRepulsionDistance) * k_WallRepulsionStrength
                : 0f;

            // rightForce > 0 = rechte Wand nah → nach links druecken
            float rightForce = rightAvg < k_WallRepulsionDistance
                ? (1f - rightAvg / k_WallRepulsionDistance) * k_WallRepulsionStrength
                : 0f;

            // Netto-Abstossung: positiv = nach rechts, negativ = nach links
            return leftForce - rightForce;
        }

        // ===== Stuck Detection (4-Phasen-Eskalation) =====

        /// <summary>
        /// Prueft ob der Bot feststeckt und eskaliert die Befreiungsstrategie:
        /// Phase 1 (2s): Wegpunkt ueberspringen + vertikaler Sprung.
        /// Phase 2 (3s): Vorwaerts-Sprung Richtung naechstem Wegpunkt.
        /// Phase 3 (5s): 180° drehen + rueckwaerts laufen + ducken fuer 1s.
        /// Phase 4 (7s): Aktuellen Checkpoint aufgeben, als besucht markieren, neuen waehlen.
        /// </summary>
        private void CheckStuck()
        {
            // Phase 3 aktiv: Rueckwaertsbewegung ausfuehren
            if (m_StuckReverseTimer > 0f)
            {
                m_StuckReverseTimer -= Time.deltaTime;

                // Ducken nur wenn der Bot nicht gerade einen Spieler verfolgt —
                // beim Chase verlangsamt PmDuckScale=0.25 die Befreiung massiv.
                if (!PlayerSensorDetected)
                {
                    m_ShouldCrouch = true;
                }

                // Bewegungsrichtung umkehren: MoveTarget hinter den Bot setzen
                Vector3 behindBot = transform.position - transform.forward * 5f;
                m_MoveTarget = behindBot;
                m_StuckOverrideYaw = m_CurrentYaw + 180f;

                if (m_StuckReverseTimer <= 0f)
                {
                    // Rueckwaertsbewegung beendet — Pfad-Neuberechnung erzwingen
                    m_StuckOverrideYaw = float.NaN;
                    m_LastPathTarget = Vector3.zero;
                    m_PathCorners = System.Array.Empty<Vector3>();
                    m_PathIndex = 0;
                    Debug.Log("[AI·Stuck] Phase 3 beendet — Pfad-Neuberechnung");
                }

                return;
            }

            if (!m_MoveTarget.HasValue || m_PathCorners.Length == 0)
            {
                m_StuckTimer = 0f;
                m_StuckCheckAccum = 0f;
                m_StuckPhase = 0;
                return;
            }

            // Im aktiven Kampf (Schiessen) nicht als stuck werten
            if (m_ShouldAttack)
            {
                m_StuckTimer = 0f;
                m_StuckCheckAccum = 0f;
                m_StuckPhase = 0;
                return;
            }

            // Beim Verfolgen: Stuck erkennen und letzte gespiegelte Spieler-Aktion nochmal ausfuehren
            bool isChasingPlayer = PlayerSensorDetected && !m_ShouldAttack;

            m_StuckCheckAccum += Time.deltaTime;
            if (m_StuckCheckAccum < k_StuckCheckInterval)
            {
                return;
            }

            m_StuckCheckAccum = 0f;

            float movedDist = HorizontalDistance(transform.position, m_StuckCheckPos);
            m_StuckCheckPos = transform.position;

            if (movedDist >= k_StuckDistThreshold)
            {
                // Bot bewegt sich — alles zuruecksetzen
                m_StuckTimer = 0f;
                m_StuckPhase = 0;
                return;
            }

            m_StuckTimer += k_StuckCheckInterval;

            // Stuck waehrend Chase: Nur Jump spiegeln — Ducken verlangsamt den Bot
            // (PmDuckScale=0.25) und bricht Bhop-Beschleunigung beim Verfolgen.
            if (isChasingPlayer && m_StuckTimer >= k_StuckPhase1)
            {
                if (m_LastMirrorJump)
                {
                    m_ShouldJump = true;
                }

                Debug.Log($"[AI·Stuck] Chase-Replay: Jump={m_LastMirrorJump} | T={m_StuckTimer:F1}s");
            }

            // Phase 1: Wegpunkt ueberspringen + vertikaler Sprung
            if (m_StuckPhase == 0 && m_StuckTimer >= k_StuckPhase1)
            {
                m_StuckPhase = 1;
                Debug.LogWarning($"[AI·Stuck] Phase 1 (Skip+Jump) | T={m_StuckTimer:F1}s idx={m_PathIndex}/{m_PathCorners.Length} | Pos=({transform.position.x:F1},{transform.position.y:F1},{transform.position.z:F1})");

                m_PathIndex++;
                if (m_PathIndex >= m_PathCorners.Length)
                {
                    m_LastPathTarget = Vector3.zero;
                    m_PathCorners = System.Array.Empty<Vector3>();
                    m_PathIndex = 0;
                }

                m_ShouldJump = true;
            }

            // Phase 2: Vorwaerts-Sprung Richtung Wegpunkt (MoveInput bleibt vorwaerts)
            if (m_StuckPhase == 1 && m_StuckTimer >= k_StuckPhase2)
            {
                m_StuckPhase = 2;
                Debug.LogWarning($"[AI·Stuck] Phase 2 (Forward Jump) | T={m_StuckTimer:F1}s | Pos=({transform.position.x:F1},{transform.position.y:F1},{transform.position.z:F1})");

                // Sprung + volle Vorwaertsbewegung — MoveTarget bleibt erhalten,
                // dadurch laeuft der Bot mit Schwung ueber das Hindernis
                m_ShouldJump = true;
            }

            // Phase 3: 180° drehen + rueckwaerts + ducken
            if (m_StuckPhase == 2 && m_StuckTimer >= k_StuckPhase3)
            {
                m_StuckPhase = 3;
                Debug.LogWarning($"[AI·Stuck] Phase 3 (Reverse+Crouch) | T={m_StuckTimer:F1}s | Pos=({transform.position.x:F1},{transform.position.y:F1},{transform.position.z:F1})");

                m_StuckReverseTimer = 1.0f;
            }

            // Phase 4: Checkpoint aufgeben
            if (m_StuckPhase == 3 && m_StuckTimer >= k_StuckPhase4)
            {
                m_StuckPhase = 0;
                m_StuckTimer = 0f;

                Debug.LogWarning($"[AI·Stuck] Phase 4 (Checkpoint aufgeben) | Pos=({transform.position.x:F1},{transform.position.y:F1},{transform.position.z:F1})");

                // Aktuellen Ziel-Checkpoint als unerreichbar markieren und naechsten aus Shuffle waehlen
                if (m_CurrentCheckpointIdx >= 0)
                {
                    m_UnreachableCheckpoints.Add(m_CurrentCheckpointIdx);
                    Debug.Log($"[AI·Stuck] Checkpoint #{m_CurrentCheckpointIdx} als unerreichbar markiert");
                    m_CurrentCheckpointIdx = -1;
                }
                else if (s_Checkpoints != null && m_MoveTarget.HasValue)
                {
                    float nearestDist = float.MaxValue;
                    int nearestIdx = -1;
                    for (int i = 0; i < s_Checkpoints.Length; i++)
                    {
                        float dist = HorizontalDistance(s_Checkpoints[i], m_MoveTarget.Value);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestIdx = i;
                        }
                    }

                    if (nearestIdx >= 0)
                    {
                        m_UnreachableCheckpoints.Add(nearestIdx);
                        Debug.Log($"[AI·Stuck] Checkpoint #{nearestIdx} als unerreichbar markiert");
                    }
                }

                // Pfad komplett zuruecksetzen
                m_LastPathTarget = Vector3.zero;
                m_PathCorners = System.Array.Empty<Vector3>();
                m_PathIndex = 0;
                m_MoveTarget = null;
            }
        }

        // ===== Damage Reaction =====

        /// <summary>
        /// Wird aufgerufen wenn der Bot Schaden erhaelt. Speichert die Position des Angreifers
        /// fuer GOAP-Damage-Reaction (Zuruckfeuern in Richtung des Angreifers).
        /// </summary>
        /// <param name="attackerPosition">Weltposition des Angreifers.</param>
        /// <param name="damageAmount">Erlittener Schaden.</param>
        public void NotifyDamageTaken(Vector3 attackerPosition, int damageAmount)
        {
            m_LastAttackerPosition = attackerPosition;
            m_LastDamageTime = Time.time;
            Debug.Log($"[AI·Damage] Bot hat {damageAmount} Schaden erhalten von ({attackerPosition.x:F1},{attackerPosition.y:F1},{attackerPosition.z:F1}) | Pos=({transform.position.x:F1},{transform.position.y:F1},{transform.position.z:F1})");
        }

        /// <summary>Setzt die Damage-Reaction zurueck (z.B. nach Runden-Neustart).</summary>
        public void ClearDamageReaction()
        {
            m_LastAttackerPosition = null;
            m_LastDamageTime = 0f;
        }

        // ===== NavMesh-Pfadberechnung =====

        /// <summary>
        /// Markiert den naechsten Checkpoint zur angegebenen Position als unerreichbar.
        /// Verhindert dass der Bot immer wieder dasselbe unerreichbare Ziel ansteuert.
        /// </summary>
        private void MarkNearestCheckpointUnreachable(Vector3 targetPos)
        {
            if (s_Checkpoints == null)
            {
                return;
            }

            float nearestDist = float.MaxValue;
            int nearestIdx = -1;
            for (int i = 0; i < s_Checkpoints.Length; i++)
            {
                float dist = Vector3.Distance(s_Checkpoints[i], targetPos);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestIdx = i;
                }
            }

            if (nearestIdx >= 0 && nearestDist < 5f)
            {
                m_UnreachableCheckpoints.Add(nearestIdx);
                Debug.Log($"[AI·Nav] Checkpoint #{nearestIdx} als unerreichbar markiert (nicht auf NavMesh)");
            }
        }

        /// <summary>Mindestabstand in Metern den Pfad-Corners von NavMesh-Kanten haben sollen.</summary>
        private const float k_PathCornerInsetDistance = 0.8f;

        /// <summary>
        /// Schiebt Pfad-Corners von NavMesh-Kanten (Waenden) weg.
        /// Verhindert dass der Bot direkt an Waenden entlanglaueft.
        /// Nutzt NavMesh.FindClosestEdge um die naechste Kante zu finden
        /// und verschiebt den Corner in die entgegengesetzte Richtung.
        /// Start- und End-Corner werden nicht verschoben.
        /// </summary>
        private void InsetPathCornersFromWalls()
        {
            if (m_PathCorners.Length <= 2)
            {
                return;
            }

            // Nur innere Corners verschieben (Start und Ziel bleiben)
            for (int i = 1; i < m_PathCorners.Length - 1; i++)
            {
                Vector3 corner = m_PathCorners[i];

                if (!NavMesh.FindClosestEdge(corner, out NavMeshHit edgeHit, NavMesh.AllAreas))
                {
                    continue;
                }

                // Wenn die Kante naeher als der gewuenschte Abstand ist → wegschieben
                if (edgeHit.distance < k_PathCornerInsetDistance)
                {
                    // Richtung von der Kante weg (Edge-Normal zeigt ins NavMesh hinein)
                    Vector3 pushDir = edgeHit.normal;
                    pushDir.y = 0f;

                    if (pushDir.sqrMagnitude < 0.001f)
                    {
                        // Fallback: Richtung vom Edge-Punkt zum Corner
                        pushDir = (corner - edgeHit.position);
                        pushDir.y = 0f;
                    }

                    if (pushDir.sqrMagnitude > 0.001f)
                    {
                        pushDir.Normalize();
                        float pushAmount = k_PathCornerInsetDistance - edgeHit.distance;
                        Vector3 newCorner = corner + pushDir * pushAmount;

                        // Sicherstellen dass der neue Punkt noch auf dem NavMesh liegt
                        if (NavMesh.SamplePosition(newCorner, out NavMeshHit sampleHit, 1f, NavMesh.AllAreas))
                        {
                            m_PathCorners[i] = sampleHit.position;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// String-Pulling / Simplify-Pass: Entfernt ueberfluessige Zwischen-Corners.
        /// Fuer jeden Corner wird geprueft ob der uebernaechste per NavMesh.Raycast
        /// direkt erreichbar ist → mittleren Corner entfernen.
        /// Erzeugt glattere, natuerlichere Pfade ohne Zickzack.
        /// </summary>
        private void SmoothPath()
        {
            if (m_PathCorners.Length <= 2)
            {
                return;
            }

            System.Collections.Generic.List<Vector3> smoothed = new() { m_PathCorners[0] };

            int current = 0;
            while (current < m_PathCorners.Length - 1)
            {
                // Versuche den am weitesten direkt erreichbaren Corner zu finden
                int farthestDirect = current + 1;

                for (int probe = current + 2; probe < m_PathCorners.Length; probe++)
                {
                    // NavMesh.Raycast gibt true zurueck wenn der Strahl auf eine Kante trifft (blockiert)
                    if (!NavMesh.Raycast(m_PathCorners[current], m_PathCorners[probe], out NavMeshHit _, NavMesh.AllAreas))
                    {
                        // Direkter Weg frei → diesen Corner als Kandidat merken
                        farthestDirect = probe;
                    }
                    else
                    {
                        // Blockiert → weiter testen bringt nichts (Corners dazwischen waren schon frei)
                        break;
                    }
                }

                smoothed.Add(m_PathCorners[farthestDirect]);
                current = farthestDirect;
            }

            m_PathCorners = smoothed.ToArray();
        }

        /// <summary>
        /// Berechnet einen NavMesh-Pfad zum Ziel (nur wenn sich das Ziel geaendert hat
        /// oder der Bot weit genug vom letzten Pfad-Ziel entfernt ist).
        /// Nutzt waehrend Chase/Shoot einen reduzierten Recalc-Threshold fuer reaktiveres Umpathing.
        /// </summary>
        private void UpdateNavPath(Vector3 targetPos)
        {
            // Dynamischen Threshold waehlen: Chase = reaktiver, Patrol = stabiler
            float threshold = m_IsDirectLOSSteering ? k_PathRecalcThresholdChase : k_PathRecalcThreshold;

            // Nur neu berechnen wenn sich das Ziel signifikant geaendert hat
            if (m_PathCorners.Length > 0
                && Vector3.Distance(targetPos, m_LastPathTarget) < threshold)
            {
                return;
            }

            if (m_NavPath == null)
            {
                m_NavPath = new NavMeshPath();
            }

            Vector3 startPos = transform.position;

            // Positionen auf NavMesh projizieren
            if (!NavMesh.SamplePosition(startPos, out NavMeshHit startHit, k_NavMeshSampleHeight, NavMesh.AllAreas))
            {
                // Bot ist nicht auf dem NavMesh — Pfad nicht berechenbar
                m_PathCorners = System.Array.Empty<Vector3>();
                m_PathIndex = 0;
                Debug.LogWarning($"[AI·Nav] SamplePosition START fehlgeschlagen | BotPos=({startPos.x:F1},{startPos.y:F1},{startPos.z:F1}) | SampleHeight={k_NavMeshSampleHeight}");
                return;
            }

            if (!NavMesh.SamplePosition(targetPos, out NavMeshHit endHit, k_NavMeshSampleHeight, NavMesh.AllAreas))
            {
                // Ziel ist nicht auf dem NavMesh — Checkpoint als unerreichbar markieren
                m_PathCorners = System.Array.Empty<Vector3>();
                m_PathIndex = 0;
                MarkNearestCheckpointUnreachable(targetPos);
                Debug.LogWarning($"[AI·Nav] SamplePosition END fehlgeschlagen | TargetPos=({targetPos.x:F1},{targetPos.y:F1},{targetPos.z:F1}) | SampleHeight={k_NavMeshSampleHeight} | Checkpoint übersprungen");
                return;
            }

            if (NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, m_NavPath)
                && m_NavPath.status != NavMeshPathStatus.PathInvalid)
            {
                m_PathCorners = m_NavPath.corners;
                InsetPathCornersFromWalls();
                SmoothPath();
                m_PathIndex = 1; // Index 0 ist die Startposition, ueberspringen
                m_LastPathTarget = targetPos;
                Debug.Log($"[AI·Nav] Pfad OK: {m_PathCorners.Length} corners | Status={m_NavPath.status} | Start=({startHit.position.x:F1},{startHit.position.y:F1},{startHit.position.z:F1}) → End=({endHit.position.x:F1},{endHit.position.y:F1},{endHit.position.z:F1})");
            }
            else
            {
                // Pfadberechnung fehlgeschlagen — Fallback auf Direktbewegung
                m_PathCorners = System.Array.Empty<Vector3>();
                m_PathIndex = 0;
                Debug.LogWarning($"[AI·Nav] Pfad FEHLGESCHLAGEN | Status={m_NavPath.status} | Start=({startHit.position.x:F1},{startHit.position.y:F1},{startHit.position.z:F1}) → End=({endHit.position.x:F1},{endHit.position.y:F1},{endHit.position.z:F1}) | BotPos=({transform.position.x:F1},{transform.position.y:F1},{transform.position.z:F1}) → TargetPos=({targetPos.x:F1},{targetPos.y:F1},{targetPos.z:F1})");
            }
        }

        /// <summary>
        /// Gibt den naechsten Steuerungspunkt zurueck (NavMesh-Wegpunkt oder Direktziel).
        /// Rueckt automatisch zum naechsten Wegpunkt vor wenn der aktuelle erreicht wurde.
        /// </summary>
        private Vector3 GetSteeringTarget(Vector3 finalTarget)
        {
            if (m_PathCorners.Length == 0)
            {
                return finalTarget;
            }

            // Zum naechsten Wegpunkt vorrücken wenn der aktuelle erreicht wurde
            while (m_PathIndex < m_PathCorners.Length)
            {
                Vector3 wp = m_PathCorners[m_PathIndex];
                float dist = Vector3.Distance(
                    new Vector3(transform.position.x, 0f, transform.position.z),
                    new Vector3(wp.x, 0f, wp.z));

                if (dist > k_WaypointArrivalDist)
                {
                    return wp;
                }

                m_PathIndex++;
            }

            // Alle Wegpunkte abgelaufen — letzten NavMesh-Wegpunkt nutzen
            // (NavMesh-projiziert, korrekte Y-Hoehe) statt des rohen Ziels,
            // das eine andere Y-Koordinate haben kann als der Bot.
            if (m_PathCorners.Length > 0)
            {
                return m_PathCorners[m_PathCorners.Length - 1];
            }

            return finalTarget;
        }

        // ===== Debug-Visualisierung (In-Game LineRenderer) =====

        /// <summary>LineRenderer fuer die Pfad-Visualisierung im Game View.</summary>
        private LineRenderer m_PathLineRenderer;

        /// <summary>LineRenderer fuer die Linie Bot → aktueller Wegpunkt.</summary>
        private LineRenderer m_SteerLineRenderer;

        /// <summary>LineRenderer fuer die Linie zum Ziel-Checkpoint.</summary>
        private LineRenderer m_TargetLineRenderer;

        /// <summary>Gecachter Sprites/Default Shader fuer LineRenderer-Material.</summary>
        private static Shader s_CachedLineShader;

        /// <summary>
        /// Erstellt einen LineRenderer mit unbeleuchteter, transparenter Darstellung.
        /// </summary>
        private LineRenderer CreatePathLineRenderer(string objectName, Color color, float width)
        {
            GameObject go = new(objectName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.positionCount = 0;
            lr.numCornerVertices = 3;
            lr.numCapVertices = 3;

            if (s_CachedLineShader == null)
            {
                s_CachedLineShader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (s_CachedLineShader == null)
            {
                s_CachedLineShader = Shader.Find("Sprites/Default");
            }

            if (s_CachedLineShader == null)
            {
                s_CachedLineShader = Shader.Find("Unlit/Color");
            }

            if (s_CachedLineShader == null)
            {
                Debug.LogWarning("[AIBotController] Kein Shader fuer LineRenderer gefunden.");
                Object.Destroy(go);
                return null;
            }

            Material mat = new(s_CachedLineShader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.renderQueue = 5000;
            lr.material = mat;
            lr.startColor = color;
            lr.endColor = color;
            lr.sortingOrder = 100;

            return lr;
        }

        /// <summary>
        /// Aktualisiert die In-Game Pfad-Visualisierung per LineRenderer.
        /// Cyan = kompletter Pfad, Gelb = Bot → aktueller Wegpunkt, Rot = Ziel-Checkpoint.
        /// Sichtbar im Game View ohne Gizmos.
        /// </summary>
        private void DrawDebugPath()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            float offsetY = 0.2f;

            // LineRenderer lazy erstellen
            if (m_PathLineRenderer == null)
            {
                m_PathLineRenderer = CreatePathLineRenderer("AI_PathLine", Color.cyan, 0.08f);
            }

            if (m_SteerLineRenderer == null)
            {
                m_SteerLineRenderer = CreatePathLineRenderer("AI_SteerLine", Color.yellow, 0.12f);
            }

            if (m_TargetLineRenderer == null)
            {
                m_TargetLineRenderer = CreatePathLineRenderer("AI_TargetLine", Color.red, 0.06f);
            }

            // Shader nicht verfuegbar → keine Visualisierung moeglich
            if (m_PathLineRenderer == null || m_SteerLineRenderer == null || m_TargetLineRenderer == null)
            {
                return;
            }

            // Pfad-Linie (Cyan): Alle Corners
            if (m_PathCorners.Length > 1)
            {
                m_PathLineRenderer.positionCount = m_PathCorners.Length;
                for (int i = 0; i < m_PathCorners.Length; i++)
                {
                    m_PathLineRenderer.SetPosition(i, m_PathCorners[i] + Vector3.up * offsetY);
                }
            }
            else
            {
                m_PathLineRenderer.positionCount = 0;
            }

            // Steering-Linie (Gelb): Bot → aktueller Wegpunkt
            if (m_PathIndex < m_PathCorners.Length)
            {
                m_SteerLineRenderer.positionCount = 2;
                m_SteerLineRenderer.SetPosition(0, transform.position + Vector3.up * offsetY);
                m_SteerLineRenderer.SetPosition(1, m_PathCorners[m_PathIndex] + Vector3.up * offsetY);
            }
            else
            {
                m_SteerLineRenderer.positionCount = 0;
            }

            // Ziel-Linie (Rot): Vertikale Markierung am Checkpoint
            if (m_MoveTarget.HasValue)
            {
                m_TargetLineRenderer.positionCount = 2;
                m_TargetLineRenderer.SetPosition(0, m_MoveTarget.Value + Vector3.up * offsetY);
                m_TargetLineRenderer.SetPosition(1, m_MoveTarget.Value + Vector3.up * 3f);
            }
            else
            {
                m_TargetLineRenderer.positionCount = 0;
            }

            // Pfaddaten an Clients synchronisieren fuer client-seitige Visualisierung
            if (m_NetworkedAICharacter != null)
            {
                Vector3 steerEnd = m_PathIndex < m_PathCorners.Length ? m_PathCorners[m_PathIndex] : Vector3.zero;
                m_NetworkedAICharacter.SyncDebugPath(
                    m_PathCorners,
                    transform.position,
                    steerEnd,
                    m_MoveTarget.GetValueOrDefault(),
                    m_MoveTarget.HasValue);
            }
#endif
        }

        // ===== Sensor-Management =====

        /// <summary>
        /// Stellt sicher dass die Sensoren in diesem Frame getickt wurden.
        /// Wird von GOAP-Sensoren aufgerufen (Lazy-Tick-Pattern).
        /// </summary>
        public void EnsureSensorsTicked()
        {
            if (m_SensorsTickedThisFrame || m_SensorArray == null || !m_SensorsReady)
            {
                return;
            }

            m_SensorArray.TickAll();
            m_SensorsTickedThisFrame = true;
        }

        /// <summary>Setzt das Lazy-Tick-Flag zurueck (naechster Frame erlaubt neuen Tick).</summary>
        private void LateUpdate()
        {
            m_SensorsTickedThisFrame = false;
            m_RadiusScanDoneThisFrame = false;
        }

        /// <summary>
        /// Prueft ob der von einem Sensor getroffene Spieler ein gueltiges feindliches Ziel ist.
        /// Gibt false zurueck wenn kein Collider, Spieler tot, oder Teammitglied.
        /// Bei fehlendem CharacterState wird der Hit als Welt-Geometrie behandelt (true).
        /// </summary>
        private bool IsHitPlayerAlive(Sensor3D sensor)
        {
            if (sensor.HitCollider == null)
            {
                return false;
            }

            NetworkedCharacterState hitState = sensor.HitCollider.GetComponentInParent<NetworkedCharacterState>();
            if (hitState == null)
            {
                // Kein CharacterState → Welt-Geometrie / Prop, fuer FOV-Sensor weiterhin gueltig
                return true;
            }

            return IsHostileTarget(hitState);
        }

        /// <summary>
        /// Findet den naechsten lebenden Spieler per 360° Radius-Scan (OverlapSphere).
        /// Prueft Line-of-Sight per Raycast und filtert tote Spieler.
        /// Gibt die Weltrichtung und Distanz zum naechsten sichtbaren Spieler zurueck.
        /// </summary>
        /// <summary>Geschaetzte Kopfhoehe eines Spielers ueber dessen Fussposition (SoF2: 72/89 ratio).</summary>
        private const float k_TargetHeadHeight = 1.7f;

        public void FindNearestPlayerBySensors(out Vector3 direction, out float distanceMeters, out bool detected)
        {
            EnsureRadiusScan();

            direction = Vector3.zero;
            distanceMeters = m_NearestPlayerDistance;
            detected = m_NearestPlayerTransform != null;

            if (detected)
            {
                // Auf Kopfhoehe des Spielers zielen (nicht auf Fuesse)
                Vector3 playerHeadPos = m_NearestPlayerTransform.position + Vector3.up * k_TargetHeadHeight;
                Vector3 eyePos = EyePosition;

                // Predictive Aiming: Spieler-Velocity aus Position-Delta schaetzen
                float now = Time.time;
                float dt = now - m_TrackedPlayerLastTime;
                if (dt > 0.01f && m_TrackedPlayerLastTime > 0f)
                {
                    m_TrackedPlayerVelocity = (playerHeadPos - m_TrackedPlayerLastPos) / dt;
                }

                m_TrackedPlayerLastPos = playerHeadPos;
                m_TrackedPlayerLastTime = now;

                // Vorhalt-Offset: Ziel = aktuelle Position + Velocity * LeadTime * Personality-Lead-Compensation.
                // Rookies (aimLeadCompensation~0.3) schiessen fast auf aktuelle Position, Veteranen (~1) voll vorausziehend.
                float leadCompensation = Mathf.Clamp01(m_Personality.aimLeadCompensation);
                Vector3 leadOffset = m_TrackedPlayerVelocity * k_AimLeadTimeSec * leadCompensation;
                if (leadOffset.sqrMagnitude > k_AimLeadMaxOffset * k_AimLeadMaxOffset)
                {
                    leadOffset = leadOffset.normalized * k_AimLeadMaxOffset;
                }

                Vector3 predictedPos = playerHeadPos + leadOffset;
                direction = (predictedPos - eyePos).normalized;
                distanceMeters = Vector3.Distance(eyePos, predictedPos);

                // Personality-Accuracy: Aim-Noise auf direction. Bei accuracy=1 keine Streuung,
                // bei accuracy=0 maximale Streuung (k_MaxAimErrorDegrees). Perlin-Noise bewegt sich
                // smooth ueber Zeit (kein Frame-Jitter), Seed sorgt fuer Bot-individuelles Muster.
                float aimErrorDeg = Mathf.Clamp01(1f - m_Personality.accuracy) * k_MaxAimErrorDegrees;
                if (aimErrorDeg > 0.001f)
                {
                    float t = Time.time * 0.6f;
                    float yawErr = (Mathf.PerlinNoise(t, m_AimNoiseSeed) - 0.5f) * 2f * aimErrorDeg;
                    float pitchErr = (Mathf.PerlinNoise(m_AimNoiseSeed, t) - 0.5f) * 2f * aimErrorDeg;
                    direction = Quaternion.Euler(pitchErr, yawErr, 0f) * direction;
                }
            }
            else if (PlayerMemoryActive)
            {
                // Spieler nicht direkt sichtbar aber im Gedaechtnis → letzte bekannte Position verwenden
                Vector3 eyePos = EyePosition;
                Vector3 toLastKnown = m_LastKnownPlayerPosition - eyePos;
                distanceMeters = toLastKnown.magnitude;
                detected = true;

                if (distanceMeters > 0.1f)
                {
                    direction = toLastKnown / distanceMeters;
                }
            }
        }

        /// <summary>
        /// Fire-Pattern-Gate: Simuliert Trigger-Druecken/Loslassen-Rhythmus passend zum
        /// FireMode der aktuellen Waffe. Erkennt gefeuerte Schuesse anhand des
        /// Clip-Dekrements (kein Server-Roundtrip noetig).
        ///
        /// - "auto": Trigger gehalten (kein Gate).
        /// - "single": Nach jedem Schuss Trigger fuer triggerDisciplineSec loslassen.
        /// - "burst": Nach burstShots Schuessen Trigger fuer triggerDisciplineSec*1.5 loslassen.
        ///
        /// Gibt true zurueck wenn der Attack-Button diesen Tick gesetzt werden darf.
        /// </summary>
        private bool UpdateFirePatternGate()
        {
            if (m_CharacterState == null)
            {
                return true;
            }

            // Schuss-Erkennung via Clip-Dekrement (Infinite-Ammo-Waffen werden ueber FireDelay-Timer
            // unten abgedeckt; hier reicht uns das Clip-Delta als Schuss-Signal).
            int clip = m_CharacterState.CurrentClipAmmo;
            if (m_LastObservedClip < 0)
            {
                m_LastObservedClip = clip;
            }
            bool shotFired = clip < m_LastObservedClip;
            m_LastObservedClip = clip;

            // Aktive Trigger-Release-Phase blockt Attack komplett.
            if (Time.time < m_TriggerReleaseUntil)
            {
                return false;
            }

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(m_CharacterState.CurrentWeaponName);
            string fireMode = weapon?.Attack?.FireMode;
            if (string.IsNullOrEmpty(fireMode))
            {
                fireMode = "auto";
            }

            if (shotFired)
            {
                if (fireMode == "single")
                {
                    m_TriggerReleaseUntil = Time.time + Mathf.Max(0.05f, m_Personality.triggerDisciplineSec);
                    m_BurstShotsFired = 0;
                    return false;
                }

                if (fireMode == "burst")
                {
                    m_BurstShotsFired++;
                    int burstLimit = Mathf.Max(1, m_Personality.burstShots);
                    if (m_BurstShotsFired >= burstLimit)
                    {
                        m_TriggerReleaseUntil = Time.time + Mathf.Max(0.10f, m_Personality.triggerDisciplineSec * 1.5f);
                        m_BurstShotsFired = 0;
                        return false;
                    }
                }
                // "auto": einfach weiterhalten.
            }

            return true;
        }

        /// <summary>
        /// Fuehrt den Radius-Scan per OverlapSphere durch (einmal pro Frame, Lazy-Tick).
        /// Erkennt alle lebenden Spieler auf Layer 7 im Radius, prueft LOS per Raycast.
        /// </summary>
        private void EnsureRadiusScan()
        {
            if (m_RadiusScanDoneThisFrame)
            {
                return;
            }

            m_RadiusScanDoneThisFrame = true;
            m_NearestPlayerTransform = null;
            m_NearestPlayerCharacter = null;
            m_NearestPlayerDistance = float.MaxValue;
            m_NearestPlayerDistanceXZ = float.MaxValue;

            Vector3 eyePos = EyePosition;

            // Gedaechtnis aktualisieren: Letzte bekannte Position aus Bewegung des Bots nachfuehren
            // (Bot bewegt sich auf die Position zu → Distanz verringert sich natuerlich)
            if (PlayerMemoryActive)
            {
                m_LastKnownPlayerDistance = Vector3.Distance(eyePos, m_LastKnownPlayerPosition);
                m_LastKnownPlayerDistanceXZ = Vector3.Distance(
                    new Vector3(eyePos.x, 0f, eyePos.z),
                    new Vector3(m_LastKnownPlayerPosition.x, 0f, m_LastKnownPlayerPosition.z));
            }
            int hitCount = Physics.OverlapSphereNonAlloc(eyePos, k_PlayerDetectionRadius, m_RadiusHits, k_PlayerLayerMask);

            // LOS-Check LayerMask: Environment-Layer die den Blick blockieren koennen
            // Default (0) + Ground (6) + BrushCollision (9) — aber NICHT Player (7)
            int losBlockMask = (1 << 0) | (1 << 6) | (1 << 9);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = m_RadiusHits[i];
                if (col == null)
                {
                    continue;
                }

                // Eigenen Collider ignorieren
                if (col.transform.root == transform.root)
                {
                    continue;
                }

                // Feindliches Ziel? (lebend + anderes Team / FFA)
                NetworkedCharacterState targetState = col.GetComponentInParent<NetworkedCharacterState>();
                if (!IsHostileTarget(targetState))
                {
                    continue;
                }

                // Ziel auf Kopfhoehe des Spielers (nicht Fuesse) fuer LOS-Check
                Vector3 targetPos = col.transform.position + Vector3.up * k_TargetHeadHeight;
                float dist = Vector3.Distance(eyePos, targetPos);

                // Line-of-Sight pruefen: Raycast darf nicht von Wand blockiert werden
                Vector3 dirToTarget = (targetPos - eyePos).normalized;
                bool blocked = Physics.Raycast(eyePos, dirToTarget, dist - 0.5f, losBlockMask);

                if (!blocked && dist < m_NearestPlayerDistance)
                {
                    m_NearestPlayerDistance = dist;
                    m_NearestPlayerDistanceXZ = Vector3.Distance(
                        new Vector3(eyePos.x, 0f, eyePos.z),
                        new Vector3(targetPos.x, 0f, targetPos.z));
                    m_NearestPlayerTransform = col.transform;
                    m_NearestPlayerCharacter = col.GetComponentInParent<NetworkedPlayerCharacter>();

                    // Spieler-Gedaechtnis aktualisieren bei direktem Sichtkontakt
                    m_LastKnownPlayerPosition = targetPos;
                    m_LastKnownPlayerDistance = dist;
                    m_LastKnownPlayerDistanceXZ = m_NearestPlayerDistanceXZ;
                    m_LastPlayerSeenTime = Time.time;

                    // Squad-Awareness: Sichtung an Teamkollegen weitergeben (sofern Profil das erlaubt)
                    if (m_Personality.shareSightingsToTeam)
                    {
                        PublishTeamSighting(targetPos);
                    }
                }
            }

            // Personality-ReactionTime: Bot verzoegert die Reaktion auf einen NEUEN Sichtkontakt.
            // Erkennen wir gerade einen Feind und der Timer war noch nicht aktiv → Timer starten.
            // Wichtig: NICHT auf m_HadSightingLastScan pruefen — solange reactionPending laeuft
            // wird m_NearestPlayerTransform unten genullt, wodurch m_HadSightingLastScan=false bleibt.
            // Wuerden wir das hier als Reset-Signal interpretieren, setzten wir m_FirstSpotTime
            // jeden Frame neu und der Bot waere fuer immer "in Reaktion".
            bool sightingNow = m_NearestPlayerTransform != null;
            if (sightingNow && m_FirstSpotTime <= 0f)
            {
                m_FirstSpotTime = Time.time;
            }
            if (!sightingNow)
            {
                m_FirstSpotTime = -1f;
            }

            bool reactionPending = sightingNow
                && m_FirstSpotTime > 0f
                && (Time.time - m_FirstSpotTime) < m_Personality.reactionTimeSec;

            if (reactionPending)
            {
                // Sichtung "verschlucken" — Bot hat den Feind zwar gesehen, aber sein Hirn
                // hat noch nicht reagiert. Memory NICHT schreiben (sonst bleibt der LKP
                // hartnaeckig und der Reaktions-Effekt wird wirkungslos).
                m_NearestPlayerTransform = null;
                m_NearestPlayerCharacter = null;
                m_NearestPlayerDistance = float.MaxValue;
                m_NearestPlayerDistanceXZ = float.MaxValue;
            }

            m_HadSightingLastScan = sightingNow && !reactionPending;

            // Wenn der Bot selbst nichts sieht aber ein Teamkollege etwas Frisches gemeldet hat,
            // uebernimm den geteilten LKP — der Bot laeuft dann ueber ChasePlayerAction zur Position.
            if (m_NearestPlayerTransform == null)
            {
                TryAdoptTeamSighting();
            }
        }

        /// <summary>
        /// Reagiert auf akustische Reize anderer Akteure (Schuesse, Explosionen).
        /// Wenn der Reiz von einem Feind in Hoerweite kommt, wird er als LKP behandelt
        /// und an Teamkollegen weitergegeben. Server-only Event.
        /// </summary>
        private void OnAudioStimulus(AudioStimulus.Stimulus stim)
        {
            if (m_CharacterState == null || !m_CharacterState.IsAlive)
            {
                return;
            }

            // Eigene Geraeusche ignorieren (Bot soll nicht auf seine eigenen Schuesse reagieren).
            // Teamkollegen ignorieren — sie senden ohnehin Squad-LKP-Sichtungen statt Audio.
            if (stim.EmitterTeamId != 0u && stim.EmitterTeamId == OwnTeamId)
            {
                return;
            }

            // Hoerradius: Personality bestimmt wie weit der Bot hoert,
            // gedeckelt durch den Reichweiten-Cap der Schallquelle.
            float hearRange = Mathf.Min(m_Personality.audioHearRangeMeters, stim.MaxRangeMeters);
            float sqrDist = (stim.Position - transform.position).sqrMagnitude;
            if (sqrDist > hearRange * hearRange)
            {
                return;
            }

            // Direkten Sichtkontakt nicht ueberschreiben (frischer Sicht-LKP > Audio-LKP).
            if (m_NearestPlayerTransform != null)
            {
                return;
            }

            // Eigener LKP frischer? → ignorieren.
            if (m_LastPlayerSeenTime > stim.Time)
            {
                return;
            }

            // Audio-LKP uebernehmen
            Vector3 eyePos = EyePosition;
            m_LastKnownPlayerPosition = stim.Position;
            m_LastPlayerSeenTime = stim.Time;
            m_LastKnownPlayerDistance = Vector3.Distance(eyePos, stim.Position);
            m_LastKnownPlayerDistanceXZ = Vector3.Distance(
                new Vector3(eyePos.x, 0f, eyePos.z),
                new Vector3(stim.Position.x, 0f, stim.Position.z));

            // An Teamkollegen weitergeben — wenn ein Bot einen Schuss hoert, sollen die anderen
            // im Team auch Bescheid wissen (sofern Profil teilt).
            if (m_Personality.shareSightingsToTeam)
            {
                PublishTeamSighting(stim.Position);
            }
        }

        /// <summary>
        /// Schreibt den aktuellen Sichtungspunkt in den geteilten Team-Slot, sofern Team-Spiel.
        /// In FFA (TeamId == 0) wird nichts geteilt.
        /// </summary>
        private void PublishTeamSighting(Vector3 targetPos)
        {
            uint team = OwnTeamId;
            if (team == 0u)
            {
                return;
            }

            s_TeamSharedLkp[team] = new TeamSharedSighting
            {
                Position = targetPos,
                Time = Time.time,
            };
        }

        /// <summary>
        /// Uebernimmt einen frischen geteilten LKP eines Teamkollegen wenn der eigene fehlt
        /// oder aelter ist als der geteilte. Ignoriert eigene gerade gepublishten Sichtungen
        /// indirekt ueber den MinDelta-Check (sonst wuerde der Bot seinen eigenen LKP zurueckziehen).
        /// </summary>
        private void TryAdoptTeamSighting()
        {
            uint team = OwnTeamId;
            if (team == 0u)
            {
                return;
            }

            if (!s_TeamSharedLkp.TryGetValue(team, out TeamSharedSighting shared))
            {
                return;
            }

            float age = Time.time - shared.Time;
            if (age >= k_TeamSharedLkpDuration)
            {
                return;
            }

            // Eigener LKP ist juenger als der geteilte → nichts tun
            if (m_LastPlayerSeenTime > 0f && shared.Time <= m_LastPlayerSeenTime)
            {
                return;
            }

            // Position ist quasi identisch zum eigenen letzten LKP → kein Update noetig
            if (m_LastPlayerSeenTime > 0f
                && (shared.Position - m_LastKnownPlayerPosition).sqrMagnitude
                    < k_TeamSharedLkpMinDelta * k_TeamSharedLkpMinDelta)
            {
                return;
            }

            // Geteilte Sichtung uebernehmen
            Vector3 eyePos = EyePosition;
            m_LastKnownPlayerPosition = shared.Position;
            m_LastPlayerSeenTime = shared.Time;
            m_LastKnownPlayerDistance = Vector3.Distance(eyePos, shared.Position);
            m_LastKnownPlayerDistanceXZ = Vector3.Distance(
                new Vector3(eyePos.x, 0f, eyePos.z),
                new Vector3(shared.Position.x, 0f, shared.Position.z));
        }

        /// <summary>
        /// Stealth Awareness: Erkennt Spieler in einem Grossradius OHNE Line-of-Sight-Check.
        /// Gibt dem Bot eine unsichtbare Tendenz, Checkpoints in der Naehe versteckter Spieler anzulaufen.
        /// Wird nur alle k_StealthScanInterval Sekunden ausgefuehrt (nicht performance-kritisch).
        /// Nur aktiv wenn der Bot keinen Spieler direkt sieht (kein Chase/Shoot aktiv).
        /// </summary>
        private void UpdateStealthAwareness()
        {
            m_StealthScanAccum += Time.deltaTime;
            if (m_StealthScanAccum < k_StealthScanInterval)
            {
                return;
            }

            m_StealthScanAccum = 0f;

            // Wenn der Bot den Spieler bereits direkt sieht → Stealth Awareness nicht noetig
            if (m_NearestPlayerTransform != null || PlayerMemoryActive)
            {
                return;
            }

            Vector3 botPos = transform.position;
            int hitCount = Physics.OverlapSphereNonAlloc(botPos, k_StealthAwarenessRadius, m_StealthHits, k_PlayerLayerMask);

            float nearestDist = float.MaxValue;
            Vector3 nearestPos = Vector3.zero;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = m_StealthHits[i];
                if (col == null)
                {
                    continue;
                }

                // Eigenen Collider ignorieren
                if (col.transform.root == transform.root)
                {
                    continue;
                }

                // Feindliches Ziel? Stealth-Awareness ignoriert Teammitglieder.
                NetworkedCharacterState targetState = col.GetComponentInParent<NetworkedCharacterState>();
                if (!IsHostileTarget(targetState))
                {
                    continue;
                }

                // KEIN LOS-Check — das ist der Punkt: Bot "spuert" den Spieler ohne ihn zu sehen
                float dist = Vector3.Distance(botPos, col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestPos = col.transform.position;
                    found = true;
                }
            }

            m_StealthAwarenessActive = found;
            if (found)
            {
                m_StealthAwarenessPosition = nearestPos;
            }
        }

        // ===== Initialisierung =====

        /// <summary>Konfiguriert den Controller als Seeker oder Hider.</summary>
        public void SetRole(bool isSeeker)
        {
            IsSeeker = isSeeker;
        }

        /// <summary>
        /// Leert alle Intents wenn der Bot stirbt.
        /// GOAP-Goals werden NICHT gestoppt/gecleared — der ServerAICharacter.Update()-Guard
        /// (IsAlive-Check) verhindert bereits dass Tick() aufgerufen wird.
        /// So bleibt das GOAP-Goal aktiv und laeuft beim Respawn sofort weiter.
        /// </summary>
        public void OnDeath()
        {
            // Intents sofort leeren
            m_MoveTarget = null;
            m_LookTarget = null;
            m_ShouldAttack = false;
            m_ShouldJump = false;
            m_ShouldCrouch = false;

            Debug.Log($"[AI·Bot] OnDeath: Intents geleert (GOAP-Goal bleibt aktiv).");
        }

        /// <summary>
        /// Setzt Checkpoint-/Stuck-State nach Respawn zurueck.
        /// GOAP-Goal muss nicht neu angefordert werden — es ist noch aktiv.
        /// </summary>
        public void OnRespawn()
        {
            ResetCheckpointProgress();
            Debug.Log($"[AI·Bot] OnRespawn: Checkpoint-Progress zurueckgesetzt.");
        }

        /// <summary>
        /// Setzt die Referenz zur Physik-Simulation fuer Zustandsinputs.
        /// Wird von ServerAICharacter nach SetReady aufgerufen.
        /// </summary>
        public void SetPhysicsSimulation(PlayerPhysicsSimulation simulation)
        {
            m_PhysicsSimulation = simulation;
        }

        /// <summary>
        /// Initialisiert die Sensoren am VisualRoot auf Kopfhoehe.
        /// Wird von ServerAICharacter nach Visual-Load aufgerufen.
        /// </summary>
        public void InitializeSensors(Transform visualRoot)
        {
            if (m_SensorArray == null)
            {
                m_SensorArray = GetComponent<SensorArrayGenerator>();
            }

            if (m_CharacterState == null)
            {
                m_CharacterState = GetComponent<NetworkedCharacterState>();
            }

            if (m_NetworkedAICharacter == null)
            {
                m_NetworkedAICharacter = GetComponent<NetworkedAICharacter>();
            }

            if (m_SensorArray == null)
            {
                Debug.LogWarning("[AIBotController] Kein SensorArrayGenerator auf dem GameObject gefunden.");
                return;
            }

            m_SensorArray.ConfigureFOV(120f, 40f);
            m_SensorArray.Generate(transform, k_SensorHeightOffset);
            m_SensorsReady = m_SensorArray.SensorCount > 0;

            // Persoenlichkeit zuweisen (gewichtete Zufallsauswahl aus JSON-Profilen)
            BotPersonalityLoader personalityLoader = ServiceLocator.Get<BotPersonalityLoader>();
            if (personalityLoader != null && personalityLoader.ProfileCount > 0)
            {
                m_Personality = personalityLoader.PickRandom();
            }

            // Per-Bot Aim-Noise-Seed (verschiedene Bots zittern unterschiedlich)
            m_AimNoiseSeed = Random.value * 1000f;

            // Audio-LKP: auf Schussgeraeusche etc. reagieren
            AudioStimulus.OnEmitted += OnAudioStimulus;

            Debug.Log($"[AI·Sensor] Init: {m_SensorArray.SensorCount} Sensoren | Persoenlichkeit='{m_Personality.name}' (Acc={m_Personality.accuracy:F2}, RT={m_Personality.reactionTimeSec:F2}s, Range×{m_Personality.engageRangeMul:F2})");
        }

        /// <summary>
        /// Cleanup: Audio-Subscription abmelden (verhindert Memory-Leak bei Bot-Despawn).
        /// </summary>
        private void OnDestroy()
        {
            AudioStimulus.OnEmitted -= OnAudioStimulus;
        }

        // ===== Haupttick: Baut PlayerCommand aus GOAP-Intents =====

        /// <summary>
        /// Fuehrt einen Tick aus: Liest die von GOAP-Aktionen gesetzten Intents und baut einen PlayerCommand.
        /// Sensoren werden lazy getickt (EnsureSensorsTicked), nicht hier.
        /// </summary>
        /// <param name="currentYaw">Aktueller Yaw-Winkel des Bots.</param>
        /// <returns>Der aus GOAP-Intents berechnete PlayerCommand.</returns>
        public PlayerCommand Tick(float currentYaw)
        {
            m_CurrentYaw = currentYaw;

            // Sensoren HIER ticken — zu diesem Zeitpunkt ist der eigene Collider disabled
            // (von ServerAICharacter.SimulatePhysics). Wenn wir nur auf GOAP-Sensor-Timing
            // warten, sind die Sensoren ggf. veraltet oder ticken mit aktivem Collider.
            EnsureSensorsTicked();

            // Stuck-Detection pruefen (vor BuildCommand damit Jump gesetzt werden kann)
            CheckStuck();

            // Proaktive Sensor-basierte Hindernis-Reaktion (Springen/Ducken/Ausweichen)
            ReactToSensorObstacles();

            // Stealth Awareness: Spieler im Grossradius erkennen (fuer Checkpoint-Bias)
            UpdateStealthAwareness();

            // Waffenreichweite aktualisieren (lazy bei Waffenwechsel)
            UpdateWeaponRange();

            // Waffenstatus evaluieren (Reload, Wechsel, Praeferenz)
            EvaluateWeaponState();

            PlayerCommand cmd = BuildCommand();

            // Periodisches Tick-Log (alle 300 Ticks ~ 5 Sekunden bei 60fps)
            m_TickCount++;
            if (m_TickCount == 1 || m_TickCount % 300 == 0)
            {
                string moveInfo = m_MoveTarget.HasValue ? $"{Vector3.Distance(transform.position, m_MoveTarget.Value):F1}m" : "none";
                string lookInfo = m_LookTarget.HasValue ? "yes" : "no";
                bool playerDetected = PlayerSensorDetected;
                string playerInfo = playerDetected ? $"{PlayerSensorDistance:F1}m" : "none";

                // GOAP-Agent-State diagnostik
                CrashKonijn.Agent.Runtime.AgentBehaviour agentBehaviour = GetComponent<CrashKonijn.Agent.Runtime.AgentBehaviour>();
                CrashKonijn.Goap.Runtime.GoapActionProvider actionProvider = GetComponent<CrashKonijn.Goap.Runtime.GoapActionProvider>();
                string agentState = agentBehaviour != null ? agentBehaviour.State.ToString() : "NoAgent";
                string actionName = actionProvider?.CurrentPlan?.Goal != null ? actionProvider.CurrentPlan.Goal.GetType().Name : "NoPlan";
                string currentAction = agentBehaviour?.ActionState?.Action != null ? agentBehaviour.ActionState.Action.GetType().Name : "NoAction";
                string agentType = actionProvider?.AgentType != null ? actionProvider.AgentType.Id : "NoType";

                // Erweiterte Diagnostik: NavMesh, Steering, Physik
                string navInfo = $"Path={m_PathCorners.Length}corners/idx={m_PathIndex}";
                string steerInfo = "none";
                if (m_MoveTarget.HasValue)
                {
                    Vector3 steer = GetSteeringTarget(m_MoveTarget.Value);
                    float steerDist = Vector3.Distance(transform.position, steer);
                    steerInfo = $"({steer.x:F1},{steer.y:F1},{steer.z:F1}) d={steerDist:F1}m";
                }

                string groundInfo = m_PhysicsSimulation != null ? $"Grounded={m_PhysicsSimulation.IsGrounded} Vel=({m_PhysicsSimulation.Velocity.x:F1},{m_PhysicsSimulation.Velocity.y:F1},{m_PhysicsSimulation.Velocity.z:F1})" : "NoPhys";
                string cmdInfo = $"MoveIn=({cmd.MoveInput.x:F2},{cmd.MoveInput.y:F2}) Yaw={cmd.YawAngle:F1}";
                string cpInfo = $"CP#{m_CurrentCheckpointIdx} Visits=[{(m_CheckpointVisitCounts != null ? string.Join(",", m_CheckpointVisitCounts) : "null")}] Unreachable={m_UnreachableCheckpoints.Count}";
                string sensorInfo = m_SensorArray != null ? $"Sensors={m_SensorArray.SensorCount} Ticked={m_SensorsTickedThisFrame}" : "NoSensors";
                string radiusInfo = m_NearestPlayerTransform != null ? $"RadiusPlayer={m_NearestPlayerDistance:F1}m(XZ={m_NearestPlayerDistanceXZ:F1}m) WpnRange={m_WeaponRangeMeters:F1}m({m_CachedWeaponName})" : "RadiusPlayer=none";
                string stuckInfo = $"StuckT={m_StuckTimer:F1}s P={m_StuckPhase}";
                string damageInfo = WasDamagedRecently ? $"DmgAgo={Time.time - m_LastDamageTime:F1}s" : "NoDmg";

                // Sensor-Layer-Diagnose: Zaehle Treffer pro Layer
                string sensorLayerDiag = "";
                if (m_SensorArray != null && m_SensorArray.Sensors != null)
                {
                    int hitNone = 0;
                    int hitDefault = 0;
                    int hitGround = 0;
                    int hitPlayer = 0;
                    int hitBrush = 0;
                    int hitOther = 0;
                    Sensor3D[] diagSensors = m_SensorArray.Sensors;
                    for (int i = 0; i < diagSensors.Length; i++)
                    {
                        if (diagSensors[i] == null)
                        {
                            continue;
                        }

                        switch (diagSensors[i].HitLayer)
                        {
                            case -1: hitNone++; break;
                            case 0: hitDefault++; break;
                            case 6: hitGround++; break;
                            case 7: hitPlayer++; break;
                            case 9: hitBrush++; break;
                            default: hitOther++; break;
                        }
                    }

                    sensorLayerDiag = $"Layers[None={hitNone} Def={hitDefault} Gnd={hitGround} Player={hitPlayer} Brush={hitBrush} Other={hitOther}]";
                }

                Debug.Log($"[AI·GOAP·Tick] T={m_TickCount} | Move={moveInfo} | Look={lookInfo} | Atk={m_ShouldAttack} | Player={playerInfo} | Pos=({transform.position.x:F1},{transform.position.y:F1},{transform.position.z:F1}) | Agent={agentState} | Type={agentType} | Plan={actionName} | Action={currentAction}");
                Debug.Log($"[AI·GOAP·Diag] T={m_TickCount} | {navInfo} | Steer={steerInfo} | {groundInfo} | {cmdInfo} | {cpInfo} | {sensorInfo} | {radiusInfo} | {stuckInfo} | {damageInfo} | {sensorLayerDiag}");
            }

            // Debug-Pfad im Game zeichnen
            DrawDebugPath();

            // Intents fuer naechsten Frame zuruecksetzen
            m_MoveTarget = null;
            m_LookTarget = null;
            m_ShouldAttack = false;
            m_ShouldJump = false;
            m_ShouldCrouch = false;

            return cmd;
        }

        /// <summary>
        /// Baut einen PlayerCommand aus den aktuellen GOAP-Intents.
        /// Berechnet Yaw/Pitch zum LookTarget, Bewegungsrichtung zum MoveTarget (relativ zum Yaw),
        /// und setzt Action-Buttons.
        /// </summary>
        private PlayerCommand BuildCommand()
        {
            float targetYaw = m_CurrentYaw;
            Vector2 moveInput = Vector2.zero;
            int buttons = 0;

            // Blickrichtung berechnen (LookTarget hat Vorrang)
            if (m_LookTarget.HasValue)
            {
                Vector3 eyePos = EyePosition;
                Vector3 dir = (m_LookTarget.Value - eyePos);
                Vector3 dirHorizontal = new(dir.x, 0f, dir.z);

                if (dirHorizontal.sqrMagnitude > 0.001f)
                {
                    targetYaw = Mathf.Atan2(dirHorizontal.x, dirHorizontal.z) * Mathf.Rad2Deg;
                }

                if (dir.sqrMagnitude > 0.001f)
                {
                    Vector3 dirNorm = dir.normalized;
                    m_CurrentPitch = -Mathf.Asin(dirNorm.y) * Mathf.Rad2Deg;
                    m_CurrentPitch = Mathf.Clamp(m_CurrentPitch, -89f, 89f);
                }
            }
            else if (m_MoveTarget.HasValue)
            {
                // Ohne LookTarget: Blickrichtung = naechster Wegpunkt
                Vector3 steerTarget = GetSteeringTarget(m_MoveTarget.Value);
                Vector3 toTarget = steerTarget - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.1f)
                {
                    targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                }

                // Pitch langsam zurueckfuehren
                float decay = k_PitchDecayDegPerSec * Time.deltaTime;
                if (m_CurrentPitch > decay) { m_CurrentPitch -= decay; }
                else if (m_CurrentPitch < -decay) { m_CurrentPitch += decay; }
                else { m_CurrentPitch = 0f; }
            }
            else
            {
                // Kein Ziel: Pitch langsam zurueckfuehren
                float decay = k_PitchDecayDegPerSec * Time.deltaTime;
                if (m_CurrentPitch > decay) { m_CurrentPitch -= decay; }
                else if (m_CurrentPitch < -decay) { m_CurrentPitch += decay; }
                else { m_CurrentPitch = 0f; }
            }

            // Bewegungsrichtung berechnen (NavMesh-Wegpunkt, relativ zum Yaw)
            if (m_MoveTarget.HasValue)
            {
                Vector3 steerTarget = GetSteeringTarget(m_MoveTarget.Value);
                Vector3 toTarget = steerTarget - transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > 0.5f)
                {
                    float moveWorldYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                    float relAngle = Mathf.DeltaAngle(targetYaw, moveWorldYaw) * Mathf.Deg2Rad;
                    moveInput = new Vector2(Mathf.Sin(relAngle), Mathf.Cos(relAngle));
                    moveInput = Vector2.ClampMagnitude(moveInput, 1f);
                }
            }

            // Wand-Abstossung: lateralen Offset addieren um Korridore mittig zu laufen
            if (m_MoveTarget.HasValue && !m_ShouldAttack)
            {
                float repulsion = CalculateWallRepulsion();
                if (Mathf.Abs(repulsion) > 0.01f)
                {
                    moveInput.x += repulsion;
                    moveInput = Vector2.ClampMagnitude(moveInput, 1f);
                }
            }

            // Auto-Reload: Magazin leer aber Reserve vorhanden → nachladen
            bool clipEmpty = m_CharacterState != null && m_CharacterState.CurrentClipAmmo <= 0;
            bool hasReserve = m_CharacterState != null && m_CharacterState.ReserveAmmo > 0;
            if (clipEmpty && hasReserve)
            {
                buttons |= CommandButtons.Reload;
            }

            // Action-Buttons
            // Bot darf nicht feuern wenn Clip leer (sonst spamt er Attack-Sounds und blockt
            // sich selbst im Cooldown). Server gated zusaetzlich, aber Client-Gate spart RPC-Last.
            bool currentWeaponInfinite = false;
            if (m_CharacterState != null)
            {
                WeaponDataLoader weaponLoader = ServiceLocator.Get<WeaponDataLoader>();
                WeaponDefinition currentWeaponDef = weaponLoader?.GetById(m_CharacterState.CurrentWeaponName);
                currentWeaponInfinite = currentWeaponDef?.Ammo?.Infinite ?? false;
            }
            bool canFire = !clipEmpty || currentWeaponInfinite;
            bool firePatternAllow = UpdateFirePatternGate();
            if (m_ShouldAttack && canFire && firePatternAllow)
            {
                buttons |= CommandButtons.Attack;
            }

            // SoF2-Style Bhop:
            // 1) Step-Up-Jump: Wenn der naechste Wegpunkt deutlich hoeher liegt als der Bot
            //    (ueber PmStepSize hinaus), springe rechtzeitig statt nur die StepUp-Mechanic
            //    zu nutzen — sonst verliert der Bot Schwung an Treppen/Kanten.
            // 2) Land-Chain: Sobald der Bot landet und sich noch in Bewegung Richtung Ziel
            //    befindet, sofort wieder springen → Quake/SoF2 Bhop-Beschleunigung
            //    (kein Boden-Friction-Tick).
            if (m_PhysicsSimulation != null && m_MoveTarget.HasValue && moveInput.sqrMagnitude > 0.1f)
            {
                // (1) Step-Up-Jump
                if (m_PathCorners.Length > 0 && m_PathIndex < m_PathCorners.Length)
                {
                    Vector3 nextWp = m_PathCorners[m_PathIndex];
                    float yDelta = nextWp.y - transform.position.y;
                    Vector3 flatDelta = new(nextWp.x - transform.position.x, 0f, nextWp.z - transform.position.z);
                    float xzDist = flatDelta.magnitude;

                    // Hoehensprung > StepSize und nah genug zum Anlaufen (~2m vor der Kante)
                    if (yDelta > m_PhysicsSimulation.PmStepSize && xzDist < 2f && xzDist > 0.1f)
                    {
                        m_ShouldJump = true;
                    }
                }

                // (2) Land-Chain (Bhop): Direkt nach Landung wieder springen.
                //     PlayerSensorDetected = aktiver Chase → besonders wertvoll fuer Bhop.
                //     JumpDebounce der Simulation verhindert Spam nach harter Landung.
                if (m_PhysicsSimulation.JustLanded && !m_PhysicsSimulation.IsDebounceActive)
                {
                    m_ShouldJump = true;
                }
            }

            if (m_ShouldJump)
            {
                buttons |= CommandButtons.Jump;
            }

            if (m_ShouldCrouch)
            {
                buttons |= CommandButtons.Crouch;
            }

            // Stuck Phase 2: Yaw-Override fuer Rueckwaertsbewegung
            if (!float.IsNaN(m_StuckOverrideYaw))
            {
                targetYaw = m_CurrentYaw;
                moveInput = new Vector2(0f, -1f);
            }

            PlayerCommand cmd = new()
            {
                MoveInput = moveInput,
                YawAngle = targetYaw,
                PitchAngle = m_CurrentPitch,
                MoveYawAngle = targetYaw,
                Buttons = buttons,
                DeltaTime = Time.deltaTime,
                SequenceNumber = 0,
            };

            return cmd;
        }

        /// <summary>
        /// Evaluiert den aktuellen Waffenstatus und wechselt bei Bedarf.
        /// Periodisch im Tick aufgerufen (alle k_WeaponEvalInterval Sekunden).
        /// Auto-Switch bei trockener Waffe, Praeferenz fuer Fernkampf in Kampfsituationen.
        /// </summary>
        private void EvaluateWeaponState()
        {
            if (m_CharacterState == null || m_CharacterState.WeaponCount <= 1)
            {
                return;
            }

            // Erst zur Waffe wechseln sobald die Runde tatsaechlich laeuft (nach 3,2,1 GO).
            // Waehrend des Pre-Round-Countdowns bleibt der Bot bei der Startwaffe (Messer).
            NetworkedGameState gameState = NetworkedGameState.Singleton;
            if (gameState != null && gameState.roundStartCountdown.Value > 0)
            {
                return;
            }

            if (Time.time - m_LastWeaponEvalTime < k_WeaponEvalInterval)
            {
                return;
            }

            // Waehrend eines laufenden Swaps NICHT erneut cyclen — sonst Ping-Pong:
            // CurrentWeaponName ist noch der alte Wert (z.B. "knife"), aber CycleWeapon
            // operiert auf m_PendingSwapTarget (z.B. "m4") und wuerde von dort weiter
            // auf "knife" wrappen. Erst warten bis der Swap committed ist.
            if (m_CharacterState.HasPendingWeaponSwap)
            {
                return;
            }

            m_LastWeaponEvalTime = Time.time;

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            // Inventar scannen: beste Ranged-mit-Ammo + erste Melee-Fallback
            // ermitteln. So entscheiden wir BEVOR wir swappen — kein Ping-Pong mehr.
            string currentWeaponName = m_CharacterState.CurrentWeaponName;
            string bestRanged = null;
            string fallbackMelee = null;
            int weaponCount = m_CharacterState.WeaponCount;

            for (int i = 0; i < weaponCount; i++)
            {
                string weaponId = m_CharacterState.GetWeaponAt(i);
                if (string.IsNullOrEmpty(weaponId))
                {
                    continue;
                }

                WeaponDefinition def = loader.GetById(weaponId);
                if (def == null)
                {
                    continue;
                }

                bool defInfinite = def.Ammo?.Infinite ?? false;
                bool hasUsableAmmo = defInfinite;
                if (!hasUsableAmmo && m_CharacterState.TryGetAmmoFor(weaponId, out int wClip, out int wReserve))
                {
                    hasUsableAmmo = (wClip + wReserve) > 0;
                }

                if (def.IsMelee)
                {
                    if (fallbackMelee == null && hasUsableAmmo)
                    {
                        fallbackMelee = weaponId;
                    }
                }
                else if (hasUsableAmmo && bestRanged == null)
                {
                    // Erste schussbereite Ranged-Waffe = Wunsch-Ziel.
                    // (Spaeter ggf. Score-basiert: Reichweite vs Sensor-Distanz.)
                    bestRanged = weaponId;
                }
            }

            // Entscheidungslogik:
            // 1) Aktuelle Waffe trocken & Alternative vorhanden → wechseln.
            // 2) HideAndSeek: Messer ist die einzige toetende Waffe; M4 stunnt nur (3 Schuss).
            //    → Ziel gestunnt ODER in Rush-Range → Messer (rush-kill).
            //    → Ziel weit & nicht gestunnt → Ranged (M4) zum Stunnen.
            // 3) Sonstige Gametypes: Melee in aktivem Kampf → auf Ranged wechseln.
            // 4) Sonst: bleiben (kein Swap, kein Ping-Pong).
            WeaponDefinition currentDef = loader.GetById(currentWeaponName);
            bool currentIsMelee = currentDef?.IsMelee ?? false;
            bool currentIsInfinite = currentDef?.Ammo?.Infinite ?? false;
            bool currentIsDry = !currentIsInfinite
                && m_CharacterState.CurrentClipAmmo <= 0
                && m_CharacterState.ReserveAmmo <= 0;

            // HideAndSeek-Kontext: Gametype + Stun-Status + Rush-Range bestimmen.
            bool isHideAndSeek = false;
            NetworkedGameState gameStateForGametype = NetworkedGameState.Singleton;
            if (gameStateForGametype != null)
            {
                isHideAndSeek = gameStateForGametype.activeGametypeId.Value == "hideandseek";
            }
            bool targetStunned = m_NearestPlayerCharacter != null && m_NearestPlayerCharacter.IsStunned;
            bool targetInRushRange = m_NearestPlayerTransform != null
                && m_NearestPlayerDistance <= k_HideAndSeekRushRange;

            string desired = null;

            if (currentIsDry)
            {
                desired = bestRanged ?? fallbackMelee;
                if (!string.IsNullOrEmpty(desired) && desired != currentWeaponName)
                {
                    Debug.Log($"[AI·Weapon] {m_CharacterState.CharacterName}: '{currentWeaponName}' trocken → '{desired}'.");
                }
            }
            else if (isHideAndSeek && PlayerSensorDetected
                     && (targetStunned || targetInRushRange)
                     && fallbackMelee != null && fallbackMelee != currentWeaponName)
            {
                // H&S: Ziel betaeubt oder in Rush-Range → Messer ziehen und toeten.
                desired = fallbackMelee;
                string reason = targetStunned ? "Ziel gestunnt" : $"Rush-Range ({m_NearestPlayerDistance:F1}m)";
                Debug.Log($"[AI·Weapon|H&S] {m_CharacterState.CharacterName}: {reason} → '{fallbackMelee}'.");
            }
            else if (isHideAndSeek && PlayerSensorDetected
                     && !targetStunned && !targetInRushRange
                     && currentIsMelee && bestRanged != null && bestRanged != currentWeaponName)
            {
                // H&S: Ziel weit & wach → erst mit M4 stunnen.
                desired = bestRanged;
                Debug.Log($"[AI·Weapon|H&S] {m_CharacterState.CharacterName}: Ziel weit ({m_NearestPlayerDistance:F1}m) & wach → Stun mit '{bestRanged}'.");
            }
            else if (!isHideAndSeek && currentIsMelee && PlayerSensorDetected
                     && bestRanged != null && bestRanged != currentWeaponName)
            {
                desired = bestRanged;
                Debug.Log($"[AI·Weapon] {m_CharacterState.CharacterName}: Kampf, Melee gehalten → '{bestRanged}'.");
            }

            if (!string.IsNullOrEmpty(desired) && desired != currentWeaponName)
            {
                m_CharacterState.ServerSelectWeapon(desired);
            }
        }

        /// <summary>
        /// Aktualisiert die gecachte Waffenreichweite aus dem WeaponDataLoader.
        /// Wird nur bei Waffenwechsel aufgerufen (lazy).
        /// </summary>
        private void UpdateWeaponRange()
        {
            string currentWeapon = m_CharacterState != null ? m_CharacterState.CurrentWeaponName : "knife";
            if (currentWeapon == m_CachedWeaponName)
            {
                return;
            }

            m_CachedWeaponName = currentWeapon;
            m_WeaponRangeMeters = 1.5f;

            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader != null)
            {
                WeaponDefinition weaponDef = loader.GetById(currentWeapon);
                if (weaponDef != null && weaponDef.Attack != null)
                {
                    m_WeaponRangeMeters = weaponDef.Attack.Range * k_Sof2UnitScale;
                }
            }
        }
    }
}
