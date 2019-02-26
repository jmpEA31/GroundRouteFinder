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
            this.rtb = new System.Windows.Forms.RichTextBox();
            this.rbNormal = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.btnAnalyseAirport = new System.Windows.Forms.Button();
            this.txtIcao = new System.Windows.Forms.TextBox();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbxOverwriteAirportOperations = new System.Windows.Forms.CheckBox();
            this.cbxOverwriteParkingDefs = new System.Windows.Forms.CheckBox();
            this.cbxOverwriteOutboundRoutes = new System.Windows.Forms.CheckBox();
            this.cbxOverwriteInboundRoutes = new System.Windows.Forms.CheckBox();
            this.gbxOutput = new System.Windows.Forms.GroupBox();
            this.gbxAiport = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.cbxGenerateDebugFiles = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cbxOwOperationsDefault = new System.Windows.Forms.CheckBox();
            this.cbxOwParkingDefsDefault = new System.Windows.Forms.CheckBox();
            this.cbxOwOutboundDefault = new System.Windows.Forms.CheckBox();
            this.cbxOwInboundDefault = new System.Windows.Forms.CheckBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtXplaneLocation = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabAircraft = new System.Windows.Forms.TabPage();
            this.rtbAircraft = new System.Windows.Forms.RichTextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.gbxOutput.SuspendLayout();
            this.gbxAiport.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tabAircraft.SuspendLayout();
            this.SuspendLayout();
            // 
            // rtb
            // 
            this.rtb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtb.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtb.Location = new System.Drawing.Point(13, 172);
            this.rtb.Margin = new System.Windows.Forms.Padding(4);
            this.rtb.Name = "rtb";
            this.rtb.Size = new System.Drawing.Size(1620, 623);
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
            this.btnAnalyseAirport.Location = new System.Drawing.Point(67, 86);
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
            this.txtIcao.Size = new System.Drawing.Size(100, 22);
            this.txtIcao.TabIndex = 0;
            this.txtIcao.Text = "KDFW";
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(57, 86);
            this.btnGenerate.Margin = new System.Windows.Forms.Padding(4);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(100, 44);
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
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.gbxOutput);
            this.tabPage1.Controls.Add(this.gbxAiport);
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
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbxOverwriteAirportOperations);
            this.groupBox1.Controls.Add(this.cbxOverwriteParkingDefs);
            this.groupBox1.Controls.Add(this.cbxOverwriteOutboundRoutes);
            this.groupBox1.Controls.Add(this.cbxOverwriteInboundRoutes);
            this.groupBox1.Location = new System.Drawing.Point(219, 14);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Size = new System.Drawing.Size(215, 153);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Overwrite";
            // 
            // cbxOverwriteAirportOperations
            // 
            this.cbxOverwriteAirportOperations.AutoSize = true;
            this.cbxOverwriteAirportOperations.Location = new System.Drawing.Point(24, 110);
            this.cbxOverwriteAirportOperations.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxOverwriteAirportOperations.Name = "cbxOverwriteAirportOperations";
            this.cbxOverwriteAirportOperations.Size = new System.Drawing.Size(146, 21);
            this.cbxOverwriteAirportOperations.TabIndex = 3;
            this.cbxOverwriteAirportOperations.Text = "Airport Operations";
            this.cbxOverwriteAirportOperations.UseVisualStyleBackColor = true;
            // 
            // cbxOverwriteParkingDefs
            // 
            this.cbxOverwriteParkingDefs.AutoSize = true;
            this.cbxOverwriteParkingDefs.Location = new System.Drawing.Point(24, 82);
            this.cbxOverwriteParkingDefs.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxOverwriteParkingDefs.Name = "cbxOverwriteParkingDefs";
            this.cbxOverwriteParkingDefs.Size = new System.Drawing.Size(111, 21);
            this.cbxOverwriteParkingDefs.TabIndex = 2;
            this.cbxOverwriteParkingDefs.Text = "Parking Defs";
            this.cbxOverwriteParkingDefs.UseVisualStyleBackColor = true;
            // 
            // cbxOverwriteOutboundRoutes
            // 
            this.cbxOverwriteOutboundRoutes.AutoSize = true;
            this.cbxOverwriteOutboundRoutes.Location = new System.Drawing.Point(24, 55);
            this.cbxOverwriteOutboundRoutes.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxOverwriteOutboundRoutes.Name = "cbxOverwriteOutboundRoutes";
            this.cbxOverwriteOutboundRoutes.Size = new System.Drawing.Size(142, 21);
            this.cbxOverwriteOutboundRoutes.TabIndex = 1;
            this.cbxOverwriteOutboundRoutes.Text = "Outbound Routes";
            this.cbxOverwriteOutboundRoutes.UseVisualStyleBackColor = true;
            // 
            // cbxOverwriteInboundRoutes
            // 
            this.cbxOverwriteInboundRoutes.AutoSize = true;
            this.cbxOverwriteInboundRoutes.Location = new System.Drawing.Point(24, 28);
            this.cbxOverwriteInboundRoutes.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxOverwriteInboundRoutes.Name = "cbxOverwriteInboundRoutes";
            this.cbxOverwriteInboundRoutes.Size = new System.Drawing.Size(130, 21);
            this.cbxOverwriteInboundRoutes.TabIndex = 0;
            this.cbxOverwriteInboundRoutes.Text = "Inbound Routes";
            this.cbxOverwriteInboundRoutes.UseVisualStyleBackColor = true;
            // 
            // gbxOutput
            // 
            this.gbxOutput.Controls.Add(this.btnGenerate);
            this.gbxOutput.Controls.Add(this.rbNormal);
            this.gbxOutput.Controls.Add(this.radioButton2);
            this.gbxOutput.Location = new System.Drawing.Point(440, 14);
            this.gbxOutput.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbxOutput.Name = "gbxOutput";
            this.gbxOutput.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbxOutput.Size = new System.Drawing.Size(216, 153);
            this.gbxOutput.TabIndex = 10;
            this.gbxOutput.TabStop = false;
            this.gbxOutput.Text = "Output";
            // 
            // gbxAiport
            // 
            this.gbxAiport.Controls.Add(this.label2);
            this.gbxAiport.Controls.Add(this.btnAnalyseAirport);
            this.gbxAiport.Controls.Add(this.txtIcao);
            this.gbxAiport.Location = new System.Drawing.Point(13, 14);
            this.gbxAiport.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbxAiport.Name = "gbxAiport";
            this.gbxAiport.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbxAiport.Size = new System.Drawing.Size(200, 153);
            this.gbxAiport.TabIndex = 9;
            this.gbxAiport.TabStop = false;
            this.gbxAiport.Text = "Airport";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 17);
            this.label2.TabIndex = 7;
            this.label2.Text = "ICAO:";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox3);
            this.tabPage2.Controls.Add(this.groupBox2);
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
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.cbxGenerateDebugFiles);
            this.groupBox3.Location = new System.Drawing.Point(284, 62);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox3.Size = new System.Drawing.Size(267, 153);
            this.groupBox3.TabIndex = 13;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Debug Options";
            // 
            // cbxGenerateDebugFiles
            // 
            this.cbxGenerateDebugFiles.AutoSize = true;
            this.cbxGenerateDebugFiles.Location = new System.Drawing.Point(24, 28);
            this.cbxGenerateDebugFiles.Margin = new System.Windows.Forms.Padding(4);
            this.cbxGenerateDebugFiles.Name = "cbxGenerateDebugFiles";
            this.cbxGenerateDebugFiles.Size = new System.Drawing.Size(169, 21);
            this.cbxGenerateDebugFiles.TabIndex = 0;
            this.cbxGenerateDebugFiles.Text = "Generate Debug Files";
            this.cbxGenerateDebugFiles.UseVisualStyleBackColor = true;
            this.cbxGenerateDebugFiles.CheckedChanged += new System.EventHandler(this.chkGenerateDebugFiles_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cbxOwOperationsDefault);
            this.groupBox2.Controls.Add(this.cbxOwParkingDefsDefault);
            this.groupBox2.Controls.Add(this.cbxOwOutboundDefault);
            this.groupBox2.Controls.Add(this.cbxOwInboundDefault);
            this.groupBox2.Location = new System.Drawing.Point(12, 62);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox2.Size = new System.Drawing.Size(265, 153);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Overwrite Defaults";
            // 
            // cbxOwOperationsDefault
            // 
            this.cbxOwOperationsDefault.AutoSize = true;
            this.cbxOwOperationsDefault.Location = new System.Drawing.Point(24, 110);
            this.cbxOwOperationsDefault.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxOwOperationsDefault.Name = "cbxOwOperationsDefault";
            this.cbxOwOperationsDefault.Size = new System.Drawing.Size(146, 21);
            this.cbxOwOperationsDefault.TabIndex = 3;
            this.cbxOwOperationsDefault.Text = "Airport Operations";
            this.cbxOwOperationsDefault.UseVisualStyleBackColor = true;
            this.cbxOwOperationsDefault.CheckedChanged += new System.EventHandler(this.cbxOwOperationsDefault_CheckedChanged);
            // 
            // cbxOwParkingDefsDefault
            // 
            this.cbxOwParkingDefsDefault.AutoSize = true;
            this.cbxOwParkingDefsDefault.Location = new System.Drawing.Point(24, 82);
            this.cbxOwParkingDefsDefault.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxOwParkingDefsDefault.Name = "cbxOwParkingDefsDefault";
            this.cbxOwParkingDefsDefault.Size = new System.Drawing.Size(111, 21);
            this.cbxOwParkingDefsDefault.TabIndex = 2;
            this.cbxOwParkingDefsDefault.Text = "Parking Defs";
            this.cbxOwParkingDefsDefault.UseVisualStyleBackColor = true;
            this.cbxOwParkingDefsDefault.CheckedChanged += new System.EventHandler(this.cbxOwParkingDefsDefault_CheckedChanged);
            // 
            // cbxOwOutboundDefault
            // 
            this.cbxOwOutboundDefault.AutoSize = true;
            this.cbxOwOutboundDefault.Location = new System.Drawing.Point(24, 55);
            this.cbxOwOutboundDefault.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxOwOutboundDefault.Name = "cbxOwOutboundDefault";
            this.cbxOwOutboundDefault.Size = new System.Drawing.Size(142, 21);
            this.cbxOwOutboundDefault.TabIndex = 1;
            this.cbxOwOutboundDefault.Text = "Outbound Routes";
            this.cbxOwOutboundDefault.UseVisualStyleBackColor = true;
            this.cbxOwOutboundDefault.CheckedChanged += new System.EventHandler(this.cbxOwOutboundDefault_CheckedChanged);
            // 
            // cbxOwInboundDefault
            // 
            this.cbxOwInboundDefault.AutoSize = true;
            this.cbxOwInboundDefault.Location = new System.Drawing.Point(24, 28);
            this.cbxOwInboundDefault.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbxOwInboundDefault.Name = "cbxOwInboundDefault";
            this.cbxOwInboundDefault.Size = new System.Drawing.Size(130, 21);
            this.cbxOwInboundDefault.TabIndex = 0;
            this.cbxOwInboundDefault.Text = "Inbound Routes";
            this.cbxOwInboundDefault.UseVisualStyleBackColor = true;
            this.cbxOwInboundDefault.CheckedChanged += new System.EventHandler(this.cbxOwInboundDefault_CheckedChanged);
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
            this.btnExit.Location = new System.Drawing.Point(1541, 853);
            this.btnExit.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
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
            this.Text = "Ground Route Generator 0.3";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gbxOutput.ResumeLayout(false);
            this.gbxOutput.PerformLayout();
            this.gbxAiport.ResumeLayout(false);
            this.gbxAiport.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.tabAircraft.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
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
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox cbxOverwriteAirportOperations;
        private System.Windows.Forms.CheckBox cbxOverwriteParkingDefs;
        private System.Windows.Forms.CheckBox cbxOverwriteOutboundRoutes;
        private System.Windows.Forms.CheckBox cbxOverwriteInboundRoutes;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox cbxOwOperationsDefault;
        private System.Windows.Forms.CheckBox cbxOwParkingDefsDefault;
        private System.Windows.Forms.CheckBox cbxOwOutboundDefault;
        private System.Windows.Forms.CheckBox cbxOwInboundDefault;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox cbxGenerateDebugFiles;
    }
}

