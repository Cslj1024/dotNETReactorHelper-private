namespace dotNETReactorHelper
{
    partial class DisPlayForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.checkedListBoxDisPlay = new System.Windows.Forms.CheckedListBox();
            this.checkBoxAll = new System.Windows.Forms.CheckBox();
            this.buttonEnter = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 4.909091F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 95.09091F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tableLayoutPanel1.Controls.Add(this.checkedListBoxDisPlay, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxAll, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonEnter, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 88.39286F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.60714F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(753, 305);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // checkedListBoxDisPlay
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.checkedListBoxDisPlay, 3);
            this.checkedListBoxDisPlay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBoxDisPlay.FormattingEnabled = true;
            this.checkedListBoxDisPlay.Location = new System.Drawing.Point(3, 3);
            this.checkedListBoxDisPlay.Name = "checkedListBoxDisPlay";
            this.checkedListBoxDisPlay.Size = new System.Drawing.Size(747, 207);
            this.checkedListBoxDisPlay.TabIndex = 0;
            // 
            // checkBoxAll
            // 
            this.checkBoxAll.AutoSize = true;
            this.checkBoxAll.Location = new System.Drawing.Point(38, 216);
            this.checkBoxAll.Name = "checkBoxAll";
            this.checkBoxAll.Size = new System.Drawing.Size(59, 19);
            this.checkBoxAll.TabIndex = 1;
            this.checkBoxAll.Text = "全选";
            this.checkBoxAll.UseVisualStyleBackColor = true;
            // 
            // buttonEnter
            // 
            this.buttonEnter.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonEnter.Location = new System.Drawing.Point(333, 256);
            this.buttonEnter.Name = "buttonEnter";
            this.buttonEnter.Size = new System.Drawing.Size(88, 32);
            this.buttonEnter.TabIndex = 2;
            this.buttonEnter.Text = "确定";
            this.buttonEnter.UseVisualStyleBackColor = true;
            this.buttonEnter.Click += new System.EventHandler(this.buttonEnter_Click);
            // 
            // DisPlayForm
            // 
            this.ClientSize = new System.Drawing.Size(753, 305);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "DisPlayForm";
            this.Text = "选择需要混淆的 DLL";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.CheckedListBox checkedListBoxDisPlay;
        private System.Windows.Forms.CheckBox checkBoxAll;
        private System.Windows.Forms.Button buttonEnter;
    }
}