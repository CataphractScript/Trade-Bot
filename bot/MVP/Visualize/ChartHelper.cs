using CsvHelper;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Visualize;

namespace Visualize
{
    public static class ChartHelper
    {
        //public static Axis[] XAxes { get; set; }
        //public static ISeries[] Series { get; set; }

        private static ObservableCollection<FinancialPoint> _candles = new();
        public static ObservableCollection<FinancialPoint> Candles => _candles;
        private const int MaxCandles = 10000;

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

        private static CandlesticksSeries<FinancialPoint> CreateCandleSeries(ObservableCollection<FinancialPoint> values)
        {
            return new CandlesticksSeries<FinancialPoint>
            {
                Values = values
            };
        }


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
                Values = chartData
            };

            chart.Series = new ISeries[] { series };
            chart.XAxes = CreateDefaultXAxis();
            chart.YAxes = CreateDefaultYAxis();
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

                Chart.Series = new ISeries[] { newSeries };
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
