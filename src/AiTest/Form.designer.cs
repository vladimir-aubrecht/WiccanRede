namespace WiccanRede
{
    partial class TestForm
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
            this.gridPanel = new System.Windows.Forms.Panel();
            this.openDialog = new System.Windows.Forms.OpenFileDialog();
            this.lbPosition = new System.Windows.Forms.Label();
            this.CNetHelpProvider = new System.Windows.Forms.HelpProvider();
            this.SuspendLayout();
            // 
            // gridPanel
            // 
            this.gridPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.gridPanel.Location = new System.Drawing.Point(0, 0);
            this.gridPanel.Name = "gridPanel";
            this.gridPanel.Size = new System.Drawing.Size(780, 569);
            this.gridPanel.TabIndex = 0;
            // 
            // openDialog
            // 
            this.openDialog.Title = "Choose map";
            // 
            // lbPosition
            // 
            this.lbPosition.AutoSize = true;
            this.CNetHelpProvider.SetHelpKeyword(this.lbPosition, "PathFinderForm.htm#PathFinderForm_lbPosition");
            this.CNetHelpProvider.SetHelpNavigator(this.lbPosition, System.Windows.Forms.HelpNavigator.Topic);
            this.lbPosition.Location = new System.Drawing.Point(702, 529);
            this.lbPosition.Name = "lbPosition";
            this.CNetHelpProvider.SetShowHelp(this.lbPosition, true);
            this.lbPosition.Size = new System.Drawing.Size(22, 13);
            this.lbPosition.TabIndex = 2;
            this.lbPosition.Text = "0,0";
            // 
            // CNetHelpProvider
            // 
            this.CNetHelpProvider.HelpNamespace = "PathFinder.chm";
            // 
            // TestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 569);
            this.Controls.Add(this.lbPosition);
            this.Controls.Add(this.gridPanel);
            this.DoubleBuffered = true;
            this.CNetHelpProvider.SetHelpKeyword(this, "PathFinderForm.htm");
            this.CNetHelpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
            this.MaximizeBox = false;
            this.Name = "TestForm";
            this.CNetHelpProvider.SetShowHelp(this, true);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Path Finder";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel gridPanel;
        private System.Windows.Forms.OpenFileDialog openDialog;
        private System.Windows.Forms.Label lbPosition;
        private System.Windows.Forms.HelpProvider CNetHelpProvider;
    }
}

