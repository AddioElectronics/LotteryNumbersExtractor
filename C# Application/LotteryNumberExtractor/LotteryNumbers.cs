using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Addio.Universal.NET.Standard.Extensions;
using LottoNumbers.CSV;

namespace LottoNumbers
{
    /// <summary>
    /// A structure which holds the Draw Date, Numbers, Bonus, and Extra Numbers.
    /// </summary>
    [Serializable]
    public struct LotteryNumbers
    {
        /// <summary>
        /// The formats available to export Lottery Numbers as, and import from.
        /// </summary>
        public enum FileFormat { JSON, XML, CSV };

        #region Properties

        /// <summary>
        /// The index of the draw, from the lottery's perspective.
        /// </summary>
        /// <remarks>If data is not available, set to -1.</remarks>
        public int drawNumber { get; set; }

        /// <summary>
        /// The date these numbers were extracted from.
        /// </summary>
        public DateTime drawDate { get; set; }

        /// <summary>
        /// The standard numbers.
        /// </summary>
        public int[] numbers { get; set; }

        /// <summary>
        /// The Bonus number.
        /// </summary>
        public int bonus { get; set; }

        /// <summary>
        /// The Extra numbers, if they are displayed with the numbers.
        /// </summary>
        public int[] extra { get; set; }


        /// <summary>
        /// Extra info about the draw that most websites will not contain.
        /// </summary>
        public DrawInfo? verboseDrawInfo { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance for a website which displays the draw date, numbers w/bonus, extra numbers, draw index, and jackpot.
        /// </summary>
        /// <param name="drawDate">The date these numbers were extracted from.</param>
        /// <param name="numbers">The standard numbers.</param>
        /// <param name="bonus">The Bonus number.</param>
        /// <param name="extra">The Extra numbers.</param>
        /// <param name="drawNumber">The index of the draw, from the lottery's perspective.</param>
        /// <param name="jackpot">The top prize for the draw.</param>
        /// <param name="verboseDrawInfo">Extra information about the draw that most websites will not have.</param>
        public LotteryNumbers(DateTime drawDate, int[] numbers, int bonus, int[] extra, int drawNumber = -1, DrawInfo? verboseDrawInfo = null)
        {
            this.drawDate = drawDate;
            this.numbers = numbers;
            this.bonus = bonus;
            this.extra = extra;
            this.drawNumber = drawNumber;
            this.verboseDrawInfo = verboseDrawInfo;
        }

        /// <summary>
        /// Creates an instance for a website that only displays the draw date, numbers w/bonus, and total jackpot.
        /// </summary>
        /// <param name="drawDate">The date these numbers were extracted from.</param>
        /// <param name="numbers">The standard numbers.</param>
        /// <param name="bonus">The Bonus number.</param>
        /// <param name="jackpot">The top prize for the draw.</param>
        /// <param name="verboseDrawInfo">Extra information about the draw that most websites will not have.</param>
        public LotteryNumbers(DateTime drawDate, int[] numbers, int bonus, DrawInfo? verboseDrawInfo = null)
        {
            this.drawDate = drawDate;
            this.numbers = numbers;
            this.bonus = bonus;
            this.extra = null;
            this.drawNumber = -1;
            this.verboseDrawInfo = verboseDrawInfo;
        }

        #endregion

        #region Operators


        public static bool operator ==(LotteryNumbers left, LotteryNumbers right)
        {
            return left.drawDate.CompareTo(right.drawDate.Date) == 0 &&
                left.numbers == right.numbers &&
                left.bonus == right.bonus &&
                left.extra == right.extra;
        }

        public static bool operator !=(LotteryNumbers left, LotteryNumbers right)
        {
            return left.drawDate.CompareTo(right.drawDate.Date) != 0 ||
                left.numbers != right.numbers ||
                left.bonus != right.bonus ||
                left.extra != right.extra;
        }
        #endregion

        #region Methods

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Gets all <see cref="LotteryNumbers"/> that has a draw date inbetween or equal to <paramref name="start"/> and <paramref name="stop"/>.
        /// </summary>
        /// <param name="lotteryNumbers">The lottery numbers to search.</param>
        /// <param name="start">The earliest date to get.</param>
        /// <param name="stop">The latest date to get.</param>
        /// <returns>An array of <see cref="LotteryNumbers"/> selected from <paramref name="lotteryNumbers"/>.</returns>
        public static LotteryNumbers[] GetRange(LotteryNumbers[] lotteryNumbers, DateTime start, DateTime stop)
        {
            List<LotteryNumbers> newRange = new List<LotteryNumbers>();

            foreach (LotteryNumbers ln in lotteryNumbers)
            {
                if (ln.drawDate.Date.CompareTo(start.Date) >= 0 && ln.drawDate.Date.CompareTo(stop.Date) <= 0)
                    newRange.Add(ln);
            }

            return newRange.ToArray();
        }

        /// <summary>
        /// Sorts an array of <see cref="LotteryNumbers"/> from oldest <see cref="DateTime"/> to newest <see cref="DateTime"/>.
        /// </summary>
        /// <param name="lotteryNumbers">The <see cref="LotteryNumbers"/> array to sort.</param>
        public static void SortLotteryNumbers(LotteryNumbers[] lotteryNumbers)
        {
            Array.Sort(lotteryNumbers, (x, y) => x.drawDate.Date.CompareTo(y.drawDate.Date));
        }

        /// <summary>
        /// Sorts a list of <see cref="LotteryNumbers"/> from oldest <see cref="DateTime"/> to newest <see cref="DateTime"/>.
        /// </summary>
        /// <param name="lotteryNumbers">The <see cref="LotteryNumbers"/> list to sort.</param>
        public static void SortLotteryNumbers(List<LotteryNumbers> lotteryNumbers)
        {
            lotteryNumbers.Sort((x, y) => x.drawDate.Date.CompareTo(y.drawDate.Date));
        }

        /// <summary>
        /// Combines 2 <see cref="LotteryNumbers"/>.
        /// </summary>
        ///  <remarks>
        /// Sometimes websites are missing draws, and so someone may want to run different <see cref="WebParsers.WebsiteParser"/>s for the same lottery, and combine the results later. 
        /// In the event their results contain the same <see cref="LotteryNumbers.drawDate"/>, you only want to keep one, but different websites have different amounts of info.
        /// So you instead of randomly choosing which one to keep and possibly throwing out data, we combine them so no data goes missing.
        /// </remarks>
        /// <returns>A new <see cref="LotteryNumbers"/> instance filled with values from <paramref name="left"/> and <paramref name="right"/>. Returns null if they do not have a matching <see cref="LotteryNumbers.drawDate"/>.</returns>
        public static LotteryNumbers? Combine(LotteryNumbers left, LotteryNumbers right)
        {

            if (left.drawDate.Date.CompareTo(right.drawDate.Date) != 0)
                return null;

            LotteryNumbers combined = left;

            if (right.extra != null)
                if ((combined.extra == null || combined.extra.Length == 0) && right.extra.Length > 0)
                    combined.extra = right.extra;

            if (right.extra != null)
                if (combined.verboseDrawInfo == null)
                    combined.verboseDrawInfo = right.verboseDrawInfo;

            if (right.verboseDrawInfo != null)
                if (left.verboseDrawInfo == null)
                    combined.verboseDrawInfo = right.verboseDrawInfo;
                else
                    combined.verboseDrawInfo = DrawInfo.Combine(left.verboseDrawInfo.Value, right.verboseDrawInfo.Value);

            return combined;
        }

        /// <summary>
        /// Converts a <see cref="LotteryNumbers"/> array into a <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="lotteryNumbers">The array of <see cref="LotteryNumbers"/>.</param>
        /// <returns>The <paramref name="lotteryNumbers"/> as a <see cref="Dictionary{TKey, TValue}"/>.</returns>
        public static Dictionary<DateTime, LotteryNumbers> AddToDictionary(LotteryNumbers[] lotteryNumbers)
        {
            Dictionary<DateTime, LotteryNumbers> numbers = new Dictionary<DateTime, LotteryNumbers>();
            SortLotteryNumbers(lotteryNumbers);
            Array.ForEach<LotteryNumbers>(lotteryNumbers, (n) =>
            {
                if (!numbers.ContainsKey(n.drawDate.Date))
                    numbers.Add(n.drawDate.Date, n);
                else
                {
                    //Contained numbers with same date.
                    //Try to fill in any missing data.
                    if (numbers[n.drawDate.Date] != n)
                        numbers[n.drawDate.Date] = Combine(numbers[n.drawDate.Date], n).Value;
                }
            });
            return numbers;
        }

        /// <summary>
        /// Converts a <see cref="LotteryNumbers"/> array into a <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="lotteryNumbers">The array of <see cref="LotteryNumbers"/>.</param>
        /// <returns>The <paramref name="lotteryNumbers"/> as a <see cref="Dictionary{TKey, TValue}"/>.</returns>
        public static Dictionary<DateTime, LotteryNumbers> AddToDictionary(List<LotteryNumbers> lotteryNumbers)
        {
            return AddToDictionary(lotteryNumbers.ToArray());
        }

        /// <summary>
        /// Adds a <see cref="LotteryNumbers"/> array into <paramref name="addTo"/>.
        /// </summary>
        /// <param name="lotteryNumbers">The array of <see cref="LotteryNumbers"/>.</param>
        /// <param name="addTo">The <see cref="Dictionary{TKey, TValue}"/> to add <paramref name="lotteryNumbers"/> to.</param>
        public static void AddToDictionary(LotteryNumbers[] lotteryNumbers, ref Dictionary<DateTime, LotteryNumbers> addTo)
        {
            List<LotteryNumbers> sortList = addTo.Values.ToList();
            sortList.AddRange(lotteryNumbers);
            addTo.Clear();
            foreach (LotteryNumbers ln in sortList)
            {
                //Already in the dictionary
                if (addTo.ContainsKey(ln.drawDate.Date))
                {
                    //Try and fill in missing data if any.
                    if (addTo[ln.drawDate.Date] != ln)
                        addTo[ln.drawDate.Date] = Combine(addTo[ln.drawDate.Date], ln).Value;
                    continue;
                }

                addTo.Add(ln.drawDate.Date, ln);
            }
        }

        /// <summary>
        /// Exports <see cref="LotteryNumbers"/> into a text file.
        /// Determines the <see cref="FileFormat"/> from the <paramref name="filepath"/> extension.
        /// If it can not be determined, it will default to <see cref="FileFormat.CSV"/>.
        /// </summary>
        /// <param name="numbers">The array of lotterys numbers.</param>
        /// <param name="filepath">The path of the file.</param>
        public static void ExportNumbers(LotteryNumbers[] numbers, string filepath)
        {
            FileFormat format = filepath.Split('.').Last().ToLower() switch
            {
                "csv" => FileFormat.CSV,
                "xml" => FileFormat.XML,
                "json" => FileFormat.JSON,
                _ => FileFormat.CSV
            };

            ExportNumbers(numbers, filepath, format);
        }

        /// <summary>
        /// Exports <see cref="LotteryNumbers"/> into a text file in the selected <paramref name="format"/>.
        /// </summary>
        /// <param name="numbers">The array of lotterys numbers.</param>
        /// <param name="filepath">The path of the file.</param>
        /// <param name="format">The format to export as. CSV, JSON or XML.</param>
        public static void ExportNumbers(LotteryNumbers[] numbers, string filepath, FileFormat format)
        {
            SortLotteryNumbers(numbers);

            string[] split = filepath.Split('.');

            string fileContents = format switch
            {
                FileFormat.JSON => JsonSerializer.Serialize(numbers),
                FileFormat.CSV => CsvSerializer.Serialize(numbers, new CsvSerializerOptions.Serializing { datetimeFormat = CsvSerializerOptions.Serializing.DateTimeFormat.BasicDate, separator = CsvSerializerOptions.Separator.Space, headerContainsPropertyNames = true }),
                FileFormat.XML => new Func<string>(() =>
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(LotteryNumbers[]));
                    using (TextWriter writer = new StringWriter())
                    {
                        serializer.Serialize(writer, numbers);
                        return writer.ToString();
                    }
                })(),
                _ => throw new FormatException("Cannot Serialize LotteryNumbers : Unsupported format.")
            };

            File.WriteAllText(filepath, fileContents);
        }

        /// <summary>
        /// Imports <see cref="LotteryNumbers"/> from a local file.
        /// Determines the format by its extension.
        /// </summary>
        /// <param name="filepath">The path of the file.</param>
        /// <returns><see cref="LotteryNumbers"/> that were parsed from <paramref name="filepath"/>.</returns>
        public static LotteryNumbers[] ImportNumbers(string filepath)
        {

            FileFormat format = filepath.Split('.').Last().ToLower() switch
            {
                "csv" => FileFormat.CSV,
                "xml" => FileFormat.XML,
                "json" => FileFormat.JSON,
                _ => throw new FormatException("Invalid extension, was not able to determine format.")
            };

            return ImportNumbers(filepath, format);
        }

        /// <summary>
        /// Imports <see cref="LotteryNumbers"/> from a local file.
        /// </summary>
        /// <param name="filepath">The path of the file.</param>
        /// <param name="format">The format of the file. CSV, JSON or XML.</param>
        /// <returns><see cref="LotteryNumbers"/> that were parsed from <paramref name="filepath"/>.</returns>
        public static LotteryNumbers[] ImportNumbers(string filepath, FileFormat format)
        {
            string filecontents = File.ReadAllText(filepath);

            return format switch
            {
                FileFormat.CSV => CsvSerializer.Deserialize<LotteryNumbers>(filecontents),
                FileFormat.JSON => JsonSerializer.Deserialize<LotteryNumbers[]>(filecontents),
                FileFormat.XML => new Func<LotteryNumbers[]>(() =>
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(LotteryNumbers[]));
                    using (TextReader reader = new StringReader(filecontents))
                    {
                        return (LotteryNumbers[])deserializer.Deserialize(reader);
                    }
                })(),
                _ => throw new NotImplementedException("This will never be thrown, but lets get rid of the warning.")
            };
        }


        #endregion



        /// <summary>
        /// Extra information about the draw that most websites will not contain.
        /// </summary>
        public struct DrawInfo
        {

            /// <summary>
            /// The top prize.
            /// </summary>
            public decimal jackpot;

            /// <summary>
            /// Information about each prize for the draw.
            /// </summary>
            public PrizeInfo[] prizeInfo;

            /// <summary>
            /// Information the Extra prizes for the draw, if Extra was included.
            /// </summary>
            public PrizeInfo[] extraPrizeInfo;


            /// <summary>
            /// Combines 2 <see cref="DrawInfo"/>.
            /// </summary>
            /// <remarks>
            /// Sometimes websites are missing draws, and so someone may want to run different <see cref="WebParsers.WebsiteParser"/>s for the same lottery, and combine the results later. 
            /// In the event their results contain the same <see cref="LotteryNumbers.drawDate"/>, you only want to keep one, but different websites have different amounts of info.
            /// So you instead of randomly choosing which one to keep and possibly throwing out data, we combine them so no data goes missing.
            /// </remarks>
            /// <returns>A new <see cref="DrawInfo"/> instance filled with values from <paramref name="left"/> and <paramref name="right"/>.</returns>
            public static DrawInfo Combine(DrawInfo first, DrawInfo last)
            {
                DrawInfo combined = first;

                if (first.jackpot <= 0)
                    combined.jackpot = last.jackpot;

                if (first.prizeInfo == null)
                    combined.prizeInfo = last.prizeInfo;
                else if (first.prizeInfo != null && last.prizeInfo != null && !first.prizeInfo.SequenceEqualNullable(last.prizeInfo))
                {
                    //I hate this code.

                    PrizeInfo[] merging;
                    if (first.prizeInfo.Length > last.prizeInfo.Length)
                    {
                        combined.prizeInfo = first.prizeInfo;
                        merging = last.prizeInfo;
                    }
                    else
                    {
                        combined.prizeInfo = last.prizeInfo;
                        merging = first.prizeInfo;
                    }

                    for (int i = 0; i < combined.prizeInfo.Length; i++)
                    {
                        PrizeInfo secondary = merging.First(x => x.prizeIndex == combined.prizeInfo[i].prizeIndex);
                        combined.prizeInfo[i] = PrizeInfo.Combine(combined.prizeInfo[i], secondary);
                    }
                }

                if (first.extraPrizeInfo == null)
                    combined.extraPrizeInfo = last.extraPrizeInfo;

                return combined;
            }

            #region Operators

            public static bool operator ==(DrawInfo left, DrawInfo right)
            {
                return left.jackpot == right.jackpot &&
                    left.prizeInfo.SequenceEqualNullable(right.prizeInfo) &&
                    left.extraPrizeInfo.SequenceEqualNullable(right.extraPrizeInfo);

            }

            public static bool operator !=(DrawInfo left, DrawInfo right)
            {
                return left.jackpot != right.jackpot ||
                     !left.prizeInfo.SequenceEqualNullable(right.prizeInfo) ||
                     !left.extraPrizeInfo.SequenceEqualNullable(right.extraPrizeInfo);
            }

            #endregion

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public struct PrizeInfo
            {
                /// <summary>
                /// The index of the prize.
                /// </summary>
                public int prizeIndex;

                /// <summary>
                /// How many numbers out of <see cref="LotteryNumberRanges.standardPickCount"/> are needed to win.
                /// </summary>
                public int matchingNumbers;

                /// <summary>
                /// Is the bonus required for this prize?
                /// </summary>
                public bool bonusRequired;

                /// <summary>
                /// How much money is the winning prize.
                /// </summary>
                public decimal prizeAmount;

                /// <summary>
                /// How many winners won this prize.
                /// </summary>
                public int winnerCount;

                /// <summary>
                /// Description
                /// </summary>
                public string prizeDescription;

                /// <summary>
                /// Where is the winner located?
                /// </summary>
                public string winnerLocation;


                /// <summary>
                /// Combines 2 <see cref="PrizeInfo"/>.
                /// </summary>
                /// <remarks>
                /// Sometimes websites are missing draws, and so someone may want to run different <see cref="WebParsers.WebsiteParser"/>s for the same lottery, and combine the results later. 
                /// In the event their results contain the same <see cref="LotteryNumbers.drawDate"/>, you only want to keep one, but different websites have different amounts of info.
                /// So you instead of randomly choosing which one to keep and possibly throwing out data, we combine them so no data goes missing.
                /// </remarks>
                /// <returns>A new <see cref="PrizeInfo"/> instance filled with values from <paramref name="left"/> and <paramref name="right"/>.</returns>
                public static PrizeInfo Combine(PrizeInfo first, PrizeInfo last)
                {
                    PrizeInfo combined = first;

                    if (first.prizeIndex <= 0)
                        combined.prizeIndex = last.prizeIndex;

                    if (first.matchingNumbers <= 0)
                        combined.matchingNumbers = last.matchingNumbers;

                    if (first.bonusRequired == false)
                        combined.bonusRequired = last.bonusRequired;

                    if (first.prizeAmount <= 0)
                        combined.prizeAmount = last.prizeAmount;

                    if (first.winnerCount <= 0)
                        combined.winnerCount = last.winnerCount;

                    if (first.prizeDescription == null)
                        combined.prizeDescription = last.prizeDescription;

                    if (first.winnerLocation == null)
                        combined.winnerLocation = last.winnerLocation;

                    return combined;
                }



                #region Operators

                public static bool operator ==(PrizeInfo left, PrizeInfo right)
                {
                    return left.prizeIndex == right.prizeIndex &&
                        left.matchingNumbers == right.matchingNumbers &&
                        left.bonusRequired == right.bonusRequired &&
                        left.prizeAmount == right.prizeAmount &&
                        left.winnerCount == right.winnerCount &&
                        left.prizeDescription == right.prizeDescription &&
                        left.winnerLocation == right.winnerLocation;
                }

                public static bool operator !=(PrizeInfo left, PrizeInfo right)
                {
                    return left.prizeIndex != right.prizeIndex ||
                         left.matchingNumbers != right.matchingNumbers ||
                         left.bonusRequired != right.bonusRequired ||
                         left.prizeAmount != right.prizeAmount ||
                         left.winnerCount != right.winnerCount ||
                         left.prizeDescription != right.prizeDescription ||
                         left.winnerLocation != right.winnerLocation;
                }

                #endregion

                public override bool Equals(object obj)
                {
                    return base.Equals(obj);
                }

                public override int GetHashCode()
                {
                    return base.GetHashCode();
                }


            }

        }


    }
}






