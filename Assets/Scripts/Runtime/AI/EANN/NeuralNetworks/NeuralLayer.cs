using System;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Represents a single layer of a fully connected feedforward neural network.
    /// Weights are stored as float for memory efficiency. Computation is done in double precision.
    /// </summary>
    public class NeuralLayer
    {
        /// <summary>
        /// Delegate representing the activation function of an artificial neuron.
        /// </summary>
        /// <param name="xValue">The input value of the function.</param>
        /// <returns>The calculated output value of the function.</returns>
        public delegate double ActivationFunction(double xValue);

        /// <summary>
        /// The activation function used by the neurons of this layer.
        /// </summary>
        public ActivationFunction NeuronActivationFunction = MathHelper.SigmoidFunction;

        /// <summary>
        /// The amount of neurons in this layer.
        /// </summary>
        public uint NeuronCount { get; private set; }

        /// <summary>
        /// The amount of neurons this layer is connected to, i.e., the amount of neurons of the next layer.
        /// </summary>
        public uint OutputCount { get; private set; }

        /// <summary>
        /// The weights of the connections of this layer to the next layer.
        /// Weight [i, j] is the weight of the connection from the i-th neuron
        /// of this layer to the j-th neuron of the next layer.
        /// Row count includes +1 for the bias node.
        /// </summary>
        public float[,] Weights { get; private set; }

        /// <summary>
        /// Initialises a new neural layer with given node count and connections to the next layer.
        /// </summary>
        /// <param name="nodeCount">The amount of nodes in this layer.</param>
        /// <param name="outputCount">The amount of nodes in the next layer.</param>
        public NeuralLayer(uint nodeCount, uint outputCount)
        {
            NeuronCount = nodeCount;
            OutputCount = outputCount;
            Weights = new float[nodeCount + 1, outputCount]; // +1 for bias node
        }

        /// <summary>
        /// Sets the weights of this layer to the given values.
        /// Values are ordered in neuron order: weights [0..outputCount-1] are from neuron 0 to all output neurons, etc.
        /// </summary>
        /// <param name="weights">The flat array of weight values to set.</param>
        public void SetWeights(float[] weights)
        {
            if (weights.Length != Weights.Length)
                throw new ArgumentException("Input weights do not match layer weight count.");

            int k = 0;
            for (int i = 0; i < Weights.GetLength(0); i++)
                for (int j = 0; j < Weights.GetLength(1); j++)
                    Weights[i, j] = weights[k++];
        }

        /// <summary>
        /// Processes the given inputs using the current weights to the next layer.
        /// Computation is done in double precision for numerical stability.
        /// </summary>
        /// <param name="inputs">The inputs to be processed.</param>
        /// <returns>The calculated outputs.</returns>
        public double[] ProcessInputs(double[] inputs)
        {
            if (inputs.Length != NeuronCount)
                throw new ArgumentException("Given inputs do not match layer input count.");

            // Add bias (always-on) neuron to inputs
            double[] biasedInputs = new double[NeuronCount + 1];
            inputs.CopyTo(biasedInputs, 0);
            biasedInputs[inputs.Length] = 1.0;

            // Calculate weighted sum for each output neuron
            double[] sums = new double[OutputCount];
            for (int j = 0; j < Weights.GetLength(1); j++)
                for (int i = 0; i < Weights.GetLength(0); i++)
                    sums[j] += biasedInputs[i] * Weights[i, j];

            // Apply activation function
            if (NeuronActivationFunction != null)
            {
                for (int i = 0; i < sums.Length; i++)
                    sums[i] = NeuronActivationFunction(sums[i]);
            }

            return sums;
        }

        /// <summary>
        /// Creates a deep copy of this NeuralLayer including its weights and activation function.
        /// </summary>
        /// <returns>A deep copy of this NeuralLayer.</returns>
        public NeuralLayer DeepCopy()
        {
            float[,] copiedWeights = new float[Weights.GetLength(0), Weights.GetLength(1)];
            for (int x = 0; x < Weights.GetLength(0); x++)
                for (int y = 0; y < Weights.GetLength(1); y++)
                    copiedWeights[x, y] = Weights[x, y];

            NeuralLayer newLayer = new NeuralLayer(NeuronCount, OutputCount);
            newLayer.Weights = copiedWeights;
            newLayer.NeuronActivationFunction = NeuronActivationFunction;
            return newLayer;
        }

        /// <summary>
        /// Sets the weights of this layer to random values in given range.
        /// </summary>
        /// <param name="minValue">The minimum value a weight may be set to.</param>
        /// <param name="maxValue">The maximum value a weight may be set to.</param>
        public void SetRandomWeights(float minValue, float maxValue)
        {
            float range = Math.Abs(maxValue - minValue);
            for (int i = 0; i < Weights.GetLength(0); i++)
                for (int j = 0; j < Weights.GetLength(1); j++)
                    Weights[i, j] = minValue + (float)(MathHelper.Randomizer.NextDouble() * range);
        }

        /// <summary>
        /// Returns a string representing this layer's connection weights.
        /// </summary>
        public override string ToString()
        {
            string output = "";
            for (int x = 0; x < Weights.GetLength(0); x++)
            {
                for (int y = 0; y < Weights.GetLength(1); y++)
                    output += "[" + x + "," + y + "]: " + Weights[x, y];
                output += "\n";
            }
            return output;
        }
    }
}
