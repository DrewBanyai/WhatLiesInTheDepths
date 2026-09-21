// Ursine — odds from a strength ratio.
//
// Twice their strength is certain, half is hopeless, equal is even, and between them it
// runs straight in log2. The shape matters more than the numbers: it means doubling an
// army is always worth the same amount of confidence, wherever the player starts.
using System;

namespace Ursine.Combat
{
    public static class Odds
    {
        /// <summary>Chance of winning, 0 to 1. At or below the floor ratio it is zero; at or
        /// above the ceiling ratio it is one; equal strength is exactly one half.</summary>
        public static double Chance(double ourStrength, double theirStrength,
                                    double floorRatio = 0.5, double ceilingRatio = 2.0)
        {
            if (theirStrength <= 0) return 1.0;
            double ratio = ourStrength / theirStrength;
            if (ratio <= floorRatio) return 0.0;
            if (ratio >= ceilingRatio) return 1.0;
            return 0.5 * (1.0 + Math.Log(ratio, 2.0));
        }

        /// <summary>What a loss costs, as a fraction of strength.</summary>
        public static double LossFraction(Random rng, double min = 0.25, double max = 0.45)
            => min + rng.NextDouble() * (max - min);
    }
}
