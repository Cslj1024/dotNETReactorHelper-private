using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;


namespace dotNETReactorHelper
{
    public partial class DisPlayForm : Form
    {
        //private string ConfigFilePath = Path.Combine(Application.StartupPath ,"citedllconfig.json");
        string ConfigFilePath = Application.StartupPath + "\\citedllconfig.json";

        private List<string> exePath = new List<string>();

        private List<string> dllPaths = new List<string>();

        public List<string> SelectedCiteDllPaths { get; private set; }
        public List<string> SelectedDllPaths { get; private set; }


        public DisPlayForm(List<string> exeAndDllPaths, List<string> citedllPaths)
        {
            InitializeComponent();
            InitializeCheckBoxAll();
            PartexeAndDllPaths(exeAndDllPaths);
            InitializeCheckedListBox(dllPaths, citedllPaths);
            InitializeTextBox(exePath);
            RestoreCheckItems();
        }

        private void InitializeCheckBoxAll()
        {
            checkBoxCiteDllAll.CheckedChanged += checkBoxCiteDllAll_CheckedChanged;

            checkBoxDllAll.CheckedChanged += checkBoxDllAll_CheckedChanged;
        }

        private void PartexeAndDllPaths(List<string> exeAndDllPaths)
        {
            dllPaths.Clear();
            exePath.Clear();
            try
            {
                System.Diagnostics.Debug.WriteLine("分离exe和dll文件");
                exePath.AddRange(exeAndDllPaths.Where(path => path.EndsWith(".exe")));
                dllPaths.AddRange(exeAndDllPaths.Where(path => path.EndsWith(".dll")));
            }
            catch (Exception ex)
            {
                MessageBox.Show("exe和dll路径解析错误 ！" + ex.Message);
            }
        }

        private void InitializeCheckedListBox(List<string> dllPaths, List<string> citedllPaths)
        {
            checkedListBoxDll.Items.Clear();
            checkedListBoxCiteDll.Items.Clear();

            foreach (var dllPath in dllPaths)
            {
                checkedListBoxDll.Items.Add(dllPath);
            }
            foreach (var citedllPath in citedllPaths)
            {
                checkedListBoxCiteDll.Items.Add(citedllPath);
            }

            //dll默认全选
            for (int i = 0; i < checkedListBoxDll.Items.Count; i++)
            {
                checkedListBoxDll.SetItemChecked(i, true);
            }
        }

        private void InitializeTextBox(List<string> exePath)
        {
            textBoxExe.Text = exePath[0];
        }

        //引用的dll文件全选
        private void checkBoxDllAll_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = checkBoxDllAll.Checked;
            for (int i = 0; i < checkedListBoxDll.Items.Count; i++)
            {
                checkedListBoxDll.SetItemChecked(i, isChecked);
            }
        }

        //外部的dll文件全选
        private void checkBoxCiteDllAll_CheckedChanged(object sender, EventArgs e)
        {
            bool isCheked = checkBoxCiteDllAll.Checked;
            for (int i = 0; i < checkedListBoxCiteDll.Items.Count; i++)
            {
                checkedListBoxCiteDll.SetItemChecked(i, isCheked);
            }
        }

        //确定按钮
        private void buttonEnter_Click(object sender, EventArgs e)
        {
            SelectedDllPaths = new List<string>();
            foreach (var item in checkedListBoxDll.CheckedItems)
            {
                SelectedDllPaths.Add(item.ToString());
            }

            SelectedCiteDllPaths = new List<string>();
            foreach (var item in checkedListBoxCiteDll.CheckedItems)
            {
                SelectedCiteDllPaths.Add(item.ToString());
            }

            //将exe文件路径加入到SelectedDllPaths中
            SelectedDllPaths.AddRange(exePath);
            //将引入的dll文件路径加入到配置文件中
            SaveSelectedItemsToConfig(SelectedCiteDllPaths);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        //重选操作
        private void RestoreCheckItems()
        {
            var savePaths = LoadSelectedItemsFromConfig();
            if (savePaths != null)
            {
                // 创建两个列表，一个保存选中的项，一个保存未选中的项
                var checkedItems = new List<string>();
                var uncheckedItems = new List<string>();

                for (int i = 0; i < checkedListBoxCiteDll.Items.Count; i++)
                {
                    var item = checkedListBoxCiteDll.Items[i].ToString();
                    if (savePaths.Contains(item)) // 如果该项在已保存的选中路径中
                    {
                        checkedItems.Add(item);  // 添加到已选中列表
                    }
                    else
                    {
                        uncheckedItems.Add(item);  // 添加到未选中列表
                    }
                }

                // 清空 checkedListBoxCiteDll 中的所有项
                checkedListBoxCiteDll.Items.Clear();

                // 先添加选中的项到 checkedListBoxCiteDll，并将其设置为选中状态
                foreach (var checkedItem in checkedItems)
                {
                    checkedListBoxCiteDll.Items.Add(checkedItem, true);
                }

                // 再添加未选中的项到 checkedListBoxCiteDll，并将其设置为未选中状态
                foreach (var uncheckedItem in uncheckedItems)
                {
                    checkedListBoxCiteDll.Items.Add(uncheckedItem, false);
                }
            }
        }


        private void SaveSelectedItemsToConfig(List<string> selectedPaths)
        {
            try
            {
                string json = JsonConvert.SerializeObject(selectedPaths, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存到配置文件失败" + ex.Message);
            }
        }

        private List<string> LoadSelectedItemsFromConfig()
        {
            System.Diagnostics.Debug.WriteLine(ConfigFilePath);

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonConvert.DeserializeObject<List<string>>(json);
                }
                else
                {
                    MessageBox.Show("citedllconfig.json文件不存在，在当前文件夹" + Application.StartupPath + "中创建citedllconfig.json");
                    File.Create(ConfigFilePath);

                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonConvert.DeserializeObject<List<string>>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载citedllconfig.json文件失败" + ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return new List<string>();
        }

        private void checkedListBoxCiteDll_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void DisPlayForm_Load(object sender, EventArgs e)
        {

        }
    }
}