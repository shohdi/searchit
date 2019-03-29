using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SearchInFiles
{
    public partial class frmSearch : Form
    {
        private string lastFile = "";
        private int countFiles = 0;
        List<FileInfo> lstFiles = new List<FileInfo>();
        private bool started = false;
        public frmSearch()
        {
            InitializeComponent();
        }

        private string folder = "";
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog ();
            var dialogResult = dialog.ShowDialog();
            if(dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                this.folder = new FileInfo( dialog.FileName).Directory.FullName;
            }


            txtDirectory.Text = this.folder;
        }

        private void chkEnableDate_CheckedChanged(object sender, EventArgs e)
        {
            
                dtFrom.Enabled = chkEnableDate.Checked;
                dtTo.Enabled = chkEnableDate.Checked;
            
        }

        private void frmSearch_Load(object sender, EventArgs e)
        {

        }

        Thread thThread = null;
        private void btnSearch_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrWhiteSpace( txtSearch.Text) && !string.IsNullOrWhiteSpace(txtDirectory.Text))
            {
                started = true;
                btnSearch.Enabled = false;

                lstFiles = new List<FileInfo>();
                this.folder = txtDirectory.Text;
                DateTime? fromDate = null;
                DateTime? toDate = null;
                if(chkEnableDate.Checked)
                {
                    fromDate = dtFrom.Value;
                    toDate = dtTo.Value;
                }
                Regex regSearch = new Regex("");
                try
                {
                    regSearch = new Regex(txtSearch.Text, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                }
                catch
                {
                    started = false;
                    return;
                }
                
                thThread = new Thread(() => {
                    lstFiles = searchDir(this.folder, fromDate, toDate, regSearch,chkKeepPath.Checked, lstFiles);
                    started = false;
                    var dirName = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
                    Directory.CreateDirectory(".\\" + dirName);
                    foreach (var file in lstFiles)
                    {
                        try
                        {
                            Regex nameTester = new Regex("[\\\\\\/\\:\\*\\?\\\"\\<\\>\\|]+");
                            string fileName = "";
                            if (chkKeepPath.Checked)
                                fileName = nameTester.Replace(file.FullName, "_");
                            else
                                fileName = file.Name;
                            fileName = Path.Combine(".\\" + dirName, fileName);
                            File.Copy(file.FullName, fileName);

                        }
                        catch(Exception ex)
                        {

                        }
                    }

                });

                thThread.Start();
                
            }

        }

        private List<FileInfo> searchDir(string dir, DateTime? fromDate, DateTime? toDate, Regex regSearch,bool keepPath, List<FileInfo> outFiles = null)
        {
            List<FileInfo> ret = new List<FileInfo>();
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            DirectoryInfo[] dirs = dirInfo.GetDirectories();
            foreach(var inDir in dirs)
            {
                var lstFoundFiles = searchDir(inDir.FullName, fromDate, toDate, regSearch,keepPath,outFiles);
                ret.AddRange(lstFoundFiles);
            }
            FileInfo[] files = dirInfo.GetFiles();
            foreach(var f in files)
            {
                lastFile = f.FullName;
                if (fromDate != null && toDate != null && (f.LastWriteTime < fromDate.Value || f.LastWriteTime > toDate.Value))
                {
                    
                }
                else
                {
                    string fileData = File.ReadAllText(f.FullName);
                    if(regSearch.IsMatch(fileData))
                    {
                        ret.Add(f);
                        if(outFiles != null)
                        {
                            outFiles.Add(f);
                        }
                    }
                }
            }

            return ret;

        }

        private void tmrUpdateList_Tick(object sender, EventArgs e)
        {
            lblFile.Text = lastFile;
            if(lstFiles.Count != countFiles)
            {
                countFiles = lstFiles.Count;
                List<string> lstOutput = lstFiles.Select(l => l.FullName).ToList();
                lstSearchResult.DataSource = null;
                lstSearchResult.Refresh();
                lstSearchResult.DataSource = lstOutput;
                lstSearchResult.Refresh();

            }

            if(!started)
            {
                btnSearch.Enabled = !started;
            }
        }

        private void lstSearchResult_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lstSearchResult_DoubleClick(object sender, EventArgs e)
        {
            if(lstSearchResult.SelectedValue != null)
            {
                string val = lstSearchResult.SelectedValue.ToString();
                if(!string.IsNullOrWhiteSpace(val))
                {
                    Process.Start(val);
                    
                }
            }

           
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                thThread.Abort();

            }
            catch
            {

            }

            started = false;
        }
    }
}
