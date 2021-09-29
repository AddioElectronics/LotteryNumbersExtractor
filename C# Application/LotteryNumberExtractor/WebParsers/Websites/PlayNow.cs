using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LottoNumbers.Extractor.WebParsers;

namespace LottoNumbers.Extractor.WebParsers.Websites
{

    /// <summary>
    /// The extractor which gets lottery numbers from PlayNow.com.
    /// </summary>
    public class PlayNow : DayParser
    {


        #region Fields

        /// <summary>
        /// The base URL used for the GET requests.
        /// </summary>
        const string GET_URL = @"https://www.playnow.com/services2/lotto/draw/%LOTTO/%DATE";


        #endregion

        #region Properties

        
        internal override Lottery.Lotto[] extractableLotteries =>  new Lottery.Lotto[] { Lottery.Lotto.BC49, Lottery.Lotto.Lotto649, Lottery.Lotto.LottoMax, Lottery.Lotto.DailyGrand };

        public override string websiteURL => "https://www.Playnow.com";

        public override bool officialWebsite => true;


        #endregion


        #region Constructors

        public PlayNow()
        {
            //Will send a GET request every 5 milliseconds, or 200 times a second.
            _getRequestMinimumInterval = 5;
        }

        #endregion

        #region Methods

        internal override string CreateGetRequestURL(Lottery.Lotto lottery, DateTime date)
        {
            string lotto = null;


            switch (lottery)
            {
                case Lottery.Lotto.BC49:
                case Lottery.Lotto.BC49 | Lottery.Lotto.Extra:
                    lotto = "bc49";
                    break;
                case Lottery.Lotto.Lotto649:
                case Lottery.Lotto.Lotto649 | Lottery.Lotto.Extra:
                    lotto = "six49";
                    break;
                case Lottery.Lotto.LottoMax:
                case Lottery.Lotto.LottoMax | Lottery.Lotto.Extra:
                    lotto = "lmax";
                    break;
                case Lottery.Lotto.DailyGrand:
                case Lottery.Lotto.DailyGrand | Lottery.Lotto.Extra:
                    lotto = "dgrd";
                    break;
                default:
                    throw new NotImplementedException("The Lottery.Lotto is not compatible with this extractor, or has not yet been implemented.");
            }

            return GET_URL.Replace("%LOTTO", lotto).Replace("%DATE", String.Format("{0:0000}-{1:00}-{2:00}", date.Year, date.Month, date.Day));
        }


        protected override LotteryNumbers? ParseNumbers(string getResponse)
        {
            RequestData requestData;
            try
            {
                requestData = JsonSerializer.Deserialize<RequestData>(getResponse);
            }
            catch (Exception ex)
            {
                string message = ex.Message;

                WebsiteParserEventArgs args = new WebsiteParserEventArgs { errorMessage = "Unable to Parse Extracted Json Data : Please Report Error! | Draw Date : "+ currentDrawDate.ToShortDateString() + " | Exception Message : " + message };
                RaiseEventError(args);
                return null;
            }

            LotteryNumbers lotteryNumbers = new LotteryNumbers();
            lotteryNumbers.drawDate = DateTime.ParseExact(requestData.drawDate, dateFormats, System.Globalization.CultureInfo.InvariantCulture);
            lotteryNumbers.drawNumber = requestData.drawNbr;
            lotteryNumbers.numbers = requestData.drawNbrs;
            lotteryNumbers.bonus = requestData.bonusNbr;
            lotteryNumbers.extra = requestData.extraNbrs;

            if (extractVerboseDrawInfo)
            {

                LotteryNumbers.DrawInfo drawInfo = new LotteryNumbers.DrawInfo();
                LotteryNumbers.DrawInfo.PrizeInfo[] prizeInfo = null;
                LotteryNumbers.DrawInfo.PrizeInfo[] extraInfo = null;

                if (requestData.gameBreakdown != null)
                {
                    drawInfo.jackpot = requestData.gameBreakdown.First(x => x.desc == "6/6").prizeAmount;

                    prizeInfo = drawInfo.prizeInfo = new LotteryNumbers.DrawInfo.PrizeInfo[requestData.gameBreakdown.Length];
                    

                    for (int i = 0; i < requestData.gameBreakdown.Length; i++)
                    {
                        prizeInfo[i].prizeIndex = requestData.gameBreakdown[i].prizeDiv;
                        int.TryParse(requestData.gameBreakdown[i].abbrev[0].ToString(), out prizeInfo[i].matchingNumbers);
                        prizeInfo[i].bonusRequired = requestData.gameBreakdown[i].abbrev.IndexOf('+') != -1;
                        prizeInfo[i].prizeAmount = requestData.gameBreakdown[i].prizeAmount;
                        prizeInfo[i].prizeDescription = requestData.gameBreakdown[i].desc;
                        prizeInfo[i].winnerCount = requestData.gameBreakdown[i].winnersTotal;

                        if (requestData.gameBreakdown[i].location != null)
                            prizeInfo[i].winnerLocation = requestData.gameBreakdown[i].location;
                    }
                }

                if (requestData.extraBreakdown != null)
                {

                    extraInfo = drawInfo.extraPrizeInfo = new LotteryNumbers.DrawInfo.PrizeInfo[requestData.extraBreakdown.Length];

                    for (int i = 0; i < requestData.extraBreakdown.Length; i++)
                    {
                        extraInfo[i].prizeIndex = requestData.extraBreakdown[i].prizeDiv;
                        int.TryParse(requestData.extraBreakdown[i].abbrev[0].ToString(), out extraInfo[i].matchingNumbers);
                        extraInfo[i].bonusRequired = requestData.extraBreakdown[i].abbrev.IndexOf('+') != -1;
                        extraInfo[i].prizeAmount = requestData.extraBreakdown[i].prizeAmount;
                        extraInfo[i].prizeDescription = requestData.extraBreakdown[i].desc;
                        extraInfo[i].winnerCount = requestData.extraBreakdown[i].winnersTotal;

                        if (requestData.extraBreakdown[i].location != null)
                            extraInfo[i].winnerLocation = requestData.extraBreakdown[i].location;
                    }

                }

                if (prizeInfo != null || extraInfo != null)
                    lotteryNumbers.verboseDrawInfo = drawInfo;

            }

            return lotteryNumbers;
        }


        #endregion

        #region Static Methods

        /// <summary>
        /// Creates a <see cref="Lottery"/> instance setup for extracting BC49 numbers from PlayNow.com
        /// </summary>
        /// <returns>Returns a <see cref="Lottery"/> instance for extracting BC49 numbers from PlayNow.com</returns>
        public static Lottery CreateBC49Instance()
        {
            Lottery bc49 = new Lottery(
                Lottery.Lotto.BC49,
                new DateTime(1992, 1, 29),
                Lottery.LotteryDrawTimes[Lottery.Lotto.BC49],
                Lottery.LotteryDrawDays[Lottery.Lotto.BC49],
                Lottery.LotteryNumberRanges[Lottery.Lotto.BC49],
                new PlayNow()
                );

            return bc49;
        }

        /// <summary>
        /// Creates a <see cref="Lottery"/> instance setup for extracting Lotto649 numbers from PlayNow.com
        /// </summary>
        /// <returns>Returns a <see cref="Lottery"/> instance for extracting Lotto649 numbers from PlayNow.com</returns>
        public static Lottery CreateLotto649Instance()
        {
            Lottery lotto649 = new Lottery(
                Lottery.Lotto.Lotto649,
                new DateTime(1988, 10, 24),
               Lottery.LotteryDrawTimes[Lottery.Lotto.Lotto649],
                Lottery.LotteryDrawDays[Lottery.Lotto.Lotto649],
                Lottery.LotteryNumberRanges[Lottery.Lotto.Lotto649],
                new PlayNow()
                );


            return lotto649;
        }

        /// <summary>
        /// Creates a <see cref="Lottery"/> instance setup for extracting LottoMax numbers from PlayNow.com
        /// </summary>
        /// <returns>Returns a <see cref="Lottery"/> instance for extracting LottoMax numbers from PlayNow.com</returns>
        public static Lottery CreateLottoMaxInstance()
        {
            Lottery lottoMax = new Lottery(
                Lottery.Lotto.LottoMax,
                new DateTime(2009, 9, 25),
               Lottery.LotteryDrawTimes[Lottery.Lotto.LottoMax],
                Lottery.LotteryDrawDays[Lottery.Lotto.LottoMax],
                Lottery.LotteryNumberRanges[Lottery.Lotto.LottoMax],
                new PlayNow()
                );


            return lottoMax;
        }

        /// <summary>
        /// Creates a <see cref="Lottery"/> instance setup for extracting DailyGrand numbers from PlayNow.com
        /// </summary>
        /// <returns>Returns a <see cref="Lottery"/> instance for extracting DailyGrand numbers from PlayNow.com</returns>
        public static Lottery CreateDailyGrandInstance()
        {
            Lottery dailyGrand = new Lottery(
                Lottery.Lotto.DailyGrand,
                new DateTime(2016, 10, 20),
                Lottery.LotteryDrawTimes[Lottery.Lotto.DailyGrand],
                Lottery.LotteryDrawDays[Lottery.Lotto.DailyGrand],
                Lottery.LotteryNumberRanges[Lottery.Lotto.DailyGrand],
                new PlayNow()
                );


            return dailyGrand;
        }

        #endregion


        /// <summary>
        /// The Response data from the GET Request, as a class for deserialization.
        /// </summary>
        /// <remarks>Some value types may be incorrect, and could possibly cause an exception while deserializing. 
        /// In the Response I looked at, they were null, just need to look at more draw dates to find out what they really are.</remarks>
        [Serializable]
        public class RequestData
        {

            /// <summary>
            /// The index of the draw.
            /// </summary>
            public int drawNbr { get; set; }

            /// <summary>
            /// The date of the draw.
            /// </summary>
            public string drawDate { get; set; }

            /// <summary>
            /// The numbers that were drawn.
            /// </summary>
            public int[] drawNbrs{ get; set; }

            /// <summary>
            /// The bomus number.
            /// </summary>
            public int bonusNbr { get; set; }

            /// <summary>
            /// The Extra numbers that were drawn.
            /// </summary>
            public int[] extraNbrs{ get; set; }

            /// <summary>
            /// If there is a Max Millions in play. I think?
            /// </summary>
            public int maxMillionPending { get; set; }

            /// <summary>
            /// The prize index. 1 = Jackpot
            /// </summary>
            public int prizeDiv{ get; set; }
            public int seqNbr{ get; set; }
            public int[] bonusDraws{ get; set; }               //Not sure the type, maybe int array?
            public string bonusDrawDetails{ get; set; }        //Not sure the type, maybe string?
            public Breakdown[] gameBreakdown{ get; set; }
            public Breakdown[] extraBreakdown{ get; set; }
            public int[] gpNumbers{ get; set; }                //Not sure the type,  maybe int array?
            public int[] gpAdditionalNumbers{ get; set; }      //Not sure the type,  maybe int array?

            public class Breakdown
            {
                public int prodCD{ get; set; }
                public int drawNbr{ get; set; }
                public string drawDate{ get; set; }
                public int prizeDiv{ get; set; }
                public int seqNbr{ get; set; }
                public string abbrev{ get; set; }
                public string desc{ get; set; }
                public string prizeType{ get; set; }
                public int winnersTotal{ get; set; }
                public decimal prizeAmount{ get; set; }
                public int winnersCount{ get; set; }
                public string location{ get; set; }
                public string annuityDetails { get; set; }  //Not sure the type, maybe string?
            }

        }
    }
}
