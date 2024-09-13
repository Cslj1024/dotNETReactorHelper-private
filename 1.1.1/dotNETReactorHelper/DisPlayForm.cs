using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace dotNETReactorHelper
{
    public partial class DisPlayForm : Form
    {
        string ConfigFilePath = Application.StartupPath + "dllconfig.json";
        public List<string> SelectedDllPaths { get; private set; }

        public DisPlayForm(List<string> dllPaths)
        {
            InitializeComponent();
            InitializeCheckBoxAll();
            InitializeCheckedListBox(dllPaths);
            RestoreCheckedItems();
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

            
            SaveSelectedItemsToConfig(SelectedDllPaths);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void RestoreCheckedItems()
        {
            
            var savedPaths = LoadSelectedItemsFromConfig();
            if (savedPaths != null)
            {
                for (int i = 0; i < checkedListBoxDisPlay.Items.Count; i++)
                {
                    if (savedPaths.Contains(checkedListBoxDisPlay.Items[i].ToString()))
                    {
                        checkedListBoxDisPlay.SetItemChecked(i, true);
                    }
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
                MessageBox.Show("保存配置文件失败" + ex.Message);
            }
        }

        
        private List<string> LoadSelectedItemsFromConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonConvert.DeserializeObject<List<string>>(json);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("dllconfig.json文件不存在");
                    MessageBox.Show("dllconfig.json文件不存在，在当前文件夹" + Application.StartupPath + "中创建dllconfig.json");
                    File.Create(ConfigFilePath);

                    string json = File.ReadAllText(ConfigFilePath); 
                    return JsonConvert.DeserializeObject<List<string>>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载dllconfig.json文件失败" + ex.Message);
            }
            return new List<string>();
        }

        private void checkedListBoxDisPlay_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void DisPlayForm_Load(object sender, EventArgs e)
        {
        }

        //恢复默认设置，即删去dllconfig.json文件中所有内容
        private void buttonResume_Click(object sender, EventArgs e)
        {
            //对checkListBoxDisPlay进行取消选择操作
            bool noChecked = false;
            for (int i = 0; i < checkedListBoxDisPlay.Items.Count; i++)
            {
                checkedListBoxDisPlay.SetItemChecked(i, noChecked);
            }

            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    File.Delete(ConfigFilePath);
                    MessageBox.Show("恢复默认设置成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除dllconfig.json文件失败: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("恢复默认设置失败,dllconfig.json文件不存在");
            }
        }
    }
}
