using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LottoNumbers.Extractor.WebParsers
{
    /// <summary>
    /// Base class for extracting and parsing lottery numbers from a particular website.
    /// </summary>
    public abstract class WebsiteParser
    {

        #region Events

        /// <summary>
        /// The event handler for <see cref="WebsiteParser"/>.
        /// </summary>
        /// <param name="args">Used to pass the event data.</param>
        public delegate void ExtractorEventHandler(WebsiteParserEventArgs args);

        /* Decided to go with a synchronous implementation.*/
        ///// <summary>
        ///// Event for when each GET request is finished.
        ///// </summary>
        //internal protected event ExtractorEventHandler GetRequestFinished;

        ///// <summary>
        ///// Event for when a GET request has failed.
        ///// </summary>
        //internal protected event ExtractorEventHandler GetRequestFailed;

        ///// <summary>
        ///// Event for when all GET requests are complete, and the <see cref="LotteryNumbers"/> have been parsed.
        ///// </summary>
        //internal protected event ExtractorEventHandler ExtractingComplete;

        ///// <summary>
        ///// Event for when an extraction was unable to complete.
        ///// </summary>
        //internal protected event ExtractorEventHandler ExtractingFailed;

        /// <summary>
        /// The event for retrieving status messages, and progress.
        /// The message can be retrieved from 
        /// </summary>
        public event ExtractorEventHandler Status;

        /// <summary>
        /// The event for retrieving warning messages.
        /// The error message can be retrieved from <see cref="WebsiteParserEventArgs.warningMessage"/>.
        /// </summary>
        public event ExtractorEventHandler Warning;

        /// <summary>
        /// The event for retrieving error messages from the extraction process.
        /// The error message can be retrieved from <see cref="WebsiteParserEventArgs.errorMessage"/>.
        /// </summary>
        public event ExtractorEventHandler Error;

        /// <summary>
        /// Allows derived classes to raise the event.
        /// </summary>
        /// <param name="args">Used to pass the event data.</param>
        protected void RaiseEventStatus(WebsiteParserEventArgs args)
        {
            Status?.Invoke(args);
        }

        /// <summary>
        /// Allows derived classes to raise the event.
        /// </summary>
        /// <param name="args">Used to pass the event data.</param>
        protected void RaiseEventWarning(WebsiteParserEventArgs args)
        {
            Warning?.Invoke(args);
        }

        /// <summary>
        /// Allows derived classes to raise the event.
        /// </summary>
        /// <param name="args">Used to pass the event data.</param>
        protected void RaiseEventError(WebsiteParserEventArgs args)
        {
            Error?.Invoke(args);
        }

        #endregion

        #region Properties

        ///// <summary>
        ///// How does the extractor get numbers from the website?
        ///// </summary>
        //internal abstract ExtractionMethod extractionMethod { get; }

        /// <summary>
        /// The <see cref="Lottery"/> that is parent to this.
        /// </summary>
        public Lottery parentLottery { get; internal set; }


        /// <summary>
        /// The HTTP client for executing GET requests.
        /// </summary>
        protected static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Is the website an official and trusted source?
        /// </summary>
        public abstract bool officialWebsite { get; }

        /// <summary>
        /// The URL the <see cref="WebsiteParser"/> was designed for.
        /// </summary>
        public abstract string websiteURL { get; }


        /// <summary>
        /// Timer used to make sure the <see cref="GetRequestMinimumInterval"/> is met.
        /// </summary>
        protected Stopwatch requestIntervalTimer = new Stopwatch();


        /// <summary>
        /// The lotteries this extractor is capable of getting numbers for.
        /// </summary>
        internal abstract Lottery.Lotto[] extractableLotteries { get; }

        /// <summary>
        /// Should the parser extract the <see cref="DrawInfo"/>, if it can?
        /// </summary>
        public bool extractVerboseDrawInfo { get; set; } = true;


        /// <summary>
        /// The formats used for parsing the date information.
        /// </summary>
        public static string[] dateFormats { get; set; } = new string[] { "MMM d, yyyy", "MMM dd, yyyy", "MMMd,yyyy", "MMMdd,yyyy" };

        /// <summary>
        /// The minimum time between each GET request as to not piss off the server.
        /// (Milliseconds)
        /// </summary>
        protected int _getRequestMinimumInterval = 10;

        /// <summary>
        /// The minimum time between each GET request as to not piss off the server.
        /// (Milliseconds)
        /// </summary>
        internal virtual int GetRequestMinimumInterval
        {
            get
            {
                return _getRequestMinimumInterval;
            }
            set
            {
                _getRequestMinimumInterval = value;
            }
        }

        #endregion

        #region Constructors

        ///// <summary>
        ///// Stops a blank constructor from being able to be initialized.
        ///// </summary>
        //internal WebsiteParser() { }

        ///// <summary>
        ///// Initializes a <see cref="WebsiteParser"/>.
        ///// </summary>
        ///// <param name="parent">The <see cref="Lottery"/> that is handling this.</param>
        //public WebsiteParser(Lottery parent)
        //{
        //    this.parentLottery = parent;
        //}

        #endregion

        #region Methods


        /// <summary>
        /// Create the GET request string for used for getting data from the website.
        /// </summary>
        /// <param name="lottery">The lottery.</param>
        /// <param name="date">The date or year of the numbers to be retrieved.</param>
        /// <returns>The GET request string.</returns>
        internal abstract string CreateGetRequestURL(Lottery.Lotto lottery, DateTime date);


        /// <summary>
        /// Try to find the first recorded draw on the website, for the specified <paramref name="lottery"/>.
        /// </summary>
        /// <remarks>This is a method that is optional to implement. It is not used internally.</remarks>
        /// <returns>The first recorded draw on the website. Returns null if the date was not able to be retrieved from the website.</returns>
        public virtual DateTime? TryFindFirstRecordedDraw(Lottery.Lotto lottery) { return null; }


        /// <summary>
        /// Creates an HTTP GET request string, executes it, and returns the response data.
        /// </summary>
        /// <param name="lottery">The lottery you want the numbers for.</param>
        /// <param name="date">The date of the draw, or the year of the numbers to retrieve.</param>
        /// <returns>The response from the GET Request. Or null if the HTTP GET request failed.</returns>
        protected string CreateAndRunExtractionRequest(Lottery.Lotto lottery, DateTime date)
        {
            string getRequest = CreateGetRequestURL(lottery, date);
            string data = null; //The JSON data we want.

            if (requestIntervalTimer.IsRunning)
            {
                while(requestIntervalTimer.ElapsedMilliseconds < GetRequestMinimumInterval)
                {
                    //Wait for minimum interval to be met.
                }
                requestIntervalTimer.Reset();
            }

            try
            {
                //Because we don't want to flood the server with too many get requests,
                //and the program has nothing else to do in the mean time,
                //we will just run the task Synchronously.
                //If I find a reason to do it Asynchronously I will implement it later.
                var webRequest = new HttpRequestMessage(HttpMethod.Get, getRequest);

                HttpResponseMessage response = httpClient.Send(webRequest);
                using var reader = new StreamReader(response.Content.ReadAsStream());
                data = reader.ReadToEnd();
            }
            catch(Exception ex)
            {
                if (ex is HttpRequestException)
                {
                    WebsiteParserEventArgs args = new WebsiteParserEventArgs { errorMessage = "HTTP GET Request Failed : " + ex.Message };
                    Error?.Invoke(args);
                }
                else
                {
                    WebsiteParserEventArgs args = new WebsiteParserEventArgs { errorMessage = "GET Request Failed : " + ex.Message };
                    Error?.Invoke(args);
                }
            }

            requestIntervalTimer.Start();

            return data;
        }

        /// <summary>
        /// Ping the website to make sure its online.
        /// This is used in the event a GET Request fails, so it can see if its network problems, or the request.
        /// </summary>
        /// <returns>If the website is online.</returns>
        public virtual bool ConfirmWebsiteOnline()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(websiteURL);
            request.AllowAutoRedirect = false;
            request.Method = "HEAD";
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.InternalServerError:
                    case HttpStatusCode.BadGateway:
                    case HttpStatusCode.GatewayTimeout:
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.HttpVersionNotSupported:
                    case HttpStatusCode.Gone:
                    case HttpStatusCode.Locked:
                    case HttpStatusCode.MethodNotAllowed:
                    case HttpStatusCode.MisdirectedRequest:
                    case HttpStatusCode.ExpectationFailed:
                    case HttpStatusCode.MovedPermanently:
                    case HttpStatusCode.NetworkAuthenticationRequired:
                    case HttpStatusCode.NoContent:
                    case HttpStatusCode.PermanentRedirect:
                    default:
                        return false;
                    case HttpStatusCode.OK:
                        return true;

                }

            }
            catch (WebException wex)
            {
                return false;
            }
        }


        #endregion

    }

    /// <summary>
    /// Provides data for the <see cref="WebsiteParser.ExtractorEventHandler"/>.
    /// </summary>
    public class WebsiteParserEventArgs : EventArgs
    {

        /// <summary>
        /// The <see cref="WebsiteParser"/> that fired the event.
        /// </summary>
        public WebsiteParser webparser { get; set; }

        ///// <summary>
        ///// Used to retrieve an error code from the <see cref="IExtractor.Error"/> event.
        ///// </summary>
        //int errorCode = 0;

        /// <summary>
        /// Used to retrieve a status message from the <see cref="IExtractor.Status"/> event.
        /// </summary>
        public string statusMessage { get; set; }

        /// <summary>
        /// Used to retrieve a warning message from the <see cref="WebsiteParser.Warning"/> event.
        /// </summary>
        public string warningMessage { get; set; }

        /// <summary>
        /// Used to retrieve error messages from the <see cref="WebsiteParser.Error"/> event.
        /// </summary>
        public string errorMessage { get; set; }

        /// <summary>
        /// Used to retrieve the progress for an extraction from the <see cref="WebsiteParser.Status"/> event.
        /// From 0 to 1.
        /// </summary>
        /// <remarks>If no progress data is passed, the value will be -1.</remarks>
        public double progress { get; set; } = -1;

        /* Decided to go with a synchronous implementation instead.*/
        ///// <summary>
        ///// <see cref="Lottery"/> uses this to retrieve the parsed lottery numbers once an asynchronous extraction is complete.
        ///// Passed with the <see cref="Extractors.IExtractor.ExtractionComplete"/> event.
        ///// </summary>
        //internal LotteryNumbers[] lotteryNumbers;
    }
}
