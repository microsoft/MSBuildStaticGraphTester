namespace MSBuildGraphUI
{
    partial class Form1
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
            this._loadButton = new System.Windows.Forms.Button();
            this._openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this._statusBar = new System.Windows.Forms.StatusStrip();
            this._statusBarLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.button1 = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this._propertyGrid = new System.Windows.Forms.PropertyGrid();
            this._treeVew = new System.Windows.Forms.TreeView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.tbSaveFile = new System.Windows.Forms.TextBox();
            this.numRankSep = new System.Windows.Forms.NumericUpDown();
            this.numNodeSep = new System.Windows.Forms.NumericUpDown();
            this._statusBar.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numRankSep)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNodeSep)).BeginInit();
            this.SuspendLayout();
            // 
            // _loadButton
            // 
            this._loadButton.Location = new System.Drawing.Point(12, 12);
            this._loadButton.Name = "_loadButton";
            this._loadButton.Size = new System.Drawing.Size(94, 28);
            this._loadButton.TabIndex = 1;
            this._loadButton.Text = "Load Project";
            this._loadButton.UseVisualStyleBackColor = true;
            this._loadButton.Click += new System.EventHandler(this._loadButton_Click);
            // 
            // _openFileDialog
            // 
            this._openFileDialog.Filter = "Projects|*.*proj;*.sln";
            // 
            // _statusBar
            // 
            this._statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._statusBarLabel});
            this._statusBar.Location = new System.Drawing.Point(0, 690);
            this._statusBar.Name = "_statusBar";
            this._statusBar.Size = new System.Drawing.Size(1103, 22);
            this._statusBar.TabIndex = 2;
            this._statusBar.Text = "statusStrip1";
            // 
            // _statusBarLabel
            // 
            this._statusBarLabel.Name = "_statusBarLabel";
            this._statusBarLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(112, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(94, 28);
            this.button1.TabIndex = 1;
            this.button1.Text = "test";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 46);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1103, 641);
            this.tabControl1.TabIndex = 4;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this._propertyGrid);
            this.tabPage1.Controls.Add(this._treeVew);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1095, 615);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Tree";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // _propertyGrid
            // 
            this._propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._propertyGrid.Location = new System.Drawing.Point(743, 6);
            this._propertyGrid.Name = "_propertyGrid";
            this._propertyGrid.Size = new System.Drawing.Size(344, 616);
            this._propertyGrid.TabIndex = 5;
            // 
            // _treeVew
            // 
            this._treeVew.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._treeVew.Location = new System.Drawing.Point(0, 0);
            this._treeVew.Name = "_treeVew";
            this._treeVew.Size = new System.Drawing.Size(737, 615);
            this._treeVew.TabIndex = 4;
            this._treeVew.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this._treeVew_AfterSelect);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.webBrowser1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1095, 615);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Graph";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(3, 3);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(1089, 609);
            this.webBrowser1.TabIndex = 6;
            // 
            // tbSaveFile
            // 
            this.tbSaveFile.Location = new System.Drawing.Point(314, 16);
            this.tbSaveFile.Name = "tbSaveFile";
            this.tbSaveFile.Size = new System.Drawing.Size(617, 20);
            this.tbSaveFile.TabIndex = 5;
            // 
            // numRankSep
            // 
            this.numRankSep.DecimalPlaces = 2;
            this.numRankSep.Location = new System.Drawing.Point(212, 16);
            this.numRankSep.Name = "numRankSep";
            this.numRankSep.Size = new System.Drawing.Size(45, 20);
            this.numRankSep.TabIndex = 6;
            this.numRankSep.Value = new decimal(new int[] {
            30,
            0,
            0,
            65536});
            // 
            // numNodeSep
            // 
            this.numNodeSep.DecimalPlaces = 2;
            this.numNodeSep.Location = new System.Drawing.Point(263, 16);
            this.numNodeSep.Name = "numNodeSep";
            this.numNodeSep.Size = new System.Drawing.Size(45, 20);
            this.numNodeSep.TabIndex = 6;
            this.numNodeSep.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1103, 712);
            this.Controls.Add(this.numNodeSep);
            this.Controls.Add(this.numRankSep);
            this.Controls.Add(this.tbSaveFile);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this._statusBar);
            this.Controls.Add(this.button1);
            this.Controls.Add(this._loadButton);
            this.Name = "Form1";
            this.Text = "Form1";
            this._statusBar.ResumeLayout(false);
            this._statusBar.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numRankSep)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNodeSep)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button _loadButton;
        private System.Windows.Forms.OpenFileDialog _openFileDialog;
        private System.Windows.Forms.StatusStrip _statusBar;
        private System.Windows.Forms.ToolStripStatusLabel _statusBarLabel;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.PropertyGrid _propertyGrid;
        private System.Windows.Forms.TreeView _treeVew;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.TextBox tbSaveFile;
        private System.Windows.Forms.NumericUpDown numRankSep;
        private System.Windows.Forms.NumericUpDown numNodeSep;
    }
}

