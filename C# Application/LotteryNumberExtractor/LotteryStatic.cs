using LottoNumbers.Extractor.WebParsers;
using LottoNumbers.Extractor.WebParsers.Websites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LottoNumbers.Extractor.Lottery;

namespace LottoNumbers.Extractor
{
    public partial class Lottery
    {

        #region Properties

        /// <summary>
        /// A <see cref="Dictionary{TKey, TValue}"/> containing the <see cref="DrawTime"/>s for the supported default lotteries.
        /// </summary>
        private static Dictionary<Lotto, DrawTime> _lotteryDrawTimes = new Dictionary<Lotto, DrawTime>()
        {
            { Lotto.BC49, new DrawTime(7, 30, -8)},
            { Lotto.Lotto649, new DrawTime(10, 30, -5)},
            { Lotto.LottoMax, new DrawTime(10, 30, -5)},
            { Lotto.DailyGrand, new DrawTime(10, 30, -5)}
        };

        /// <inheritdoc cref="_lotteryDrawTimes"/>
        public static Dictionary<Lotto, DrawTime> LotteryDrawTimes
        {
            get { return _lotteryDrawTimes; }
        }


        /// <summary>
        /// A <see cref="Dictionary{TKey, TValue}"/> containing the <see cref="DayOfWeek"/>s for the supported default lotteries.
        /// </summary>
        private static Dictionary<Lotto, DayOfWeek[]> _lotteryDrawDays = new Dictionary<Lotto, DayOfWeek[]>()
        {
            { Lotto.BC49, new DayOfWeek[]{ DayOfWeek.Wednesday, DayOfWeek.Saturday } },
            { Lotto.Lotto649, new DayOfWeek[]{ DayOfWeek.Wednesday, DayOfWeek.Saturday }},
            { Lotto.LottoMax, new DayOfWeek[]{ DayOfWeek.Thursday, DayOfWeek.Friday }},
            { Lotto.DailyGrand, new DayOfWeek[]{ DayOfWeek.Monday, DayOfWeek.Thursday }}
        };

        /// <inheritdoc cref="_lotteryDrawDays"/>
        public static Dictionary<Lotto, DayOfWeek[]> LotteryDrawDays
        {
            get { return _lotteryDrawDays; }
        }

        /// <summary>
        /// A <see cref="Dictionary{TKey, TValue}"/> containing the <see cref="LotteryNumberRanges"/>s for the supported default lotteries.
        /// </summary>
        private static Dictionary<Lotto, LotteryNumberRanges> _lotteryNumberRanges = new Dictionary<Lotto, LotteryNumberRanges>()
        {
            { Lotto.BC49, new LotteryNumberRanges(Lotto.BC49, 1, 49, 6, 1, 99, true)},
            { Lotto.Lotto649, new LotteryNumberRanges(Lotto.BC49, 1, 49, 6, 1, 99, true)},
            { Lotto.LottoMax, new LotteryNumberRanges(Lotto.BC49, 1, 49, 6, 1, 99, true)},
            { Lotto.DailyGrand, new LotteryNumberRanges(Lotto.BC49, 1, 49, 6, 1, 99, true)}
        };

        /// <inheritdoc cref="_lotteryNumberRanges"/>
        public static Dictionary<Lotto, LotteryNumberRanges> LotteryNumberRanges
        {
            get { return _lotteryNumberRanges; }
        }

        #endregion

        #region Methods

       

        #endregion

    }
}
