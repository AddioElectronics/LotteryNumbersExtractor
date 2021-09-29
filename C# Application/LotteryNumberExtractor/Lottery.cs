using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Threading.Tasks;

using LottoNumbers.Extractor.WebParsers;
using System.Threading;

namespace LottoNumbers.Extractor
{

    /// <summary>
    /// The class which handles choosing draw <see cref="DateTime"/>s for a specific <see cref="Lottery"/>/<see cref="Lottery.Lotto"/>, controlling the <see cref="WebsiteParser"/> and holding its results.
    /// </summary>
    [Serializable]
    public partial class Lottery
    {

        #region Events

        /// <summary>
        /// The event handler for <see cref="Lottery"/>.
        /// </summary>
        /// <param name="args">Used to pass the event data.</param>
        public delegate void LotteryEventHandler(LotteryEventArgs args);

        /// <summary>
        /// The event for retrieving status messages, and progress.
        /// The message can be retrieved from 
        /// </summary>
        public event LotteryEventHandler Status;

        /// <summary>
        /// The event for retrieving warning messages.
        /// The error message can be retrieved from <see cref="LotteryEventArgs.warningMessage"/>.
        /// </summary>
        public event LotteryEventHandler Warning;

        /// <summary>
        /// The event for retrieving error messages from the extraction process.
        /// The error message can be retrieved from <see cref="LotteryEventArgs.errorMessage"/>.
        /// </summary>
        public event LotteryEventHandler Error;

        /// <summary>
        /// The event for retrieving <see cref="LotteryNumbers"/>.
        /// The numbers can be retrieved from <see cref="LotteryEventArgs.lotteryNumbers"/>.
        /// </summary>
        public event LotteryEventHandler ExtractionComplete;


        #endregion

        #region Fields

        #endregion

        #region Properties

        /// <summary>
        /// The lottery.
        /// </summary>
        [Flags]
        public enum Lotto { Extra = 1, BC49 = 2, Lotto649 = 4, LottoMax = 8, DailyGrand = 16  };

        /// <summary>
        /// The status for the <see cref="Lottery"/>'s extraction.
        /// </summary>
        public enum ExtractionStatus { 
            /// <summary>
            /// Lottery is waiting to start an extraction.
            /// </summary>
            Idle, 
            /// <summary>
            /// Lottery is currently executing an extraction.
            /// </summary>
            Running, 
            /// <summary>
            /// Lottery has recently finished an extraction.
            /// </summary>
            Completed,
            /// <summary>
            /// Lottery extraction failed the last extraction.
            /// </summary>
            Faulted
        };

        /// <summary>
        /// The lottery.
        /// </summary>
        public Lotto lotto { get; set; }

        /// <summary>
        /// The name of the instance used for differentiating files for importing and exporting.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// The first recorded draw date on the website.
        /// </summary>
        public DateTime firstDrawDate { get; set; }

        /// <summary>
        /// The time of day the draw results are posted.
        /// </summary>
        public DrawTime drawTime { get; set; }

        /// <summary>
        /// The days of the week the draws occur on.
        /// </summary>
        public DayOfWeek[] drawDays { get; set; }


        /// <inheritdoc cref="ExtractionStatus"/>
        public ExtractionStatus extractionStatus { get; set; }


        /// <summary>
        /// Lock for <see cref="_lotteryNumbers"/>.
        /// </summary>
        private object _lotteryNumbersLock = new object();

        /// <inheritdoc cref="lotteryNumbers"/>
        private Dictionary<DateTime, LotteryNumbers> _lotteryNumbers = new Dictionary<DateTime, LotteryNumbers>();

        /// <summary>
        /// The extracted lottery numbers.
        /// </summary>
        public Dictionary<DateTime,LotteryNumbers> lotteryNumbers
        {
            get
            {
                //bool lockTaken = false;
                Monitor.Enter(_lotteryNumbersLock);

                //if (lockTaken == false)
                //    return null;

                Dictionary<DateTime, LotteryNumbers> ln = _lotteryNumbers;

                Monitor.Exit(_lotteryNumbersLock);

                //if (extractionStatus == ExtractionStatus.Completed)
                //    extractionStatus = ExtractionStatus.Idle;

                return _lotteryNumbers;
            }
            internal set
            {
                //bool lockTaken = false;
                Monitor.Enter(_lotteryNumbersLock);

                //if (lockTaken == false)
                //{
                //    Monitor.Exit(_lotteryNumbers);
                //    throw new Exception("Cannot set lotteryNumbers, lock was unable to be aquired.");
                //}

                _lotteryNumbers = value;

                Monitor.Exit(_lotteryNumbersLock);
            }
        }

        /// <summary>
        /// Safely sets <see cref="lotteryNumbers"/> to <paramref name="lotteryNumbers"/>.
        /// </summary>
        /// <param name="lotteryNumbers"></param>
        /// <returns>True if <see cref="lotteryNumbers"/> were set, or false if the lock was unable to be aquired.</returns>
        public bool SetLotteryNumbers(Dictionary<DateTime, LotteryNumbers> lotteryNumbers)
        {
            try
            {
                this.lotteryNumbers = lotteryNumbers;
                return true;
            }
            catch
            {
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, errorMessage = "lotteryNumbers was not set | Lock was unable to be aquired." };
                Error?.Invoke(args);
                return false;
            }
        }

        /// <summary>
        /// A list containing dates of draws that were unable to be extracted.
        /// They may be missing from the website, or something in the extraction process may have gone wrong.
        /// </summary>
        public List<DateTime> missingDrawDates = new List<DateTime>();


        /// <summary>
        /// The minimum and maximum numbers for each number set in the lottery.
        /// </summary>
        public LotteryNumberRanges numberRanges { get; set; }

        /// <summary>
        /// The extraction interface which handles grabbing the numbers from a website.
        /// </summary>
        private WebsiteParser _extractor;

        public WebsiteParser extractor
        {
            get
            {
                return _extractor;
            }
            set
            {
                if(_extractor != null)
                {
                    _extractor.Status -= PropagateExtractorStatusEvent;
                    _extractor.Warning -= PropagateExtractorWarningEvent;
                    _extractor.Error -= PropagateExtractorErrorEvent;
                    _extractor.parentLottery = null;
                }

                if (value.parentLottery != null && value.parentLottery != this)
                {
                    value.parentLottery.extractor = null;
                    LotteryEventArgs args = new LotteryEventArgs { lottery = this, warningMessage = "Lottery.extractor set to an extractor that is currently being used by another Lottery. The other Lottery's extractor property was set to null." };
                    Warning?.Invoke(args);
                }

                _extractor = value;

                _extractor.Status += PropagateExtractorStatusEvent;
                _extractor.Warning += PropagateExtractorWarningEvent;
                _extractor.Error += PropagateExtractorErrorEvent;
            }
        }

        /// <summary>
        /// The date to start extracting numbers from.
        /// </summary>
        private DateTime _startDrawDate;

        /// <inheritdoc cref="_startDrawDate">
        public DateTime startDrawDate
        {
            get
            {
                return _startDrawDate;
            }
            set
            {
                DateTime drawDate = value;

                if (!IsDrawDay(drawDate))
                {
                    drawDate = GetNextDrawDay(drawDate);
                    LotteryEventArgs args = new LotteryEventArgs { lottery = this, warningMessage = "Set startDrawDate : The date is not on valid draw day. startDrawDate has been adjusted to the next day of the week a draw is held on.", drawDate = drawDate };
                    Warning?.Invoke(args);
                }

                if (firstDrawDate.Date.CompareTo(value.Date) > 0)
                {
                    _startDrawDate = firstDrawDate;
                    LotteryEventArgs args = new LotteryEventArgs { lottery = this, warningMessage = "Set startDrawDate : The date is before the first valid draw date (firstDrawDate). startDrawDate has been adjusted to firstDrawDate.", drawDate = _startDrawDate };
                    Warning?.Invoke(args);
                }
                else
                {
                    _startDrawDate = value;
                }
            }
        }

        /// <summary>
        /// The date to stop the extraction at.
        /// </summary>
        private DateTime _stopDrawDate;

        /// <inheritdoc cref="_stopDrawDate">
        public DateTime stopDrawDate
        {
            get
            {
                return _stopDrawDate;
            }
            set
            {
                DateTime drawDate = value;
                if (!IsDrawDay(drawDate))
                {
                    drawDate = GetPreviousDrawDay(value);
                    LotteryEventArgs args = new LotteryEventArgs { lottery = this, warningMessage = "Set stopDrawDate : The date is not on valid draw day. stopDrawDate has been adjusted to the previous day of the week a draw is held on.", drawDate = drawDate };
                    Warning?.Invoke(args);
                }

                if (HasDrawHappenedYet(drawDate) == true)
                {
                    _stopDrawDate = value;
                }
                else
                {
                    _stopDrawDate = GetMostRecentDrawDate();
                    LotteryEventArgs args = new LotteryEventArgs { lottery = this, warningMessage = "Set stopDrawDate : The draw on the date is still pending. stopDrawDate has been adjusted to the most recent date a draw has happened.", drawDate = _stopDrawDate };
                    Warning?.Invoke(args);
                }
            }
        }

        /// <summary>
        /// Gets the oldest <see cref="LotteryNumbers"/> in <see cref="lotteryNumbers"/>.
        /// </summary>
        public LotteryNumbers? OldestNumbers
        {
            get
            {
                if (lotteryNumbers.Count == 0) return null;

                LotteryNumbers oldest = lotteryNumbers.Values.First();

                foreach(LotteryNumbers n in lotteryNumbers.Values)
                {
                    if (n.drawDate.Date.CompareTo(oldest.drawDate.Date) >= 0)
                        oldest = n;
                }

                return oldest;
            }
        }

        /// <summary>
        /// Gets the oldest <see cref="LotteryNumbers"/> in <see cref="lotteryNumbers"/>.
        /// </summary>
        public LotteryNumbers? EarliestNumbers
        {
            get
            {
                if (lotteryNumbers.Count == 0) return null;

                LotteryNumbers earliest = lotteryNumbers.Values.First();

                foreach (LotteryNumbers n in lotteryNumbers.Values)
                {
                    if (n.drawDate.Date.CompareTo(earliest.drawDate.Date) <= 0)
                        earliest = n;
                }

                return earliest;
            }
        }

        /// <summary>
        /// Gets the <see cref="DateTime"/> of the oldest <see cref="LotteryNumbers"/> in <see cref="lotteryNumbers"/>.
        /// </summary>
        public DateTime? OldestNumbersDate
        {
            get
            {
                LotteryNumbers? oldest = OldestNumbers;
                if (oldest == null) return null;
                return oldest.Value.drawDate.Date;
            }
        }

        /// <summary>
        /// Gets the <see cref="DateTime"/> of the youngest <see cref="LotteryNumbers"/> in <see cref="lotteryNumbers"/>.
        /// </summary>
        public DateTime? EarliestNumbersDate
        {
            get
            {
                LotteryNumbers? youngest = EarliestNumbers;
                if (youngest == null) return null;
                return youngest.Value.drawDate.Date;
            }
        }

        /// <summary>
        /// Gets the total amount of draws since the <see cref="firstDrawDate"/> until today.
        /// </summary>
        /// <remarks>May not be accurate! Has not been confirmed.</remarks>
        public int TotalDraws
        {
            get
            {
                return GetDrawCount(firstDrawDate, LotteryTimeZoneNow);
            }
        }

      

        /// <summary>
        /// Converts <see cref="DateTime.UtcNow"/> to the Lottery's timezone.
        /// </summary>
        public DateTime LotteryTimeZoneNow
        {
            get
            {
                return DateTime.UtcNow.AddHours(drawTime.timezoneOffset);
            }
        }

        #endregion

        #region Constructors


        /// <summary>
        /// Stops a blank constructor from being able to be used.
        /// </summary>
        private Lottery() { }


        /// <summary>
        /// Initialize a lottery.
        /// </summary>
        /// <param name="lotto">The lottery.</param>
        /// <param name="firstDate">The date of the first draw that is recorded on the website.</param>
        /// <param name="drawTime">The time the lottery posts the draw results at.</param>
        /// <param name="drawDays">The days of the week the draws are done on.</param>
        /// <param name="parser">The extractor handles grabbing data from a specific website.</param>
        /// <param name="startDate">The date you want to start grabbing draw records from. If null, the <paramref name="firstDate"/> will be used.</param>
        /// <param name="stopDate">The date you want to stop grabbing draw records at. If null, today will be used.</param>
        public Lottery(Lotto lotto, DateTime firstDate, DrawTime drawTime, DayOfWeek[] drawDays, LotteryNumberRanges numberRanges, WebsiteParser parser, DateTime? startDate = null,  DateTime? stopDate = null)
        {

            if (parser == null)
                throw new ArgumentNullException("Failed to Initialize Lottery : WebsiteParser cannot be null");

            this.lotto = lotto;
            this.firstDrawDate = firstDate;
            this.drawTime = drawTime;
            this.drawDays = drawDays;
            this.numberRanges = numberRanges;
            this.extractor = parser;

            this.extractor.parentLottery = this;

            if(startDate == null)
            {
                this.startDrawDate = firstDate;
            }
            
            if(stopDate == null)
            {
                this.stopDrawDate = LotteryTimeZoneNow;
            }

            if (this.firstDrawDate.CompareTo(this.startDrawDate) > 0)
                throw new ArgumentOutOfRangeException("The startDate is earlier than the firstDate");

            if (this.startDrawDate.CompareTo(this.stopDrawDate) > 0)
                throw new ArgumentOutOfRangeException("The stopDate is earlier than the startDate");

            if (!parser.extractableLotteries.Contains(lotto))
                throw new ArgumentException("The Lotto is not compatible with the Extractor.");

        }

        ~Lottery()
        {
            if (_extractor != null)
            {
                _extractor.Status -= PropagateExtractorStatusEvent;
                _extractor.Warning -= PropagateExtractorWarningEvent;
                _extractor.Error -= PropagateExtractorErrorEvent;
                _extractor.parentLottery = null;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the total amount of draws from the <paramref name="start"/> until the <paramref name="stop"/>.
        /// </summary>
        /// <param name="start">The date to start counting from.</param>
        /// <param name="stop">The date to count to.</param>
        /// <returns>The total draw count from <paramref name="start"/> to <paramref name="stop"/>.</returns>
        public int GetDrawCount(DateTime start, DateTime stop)
        {

            if (firstDrawDate.CompareTo(start) > 0)
            {
                start = firstDrawDate;
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, errorMessage = "GetExtractionCount(DateTime start, DateTime stop) : The start date is before the first valid draw date (firstDrawDate). Start has been adjusted." };
                Error?.Invoke(args);
            }

            TimeSpan elapsed = stop.Subtract(start);
            int weeks = elapsed.Days / 7;
            int daysLeft = elapsed.Days % 7;
            int totalDraws = weeks * drawDays.Length;

            //Because ints always round down, we don't know if there are draw dates in the remainder.
            //So the rest of the code checks to see if there are any draws in the remainder.
            //There must be a better way to do this no?
            DateTime startWeek = start.Date.AddDays(weeks * 7);
            DateTime endWeek = startWeek.AddDays(daysLeft);

            while (startWeek.CompareTo(endWeek) <= 0)
            {
                if (drawDays.Any(x => x == startWeek.DayOfWeek))
                    totalDraws++;

                startWeek = startWeek.AddDays(1);
            }

            return totalDraws;
        }

        #region Draw Date Methods



        /// <summary>
        /// Checks to see if the draw results have been posted for a particular date.
        /// </summary>
        /// <param name="drawdate">The date to check.</param>
        /// <returns>If the draw date and time have passed the draw time. Returns null if <paramref name="drawdate"/> is not a day draws happen.</returns>
        public bool? HasDrawHappenedYet(DateTime drawdate)
        {

            if(!IsDrawDay(drawdate))
            {
                //drawdate is not a day the draws happen.
                return null;
            }

            DateTime nowAdjusted = LotteryTimeZoneNow;

            if (drawdate.Year < nowAdjusted.Year)
                return true;
            else if (drawdate.Year > nowAdjusted.Year)
                return false;

            if (drawdate.Month < nowAdjusted.Month)
                return true;
            else if (drawdate.Month > nowAdjusted.Month)
                return false;

            if (drawdate.Day < nowAdjusted.Day)
                return true;
            else if (drawdate.Day > nowAdjusted.Day)
                return false;

            if (drawTime.hour < nowAdjusted.Hour)
                return true;
            else if (drawTime.hour > nowAdjusted.Hour)
                return false;

            if (drawTime.minute <= nowAdjusted.Minute)
                return true;

            return false;
        }

        /// <summary>
        /// Checks to see if <paramref name="date"/> is on a day of the week a draw happens.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>Returns true if the <paramref name="date"/> is on a day a draw happens.</returns>
        public bool IsDrawDay(DateTime date)
        {
            return drawDays.Any(x => x == date.DayOfWeek);
        }



        /// <summary>
        /// Gets the draw date previous to UTC's current time, adjusted to the timezone of the lottery. 
        /// </summary>
        /// <returns>The most recent draw date.</returns>
        public DateTime GetMostRecentDrawDate()
        {
            return GetPreviousDrawDay(LotteryTimeZoneNow);
        }


        /// <summary>
        /// Gets the draw date previous to <paramref name="date"/>.
        /// </summary>
        /// <param name="date">Find the draw date previous to this.</param>
        /// <param name="getSameDay">If true, and the <paramref name="date"/> is on a draw date, it will return <paramref name="date"/>.</param>
        /// <returns>The draw date previous to <paramref name="date"/>.  Or if <paramref name="date"/> is on a draw date, and <paramref name="getSameDay"/> is true, it will just return that date.</returns>
        public DateTime GetPreviousDrawDay(DateTime date, bool getSameDay = false)
        {

            if (IsDrawDay(date))
            {
                if (getSameDay)
                {
                    return date;
                }
                else
                {
                    //Makes sure the code below doesnt return the same date.
                    date = date.AddDays(-1);
                }
            }

            if (!drawDays.Contains(date.DayOfWeek))
            {
                //Make sure date is on a draw day, and correft if not.
                int index = drawDays.Length - 1;
                for (int i = drawDays.Length - 1; i >= 0; i--)
                {
                    if ((int)date.DayOfWeek > (int)drawDays[i])
                    {
                        index = i;
                        break;
                    }
                }

                //Closest draw day is days away
                int days = (int)drawDays[index] - (int)date.DayOfWeek;
                if (days > 0) days -= 7;

                date = date.AddDays(days);
            }

            return date;
        }

        /// <summary>
        /// Gets the next draw date after UTC's current time, adjusted to the timezone of the lottery. 
        /// </summary>
        /// <returns>The next upcoming draw date.</returns>
        public DateTime? GetNexUpcomingtDrawDate()
        {
            return GetNextDrawDay(LotteryTimeZoneNow);
        }

        /// <summary>
        /// Gets the draw date after to <paramref name="date"/>.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="getSameDay">If true, and the <paramref name="date"/> is on a draw date, it will return <paramref name="date"/>.</param>
        /// <returns>The next draw date after <paramref name="date"/>. Or if <paramref name="date"/> is on a draw date, and <paramref name="getSameDay"/> is true, it will just return that date.</returns>
        public DateTime GetNextDrawDay(DateTime date, bool getSameDay = false)
        {

            if (IsDrawDay(date))
            {
                if (getSameDay)
                {
                    return date;
                }
                else
                {
                    //Makes sure the code below doesnt return the same date.
                    date = date.AddDays(1);
                }
            }


            if (!drawDays.Contains(date.DayOfWeek))
            {
                //Make sure date is on a draw day, and correft if not.
                int index = 0;
                for (int i = 0; i < drawDays.Length; i++)
                {
                    if ((int)date.DayOfWeek < (int)drawDays[i])
                    {
                        index = i;
                        break;
                    }
                }

                //Closest draw day is days away
                int days = (int)drawDays[index] - (int)date.DayOfWeek;
                if (days < 0) days += 7;

                date = date.AddDays(days);
            }

            return date;
        }


        /// <summary>
        /// Gets the most recent draw date that has happened before <paramref name="date"/>.
        /// </summary>
        /// <param name="date">Find the most recent draw date previous to this.</param>
        /// <param name="getSameDay">If true, and the <paramref name="date"/> is on a draw date, it will return <paramref name="date"/>.</param>
        /// <returns>The most recent draw date that has happened.</returns>
        public DateTime GetPreviosDrawDateThatHappened(DateTime date, bool getSameDay = false)
        {
            date = GetPreviousDrawDay(date);

            if (!HasDrawHappenedYet(date).Value)
                return GetPreviousDrawDay(LotteryTimeZoneNow, getSameDay);
            else
                return date;
        }

        #endregion

        #region Synchronous Methods

        /// <summary>
        /// Extracts all lottery numbers after the <see cref="firstDrawDate"/> until <see cref="DateTime.Now"/>.
        /// Sets <see cref="lotteryNumbers"/> to the extraction result.
        /// Adds the results to <see cref="lotteryNumbers"/>, and passes them to the <see cref="ExtractionComplete"/> event with <see cref="LotteryEventArgs.lotteryNumbers"/>.
        /// </summary>
        /// <param name="stopCompleteEvent">Stops the <see cref="ExtractionComplete"/> event from being fired. Allows async methods to fire the event when the <see cref="Task{TResult}"/> is considered complete.</param>
        /// <returns>Returns <see cref="lotteryNumbers"/> after they are set to the extraction result.</returns>
        public Dictionary<DateTime, LotteryNumbers> GetAllLotteryNumbers(bool stopCompleteEvent = false)
        {
            return GetAllLotteryNumbersFrom(firstDrawDate, stopCompleteEvent);
        }

        /// <summary>
        /// Extracts all lottery numbers from today back to <paramref name="firstDate"/>.
        /// Adds the <see cref="LotteryNumbers"/> in <see cref="Extraction"/> to <see cref="lotteryNumbers"/> and passes it to <see cref="ExtractionComplete"/> via <see cref="WebsiteParserEventArgs.extraction"/>.
        /// </summary>
        /// <param name="firstDate">The draw date to start extracting from.</param>
        /// <param name="name">An optional name to differentiate the extractions.</param>
        /// <param name="stopCompleteEvent">Stops the <see cref="ExtractionComplete"/> event from being fired. Allows async methods to fire the event when the <see cref="Task{TResult}"/> is considered complete.</param>
        /// <returns>Returns <see cref="lotteryNumbers"/> after they are set to the extraction result.</returns>
        public Dictionary<DateTime, LotteryNumbers> GetAllLotteryNumbersFrom(DateTime firstDate, bool stopCompleteEvent = false)
        {
            return GetAllLotteryNumbersInRange(firstDate, LotteryTimeZoneNow, stopCompleteEvent);
        }

        /// <summary>
        /// Extracts all lottery numbers from <see cref="WebsiteInfo.firstAvailableDraws"/> to <paramref name="endDate"/>.
        /// Adds the <see cref="LotteryNumbers"/> in <see cref="Extraction"/> to <see cref="lotteryNumbers"/> and passes it to <see cref="ExtractionComplete"/> via <see cref="WebsiteParserEventArgs.extraction"/>.
        /// </summary>
        /// <param name="endDate">The draw date to stop extracting on.</param>
        /// <param name="name">An optional name to differentiate the extractions.</param>
        /// <param name="stopCompleteEvent">Stops the <see cref="ExtractionComplete"/> event from being fired. Allows async methods to fire the event when the <see cref="Task{TResult}"/> is considered complete.</param>
        /// <returns>Returns <see cref="lotteryNumbers"/> after they are set to the extraction result.</returns>
        public Dictionary<DateTime, LotteryNumbers> GetAllLotteryNumbersTo(DateTime endDate, bool stopCompleteEvent = false)
        {
            return GetAllLotteryNumbersInRange(startDrawDate, endDate, stopCompleteEvent);
        }

        /// <summary>
        /// Extracts lottery numbers from <see cref="startDrawDate"/> until <see cref="stopDrawDate"/>.
        /// Adds the results to <see cref="lotteryNumbers"/>, and passes them to the <see cref="ExtractionComplete"/> event with <see cref="LotteryEventArgs.lotteryNumbers"/>.
        /// </summary>
        /// <param name="stopCompleteEvent">Stops the <see cref="ExtractionComplete"/> event from being fired. Allows async methods to fire the event when the <see cref="Task{TResult}"/> is considered complete.</param>
        /// <returns>Returns <see cref="lotteryNumbers"/> after they are set to the extraction result.</returns>
        public Dictionary<DateTime, LotteryNumbers> GetAllLotteryNumbersInRange(bool stopCompleteEvent = false)
        {
            return GetAllLotteryNumbersInRange(startDrawDate, stopDrawDate, stopCompleteEvent);
        }

        /// <summary>
        /// Extracts lottery numbers from <paramref name="firstDate"/> until <paramref name="lastDate"/>.
        /// Adds the results to <see cref="lotteryNumbers"/>, and passes them to the <see cref="ExtractionComplete"/> event with <see cref="LotteryEventArgs.lotteryNumbers"/>.
        /// </summary>
        /// <param name="firstDate">The draw date to start extracting from.</param>
        /// <param name="lastDate">The draw date to end the extraction on.</param>
        /// <param name="stopCompleteEvent">Stops the <see cref="ExtractionComplete"/> event from being fired. Allows async methods to fire the event when the <see cref="Task{TResult}"/> is considered complete.</param>
        /// <returns>Returns <see cref="lotteryNumbers"/> after they are set to the extraction result.</returns>
        public Dictionary<DateTime, LotteryNumbers> GetAllLotteryNumbersInRange(DateTime firstDate, DateTime lastDate, bool stopCompleteEvent = false)
        {
            extractionStatus = ExtractionStatus.Running;
            Dictionary<DateTime, LotteryNumbers> numbersList = new Dictionary<DateTime, LotteryNumbers>();
            DateTime currentDate = lastDate;
            int consecutiveFails = 0;

            int totalExtractionCount = 0;              
            int currentExtractionCount = 0;
            int failCount = 0;

            //Initialize method for type of extractor.
            if (extractor.GetType().IsSubclassOf(typeof(DayParser)))
            {
                //Make sure we start on a draw day.
                currentDate = GetPreviosDrawDateThatHappened(currentDate, true);

                //Make sure the start date isn't earlier than the first recorded date.
                if (firstDrawDate.Date.CompareTo(currentDate.Date) > 0)
                    firstDate = firstDrawDate.Date;
                
                totalExtractionCount = GetDrawCount(firstDate, lastDate);
            }
            else if (extractor.GetType().IsSubclassOf(typeof(YearParser)))
            {
                //Make sure the first date isn't earlier than the first recorded year.
                if (firstDrawDate.Year > firstDate.Year)
                    firstDate = new DateTime(firstDrawDate.Year, 1, 1);

                //Make sure the last date isn't later than the current year.
                if (LotteryTimeZoneNow.Year < currentDate.Year)
                    currentDate = new DateTime(LotteryTimeZoneNow.Year, 1, 1);

                totalExtractionCount = currentDate.Year - firstDate.Year;
            }

            //Log that the extraction has started.
            {
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, statusMessage = "Extraction Status : Started", extractionProgress = 0 };
                Status?.Invoke(args);
            }

            //If we have multiple continous fails, it is probably because we are going past the first draw date recorded on the website.
            //This will make sure we stay in the while loop, and capture the date.
            bool capturingFirstDrawDate = false;

            //Grab every draw date until we reach the stop date.
            do
            {
                if (extractor.GetType().IsSubclassOf(typeof(DayParser)))
                {
                    //Extract Numbers day at a time.

                    if (!IsDrawDay(currentDate))
                    {
                        extractionStatus = ExtractionStatus.Faulted;
                        throw new ArgumentException("CurrentDate is not ona valid draw day.");
                    }

                    //Make sure we don't try to extract draws from the future.
                    if (!HasDrawHappenedYet(currentDate).Value)
                        break;

                    LotteryNumbers? numbers = GetLotteryNumbers(currentDate);

                    if (numbers.HasValue)
                    {
                        //Extraction success
                        if (!capturingFirstDrawDate)
                            numbersList.Add(currentDate.Date, numbers.Value);

                        LotteryEventArgs args = new LotteryEventArgs { lottery = this, statusMessage = "Extraction Status : Extracted Draw from " + currentDate.ToString("MMM, dd, yyyy"), drawDate = currentDate.Date, extractionProgress = ((double)currentExtractionCount + 1d) / (double)totalExtractionCount };
                        Status?.Invoke(args);
                        consecutiveFails = 0;
                    }
                    else
                    {
                        //Extraction failed.
                        if (!capturingFirstDrawDate)
                            missingDrawDates.Add(currentDate);

                        LotteryEventArgs args = new LotteryEventArgs { lottery = this, errorMessage = "Extraction Failed : Unable to extract LotteryNumbers for draw date " + currentDate.ToString("MMM, dd, yyyy"), statusMessage = "Extraction Failed : Unable to extract LotteryNumbers for draw date " + currentDate.ToString("MMM, dd, yyyy"), drawDate = currentDate.Date, extractionProgress = ((double)currentExtractionCount + 1d) / (double)totalExtractionCount };
                        Error?.Invoke(args);
                        Status?.Invoke(args);
                        failCount++;
                        consecutiveFails++;
                    }

                    currentDate = GetPreviousDrawDay(currentDate);
                }
                else if (extractor.GetType().IsSubclassOf(typeof(YearParser)))
                {
                    //Extract numbers 1 year at a time.

                    //Make sure we don't try to extract draws from the future.
                    if (currentDate.Year > LotteryTimeZoneNow.Year)
                        break;

                    LotteryNumbers[] yearsNumbers = ((YearParser)extractor).ExtractNumbers(lotto, currentDate);
                    if (yearsNumbers != null)
                    {
                        //Extraction success
                        LotteryNumbers.AddToDictionary(yearsNumbers, ref numbersList);
                        LotteryEventArgs args = new LotteryEventArgs { lottery = this, statusMessage = "Extraction Status : Extracted Draws from year " + currentDate.Year, extractionProgress = ((double)currentExtractionCount + 1d) / (double)totalExtractionCount, drawDate = currentDate.Date };
                        Status?.Invoke(args);
                    }
                    else
                    {
                        //Extraction failed.
                        LotteryEventArgs args = new LotteryEventArgs { lottery = this, errorMessage = "Extraction Failed : Unable to extract LotteryNumbers for the year " + currentDate.Year, drawDate = currentDate.Date };
                        Error?.Invoke(args);
                        failCount++;
                    }

                    currentDate = currentDate.AddYears(-1);
                }

                if (currentDate.CompareTo(firstDate) >= 0 && consecutiveFails > 0 && firstDrawDate == default(DateTime) && !capturingFirstDrawDate)
                {
                    capturingFirstDrawDate = true;
                }

                currentExtractionCount++;

            } while ((currentDate.CompareTo(firstDate) >= 0 && consecutiveFails < 5) || (consecutiveFails != 0 && consecutiveFails < 5 && capturingFirstDrawDate));


            //if (lockTaken)
            //{
            //    LotteryNumbers.AddToDictionary(numbersList.Values.ToArray(), ref _lotteryNumbers);
            //    Monitor.Exit(_lotteryNumbersLock);
            //}
            //else
            //{
            //    LotteryEventArgs args = new LotteryEventArgs { lottery = this, errorMessage = "Unable to add Completed Extraction to lotteryNumbers | Lock was unable to be aquired.", lotteryNumbers = numbersList };
            //    Error?.Invoke(args);
            //}

            //If the firstAvailableDraw was default, that means the first draw was never set.
            //If there was multiple consecutive fails during this extraction, that quite possibly means we have reached the earliest draw recorded on the website.
            //So if both of these are met, we can set the earliest draw.

            if (consecutiveFails > 4 && (currentDate.CompareTo(firstDate) >= 0 || capturingFirstDrawDate))
            {
                //Come back and clean this up.
                if (!extractor.ConfirmWebsiteOnline())
                    if (firstDrawDate == default(DateTime) && EarliestNumbersDate != null)
                    {
                        if (firstDrawDate.Date.CompareTo(EarliestNumbersDate.Value.Date) < 0)
                        {
                            LotteryEventArgs args = new LotteryEventArgs { lottery = this, warningMessage = "Extraction stopped due to multiple consecutive failed GET Requests. firstDrawDate was a default DateTime, and so it was set to the exctractions earliest draw date.", lotteryNumbers = numbersList };
                            Warning?.Invoke(args);

                            firstDrawDate = EarliestNumbersDate.Value;
                        }
                    }

                extractionStatus = ExtractionStatus.Faulted;
                return null;
            }

            //Add extracted numbers to the dictionary.
            //bool lockTaken = false;
            Monitor.Enter(_lotteryNumbersLock);

            LotteryNumbers.AddToDictionary(numbersList.Values.ToArray(), ref _lotteryNumbers);
            Monitor.Exit(_lotteryNumbersLock);

            extractionStatus = ExtractionStatus.Completed;

            //Log events
            {
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, statusMessage = "Extraction Status : Complete", extractionProgress = 1 };
                Status?.Invoke(args);
            }

            if(!stopCompleteEvent)
            {
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, statusMessage = "Extraction Complete", lotteryNumbers = numbersList, extractionProgress = 1 };
                ExtractionComplete?.Invoke(args);
            }


            return  numbersList;
        }




        /// <summary>
        /// Gets the lottery numbers for a particular date.
        /// </summary>
        /// <param name="date">The date of the draw.</param>
        /// <param name="nextIfInvalid">If true, and if the <paramref name="date"/> is not a draw day. Should we throw an error?</param>
        /// <returns>False if it failed to start the extraction. Null if the draw date is invalid. True if the <see cref="extractor"/> has accepted the task.</returns>
        public LotteryNumbers? GetLotteryNumbers(DateTime date, bool nextIfInvalid = false)
        {

            if (!IsDrawDay(date))
            {
                if (nextIfInvalid)
                {
                    date = GetNextDrawDay(date);

                    bool? drawHappened = HasDrawHappenedYet(date);

                    if (!drawHappened.HasValue || !drawHappened.Value)
                    {
                        LotteryEventArgs args = new LotteryEventArgs { lottery = this, errorMessage = "Cannot get lottery numbers for the date of " + date.ToString("MMM, dd, yyyy") + ". A draw did not happen on that day, or the results have not been posted yet.", drawDate = date.Date };
                        Error?.Invoke(args);
                        return null;
                    }
                }
                else
                {
                    LotteryEventArgs args = new LotteryEventArgs { lottery = this, errorMessage = "Cannot get lottery numbers for the date of " + date.ToString("MMM, dd, yyyy") + ". A draw did not happen on that day.", drawDate = date.Date };
                    Error?.Invoke(args);
                    return null; //throw new ArgumentException("date is not on a draw date.");
                }
            }

            //Before we send a get reuqest, lets check to see if the numbers for this date have already been extracted.
            LotteryNumbers? previouslyExtracted = FindNumbersFromPreviousExtraction(date);
            if (previouslyExtracted.HasValue)
                return previouslyExtracted;


            if (extractor.GetType().IsSubclassOf(typeof(DayParser)))
            {
                return ((DayParser)extractor).ExtractNumbers(lotto, date);

            }
            else if (extractor.GetType().IsSubclassOf(typeof(YearParser)))
            {
                //Get all lottery numbers for the year, then find the matching date in the list, and return.
                if (lotteryNumbers == null || lotteryNumbers.Count == 0 || !lotteryNumbers.ContainsKey(date.Date))
                    GetAllLotteryNumbersInRange(date, date.Date.AddDays(1));

                LotteryNumbers number = lotteryNumbers[date.Date];

                if (number == default(LotteryNumbers))
                {
                    LotteryEventArgs args = new LotteryEventArgs { lottery = this, errorMessage = "Unable to get LotteryNumbers for the date of " + date.Date.ToString("MMM, dd, yyyy"), drawDate = date.Date };
                    Error?.Invoke(args);
                    return null;
                }
                else
                    return number;
            }


            throw new NotImplementedException("The selected extractor has not been implemented.");
        }

        /// <summary>
        /// Gets the most recent draw.
        /// </summary>
        /// <returns>The most recent draw, or null if the GET request failed.</returns>
        public LotteryNumbers? GetRecentLotteryNumbers()
        {
            DateTime lastDraw = GetMostRecentDrawDate();

            return GetLotteryNumbers(lastDraw);
        }

        /// <summary>
        /// Searches <see cref="lotteryNumbers"/> for <see cref="LotteryNumbers"/> that were drawn on <paramref name="date"/>.
        /// </summary>
        /// <param name="date">The draw date of the numbers to find.</param>
        /// <returns>The <see cref="LotteryNumbers"/> which happened on <paramref name="date"/>, or NULL if they did not exist in <see cref="lotteryNumbers"/>.</returns>
        public LotteryNumbers? FindNumbersFromPreviousExtraction(DateTime date)
        {

            //bool lockTaken = false;
            Monitor.Enter(_lotteryNumbersLock);

            //if (lockTaken == false)
            //    return null;



            //No lottery numbers have been extracted or imported.
            if (_lotteryNumbers == null || _lotteryNumbers.Count == 0)
            {
                Monitor.Exit(_lotteryNumbersLock);
                return null;
            }

            //Check to see if date is on a draw day, and if it has happened yet.
            if (IsDrawDay(date))
            {
                if (!HasDrawHappenedYet(date).Value)
                {
                    Monitor.Exit(_lotteryNumbersLock);
                    return null;
                }
            }
            else
            {
                Monitor.Exit(_lotteryNumbersLock);
                return null;
            }


            if (_lotteryNumbers.ContainsKey(date.Date)){
                //It does exist in the list, grab it and return it.
                LotteryNumbers ln = _lotteryNumbers[date.Date];
                Monitor.Exit(_lotteryNumbersLock);
                return ln;
            }
            else
            {
                Monitor.Exit(_lotteryNumbersLock);
                //No previously extracted numbers happened on the date.
                return null;
            }
        }

        #endregion

        #region Asynchronous Methods

        /// <summary>
        /// Creates a <see cref="Task"/> which calls <see cref="GetAllLotteryNumbers"/> on another thread.
        /// The <see cref="Task"/> extracts all lottery numbers after the <see cref="firstDrawDate"/> until today.
        /// Sets <see cref="lotteryNumbers"/> to the extraction result.
        /// Adds the results to <see cref="lotteryNumbers"/>, <see cref="Task{TResult}"/>, and passes them to the <see cref="ExtractionComplete"/> event with <see cref="LotteryEventArgs.lotteryNumbers"/>.
        /// </summary>
        /// <returns>Returns the <see cref="Task"/>, awaiting to be started.</returns>
        public Task<Dictionary<DateTime, LotteryNumbers>> GetAllLotteryNumbersAsyncTask()
        {
            Task<Dictionary<DateTime, LotteryNumbers>> task = new Task<Dictionary<DateTime, LotteryNumbers>>(() => { return GetAllLotteryNumbers(true); });
            AddExtractionCompleteEventToAsyncTask(task);
            return task;
        }

        /// <summary>
        /// Creates a <see cref="Task"/> which calls <see cref="GetAllLotteryNumbersFrom(DateTime)"/>, with <paramref name="startDate"/>, on another thread.
        /// The <see cref="Task"/> extracts all lottery numbers past a particular date until today.
        /// Sets <see cref="lotteryNumbers"/> to the extraction result.
        /// Adds the results to <see cref="lotteryNumbers"/>, <see cref="Task{TResult}"/>, and passes them to the <see cref="ExtractionComplete"/> event with <see cref="LotteryEventArgs.lotteryNumbers"/>.
        /// </summary>
        /// <param name="startDate">The draw date to start extracting from.</param>
        /// <returns>Returns the <see cref="Task"/>, awaiting to be started.</returns>
        public Task<Dictionary<DateTime, LotteryNumbers>> GetAllLotteryNumbersFromAsyncTask(DateTime startDate)
        {
            startDate = GetNextDrawDay(startDate, true);
            Task<Dictionary<DateTime, LotteryNumbers>> task = new Task<Dictionary<DateTime, LotteryNumbers>>(() => 
            {
                startDate = GetNextDrawDay(startDate, true);
                return GetAllLotteryNumbersFrom(startDate, true); 
            });
            AddExtractionCompleteEventToAsyncTask(task);
            return task;
        }

        /// <summary>
        /// Creates a <see cref="Task"/> which calls <see cref="GetAllLotteryNumbersFrom(DateTime)"/>, with <paramref name="endDate"/>, on another thread.
        /// The <see cref="Task"/> extracts all lottery numbers past a particular date until today.
        /// Sets <see cref="lotteryNumbers"/> to the extraction result.
        /// Adds the results to <see cref="lotteryNumbers"/>, <see cref="Task{TResult}"/>, and passes them to the <see cref="ExtractionComplete"/> event with <see cref="LotteryEventArgs.lotteryNumbers"/>.
        /// </summary>
        /// <param name="endDate">The draw date to start extracting from.</param>
        /// <returns>Returns the <see cref="Task"/>, awaiting to be started.</returns>
        public Task<Dictionary<DateTime, LotteryNumbers>> GetAllLotteryNumbersToAsyncTask(DateTime endDate)
        {
            endDate = GetNextDrawDay(endDate, true);
            Task<Dictionary<DateTime, LotteryNumbers>> task = new Task<Dictionary<DateTime, LotteryNumbers>>(() =>
            {
                endDate = GetNextDrawDay(endDate, true);
                return GetAllLotteryNumbersTo(endDate, true);
            });
            AddExtractionCompleteEventToAsyncTask(task);
            return task;
        }

        /// <summary>
        /// Creates a <see cref="Task"/> which calls <see cref="GetAllAsyncLotteryNumbersInRange(DateTime, DateTime)"/>, with <see cref="startDrawDate"/> and <see cref="stopDrawDate"/>, on another thread.
        /// The <see cref="Task"/> extracts lottery numbers from <see cref="startDrawDate"/> until <see cref="stopDrawDate"/>.
        /// Adds the results to <see cref="lotteryNumbers"/>, <see cref="Task{TResult}"/>, and passes them to the <see cref="ExtractionComplete"/> event with <see cref="LotteryEventArgs.lotteryNumbers"/>.
        /// </summary>
        /// <returns>Returns the <see cref="Task"/>, awaiting to be started.</returns>
        public Task<Dictionary<DateTime, LotteryNumbers>> GetAllLotteryNumbersInRangeAsyncTask()
        {
            Task<Dictionary<DateTime, LotteryNumbers>> task = new Task<Dictionary<DateTime, LotteryNumbers>>(() => { return GetAllLotteryNumbersInRange(startDrawDate, stopDrawDate, true); });
            AddExtractionCompleteEventToAsyncTask(task);
            return task;
        }

        /// <summary>
        /// Creates a <see cref="Task"/> which calls <see cref="GetAllLotteryNumbersInRange"/>, with <paramref name="startDate"/> and <paramref name="stopDate"/>, on another thread.
        /// The <see cref="Task"/> extracts lottery numbers from <paramref name="startDate"/> until <paramref name="stopDate"/>.
        /// Adds the results to <see cref="lotteryNumbers"/>, <see cref="Task{TResult}"/>, and passes them to the <see cref="ExtractionComplete"/> event with <see cref="LotteryEventArgs.lotteryNumbers"/>.
        /// </summary>
        /// <param name="startDate">The draw date to start extracting from.</param>
        /// <param name="stopDate">The draw date to end the extraction on.</param>
        /// <returns>Returns the <see cref="Task"/>, awaiting to be started.</returns>
        public Task<Dictionary<DateTime, LotteryNumbers>> GetAllLotteryNumbersInRangeAsyncTask(DateTime startDate, DateTime stopDate)
        {
            Task<Dictionary<DateTime, LotteryNumbers>> task = new Task<Dictionary<DateTime, LotteryNumbers>>(() => { return GetAllLotteryNumbersInRange(startDate, stopDate, true); });
            AddExtractionCompleteEventToAsyncTask(task);
            return task;
        }

        /// <summary>
        /// Adds a continueing action to the asynchronous extraction <see cref="Task"/>, which fires off the <see cref="ExtractionComplete"/> event when the task is considered complete.
        /// </summary>
        /// <param name="task">The asynchronous extraction <see cref="Task"/>.</param>
        private void AddExtractionCompleteEventToAsyncTask(Task<Dictionary<DateTime, LotteryNumbers>> task)
        {
            task.ContinueWith((t) => {
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, statusMessage = "Extraction Complete", lotteryNumbers = t.Result, extractionTask = task, extractionProgress = 1 };
                ExtractionComplete?.Invoke(args);
            });
        }
       

        #endregion

        #region Event Propagation

        /// <summary>
        /// Used to propagate the <see cref="WebsiteParser.Status"/> event to <see cref="Status"/>.
        /// </summary>
        /// <param name="args">Used to pass event data.</param>
        private void PropagateExtractorStatusEvent(WebsiteParserEventArgs args)
        {
            LotteryEventArgs lotteryArgs = new LotteryEventArgs { lottery = this, statusMessage = args.statusMessage, extractorEventArgs = args };
        }

        /// <summary>
        /// Used to propagate the <see cref="WebsiteParser.Warning"/> event to <see cref="Warning"/>.
        /// </summary>
        /// <param name="args">Used to pass event data.</param>
        private void PropagateExtractorWarningEvent(WebsiteParserEventArgs args)
        {
            LotteryEventArgs lotteryArgs = new LotteryEventArgs { lottery = this, warningMessage = args.warningMessage, extractorEventArgs = args };
        }

        /// <summary>
        /// Used to propagate the <see cref="WebsiteParser.Error"/> event to <see cref="Error"/>.
        /// </summary>
        /// <param name="args">Used to pass event data.</param>
        private void PropagateExtractorErrorEvent(WebsiteParserEventArgs args)
        {
            LotteryEventArgs lotteryArgs = new LotteryEventArgs { lottery = this, errorMessage = args.errorMessage, extractorEventArgs = args };
        }


        #endregion

        #region IO

        /// <summary>
        /// Exports <see cref="lotteryNumbers"/> to a text file in the CSV format.
        /// </summary>
        /// <param name="filepath">The path of the file.</param>
        /// <returns>If the file was serialized and saved without any exceptions.</returns>
        public bool ExportNumbers(string filepath)
        {
            try
            {
                LotteryNumbers[] sorted = lotteryNumbers.Values.ToArray();
                Array.Sort(sorted, (x, y) => x.drawDate.Date.CompareTo(y.drawDate.Date));
                LotteryNumbers.ExportNumbers(sorted, filepath, LotteryNumbers.FileFormat.CSV);
                return true;
            }
            catch (Exception ex)
            {
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, exception = ex, errorMessage = "Export Lottery Numbers Failed | " + ex.Message };
                Error?.Invoke(args);
                return false;
            }
        }

        /// <summary>
        /// Exports <see cref="lotteryNumbers"/> to a text file in the selected <paramref name="format"/>
        /// </summary>
        /// <param name="filepath">The path of the file.</param>
        /// <param name="format">The format of the file. CSV, JSON or XML.</param>
        /// <returns>If the file was serialized and saved without any exceptions.</returns>
        public bool ExportNumbers(string filepath, LotteryNumbers.FileFormat format)
        {
            try
            {
                LotteryNumbers[] sorted = lotteryNumbers.Values.ToArray();
                Array.Sort(sorted, (x, y) => x.drawDate.Date.CompareTo(y.drawDate.Date));
                LotteryNumbers.ExportNumbers(sorted, filepath, format);
                return true;
            }
            catch (Exception ex)
            {
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, exception = ex, errorMessage = "Export Lottery Numbers Failed | " + ex.Message };
                Error?.Invoke(args);
                return false;
            }
        }


        /// <summary>
        /// Imports previously exported <see cref="LotteryNumbers"/> into <see cref="lotteryNumbers"/>.
        /// Attempts to determine the format of the file by its extension.
        /// </summary>
        /// <param name="filepath">The path of the file.</param>
        /// <returns><see cref="lotteryNumbers"/> after deserializing. If an exception was thrown, NULL will be returned.</returns>
        public Dictionary<DateTime, LotteryNumbers> ImportNumbers(string filepath)
        {
            try
            {
                LotteryNumbers[] numbers = LotteryNumbers.ImportNumbers(filepath);
                return lotteryNumbers = LotteryNumbers.AddToDictionary(numbers);
            }
            catch (Exception ex)
            {
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, exception = ex, errorMessage = "Import Lottery Numbers Failed | "+ ex.Message };
                Error?.Invoke(args);
                return null;
            }
        }

        /// <summary>
        /// Imports previously exported <see cref="LotteryNumbers"/> into <see cref="lotteryNumbers"/>.
        /// Attempts to determine the format of the file by its extension.
        /// </summary>
        /// <param name="filepath">The path of the file.</param>
        /// <param name="format">The format of the file. CSV, JSON or XML.</param>
        /// <returns><see cref="lotteryNumbers"/> after deserializing. If an exception was thrown, NULL will be returned.</returns>
        public Dictionary<DateTime, LotteryNumbers> ImportNumbers(string filepath, LotteryNumbers.FileFormat format)
        {
            try
            {
                LotteryNumbers[] numbers = LotteryNumbers.ImportNumbers(filepath, format);
                return lotteryNumbers = LotteryNumbers.AddToDictionary(numbers);
            }
            catch (Exception ex)
            {
                LotteryEventArgs args = new LotteryEventArgs { lottery = this, exception = ex, errorMessage = "Import Lottery Numbers Failed | " + ex.Message };
                Error?.Invoke(args);
                return null;
            }
        }


            #endregion

        #endregion

        #region Classes/Structures


        /// <summary>
        /// The time of day the draws are released online.
        /// </summary>
        public struct DrawTime
        {
            /// <summary>
            /// The hour in a 24 hour format.
            /// </summary>
            public int hour;

            /// <summary>
            /// The minute.
            /// </summary>
            public int minute;

            /// <summary>
            /// The timezone offset.
            /// </summary>
            public float timezoneOffset;


            public DrawTime(int _hour, int _minute, float _timezoneOffset)
            {
                this.hour = _hour;
                this.minute = _minute;
                this.timezoneOffset = _timezoneOffset;
            }
        }
    }

    #endregion

    /// <summary>
    /// Provides data for the <see cref="Lottery.LotteryEventHandler"/>.
    /// </summary>
    public class LotteryEventArgs
    {

        /// <summary>
        /// The <see cref="Lottery"/> that fired the event.
        /// </summary>
        public Lottery lottery { get; set; }


        ///// <summary>
        ///// Used to retrieve an error code from the <see cref="Lottery.Error"/> event.
        ///// </summary>
        //int errorCode = 0;

        /// <summary>
        /// Used to retrieve a status message from the <see cref="Lottery.Status"/> event.
        /// </summary>
        public string statusMessage { get; set; }

        /// <summary>
        /// Used to retrieve a warning message from the <see cref="Lottery.Warning"/> event.
        /// </summary>
        public string warningMessage { get; set; }

        private double _extractionProgress  = -1;

        /// <summary>
        /// Used to retrieve the progress for an extraction from the <see cref="Lottery.Status"/> event.
        /// From 0 to 1.
        /// </summary>
        /// <remarks>If no progress data is passed, the value will be -1.</remarks>
        public double extractionProgress
        {
            get
            {
                if (double.IsNaN(_extractionProgress) || double.IsInfinity(_extractionProgress))
                    return 0;
                else
                    return _extractionProgress;
            }
            set { _extractionProgress = value; }
        }

        /// <summary>
        /// Used to retrieve error messages from the <see cref="Lottery.Error"/> event.
        /// </summary>
        public string errorMessage { get; set; }

        /// <summary>
        /// The <see cref="DateTime"/> that corresponds to the event.
        /// </summary>
        public DateTime drawDate { get; set; }

        /// <summary>
        /// Used to pass event data from <see cref="WebsiteParser.ExtractorEventHandler"/> to the <see cref="Lottery.LotteryEventHandler"/>.
        /// </summary>
        public WebsiteParserEventArgs extractorEventArgs { get; set; }

        /// <summary>
        /// Used to retrieve <see cref="LotteryNumbers"/> from the <see cref="Lottery.ExtractionComplete"/> event.
        /// </summary>
        public Dictionary<DateTime, LotteryNumbers> lotteryNumbers { get; set; }


        /// <summary>
        /// Passed when an asynchronous extraction finishes.
        /// </summary>
        public Task<Dictionary<DateTime, LotteryNumbers>> extractionTask { get; set; }

        /// <summary>
        /// The exception relevant to the event, if any.
        /// </summary>
        public Exception exception { get; set; }
    }

}
