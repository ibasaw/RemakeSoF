using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Implements a modified genetic algorithm with pluggable operators for initialisation,
    /// evaluation, fitness calculation, selection, recombination, and mutation.
    /// </summary>
    public class GeneticAlgorithm
    {
        #region Default Parameters
        /// <summary>
        /// Default min value of initial population parameters.
        /// </summary>
        public const float DefInitParamMin = -1.0f;

        /// <summary>
        /// Default max value of initial population parameters.
        /// </summary>
        public const float DefInitParamMax = 1.0f;

        /// <summary>
        /// Default probability of a parameter being swapped during crossover.
        /// </summary>
        public const float DefCrossSwapProb = 0.6f;

        /// <summary>
        /// Default probability of a parameter being mutated.
        /// </summary>
        public const float DefMutationProb = 0.3f;

        /// <summary>
        /// Default amount by which parameters may be mutated.
        /// </summary>
        public const float DefMutationAmount = 2.0f;

        /// <summary>
        /// Default percent of genotypes in a new population that are mutated.
        /// </summary>
        public const float DefMutationPerc = 1.0f;
        #endregion

        #region Operator Delegates
        /// <summary>
        /// Method template for population initialisation.
        /// </summary>
        public delegate void InitialisationOperator(IEnumerable<Genotype> initialPopulation);

        /// <summary>
        /// Method template for evaluating (or starting evaluation of) the current population.
        /// </summary>
        public delegate void EvaluationOperator(IEnumerable<Genotype> currentPopulation);

        /// <summary>
        /// Method template for calculating the fitness value of each genotype.
        /// </summary>
        public delegate void FitnessCalculation(IEnumerable<Genotype> currentPopulation);

        /// <summary>
        /// Method template for selecting genotypes to create the intermediate population.
        /// </summary>
        public delegate List<Genotype> SelectionOperator(List<Genotype> currentPopulation);

        /// <summary>
        /// Method template for recombining the intermediate population into a new population.
        /// </summary>
        public delegate List<Genotype> RecombinationOperator(List<Genotype> intermediatePopulation, uint newPopulationSize);

        /// <summary>
        /// Method template for mutating the new population.
        /// </summary>
        public delegate void MutationOperator(List<Genotype> newPopulation);

        /// <summary>
        /// Method template for checking whether any termination criterion has been met.
        /// </summary>
        public delegate bool CheckTerminationCriterion(IEnumerable<Genotype> currentPopulation);
        #endregion

        #region Operator Methods
        /// <summary>
        /// Method used to initialise the initial population.
        /// </summary>
        public InitialisationOperator InitialisePopulation = DefaultPopulationInitialisation;

        /// <summary>
        /// Method used to evaluate (or start evaluation of) the current population.
        /// </summary>
        public EvaluationOperator Evaluation = AsyncEvaluation;

        /// <summary>
        /// Method used to calculate the fitness value of each genotype.
        /// </summary>
        public FitnessCalculation FitnessCalculationMethod = DefaultFitnessCalculation;

        /// <summary>
        /// Method used to select genotypes for the intermediate population.
        /// </summary>
        public SelectionOperator Selection = DefaultSelectionOperator;

        /// <summary>
        /// Method used to recombine the intermediate population into a new population.
        /// </summary>
        public RecombinationOperator Recombination = DefaultRecombinationOperator;

        /// <summary>
        /// Method used to mutate the new population.
        /// </summary>
        public MutationOperator Mutation = DefaultMutationOperator;

        /// <summary>
        /// Method used to check whether any termination criterion has been met.
        /// </summary>
        public CheckTerminationCriterion TerminationCriterion = null;
        #endregion

        private List<Genotype> currentPopulation;

        /// <summary>
        /// The number of genotypes in a population.
        /// </summary>
        public uint PopulationSize { get; private set; }

        /// <summary>
        /// The number of generations that have already passed.
        /// </summary>
        public uint GenerationCount { get; private set; }

        /// <summary>
        /// Whether the current population shall be sorted before calling the termination criterion.
        /// </summary>
        public bool SortPopulation { get; private set; }

        /// <summary>
        /// Whether the genetic algorithm is currently running.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Event fired when the algorithm is terminated.
        /// </summary>
        public event Action<GeneticAlgorithm> AlgorithmTerminated;

        /// <summary>
        /// Event fired after fitness calculation is complete. The population is sorted if sorting is enabled.
        /// </summary>
        public event Action<IEnumerable<Genotype>> FitnessCalculationFinished;

        /// <summary>
        /// Initialises a new genetic algorithm with the given population size and genotype parameter count.
        /// </summary>
        /// <param name="genotypeParamCount">The number of parameters per genotype.</param>
        /// <param name="populationSize">The size of the population.</param>
        public GeneticAlgorithm(uint genotypeParamCount, uint populationSize)
        {
            PopulationSize = populationSize;
            currentPopulation = new List<Genotype>((int)populationSize);
            for (int i = 0; i < populationSize; i++)
                currentPopulation.Add(new Genotype(new float[genotypeParamCount]));

            GenerationCount = 1;
            SortPopulation = true;
            Running = false;
        }

        /// <summary>
        /// Starts the genetic algorithm by initialising and evaluating the first population.
        /// </summary>
        public void Start()
        {
            Running = true;
            InitialisePopulation(currentPopulation);
            Evaluation(currentPopulation);
        }

        /// <summary>
        /// To be called when evaluation of the current population is complete.
        /// Triggers the next generation cycle: fitness → selection → recombination → mutation → evaluation.
        /// </summary>
        public void EvaluationFinished()
        {
            // Calculate fitness from evaluation
            FitnessCalculationMethod(currentPopulation);

            // Sort population if flag was set
            if (SortPopulation)
                currentPopulation.Sort();

            // Fire fitness calculation finished event
            FitnessCalculationFinished?.Invoke(currentPopulation);

            // Check termination criterion
            if (TerminationCriterion != null && TerminationCriterion(currentPopulation))
            {
                Terminate();
                return;
            }

            // Apply Selection → Recombination → Mutation
            // Bei PopulationSize <= 1 kann keine Selektion/Rekombination stattfinden
            // → Einzelnen Genotype kopieren und nur mutieren
            List<Genotype> newPopulation;
            if (PopulationSize <= 1)
            {
                newPopulation = new List<Genotype>();
                foreach (Genotype g in currentPopulation)
                    newPopulation.Add(new Genotype(g.GetParameterCopy()));
                Mutation(newPopulation);
            }
            else
            {
                List<Genotype> intermediatePopulation = Selection(currentPopulation);

                // Selection kann bei schlechter Fitness 0-1 Members liefern
                // → mit den besten Genotypen auffuellen (Population ist absteigend sortiert)
                int padIdx = 0;
                while (intermediatePopulation.Count < 2 && padIdx < currentPopulation.Count)
                {
                    intermediatePopulation.Add(new Genotype(currentPopulation[padIdx].GetParameterCopy()));
                    padIdx++;
                }

                newPopulation = Recombination(intermediatePopulation, PopulationSize);
                Mutation(newPopulation);
            }

            // Set current population to newly generated one and start evaluation again
            currentPopulation = newPopulation;
            GenerationCount++;
            Evaluation(currentPopulation);
        }

        private void Terminate()
        {
            Running = false;
            AlgorithmTerminated?.Invoke(this);
        }

        #region Default Operators
        /// <summary>
        /// Initialises the population by setting each parameter to a random value in the default range.
        /// </summary>
        public static void DefaultPopulationInitialisation(IEnumerable<Genotype> population)
        {
            foreach (Genotype genotype in population)
                genotype.SetRandomParameters(DefInitParamMin, DefInitParamMax);
        }

        /// <summary>
        /// Placeholder for async evaluation. Override this with your actual evaluation logic.
        /// Call <see cref="EvaluationFinished"/> when evaluation is done.
        /// </summary>
        public static void AsyncEvaluation(IEnumerable<Genotype> currentPopulation)
        {
            // Override: start evaluation, then call EvaluationFinished() when complete.
        }

        /// <summary>
        /// Calculates fitness of each genotype: fitness = evaluation / averageEvaluation.
        /// </summary>
        public static void DefaultFitnessCalculation(IEnumerable<Genotype> currentPopulation)
        {
            uint populationSize = 0;
            float overallEvaluation = 0;
            foreach (Genotype genotype in currentPopulation)
            {
                overallEvaluation += genotype.Evaluation;
                populationSize++;
            }

            float averageEvaluation = overallEvaluation / populationSize;

            foreach (Genotype genotype in currentPopulation)
                genotype.Fitness = genotype.Evaluation / averageEvaluation;
        }

        /// <summary>
        /// Selects the best three genotypes from the current population (elitist selection).
        /// </summary>
        public static List<Genotype> DefaultSelectionOperator(List<Genotype> currentPopulation)
        {
            List<Genotype> intermediatePopulation = new List<Genotype>();
            intermediatePopulation.Add(currentPopulation[0]);
            intermediatePopulation.Add(currentPopulation[1]);
            intermediatePopulation.Add(currentPopulation[2]);
            return intermediatePopulation;
        }

        /// <summary>
        /// Crosses the first with the second genotype of the intermediate population until the new
        /// population reaches the desired size.
        /// </summary>
        public static List<Genotype> DefaultRecombinationOperator(List<Genotype> intermediatePopulation, uint newPopulationSize)
        {
            if (intermediatePopulation.Count < 2)
                throw new ArgumentException("Intermediate population size must be at least 2 for this operator.");

            List<Genotype> newPopulation = new List<Genotype>();
            while (newPopulation.Count < newPopulationSize)
            {
                CompleteCrossover(intermediatePopulation[0], intermediatePopulation[1], DefCrossSwapProb,
                    out Genotype offspring1, out Genotype offspring2);

                newPopulation.Add(offspring1);
                if (newPopulation.Count < newPopulationSize)
                    newPopulation.Add(offspring2);
            }

            return newPopulation;
        }

        /// <summary>
        /// Mutates each genotype with the default mutation probability and amount.
        /// </summary>
        public static void DefaultMutationOperator(List<Genotype> newPopulation)
        {
            foreach (Genotype genotype in newPopulation)
            {
                if (MathHelper.Randomizer.NextDouble() < DefMutationPerc)
                    MutateGenotype(genotype, DefMutationProb, DefMutationAmount);
            }
        }
        #endregion

        #region Recombination Operators
        /// <summary>
        /// Performs complete crossover between two parent genotypes: for each parameter,
        /// swap with the given probability.
        /// </summary>
        public static void CompleteCrossover(Genotype parent1, Genotype parent2, float swapChance,
            out Genotype offspring1, out Genotype offspring2)
        {
            int parameterCount = parent1.ParameterCount;
            float[] off1Parameters = new float[parameterCount];
            float[] off2Parameters = new float[parameterCount];

            for (int i = 0; i < parameterCount; i++)
            {
                if (MathHelper.Randomizer.NextDouble() < swapChance)
                {
                    off1Parameters[i] = parent2[i];
                    off2Parameters[i] = parent1[i];
                }
                else
                {
                    off1Parameters[i] = parent1[i];
                    off2Parameters[i] = parent2[i];
                }
            }

            offspring1 = new Genotype(off1Parameters);
            offspring2 = new Genotype(off2Parameters);
        }
        #endregion

        #region Mutation Operators
        /// <summary>
        /// Mutates the given genotype by adding a random value in [-mutationAmount, mutationAmount]
        /// to each parameter with the given probability.
        /// </summary>
        public static void MutateGenotype(Genotype genotype, float mutationProb, float mutationAmount)
        {
            for (int i = 0; i < genotype.ParameterCount; i++)
            {
                if (MathHelper.Randomizer.NextDouble() < mutationProb)
                    genotype[i] += (float)(MathHelper.Randomizer.NextDouble() * (mutationAmount * 2) - mutationAmount);
            }
        }
        #endregion

        #region Additional Selection Operators
        /// <summary>
        /// Remainder stochastic sampling: selects genotypes based on their fitness value.
        /// Integer part determines guaranteed copies, fractional part is a probability for an additional copy.
        /// </summary>
        public static List<Genotype> RemainderStochasticSampling(List<Genotype> currentPopulation)
        {
            List<Genotype> intermediatePopulation = new List<Genotype>();

            // Put integer portion of genotypes into intermediate population (assumes sorted)
            foreach (Genotype genotype in currentPopulation)
            {
                if (genotype.Fitness < 1)
                    break;

                for (int i = 0; i < (int)genotype.Fitness; i++)
                    intermediatePopulation.Add(new Genotype(genotype.GetParameterCopy()));
            }

            // Put remainder portion
            foreach (Genotype genotype in currentPopulation)
            {
                float remainder = genotype.Fitness - (int)genotype.Fitness;
                if (MathHelper.Randomizer.NextDouble() < remainder)
                    intermediatePopulation.Add(new Genotype(genotype.GetParameterCopy()));
            }

            return intermediatePopulation;
        }

        /// <summary>
        /// Random recombination: picks random pairs from the intermediate population for crossover.
        /// Always preserves the best two genotypes unmodified.
        /// </summary>
        public static List<Genotype> RandomRecombination(List<Genotype> intermediatePopulation, uint newPopulationSize)
        {
            if (intermediatePopulation.Count < 2)
                throw new ArgumentException("Intermediate population must have at least 2 members.");

            List<Genotype> newPopulation = new List<Genotype>();
            // Always preserve best two (unmodified)
            newPopulation.Add(intermediatePopulation[0]);
            newPopulation.Add(intermediatePopulation[1]);

            while (newPopulation.Count < newPopulationSize)
            {
                int randomIndex1 = MathHelper.Randomizer.Next(0, intermediatePopulation.Count);
                int randomIndex2;
                do
                {
                    randomIndex2 = MathHelper.Randomizer.Next(0, intermediatePopulation.Count);
                } while (randomIndex2 == randomIndex1);

                CompleteCrossover(intermediatePopulation[randomIndex1], intermediatePopulation[randomIndex2],
                    DefCrossSwapProb, out Genotype offspring1, out Genotype offspring2);

                newPopulation.Add(offspring1);
                if (newPopulation.Count < newPopulationSize)
                    newPopulation.Add(offspring2);
            }

            return newPopulation;
        }

        /// <summary>
        /// Mutates all genotypes except the best two (elitist preservation).
        /// </summary>
        public static void MutateAllButBestTwo(List<Genotype> newPopulation)
        {
            for (int i = 2; i < newPopulation.Count; i++)
            {
                if (MathHelper.Randomizer.NextDouble() < DefMutationPerc)
                    MutateGenotype(newPopulation[i], DefMutationProb, DefMutationAmount);
            }
        }
        #endregion

        #region Population Access (Save/Load)
        /// <summary>
        /// Gibt eine Kopie der aktuellen Population zurueck (fuer Serialisierung).
        /// </summary>
        public List<Genotype> GetPopulationCopy()
        {
            List<Genotype> copy = new List<Genotype>(currentPopulation.Count);
            foreach (Genotype g in currentPopulation)
            {
                copy.Add(new Genotype(g.GetParameterCopy()));
            }
            return copy;
        }

        /// <summary>
        /// Laedt eine gespeicherte Population und setzt die Generationszaehlung.
        /// Ueberschreibt die aktuelle Population. Muss vor Start() aufgerufen werden
        /// oder ersetzt die laufende Population (GA wird neugestartet mit Evaluation).
        /// </summary>
        /// <param name="genotypes">Liste von Genotypes mit gespeicherten Weights.</param>
        /// <param name="generation">Gespeicherte Generationszahl.</param>
        public void LoadPopulation(List<Genotype> genotypes, uint generation)
        {
            currentPopulation = new List<Genotype>((int)PopulationSize);
            int loadCount = Math.Min(genotypes.Count, (int)PopulationSize);
            for (int i = 0; i < loadCount; i++)
            {
                currentPopulation.Add(genotypes[i]);
            }

            // Falls gespeicherte Population kleiner als aktuelle PopSize: Rest mit Random auffuellen
            uint paramCount = genotypes.Count > 0 ? (uint)genotypes[0].ParameterCount : 0;
            while (currentPopulation.Count < (int)PopulationSize)
            {
                Genotype fill = Genotype.GenerateRandom(paramCount, DefInitParamMin, DefInitParamMax);
                currentPopulation.Add(fill);
            }

            GenerationCount = generation;
        }
        #endregion
    }
}
