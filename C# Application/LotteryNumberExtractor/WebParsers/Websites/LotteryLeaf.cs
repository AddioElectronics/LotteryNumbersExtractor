using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LottoNumbers.Extractor.WebParsers;

namespace LottoNumbers.Extractor.WebParsers.Websites
{
    public class LotteryLeaf : YearParser
    {
        


        #region Fields

        /// <summary>
        /// The base URL used for the GET requests.
        /// </summary>
        const string GET_URL = @"https://www.lotteryleaf.com/%REGION/%LOTTO/%DATE";


        /// <summary>
        /// The current draw date being extracted/parsed.
        /// </summary>
        /// <remarks>This is for adding to error messages.</remarks>
        private DateTime currentDrawDate;

        #endregion

        #region Properties

        internal override Lottery.Lotto[] extractableLotteries => new Lottery.Lotto[] { Lottery.Lotto.BC49, Lottery.Lotto.Lotto649, Lottery.Lotto.LottoMax, Lottery.Lotto.DailyGrand, Lottery.Lotto.Extra };

        public override string websiteURL => "https://www.LotteryLeaf.com";

        public override bool officialWebsite => false;

        /// <summary>
        /// The current state of the parser.
        /// </summary>
        private enum ParsingState
        {
            /// <summary>
            /// Used for describing when a line of HTML contains no data.
            /// </summary>
            Invalid = -1,
            /// <summary>
            /// Parser is  expecting the <see cref="LotteryNumbers.drawDate"/> next.
            /// </summary>
            Date,
            /// <summary>
            /// Parser is expecting the <see cref="LotteryNumbers.numbers"/> next.
            /// </summary>
            Numbers,
            /// <summary>
            /// Parser is  expecting the <see cref="LotteryNumbers.bonus"/> next.
            /// </summary>
            Bonus,
            /// <summary>
            /// Parser is  expecting the <see cref="LotteryNumbers.DrawInfo.jackpot"/> next.
            /// </summary>
            Jackpot
        };

        #endregion


        #region Constructors

        public LotteryLeaf()
        {
            //Will send a GET request every 5 milliseconds, or 200 times a second.
            _getRequestMinimumInterval = 5;
        }

        #endregion

        #region Methods

        internal override string CreateGetRequestURL(Lottery.Lotto lottery, DateTime date)
        {
            string lotto = null;
            string region = null;

            switch (lottery)
            {
                case Lottery.Lotto.BC49:
                    lotto = "bc-49";
                    region = "bc";
                    break;
                case Lottery.Lotto.Lotto649:
                    lotto = "lotto-649";
                    region = "bc";
                    break;
                case Lottery.Lotto.LottoMax:
                    lotto = "lotto-max";
                    region = "ac";
                    break;
                case Lottery.Lotto.DailyGrand:
                    lotto = "daily-grand";
                    region = "bc";
                    break;
                default:
                    throw new NotImplementedException("The Lottery.Lotto is not compatible with this extractor, or has not yet been implemented.");
            }

            return GET_URL.Replace("%LOTTO", lotto).Replace("%REGION", region).Replace("%DATE", date.Year.ToString());
        }

      

        /// <summary>
        /// Strings used in the parsing process for LotteryLeaf.com.
        /// </summary>
        internal static class ParsingStrings
        {
            internal const string htmlclass_date = "win-nbr-dat";
            internal const string htmlclass_numbers = "nbr-grp";
            internal const string htmlclass_jackpot = "win-nbr-jackpot";
            internal static readonly string[] html_dataWrapper = new string[] { "<table>", "</table>" };
            internal static readonly string[] html_datewrapper = new string[] { "<td class=\"win-nbr-date col-sm-3 col-xs-4\">", "</td>" };
            internal static readonly string[] html_numberswrapper = new string[] { "<ul class=\"nbr-grp\">", "<li class=\"sp-ball power\">" };
            internal static readonly string[] html_numberwrapper = new string[] { "<li>", "</li>" };
            internal static readonly string[] html_bonuswrapper = new string[] { "</span>", "</li>" };
            internal static readonly string[] html_jackpotwrapper = new string[] { "&nbsp;&nbsp;", "</td>" };
        }





        /// <summary>
        /// Gets the strings that wrap the data we are looking for.
        /// </summary>
        /// <param name="state">The state which implys what data that is expected.</param>
        /// <returns>The strings that wrap the data in the HTML response.</returns>
        private string[] GetDataWrappers(ParsingState state)
        {
            string[] match = state switch
            {
                ParsingState.Date => ParsingStrings.html_datewrapper,
                ParsingState.Numbers => ParsingStrings.html_numberswrapper,
                ParsingState.Bonus => ParsingStrings.html_bonuswrapper,
                ParsingState.Jackpot => ParsingStrings.html_jackpotwrapper,
                _ => throw new NotImplementedException("Parsing State invalid.")
            };
            return match;
        }

        /// <summary>
        /// Checks a line of text to see if it contains any data.
        /// </summary>
        /// <param name="line">The line of HTML to check.</param>
        /// <param name="ignoringState">Will not return this state. Used for when data for more than one state is on the <paramref name="line"/></param>
        /// <returns>The <see cref="ParsingState"/> related to the data, or <see cref="ParsingState.Invalid"/> if the <paramref name="line"/> did not contain any data we want.</returns>
        private ParsingState CheckLineForState(string line, ParsingState ignoringState = ParsingState.Invalid)
        {
            foreach (ParsingState state in (ParsingState[])Enum.GetValues(typeof(ParsingState)))
            {
                if (state == ParsingState.Invalid || state == ignoringState) continue;

                string match = GetDataWrappers(state)[0];
                if (line.IndexOf(match) != -1)
                    return state;
            }
            return ParsingState.Invalid;
        }


        /// <summary>
        /// Parses HTML for <see cref="LotteryNumbers"/>.
        /// </summary>
        /// <remarks>
        /// Not going to write a full HTML parser, yet.
        /// So for now this cheat will have to do for an example on how to write a YearParser.
        /// </remarks>
        /// <param name="lottery">The lottery you want the numbers for.</param>
        /// <param name="getResponse">The response from the GET request.</param>
        /// <returns>The parsed <see cref="LotteryNumbers"/> array, or null if numbers were unable to be parsed.</returns>
        protected override LotteryNumbers[] ParseNumbers(string getResponse)
        {
            ParsingState state = ParsingState.Date;
            List<LotteryNumbers> parsedNumbers = new List<LotteryNumbers>();

            //Select only the data we want
            int startIndex = getResponse.IndexOf(ParsingStrings.html_dataWrapper[0]);
            int endIndex = getResponse.IndexOf(ParsingStrings.html_dataWrapper[1], startIndex);
            string data = getResponse.Substring(startIndex, endIndex - startIndex);

            //The data split up line by line.
            string[] dataLines = data.Split('\n');

            //Holds the parsed data for each draw.
            //Every draw will create a new instance.
            LotteryNumbers currentNumbers = new LotteryNumbers();

            Regex regex = new Regex(@"(\s|\r|\n)");
            Regex jackpotRegex = new Regex(@"(\s|\r|\n|,)");

            for (int i = 0; i < dataLines.Length; i++)
            {
            RestartLine:
                string line = dataLines[i];

                ///Find out where the data starts for the current <see cref="state"/>.
                string[] dataWrapper = GetDataWrappers(state);
                int dataStart = line.IndexOf(dataWrapper[0]);

                //No data found for the current state.
                if (dataStart == -1)
                {
                    //In the event the website is missing Data for a draw date,
                    //we need to make sure we arent stuck looking for that missing data, while passing over the next draw's data.
                    ParsingState offState = CheckLineForState(line);

                    //No data for any state on this line, continue.
                    if (offState == ParsingState.Invalid) continue;

                    //Found data for a later state, the current draw is missing data.
                    if ((int)offState > (int)state)
                    {
                        //Move on to that state and re-process the line.
                        state = offState;
                        goto RestartLine;
                    }

                    //Found data for an earlier state.
                    //The current draw is missing data, and all we can do is move on to the next draw.
                    if ((int)offState < (int)state)
                    {
                        //Add the data we did manage to parse into the list.
                        //Create new instance for the next draw,
                        //and then restart this line in the new state.
                        parsedNumbers.Add(currentNumbers);
                        currentNumbers = new LotteryNumbers();
                        state = offState;
                        goto RestartLine;
                    }
                }

                //Get the index of where the actual data starts.
                dataStart += dataWrapper[0].Length;
                int dataEnd = line.IndexOf(dataWrapper[1], dataStart);
                string dataString = dataEnd != -1 ? line.Substring(dataStart, dataEnd - dataStart) : line.Substring(dataStart);

                switch (state)
                {
                    case ParsingState.Date:
                        dataString = regex.Replace(dataString, "");
                        DateTime drawDate = DateTime.ParseExact(dataString, dateFormats, null);
                        currentNumbers.drawDate = drawDate;
                        state = ParsingState.Numbers;
                        break;

                    case ParsingState.Numbers:
                        dataStart = 0;
                        int[] numbers = new int[parentLottery.numberRanges.standardPickCount];
                        for (int n = 0; n < numbers.Length; n++)
                        {
                            dataStart = dataString.IndexOf(ParsingStrings.html_numberwrapper[0], dataStart) + ParsingStrings.html_numberwrapper[0].Length;
                            dataEnd = dataString.IndexOf(ParsingStrings.html_numberwrapper[1], dataStart);
                            string numberString = regex.Replace(dataString.Substring(dataStart, dataEnd - dataStart), "");
                            numbers[n] = int.Parse(numberString);
                        }
                        currentNumbers.numbers = numbers;
                        state = ParsingState.Bonus;

                        //Check to see if the bonus data is also included on this line.
                        //If it is, re-process the line.
                        if (CheckLineForState(line, ParsingState.Numbers) != ParsingState.Invalid)
                            goto RestartLine;
                        break;

                    case ParsingState.Bonus:
                        dataString = regex.Replace(dataString, "");
                        int bonus = int.Parse(dataString);
                        currentNumbers.bonus = bonus;
                        state = ParsingState.Jackpot;
                        break;

                    case ParsingState.Jackpot:
                        dataString = jackpotRegex.Replace(dataString, "");
                        decimal jackpot = 0;

                        if (!decimal.TryParse(dataString, out jackpot))
                        {
                            //BC49 only displays jackpot as "2 Million"
                            //If the decimal parse fails we will assume its because of that,
                            //but we will still check anyways.
                            if (dataString.IndexOf("2 Million") != -1)
                            {
                                jackpot = 2_000_000;
                            }
                        }

                        if (extractVerboseDrawInfo && jackpot != 0)
                            currentNumbers.verboseDrawInfo = new LotteryNumbers.DrawInfo { jackpot = jackpot };


                        //We have finished parsing the draw.
                        //Add it to the list, and create a new instance to parse the next draw.
                        state = ParsingState.Date;
                        parsedNumbers.Add(currentNumbers);
                        currentNumbers = new LotteryNumbers();
                        break;
                }

                //Reached the end of the HTML data.
                if (i == dataLines.Length - 1)
                {
                    //We have a partially parsed draw that was unable to finish because of missing data.
                    if (state > ParsingState.Date)
                    {
                        //Add the draw to the list with the data it was able to retrieve.
                        parsedNumbers.Add(currentNumbers);
                    }
                }
            }

            if (parsedNumbers.Count > 0)
                return parsedNumbers.ToArray();
            else
                return null;


            //            while (startIndex != -1)
            //            {
            //#warning Change Replaces to Regex
            //                //Parse Date
            //                startIndex = data.IndexOf(ParsingStrings.html_datewrapper[0], startIndex) + ParsingStrings.html_datewrapper[0].Length;
            //                endIndex = data.IndexOf(ParsingStrings.html_datewrapper[1], startIndex);
            //                string dateString = data.Substring(startIndex, endIndex - startIndex).Replace("\n", "").Replace("\r", "").Replace(" ", "");
            //                DateTime drawDate = DateTime.ParseExact(dateString, dateFormats, null);

            //                //Parse Numbers
            //                int[] numbers = new int[lottery.numberRanges.standardPickCount];
            //                for (int n = 0; n < numbers.Length; n++)
            //                {
            //                    startIndex = data.IndexOf(ParsingStrings.html_numberwrapper[0], startIndex) + ParsingStrings.html_numberwrapper[0].Length;
            //                    endIndex = data.IndexOf(ParsingStrings.html_numberwrapper[1], startIndex);
            //                    string numberString = data.Substring(startIndex, endIndex - startIndex).Replace("\n", "").Replace("\r", "").Replace(" ", "");
            //                    numbers[n] = int.Parse(numberString);
            //                }

            //                //Parse Bonus
            //                startIndex = data.IndexOf(ParsingStrings.html_bonuswrapper[0], startIndex) + ParsingStrings.html_bonuswrapper[0].Length;
            //                endIndex = data.IndexOf(ParsingStrings.html_bonuswrapper[1], startIndex);
            //                string bonusString = data.Substring(startIndex, endIndex - startIndex).Replace("\n", "").Replace("\r", "").Replace(" ", "");
            //                int bonus = int.Parse(bonusString);

            //                //Parse Jackpot
            //                //Seems to only display jackpot for BC49 and Extra,
            //                //and its always the same. So I'm going to cheat and just check for the text.
            //                startIndex = data.IndexOf(ParsingStrings.html_jackpotwrapper[0], startIndex);
            //                endIndex = data.IndexOf(ParsingStrings.html_jackpotwrapper[1], startIndex);
            //                string jackpotString = data.Substring(startIndex, endIndex - startIndex).Replace("\n", "").Replace("\r", "").Replace(" ", "");
            //                decimal jackpot = jackpotString.IndexOf("2 Million") != -1 ? 2_000_000 :
            //                    (jackpotString.IndexOf("500,000") != -1 ? 500_000 : 0);

            //                LotteryNumbers.DrawInfo? verbose = null;
            //                if (extractVerboseDrawInfo && jackpot != 0)
            //                    verbose = new LotteryNumbers.DrawInfo { jackpot = jackpot };

            //                parsedNumbers.Add(new LotteryNumbers(drawDate, numbers, bonus, verbose));

            //                //Start to parse date, and check for end at the same time.
            //                if (data.Length - startIndex < ParsingStrings.html_datewrapper[0].Length)
            //                    startIndex = -1;

            //                //if (parsedNumbers.Count > 52 * lottery.drawDays.Length)
            //                //    throw new Exception("Parsing Failed : Stuck in loop.");

            //            }

            //            if (parsedNumbers.Count > 0)
            //                return parsedNumbers.ToArray();
            //            else
            //                return null;
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
                new DateTime(2001, 9, 1),
                Lottery.LotteryDrawTimes[Lottery.Lotto.BC49],
                Lottery.LotteryDrawDays[Lottery.Lotto.BC49],
                Lottery.LotteryNumberRanges[Lottery.Lotto.BC49],
                new LotteryLeaf()
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
                new DateTime(2001, 10, 3),
               Lottery.LotteryDrawTimes[Lottery.Lotto.Lotto649],
                Lottery.LotteryDrawDays[Lottery.Lotto.Lotto649],
                Lottery.LotteryNumberRanges[Lottery.Lotto.Lotto649],
                new LotteryLeaf()
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
                new LotteryLeaf()
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
                new LotteryLeaf()
                );


            return dailyGrand;
        }

        #endregion

    }
}
