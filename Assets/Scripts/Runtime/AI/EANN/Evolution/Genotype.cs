using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Represents one member (individual) of a population, storing a vector of float parameters.
    /// Implements IComparable (sorted by fitness descending) and IEnumerable for parameter iteration.
    /// </summary>
    public class Genotype : IComparable<Genotype>, IEnumerable<float>
    {
        /// <summary>
        /// The current evaluation score of this genotype (raw performance metric).
        /// </summary>
        public float Evaluation { get; set; }

        /// <summary>
        /// The current fitness of this genotype (evaluation relative to population average).
        /// </summary>
        public float Fitness { get; set; }

        private float[] parameters;

        /// <summary>
        /// The number of parameters in this genotype's parameter vector.
        /// </summary>
        public int ParameterCount
        {
            get
            {
                if (parameters == null) return 0;
                return parameters.Length;
            }
        }

        /// <summary>
        /// Indexer for convenient parameter access.
        /// </summary>
        public float this[int index]
        {
            get { return parameters[index]; }
            set { parameters[index] = value; }
        }

        /// <summary>
        /// Creates a new genotype with the given parameter vector and initial fitness of 0.
        /// </summary>
        /// <param name="parameters">The parameter vector to initialise this genotype with.</param>
        public Genotype(float[] parameters)
        {
            this.parameters = parameters;
            Fitness = 0;
        }

        /// <summary>
        /// Compares this genotype with another by fitness (descending: higher fitness comes first).
        /// </summary>
        public int CompareTo(Genotype other)
        {
            return other.Fitness.CompareTo(Fitness);
        }

        /// <summary>
        /// Gets an enumerator to iterate over all parameters of this genotype.
        /// </summary>
        public IEnumerator<float> GetEnumerator()
        {
            for (int i = 0; i < parameters.Length; i++)
                yield return parameters[i];
        }

        /// <summary>
        /// Gets an enumerator to iterate over all parameters of this genotype.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < parameters.Length; i++)
                yield return parameters[i];
        }

        /// <summary>
        /// Sets all parameters to random values in the given range.
        /// </summary>
        /// <param name="minValue">The minimum inclusive value.</param>
        /// <param name="maxValue">The maximum exclusive value.</param>
        public void SetRandomParameters(float minValue, float maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentException("Minimum value may not exceed maximum value.");

            float range = maxValue - minValue;
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = (float)(MathHelper.Randomizer.NextDouble() * range + minValue);
        }

        /// <summary>
        /// Returns a copy of the parameter vector.
        /// </summary>
        public float[] GetParameterCopy()
        {
            float[] copy = new float[ParameterCount];
            Array.Copy(parameters, copy, ParameterCount);
            return copy;
        }

        /// <summary>
        /// Saves the parameters of this genotype to a file at the given path.
        /// </summary>
        /// <param name="filePath">The file path to save to.</param>
        public void SaveToFile(string filePath)
        {
            StringBuilder builder = new StringBuilder();
            foreach (float param in parameters)
                builder.Append(param.ToString()).Append(";");

            builder.Remove(builder.Length - 1, 1);
            File.WriteAllText(filePath, builder.ToString());
        }

        /// <summary>
        /// Generates a random genotype with parameters in the given range.
        /// </summary>
        /// <param name="parameterCount">The number of parameters.</param>
        /// <param name="minValue">The minimum inclusive value.</param>
        /// <param name="maxValue">The maximum exclusive value.</param>
        /// <returns>A genotype with random parameter values.</returns>
        public static Genotype GenerateRandom(uint parameterCount, float minValue, float maxValue)
        {
            if (parameterCount == 0) return new Genotype(new float[0]);

            Genotype randomGenotype = new Genotype(new float[parameterCount]);
            randomGenotype.SetRandomParameters(minValue, maxValue);
            return randomGenotype;
        }

        /// <summary>
        /// Loads a genotype from a semicolon-separated file.
        /// </summary>
        /// <param name="filePath">The file path to load from.</param>
        /// <returns>The loaded genotype.</returns>
        public static Genotype LoadFromFile(string filePath)
        {
            string data = File.ReadAllText(filePath);
            string[] paramStrings = data.Split(';');
            List<float> paramList = new List<float>();

            foreach (string parameter in paramStrings)
            {
                if (!float.TryParse(parameter, out float parsed))
                    throw new ArgumentException("The file does not contain a valid genotype serialisation.");
                paramList.Add(parsed);
            }

            return new Genotype(paramList.ToArray());
        }
    }
}
