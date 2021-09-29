using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LottoNumbers.Extractor.WebParsers
{
    /// <summary>
    /// Parser for a website who displays the numbers one day at a time.
    /// </summary>
    public abstract class DayParser : WebsiteParser
    {

        /// <summary>
        /// The current draw date being extracted/parsed.
        /// </summary>
        /// <remarks>This is for adding to error messages.</remarks>
        protected DateTime currentDrawDate { get; set; }

        /// <summary>
        /// Extracts the lottery numbers from a website who displays they by a particular date.
        /// </summary>
        /// <param name="lottery">The lottery you want the numbers for.</param>
        /// <param name="date">The date of the draw.</param>
        /// <returns>The lottery numbers for the draw <paramref name="date"/>. Or null if the data was invalid, or unable to be retrieved.</returns>
        internal LotteryNumbers? ExtractNumbers(Lottery.Lotto lottery, DateTime date)
        {
            currentDrawDate = date.Date;

            string response = CreateAndRunExtractionRequest(lottery, date);

            if (response != null)
                return ParseNumbers(response);
            else
                return null;
        }

        /// <summary>
        /// Parses the Lottery Numbers from the data retrieved by the GET request from the website.
        /// </summary>
        /// <param name="getResponse">The response from <see cref="CreateGetRequestURL(Lottery.Lotto, DateTime)"/></param>
        /// <returns>The parsed lottery numbers, or null if the data was invalid.</returns>
        protected abstract LotteryNumbers? ParseNumbers(string getResponse);

    }
}
