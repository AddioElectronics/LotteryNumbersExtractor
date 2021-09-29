# LotteryNumbers
 `
Free for personal and/or scientific use. Licensing available for Commercial use.
`
 A C# (Library and Application) or Javascript (Chrome Extension) implementation that extracts previous winning lottery numbers from a website's records. 
 
 *Started as a side project/excercise to see if I could find any patterns in their random number generators. I quickly realized its never going to happen, so I decided to finish the library to publish on github. Just to try and salvage any wasted time. 
 If you are hosting a website which displays lottery numbers, and would like to implement this library, contact me for licensing. I will help you integrate it with your website, and/or port the code if necessary. 
 The Chrome Extension could also be easily converted to a toolbar like extension, if you would like to hire me to create a more developed extension, feel free to contact me.*
 
 ### .NET(5.0) Library
 A library which is able to extract BC49, Lotto649, LottoMax, Daily Grand, and Extra numbers from PlayNow.Com and LotteryLeaf.com
 Extra numbers are extracted at the same time as the other lotteries on PlayNow.
 Source code contains abstract classes which make it easy to add other websites and lotteries.
 Example available inside the repo.
 ### Chrome Extension
`
I have no affiliation with LotteryLeaf, it was just the first link I clicked that contained the numbers displayed one year at a time
`
At this time, the chrome extension can only extract BC49 numbers from LotteryLeaf.com
It simply parses lottery numbers off the page you are currently visiting, and exports them as a CSV file.

 
## Installation

### .NET(5.0) Library

##### Visual Studio
Reference the dll like any other library.
1. Right click your project
2. Click Add - Project Reference
3. Click browse, and select LotteryNumbersExtractor.dll

### Chrome Extension
-Open Google Chrome
-Open the Extensions Tab chrome://extensions/
-Turn on Developer Mode
-Load Unpacked and select the "BC49 Chrome Extension" folder.


## Usage
### .NET(5.0) Library

##### How to extract BC49 Numbers from Playnow.com
###### *There is a working example included in the repo, which covers "DayParsers" and "YearParsers."*
``` C#
Lottery bc49;

void CreateInstance()
{
    bc49 = new Lottery(
        Lottery.Lotto.BC49,
        new DateTime(1992, 1, 29),                          //The first draw date on the website. If you are unsure use default(DateTime).
        Lottery.LotteryDrawTimes[Lottery.Lotto.BC49],       //The time of day the draw is posted.
        Lottery.LotteryDrawDays[Lottery.Lotto.BC49],        //The days the draws happen on.
        Lottery.LotteryNumberRanges[Lottery.Lotto.BC49],    //The number ranges ex. BC49 is from 1 to 49
        new PlayNow()                                       //The website parser, PlayNow which is a "DayParser," which means it displays the numbers one day at a time.
        );
        
        
    //Add method to event.
    bc49.ExtractionComplete += ExtractionComplete;
    //bc49.Status += GetStatusUpdate; //This along with other events can be used to get messages, or its progress which can be used for a ProgressBar.
        
        
    //PlayNow being a day parser takes quite a while to extract the full range of numbers.
    //If you have already extracted numbers for a lottery, you can import previously extracted numbers(CSV, JSON or XML).
    //This will greatly speed up the extraction, as it will only extract numbers that do we do not already have.
    //"YearParser's" will always extract the numbers whether or not they already exist. It is quicker to extract them than to check every single date.
    bc49.ImportNumbers("{Path to previously extracted numbers}");
    
    
    //These can be used to set the ranges of draws you would like to extract.
    //This example will extract all numbers in 2021.
    bc49.startDrawDate = new DateTime(2021, 1, 1);          //The the start of 2021.
    bc49.stopDrawDate = bc49.LotteryTimeZoneNow;            //The most recent date. Equal to DateTime.now, adjusted to the lotteries timezone.
     
     
    //Here are just 2 of many examples of how numbers can be extracted.
    ExtractLastDraw();      //This will extract the most recent draw. (Synchronous Example)
    ExtractDrawsInRange();  //This will extract the draws in the range we set above. (Asynchronous Example)
    ExtractAllDraws();      //This will extract the full websites records. (Asynchronous Example)
}

void ExtractLastDraw()
{
    //This will extract the most recent draw only, synchronously.
    DateTime previousDrawDate = bc49.GetMostRecentDrawDate();
    Dictionary<DateTime, LotteryNumbers> extractedNumbers = bc49.GetAllLotteryNumbersFrom(previousDrawDate);
}

void ExtractDrawsInRange()
{
    //This will extract the draws from bc49.startDrawDate to bc49.stopDrawDate, asynchronously.
    var task = lotto649.GetAllLotteryNumbersInRangeAsyncTask();
    task.Start();
    
    //When the extraction is complete, the "ExtractionComplete" event will be called.
}

void ExtractAllDraws()
{
    //This will extract the full websites records, asynchronously.
    var task = lotto649.GetAllLotteryNumbersAsyncTask();
    task.Start();
    
    //When the extraction is complete, the "ExtractionComplete" event will be called.
}


private void ExtractionComplete(LotteryEventArgs args)
{
    //Can either grab the numbers from the arguments.
    Dictionary<DateTime, LotteryNumbers> extractedNumbers = args.lotteryNumbers;
    
    //Or grab them from the bc49 instance.
    Dictionary<DateTime, LotteryNumbers> extractedNumbers = bc49.lotteryNumbers;
}
```

### Chrome Extension
-Go to lotteryleaf.com
-Navigate to the BC49 numbers and select a year (https://www.lotteryleaf.com/bc/lotto-649/2001).

-Click the Export button in the top right corner.
or
-Check Auto Export, and refresh the page.
-With Auto Export checked, every year you visit will automatically save a CSV file.

-Once you have all the years, click the Combine button
-Select all the exported csv files, and hit ok. It will combine them all into one file and export.

## Contributing


## License
Copyright (c) 2021, Addio Electronics (Canada)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

3. Redistributions in any form for commercial use, must obtain a secondary license from Addio Electronics and abide by any terms or fees that were agreed upon.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

## Author

- Author   : Addio from Addio Electronics (Canada)
- Website  : www.Addio.io
- Contact  : If you wish to contact me, please do it through github.
