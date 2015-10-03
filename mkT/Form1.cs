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

namespace mkT
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Dictionary<string, long> comboSource = new Dictionary<string, long>();
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

        private async void button3_Click(object sender, EventArgs e)
        {
            if (File.Exists(textBox1.Text) || Directory.Exists(textBox1.Text))
            {
                TorrentCreator tc = new TorrentCreator();
                tc.Hashed += delegate(object o, TorrentCreatorEventArgs ee)
                {
                    ShowProgress(ee.FileCompletion, ee.OverallCompletion, ee.OverallSize);
                };
                RawTrackerTiers trackers = new RawTrackerTiers();
                foreach (string s in this.textBox2.Lines)
                {
                    tc.Announces.Add(new RawTrackerTier(new string[] { s }));
                }
                tc.Comment = textBox3.Text;
                tc.PieceLength = ((KeyValuePair<string, long>)comboBox1.SelectedItem).Value;
                tc.Private = checkBox1.Checked;
                tc.CreatedBy = "";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    ITorrentFileSource tFS = new TorrentFileSource(textBox1.Text);
                    button3.Enabled = false;
                    tc.BeginCreate(tFS, TorrentCompletedCallback, tc);
                    //BEncodedDictionary t = await Task.Factory.FromAsync(tc.BeginCreate, tc.EndCreate, tFS, null);
                    //button3.Enabled = true;
                }
            }
            else { MessageBox.Show("Please enter a file source.", "No file source specified", MessageBoxButtons.OK, MessageBoxIcon.Stop); }
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

        private void TorrentCompletedCallback(IAsyncResult iar)
        {
            TorrentCreator tc = (TorrentCreator)iar.AsyncState;
            using (FileStream stream = File.OpenWrite(saveFileDialog1.FileName))
            {
                tc.EndCreate(iar, stream);
                TorrentCompleted();
            }
        }

        delegate void TorrentCompletedDelegate();
        private void TorrentCompleted()
        {
            // check the current thread
            if (button3.InvokeRequired == false)
            {
                button3.Enabled = true;
                if (MessageBox.Show("The torrent has been created successfully.", "Torrent created", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    progressBar1.Value = 0;
                }
            }
            else
            {
                // show async progress
                TorrentCompletedDelegate torrentCompleted = new TorrentCompletedDelegate(TorrentCompleted);
                BeginInvoke(torrentCompleted);
            }
        }
    }
}
