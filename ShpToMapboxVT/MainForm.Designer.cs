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
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.txtInputShapeFile = new System.Windows.Forms.TextBox();
            this.btnBrowseShapeFile = new System.Windows.Forms.Button();
            this.btnBrowseOutputDirectory = new System.Windows.Forms.Button();
            this.txtOutputDirectory = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.nudStartZoom = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.nudEndZoom = new System.Windows.Forms.NumericUpDown();
            this.btnProcess = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnSelectNoAttributes = new System.Windows.Forms.Button();
            this.btnSelectAllAttributes = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.clbSelectedAttributes = new System.Windows.Forms.CheckedListBox();
            this.chkExportAttributesToSeparateFile = new System.Windows.Forms.CheckBox();
            this.rtbOutput = new System.Windows.Forms.RichTextBox();
            this.ofdShapeFile = new System.Windows.Forms.OpenFileDialog();
            this.fbdOutput = new System.Windows.Forms.FolderBrowserDialog();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStartZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEndZoom)).BeginInit();
            this.groupBox1.SuspendLayout();
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
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(615, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            // 
            // txtInputShapeFile
            // 
            this.txtInputShapeFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInputShapeFile.Location = new System.Drawing.Point(93, 22);
            this.txtInputShapeFile.Name = "txtInputShapeFile";
            this.txtInputShapeFile.Size = new System.Drawing.Size(411, 20);
            this.txtInputShapeFile.TabIndex = 2;
            // 
            // btnBrowseShapeFile
            // 
            this.btnBrowseShapeFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseShapeFile.Location = new System.Drawing.Point(510, 21);
            this.btnBrowseShapeFile.Name = "btnBrowseShapeFile";
            this.btnBrowseShapeFile.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseShapeFile.TabIndex = 3;
            this.btnBrowseShapeFile.Text = "Browse";
            this.btnBrowseShapeFile.UseVisualStyleBackColor = true;
            this.btnBrowseShapeFile.Click += new System.EventHandler(this.btnBrowseShapeFile_Click);
            // 
            // btnBrowseOutputDirectory
            // 
            this.btnBrowseOutputDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseOutputDirectory.Location = new System.Drawing.Point(510, 56);
            this.btnBrowseOutputDirectory.Name = "btnBrowseOutputDirectory";
            this.btnBrowseOutputDirectory.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseOutputDirectory.TabIndex = 6;
            this.btnBrowseOutputDirectory.Text = "Browse";
            this.btnBrowseOutputDirectory.UseVisualStyleBackColor = true;
            this.btnBrowseOutputDirectory.Click += new System.EventHandler(this.btnBrowseOutputDirectory_Click);
            // 
            // txtOutputDirectory
            // 
            this.txtOutputDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputDirectory.Location = new System.Drawing.Point(93, 57);
            this.txtOutputDirectory.Name = "txtOutputDirectory";
            this.txtOutputDirectory.Size = new System.Drawing.Size(411, 20);
            this.txtOutputDirectory.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Output Directory";
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
            this.nudStartZoom.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
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
            this.label4.Location = new System.Drawing.Point(9, 129);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "End Zoom";
            // 
            // nudEndZoom
            // 
            this.nudEndZoom.Location = new System.Drawing.Point(93, 127);
            this.nudEndZoom.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nudEndZoom.Name = "nudEndZoom";
            this.nudEndZoom.Size = new System.Drawing.Size(63, 20);
            this.nudEndZoom.TabIndex = 9;
            this.nudEndZoom.Value = new decimal(new int[] {
            7,
            0,
            0,
            0});
            // 
            // btnProcess
            // 
            this.btnProcess.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnProcess.Location = new System.Drawing.Point(228, 271);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(164, 23);
            this.btnProcess.TabIndex = 11;
            this.btnProcess.Text = "Process";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.btnSelectNoAttributes);
            this.groupBox1.Controls.Add(this.btnSelectAllAttributes);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.clbSelectedAttributes);
            this.groupBox1.Controls.Add(this.chkExportAttributesToSeparateFile);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtInputShapeFile);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.btnBrowseShapeFile);
            this.groupBox1.Controls.Add(this.nudEndZoom);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtOutputDirectory);
            this.groupBox1.Controls.Add(this.nudStartZoom);
            this.groupBox1.Controls.Add(this.btnBrowseOutputDirectory);
            this.groupBox1.Location = new System.Drawing.Point(12, 27);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(591, 215);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Options";
            // 
            // btnSelectNoAttributes
            // 
            this.btnSelectNoAttributes.Location = new System.Drawing.Point(398, 137);
            this.btnSelectNoAttributes.Name = "btnSelectNoAttributes";
            this.btnSelectNoAttributes.Size = new System.Drawing.Size(75, 23);
            this.btnSelectNoAttributes.TabIndex = 16;
            this.btnSelectNoAttributes.Text = "Select None";
            this.btnSelectNoAttributes.UseVisualStyleBackColor = true;
            this.btnSelectNoAttributes.Click += new System.EventHandler(this.btnSelectNoAttributes_Click);
            // 
            // btnSelectAllAttributes
            // 
            this.btnSelectAllAttributes.Location = new System.Drawing.Point(398, 108);
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
            this.label5.Location = new System.Drawing.Point(181, 92);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(124, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "Select attribues to export";
            // 
            // clbSelectedAttributes
            // 
            this.clbSelectedAttributes.FormattingEnabled = true;
            this.clbSelectedAttributes.Location = new System.Drawing.Point(184, 108);
            this.clbSelectedAttributes.Name = "clbSelectedAttributes";
            this.clbSelectedAttributes.Size = new System.Drawing.Size(208, 94);
            this.clbSelectedAttributes.TabIndex = 12;
            // 
            // chkExportAttributesToSeparateFile
            // 
            this.chkExportAttributesToSeparateFile.AutoSize = true;
            this.chkExportAttributesToSeparateFile.Checked = true;
            this.chkExportAttributesToSeparateFile.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkExportAttributesToSeparateFile.Location = new System.Drawing.Point(398, 185);
            this.chkExportAttributesToSeparateFile.Name = "chkExportAttributesToSeparateFile";
            this.chkExportAttributesToSeparateFile.Size = new System.Drawing.Size(174, 17);
            this.chkExportAttributesToSeparateFile.TabIndex = 11;
            this.chkExportAttributesToSeparateFile.Text = "Export attributes to separate file";
            this.chkExportAttributesToSeparateFile.UseVisualStyleBackColor = true;
            // 
            // rtbOutput
            // 
            this.rtbOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbOutput.Location = new System.Drawing.Point(12, 300);
            this.rtbOutput.Name = "rtbOutput";
            this.rtbOutput.Size = new System.Drawing.Size(591, 91);
            this.rtbOutput.TabIndex = 13;
            this.rtbOutput.Text = "";
            // 
            // ofdShapeFile
            // 
            this.ofdShapeFile.Filter = "ShapeFile(*.shp)|*.shp";
            this.ofdShapeFile.Multiselect = true;
            this.ofdShapeFile.Title = "Select Input ShapeFile";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(615, 403);
            this.Controls.Add(this.rtbOutput);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnProcess);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "ShapeFile Mapbox Vector Tile Generator";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudStartZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEndZoom)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.TextBox txtInputShapeFile;
        private System.Windows.Forms.Button btnBrowseShapeFile;
        private System.Windows.Forms.Button btnBrowseOutputDirectory;
        private System.Windows.Forms.TextBox txtOutputDirectory;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudStartZoom;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nudEndZoom;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RichTextBox rtbOutput;
        private System.Windows.Forms.OpenFileDialog ofdShapeFile;
        private System.Windows.Forms.FolderBrowserDialog fbdOutput;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckedListBox clbSelectedAttributes;
        private System.Windows.Forms.CheckBox chkExportAttributesToSeparateFile;
        private System.Windows.Forms.Button btnSelectNoAttributes;
        private System.Windows.Forms.Button btnSelectAllAttributes;
    }
}

