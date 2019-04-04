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
            this.gbxProgress = new System.Windows.Forms.GroupBox();
            this.progressOutbound = new System.Windows.Forms.ProgressBar();
            this.label4 = new System.Windows.Forms.Label();
            this.progressInbound = new System.Windows.Forms.ProgressBar();
            this.label3 = new System.Windows.Forms.Label();
            this.debugGroupBox = new System.Windows.Forms.GroupBox();
            this.btnPrevLog = new System.Windows.Forms.Button();
            this.btnRunTestSet = new System.Windows.Forms.Button();
            this.btnShowLogFile = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbxOverwriteAirportOperations = new System.Windows.Forms.CheckBox();
            this.cbxOverwriteParkingDefs = new System.Windows.Forms.CheckBox();
            this.cbxOverwriteOutboundRoutes = new System.Windows.Forms.CheckBox();
            this.cbxOverwriteInboundRoutes = new System.Windows.Forms.CheckBox();
            this.gbxOutput = new System.Windows.Forms.GroupBox();
            this.gbxAiport = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.conversionLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cbxParkingReference = new System.Windows.Forms.ComboBox();
            this.cbxFixDuplicateParkingNames = new System.Windows.Forms.CheckBox();
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
            this.btnAnalyseAircraft = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.gbxProgress.SuspendLayout();
            this.debugGroupBox.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.gbxOutput.SuspendLayout();
            this.gbxAiport.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
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
            this.rtb.Location = new System.Drawing.Point(10, 140);
            this.rtb.Name = "rtb";
            this.rtb.Size = new System.Drawing.Size(1216, 507);
            this.rtb.TabIndex = 1;
            this.rtb.Text = "";
            // 
            // rbNormal
            // 
            this.rbNormal.AutoSize = true;
            this.rbNormal.Checked = true;
            this.rbNormal.Location = new System.Drawing.Point(19, 23);
            this.rbNormal.Name = "rbNormal";
            this.rbNormal.Size = new System.Drawing.Size(123, 17);
            this.rbNormal.TabIndex = 2;
            this.rbNormal.TabStop = true;
            this.rbNormal.Text = "World Traffic Routes";
            this.rbNormal.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(19, 46);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(47, 17);
            this.radioButton2.TabIndex = 3;
            this.radioButton2.Text = "KML";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // btnAnalyseAirport
            // 
            this.btnAnalyseAirport.Location = new System.Drawing.Point(50, 70);
            this.btnAnalyseAirport.Name = "btnAnalyseAirport";
            this.btnAnalyseAirport.Size = new System.Drawing.Size(75, 23);
            this.btnAnalyseAirport.TabIndex = 1;
            this.btnAnalyseAirport.Text = "Analyse";
            this.btnAnalyseAirport.UseVisualStyleBackColor = true;
            this.btnAnalyseAirport.Click += new System.EventHandler(this.btnAnalyze_Click);
            // 
            // txtIcao
            // 
            this.txtIcao.Location = new System.Drawing.Point(50, 33);
            this.txtIcao.Name = "txtIcao";
            this.txtIcao.Size = new System.Drawing.Size(76, 20);
            this.txtIcao.TabIndex = 0;
            this.txtIcao.Text = "KIAH";
            this.txtIcao.TextChanged += new System.EventHandler(this.txtIcao_TextChanged);
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(43, 70);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(75, 36);
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
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1247, 686);
            this.tabControl1.TabIndex = 7;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.gbxProgress);
            this.tabPage1.Controls.Add(this.debugGroupBox);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.gbxOutput);
            this.tabPage1.Controls.Add(this.gbxAiport);
            this.tabPage1.Controls.Add(this.rtb);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabPage1.Size = new System.Drawing.Size(1239, 660);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Generator";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // gbxProgress
            // 
            this.gbxProgress.Controls.Add(this.progressOutbound);
            this.gbxProgress.Controls.Add(this.label4);
            this.gbxProgress.Controls.Add(this.progressInbound);
            this.gbxProgress.Controls.Add(this.label3);
            this.gbxProgress.Location = new System.Drawing.Point(663, 14);
            this.gbxProgress.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gbxProgress.Name = "gbxProgress";
            this.gbxProgress.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gbxProgress.Size = new System.Drawing.Size(436, 120);
            this.gbxProgress.TabIndex = 16;
            this.gbxProgress.TabStop = false;
            this.gbxProgress.Text = "Progress";
            // 
            // progressOutbound
            // 
            this.progressOutbound.Location = new System.Drawing.Point(21, 42);
            this.progressOutbound.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.progressOutbound.Name = "progressOutbound";
            this.progressOutbound.Size = new System.Drawing.Size(401, 19);
            this.progressOutbound.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressOutbound.TabIndex = 12;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(21, 67);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(46, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "Inbound";
            // 
            // progressInbound
            // 
            this.progressInbound.Location = new System.Drawing.Point(21, 83);
            this.progressInbound.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.progressInbound.Name = "progressInbound";
            this.progressInbound.Size = new System.Drawing.Size(401, 19);
            this.progressInbound.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressInbound.TabIndex = 13;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(19, 26);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Outbound";
            // 
            // debugGroupBox
            // 
            this.debugGroupBox.Controls.Add(this.btnPrevLog);
            this.debugGroupBox.Controls.Add(this.btnRunTestSet);
            this.debugGroupBox.Controls.Add(this.btnShowLogFile);
            this.debugGroupBox.Location = new System.Drawing.Point(496, 11);
            this.debugGroupBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.debugGroupBox.Name = "debugGroupBox";
            this.debugGroupBox.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.debugGroupBox.Size = new System.Drawing.Size(162, 124);
            this.debugGroupBox.TabIndex = 11;
            this.debugGroupBox.TabStop = false;
            this.debugGroupBox.Text = "Debug";
            // 
            // btnPrevLog
            // 
            this.btnPrevLog.Location = new System.Drawing.Point(17, 54);
            this.btnPrevLog.Name = "btnPrevLog";
            this.btnPrevLog.Size = new System.Drawing.Size(132, 25);
            this.btnPrevLog.TabIndex = 8;
            this.btnPrevLog.Text = "Open Prev Log file";
            this.btnPrevLog.UseVisualStyleBackColor = true;
            this.btnPrevLog.Click += new System.EventHandler(this.btnPrevLog_Click);
            // 
            // btnRunTestSet
            // 
            this.btnRunTestSet.Location = new System.Drawing.Point(17, 85);
            this.btnRunTestSet.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnRunTestSet.Name = "btnRunTestSet";
            this.btnRunTestSet.Size = new System.Drawing.Size(132, 25);
            this.btnRunTestSet.TabIndex = 7;
            this.btnRunTestSet.Text = "Run Tests";
            this.btnRunTestSet.UseVisualStyleBackColor = true;
            this.btnRunTestSet.Visible = false;
            this.btnRunTestSet.Click += new System.EventHandler(this.btnRunTestSet_Click);
            // 
            // btnShowLogFile
            // 
            this.btnShowLogFile.Location = new System.Drawing.Point(17, 23);
            this.btnShowLogFile.Name = "btnShowLogFile";
            this.btnShowLogFile.Size = new System.Drawing.Size(132, 25);
            this.btnShowLogFile.TabIndex = 6;
            this.btnShowLogFile.Text = "Open Log file";
            this.btnShowLogFile.UseVisualStyleBackColor = true;
            this.btnShowLogFile.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnShowLogFile_MouseUp);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbxOverwriteAirportOperations);
            this.groupBox1.Controls.Add(this.cbxOverwriteParkingDefs);
            this.groupBox1.Controls.Add(this.cbxOverwriteOutboundRoutes);
            this.groupBox1.Controls.Add(this.cbxOverwriteInboundRoutes);
            this.groupBox1.Location = new System.Drawing.Point(164, 11);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox1.Size = new System.Drawing.Size(161, 124);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Overwrite";
            // 
            // cbxOverwriteAirportOperations
            // 
            this.cbxOverwriteAirportOperations.AutoSize = true;
            this.cbxOverwriteAirportOperations.Location = new System.Drawing.Point(18, 89);
            this.cbxOverwriteAirportOperations.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxOverwriteAirportOperations.Name = "cbxOverwriteAirportOperations";
            this.cbxOverwriteAirportOperations.Size = new System.Drawing.Size(110, 17);
            this.cbxOverwriteAirportOperations.TabIndex = 3;
            this.cbxOverwriteAirportOperations.Text = "Airport Operations";
            this.cbxOverwriteAirportOperations.UseVisualStyleBackColor = true;
            // 
            // cbxOverwriteParkingDefs
            // 
            this.cbxOverwriteParkingDefs.AutoSize = true;
            this.cbxOverwriteParkingDefs.Location = new System.Drawing.Point(18, 67);
            this.cbxOverwriteParkingDefs.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxOverwriteParkingDefs.Name = "cbxOverwriteParkingDefs";
            this.cbxOverwriteParkingDefs.Size = new System.Drawing.Size(87, 17);
            this.cbxOverwriteParkingDefs.TabIndex = 2;
            this.cbxOverwriteParkingDefs.Text = "Parking Defs";
            this.cbxOverwriteParkingDefs.UseVisualStyleBackColor = true;
            // 
            // cbxOverwriteOutboundRoutes
            // 
            this.cbxOverwriteOutboundRoutes.AutoSize = true;
            this.cbxOverwriteOutboundRoutes.Location = new System.Drawing.Point(18, 45);
            this.cbxOverwriteOutboundRoutes.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxOverwriteOutboundRoutes.Name = "cbxOverwriteOutboundRoutes";
            this.cbxOverwriteOutboundRoutes.Size = new System.Drawing.Size(110, 17);
            this.cbxOverwriteOutboundRoutes.TabIndex = 1;
            this.cbxOverwriteOutboundRoutes.Text = "Outbound Routes";
            this.cbxOverwriteOutboundRoutes.UseVisualStyleBackColor = true;
            // 
            // cbxOverwriteInboundRoutes
            // 
            this.cbxOverwriteInboundRoutes.AutoSize = true;
            this.cbxOverwriteInboundRoutes.Location = new System.Drawing.Point(18, 23);
            this.cbxOverwriteInboundRoutes.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxOverwriteInboundRoutes.Name = "cbxOverwriteInboundRoutes";
            this.cbxOverwriteInboundRoutes.Size = new System.Drawing.Size(102, 17);
            this.cbxOverwriteInboundRoutes.TabIndex = 0;
            this.cbxOverwriteInboundRoutes.Text = "Inbound Routes";
            this.cbxOverwriteInboundRoutes.UseVisualStyleBackColor = true;
            // 
            // gbxOutput
            // 
            this.gbxOutput.Controls.Add(this.btnGenerate);
            this.gbxOutput.Controls.Add(this.rbNormal);
            this.gbxOutput.Controls.Add(this.radioButton2);
            this.gbxOutput.Location = new System.Drawing.Point(330, 11);
            this.gbxOutput.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gbxOutput.Name = "gbxOutput";
            this.gbxOutput.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gbxOutput.Size = new System.Drawing.Size(162, 124);
            this.gbxOutput.TabIndex = 10;
            this.gbxOutput.TabStop = false;
            this.gbxOutput.Text = "Output";
            // 
            // gbxAiport
            // 
            this.gbxAiport.Controls.Add(this.label2);
            this.gbxAiport.Controls.Add(this.btnAnalyseAirport);
            this.gbxAiport.Controls.Add(this.txtIcao);
            this.gbxAiport.Location = new System.Drawing.Point(10, 11);
            this.gbxAiport.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gbxAiport.Name = "gbxAiport";
            this.gbxAiport.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.gbxAiport.Size = new System.Drawing.Size(150, 124);
            this.gbxAiport.TabIndex = 9;
            this.gbxAiport.TabStop = false;
            this.gbxAiport.Text = "Airport";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 34);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "ICAO:";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox4);
            this.tabPage2.Controls.Add(this.groupBox5);
            this.tabPage2.Controls.Add(this.groupBox3);
            this.tabPage2.Controls.Add(this.groupBox2);
            this.tabPage2.Controls.Add(this.btnBrowse);
            this.tabPage2.Controls.Add(this.txtXplaneLocation);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabPage2.Size = new System.Drawing.Size(1239, 660);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Settings";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.button2);
            this.groupBox4.Controls.Add(this.button1);
            this.groupBox4.Controls.Add(this.conversionLayoutPanel);
            this.groupBox4.Location = new System.Drawing.Point(418, 50);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox4.Size = new System.Drawing.Size(723, 469);
            this.groupBox4.TabIndex = 15;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Aircraft Mapping";
            this.groupBox4.Visible = false;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(139, 362);
            this.button2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(106, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Set Defaults++";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(16, 362);
            this.button1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(106, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Set Strict Defaults";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // conversionLayoutPanel
            // 
            this.conversionLayoutPanel.ColumnCount = 2;
            this.conversionLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.conversionLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.conversionLayoutPanel.Location = new System.Drawing.Point(5, 20);
            this.conversionLayoutPanel.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.conversionLayoutPanel.Name = "conversionLayoutPanel";
            this.conversionLayoutPanel.RowCount = 2;
            this.conversionLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.conversionLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.conversionLayoutPanel.Size = new System.Drawing.Size(713, 326);
            this.conversionLayoutPanel.TabIndex = 0;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.label5);
            this.groupBox5.Controls.Add(this.cbxParkingReference);
            this.groupBox5.Controls.Add(this.cbxFixDuplicateParkingNames);
            this.groupBox5.Location = new System.Drawing.Point(9, 187);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox5.Size = new System.Drawing.Size(404, 180);
            this.groupBox5.TabIndex = 14;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "General";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(34, 63);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Parking Reference";
            // 
            // cbxParkingReference
            // 
            this.cbxParkingReference.FormattingEnabled = true;
            this.cbxParkingReference.Location = new System.Drawing.Point(140, 61);
            this.cbxParkingReference.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxParkingReference.Name = "cbxParkingReference";
            this.cbxParkingReference.Size = new System.Drawing.Size(108, 21);
            this.cbxParkingReference.TabIndex = 1;
            this.cbxParkingReference.SelectedIndexChanged += new System.EventHandler(this.cbxParkingReference_SelectedIndexChanged);
            // 
            // cbxFixDuplicateParkingNames
            // 
            this.cbxFixDuplicateParkingNames.AutoSize = true;
            this.cbxFixDuplicateParkingNames.Location = new System.Drawing.Point(18, 30);
            this.cbxFixDuplicateParkingNames.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxFixDuplicateParkingNames.Name = "cbxFixDuplicateParkingNames";
            this.cbxFixDuplicateParkingNames.Size = new System.Drawing.Size(157, 17);
            this.cbxFixDuplicateParkingNames.TabIndex = 0;
            this.cbxFixDuplicateParkingNames.Text = "Fix duplicate parking names";
            this.cbxFixDuplicateParkingNames.UseVisualStyleBackColor = true;
            this.cbxFixDuplicateParkingNames.CheckedChanged += new System.EventHandler(this.cbxFixDuplicateParkingNames_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.cbxGenerateDebugFiles);
            this.groupBox3.Location = new System.Drawing.Point(213, 50);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(200, 124);
            this.groupBox3.TabIndex = 13;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Debug Options";
            // 
            // cbxGenerateDebugFiles
            // 
            this.cbxGenerateDebugFiles.AutoSize = true;
            this.cbxGenerateDebugFiles.Location = new System.Drawing.Point(18, 23);
            this.cbxGenerateDebugFiles.Name = "cbxGenerateDebugFiles";
            this.cbxGenerateDebugFiles.Size = new System.Drawing.Size(129, 17);
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
            this.groupBox2.Location = new System.Drawing.Point(9, 50);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox2.Size = new System.Drawing.Size(199, 124);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Overwrite Defaults";
            // 
            // cbxOwOperationsDefault
            // 
            this.cbxOwOperationsDefault.AutoSize = true;
            this.cbxOwOperationsDefault.Location = new System.Drawing.Point(18, 89);
            this.cbxOwOperationsDefault.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxOwOperationsDefault.Name = "cbxOwOperationsDefault";
            this.cbxOwOperationsDefault.Size = new System.Drawing.Size(110, 17);
            this.cbxOwOperationsDefault.TabIndex = 3;
            this.cbxOwOperationsDefault.Text = "Airport Operations";
            this.cbxOwOperationsDefault.UseVisualStyleBackColor = true;
            this.cbxOwOperationsDefault.CheckedChanged += new System.EventHandler(this.cbxOwOperationsDefault_CheckedChanged);
            // 
            // cbxOwParkingDefsDefault
            // 
            this.cbxOwParkingDefsDefault.AutoSize = true;
            this.cbxOwParkingDefsDefault.Location = new System.Drawing.Point(18, 67);
            this.cbxOwParkingDefsDefault.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxOwParkingDefsDefault.Name = "cbxOwParkingDefsDefault";
            this.cbxOwParkingDefsDefault.Size = new System.Drawing.Size(87, 17);
            this.cbxOwParkingDefsDefault.TabIndex = 2;
            this.cbxOwParkingDefsDefault.Text = "Parking Defs";
            this.cbxOwParkingDefsDefault.UseVisualStyleBackColor = true;
            this.cbxOwParkingDefsDefault.CheckedChanged += new System.EventHandler(this.cbxOwParkingDefsDefault_CheckedChanged);
            // 
            // cbxOwOutboundDefault
            // 
            this.cbxOwOutboundDefault.AutoSize = true;
            this.cbxOwOutboundDefault.Location = new System.Drawing.Point(18, 45);
            this.cbxOwOutboundDefault.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxOwOutboundDefault.Name = "cbxOwOutboundDefault";
            this.cbxOwOutboundDefault.Size = new System.Drawing.Size(110, 17);
            this.cbxOwOutboundDefault.TabIndex = 1;
            this.cbxOwOutboundDefault.Text = "Outbound Routes";
            this.cbxOwOutboundDefault.UseVisualStyleBackColor = true;
            this.cbxOwOutboundDefault.CheckedChanged += new System.EventHandler(this.cbxOwOutboundDefault_CheckedChanged);
            // 
            // cbxOwInboundDefault
            // 
            this.cbxOwInboundDefault.AutoSize = true;
            this.cbxOwInboundDefault.Location = new System.Drawing.Point(18, 23);
            this.cbxOwInboundDefault.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cbxOwInboundDefault.Name = "cbxOwInboundDefault";
            this.cbxOwInboundDefault.Size = new System.Drawing.Size(102, 17);
            this.cbxOwInboundDefault.TabIndex = 0;
            this.cbxOwInboundDefault.Text = "Inbound Routes";
            this.cbxOwInboundDefault.UseVisualStyleBackColor = true;
            this.cbxOwInboundDefault.CheckedChanged += new System.EventHandler(this.cbxOwInboundDefault_CheckedChanged);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(509, 8);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // txtXplaneLocation
            // 
            this.txtXplaneLocation.Location = new System.Drawing.Point(89, 10);
            this.txtXplaneLocation.Name = "txtXplaneLocation";
            this.txtXplaneLocation.Size = new System.Drawing.Size(414, 20);
            this.txtXplaneLocation.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "X Plane folder:";
            // 
            // tabAircraft
            // 
            this.tabAircraft.Controls.Add(this.rtbAircraft);
            this.tabAircraft.Controls.Add(this.btnAnalyseAircraft);
            this.tabAircraft.Location = new System.Drawing.Point(4, 22);
            this.tabAircraft.Name = "tabAircraft";
            this.tabAircraft.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabAircraft.Size = new System.Drawing.Size(1239, 660);
            this.tabAircraft.TabIndex = 2;
            this.tabAircraft.Text = "Aircraft Analysis";
            this.tabAircraft.UseVisualStyleBackColor = true;
            // 
            // rtbAircraft
            // 
            this.rtbAircraft.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbAircraft.Location = new System.Drawing.Point(14, 36);
            this.rtbAircraft.Name = "rtbAircraft";
            this.rtbAircraft.Size = new System.Drawing.Size(1172, 602);
            this.rtbAircraft.TabIndex = 1;
            this.rtbAircraft.Text = "";
            // 
            // btnAnalyseAircraft
            // 
            this.btnAnalyseAircraft.Location = new System.Drawing.Point(14, 6);
            this.btnAnalyseAircraft.Name = "btnAnalyseAircraft";
            this.btnAnalyseAircraft.Size = new System.Drawing.Size(135, 23);
            this.btnAnalyseAircraft.TabIndex = 0;
            this.btnAnalyseAircraft.Text = "Analyse \'Base\' Aircraft";
            this.btnAnalyseAircraft.UseVisualStyleBackColor = true;
            this.btnAnalyseAircraft.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnExit
            // 
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.Location = new System.Drawing.Point(1156, 693);
            this.btnExit.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(86, 29);
            this.btnExit.TabIndex = 8;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1252, 732);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.tabControl1);
            this.Name = "MainForm";
            this.Text = "Ground Route Generator 0.6";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.gbxProgress.ResumeLayout(false);
            this.gbxProgress.PerformLayout();
            this.debugGroupBox.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gbxOutput.ResumeLayout(false);
            this.gbxOutput.PerformLayout();
            this.gbxAiport.ResumeLayout(false);
            this.gbxAiport.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
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
        private System.Windows.Forms.Button btnAnalyseAircraft;
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
        private System.Windows.Forms.GroupBox debugGroupBox;
        private System.Windows.Forms.Button btnShowLogFile;
        private System.Windows.Forms.ProgressBar progressOutbound;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ProgressBar progressInbound;
        private System.Windows.Forms.GroupBox gbxProgress;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox cbxFixDuplicateParkingNames;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbxParkingReference;
        private System.Windows.Forms.Button btnRunTestSet;
        private System.Windows.Forms.Button btnPrevLog;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TableLayoutPanel conversionLayoutPanel;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
    }
}

