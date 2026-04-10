using System;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Provides shared math utilities for the EANN system, including activation functions
    /// and a centralised random number generator.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Shared random number generator for the entire EANN system.
        /// Centralised to avoid duplicate seeds when multiple instances are created close together.
        /// </summary>
        public static Random Randomizer { get; } = new Random();

        /// <summary>
        /// Standard sigmoid activation function: 1 / (1 + e^(-x)).
        /// Output range: (0, 1).
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The sigmoid of the input value.</returns>
        public static double SigmoidFunction(double x)
        {
            if (x > 10.0) return 1.0;
            if (x < -10.0) return 0.0;
            return 1.0 / (1.0 + Math.Exp(-x));
        }

        /// <summary>
        /// SoftSign activation function: x / (1 + |x|).
        /// Output range: (-1, 1). Smoother gradient than TanH.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The soft sign of the input value.</returns>
        public static double SoftSignFunction(double x)
        {
            return x / (1.0 + Math.Abs(x));
        }

        /// <summary>
        /// Hyperbolic tangent activation function.
        /// Output range: (-1, 1).
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The hyperbolic tangent of the input value.</returns>
        public static double TanHFunction(double x)
        {
            return Math.Tanh(x);
        }

        /// <summary>
        /// ReLU (Rectified Linear Unit) activation function: max(0, x).
        /// Output range: [0, +inf).
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The ReLU of the input value.</returns>
        public static double ReLUFunction(double x)
        {
            return Math.Max(0.0, x);
        }
    }
}
