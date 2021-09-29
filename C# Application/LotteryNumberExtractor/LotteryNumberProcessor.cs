using System;
using System.Linq;
using System.Collections.Generic;
using LottoNumbers.Extractor;
using System.Threading;
using System.Threading.Tasks;

namespace LottoNumbers.Processing
{
    public class LotteryNumberProcessor
    {


        /// <summary>
        /// Counts the frequency of each number drawn from the <paramref name="lotteryNumbers"/> set.
        /// </summary>
        /// <param name="lotteryNumbers">The set of <see cref="LotteryNumbers"/> to count.</param>
        /// <param name="ranges">The possible numbers that can be drawn.</param>
        /// <param name="extraFrequency">If Extra numbers were included in <paramref name="lotteryNumbers"/>, their frequency will be returned here. Null if extra were not included.</param>
        /// <param name="max">The highest draw frequency from all the numbers.</param>
        /// <param name="includeBonus">Should the bonus number be added to the frequency count? Only will for lotteries who's bonus number shares the same range as their standard numbers.</param>
        /// <returns>An array of <see cref="int"/>, which holds the frequency each number has been drawn.</returns>
        public static int[] GetNumberFrequency(LotteryNumbers[] lotteryNumbers, LotteryNumberRanges ranges, out int[] extraFrequency, bool includeBonus = true)
        {
            int[] numbers = new int[ranges.standardHigh];
            extraFrequency = null;

            if(lotteryNumbers[0].extra != null)
                extraFrequency = new int[ranges.extraHigh];

            foreach (LotteryNumbers ln in lotteryNumbers)
            {
                //Count standard numbers
                foreach(int i in ln.numbers)
                {
                    numbers[i-ranges.standardLow]++;
                }

                //Only if bonus is same range as standard numbers, will it be added.
                if (includeBonus)
                    if (ranges.bonusHigh == ranges.standardHigh && ranges.bonusLow == ranges.standardLow)
                        numbers[ln.bonus - ranges.standardLow]++;

                //Count extra numbers.
                if (extraFrequency != null && ln.extra != null)
                {
                    foreach (int i in ln.extra)
                    {
                        extraFrequency[i - ranges.extraLow]++;
                    }
                }
            }
            return numbers;          
        }


        /// <summary>
        /// Searches the <paramref name="lotteryNumbers"/> while counting the numbers that have shown up in a draw together.
        /// </summary>
        /// <param name="lotteryNumbers">The <see cref="LotteryNumbers"/> to search.</param>
        /// <param name="ranges">The possible numbers that can be drawn.</param>
        /// <param name="mostOccuringPairs">Returns the pairs that occured the most together.</param>
        /// <returns>A count of each pairing numbers from every draw.</returns>
        public static int[][] GetCountOfPairsThatOccurTogether(LotteryNumbers[] lotteryNumbers, LotteryNumberRanges ranges, out Tuple<int,int[]>[] mostOccuringPairs)
        {
            //object numbersLock = new object();
            int[][] numbers = new int[ranges.standardHigh - ranges.standardLow][];

            for (int a = ranges.standardLow; a < ranges.standardHigh; a++)
            {
                numbers[a - ranges.standardLow] = new int[ranges.standardHigh - ranges.standardLow];
                for (int b = ranges.standardLow; b < ranges.standardHigh; b++)
                {
                    if (a == b) continue;

                    foreach (LotteryNumbers ln in lotteryNumbers)
                    {
                        if (ln.numbers.Contains(a) && ln.numbers.Contains(b))
                        {
                            //Monitor.Enter(numbersLock);
                            numbers[a - ranges.standardLow][b - ranges.standardLow]++;
                            //Monitor.Exit(numbersLock);
                        }
                    }
                }
            }

            mostOccuringPairs = new Tuple<int, int[]>[ranges.standardPickCount];

            for (int a = ranges.standardLow; a < ranges.standardHigh; a++)
            {
                for (int b = ranges.standardLow; b < ranges.standardHigh; b++)
                {
                    if (a == b) continue;

                    if (mostOccuringPairs.Any(x => x != null && x.Item2[0] == b && x.Item2[1] == a)) continue;

                    for (int p = 0; p < mostOccuringPairs.Length; p++)
                    {
                        while (mostOccuringPairs.Any(x => x == null || x.Item1 == 0) && (mostOccuringPairs[p] != null && mostOccuringPairs[p].Item1 != 0))
                        {
                            p++;
                        }                        

                        if(mostOccuringPairs[p] == null || numbers[a - ranges.standardLow][b - ranges.standardLow] > mostOccuringPairs[p].Item1)
                        {
                            mostOccuringPairs[p] = new Tuple<int, int[]>(numbers[a - ranges.standardLow][b - ranges.standardLow], new int[] { a, b});
                            //mostOccuringPairs[p].a = a;
                            //mostOccuringPairs[p].b = b;
                            //mostOccuringPairs[p].count = numbers[a - ranges.standardLow][b - ranges.standardLow];
                            break;
                        }
                    }
                }
            }

            return numbers;
        }



        /// <summary>
        /// Searches an array of <see cref="LotteryNumbers"/> to see if there has been any draws with matching numbers.
        /// </summary>
        /// <param name="lotteryNumbers">The set of <see cref="LotteryNumbers"/> to search.</param>
        /// <returns>Null</returns>
        public static LotteryNumbers[][] FindMatchingNumbers(LotteryNumbers[] lotteryNumbers)
        {
            List<LotteryNumbers[]> matched = new List<LotteryNumbers[]>();

            foreach(LotteryNumbers a in lotteryNumbers)
            {
                List<LotteryNumbers> matching = new List<LotteryNumbers>();
                matching.Add(a);

                foreach (LotteryNumbers b in lotteryNumbers)
                {
                    if (a == b || a.drawDate.Date.CompareTo(b.drawDate.Date) == 0) continue;

                    if (a.numbers.SequenceEqual(b.numbers))
                        matching.Add(b);

                }

                if (matching.Count > 1)
                    matched.Add(matching.ToArray());
            }

            if (matched.Count > 0)
                return matched.ToArray();
            else
                return null;

        }
      


    }
}
