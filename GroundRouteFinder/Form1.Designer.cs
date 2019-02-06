namespace GroundRouteFinder
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.rtb = new System.Windows.Forms.RichTextBox();
            this.rbNormal = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.btnAnalyseAirport = new System.Windows.Forms.Button();
            this.txtIcao = new System.Windows.Forms.TextBox();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.gbxOutput = new System.Windows.Forms.GroupBox();
            this.gbxAiport = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtXplaneLocation = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabAircraft = new System.Windows.Forms.TabPage();
            this.rtbAircraft = new System.Windows.Forms.RichTextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.gbxOutput.SuspendLayout();
            this.gbxAiport.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabAircraft.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1533, 13);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 28);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // rtb
            // 
            this.rtb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtb.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtb.Location = new System.Drawing.Point(13, 145);
            this.rtb.Margin = new System.Windows.Forms.Padding(4);
            this.rtb.Name = "rtb";
            this.rtb.Size = new System.Drawing.Size(1620, 650);
            this.rtb.TabIndex = 1;
            this.rtb.Text = "";
            // 
            // rbNormal
            // 
            this.rbNormal.AutoSize = true;
            this.rbNormal.Location = new System.Drawing.Point(25, 28);
            this.rbNormal.Margin = new System.Windows.Forms.Padding(4);
            this.rbNormal.Name = "rbNormal";
            this.rbNormal.Size = new System.Drawing.Size(159, 21);
            this.rbNormal.TabIndex = 2;
            this.rbNormal.Text = "World Traffic Routes";
            this.rbNormal.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Checked = true;
            this.radioButton2.Location = new System.Drawing.Point(25, 57);
            this.radioButton2.Margin = new System.Windows.Forms.Padding(4);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(57, 21);
            this.radioButton2.TabIndex = 3;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "KML";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // btnAnalyseAirport
            // 
            this.btnAnalyseAirport.Location = new System.Drawing.Point(52, 86);
            this.btnAnalyseAirport.Margin = new System.Windows.Forms.Padding(4);
            this.btnAnalyseAirport.Name = "btnAnalyseAirport";
            this.btnAnalyseAirport.Size = new System.Drawing.Size(100, 28);
            this.btnAnalyseAirport.TabIndex = 1;
            this.btnAnalyseAirport.Text = "Analyse";
            this.btnAnalyseAirport.UseVisualStyleBackColor = true;
            this.btnAnalyseAirport.Click += new System.EventHandler(this.btnAnalyze_Click);
            // 
            // txtIcao
            // 
            this.txtIcao.Location = new System.Drawing.Point(67, 41);
            this.txtIcao.Margin = new System.Windows.Forms.Padding(4);
            this.txtIcao.Name = "txtIcao";
            this.txtIcao.Size = new System.Drawing.Size(132, 22);
            this.txtIcao.TabIndex = 0;
            this.txtIcao.Text = "EHAM";
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(60, 86);
            this.btnGenerate.Margin = new System.Windows.Forms.Padding(4);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(100, 28);
            this.btnGenerate.TabIndex = 6;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabAircraft);
            this.tabControl1.Location = new System.Drawing.Point(1, 2);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1663, 844);
            this.tabControl1.TabIndex = 7;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.gbxOutput);
            this.tabPage1.Controls.Add(this.gbxAiport);
            this.tabPage1.Controls.Add(this.button1);
            this.tabPage1.Controls.Add(this.rtb);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(4);
            this.tabPage1.Size = new System.Drawing.Size(1655, 815);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Generator";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // gbxOutput
            // 
            this.gbxOutput.Controls.Add(this.btnGenerate);
            this.gbxOutput.Controls.Add(this.rbNormal);
            this.gbxOutput.Controls.Add(this.radioButton2);
            this.gbxOutput.Location = new System.Drawing.Point(236, 13);
            this.gbxOutput.Name = "gbxOutput";
            this.gbxOutput.Size = new System.Drawing.Size(199, 125);
            this.gbxOutput.TabIndex = 10;
            this.gbxOutput.TabStop = false;
            this.gbxOutput.Text = "Output";
            // 
            // gbxAiport
            // 
            this.gbxAiport.Controls.Add(this.label2);
            this.gbxAiport.Controls.Add(this.btnAnalyseAirport);
            this.gbxAiport.Controls.Add(this.txtIcao);
            this.gbxAiport.Location = new System.Drawing.Point(13, 13);
            this.gbxAiport.Name = "gbxAiport";
            this.gbxAiport.Size = new System.Drawing.Size(217, 125);
            this.gbxAiport.TabIndex = 9;
            this.gbxAiport.TabStop = false;
            this.gbxAiport.Text = "Airport";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 17);
            this.label2.TabIndex = 7;
            this.label2.Text = "ICAO:";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btnBrowse);
            this.tabPage2.Controls.Add(this.txtXplaneLocation);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(4);
            this.tabPage2.Size = new System.Drawing.Size(1655, 815);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Settings";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(679, 10);
            this.btnBrowse.Margin = new System.Windows.Forms.Padding(4);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(100, 28);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // txtXplaneLocation
            // 
            this.txtXplaneLocation.Location = new System.Drawing.Point(119, 12);
            this.txtXplaneLocation.Margin = new System.Windows.Forms.Padding(4);
            this.txtXplaneLocation.Name = "txtXplaneLocation";
            this.txtXplaneLocation.Size = new System.Drawing.Size(551, 22);
            this.txtXplaneLocation.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 16);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "X Plane folder:";
            // 
            // tabAircraft
            // 
            this.tabAircraft.Controls.Add(this.rtbAircraft);
            this.tabAircraft.Controls.Add(this.button2);
            this.tabAircraft.Location = new System.Drawing.Point(4, 25);
            this.tabAircraft.Margin = new System.Windows.Forms.Padding(4);
            this.tabAircraft.Name = "tabAircraft";
            this.tabAircraft.Padding = new System.Windows.Forms.Padding(4);
            this.tabAircraft.Size = new System.Drawing.Size(1655, 815);
            this.tabAircraft.TabIndex = 2;
            this.tabAircraft.Text = "Aircraft Analysis";
            this.tabAircraft.UseVisualStyleBackColor = true;
            // 
            // rtbAircraft
            // 
            this.rtbAircraft.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbAircraft.Location = new System.Drawing.Point(73, 71);
            this.rtbAircraft.Margin = new System.Windows.Forms.Padding(4);
            this.rtbAircraft.Name = "rtbAircraft";
            this.rtbAircraft.Size = new System.Drawing.Size(1507, 740);
            this.rtbAircraft.TabIndex = 1;
            this.rtbAircraft.Text = "";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(35, 22);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 28);
            this.button2.TabIndex = 0;
            this.button2.Text = "button2";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnExit
            // 
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.Location = new System.Drawing.Point(1542, 853);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(115, 36);
            this.btnExit.TabIndex = 8;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1669, 901);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "Ground Route Finder 0.1";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.gbxOutput.ResumeLayout(false);
            this.gbxOutput.PerformLayout();
            this.gbxAiport.ResumeLayout(false);
            this.gbxAiport.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabAircraft.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.RichTextBox rtb;
        private System.Windows.Forms.RadioButton rbNormal;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.Button btnAnalyseAirport;
        private System.Windows.Forms.TextBox txtIcao;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TextBox txtXplaneLocation;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabAircraft;
        private System.Windows.Forms.RichTextBox rtbAircraft;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox gbxOutput;
        private System.Windows.Forms.GroupBox gbxAiport;
    }
}

