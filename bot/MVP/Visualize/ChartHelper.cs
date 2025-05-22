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

        public static void InitializeChart(CartesianChart chart)
        {
            // Load data from CSV file
            LoadCandlesFromCsv("candles.csv");

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

            // Setup X axis labels (date labels)
            chart.XAxes = new[]
            {
                new Axis
                {
                    Labeler = value => new DateTime((long)value).ToLocalTime().ToString("HH:mm"),
                    UnitWidth = TimeSpan.FromMinutes(1).Ticks,
                    LabelsRotation = 15,
                    TextSize = 12,
                }
            };

            chart.YAxes = new[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("F2"),
                    TextSize = 12
                }
            };
        }

        public static void LoadCandlesFromCsv(string csvPath)
        {
            if (!File.Exists(csvPath)) throw new FileNotFoundException($"CSV File not found at this path: {csvPath}");

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
    }
}



