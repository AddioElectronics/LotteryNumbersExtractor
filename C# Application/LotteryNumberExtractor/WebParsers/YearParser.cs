using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LottoNumbers.Extractor.WebParsers
{
    /// <summary>
    /// Parser for a website who displays the numbers a year at a time.
    /// </summary>
    public abstract class YearParser : WebsiteParser
    {

        /// <summary>
        /// Extracts all the <see cref="LotteryNumbers"/> from a website who displays the numbers a year at a time.
        /// </summary>
        /// <param name="lottery">The lottery you want the numbers for.</param>
        /// <param name="year">The <see cref="DateTime"/> holding the year of the numbers to retrieve.</param>
        /// <returns>All the lottery numbers from a particular year, or null if the data was invalid or unable to be retrieved.</returns>
        internal LotteryNumbers[] ExtractNumbers(Lottery.Lotto lottery, DateTime date)
        {
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
        protected abstract LotteryNumbers[] ParseNumbers(string getResponse);

    }
}
