namespace Visualize
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            cartesianChart1 = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            logTextBox = new RichTextBox();
            SuspendLayout();
            // 
            // cartesianChart1
            // 
            cartesianChart1.Location = new Point(12, 12);
            cartesianChart1.MatchAxesScreenDataRatio = false;
            cartesianChart1.Name = "cartesianChart1";
            cartesianChart1.Size = new Size(1348, 634);
            cartesianChart1.TabIndex = 0;
            cartesianChart1.Load += cartesianChart1_Load;
            // 
            // logTextBox
            // 
            logTextBox.BackColor = SystemColors.ControlText;
            logTextBox.ForeColor = SystemColors.Window;
            logTextBox.Location = new Point(12, 652);
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.Size = new Size(1348, 120);
            logTextBox.TabIndex = 1;
            logTextBox.Text = "";
            logTextBox.TextChanged += richTextBox1_TextChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1372, 784);
            Controls.Add(logTextBox);
            Controls.Add(cartesianChart1);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart cartesianChart1;
        private RichTextBox logTextBox;
    }
}
