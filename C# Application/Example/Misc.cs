using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LottoNumbers.Example
{
    public static class Misc
    {


        /// <summary>
        /// Remaps a value from a range to another.
        /// </summary>
        /// <param name="value">The value to remap.</param>
        /// <param name="currentLow">The value's low range.</param>
        /// <param name="currentHigh">The value's high range.</param>
        /// <param name="targetLow">The target low range.</param>
        /// <param name="targetHigh">The target high range.</param>
        /// <returns><paramref name="value"/> remapped to the target range.</returns>
        public static double RemapDouble(double value, double currentLow, double currentHigh, double targetLow, double targetHigh)
        {
            return (value - currentLow) / (currentHigh - currentLow) * (targetHigh - targetLow) + targetLow;
        }

    }
}
