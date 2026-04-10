using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Combines a <see cref="Genotype"/> and a feedforward <see cref="NeuralNetwork"/> (FNN).
    /// The genotype's parameters are used as the network's weights.
    /// </summary>
    public class Agent : IComparable<Agent>
    {
        /// <summary>
        /// The underlying genotype of this agent.
        /// </summary>
        public Genotype Genotype { get; private set; }

        /// <summary>
        /// The feedforward neural network constructed from this agent's genotype.
        /// </summary>
        public NeuralNetwork FNN { get; private set; }

        private bool isAlive = false;

        /// <summary>
        /// Whether this agent is currently alive (actively participating in the simulation).
        /// </summary>
        public bool IsAlive
        {
            get { return isAlive; }
            private set
            {
                if (isAlive != value)
                {
                    isAlive = value;
                    if (!isAlive)
                        AgentDied?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// Event fired when the agent dies (stops participating in the simulation).
        /// </summary>
        public event Action<Agent> AgentDied;

        /// <summary>
        /// Initialises a new agent from the given genotype, constructing a feedforward neural network
        /// with the specified topology and activation function.
        /// </summary>
        /// <param name="genotype">The genotype whose parameters become the network weights.</param>
        /// <param name="defaultActivation">The activation function for all layers.</param>
        /// <param name="topology">The network topology (node counts per layer from input to output).</param>
        public Agent(Genotype genotype, NeuralLayer.ActivationFunction defaultActivation, params uint[] topology)
        {
            IsAlive = false;
            Genotype = genotype;
            FNN = new NeuralNetwork(topology);

            foreach (NeuralLayer layer in FNN.Layers)
                layer.NeuronActivationFunction = defaultActivation;

            // Validate that genotype parameter count matches network weight count
            if (FNN.WeightCount != genotype.ParameterCount)
                throw new ArgumentException("The genotype's parameter count must match the neural network topology's weight count.");

            // Construct FNN weights from genotype parameters
            IEnumerator<float> parameters = genotype.GetEnumerator();
            foreach (NeuralLayer layer in FNN.Layers)
            {
                for (int i = 0; i < layer.Weights.GetLength(0); i++)
                {
                    for (int j = 0; j < layer.Weights.GetLength(1); j++)
                    {
                        layer.Weights[i, j] = parameters.Current;
                        parameters.MoveNext();
                    }
                }
            }
        }

        /// <summary>
        /// Resets this agent to be alive again with zeroed evaluation and fitness.
        /// </summary>
        public void Reset()
        {
            Genotype.Evaluation = 0;
            Genotype.Fitness = 0;
            IsAlive = true;
        }

        /// <summary>
        /// Kills this agent (sets IsAlive to false, firing the AgentDied event).
        /// </summary>
        public void Kill()
        {
            IsAlive = false;
        }

        /// <summary>
        /// Compares this agent to another by comparing their underlying genotypes (fitness descending).
        /// </summary>
        public int CompareTo(Agent other)
        {
            return Genotype.CompareTo(other.Genotype);
        }
    }
}
