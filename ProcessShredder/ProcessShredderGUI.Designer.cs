using System.Windows.Forms;

namespace ProcessShredder
{
    partial class ProcessShredderGUI
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
            if ( disposing && (components != null) )
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
            this.listProcesses = new System.Windows.Forms.ListView();
            this.ColName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ColPID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ColParent = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ColUser = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.checkIncludeChilds = new System.Windows.Forms.CheckBox();
            this.buttonEndProcesses = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listProcesses
            // 
            this.listProcesses.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listProcesses.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColName,
            this.ColPID,
            this.ColParent,
            this.ColUser});
            this.listProcesses.Location = new System.Drawing.Point(12, 12);
            this.listProcesses.Name = "listProcesses";
            this.listProcesses.Size = new System.Drawing.Size(365, 428);
            this.listProcesses.TabIndex = 0;
            this.listProcesses.UseCompatibleStateImageBehavior = false;
            this.listProcesses.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.ColumnClick);
            // 
            // ColName
            // 
            this.ColName.Text = "Name";
            this.ColName.Width = 150;
            // 
            // ColPID
            // 
            this.ColPID.Text = "PID";
            // 
            // ColParent
            // 
            this.ColParent.Text = "Parent";
            // 
            // ColUser
            // 
            this.ColUser.Text = "User";
            // 
            // checkIncludeChilds
            // 
            this.checkIncludeChilds.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkIncludeChilds.AutoSize = true;
            this.checkIncludeChilds.Location = new System.Drawing.Point(12, 452);
            this.checkIncludeChilds.Name = "checkIncludeChilds";
            this.checkIncludeChilds.Size = new System.Drawing.Size(139, 17);
            this.checkIncludeChilds.TabIndex = 1;
            this.checkIncludeChilds.Text = "Include Child Processes";
            this.checkIncludeChilds.UseVisualStyleBackColor = true;
            // 
            // buttonEndProcesses
            // 
            this.buttonEndProcesses.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEndProcesses.Location = new System.Drawing.Point(264, 446);
            this.buttonEndProcesses.Name = "buttonEndProcesses";
            this.buttonEndProcesses.Size = new System.Drawing.Size(113, 23);
            this.buttonEndProcesses.TabIndex = 2;
            this.buttonEndProcesses.Text = "End Processes";
            this.buttonEndProcesses.UseVisualStyleBackColor = true;
            this.buttonEndProcesses.Click += new System.EventHandler(this.buttonEndProcesses_Click);
            // 
            // ProcessShredderGUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(389, 481);
            this.Controls.Add(this.buttonEndProcesses);
            this.Controls.Add(this.checkIncludeChilds);
            this.Controls.Add(this.listProcesses);
            this.Name = "ProcessShredderGUI";
            this.Text = "Process Shredder";
            this.Load += new System.EventHandler(this.ProcessShredderGUI_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listProcesses;
        private System.Windows.Forms.CheckBox checkIncludeChilds;
        private System.Windows.Forms.ColumnHeader ColName;
        private System.Windows.Forms.ColumnHeader ColPID;
        private System.Windows.Forms.ColumnHeader ColParent;
        private System.Windows.Forms.ColumnHeader ColUser;
        public System.Windows.Forms.Button buttonEndProcesses;
    }
}

