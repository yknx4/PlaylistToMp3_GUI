using ffmpeg_convert;
using PlaylistToMp3_DLL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Program_Settings = PlaylistToMp3__WF_.Properties.Settings;

namespace PlaylistToMp3__WF_
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            this.SetStyle(
  ControlStyles.AllPaintingInWmPaint |
  ControlStyles.UserPaint |
  ControlStyles.DoubleBuffer, true);
            ThreadPool.SetMinThreads(200, 200);
            InitializeComponent();
        }

#if (DEBUG)
        private StreamWriter logfile;
#endif

        private int numberOfThreads = Environment.ProcessorCount * 2;
        private BindingList<MusicFile> playlist;
        private int thread_no = Environment.ProcessorCount;
        private Queue<Tuple<BackgroundWorker, convertArgs>> Conversions = new Queue<Tuple<BackgroundWorker, convertArgs>>();

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //OpenFileDialog m_open = new OpenFileDialog();
            //m_open.Multiselect = false;
            openPlaylistDialog();

        }


        private void btnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (dtgrPlaylist.SelectedRows.Count == 1)
            {
                playlist.Remove((MusicFile)dtgrPlaylist.SelectedRows[0].DataBoundItem);
                log(((MusicFile)dtgrPlaylist.SelectedRows[0].DataBoundItem).ShortFileName + " song removed from playlist.");
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            log("Conversions started");
            pgrConvert.Value = 0;
            BeginRefreshDatagrid();
            Convert();
        }

        private void runNewConversion(object sender, RunWorkerCompletedEventArgs e)
        {
            startNewConversion();
        }



        private void source_Converted(object sender, EventArgs e)
        {
            //MessageBox.Show("Converted File!");

            pgrConvert.Value++;
            tslblStatus.Text = "Converting (" + pgrConvert.Value + "/" + playlist.Count + ").";
        }

        private void convertingProcess(object sender, DoWorkEventArgs e)
        {
#if (DEBUG)
            //MessageBox.Show("Worker Called");
#endif
            convertArgs Argument = (convertArgs)e.Argument;
            FFmpeg mFFmepg = new FFmpeg();
            mFFmepg.Converter.ProgressChanged += (send, eargs) =>
            {
                BackgroundWorker origin = sender as BackgroundWorker;
                int progress = System.Convert.ToInt32(eargs.Progress * 100);
                origin.ReportProgress(progress);
            };
            e.Result = mFFmepg.Converter.ToMp3(Argument.Input.FileInformation, Argument.Output, Argument.Arguments);
            //txtLog.Text+=(mFFmepg.Converter.LastError);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string fPath = txtOuput.Text;
            if (folderDialog_output.ShowDialog() == DialogResult.OK)
            {
                txtOuput.Text = folderDialog_output.SelectedPath;
                if (txtOuput.Text != fPath)
                {
                    log("Path changed to: " + txtOuput.Text);
                    Program_Settings.Default.OutputPath = txtOuput.Text;
                    SaveSettings();
                }
            }
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PlaylistPath != null && PlaylistPath.Exists)
            {
                LoadPlaylist(PlaylistPath.FullName);
            }
            else
            {
                openPlaylistDialog();
            }
        }

        private void rdbCBR_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rdbSource = sender as RadioButton;
            if (rdbSource.Checked)
            {
                switch (rdbSource.Tag.ToString())
                {
                    case "CBR":
                        log("CBR option selected.");
                        cmbPresets.DataSource = Extensions.CBR;
                        cmbPresets.SelectedItem = Extensions.CBR.Last();
                        Program_Settings.Default.isVariable = false;
                        break;

                    case "VBR":
                        log("VBR option selected.");
                        cmbPresets.DataSource = Extensions.VBR;
                        cmbPresets.SelectedIndex = 2;
                        Program_Settings.Default.isVariable = true;
                        break;

                    default:
                        break;
                }
                SaveSettings();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            PlaylistLoader.ErrorThrown += (EventArg) =>
            {
                log("Playlist loader error: " + EventArg.Error);
                //Dirty hack to cross thread execution
                if (txtLog.InvokeRequired)
                {
                    txtLog.BeginInvoke((MethodInvoker)delegate()
                    {
                        tslblStatus.Text = "Playlist loader error";
                    });
                }
                else tslblStatus.Text = "Playlist loader error";
            };
            PlaylistLoader.Log += (EventArg) =>
            {
                log("Playlist loader message: " + EventArg.Message);

            };
#if (DEBUG)
            string log_path = DateTime.Now.Year + "." + DateTime.Now.Ticks + " log.txt";
            File.Create(log_path).Close();
            logfile = File.AppendText(log_path);
#endif
            log("pConverter main window Loaded.");
            int per_preset = Program_Settings.Default.Preset;
            int per_minbitrate = Program_Settings.Default.MinBitrate;
            if (Program_Settings.Default.isVariable)
            {
                cmbPresets.DataSource = Extensions.VBR;
            }
            else
            {
                cmbPresets.DataSource = Extensions.CBR;
            }

            cmbPresets.SelectedIndex = per_preset;
            string outPath = Program_Settings.Default.OutputPath;
            if (outPath != string.Empty && new FileInfo(outPath).Directory.Exists)
            {
                txtOuput.Text = outPath;
            }
            else
            {
                txtOuput.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                Program_Settings.Default.OutputPath = txtOuput.Text;
                SaveSettings();
            }

            folderDialog_output.SelectedPath = txtOuput.Text;
            cmbMinBR.DataSource = Extensions.CBR.ToArray();
            cmbMinBR.SelectedIndex = per_minbitrate;
            log("Preferences loaded.");
        }

        private void outputPresetChanged(object sender, EventArgs e)
        {
            ComboBox source = sender as ComboBox;
            if (Program_Settings.Default.Preset != source.SelectedIndex)
            {
                Program_Settings.Default.Preset = source.SelectedIndex;
                SaveSettings();
            }
            log("Output preset set to: " + source.SelectedValue);
        }

        private void minBitrateChanged(object sender, EventArgs e)
        {
            ComboBox source = sender as ComboBox;
            if (Program_Settings.Default.MinBitrate != source.SelectedIndex)
            {
                Program_Settings.Default.MinBitrate = source.SelectedIndex;
                SaveSettings();
            }
            log("Conversion minimum bitrate set to: " + source.SelectedValue);
        }

        private void whenClosing(object sender, FormClosingEventArgs e)
        {
            Program_Settings.Default.Save();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }
    }
}