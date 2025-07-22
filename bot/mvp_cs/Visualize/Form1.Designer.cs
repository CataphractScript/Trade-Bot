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
            log_text_box = new RichTextBox();
            reload_button = new Button();
            zigzag_button = new Button();
            SuspendLayout();
            // 
            // cartesianChart1
            // 
            cartesianChart1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cartesianChart1.Location = new Point(0, 0);
            cartesianChart1.MatchAxesScreenDataRatio = false;
            cartesianChart1.Name = "cartesianChart1";
            cartesianChart1.Size = new Size(1372, 590);
            cartesianChart1.TabIndex = 0;
            cartesianChart1.Load += cartesianChart1_Load;
            // 
            // log_text_box
            // 
            log_text_box.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            log_text_box.BackColor = SystemColors.ControlText;
            log_text_box.ForeColor = SystemColors.Window;
            log_text_box.Location = new Point(12, 596);
            log_text_box.Name = "log_text_box";
            log_text_box.ReadOnly = true;
            log_text_box.Size = new Size(1348, 120);
            log_text_box.TabIndex = 1;
            log_text_box.Text = "";
            log_text_box.TextChanged += richTextBox1_TextChanged;
            // 
            // reload_button
            // 
            reload_button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            reload_button.Location = new Point(12, 722);
            reload_button.Name = "reload_button";
            reload_button.Size = new Size(150, 50);
            reload_button.TabIndex = 2;
            reload_button.Text = "Reload Chart";
            reload_button.UseVisualStyleBackColor = true;
            reload_button.Click += reload_button_Click;
            // 
            // zigzag_button
            // 
            zigzag_button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            zigzag_button.Location = new Point(168, 722);
            zigzag_button.Name = "zigzag_button";
            zigzag_button.Size = new Size(150, 50);
            zigzag_button.TabIndex = 3;
            zigzag_button.Text = "ZigZag";
            zigzag_button.UseVisualStyleBackColor = true;
            zigzag_button.Click += zigzag_button_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1372, 784);
            Controls.Add(zigzag_button);
            Controls.Add(reload_button);
            Controls.Add(log_text_box);
            Controls.Add(cartesianChart1);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart cartesianChart1;
        private RichTextBox log_text_box;
        private Button reload_button;
        private Button zigzag_button;
    }
}
