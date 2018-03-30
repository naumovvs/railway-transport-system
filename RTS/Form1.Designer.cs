namespace RTS
{
    partial class frmMain
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
            this.pnlGraph = new System.Windows.Forms.Panel();
            this.btnSimulate = new System.Windows.Forms.Button();
            this.chbShowNames = new System.Windows.Forms.CheckBox();
            this.chbFacilities = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // pnlGraph
            // 
            this.pnlGraph.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlGraph.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlGraph.Cursor = System.Windows.Forms.Cursors.Default;
            this.pnlGraph.Location = new System.Drawing.Point(12, 12);
            this.pnlGraph.MinimumSize = new System.Drawing.Size(200, 200);
            this.pnlGraph.Name = "pnlGraph";
            this.pnlGraph.Size = new System.Drawing.Size(560, 477);
            this.pnlGraph.TabIndex = 0;
            this.pnlGraph.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlGraph_Paint);
            this.pnlGraph.Resize += new System.EventHandler(this.pnlGraph_Resize);
            // 
            // btnSimulate
            // 
            this.btnSimulate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSimulate.CausesValidation = false;
            this.btnSimulate.Location = new System.Drawing.Point(497, 525);
            this.btnSimulate.Name = "btnSimulate";
            this.btnSimulate.Size = new System.Drawing.Size(75, 23);
            this.btnSimulate.TabIndex = 0;
            this.btnSimulate.Text = "Simulate";
            this.btnSimulate.UseVisualStyleBackColor = true;
            this.btnSimulate.Click += new System.EventHandler(this.btnSimulate_Click);
            // 
            // chbShowNames
            // 
            this.chbShowNames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chbShowNames.AutoSize = true;
            this.chbShowNames.Location = new System.Drawing.Point(12, 504);
            this.chbShowNames.Name = "chbShowNames";
            this.chbShowNames.Size = new System.Drawing.Size(114, 17);
            this.chbShowNames.TabIndex = 1;
            this.chbShowNames.Text = "Show node names";
            this.chbShowNames.UseVisualStyleBackColor = true;
            this.chbShowNames.CheckedChanged += new System.EventHandler(this.chbShowNames_CheckedChanged);
            // 
            // chbFacilities
            // 
            this.chbFacilities.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chbFacilities.AutoSize = true;
            this.chbFacilities.Location = new System.Drawing.Point(12, 531);
            this.chbFacilities.Name = "chbFacilities";
            this.chbFacilities.Size = new System.Drawing.Size(131, 17);
            this.chbFacilities.TabIndex = 2;
            this.chbFacilities.Text = "Show facilities number";
            this.chbFacilities.UseVisualStyleBackColor = true;
            this.chbFacilities.CheckedChanged += new System.EventHandler(this.chbFacilities_CheckedChanged);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 562);
            this.Controls.Add(this.chbFacilities);
            this.Controls.Add(this.chbShowNames);
            this.Controls.Add(this.btnSimulate);
            this.Controls.Add(this.pnlGraph);
            this.Name = "frmMain";
            this.Text = "Transportation System Simulation";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlGraph;
        private System.Windows.Forms.Button btnSimulate;
        private System.Windows.Forms.CheckBox chbShowNames;
        private System.Windows.Forms.CheckBox chbFacilities;
    }
}

