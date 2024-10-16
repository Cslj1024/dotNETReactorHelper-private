namespace dotNETReactorHelper
{
    partial class DisPlayForm
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
            this.components = new System.ComponentModel.Container();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.checkedListBoxCiteDll = new System.Windows.Forms.CheckedListBox();
            this.checkedListBoxDll = new System.Windows.Forms.CheckedListBox();
            this.buttonEnter = new System.Windows.Forms.Button();
            this.labelexe = new System.Windows.Forms.Label();
            this.labelcitedll = new System.Windows.Forms.Label();
            this.labeldll = new System.Windows.Forms.Label();
            this.textBoxExe = new System.Windows.Forms.TextBox();
            this.checkBoxDllAll = new System.Windows.Forms.CheckBox();
            this.checkBoxCiteDllAll = new System.Windows.Forms.CheckBox();
            this.ClickEnterprogressBar = new System.Windows.Forms.ProgressBar();
            this.ClickEnterLabel = new System.Windows.Forms.Label();
            this.ClickEnterTimer = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Controls.Add(this.checkedListBoxCiteDll, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.checkedListBoxDll, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.buttonEnter, 1, 8);
            this.tableLayoutPanel1.Controls.Add(this.labelexe, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelcitedll, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.labeldll, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.textBoxExe, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxDllAll, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxCiteDllAll, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.ClickEnterprogressBar, 2, 8);
            this.tableLayoutPanel1.Controls.Add(this.ClickEnterLabel, 2, 7);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 9;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.5F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.5F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 450);
            this.tableLayoutPanel1.TabIndex = 0;
            this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
            // 
            // checkedListBoxCiteDll
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.checkedListBoxCiteDll, 3);
            this.checkedListBoxCiteDll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBoxCiteDll.FormattingEnabled = true;
            this.checkedListBoxCiteDll.Location = new System.Drawing.Point(3, 255);
            this.checkedListBoxCiteDll.Name = "checkedListBoxCiteDll";
            this.checkedListBoxCiteDll.ScrollAlwaysVisible = true;
            this.checkedListBoxCiteDll.Size = new System.Drawing.Size(794, 129);
            this.checkedListBoxCiteDll.TabIndex = 3;
            this.checkedListBoxCiteDll.SelectedIndexChanged += new System.EventHandler(this.checkedListBoxCiteDll_SelectedIndexChanged);
            // 
            // checkedListBoxDll
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.checkedListBoxDll, 3);
            this.checkedListBoxDll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBoxDll.FormattingEnabled = true;
            this.checkedListBoxDll.Location = new System.Drawing.Point(3, 92);
            this.checkedListBoxDll.Name = "checkedListBoxDll";
            this.checkedListBoxDll.ScrollAlwaysVisible = true;
            this.checkedListBoxDll.Size = new System.Drawing.Size(794, 106);
            this.checkedListBoxDll.TabIndex = 4;
            // 
            // buttonEnter
            // 
            this.buttonEnter.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonEnter.Location = new System.Drawing.Point(359, 419);
            this.buttonEnter.Name = "buttonEnter";
            this.buttonEnter.Size = new System.Drawing.Size(82, 28);
            this.buttonEnter.TabIndex = 6;
            this.buttonEnter.Text = "确定";
            this.buttonEnter.UseVisualStyleBackColor = true;
            this.buttonEnter.Click += new System.EventHandler(this.buttonEnter_Click);
            // 
            // labelexe
            // 
            this.labelexe.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelexe.Location = new System.Drawing.Point(3, 0);
            this.labelexe.Name = "labelexe";
            this.labelexe.Size = new System.Drawing.Size(154, 22);
            this.labelexe.TabIndex = 0;
            this.labelexe.Text = "exe文件";
            // 
            // labelcitedll
            // 
            this.labelcitedll.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelcitedll.Location = new System.Drawing.Point(3, 230);
            this.labelcitedll.Name = "labelcitedll";
            this.labelcitedll.Size = new System.Drawing.Size(154, 22);
            this.labelcitedll.TabIndex = 2;
            this.labelcitedll.Text = "外部的dll文件";
            // 
            // labeldll
            // 
            this.labeldll.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labeldll.Location = new System.Drawing.Point(3, 67);
            this.labeldll.Name = "labeldll";
            this.labeldll.Size = new System.Drawing.Size(154, 22);
            this.labeldll.TabIndex = 1;
            this.labeldll.Text = "引用的dll文件";
            // 
            // textBoxExe
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.textBoxExe, 3);
            this.textBoxExe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxExe.Location = new System.Drawing.Point(3, 25);
            this.textBoxExe.Name = "textBoxExe";
            this.textBoxExe.ReadOnly = true;
            this.textBoxExe.Size = new System.Drawing.Size(794, 25);
            this.textBoxExe.TabIndex = 9;
            // 
            // checkBoxDllAll
            // 
            this.checkBoxDllAll.AutoSize = true;
            this.checkBoxDllAll.Location = new System.Drawing.Point(163, 204);
            this.checkBoxDllAll.Name = "checkBoxDllAll";
            this.checkBoxDllAll.Size = new System.Drawing.Size(59, 19);
            this.checkBoxDllAll.TabIndex = 10;
            this.checkBoxDllAll.Text = "全选";
            this.checkBoxDllAll.UseVisualStyleBackColor = true;
            this.checkBoxDllAll.CheckedChanged += new System.EventHandler(this.checkBoxDllAll_CheckedChanged);
            // 
            // checkBoxCiteDllAll
            // 
            this.checkBoxCiteDllAll.AutoSize = true;
            this.checkBoxCiteDllAll.Location = new System.Drawing.Point(163, 390);
            this.checkBoxCiteDllAll.Name = "checkBoxCiteDllAll";
            this.checkBoxCiteDllAll.Size = new System.Drawing.Size(59, 19);
            this.checkBoxCiteDllAll.TabIndex = 11;
            this.checkBoxCiteDllAll.Text = "全选";
            this.checkBoxCiteDllAll.UseVisualStyleBackColor = true;
            this.checkBoxCiteDllAll.CheckedChanged += new System.EventHandler(this.checkBoxCiteDllAll_CheckedChanged);
            // 
            // ClickEnterprogressBar
            // 
            this.ClickEnterprogressBar.Location = new System.Drawing.Point(643, 419);
            this.ClickEnterprogressBar.MarqueeAnimationSpeed = 1000;
            this.ClickEnterprogressBar.Maximum = 8000;
            this.ClickEnterprogressBar.Name = "ClickEnterprogressBar";
            this.ClickEnterprogressBar.Size = new System.Drawing.Size(154, 23);
            this.ClickEnterprogressBar.TabIndex = 12;
            // 
            // ClickEnterLabel
            // 
            this.ClickEnterLabel.Location = new System.Drawing.Point(643, 387);
            this.ClickEnterLabel.Name = "ClickEnterLabel";
            this.ClickEnterLabel.Size = new System.Drawing.Size(154, 29);
            this.ClickEnterLabel.TabIndex = 13;
            this.ClickEnterLabel.Text = "label1";
            // 
            // ClickEnterTimer
            // 
            this.ClickEnterTimer.Tick += new System.EventHandler(this.ClickEnterTimer_Tick);
            // 
            // DisPlayForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "DisPlayForm";
            this.Text = "选择需要混淆的DLL";
            this.Load += new System.EventHandler(this.DisPlayForm_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labelexe;
        private System.Windows.Forms.Label labelcitedll;
        private System.Windows.Forms.Label labeldll;
        private System.Windows.Forms.CheckedListBox checkedListBoxCiteDll;
        private System.Windows.Forms.CheckedListBox checkedListBoxDll;
        private System.Windows.Forms.Button buttonEnter;
        private System.Windows.Forms.TextBox textBoxExe;
        private System.Windows.Forms.CheckBox checkBoxDllAll;
        private System.Windows.Forms.CheckBox checkBoxCiteDllAll;
        private System.Windows.Forms.ProgressBar ClickEnterprogressBar;
        private System.Windows.Forms.Label ClickEnterLabel;
        private System.Windows.Forms.Timer ClickEnterTimer;
    }
}