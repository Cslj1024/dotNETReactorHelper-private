using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using ReactorHelper;

namespace dotNETReactorHelper
{
    public partial class DisPlayForm : Form
    {
        string ConfigFilePath = Application.StartupPath + "\\citedllconfig.json";

        private List<string> exePath = new List<string>();
        private List<string> dllPaths = new List<string>();
        public List<string> SelectedCiteDllPaths { get; private set; }
        public List<string> SelectedDllPaths { get; private set; }
        public List<string> guids { get; private set; }

        public DisPlayForm(List<string> exeAndDllPaths, List<string> citedllPaths, List<string> guids)
        {
            InitializeComponent();
            this.guids = guids;
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
            for (int i = 0; i < checkedListBoxDll.Items.Count; i++)
            {
                checkedListBoxDll.SetItemChecked(i, true);
            }
        }

        private void InitializeTextBox(List<string> exePath)
        {
            textBoxExe.Text = exePath.FirstOrDefault();
        }

        // 引用的dll文件全选
        private void checkBoxDllAll_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = checkBoxDllAll.Checked;
            for (int i = 0; i < checkedListBoxDll.Items.Count; i++)
            {
                checkedListBoxDll.SetItemChecked(i, isChecked);
            }
        }

        // 外部的dll文件全选
        private void checkBoxCiteDllAll_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = checkBoxCiteDllAll.Checked;
            for (int i = 0; i < checkedListBoxCiteDll.Items.Count; i++)
            {
                checkedListBoxCiteDll.SetItemChecked(i, isChecked);
            }
        }

        // 确定按钮
        private void buttonEnter_Click(object sender, EventArgs e)
        {
            SelectedDllPaths = checkedListBoxDll.CheckedItems.Cast<string>().ToList();
            SelectedCiteDllPaths = checkedListBoxCiteDll.CheckedItems.Cast<string>().ToList();
            SelectedDllPaths.AddRange(exePath);
            SaveConfigData(new ConfigData { Guid = guids.First(), SelectedCiteDllPaths = SelectedCiteDllPaths });
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        //重选操作
        private void RestoreCheckItems()
        {
            var configData = LoadConfigData();

            if (configData != null && configData.Count > 0)
            {
                var matchedGuids = configData.Where(item => guids.Contains(item.Guid)).ToList();
                if (matchedGuids.Any())
                {
                    var matchedGuid = matchedGuids.First();
                    SelectedCiteDllPaths = matchedGuid.SelectedCiteDllPaths;

                    // 保留所有项，并将选中的项移动到最前面
                    var allItems = new List<string>();
                    var selectedItems = new List<string>();

                    foreach (var item in checkedListBoxCiteDll.Items)
                    {
                        if (SelectedCiteDllPaths.Contains(item.ToString()))
                        {
                            selectedItems.Add(item.ToString());
                        }
                        else
                        {
                            allItems.Add(item.ToString());
                        }
                    }

                    // 清空列表并重新添加，选中的项在前
                    checkedListBoxCiteDll.Items.Clear();
                    checkedListBoxCiteDll.Items.AddRange(selectedItems.ToArray());
                    checkedListBoxCiteDll.Items.AddRange(allItems.ToArray());

                    // 更新选中状态
                    for (int i = 0; i < checkedListBoxCiteDll.Items.Count; i++)
                    {
                        checkedListBoxCiteDll.SetItemChecked(i, SelectedCiteDllPaths.Contains(checkedListBoxCiteDll.Items[i].ToString()));
                    }
                }
                else
                {
                    // 如果没有匹配的 GUID，保存新的配置
                    SaveConfigData(new ConfigData { Guid = guids.First(), SelectedCiteDllPaths = new List<string>() });
                }
            }
            else
            {
                // 如果配置文件为空，保存新的配置
                SaveConfigData(new ConfigData { Guid = guids.First(), SelectedCiteDllPaths = new List<string>() });
            }
        }



        private void SaveConfigData(ConfigData configData)
        {
            try
            {
                var currentConfig = LoadConfigData() ?? new List<ConfigData>();
                currentConfig.RemoveAll(c => c.Guid == configData.Guid);
                currentConfig.Add(configData);
                string json = JsonConvert.SerializeObject(currentConfig, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存到配置文件失败: " + ex.Message);
            }
        }

        private List<ConfigData> LoadConfigData()
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                return JsonConvert.DeserializeObject<List<ConfigData>>(json);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("配置文件citedllconfig.json不存在，将在\\Common7\\IDE中创建配置文件...");
                File.Create(ConfigFilePath).Close();
            }
            return new List<ConfigData>();
        }

        private class ConfigData
        {
            public string Guid { get; set; }
            public List<string> SelectedCiteDllPaths { get; set; }
        }

       

        private void checkedListBoxCiteDll_SelectedIndexChanged(object sender, EventArgs e) { }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e) { }

        private void DisPlayForm_Load(object sender, EventArgs e) { }
    }
}
