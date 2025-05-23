using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.WinForms;
using System.IO;
using System.Threading.Tasks;

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
            cartesianChart1.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.Both;

            ChartHelper.InitializeChart(cartesianChart1);
            Logger.Initialize(log_text_box);
            await LBankWebSocket.ListenToLBank(cartesianChart1);
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
        }
    }
}
