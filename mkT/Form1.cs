using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using MonoTorrent;
using MonoTorrent.Common;
using MonoTorrent.BEncoding;
using System.Threading.Tasks;
using System.Threading;

namespace mkT
{
    public partial class Form1 : Form
    {
        public Dictionary<string, long> comboSource = new Dictionary<string, long>();
        public Form1()
        {
            InitializeComponent();
            //
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboSource.Add("Auto", -1);
            comboSource.Add("16 KiB", 1024 * 16);
            comboSource.Add("32 KiB", 1024 * 32);
            comboSource.Add("64 KiB", 1024 * 64);
            comboSource.Add("128 KiB", 1024 * 128);
            comboSource.Add("256 KiB", 1024 * 256);
            comboSource.Add("512 KiB", 1024 * 512);
            comboSource.Add("1 MiB", 1024 * 1024 * 1);
            comboSource.Add("2 MiB", 1024 * 1024 * 2);
            comboSource.Add("4 MiB", 1024 * 1024 * 4);
            comboSource.Add("8 MiB", 1024 * 1024 * 8);
            comboSource.Add("16 MiB", 1024 * 1024 * 16);
            comboBox1.DataSource = new BindingSource(comboSource, null);
            comboBox1.DisplayMember = "Key";
            comboBox1.ValueMember = "Value";
            comboBox1.SelectedIndex = 0;

            //REMOVE THIS UPON RELEASE
            textBox1.Text = @"C:\Users\Leon\Desktop\TEST";
            this.Size = new Size(Form1.ActiveForm.Size.Width, Form1.ActiveForm.Size.Height - 83);
            groupBox2.Size = new Size(391, 130);
            groupBox3.Visible = false;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //SemaphoreSlim sS = new SemaphoreSlim(10);
            //List<Task> tasks = new List<Task>();
            if (checkBox2.Checked)
            {
                if (Directory.Exists(textBox1.Text))
                {
                    if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                    {
                        button3.Enabled = false;
                        DirectoryInfo dI = new DirectoryInfo(textBox1.Text);
                        foreach (FileInfo f in dI.GetFiles("*.*", SearchOption.AllDirectories))
                        {
                            TorrentCreator tc = InitTorrentCreator();
                            ITorrentFileSource tFS = new TorrentFileSource(f.FullName);
                            if (((KeyValuePair<string, long>)comboBox1.SelectedItem).Value == -1)
                            {
                                tc.PieceLength = CalculateOptimumPieceSize(textBox1.Text);
                            }
                            string a = Path.Combine(folderBrowserDialog1.SelectedPath, Path.GetFileNameWithoutExtension(f.FullName) + ".torrent");
                            tc.CreateAsync(tFS, new FileStream(Path.Combine(folderBrowserDialog1.SelectedPath, Path.GetFileNameWithoutExtension(f.FullName) + ".torrent"), FileMode.Create)).ContinueWith((t) => TorrentCompleted(true));
                        }
                        button3.Enabled = true;
                        if (MessageBox.Show("The torrents have been created successfully.", "Torrents created", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
                        {
                            progressBar1.Value = 0;
                        }
                    }
                }
                else if (File.Exists(textBox1.Text)) { MessageBox.Show("The file source must be a directory whilst in batch mode.", "Invalid file source specified", MessageBoxButtons.OK, MessageBoxIcon.Stop); }
                else { MessageBox.Show("Please enter a file source.", "No file source specified", MessageBoxButtons.OK, MessageBoxIcon.Stop); }
            }
            else
            {
                if (File.Exists(textBox1.Text) || Directory.Exists(textBox1.Text))
                {
                    saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(textBox1.Text) + ".torrent";
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {

                        button3.Enabled = false;
                        TorrentCreator tc = InitTorrentCreator();
                        ITorrentFileSource tFS = new TorrentFileSource(textBox1.Text);
                        if (((KeyValuePair<string, long>)comboBox1.SelectedItem).Value == -1)
                        {
                            tc.PieceLength = CalculateOptimumPieceSize(textBox1.Text);
                        }
                        tc.CreateAsync(tFS, new FileStream(saveFileDialog1.FileName, FileMode.Create)).ContinueWith((t) => TorrentCompleted(false));
                    }
                }
                else { MessageBox.Show("Please enter a file source.", "No file source specified", MessageBoxButtons.OK, MessageBoxIcon.Stop); }
            }
            
        }

        delegate void ShowProgressDelegate(double fileCompletion, double overallCompletion, double overallSize);
        private void ShowProgress(double fileCompletion, double overallCompletion, double overallSize)
        {
            // check the current thread
            if (progressBar1.InvokeRequired == false)
            {
                progressBar1.Maximum = 100;
                progressBar1.Value = (int)Math.Round(overallCompletion);
            }
            else
            {
                // show async progress
                ShowProgressDelegate showProgress = new ShowProgressDelegate(ShowProgress);
                BeginInvoke(showProgress, new object[] { fileCompletion, overallCompletion, overallSize });
            }
        }

        delegate void TorrentCompletedDelegate(bool multi);
        private void TorrentCompleted(bool multi)
        {
            // check the current thread
            if (button3.InvokeRequired == false)
            {
                if (multi == false)
                {
                    button3.Enabled = true;
                    if (MessageBox.Show("The torrent has been created successfully.", "Torrent created", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
                    {
                        progressBar1.Value = 0;
                    }
                }
            }
            else
            {
                // show async progress
                TorrentCompletedDelegate torrentCompleted = new TorrentCompletedDelegate(TorrentCompleted);
                BeginInvoke(torrentCompleted, new object[] { multi });
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            button4.Enabled = checkBox2.Checked;
            button2.Enabled = !checkBox2.Checked;
            button3.Text = (checkBox2.Checked) ? "Create torrents" : "Create torrent...";
            Size = (checkBox2.Checked) ? new Size(Size.Width, Size.Height + 58) : new Size(Size.Width, Size.Height - 58);
            groupBox2.Size = (checkBox2.Checked) ? new Size(groupBox2.Size.Width, groupBox2.Size.Height - 58) : new Size(groupBox2.Size.Width, groupBox2.Size.Height + 58);
            groupBox3.Visible = checkBox2.Checked;          
        }

        private TorrentCreator InitTorrentCreator()
        {
            TorrentCreator tc = new TorrentCreator();
            tc.Hashed += delegate (object o, TorrentCreatorEventArgs ee)
            {
                ShowProgress(ee.FileCompletion, ee.OverallCompletion, ee.OverallSize);
            };
            foreach (string s in this.textBox2.Lines)
            {
                tc.Announces.Add(new RawTrackerTier(new string[] { s }));
            }
            tc.Comment = textBox3.Text;
            if (((KeyValuePair<string, long>)comboBox1.SelectedItem).Value != -1)
            {
                tc.PieceLength = ((KeyValuePair<string, long>)comboBox1.SelectedItem).Value;
            }
            tc.Private = checkBox1.Checked;
            tc.CreatedBy = "";
            return tc;
        }

        private long CalculateOptimumPieceSize(string fileSource)
        {
            long size = 0;
            if (Directory.Exists(fileSource))
            {
                foreach (FileInfo f in new DirectoryInfo(fileSource).GetFiles("*.*", SearchOption.AllDirectories))
                {
                    size += f.Length;
                }
            }
            else
            {
                size = new FileInfo(fileSource).Length;
            }
            long pieces = size / 1337;
            long closest = ((KeyValuePair<string, long>)comboBox1.Items[1]).Value;
            for (int i = 1; i < comboBox1.Items.Count; i++)
            {
                if (Math.Abs(((KeyValuePair<string, long>)comboBox1.Items[i]).Value - pieces) < Math.Abs(closest - pieces))
                {
                    closest = ((KeyValuePair<string, long>)comboBox1.Items[i]).Value;
                }
            }
            return closest;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }

    public static class ExtensionMethods
    {
        public static async Task CreateAsync(this TorrentCreator tc, ITorrentFileSource itfs, FileStream stream)
        {
            byte[] buffer = (await Task.Factory.FromAsync(tc.BeginCreate, tc.EndCreate, itfs, null)).Encode();
            await stream.WriteAsync(buffer, 0, buffer.Length);
            stream.Close();
        }
    }
}
