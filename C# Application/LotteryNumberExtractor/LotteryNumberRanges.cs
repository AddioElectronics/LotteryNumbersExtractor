using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LottoNumbers.Extractor.Lottery;

namespace LottoNumbers
{
    /// <summary>
    /// Holds the values for the specific lottery's number ranges, and pick count.
    /// </summary>
    [Serializable]
    public struct LotteryNumberRanges
    {

        /// <summary>
        /// The lottery these ranges are from.
        /// </summary>
        public Lotto lottery { get; set; }

        /// <summary>
        /// The lowest number that can be pick/drawn.
        /// </summary>
        public int standardLow { get; set; }

        /// <summary>
        /// The highest number that can be pick/drawn.
        /// </summary>
        public int standardHigh { get; set; }

        /// <summary>
        /// The amount of numbers you get to pick.
        /// </summary>
        public int standardPickCount { get; set; }

        /// <summary>
        /// The lowest bonus number that can be drawn.
        /// </summary>
        public int bonusLow { get; set; }

        /// <summary>
        /// The highest bonus number that can be drawn.
        /// </summary>
        public int bonusHigh { get; set; }

        /// <summary>
        /// The lowest extra number that can be drawn.
        /// </summary>
        public int extraLow { get; set; }

        /// <summary>
        /// The highest extra number that can be drawn.
        /// </summary>
        public int extraHigh { get; set; }

        /// <summary>
        /// For defining a lottery that has a no extra.
        /// Bonus range will take the value of the standard number range.
        /// </summary>
        /// <param name="lottery">The lottery these ranges are for.</param>
        /// <param name="low">The lowest number that can be pick/drawn.</param>
        /// <param name="high">The lowest number that can be pick/drawn.</param>
        /// <param name="standardPickCount">The amount of numbers you get to pick.</param>
        /// <param name="bonus">If true, the bonus number range will take the standard range values.</param>
        public LotteryNumberRanges(Lotto lottery, int low, int high, int standardPickCount, bool bonus = true)
        {
            this.lottery = lottery;
            this.standardLow = low;
            this.standardHigh = high;
            this.standardPickCount = standardPickCount;

            if (bonus)
            {
                this.bonusLow = this.standardLow;
                this.bonusHigh = this.standardHigh;
            }
            else
            {
                this.bonusLow = 0;
                this.bonusHigh = 0;
            }

            this.extraLow = -1;
            this.extraHigh = -1;
        }

        /// <summary>
        /// For defining a lottery that has a different bonus number range, but no extra.
        /// </summary>
        /// <param name="lottery">The lottery these ranges are for.</param>
        /// <param name="low">The lowest number that can be pick/drawn.</param>
        /// <param name="high">The lowest number that can be pick/drawn.</param>
        /// <param name="standardPickCount">The amount of numbers you get to pick.</param>
        /// <param name="bonusLow">The lowest bonus number that can be drawn.</param>
        /// <param name="bonusHigh">The highest bonus number that can be drawn.</param>
        public LotteryNumberRanges(Lotto lottery, int low, int high, int standardPickCount, int bonusLow, int bonusHigh)
        {
            this.lottery = lottery;
            this.standardLow = low;
            this.standardHigh = high;
            this.standardPickCount = standardPickCount;

            this.bonusLow = bonusLow;
            this.bonusHigh = bonusHigh;

            this.extraLow = -1;
            this.extraHigh = -1;
        }

        /// <summary>
        /// For defining a lottery that has extra numbers.
        /// Bonus numbers
        /// </summary>
        /// <param name="lottery">The lottery these ranges are for.</param>
        /// <param name="low">The lowest number that can be pick/drawn.</param>
        /// <param name="high">The lowest number that can be pick/drawn.</param>
        /// <param name="standardPickCount">The amount of numbers you get to pick.</param>
        /// <param name="extraLow">The lowest extra number that can be drawn.</param>
        /// <param name="extraHigh">The highest extra number that can be drawn.</param>
        /// <param name="bonus">If true, the bonus number range will take the standard range values.</param>
        public LotteryNumberRanges(Lotto lottery, int low, int high, int standardPickCount, int extraLow, int extraHigh, bool bonus = true)
        {
            this.lottery = lottery;
            this.standardLow = low;
            this.standardHigh = high;
            this.standardPickCount = standardPickCount;

            if (bonus)
            {
                this.bonusLow = this.standardLow;
                this.bonusHigh = this.standardHigh;
            }
            else
            {
                this.bonusLow = 0;
                this.bonusHigh = 0;
            }

            this.extraLow = extraLow;
            this.extraHigh = extraHigh;
        }


        /// <summary>
        /// For defining a lottery that has a bonus number, and extra numbers.
        /// </summary>
        /// <param name="lottery">The lottery these ranges are for.</param>
        /// <param name="low">The lowest number that can be pick/drawn.</param>
        /// <param name="high">The lowest number that can be pick/drawn.</param>
        /// <param name="standardPickCount">The amount of numbers you get to pick.</param>
        /// <param name="bonusLow">The lowest bonus number that can be drawn.</param>
        /// <param name="bonusHigh">The highest bonus number that can be drawn.</param>
        /// <param name="extraLow">The lowest extra number that can be drawn.</param>
        /// <param name="extraHigh">The highest extra number that can be drawn.</param>
        public LotteryNumberRanges(Lotto lottery, int low, int high, int standardPickCount, int bonusLow, int bonusHigh, int extraLow, int extraHigh)
        {
            this.lottery = lottery;
            this.standardLow = low;
            this.standardHigh = high;
            this.standardPickCount = standardPickCount;

            this.bonusLow = bonusLow;
            this.bonusHigh = bonusHigh;

            this.extraLow = extraLow;
            this.extraHigh = extraHigh;
        }

    }
}
