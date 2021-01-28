namespace ShpToMapboxVT
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
            if (disposing && cancellationTokenSource != null)
            {                
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();                
                cancellationTokenSource = null;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.label1 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.txtInputShapeFile = new System.Windows.Forms.TextBox();
            this.btnBrowseShapeFile = new System.Windows.Forms.Button();
            this.btnBrowseOutputDirectory = new System.Windows.Forms.Button();
            this.txtMbtilesFilePathname = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.nudStartZoom = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.nudEndZoom = new System.Windows.Forms.NumericUpDown();
            this.btnProcess = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnDisplay = new System.Windows.Forms.Button();
            this.rtbOutput = new System.Windows.Forms.RichTextBox();
            this.btnSelectNoAttributes = new System.Windows.Forms.Button();
            this.btnSelectAllAttributes = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.clbSelectedAttributes = new System.Windows.Forms.CheckedListBox();
            this.ofdShapeFile = new System.Windows.Forms.OpenFileDialog();
            this.sfdMbtiles = new System.Windows.Forms.SaveFileDialog();
            this.mapView1 = new ThinkGeo.UI.WinForms.MapView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ckbDisplayShapeFile = new System.Windows.Forms.CheckBox();
            this.ckbDisplayMbtiles = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.nudStartZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEndZoom)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Input ShapeFile";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1204, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // txtInputShapeFile
            // 
            this.txtInputShapeFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInputShapeFile.Location = new System.Drawing.Point(93, 22);
            this.txtInputShapeFile.Name = "txtInputShapeFile";
            this.txtInputShapeFile.Size = new System.Drawing.Size(1051, 20);
            this.txtInputShapeFile.TabIndex = 2;
            // 
            // btnBrowseShapeFile
            // 
            this.btnBrowseShapeFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseShapeFile.Location = new System.Drawing.Point(1150, 21);
            this.btnBrowseShapeFile.Name = "btnBrowseShapeFile";
            this.btnBrowseShapeFile.Size = new System.Drawing.Size(24, 23);
            this.btnBrowseShapeFile.TabIndex = 3;
            this.btnBrowseShapeFile.Text = "...";
            this.btnBrowseShapeFile.UseVisualStyleBackColor = true;
            this.btnBrowseShapeFile.Click += new System.EventHandler(this.btnBrowseShapeFile_Click);
            // 
            // btnBrowseOutputDirectory
            // 
            this.btnBrowseOutputDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseOutputDirectory.Location = new System.Drawing.Point(1150, 46);
            this.btnBrowseOutputDirectory.Name = "btnBrowseOutputDirectory";
            this.btnBrowseOutputDirectory.Size = new System.Drawing.Size(24, 23);
            this.btnBrowseOutputDirectory.TabIndex = 6;
            this.btnBrowseOutputDirectory.Text = "...";
            this.btnBrowseOutputDirectory.UseVisualStyleBackColor = true;
            this.btnBrowseOutputDirectory.Click += new System.EventHandler(this.btnBrowseOutputFilePathname_Click);
            // 
            // txtMbtilesFilePathname
            // 
            this.txtMbtilesFilePathname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMbtilesFilePathname.Location = new System.Drawing.Point(93, 48);
            this.txtMbtilesFilePathname.Name = "txtMbtilesFilePathname";
            this.txtMbtilesFilePathname.Size = new System.Drawing.Size(1051, 20);
            this.txtMbtilesFilePathname.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Output mbtiles";
            // 
            // nudStartZoom
            // 
            this.nudStartZoom.Location = new System.Drawing.Point(93, 92);
            this.nudStartZoom.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nudStartZoom.Name = "nudStartZoom";
            this.nudStartZoom.Size = new System.Drawing.Size(63, 20);
            this.nudStartZoom.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 96);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Start Zoom";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 118);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "End Zoom";
            // 
            // nudEndZoom
            // 
            this.nudEndZoom.Location = new System.Drawing.Point(93, 116);
            this.nudEndZoom.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nudEndZoom.Name = "nudEndZoom";
            this.nudEndZoom.Size = new System.Drawing.Size(63, 20);
            this.nudEndZoom.TabIndex = 9;
            this.nudEndZoom.Value = new decimal(new int[] {
            14,
            0,
            0,
            0});
            // 
            // btnProcess
            // 
            this.btnProcess.Location = new System.Drawing.Point(12, 142);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(144, 23);
            this.btnProcess.TabIndex = 11;
            this.btnProcess.Text = "Process";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.btnDisplay);
            this.groupBox1.Controls.Add(this.rtbOutput);
            this.groupBox1.Controls.Add(this.btnSelectNoAttributes);
            this.groupBox1.Controls.Add(this.btnSelectAllAttributes);
            this.groupBox1.Controls.Add(this.btnProcess);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.clbSelectedAttributes);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtInputShapeFile);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.btnBrowseShapeFile);
            this.groupBox1.Controls.Add(this.nudEndZoom);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtMbtilesFilePathname);
            this.groupBox1.Controls.Add(this.nudStartZoom);
            this.groupBox1.Controls.Add(this.btnBrowseOutputDirectory);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1180, 204);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Options";
            // 
            // btnDisplay
            // 
            this.btnDisplay.Location = new System.Drawing.Point(12, 170);
            this.btnDisplay.Name = "btnDisplay";
            this.btnDisplay.Size = new System.Drawing.Size(144, 23);
            this.btnDisplay.TabIndex = 17;
            this.btnDisplay.Text = "Display ";
            this.btnDisplay.UseVisualStyleBackColor = true;
            this.btnDisplay.Click += new System.EventHandler(this.btnDisplay_Click);
            // 
            // rtbOutput
            // 
            this.rtbOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbOutput.Location = new System.Drawing.Point(469, 83);
            this.rtbOutput.Name = "rtbOutput";
            this.rtbOutput.Size = new System.Drawing.Size(705, 110);
            this.rtbOutput.TabIndex = 13;
            this.rtbOutput.Text = "";
            // 
            // btnSelectNoAttributes
            // 
            this.btnSelectNoAttributes.Location = new System.Drawing.Point(388, 128);
            this.btnSelectNoAttributes.Name = "btnSelectNoAttributes";
            this.btnSelectNoAttributes.Size = new System.Drawing.Size(75, 23);
            this.btnSelectNoAttributes.TabIndex = 16;
            this.btnSelectNoAttributes.Text = "Select None";
            this.btnSelectNoAttributes.UseVisualStyleBackColor = true;
            this.btnSelectNoAttributes.Click += new System.EventHandler(this.btnSelectNoAttributes_Click);
            // 
            // btnSelectAllAttributes
            // 
            this.btnSelectAllAttributes.Location = new System.Drawing.Point(388, 99);
            this.btnSelectAllAttributes.Name = "btnSelectAllAttributes";
            this.btnSelectAllAttributes.Size = new System.Drawing.Size(75, 23);
            this.btnSelectAllAttributes.TabIndex = 15;
            this.btnSelectAllAttributes.Text = "Select All";
            this.btnSelectAllAttributes.UseVisualStyleBackColor = true;
            this.btnSelectAllAttributes.Click += new System.EventHandler(this.btnSelectAllAttributes_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(181, 83);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(124, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "Select attribues to export";
            // 
            // clbSelectedAttributes
            // 
            this.clbSelectedAttributes.FormattingEnabled = true;
            this.clbSelectedAttributes.Location = new System.Drawing.Point(174, 99);
            this.clbSelectedAttributes.Name = "clbSelectedAttributes";
            this.clbSelectedAttributes.Size = new System.Drawing.Size(208, 94);
            this.clbSelectedAttributes.TabIndex = 12;
            // 
            // ofdShapeFile
            // 
            this.ofdShapeFile.Filter = "ShapeFile(*.shp)|*.shp";
            this.ofdShapeFile.Multiselect = true;
            this.ofdShapeFile.Title = "Select Input ShapeFile";
            // 
            // mapView1
            // 
            this.mapView1.BackColor = System.Drawing.Color.White;
            this.mapView1.CurrentScale = 0D;
            this.mapView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapView1.Location = new System.Drawing.Point(3, 16);
            this.mapView1.MapFocusMode = ThinkGeo.Core.MapFocusMode.Default;
            this.mapView1.MapResizeMode = ThinkGeo.Core.MapResizeMode.PreserveScale;
            this.mapView1.MaximumScale = 1.7976931348623157E+308D;
            this.mapView1.MinimumScale = 200D;
            this.mapView1.Name = "mapView1";
            this.mapView1.RestrictExtent = null;
            this.mapView1.RotatedAngle = 0F;
            this.mapView1.Size = new System.Drawing.Size(1186, 499);
            this.mapView1.TabIndex = 13;
            this.mapView1.Text = "mapView1";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.ckbDisplayMbtiles);
            this.groupBox2.Controls.Add(this.ckbDisplayShapeFile);
            this.groupBox2.Controls.Add(this.mapView1);
            this.groupBox2.Location = new System.Drawing.Point(12, 223);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1192, 518);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "groupBox2";
            // 
            // ckbDisplayShapeFile
            // 
            this.ckbDisplayShapeFile.AutoSize = true;
            this.ckbDisplayShapeFile.Checked = true;
            this.ckbDisplayShapeFile.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ckbDisplayShapeFile.Enabled = false;
            this.ckbDisplayShapeFile.Location = new System.Drawing.Point(1008, 36);
            this.ckbDisplayShapeFile.Name = "ckbDisplayShapeFile";
            this.ckbDisplayShapeFile.Size = new System.Drawing.Size(113, 17);
            this.ckbDisplayShapeFile.TabIndex = 14;
            this.ckbDisplayShapeFile.Text = "Display Shape File";
            this.ckbDisplayShapeFile.UseVisualStyleBackColor = true;
            this.ckbDisplayShapeFile.CheckedChanged += new System.EventHandler(this.ckbDisplayMbtiles_CheckedChanged);
            // 
            // ckbDisplayMbtiles
            // 
            this.ckbDisplayMbtiles.AutoSize = true;
            this.ckbDisplayMbtiles.Checked = true;
            this.ckbDisplayMbtiles.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ckbDisplayMbtiles.Enabled = false;
            this.ckbDisplayMbtiles.Location = new System.Drawing.Point(1008, 59);
            this.ckbDisplayMbtiles.Name = "ckbDisplayMbtiles";
            this.ckbDisplayMbtiles.Size = new System.Drawing.Size(96, 17);
            this.ckbDisplayMbtiles.TabIndex = 15;
            this.ckbDisplayMbtiles.Text = "Display Mbtiles";
            this.ckbDisplayMbtiles.UseVisualStyleBackColor = true;
            this.ckbDisplayMbtiles.CheckedChanged += new System.EventHandler(this.ckbDisplayMbtiles_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1204, 753);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "ShapeFile Mapbox Vector Tile Generator";
            ((System.ComponentModel.ISupportInitialize)(this.nudStartZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEndZoom)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.TextBox txtInputShapeFile;
        private System.Windows.Forms.Button btnBrowseShapeFile;
        private System.Windows.Forms.Button btnBrowseOutputDirectory;
        private System.Windows.Forms.TextBox txtMbtilesFilePathname;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudStartZoom;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nudEndZoom;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RichTextBox rtbOutput;
        private System.Windows.Forms.OpenFileDialog ofdShapeFile;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckedListBox clbSelectedAttributes;
        private System.Windows.Forms.Button btnSelectNoAttributes;
        private System.Windows.Forms.Button btnSelectAllAttributes;
        private System.Windows.Forms.SaveFileDialog sfdMbtiles;
        private ThinkGeo.UI.WinForms.MapView mapView1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnDisplay;
        private System.Windows.Forms.CheckBox ckbDisplayMbtiles;
        private System.Windows.Forms.CheckBox ckbDisplayShapeFile;
    }
}

