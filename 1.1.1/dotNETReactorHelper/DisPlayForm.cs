using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace dotNETReactorHelper
{
    public partial class DisPlayForm : Form
    {
        public List<string> SelectedDllPaths { get; private set; }

        public DisPlayForm(List<string> dllPaths)
        {
            InitializeComponent();
            InitializeCheckBoxAll();
            InitializeCheckedListBox(dllPaths);
        }

        private void InitializeCheckBoxAll()
        {
            checkBoxAll.CheckedChanged += CheckBoxAll_CheckedChanged;
        }

        private void InitializeCheckedListBox(List<string> dllPaths)
        {
            checkedListBoxDisPlay.Items.Clear();
            foreach (var path in dllPaths)
            {
                checkedListBoxDisPlay.Items.Add(path);
            }
        }

        private void CheckBoxAll_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = checkBoxAll.Checked;
            for (int i = 0; i < checkedListBoxDisPlay.Items.Count; i++)
            {
                checkedListBoxDisPlay.SetItemChecked(i, isChecked);
            }
        }

        private void buttonEnter_Click(object sender, EventArgs e)
        {
            SelectedDllPaths = new List<string>();
            foreach (var item in checkedListBoxDisPlay.CheckedItems)
            {
                SelectedDllPaths.Add(item.ToString());
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}