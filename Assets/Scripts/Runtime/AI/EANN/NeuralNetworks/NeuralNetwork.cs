using System;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Represents a fully connected feedforward neural network.
    /// </summary>
    public class NeuralNetwork
    {
        /// <summary>
        /// The individual neural layers of this network.
        /// </summary>
        public NeuralLayer[] Layers { get; private set; }

        /// <summary>
        /// An array of unsigned integers representing the node count
        /// of each layer of the network from input to output layer.
        /// </summary>
        public uint[] Topology { get; private set; }

        /// <summary>
        /// The total number of weights (connections) in this network, including bias weights.
        /// </summary>
        public int WeightCount { get; private set; }

        /// <summary>
        /// Initialises a new fully connected feedforward neural network with given topology.
        /// </summary>
        /// <param name="topology">Node counts per layer from input to output.</param>
        public NeuralNetwork(params uint[] topology)
        {
            Topology = topology;

            // Calculate overall weight count (+1 per layer for bias node)
            WeightCount = 0;
            for (int i = 0; i < topology.Length - 1; i++)
                WeightCount += (int)((topology[i] + 1) * topology[i + 1]);

            // Initialise layers
            Layers = new NeuralLayer[topology.Length - 1];
            for (int i = 0; i < Layers.Length; i++)
                Layers[i] = new NeuralLayer(topology[i], topology[i + 1]);
        }

        /// <summary>
        /// Processes the given inputs using the current network's weights.
        /// </summary>
        /// <param name="inputs">The inputs to be processed.</param>
        /// <returns>The calculated outputs.</returns>
        public double[] ProcessInputs(double[] inputs)
        {
            if (inputs.Length != Layers[0].NeuronCount)
                throw new ArgumentException("Given inputs do not match network input amount.");

            double[] outputs = inputs;
            foreach (NeuralLayer layer in Layers)
                outputs = layer.ProcessInputs(outputs);

            return outputs;
        }

        /// <summary>
        /// Sets the weights of this network to random values in given range.
        /// </summary>
        /// <param name="minValue">The minimum value a weight may be set to.</param>
        /// <param name="maxValue">The maximum value a weight may be set to.</param>
        public void SetRandomWeights(float minValue, float maxValue)
        {
            if (Layers == null) return;

            foreach (NeuralLayer layer in Layers)
                layer.SetRandomWeights(minValue, maxValue);
        }

        /// <summary>
        /// Returns a new NeuralNetwork with the same topology and activation functions but default weights.
        /// </summary>
        public NeuralNetwork GetTopologyCopy()
        {
            NeuralNetwork copy = new NeuralNetwork(Topology);
            for (int i = 0; i < Layers.Length; i++)
                copy.Layers[i].NeuronActivationFunction = Layers[i].NeuronActivationFunction;

            return copy;
        }

        /// <summary>
        /// Creates a deep copy of this NeuralNetwork including topology, weights and activation functions.
        /// </summary>
        /// <returns>A deep copy of this NeuralNetwork.</returns>
        public NeuralNetwork DeepCopy()
        {
            NeuralNetwork newNet = new NeuralNetwork(Topology);
            for (int i = 0; i < Layers.Length; i++)
                newNet.Layers[i] = Layers[i].DeepCopy();

            return newNet;
        }

        /// <summary>
        /// Returns a string representing this network in layer order.
        /// </summary>
        public override string ToString()
        {
            string output = "";
            for (int i = 0; i < Layers.Length; i++)
                output += "Layer " + i + ":\n" + Layers[i].ToString();

            return output;
        }
    }
}
