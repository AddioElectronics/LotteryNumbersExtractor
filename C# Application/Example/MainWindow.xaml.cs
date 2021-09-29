using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using LottoNumbers.Extractor;
using LottoNumbers.Extractor.WebParsers.Websites;
using LottoNumbers.Processing;

namespace LottoNumbers.Example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// The lottery instance which extracts BC49 numbers from PlayNow.com.
        /// Uses a <see cref="Extractor.WebParsers.DayParser"/>.
        /// </summary>
        Lottery bc49;

        /// <summary>
        /// The lottery instance which extracts Lotto649 numbers from LotteryLeaf.com.
        /// Uses a <see cref="Extractor.WebParsers.YearParser"/>.
        /// </summary>
        Lottery lotto649;

        /// <summary>
        /// Path to local BC49 numbers.
        /// </summary>
        string bc49Path { get { return System.IO.Path.Combine(Environment.CurrentDirectory, "Resources\\BC49_PlayNow.csv"); } }

        /// <summary>
        /// Path to local 649 numbers.
        /// </summary>
        string lotto649Path { get { return System.IO.Path.Combine(Environment.CurrentDirectory, "Resources\\Lotto649_LotteryLeaf.csv"); } }

        /// <summary>
        /// A count of how many times each number has appeared in the draws.
        /// </summary>
        int[] bc49NumberFrequency, lotto649NumberFrequency;

        /// <summary>
        /// The lowest value displayed on the graph.
        /// </summary>
        int graphMin = 400;

        /// <summary>
        /// Which lottery to display?
        /// 0 = BC49
        /// 1 = Lotto649
        /// </summary>
        int displayLottery = 0;

        /// <summary>
        /// The <see cref="Task"/> which asynchronously extracts BC49 <see cref="LotteryNumbers"/>.
        /// </summary>
        Task<Dictionary<DateTime, LotteryNumbers>> bc49_ExtractionTask;


        public MainWindow()
        {
            InitializeComponent();
            textBox_MinValue.Text = graphMin.ToString();

            InitializeExtractor();
        }

        /// <summary>
        /// Initialize an extractor.
        /// When the numbers are either imported or extracted, it will display their frequency as a graph.
        /// </summary>
        private void InitializeExtractor()
        {
            //Create a Lottery instance for BC49, which extracts from PlayNow.com.
            bc49 = new Lottery(
                Lottery.Lotto.BC49,
                new DateTime(1992, 1, 29),
                Lottery.LotteryDrawTimes[Lottery.Lotto.BC49],
                Lottery.LotteryDrawDays[Lottery.Lotto.BC49],
                Lottery.LotteryNumberRanges[Lottery.Lotto.BC49],
                new PlayNow()
                );

            //Create a Lottery instance for BC49, which extracts from PlayNow.com.
            lotto649 = new Lottery(
                Lottery.Lotto.Lotto649,
                new DateTime(2001, 10, 3),
                Lottery.LotteryDrawTimes[Lottery.Lotto.Lotto649],
                Lottery.LotteryDrawDays[Lottery.Lotto.Lotto649],
                Lottery.LotteryNumberRanges[Lottery.Lotto.Lotto649],
                new LotteryLeaf()
                );

            //Capture events to display the information and progress.
            bc49.Error += GetErrorMessage;
            bc49.ExtractionComplete += ExtractionComplete;
            bc49.Status += GetStatusUpdate;
            lotto649.Error += GetErrorMessage;
            lotto649.ExtractionComplete += ExtractionComplete;
            lotto649.Status += GetStatusUpdate;

            //Import previously extracted numbers to try and avoid another extraction.
            //If we do have to do another extraction, it will greatly decrease the time it takes.
            bc49.ImportNumbers(bc49Path);

            //Even if lotto649 is up to date, the extraction will be ran.
            ///This is because it uses a <see cref="Extractor.WebParsers.YearParser"/>,
            //and most likely takes just as long to extract them again, as to check every single draw date.
            lotto649.ImportNumbers(lotto649Path);


            //Does BC49 does not contain up-to-date numbers?
            if (bc49.OldestNumbersDate == null || bc49.OldestNumbersDate.Value.Date.CompareTo(bc49.GetMostRecentDrawDate().Date) < 0)
            {
                //Numbers were not up to date, extract them.
                bc49_ExtractionTask = bc49.GetAllLotteryNumbersAsyncTask();
                bc49_ExtractionTask.Start();

                //When it finishes, the Bc49_ExtractionComplete method will capture an event.
                //ProcessNumbers() will be called from there.
            }
            else
            {
                //Numbers were up to date, we can go directly to displaying them.
                bc49NumberFrequency = LotteryNumberProcessor.GetNumberFrequency(bc49.lotteryNumbers.Values.ToArray(), bc49.numberRanges, out _, true);
                ProcessNumbers();
            }


            var task = lotto649.GetAllLotteryNumbersAsyncTask();
            task.Start();
        }


        /// <summary>
        /// Run the numbers through some alogirthms and display results.
        /// </summary>
        private void ProcessNumbers()
        {
            Lottery display = displayLottery == 0 ? bc49 : lotto649;

            if (display == null) return;

            //Displays the number frequency as a graph.
            DrawNumberFrequencyGraph(displayLottery == 0 ? bc49NumberFrequency : lotto649NumberFrequency);

            //Can find most occuring pairs. (In this example they are not displayed)
            Tuple<int, int[]>[] mostOccuringPairs;
            var occuringTogether = LotteryNumberProcessor.GetCountOfPairsThatOccurTogether(display.lotteryNumbers.Values.ToArray(), display.numberRanges, out mostOccuringPairs);

            //Can find matching draws. (In this example they are not displayed)
            var matchingDraws = LotteryNumberProcessor.FindMatchingNumbers(display.lotteryNumbers.Values.ToArray());
            if (matchingDraws != null)
            {
                throw new Exception("Matching draws!?!? Must be a bug!");
            }
        }


        /// <summary>
        /// Draw a graph containing the frequency that each number was drawn.
        /// </summary>
        /// <remarks>This crappy graph is just an example of how the library can be used.</remarks>
        private void DrawNumberFrequencyGraph(int[] frequency)
        {
            if (canvas_NumberFrequency == null || frequency == null) return;

            canvas_NumberFrequency.Children.Clear();

            int lowRange = displayLottery == 0 ? bc49.numberRanges.standardLow : lotto649.numberRanges.standardLow;
            int highRange = displayLottery == 0 ? bc49.numberRanges.standardHigh : lotto649.numberRanges.standardHigh;

            int max = frequency.Max();

            int labelWidth = 30;
            int offsetH = 40;

            int height = (int)canvas_NumberFrequency.ActualHeight;

            if (height == 0)
                height = 550;



            Line bottomLine = new Line();
            bottomLine.StrokeThickness = 1;
            bottomLine.Stroke = new SolidColorBrush(Colors.DarkGray);
            bottomLine.X1 = 0;
            bottomLine.X2 = (highRange * labelWidth) + labelWidth + offsetH;
            Canvas.SetBottom(bottomLine, 50);
            Canvas.SetLeft(bottomLine, offsetH);
            canvas_NumberFrequency.Children.Add(bottomLine);

            Rectangle container = new Rectangle();
            container.Height = height - 150 + 55;
            container.Width = offsetH + (highRange * labelWidth) + labelWidth + offsetH;
            container.StrokeThickness = 1;
            container.Stroke = new SolidColorBrush(Colors.Black);
            Canvas.SetBottom(container, 10);
            Canvas.SetLeft(container, offsetH / 2);
            canvas_NumberFrequency.Children.Add(container);

            Label minLabel = new Label();
            minLabel.Content = graphMin.ToString();
            Canvas.SetBottom(minLabel, 50);
            Canvas.SetLeft(minLabel, offsetH);
            canvas_NumberFrequency.Children.Add(minLabel);

            Label maxLabel = new Label();
            maxLabel.Content = max.ToString();
            Canvas.SetBottom(maxLabel, height - 150 + 55 - 20);
            Canvas.SetLeft(maxLabel, offsetH);
            canvas_NumberFrequency.Children.Add(maxLabel);

            for (int i = lowRange; i <= highRange; i++)
            {
                Label label = new Label();
                label.Width = labelWidth;
                label.HorizontalContentAlignment = HorizontalAlignment.Center;
                label.Content = i.ToString();
                Canvas.SetLeft(label, offsetH + (i * labelWidth));
                Canvas.SetBottom(label, 20);
                canvas_NumberFrequency.Children.Add(label);

                Line line = new Line();
                line.StrokeThickness = 1;
                line.Stroke = new SolidColorBrush(Colors.Black);
                line.Y2 = 5;
                Canvas.SetBottom(line, 45);
                Canvas.SetLeft(line, offsetH + (i * labelWidth) + (labelWidth / 2));
                canvas_NumberFrequency.Children.Add(line);

                Rectangle rect = new Rectangle();
                rect.Width = labelWidth / 2;
                double rh = Misc.RemapDouble((double)frequency[i - 1] - graphMin, 0, max - graphMin, 0, height - 150);
                rect.Height = rh >= 0 ? rh : 0;
                rect.Fill = new SolidColorBrush(Colors.Green);
                Canvas.SetLeft(rect, offsetH + (i * labelWidth) + (rect.Width / 2));
                Canvas.SetBottom(rect, 55);
                canvas_NumberFrequency.Children.Add(rect);

            }

        }

        /// <summary>
        /// Catch <see cref="Lottery.Status"/> events, which will update a <see cref="ProgressBar"/> and <see cref="Label"/>.
        /// </summary>
        /// <param name="args"></param>
        private void GetStatusUpdate(LotteryEventArgs args)
        {
            if (args.extractionProgress != -1)
                Dispatcher.Invoke(() => { progressBar.Value = args.extractionProgress * 100; });            

            string status = args.statusMessage;
            Dispatcher.Invoke(() => { label_Debug.Content = status; }); ;
        }

        /// <summary>
        /// Catch the <see cref="Lottery.ExtractionComplete"/> event.
        /// </summary>
        /// <param name="args"></param>
        private void ExtractionComplete(LotteryEventArgs args)
        {

            if(args.lottery == bc49)
            {
                //Export the numbers so the next time this is ran, it will not have to extract them again.
                LotteryNumbers.ExportNumbers(bc49.lotteryNumbers.Values.ToArray(), bc49Path);

                //Get the number freq for displaying.
                bc49NumberFrequency = LotteryNumberProcessor.GetNumberFrequency(bc49.lotteryNumbers.Values.ToArray(), bc49.numberRanges, out _, true);
            }
            else if(args.lottery == lotto649)
            {
                //Export the numbers so the next time this is ran, it will not have to extract them again.
                LotteryNumbers.ExportNumbers(lotto649.lotteryNumbers.Values.ToArray(), lotto649Path);

                //Get the number freq for displaying.
                lotto649NumberFrequency = LotteryNumberProcessor.GetNumberFrequency(lotto649.lotteryNumbers.Values.ToArray(), lotto649.numberRanges, out _, true);
            }

            Dispatcher.BeginInvoke((Action)(() => { ProcessNumbers(); }));

            

        }

        /// <summary>
        /// Catch <see cref="Lottery.Error"/> events.
        /// </summary>
        /// <param name="args"></param>
        void GetErrorMessage(LotteryEventArgs args)
        {
            string error = args.errorMessage;
            Dispatcher.Invoke(() => { label_Debug.Content = error; }); ;
        }

        /// <summary>
        /// Change the graphs bottom on textbox change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_MinValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number;

            if (!int.TryParse(textBox_MinValue.Text, out number))
            {
                textBox_MinValue.Text = graphMin.ToString();
                number = graphMin;
                return;
            }

            if (number < 0)
            {
                textBox_MinValue.Text = graphMin.ToString();
                number = graphMin;
            }

            graphMin = number;
            DrawNumberFrequencyGraph(displayLottery == 0 ? bc49NumberFrequency : lotto649NumberFrequency);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            displayLottery = comboBox_Lottery.SelectedIndex;

            if (displayLottery == 0)
                textBox_MinValue.Text = 400.ToString();
            else
                textBox_MinValue.Text = 250.ToString();

            //ProcessNumbers(); //textBox_MinValue will call this.
        }

        /// <summary>
        /// Window size change event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ProcessNumbers();
        }
    }
}
