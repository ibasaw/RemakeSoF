#if EANN_ENABLED
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Orchestriert den evolutionaeren Lernprozess fuer AI-Bots.
    /// Verwaltet je eine Population von Agents fuer Seeker und Hider,
    /// weist sie den aktiven Bots zu, trackt Checkpoint-basierte Fitness kontinuierlich,
    /// und triggert Generationswechsel ueber den GeneticAlgorithm.
    ///
    /// Checkpoint-basierte Evolution: Jeder Agent bekommt eine Evaluationsperiode bis zum
    /// naechsten Checkpoint (oder Timeout). Danach wird der naechste Genotype aus der Population
    /// zugewiesen. Wenn alle Genotypes evaluiert sind, wird der GA-Generationswechsel ausgeloest.
    /// Dies ermoeglicht schnelles Lernen innerhalb einer einzigen Runde.
    ///
    /// Topology wird automatisch berechnet: Input (SensorCount + StateInputs) und Output (Rolle)
    /// werden aus dem ersten registrierten Bot gelesen; nur Hidden-Layers sind tweakbar.
    /// Checkpoints kommen zur Laufzeit aus dem geladenen Map-Prefab.
    /// Laeuft nur auf dem Server.
    /// </summary>
    public class AIEvolutionManager : MonoBehaviour
    {
        [Header("Hidden Layers (Seeker)")]
        [Tooltip("Nur die Hidden-Layer-Groessen. Input/Output werden automatisch berechnet.")]
        [SerializeField]
        private uint[] m_SeekerHiddenLayers = { 32, 20 };

        [Header("Hidden Layers (Hider)")]
        [Tooltip("Nur die Hidden-Layer-Groessen. Input/Output werden automatisch berechnet.")]
        [SerializeField]
        private uint[] m_HiderHiddenLayers = { 28, 18 };

        [Header("Checkpoint Settings")]
        [Tooltip("Einfangradius fuer Checkpoint-Erkennung in Unity-Einheiten.")]
        [SerializeField]
        private float m_CheckpointCaptureRadius = 5f;

        [Header("Population Settings")]
        [Tooltip("Mindest-Populationsgroesse pro Rolle fuer genetische Diversitaet. Bei weniger Bots wird Round-Robin ueber mehrere Runden evaluiert.")]
        [SerializeField]
        private uint m_MinPopulationSize = 50;

        /// <summary>GA-Populationsgroesse fuer Seeker (= Max(botCount, minPop)).</summary>
        private uint m_SeekerPopulationSize;

        /// <summary>GA-Populationsgroesse fuer Hider (= Max(botCount, minPop)).</summary>
        private uint m_HiderPopulationSize;

        /// <summary>Tatsaechliche Anzahl Seeker-Bots (aus Config).</summary>
        private uint m_SeekerBotCount;

        /// <summary>Tatsaechliche Anzahl Hider-Bots (aus Config).</summary>
        private uint m_HiderBotCount;

        /// <summary>Naechster zu evaluierender Genotype-Index fuer Seeker (fortlaufend).</summary>
        private int m_SeekerNextGenotypeIndex;

        /// <summary>Naechster zu evaluierender Genotype-Index fuer Hider (fortlaufend).</summary>
        private int m_HiderNextGenotypeIndex;

        [Header("Agent Timeout")]
        [Tooltip("Maximale Sekunden pro Agent bevor er bei fehlendem Checkpoint-Fortschritt getauscht wird.")]
        [SerializeField]
        private float m_AgentTimeoutSeconds = 15f;

        /// <summary>Vollstaendige Seeker-Topologie (lazy berechnet beim ersten RegisterBot).</summary>
        private uint[] m_SeekerTopology;

        /// <summary>Vollstaendige Hider-Topologie (lazy berechnet beim ersten RegisterBot).</summary>
        private uint[] m_HiderTopology;

        /// <summary>GA-Instanz fuer Seeker-Population (lazy erstellt).</summary>
        private GeneticAlgorithm m_SeekerGA;

        /// <summary>GA-Instanz fuer Hider-Population (lazy erstellt).</summary>
        private GeneticAlgorithm m_HiderGA;

        /// <summary>Ob Initialize() aufgerufen wurde.</summary>
        private bool m_Initialized;

        /// <summary>Aktive Seeker-Agents (Index - Agent).</summary>
        private readonly List<Agent> m_ActiveSeekerAgents = new();

        /// <summary>Aktive Hider-Agents (Index - Agent).</summary>
        private readonly List<Agent> m_ActiveHiderAgents = new();

        /// <summary>Fitness-Evaluatoren pro Bot-Controller.</summary>
        private readonly Dictionary<AIBotController, AIFitnessEvaluator> m_Evaluators = new();

        /// <summary>Alle registrierten Bot-Controller.</summary>
        private readonly List<AIBotController> m_RegisteredControllers = new();

        /// <summary>Zeitstempel wann der aktuelle Agent einem Controller zugewiesen wurde (fuer Timeout).</summary>
        private readonly Dictionary<AIBotController, float> m_AgentAssignTimes = new();

        /// <summary>Zeitstempel fuer naechsten periodischen Fortschritts-Log.</summary>
        private float m_NextProgressLogTime;

        /// <summary>Cache fuer Seeker-Genotypes der aktuellen Population.</summary>
        private readonly List<Genotype> m_CachedSeekerGenotypes = new();

        /// <summary>Cache fuer Hider-Genotypes der aktuellen Population.</summary>
        private readonly List<Genotype> m_CachedHiderGenotypes = new();

        /// <summary>Checkpoint-Positionen aus der geladenen Map. Null bis SetCheckpoints() aufgerufen wird.</summary>
        private Vector3[] m_Checkpoints;

        /// <summary>Aktuelle Seeker-Generationszaehler.</summary>
        public uint SeekerGeneration => m_SeekerGA != null ? m_SeekerGA.GenerationCount : 0;

        /// <summary>Aktuelle Hider-Generationszaehler.</summary>
        public uint HiderGeneration => m_HiderGA != null ? m_HiderGA.GenerationCount : 0;

        /// <summary>Event: Wird nach jedem Generationswechsel gefeuert.</summary>
        public event Action<uint, uint> OnGenerationChanged;

        /// <summary>Ob die Evolution bereits gestartet wurde (mindestens ein GA laeuft).</summary>
        public bool Running => (m_SeekerGA != null && m_SeekerGA.Running)
                            || (m_HiderGA != null && m_HiderGA.Running);

        /// <summary>
        /// Setzt die Checkpoint-Positionen aus dem geladenen Map-Prefab.
        /// Wird nach dem Map-Laden aufgerufen (z.B. aus RoundFlowStateMachine).
        /// </summary>
        /// <param name="checkpoints">Array von Checkpoint-Positionen in Reihenfolge.</param>
        public void SetCheckpoints(Vector3[] checkpoints)
        {
            m_Checkpoints = checkpoints;
            Debug.Log($"[AI·Evo] Checkpoints gesetzt: {(checkpoints != null ? checkpoints.Length : 0)}");
        }

        /// <summary>
        /// Initialisiert den Manager mit den Bot-Anzahlen aus der ServerConfiguration.
        /// Die GA-Populationsgroesse wird auf mindestens MinPopulationSize hochgesetzt
        /// fuer genetische Diversitaet. Bei weniger Bots als PopSize werden die Genotypes
        /// ueber mehrere Runden per Round-Robin evaluiert.
        /// Die NN-Topologie und GAs werden erst beim ersten RegisterBot lazy erstellt.
        /// </summary>
        /// <param name="seekerBotCount">Anzahl Seeker-Bots (aus hideandseek_seekercount).</param>
        /// <param name="hiderBotCount">Anzahl Hider-Bots (aus sv_botcount - seekercount).</param>
        public void Initialize(uint seekerBotCount, uint hiderBotCount)
        {
            m_SeekerBotCount = seekerBotCount;
            m_HiderBotCount = hiderBotCount;

            // Population = Max(botCount, minPop) fuer genetische Diversitaet
            m_SeekerPopulationSize = seekerBotCount > 0
                ? Math.Max(seekerBotCount, m_MinPopulationSize)
                : 0;
            m_HiderPopulationSize = hiderBotCount > 0
                ? Math.Max(hiderBotCount, m_MinPopulationSize)
                : 0;

            m_SeekerNextGenotypeIndex = 0;
            m_HiderNextGenotypeIndex = 0;
            m_Initialized = true;

            Debug.Log($"[AI·Evo] Init: Seeker={seekerBotCount}→Pop={m_SeekerPopulationSize} | " +
                      $"Hider={hiderBotCount}→Pop={m_HiderPopulationSize} | MinPop={m_MinPopulationSize}");
        }

        /// <summary>
        /// Baut den GA fuer eine Rolle, wenn er noch nicht existiert.
        /// Liest die SensorCount vom SensorArrayGenerator des uebergebenen Bots.
        /// </summary>
        private void EnsureGA(AIBotController controller, bool isSeeker)
        {
            GeneticAlgorithm existingGA = isSeeker ? m_SeekerGA : m_HiderGA;
            if (existingGA != null)
            {
                return;
            }

            // SensorCount vom Bot-Prefab lesen
            SensorArrayGenerator sensorArray = controller.GetComponent<SensorArrayGenerator>();
            uint sensorCount = sensorArray != null ? (uint)sensorArray.TotalSensorCount : 7;
            uint inputCount = sensorCount + (uint)AIBotController.StateInputCount;

            uint populationSize = isSeeker ? m_SeekerPopulationSize : m_HiderPopulationSize;
            if (populationSize == 0)
            {
                Debug.LogWarning($"[AI·Evo] PopSize=0 fuer {(isSeeker ? "Seeker" : "Hider")}. Kein GA.");
                return;
            }

            uint[] hiddenLayers = isSeeker ? m_SeekerHiddenLayers : m_HiderHiddenLayers;
            uint outputCount = isSeeker ? (uint)AIBotController.SeekerOutputCount : (uint)AIBotController.HiderOutputCount;
            uint[] topology = BuildTopology(inputCount, hiddenLayers, outputCount);

            NeuralNetwork probe = new(topology);
            GeneticAlgorithm ga = new((uint)probe.WeightCount, populationSize);
            ga.Selection = GeneticAlgorithm.RemainderStochasticSampling;
            ga.Recombination = GeneticAlgorithm.RandomRecombination;
            ga.Mutation = GeneticAlgorithm.MutateAllButBestTwo;

            if (isSeeker)
            {
                ga.Evaluation = EvaluateSeekerPopulation;
                m_SeekerGA = ga;
                m_SeekerTopology = topology;
            }
            else
            {
                ga.Evaluation = EvaluateHiderPopulation;
                m_HiderGA = ga;
                m_HiderTopology = topology;
            }

            // Gespeicherte Population laden (falls vorhanden) — Training wird fortgesetzt
            bool loaded = AIPopulationSerializer.TryLoad(isSeeker, out List<Genotype> savedGenotypes, out uint savedGeneration);
            if (loaded)
            {
                ga.LoadPopulation(savedGenotypes, savedGeneration);
                // InitialisePopulation mit No-Op ueberschreiben damit Start() die geladenen Weights nicht randomisiert
                ga.InitialisePopulation = _ => { };
                Debug.Log($"[AI·Evo] {(isSeeker ? "Seeker" : "Hider")}: Population aus Datei geladen (Gen={savedGeneration}).");
            }

            // GA sofort starten damit Genotypes gecacht werden (benoetigt fuer CreateNextAgent)
            ga.Start();

            // Nach Start() die Standard-Initialisierung wiederherstellen fuer zukuenftige Neustarts
            if (loaded)
            {
                ga.InitialisePopulation = GeneticAlgorithm.DefaultPopulationInitialisation;
            }

            Debug.Log($"[AI·Evo] GA erstellt: {(isSeeker ? "Seeker" : "Hider")} | " +
                      $"Topology=[{string.Join(",", topology)}] | Weights={probe.WeightCount} | " +
                      $"Pop={populationSize} | Timeout={m_AgentTimeoutSeconds}s" +
                      (loaded ? $" | Fortgesetzt ab Gen={savedGeneration}" : " | Neue Population"));
        }

        /// <summary>
        /// Startet die Evolution fuer alle erstellten GAs.
        /// Ruft GA.Start() auf, was InitialisePopulation + Evaluation triggert.
        /// </summary>
        public void StartEvolution()
        {
            if (m_SeekerGA != null && !m_SeekerGA.Running)
            {
                m_SeekerGA.Start();
            }

            if (m_HiderGA != null && !m_HiderGA.Running)
            {
                m_HiderGA.Start();
            }

            Debug.Log($"[AI·Evo] Evolution gestartet | Seeker={m_SeekerGA != null} Hider={m_HiderGA != null}");
        }

        /// <summary>
        /// Registriert einen AIBotController und weist ihm den naechsten freien Agent zu.
        /// Beim ersten Aufruf pro Rolle wird der GA lazy erstellt (SensorCount wird vom Bot gelesen).
        /// Der Genotype-Index wird aus dem aktuellen Batch-Index berechnet (Round-Robin).
        /// </summary>
        /// <param name="controller">Der Controller des gespawnten Bots.</param>
        /// <param name="isSeeker">True = Seeker, False = Hider.</param>
        public void RegisterBot(AIBotController controller, bool isSeeker)
        {
            if (controller == null || !m_Initialized)
            {
                return;
            }

            // Lazy: GA erstellen beim ersten Bot dieser Rolle
            EnsureGA(controller, isSeeker);

            uint botCount = isSeeker ? m_SeekerBotCount : m_HiderBotCount;
            List<Agent> agentPool = isSeeker ? m_ActiveSeekerAgents : m_ActiveHiderAgents;

            if (agentPool.Count >= (int)botCount)
            {
                Debug.LogWarning($"[AI·Evo] Alle {(isSeeker ? "Seeker" : "Hider")}-Agents vergeben (Pool voll).");
                return;
            }

            Agent agent = CreateNextAgent(isSeeker);
            if (agent == null)
            {
                return;
            }

            agentPool.Add(agent);
            agent.Reset();

            controller.AssignAgent(agent, isSeeker);

            AIFitnessEvaluator evaluator = new();
            m_Evaluators[controller] = evaluator;
            m_RegisteredControllers.Add(controller);
            m_AgentAssignTimes[controller] = Time.time;

            // Evaluator-Referenz im Controller setzen (fuer Checkpoint-Richtungs-Inputs)
            controller.SetFitnessEvaluator(evaluator);

            int genotypeIdx = isSeeker ? m_SeekerNextGenotypeIndex : m_HiderNextGenotypeIndex;
            uint popSize = isSeeker ? m_SeekerPopulationSize : m_HiderPopulationSize;
            Debug.Log($"[AI·Evo] Bot registriert: {(isSeeker ? "Seeker" : "Hider")} | Pool={agentPool.Count}/{botCount} | Genotype={genotypeIdx}/{popSize}");
        }

        /// <summary>
        /// Deregistriert einen Bot-Controller (z.B. beim Despawn).
        /// </summary>
        public void UnregisterBot(AIBotController controller)
        {
            if (controller == null)
            {
                return;
            }

            m_Evaluators.Remove(controller);
            m_RegisteredControllers.Remove(controller);
        }

        /// <summary>
        /// Initialisiert den Fitness-Evaluator fuer einen bestimmten Bot mit Checkpoints.
        /// Wird nach Spawn/Respawn aufgerufen.
        /// </summary>
        public void ResetEvaluator(AIBotController controller, Vector3 startPosition, bool isSeeker)
        {
            if (m_Evaluators.TryGetValue(controller, out AIFitnessEvaluator evaluator))
            {
                evaluator.Reset(startPosition, isSeeker, m_Checkpoints, m_CheckpointCaptureRadius);
            }
        }

        /// <summary>
        /// Aktualisiert die Fitness-Evaluatoren aller registrierten Bots kontinuierlich.
        /// Schreibt den aktuellen Evaluation-Wert direkt in den Genotype (wie im Auto-Beispiel).
        /// Prueft nach jedem Tick ob ein Checkpoint erreicht oder das Timeout abgelaufen ist.
        /// Bei Checkpoint/Timeout: Agent tauschen (naechster Genotype aus Population).
        /// Wird pro Server-Tick von aussen aufgerufen.
        /// </summary>
        public void TickEvaluators()
        {
            for (int i = 0; i < m_RegisteredControllers.Count; i++)
            {
                AIBotController controller = m_RegisteredControllers[i];
                if (controller == null || controller.CurrentAgent == null || !controller.CurrentAgent.IsAlive)
                {
                    continue;
                }

                if (!m_Evaluators.TryGetValue(controller, out AIFitnessEvaluator evaluator))
                {
                    continue;
                }

                float evaluation = evaluator.Update(controller.transform.position, Time.deltaTime,
                    controller.IsJumping, controller.IsCrouching, controller.NormalizedSpeed,
                    controller.NormalizedForwardSpeed, controller.CloseWallHitCount,
                    controller.TotalSensorCount, controller.PlayerSensorDetected,
                    controller.PlayerSensorDistance, controller.IsAttacking);
                controller.CurrentAgent.Genotype.Evaluation = evaluation;

                // Timeout-Reset bei Checkpoint (wie Auto-Beispiel):
                // Gute Bots die Checkpoints erreichen bekommen mehr Zeit.
                // Festsitzende Bots werden nach m_AgentTimeoutSeconds getauscht.
                if (evaluator.CheckpointJustReached
                    && m_AgentAssignTimes.ContainsKey(controller))
                {
                    m_AgentAssignTimes[controller] = Time.time;
                }

                bool timeout = m_AgentAssignTimes.TryGetValue(controller, out float assignTime)
                    && (Time.time - assignTime) >= m_AgentTimeoutSeconds;

                if (timeout)
                {
                    SwapAgent(controller, controller.IsSeeker);
                }
            }

            // Periodischer Fortschritts-Log (alle 30 Sekunden)
            if (Time.time >= m_NextProgressLogTime)
            {
                m_NextProgressLogTime = Time.time + 30f;
                int seekerIdx = m_SeekerNextGenotypeIndex;
                uint seekerPop = m_SeekerPopulationSize;
                uint seekerGen = SeekerGeneration;
                Debug.Log($"[AI·Progress] Seeker: Gen={seekerGen} | Genotype={seekerIdx}/{seekerPop} | Bots={m_RegisteredControllers.Count}");
            }
        }

        /// <summary>
        /// Registriert einen Kill fuer einen Seeker-Bot.
        /// </summary>
        public void RegisterKill(AIBotController controller)
        {
            if (m_Evaluators.TryGetValue(controller, out AIFitnessEvaluator evaluator))
            {
                evaluator.RegisterKill();
            }
        }

        /// <summary>
        /// Tauscht den aktuellen Agent eines Controllers gegen den naechsten Genotyp aus der Population.
        /// Wird aufgerufen wenn das Timeout abgelaufen ist.
        /// Wenn alle Genotypes evaluiert → Generationswechsel (Selection + Crossover + Mutation).
        /// </summary>
        private void SwapAgent(AIBotController controller, bool isSeeker)
        {
            // Alten Agent beenden — Fitness loggen
            if (controller.CurrentAgent != null)
            {
                float fitness = controller.CurrentAgent.Genotype.Evaluation;
                int genotypeIdx = isSeeker ? m_SeekerNextGenotypeIndex : m_HiderNextGenotypeIndex;
                uint popSize = isSeeker ? m_SeekerPopulationSize : m_HiderPopulationSize;
                int cps = 0;
                string breakdown = "n/a";
                if (m_Evaluators.TryGetValue(controller, out AIFitnessEvaluator swapEval))
                {
                    cps = swapEval.CapturedCheckpointCount;
                    breakdown = swapEval.GetBreakdown(controller.transform.position);
                }
                Debug.Log($"[AI·Swap] #{genotypeIdx}/{popSize} | Eval={fitness:F3} | CPs={cps} | {breakdown}");
                controller.CurrentAgent.Kill();
            }
            controller.ClearAgent();

            // Genotype-Index inkrementieren
            int nextIndex;
            uint nextPopSize;
            if (isSeeker)
            {
                m_SeekerNextGenotypeIndex++;
                nextIndex = m_SeekerNextGenotypeIndex;
                nextPopSize = m_SeekerPopulationSize;
            }
            else
            {
                m_HiderNextGenotypeIndex++;
                nextIndex = m_HiderNextGenotypeIndex;
                nextPopSize = m_HiderPopulationSize;
            }

            // Pruefen ob alle Genotypes evaluiert → Generationswechsel
            if (nextIndex >= (int)nextPopSize)
            {
                GeneticAlgorithm ga = isSeeker ? m_SeekerGA : m_HiderGA;
                if (ga != null)
                {
                    // Vor EvaluationFinished: Populationsdaten fuer Report sammeln
                    string role = isSeeker ? "Seeker" : "Hider";
                    uint oldGen = isSeeker ? SeekerGeneration : HiderGeneration;
                    List<Genotype> preGenCache = isSeeker ? m_CachedSeekerGenotypes : m_CachedHiderGenotypes;
                    LogGenerationReport(role, oldGen, preGenCache);

                    ga.EvaluationFinished();

                    // Auto-Save: Population nach jeder Generation persistieren
                    AIPopulationSerializer.Save(ga, isSeeker);

                    uint newGen = isSeeker ? SeekerGeneration : HiderGeneration;
                    Debug.Log($"[AI·Generation] {role}: Gen {newGen} gestartet (Population gespeichert).");
                    OnGenerationChanged?.Invoke(SeekerGeneration, HiderGeneration);
                }

                if (isSeeker)
                {
                    m_SeekerNextGenotypeIndex = 0;
                }
                else
                {
                    m_HiderNextGenotypeIndex = 0;
                }
            }

            // Neuen Agent aus active pool entfernen und neuen erstellen
            List<Agent> agentPool = isSeeker ? m_ActiveSeekerAgents : m_ActiveHiderAgents;
            agentPool.Clear();

            Agent newAgent = CreateNextAgent(isSeeker);
            if (newAgent == null)
            {
                return;
            }

            agentPool.Add(newAgent);
            newAgent.Reset();

            controller.AssignAgent(newAgent, isSeeker);
            m_AgentAssignTimes[controller] = Time.time;

            // Evaluator zuruecksetzen fuer neues Segment (ab aktueller Position)
            if (m_Evaluators.TryGetValue(controller, out AIFitnessEvaluator evaluator))
            {
                evaluator.Reset(controller.transform.position, isSeeker, m_Checkpoints, m_CheckpointCaptureRadius);
                // Evaluator-Referenz im Controller neu setzen (ClearAgent() hat sie geloescht)
                controller.SetFitnessEvaluator(evaluator);
            }

            int swapIdx = isSeeker ? m_SeekerNextGenotypeIndex : m_HiderNextGenotypeIndex;
            uint swapPopSize = isSeeker ? m_SeekerPopulationSize : m_HiderPopulationSize;
            Debug.Log($"[AI·Swap] Neuer Genotype zugewiesen: #{swapIdx}/{swapPopSize} Gen={( isSeeker ? SeekerGeneration : HiderGeneration)}");
        }

        /// <summary>
        /// Beendet die aktuelle Runde.
        /// Cleanup: Agents von Controllern trennen, Listen zuruecksetzen.
        /// Evolution passiert bereits kontinuierlich per Checkpoint/Timeout (siehe SwapAgent).
        /// </summary>
        public void EndRound()
        {
            // Agents von Controllern trennen (Bot geht in Idle bis naechste Runde)
            for (int i = 0; i < m_RegisteredControllers.Count; i++)
            {
                AIBotController controller = m_RegisteredControllers[i];
                if (controller != null)
                {
                    if (controller.CurrentAgent != null)
                    {
                        controller.CurrentAgent.Kill();
                    }
                    controller.ClearAgent();
                }
            }

            // Active-Agent-Listen zuruecksetzen fuer neue Zuweisung
            m_ActiveSeekerAgents.Clear();
            m_ActiveHiderAgents.Clear();
            m_Evaluators.Clear();
            m_AgentAssignTimes.Clear();
            m_RegisteredControllers.Clear();

            Debug.Log($"[AI·Evo] Runde beendet | " +
                      $"Seeker: Gen={SeekerGeneration} Idx={m_SeekerNextGenotypeIndex}/{m_SeekerPopulationSize} | " +
                      $"Hider: Gen={HiderGeneration} Idx={m_HiderNextGenotypeIndex}/{m_HiderPopulationSize}");
        }

        /// <summary>
        /// Loggt einen detaillierten Report am Ende einer Generation:
        /// Top-5 Genotypes, Worst-3, Statistiken (Avg/Median/StdDev), Survivors-Count.
        /// Die Population muss noch unsortiert sein (sortierung passiert in EvaluationFinished).
        /// </summary>
        private void LogGenerationReport(string role, uint generation, List<Genotype> population)
        {
            if (population == null || population.Count == 0)
            {
                return;
            }

            int count = population.Count;

            // Evaluations sammeln und sortieren (lokal, ohne GA-Population zu aendern)
            float[] evals = new float[count];
            for (int i = 0; i < count; i++)
            {
                evals[i] = population[i].Evaluation;
            }
            Array.Sort(evals);
            Array.Reverse(evals); // Absteigend: best first

            // Statistiken berechnen
            float sum = 0f;
            float best = evals[0];
            float worst = evals[count - 1];
            for (int i = 0; i < count; i++)
            {
                sum += evals[i];
            }
            float avg = sum / count;
            float median = count % 2 == 0
                ? (evals[count / 2 - 1] + evals[count / 2]) / 2f
                : evals[count / 2];

            // Standardabweichung
            float varianceSum = 0f;
            for (int i = 0; i < count; i++)
            {
                float diff = evals[i] - avg;
                varianceSum += diff * diff;
            }
            float stdDev = Mathf.Sqrt(varianceSum / count);

            // Zählen wieviele Evaluation > 0 haben (waren aktiv)
            int activeCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (evals[i] > 0f)
                {
                    activeCount++;
                }
            }

            // Fitness berechnen (gleiche Logik wie DefaultFitnessCalculation)
            // Survivors = Fitness >= 1.0 (werden von RemainderStochasticSampling selektiert)
            int survivorCount = 0;
            if (avg > 0f)
            {
                for (int i = 0; i < count; i++)
                {
                    float fitness = evals[i] / avg;
                    if (fitness >= 1f)
                    {
                        survivorCount++;
                    }
                }
            }

            // Report bauen
            StringBuilder sb = new();
            sb.AppendLine($"╔══════════════════════════════════════════════════════════");
            sb.AppendLine($"║ [{role}] Generation {generation} Abgeschlossen");
            sb.AppendLine($"╠══════════════════════════════════════════════════════════");
            sb.AppendLine($"║ Population: {count} | Aktiv (Eval>0): {activeCount} | Survivors (Fitness≥1): {survivorCount}");
            sb.AppendLine($"║ Best: {best:F3} | Worst: {worst:F3} | Avg: {avg:F3} | Median: {median:F3} | StdDev: {stdDev:F3}");
            sb.AppendLine($"╠── Top 5 ──────────────────────────────────────────────");

            int topN = Mathf.Min(5, count);
            for (int i = 0; i < topN; i++)
            {
                float fitness = avg > 0f ? evals[i] / avg : 0f;
                sb.AppendLine($"║  #{i + 1}: Eval={evals[i]:F3} Fitness={fitness:F2}x");
            }

            if (count > 5)
            {
                sb.AppendLine($"╠── Worst 3 ─────────────────────────────────────────────");
                int worstStart = Mathf.Max(count - 3, topN);
                for (int i = count - 1; i >= worstStart; i--)
                {
                    float fitness = avg > 0f ? evals[i] / avg : 0f;
                    sb.AppendLine($"║  #{i + 1}: Eval={evals[i]:F3} Fitness={fitness:F2}x");
                }
            }

            sb.AppendLine($"╚══════════════════════════════════════════════════════════");

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Baut die vollstaendige NN-Topologie aus Input + Hidden-Layers + Output.
        /// </summary>
        private static uint[] BuildTopology(uint inputCount, uint[] hiddenLayers, uint outputCount)
        {
            uint[] topology = new uint[hiddenLayers.Length + 2];
            topology[0] = inputCount;
            for (int i = 0; i < hiddenLayers.Length; i++)
            {
                topology[i + 1] = hiddenLayers[i];
            }
            topology[topology.Length - 1] = outputCount;
            return topology;
        }

        /// <summary>
        /// Erstellt den naechsten Agent fuer den aktuellen Genotype-Index.
        /// Der Index wird fortlaufend durch die Population rotiert.
        /// </summary>
        private Agent CreateNextAgent(bool isSeeker)
        {
            uint[] topology = isSeeker ? m_SeekerTopology : m_HiderTopology;
            GeneticAlgorithm ga = isSeeker ? m_SeekerGA : m_HiderGA;

            if (ga == null || topology == null)
            {
                Debug.LogWarning($"[AI·Evo] GA nicht erstellt fuer {(isSeeker ? "Seeker" : "Hider")}.");
                return null;
            }

            int genotypeIndex = isSeeker ? m_SeekerNextGenotypeIndex : m_HiderNextGenotypeIndex;
            uint popSize = isSeeker ? m_SeekerPopulationSize : m_HiderPopulationSize;

            // Schutz: Index wrappen falls noetig
            if (genotypeIndex >= (int)popSize)
            {
                genotypeIndex = genotypeIndex % (int)popSize;
            }

            Genotype genotype = GetGenotypeFromGA(genotypeIndex, isSeeker);

            Agent agent = new(genotype, MathHelper.SoftSignFunction, topology);
            return agent;
        }

        /// <summary>
        /// Holt einen Genotype aus der gecachten Population des GA per Index.
        /// </summary>
        private Genotype GetGenotypeFromGA(int index, bool isSeeker)
        {
            List<Genotype> cache = isSeeker ? m_CachedSeekerGenotypes : m_CachedHiderGenotypes;

            if (index < cache.Count)
            {
                return cache[index];
            }

            Debug.LogWarning($"[AI·Evo] Genotype#{index} nicht verfuegbar — Fallback (leere Weights).");
            uint[] topology = isSeeker ? m_SeekerTopology : m_HiderTopology;
            NeuralNetwork probe = new(topology);
            return new Genotype(new float[probe.WeightCount]);
        }

        /// <summary>
        /// GA-Evaluation-Callback fuer Seeker: cached die Population und wartet auf Runden-Ende.
        /// </summary>
        private void EvaluateSeekerPopulation(IEnumerable<Genotype> population)
        {
            m_CachedSeekerGenotypes.Clear();
            foreach (Genotype g in population)
            {
                m_CachedSeekerGenotypes.Add(g);
            }
        }

        /// <summary>
        /// GA-Evaluation-Callback fuer Hider: cached die Population und wartet auf Runden-Ende.
        /// </summary>
        private void EvaluateHiderPopulation(IEnumerable<Genotype> population)
        {
            m_CachedHiderGenotypes.Clear();
            foreach (Genotype g in population)
            {
                m_CachedHiderGenotypes.Add(g);
            }
        }
    }
}
#endif
