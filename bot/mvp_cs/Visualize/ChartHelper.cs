using CsvHelper;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using Skender.Stock.Indicators;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using static SkiaSharp.HarfBuzz.SKShaper;


namespace Visualize
{
    public static class ChartHelper
    {
        //public static Axis[] XAxes { get; set; }
        //public static ISeries[] Series { get; set; }

        private static ObservableCollection<FinancialPoint> _candles = new();
        public static ObservableCollection<FinancialPoint> Candles => _candles;
        private const int MaxCandles = 10000;
        private const int ThresholdPercent = 1;

        // Setup X axis (time axis)
        private static Axis[] CreateDefaultXAxis()
        {
            return new[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToString("HH:mm"),
                    UnitWidth = TimeSpan.FromMinutes(1).Ticks,
                    LabelsRotation = 15,
                    TextSize = 12,
                }
            };
        }

        // Setup Y axis (price axis)
        private static Axis[] CreateDefaultYAxis()
        {
            return new[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("F2"),
                    TextSize = 12
                }
            };
        }

        //private static CandlesticksSeries<FinancialPoint> CreateCandleSeries(ObservableCollection<FinancialPoint> values)
        //{
        //    return new CandlesticksSeries<FinancialPoint>
        //    {
        //        Values = values
        //    };
        //}


        public static void InitializeChart(CartesianChart chart)
        {
            string csvPath = "candles.csv";

            try
            {
                if (!File.Exists(csvPath)) throw new FileNotFoundException();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, Color.Red);
                return;
            }

            // Load data from CSV file
            LoadCandlesFromCsv(csvPath);


            // Convert CandleModel to FinancialPointI
            var chartData = _candles.Select(c => new FinancialPoint
            {
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Date = c.Date
            }).ToList();

            // Setup chart series
            var series = new CandlesticksSeries<FinancialPoint>
            {
                Values = chartData,
                UpFill = new SolidColorPaint(new SKColor(0, 204, 0)),
                UpStroke = null,
                DownFill = new SolidColorPaint(new SKColor(0, 0, 0)),
                DownStroke = null,
            };

            chart.Series = new LiveChartsCore.ISeries[] { series };
            chart.XAxes = CreateDefaultXAxis();
            chart.YAxes = CreateDefaultYAxis();

            // Add ZigZag indicator series
            //AddZigZagSeries(chart);
        }

        // Add ZigZag indicator as a line series to the chart with Fibonacci levels at the end
        public static void AddZigZagSeries(CartesianChart chart)
        {
            // Convert candles to list of Quote for indicator calculation
            var quotes = _candles.Select(c => new Quote
            {
                Date = c.Date,
                Open = (decimal)c.Open,
                High = (decimal)c.High,
                Low = (decimal)c.Low,
                Close = (decimal)c.Close,
                Volume = 0
            }).ToList();

            // Calculate ZigZag indicator with (ThresholdPercent)% threshold using HighLow prices
            var zigzag = quotes.GetZigZag(endType: EndType.HighLow, percentChange: ThresholdPercent);

            // Extract non-null ZigZag points as ObservablePoints
            var zigzagPoints = zigzag
                .Where(p => p.ZigZag != null)
                .Select(p => new ObservablePoint(p.Date.Ticks, (double)p.ZigZag!))
                .ToList();

            // Create line series for ZigZag indicator
            var zigzagSeries = new LineSeries<ObservablePoint>
            {
                Values = zigzagPoints,
                Stroke = new SolidColorPaint(new SKColor(255, 0, 255)),
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0
            };

            // Get existing series or create new list
            var currentSeries = chart.Series?.ToList() ?? new List<LiveChartsCore.ISeries>();
            currentSeries.Add(zigzagSeries);

            // Identify turning points (Highs and Lows) from zigzag points
            var turningPoints = new List<(long ticks, double Price, string Type)>();

            for (int i = 1; i < zigzagPoints.Count - 1; i++)
            {
                var prev = zigzagPoints[i - 1];
                var current = zigzagPoints[i];
                var next = zigzagPoints[i + 1];

                string type = "";
                if (current.Y > prev.Y && current.Y > next.Y)
                    type = "High";
                else if (current.Y < prev.Y && current.Y < next.Y)
                    type = "Low";

                if (type != "")
                    turningPoints.Add(((long)current.X, current.Y.Value, type));
            }

            // Separate High and Low points for plotting
            var highPoints = turningPoints
                .Where(p => p.Type == "High")
                .Select(p => new ObservablePoint(p.ticks, p.Price))
                .ToList();

            var lowPoints = turningPoints
                .Where(p => p.Type == "Low")
                .Select(p => new ObservablePoint(p.ticks, p.Price))
                .ToList();

            // Create scatter series for High points (blue)
            var highSeries = new ScatterSeries<ObservablePoint>
            {
                Values = highPoints,
                GeometrySize = 8,
                Fill = new SolidColorPaint(SKColors.Blue),
            };

            // Create scatter series for Low points (orange)
            var lowSeries = new ScatterSeries<ObservablePoint>
            {
                Values = lowPoints,
                GeometrySize = 8,
                Fill = new SolidColorPaint(SKColors.Orange)
            };

            currentSeries.Add(highSeries);
            currentSeries.Add(lowSeries);

            // Take the last High and Low points for Fibonacci levels calculation
            if (highPoints.Count == 0 || lowPoints.Count == 0)
            {
                chart.Series = currentSeries.ToArray();
                return; // Not enough points to draw Fibonacci
            }

            var high = highPoints.Last();
            var low = lowPoints.Last();

            var fibonacciLevels = GetFibonacciLevels(high.Y.Value, low.Y.Value);

            // Define start and end X axis range for Fibonacci lines
            var startDate = low.X < high.X ? low.X : high.X;
            var endDate = _candles.Last().Date.Ticks;

            // Add Fibonacci retracement lines and labels
            foreach (var level in fibonacciLevels)
            {
                // Create a Fibonacci line series for each level
                var fibSeries = new LineSeries<ObservablePoint>
                {
                    Values = new List<ObservablePoint>
                    {
                        new ObservablePoint(startDate, level.Level),
                        new ObservablePoint(endDate, level.Level)
                    },
                    Stroke = new SolidColorPaint(SKColors.Gray, 1),
                    Fill = null,
                    GeometrySize = 0,
                    LineSmoothness = 0,
                    Name = level.Label,
                };

                currentSeries.Add(fibSeries);

                // Create a label for the Fibonacci level (invisible geometry but with data label)
                var label = new ScatterSeries<ObservablePoint>
                {
                    Values = new List<ObservablePoint>
                    {
                        new ObservablePoint((high.X + low.X) / 2, level.Level)
                    },
                    GeometrySize = 0,
                    Fill = new SolidColorPaint(SKColors.Transparent),
                    Name = level.Label,
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsSize = 15,
                    DataLabelsFormatter = point => $"{level.Label}: {point.Model.Y:F2}",
                    Stroke = new SolidColorPaint(SKColors.Transparent)
                };
                currentSeries.Add(label);
            }

            // Update chart series
            chart.Series = currentSeries.ToArray();
        }

        /// <summary>
        /// Calculate common Fibonacci retracement levels between high and low prices.
        /// </summary>
        /// <param name=\"high\">High price point</param>
        /// <param name=\"low\">Low price point</param>
        /// <returns>List of tuples with Level value and Label</returns>
        private static List<(double Level, string Label)> GetFibonacciLevels(double high, double low)
        {
            var range = high - low;

            return
            [
                (high - range * 0.382, "Fib 38.2%"),
                (high - range * 0.5, "Fib 50%"),
                (high - range * 0.618, "Fib 61.8%"),
                (high - range * 0.78, "Fib 78%")
            ];
        }

        public static void LoadCandlesFromCsv(string csvPath)
        {
            if (!File.Exists(csvPath)) throw new FileNotFoundException();

            var reader = new StreamReader(csvPath);
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<CandleModel>();

            var candles = records.Select(x => new FinancialPoint
            {
                Open = x.Open,
                High = x.High,
                Low = x.Low,
                Close = x.Close,
                Date = x.Date
            }).ToList();

            foreach (var candle in candles.Skip(Math.Max(0, candles.Count - MaxCandles)))
            {
                _candles.Add(candle);
            }
        }
        public static void AddCandle(CartesianChart Chart, DateTime time, double open, double high, double low, double close)
        {
            // Create a new candle (financial data point)
            var newCandle = new FinancialPoint(time, high, open, close, low);

            // Try to find an existing CandlesticksSeries in the chart
            var newSeries = Chart.Series?.OfType<CandlesticksSeries<FinancialPoint>>().FirstOrDefault();

            if (newSeries == null)
            {
                // If no series exists, create a new ObservableCollection and add the first candle
                var candleList = new ObservableCollection<FinancialPoint> { newCandle };

                // Create a new candlestick series with the initial candle list
                newSeries = new CandlesticksSeries<FinancialPoint>
                {
                    Values = candleList
                };

                Chart.Series = new LiveChartsCore.ISeries[] { newSeries };
                Chart.XAxes = CreateDefaultXAxis();
                Chart.YAxes = CreateDefaultYAxis();

                return;
            }
            else
            {
                // Ensure we are working with an ObservableCollection
                if (newSeries.Values is not ObservableCollection<FinancialPoint> candleCollection)
                {
                    // If not, convert existing data to ObservableCollection
                    var newCollection = new ObservableCollection<FinancialPoint>(
                        newSeries.Values?.Cast<FinancialPoint>() ?? Enumerable.Empty<FinancialPoint>()
                    );
                    newSeries.Values = newCollection;
                    candleCollection = newCollection;
                }

                // Insert the candle in its correct time position (sorted)
                int insertIndex = candleCollection.ToList().FindIndex(c => c.Date > time);
                if (insertIndex == -1)
                {
                    candleCollection.Add(newCandle); // Insert at end
                }
                else
                {
                    candleCollection.Insert(insertIndex, newCandle); // Insert at correct time position
                }
            }

            // Optional: force chart to update (may not be necessary)
            Chart.Update();
        }
    }
}
