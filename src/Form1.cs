using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.WinForms;
using System.IO;
using System.Threading.Tasks;
using static Visualize.API;

namespace Visualize
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                cartesianChart1.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.Both;
                Logger.Initialize(log_text_box);

                await API.RestApiAsync();
                ChartHelper.InitializeChart(cartesianChart1);
                await API.WebsocketAPI(cartesianChart1);
            }
            catch (Exception ex)
            {
                Logger.Log("⛔ Error in Form1_Load: " + ex.Message);
            }
        }

        private void cartesianChart1_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void reload_button_Click(object sender, EventArgs e)
        {
            ChartHelper.InitializeChart(cartesianChart1);
            //ChartHelperTest.DrawCandlestickChartFromCsv(cartesianChart1);
        }

        private void zigzag_button_Click(object sender, EventArgs e)
        {
            ChartHelper.AddZigZagSeries(cartesianChart1);
        }
    }
}
